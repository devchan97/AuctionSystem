using System;
using System.Collections;
using System.Collections.Generic;
using AuctionSystem.Models;
using AuctionSystem.Network;
using AuctionSystem.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class NotificationListUI : MonoBehaviour
    {
        [Header("패널 루트 (토글 대상)")]
        [SerializeField] private GameObject panelRoot;

        [Header("목록 컨테이너")]
        [SerializeField] private Transform listContent;
        [SerializeField] private GameObject itemPrefab;   // NotificationItemPrefab

        [Header("헤더")]
        [SerializeField] private TMP_Text titleText;      // "알림 (N개 미읽음)"
        [SerializeField] private Button closeButton;

        [Header("빈 상태")]
        [SerializeField] private TMP_Text emptyText;

        // 현재 표시 중인 알림 데이터
        private readonly List<Notification> _notifications = new List<Notification>();
        private int _unreadCount;

        void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(HidePanel);
            RealtimeManager.Instance.OnNotificationReceived += OnRealtimeNotif;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        void OnDestroy()
        {
            if (RealtimeManager.Instance != null)
                RealtimeManager.Instance.OnNotificationReceived -= OnRealtimeNotif;
        }

        // Realtime INSERT 이벤트 → 목록 최상단에 추가
        private void OnRealtimeNotif(Notification notif)
        {
            _notifications.Insert(0, notif);
            _unreadCount++;
            UpdateHeader();

            // 패널이 열려 있으면 즉시 새 아이템 추가
            if (panelRoot != null && panelRoot.activeSelf)
                RebuildList();
        }

        public void TogglePanel()
        {
            if (panelRoot == null) return;
            bool nowOpen = !panelRoot.activeSelf;
            panelRoot.SetActive(nowOpen);

            if (nowOpen)
            {
                // 열릴 때 DB에서 최신 목록 다시 로드
                StartCoroutine(FetchAndRebuild());
            }
        }

        public void HidePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private IEnumerator FetchAndRebuild()
        {
            if (!SupabaseManager.Instance.IsLoggedIn) yield break;

            string userId = SupabaseManager.Instance.Session.user.id;
            string url = $"{SupabaseConfig.RestUrl}/notifications"
                       + $"?user_id=eq.{userId}"
                       + "&order=created_at.desc"
                       + "&limit=30";

            yield return SupabaseManager.Instance.Get(url,
                onSuccess: json =>
                {
                    _notifications.Clear();
                    string wrapped = "{\"items\":" + json + "}";
                    var wrapper = JsonUtility.FromJson<NotifWrapper>(wrapped);
                    if (wrapper?.items != null)
                        _notifications.AddRange(wrapper.items);

                    _unreadCount = 0;
                    foreach (var n in _notifications)
                        if (!n.is_read) _unreadCount++;

                    UpdateHeader();
                    RebuildList();

                    // 패널을 열었을 때 전체 읽음 처리
                    if (_unreadCount > 0)
                        StartCoroutine(MarkAllRead(userId));
                },
                onError: err => Debug.LogWarning("[NotificationListUI] 알림 로드 실패: " + err)
            );
        }

        private IEnumerator MarkAllRead(string userId)
        {
            string url = $"{SupabaseConfig.RestUrl}/notifications"
                       + $"?user_id=eq.{userId}&is_read=eq.false";
            string body = "{\"is_read\":true}";

            yield return SupabaseManager.Instance.Patch(url, body,
                onSuccess: _ =>
                {
                    foreach (var n in _notifications) n.is_read = true;
                    _unreadCount = 0;
                    UpdateHeader();
                    RebuildList();
                },
                onError: err => Debug.LogWarning("[NotificationListUI] 읽음 처리 실패: " + err)
            );
        }

        private void RebuildList()
        {
            // 기존 아이템 정리
            foreach (Transform child in listContent)
                Destroy(child.gameObject);

            if (emptyText != null)
                emptyText.gameObject.SetActive(_notifications.Count == 0);

            foreach (var notif in _notifications)
            {
                var go = Instantiate(itemPrefab, listContent);
                var ui = go.GetComponent<NotificationItemUI>();
                if (ui != null)
                    ui.Setup(notif, OnDeleteClicked);
            }
        }

        private void OnDeleteClicked(string notifId)
        {
            // 낙관적 제거
            int idx = _notifications.FindIndex(n => n.id == notifId);
            if (idx < 0) return;
            var removed = _notifications[idx];
            _notifications.RemoveAt(idx);
            if (!removed.is_read && _unreadCount > 0) _unreadCount--;
            UpdateHeader();
            RebuildList();

            // DB 삭제
            StartCoroutine(DeleteFromDb(notifId, removed));
        }

        private IEnumerator DeleteFromDb(string notifId, Notification rollback)
        {
            string url = $"{SupabaseConfig.RestUrl}/notifications?id=eq.{notifId}";
            yield return SupabaseManager.Instance.Delete(url,
                onSuccess: _ => { },
                onError: err =>
                {
                    Debug.LogWarning("[NotificationListUI] 삭제 실패, 롤백: " + err);
                    // 롤백: 다시 삽입 (정렬 유지)
                    _notifications.Add(rollback);
                    _notifications.Sort((a, b) =>
                        string.Compare(b.created_at, a.created_at, StringComparison.Ordinal));
                    if (!rollback.is_read) _unreadCount++;
                    UpdateHeader();
                    RebuildList();
                }
            );
        }

        private void UpdateHeader()
        {
            if (titleText != null)
                titleText.text = _unreadCount > 0
                    ? $"알림 ({_unreadCount})"
                    : "알림";
        }

        [Serializable]
        private class NotifWrapper { public Notification[] items; }
    }
}
