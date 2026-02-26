using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AuctionSystem.Models;
using UnityEngine;

namespace AuctionSystem.Network
{
    // Supabase Realtime - Phoenix Protocol v1.0.0
    // JWT는 URL이 아닌 phx_join payload의 access_token 필드로 전달
    public class RealtimeManager : MonoBehaviour
    {
        public static RealtimeManager Instance { get; private set; }

        public event Action<AuctionItem>  OnBidUpdated;
        public event Action<string>       OnAuctionEnded;
        public event Action<Notification> OnNotificationReceived;
        public event Action               OnLobbyUpdated;
        public event Action<int>          OnViewerCountChanged;

        private NativeWebSocket.WebSocket _ws;
        private bool _isConnected;
        private bool _isConnecting;
        private bool _isDestroyed;
        private int  _refCounter;
        private string _joinRef;

        private readonly Dictionary<string, ChannelConfig> _channels
            = new Dictionary<string, ChannelConfig>();

        private Coroutine _heartbeatCoroutine;
        private Coroutine _tokenRefreshCoroutine;

        private string _presenceTopic;
        private readonly Dictionary<string, int> _presenceKeys = new Dictionary<string, int>();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SubscribeToLobby()
        {
            AddChannel("realtime:lobby", new ChannelConfig
            {
                postgres_changes = new[]
                {
                    new PgChangeFilter { @event = "*", schema = "public", table = "items" }
                }
            });
        }

        public void SubscribeToAuction(string itemId)
        {
            string topic = $"realtime:item-{itemId}";
            AddChannel(topic, new ChannelConfig
            {
                postgres_changes = new[]
                {
                    new PgChangeFilter
                    {
                        @event = "*", schema = "public", table = "items",
                        filter = $"id=eq.{itemId}"
                    }
                }
            });
        }

        public void SubscribeToUserNotifications()
        {
            if (!SupabaseManager.Instance.IsLoggedIn) return;
            string userId = SupabaseManager.Instance.Session.user.id;
            string topic  = $"realtime:notifications-{userId}";
            AddChannel(topic, new ChannelConfig
            {
                postgres_changes = new[]
                {
                    new PgChangeFilter
                    {
                        @event = "INSERT", schema = "public", table = "notifications",
                        filter = $"user_id=eq.{userId}"
                    }
                }
            });
        }

        public void SubscribeItemPresence(string itemId)
        {
            UnsubscribeItemPresence();

            _presenceTopic = $"realtime:item-presence-{itemId}";
            _presenceKeys.Clear();

            if (_isConnected)
                SendPresenceJoin(_presenceTopic);
            else
                Connect();
        }

        public void UnsubscribeItemPresence()
        {
            if (string.IsNullOrEmpty(_presenceTopic)) return;
            SendLeave(_presenceTopic);
            _presenceTopic = null;
            _presenceKeys.Clear();
            OnViewerCountChanged?.Invoke(0);
        }

        public void UnsubscribeAll()
        {
            _channels.Clear();
            CloseSocket();
        }

        private void AddChannel(string topic, ChannelConfig config)
        {
            _channels[topic] = config;
            if (_isConnected)
                SendJoin(topic, config);
            else
                Connect();
        }

        public void Connect()
        {
            if (_isConnected || _isConnecting) return;
            StartCoroutine(ConnectCoroutine());
        }

