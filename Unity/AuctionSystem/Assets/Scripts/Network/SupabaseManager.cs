using System;
using System.Collections;
using System.Text;
using AuctionSystem.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace AuctionSystem.Network
{
    public class SupabaseManager : MonoBehaviour
    {
        public static SupabaseManager Instance { get; private set; }

        public UserSession Session { get; private set; }
        public bool IsLoggedIn => Session != null && !string.IsNullOrEmpty(Session.access_token);

        private bool _isRefreshing;
        private System.Collections.Generic.Queue<Action<bool>> _refreshWaiters
            = new System.Collections.Generic.Queue<Action<bool>>();

        public Profile CurrentProfile { get; private set; }

        public event Action<UserSession> OnSessionChanged;
        public event Action<Profile> OnProfileLoaded;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private bool IsTokenExpiredOrExpiringSoon()
        {
            if (Session == null) return false;
            if (Session.expires_at <= 0) return true;
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return nowUnix >= Session.expires_at - 60;
        }

        public IEnumerator EnsureFreshToken(Action<bool> onResult)
        {
            if (!IsLoggedIn) { onResult?.Invoke(false); yield break; }
            if (!IsTokenExpiredOrExpiringSoon()) { onResult?.Invoke(true); yield break; }

            if (_isRefreshing)
            {
                bool waited = false;
                bool result = false;
                _refreshWaiters.Enqueue(ok => { result = ok; waited = true; });
                yield return new WaitUntil(() => waited);
                onResult?.Invoke(result);
                yield break;
            }

            if (string.IsNullOrEmpty(Session.refresh_token))
            {
                // refresh_token 없고 expires_at도 없으면 현재 토큰으로 진행 (로그인 직후 상태)
                if (Session.expires_at <= 0)
                {
                    Debug.LogWarning("[SupabaseManager] expires_at·refresh_token 모두 없음 — 현재 토큰으로 진행");
                    onResult?.Invoke(true);
                    yield break;
                }
                Debug.LogWarning("[SupabaseManager] refresh_token 없음 — 세션 삭제, 재로그인 필요");
                ClearSession();
                onResult?.Invoke(false);
                yield break;
            }

            _isRefreshing = true;
            bool refreshOk = false;
            yield return RefreshAccessToken(Session.refresh_token, ok => refreshOk = ok);
            _isRefreshing = false;

            while (_refreshWaiters.Count > 0)
                _refreshWaiters.Dequeue()?.Invoke(refreshOk);

            onResult?.Invoke(refreshOk);
        }

        private IEnumerator RefreshAccessToken(string refreshToken, Action<bool> onResult = null)
        {
            string url = $"{SupabaseConfig.AuthUrl}/token?grant_type=refresh_token";
            string body = $"{{\"refresh_token\":\"{refreshToken}\"}}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", SupabaseConfig.AnonKey);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var newSession = JsonUtility.FromJson<UserSession>(req.downloadHandler.text);
                if (newSession != null && !string.IsNullOrEmpty(newSession.access_token))
                {
                    Debug.Log("[SupabaseManager] 토큰 갱신 성공 — 새 access_token 저장");
                    SaveSession(newSession);
                    onResult?.Invoke(true);
                    yield break;
                }
            }

            Debug.LogWarning("[SupabaseManager] 토큰 갱신 실패 — 세션 삭제, 재로그인 필요\n" +
                             req.downloadHandler.text);
            ClearSession();
            onResult?.Invoke(false);
        }

        private void SaveSession(UserSession session)
        {
            if (session.expires_at <= 0 && session.expires_in > 0)
                session.expires_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + session.expires_in;

            Session = session;
            OnSessionChanged?.Invoke(session);
        }

        private static string GetJwtAlg(string jwt)
        {
            if (string.IsNullOrEmpty(jwt)) return "null";
            try
            {
                string[] parts  = jwt.Split('.');
                if (parts.Length < 2) return "invalid";
                string header   = parts[0];
                header = header.Replace('-', '+').Replace('_', '/');
                switch (header.Length % 4)
                {
                    case 2: header += "=="; break;
                    case 3: header += "=";  break;
                }
                string decoded = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(header));
                var match = System.Text.RegularExpressions.Regex.Match(decoded, "\"alg\"\\s*:\\s*\"([^\"]+)\"");
                return match.Success ? match.Groups[1].Value : "unknown";
            }
            catch { return "parse_error"; }
        }

        public void ClearSession()
        {
            Session = null;
            CurrentProfile = null;
            OnSessionChanged?.Invoke(null);
        }

        public IEnumerator Get(string url, Action<string> onSuccess, Action<string> onError, bool useAuth = true)
        {
            if (useAuth)
            {
                bool tokenOk = false;
                yield return EnsureFreshToken(ok => tokenOk = ok);
                if (!tokenOk && IsTokenExpiredOrExpiringSoon())
                {
                    onError?.Invoke("세션이 만료됐습니다. 다시 로그인해 주세요.");
                    yield break;
                }
            }

            using var req = UnityWebRequest.Get(url);
            SetCommonHeaders(req, useAuth);
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        public IEnumerator Post(string url, string jsonBody, Action<string> onSuccess, Action<string> onError,
            bool useAuth = true, System.Collections.Generic.Dictionary<string, string> extraHeaders = null)
        {
            if (useAuth)
            {
                bool tokenOk = false;
                yield return EnsureFreshToken(ok => tokenOk = ok);
                if (!tokenOk)
                {
                    onError?.Invoke("세션이 만료됐습니다. 다시 로그인해 주세요.");
                    yield break;
                }
                long nowTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long expiresAt = Session?.expires_at ?? 0;
                string alg = GetJwtAlg(Session?.access_token);
                string tokenFull = Session?.access_token ?? "";
                Debug.Log($"[SupabaseManager] POST {url}\n  alg={alg}  expires_at={expiresAt}  now={nowTs}  남은시간={expiresAt - nowTs}초\n  token(full)={tokenFull}");
            }

            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();

            // UploadHandler 세팅 후 헤더 지정 (순서 중요)
            req.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
            req.SetRequestHeader("Content-Type", "application/json");
            if (useAuth && IsLoggedIn)
                req.SetRequestHeader("Authorization", "Bearer " + Session.access_token);

            // REST 요청에만 스키마 헤더 추가 (Edge Function 제외)
            bool isEdgeFn = url.Contains("/functions/v1/");
            if (!isEdgeFn)
            {
                req.SetRequestHeader("Accept-Profile",  "public");
                req.SetRequestHeader("Content-Profile", "public");
            }

            if (extraHeaders != null)
                foreach (var kv in extraHeaders)
                    req.SetRequestHeader(kv.Key, kv.Value);

            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        public IEnumerator Patch(string url, string jsonBody, Action<string> onSuccess, Action<string> onError)
        {
            bool tokenOk = false;
            yield return EnsureFreshToken(ok => tokenOk = ok);
            if (!tokenOk) { onError?.Invoke("세션이 만료됐습니다."); yield break; }

            using var req = new UnityWebRequest(url, "PATCH");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            SetCommonHeaders(req, true);
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        public IEnumerator Delete(string url, Action<string> onSuccess, Action<string> onError)
        {
            bool tokenOk = false;
            yield return EnsureFreshToken(ok => tokenOk = ok);
            if (!tokenOk) { onError?.Invoke("세션이 만료됐습니다."); yield break; }

            using var req = new UnityWebRequest(url, "DELETE");
            req.downloadHandler = new DownloadHandlerBuffer();
            SetCommonHeaders(req, true);
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        private void SetCommonHeaders(UnityWebRequest req, bool useAuth)
        {
            req.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
            req.SetRequestHeader("Content-Type", "application/json");
            // Edge Function URL에는 PostgREST 스키마 헤더 붙이지 않음
            bool isEdgeFunction = req.url.Contains("/functions/v1/");
            if (!isEdgeFunction)
            {
                req.SetRequestHeader("Accept-Profile", "public");
                req.SetRequestHeader("Content-Profile", "public");
            }
            if (useAuth && IsLoggedIn)
                req.SetRequestHeader("Authorization", "Bearer " + Session.access_token);
        }

        private void HandleResponse(UnityWebRequest req, Action<string> onSuccess, Action<string> onError)
        {
            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(req.downloadHandler.text);
            }
            else
            {
                string errBody = req.downloadHandler?.text;
                string errMsg  = req.error;
                Debug.LogWarning($"[SupabaseManager] HTTP {req.responseCode} {req.url}\n  error={errMsg}\n  body={errBody}");
                onError?.Invoke(errBody ?? errMsg);
            }
        }

        public IEnumerator LoadProfile()
        {
            if (!IsLoggedIn) yield break;

            string url = $"{SupabaseConfig.RestUrl}/profiles?id=eq.{Session.user.id}&limit=1";
            yield return Get(url,
                onSuccess: json =>
                {
                    string wrapped = "{\"items\":" + json + "}";
                    var wrapper = JsonUtility.FromJson<ProfileListWrapper>(wrapped);
                    if (wrapper.items != null && wrapper.items.Length > 0)
                    {
                        CurrentProfile = wrapper.items[0];
                        OnProfileLoaded?.Invoke(CurrentProfile);
                    }
                },
                onError: err => Debug.LogWarning("[Supabase] 프로필 로드 실패: " + err)
            );
        }

        [Serializable]
        private class ProfileListWrapper { public Profile[] items; }

        public void SetSession(UserSession session)
        {
            SaveSession(session);
            StartCoroutine(LoadProfile());
        }
    }
}
