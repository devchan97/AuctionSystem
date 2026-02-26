using System.Collections;
using AuctionSystem.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class NotificationPopupUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Image    bgImage;

        private static readonly Color ColorInfo    = new Color(0.059f, 0.094f, 0.153f, 0.95f); // gray-900
        private static readonly Color ColorBid     = new Color(0.067f, 0.388f, 0.173f, 0.95f); // green-dark
        private static readonly Color ColorBuyout  = new Color(0.608f, 0.278f, 0.020f, 0.95f); // orange-dark
        private static readonly Color ColorOutbid  = new Color(0.498f, 0.067f, 0.067f, 0.95f); // red-dark

        private RectTransform _rt;
        private Coroutine     _routine;

        private const float SlideDistance = 340f;
        private const float SlideDuration = 0.25f;
        private const float DisplayTime   = 3.5f;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            gameObject.SetActive(false);
        }

        public void ShowNotification(Notification notif)
        {
            if (messageText != null) messageText.text = notif.message ?? "";
            if (bgImage != null) bgImage.color = GetColor(notif.type);
            _rt.anchoredPosition = new Vector2(SlideDistance, _rt.anchoredPosition.y);
            gameObject.SetActive(true);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine(notif));
        }

        private IEnumerator ShowRoutine(Notification notif)
        {
            yield return Slide(new Vector2(0, _rt.anchoredPosition.y));
            yield return new WaitForSeconds(DisplayTime);
            yield return Slide(new Vector2(SlideDistance, _rt.anchoredPosition.y));

            gameObject.SetActive(false);
        }

        private IEnumerator Slide(Vector2 target)
        {
            float elapsed = 0f;
            Vector2 start = _rt.anchoredPosition;
            while (elapsed < SlideDuration)
            {
                elapsed += Time.deltaTime;
                _rt.anchoredPosition = Vector2.Lerp(start, target, elapsed / SlideDuration);
                yield return null;
            }
            _rt.anchoredPosition = target;
        }

        private static Color GetColor(string type) => type switch
        {
            "bid_placed"   => ColorBid,
            "outbid"       => ColorOutbid,
            "buyout"       => ColorBuyout,
            "auction_won"  => ColorBid,
            "auction_end"  => ColorInfo,
            _              => ColorInfo,
        };
    }
}