        private IEnumerator ConnectCoroutine()
        {
            _isConnecting = true;

            if (SupabaseManager.Instance.IsLoggedIn)
            {
                bool ok = false;
                yield return SupabaseManager.Instance.EnsureFreshToken(r => ok = r);
                if (!ok)
                {
                    Debug.LogWarning("[Realtime] 토큰 갱신 실패 — 연결 중단");
                    _isConnecting = false;
                    yield break;
                }
            }

            string url = $"{SupabaseConfig.RealtimeUrl}?apikey={SupabaseConfig.AnonKey}&vsn=1.0.0";
            _joinRef = NextRef();

            _ws = new NativeWebSocket.WebSocket(url);

            _ws.OnOpen += () =>
            {
                Debug.Log("[Realtime] 연결됨");
                _isConnected  = true;
                _isConnecting = false;
                _heartbeatCoroutine     = StartCoroutine(HeartbeatCoroutine());
                _tokenRefreshCoroutine  = StartCoroutine(TokenRefreshCoroutine());
                foreach (var kv in _channels)
                    SendJoin(kv.Key, kv.Value);
                if (!string.IsNullOrEmpty(_presenceTopic))
                    SendPresenceJoin(_presenceTopic);
            };

            _ws.OnClose += code =>
            {
                if (_isDestroyed) return;
                Debug.Log($"[Realtime] 연결 종료: {code}");
                _isConnected  = false;
                _isConnecting = false;
                if (_heartbeatCoroutine    != null) StopCoroutine(_heartbeatCoroutine);
                if (_tokenRefreshCoroutine != null) StopCoroutine(_tokenRefreshCoroutine);
                StartCoroutine(ReconnectAfterDelay(5f));
            };

            _ws.OnError   += err  => { if (!_isDestroyed) Debug.LogWarning("[Realtime] 오류: " + err); };
            _ws.OnMessage += data => { if (!_isDestroyed) HandleMessage(Encoding.UTF8.GetString(data)); };

            yield return _ws.Connect();
        }

        private IEnumerator ReconnectAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Connect();
        }

        private void CloseSocket()
        {
            _isConnected = false;
            if (_heartbeatCoroutine    != null) StopCoroutine(_heartbeatCoroutine);
            if (_tokenRefreshCoroutine != null) StopCoroutine(_tokenRefreshCoroutine);
            if (_ws != null) _ = _ws.Close();
        }

