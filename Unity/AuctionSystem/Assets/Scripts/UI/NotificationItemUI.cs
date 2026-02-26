using System;
using AuctionSystem.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class NotificationItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text   messageText;
        [SerializeField] private TMP_Text   timeText;
        [SerializeField] private Button     deleteButton;
        [SerializeField] private Image      bgImage;        // 미읽음 강조용

        private static readonly Color UnreadBg = new Color(0.93f, 0.96f, 1f);   // blue-50
        private static readonly Color ReadBg   = new Color(0.98f, 0.98f, 0.98f); // gray-50

        public void Setup(Notification notif, Action<string> onDelete)
        {
            if (messageText != null) messageText.text = notif.message ?? "";
            if (timeText != null)
                timeText.text = FormatTime(notif.created_at);
            if (bgImage != null)
                bgImage.color = notif.is_read ? ReadBg : UnreadBg;

            if (deleteButton != null)
                deleteButton.onClick.AddListener(() => onDelete?.Invoke(notif.id));
        }

        private static string FormatTime(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "";
            if (DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
            {
                dt = dt.ToLocalTime();
                TimeSpan diff = DateTime.Now - dt;
                if (diff.TotalMinutes < 1)  return "방금 전";
                if (diff.TotalHours   < 1)  return $"{(int)diff.TotalMinutes}분 전";
                if (diff.TotalDays    < 1)  return $"{(int)diff.TotalHours}시간 전";
                if (diff.TotalDays    < 7)  return $"{(int)diff.TotalDays}일 전";
                return dt.ToString("MM/dd HH:mm");
            }
            return iso;
        }
    }
}