        private void HandleMessage(string json)
        {
            try
            {
                var msg = JsonUtility.FromJson<RtMessage>(json);
                if (msg == null) return;

                switch (msg.@event)
                {
                    case "phx_reply":
                        Debug.Log($"[Realtime] phx_reply [{msg.topic}]: {json}");
                        break;

                    case "postgres_changes":
                        HandlePostgresChange(msg);
                        break;

                    case "presence_state":
                        HandlePresenceState(msg);
                        break;

                    case "presence_diff":
                        HandlePresenceDiff(msg);
                        break;

                    case "system":
                        Debug.Log($"[Realtime] system: {json}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Realtime] 파싱 실패: {e.Message}\n{json}");
            }
        }

        private void HandlePostgresChange(RtMessage msg)
        {
            if (msg.payload?.data == null) return;

            var data  = msg.payload.data;
            var topic = msg.topic ?? "";

            if (topic.StartsWith("realtime:lobby") || topic.StartsWith("realtime:item-"))
            {
                if (data.type == "INSERT" || data.type == "UPDATE")
                {
                    var r = data.record;
                    if (r == null) return;

                    if (r.status == "active")
                        OnBidUpdated?.Invoke(RecordToAuctionItem(r));
                    else if (r.status == "sold" || r.status == "expired")
                        OnAuctionEnded?.Invoke(r.id);
                    OnLobbyUpdated?.Invoke();
                }
            }
            else if (topic.StartsWith("realtime:notifications-"))
            {
                if (data.type == "INSERT" && data.record != null)
                    OnNotificationReceived?.Invoke(RecordToNotification(data.record));
            }
        }

        private static AuctionItem RecordToAuctionItem(RtRecord r) => new AuctionItem
        {
            id          = r.id,
            name        = r.name,
            description = r.description,
            category    = r.category,
            start_price = r.start_price,
            current_bid = r.current_bid,
            buyout_price= r.buyout_price,
            status      = r.status,
            ends_at     = r.ends_at,
            seller_id   = r.seller_id,
            image_url   = r.image_url,
            created_at  = r.created_at,
        };

        private static Notification RecordToNotification(RtRecord r) => new Notification
        {
            id         = r.id,
            user_id    = r.user_id,
            type       = r.type,
            message    = r.message,
            is_read    = r.is_read,
            created_at = r.created_at,
        };

        private void HandlePresenceState(RtMessage msg)
        {
            if (msg.topic != _presenceTopic) return;
            _presenceKeys.Clear();
            if (msg.payload?.presences != null)
            {
                foreach (var p in msg.payload.presences)
                    _presenceKeys[p.key] = 1;
            }
            OnViewerCountChanged?.Invoke(_presenceKeys.Count);
        }

        private void HandlePresenceDiff(RtMessage msg)
        {
            if (msg.topic != _presenceTopic) return;
            if (msg.payload?.joins != null)
                foreach (var p in msg.payload.joins)
                    _presenceKeys[p.key] = 1;
            if (msg.payload?.leaves != null)
                foreach (var p in msg.payload.leaves)
                    _presenceKeys.Remove(p.key);
            OnViewerCountChanged?.Invoke(_presenceKeys.Count);
        }

        private void SendPresenceJoin(string topic)
        {
            string userId = SupabaseManager.Instance.IsLoggedIn
                ? SupabaseManager.Instance.Session.user.id
                : System.Guid.NewGuid().ToString();
            string token = SupabaseManager.Instance.IsLoggedIn
                ? SupabaseManager.Instance.Session.access_token
                : "";

            // phx_join with presence enabled
            string joinJson = "{"
                + $"\"topic\":\"{topic}\","
                + "\"event\":\"phx_join\","
                + "\"payload\":{"
                + "\"config\":{"
                + "\"broadcast\":{\"ack\":false,\"self\":true},"
                + "\"presence\":{\"enabled\":true},"
                + "\"postgres_changes\":[],"
                + "\"private\":false"
                + "},"
                + $"\"access_token\":\"{token}\""
                + "},"
                + $"\"ref\":\"{NextRef()}\","
                + $"\"join_ref\":\"{_joinRef}\""
                + "}";
            SendRaw(joinJson);

            string trackJson = "{"
                + $"\"topic\":\"{topic}\","
                + "\"event\":\"presence\","
                + "\"payload\":{"
                + "\"event\":\"track\","
                + $"\"payload\":{{\"user_id\":\"{userId}\"}}"
                + "},"
                + $"\"ref\":\"{NextRef()}\","
                + $"\"join_ref\":\"{_joinRef}\""
                + "}";
            SendRaw(trackJson);
        }

        private void SendLeave(string topic)
        {
            string leaveJson = "{"
                + $"\"topic\":\"{topic}\","
                + "\"event\":\"phx_leave\","
                + "\"payload\":{},"
                + $"\"ref\":\"{NextRef()}\","
                + $"\"join_ref\":\"{_joinRef}\""
                + "}";
            SendRaw(leaveJson);
        }

        private void SendJoin(string topic, ChannelConfig config)
        {
            string token = SupabaseManager.Instance.IsLoggedIn
                ? SupabaseManager.Instance.Session.access_token
                : "";

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"topic\":\"{topic}\",");
            sb.Append("\"event\":\"phx_join\",");
            sb.Append("\"payload\":{");
            sb.Append("\"config\":{");
            sb.Append("\"broadcast\":{\"ack\":false,\"self\":false},");
            sb.Append("\"presence\":{\"enabled\":false},");
            sb.Append("\"postgres_changes\":[");
            for (int i = 0; i < config.postgres_changes.Length; i++)
            {
                var f = config.postgres_changes[i];
                if (i > 0) sb.Append(",");
                sb.Append("{");
                sb.Append($"\"event\":\"{f.@event}\",");
                sb.Append($"\"schema\":\"{f.schema}\",");
                sb.Append($"\"table\":\"{f.table}\"");
                if (!string.IsNullOrEmpty(f.filter))
                    sb.Append($",\"filter\":\"{f.filter}\"");
                sb.Append("}");
            }
            sb.Append("],");
            sb.Append("\"private\":false");
            sb.Append("},");
            sb.Append($"\"access_token\":\"{token}\"");
            sb.Append("},");
            sb.Append($"\"ref\":\"{NextRef()}\",");
            sb.Append($"\"join_ref\":\"{_joinRef}\"");
            sb.Append("}");

            SendRaw(sb.ToString());
        }

        private void SendAccessTokenRefresh(string topic, string newToken)
        {
            string json = "{"
                + $"\"topic\":\"{topic}\","
                + "\"event\":\"access_token\","
                + $"\"payload\":{{\"access_token\":\"{newToken}\"}},"
                + $"\"ref\":\"{NextRef()}\","
                + $"\"join_ref\":\"{_joinRef}\""
                + "}";
            SendRaw(json);
        }

        private IEnumerator HeartbeatCoroutine()
        {
            while (_isConnected)
            {
                yield return new WaitForSeconds(25f);
                string hb = "{"
                    + "\"topic\":\"phoenix\","
                    + "\"event\":\"heartbeat\","
                    + "\"payload\":{},"
                    + $"\"ref\":\"{NextRef()}\""
                    + "}";
                SendRaw(hb);
            }
        }

        private IEnumerator TokenRefreshCoroutine()
        {
            while (_isConnected)
            {
                yield return new WaitForSeconds(60f);

                if (!SupabaseManager.Instance.IsLoggedIn) continue;

                var session = SupabaseManager.Instance.Session;
                if (session == null || session.expires_at <= 0) continue;

                long nowUnix    = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long remaining  = session.expires_at - nowUnix;
                if (remaining > 120) continue;

                bool ok = false;
                yield return SupabaseManager.Instance.EnsureFreshToken(r => ok = r);
                if (!ok) continue;

                string newToken = SupabaseManager.Instance.Session.access_token;
                Debug.Log("[Realtime] 토큰 갱신 — 모든 채널에 새 토큰 전송");
                foreach (var topic in _channels.Keys)
                    SendAccessTokenRefresh(topic, newToken);
            }
        }

        private void SendRaw(string json)
        {
            if (_ws != null && _isConnected)
                _ = _ws.SendText(json);
        }

        private string NextRef() => (++_refCounter).ToString();

        void Update()
        {
            _ws?.DispatchMessageQueue();
        }

        void OnDestroy()
        {
            _isDestroyed = true;
            _isConnected = false;
            if (_heartbeatCoroutine    != null) StopCoroutine(_heartbeatCoroutine);
            if (_tokenRefreshCoroutine != null) StopCoroutine(_tokenRefreshCoroutine);
            if (_ws != null) _ = _ws.Close();
        }

        private class ChannelConfig
        {
            public PgChangeFilter[] postgres_changes;
        }

        private class PgChangeFilter
        {
            public string @event;
            public string schema;
            public string table;
            public string filter; // optional, e.g. "id=eq.xxx"
        }

        [Serializable]
        private class RtMessage
        {
            public string    topic;
            public string    @event;
            public RtPayload payload;
            public string    @ref;
        }

        [Serializable]
        private class RtPayload
        {
            public RtData        data;
            public string        status;
            public PresenceEntry[] presences;
            public PresenceEntry[] joins;
            public PresenceEntry[] leaves;
        }

        [Serializable]
        private class PresenceEntry
        {
            public string key;
        }

        [Serializable]
        private class RtData
        {
            public string   type;             // INSERT | UPDATE | DELETE
            public string   schema;
            public string   table;
            public string   commit_timestamp;
            public RtRecord record;
        }

        [Serializable]
        private class RtRecord
        {
            // items
            public string id;
            public string name;
            public string description;
            public string category;
            public long   start_price;
            public long   current_bid;
            public long   buyout_price;
            public string status;
            public string ends_at;
            public string seller_id;
            public string image_url;
            // notifications
            public string user_id;
            public string item_id;
            public string type;
            public string message;
            public bool   is_read;
            public string created_at;
        }
    }
}
