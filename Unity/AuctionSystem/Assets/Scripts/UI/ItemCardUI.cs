using System;
using AuctionSystem.Models;
using AuctionSystem.Network;
using AuctionSystem.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class ItemCardUI : MonoBehaviour
    {
        [SerializeField] private RawImage  itemImage;
        [SerializeField] private TMP_Text  nameText;
        [SerializeField] private TMP_Text  currentBidText;
        [SerializeField] private TMP_Text  buyoutText;
        [SerializeField] private TMP_Text  timeLeftText;
        [SerializeField] private TMP_Text  categoryText;
        [SerializeField] private Button    selectButton;

        private AuctionItem _item;
        private Action      _onSelect;
        private string      _loadedUrl;

        public void Setup(AuctionItem item, Action onSelect)
        {
            _item     = item;
            _onSelect = onSelect;

            if (nameText != null)       nameText.text       = item.name;
            if (categoryText != null)   categoryText.text   = item.category;
            if (currentBidText != null) currentBidText.text = $"현재가: {item.current_bid:N0}G";
            if (buyoutText != null)     buyoutText.text     = item.buyout_price > 0
                                                               ? $"즉시구매: {item.buyout_price:N0}G"
                                                               : "즉시구매 없음";

            if (selectButton != null) selectButton.onClick.AddListener(() => _onSelect?.Invoke());

            LoadItemImage(item.image_url);
            UpdateTimeLeft();
        }

        void Update()
        {
            UpdateTimeLeft();
        }

        private void LoadItemImage(string url)
        {
            if (itemImage == null) return;

            if (_loadedUrl == url) return;

            if (string.IsNullOrEmpty(url))
            {
                itemImage.texture = null;
                itemImage.color   = new Color(0.2f, 0.2f, 0.2f, 1f);
                _loadedUrl        = url;
                return;
            }

            itemImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            _loadedUrl      = url;

            ImageCacheManager.Instance.LoadImage(url, tex =>
            {
                if (this == null || itemImage == null || _loadedUrl != url) return;

                if (tex != null)
                {
                    itemImage.texture = tex;
                    itemImage.color   = Color.white;
                }
                else
                {
                    itemImage.texture = null;
                    itemImage.color   = new Color(0.12f, 0.12f, 0.12f, 1f);
                }
            });
        }

        private void UpdateTimeLeft()
        {
            if (timeLeftText == null || _item == null) return;
            timeLeftText.text = AuctionUtils.GetTimeLeftText(_item.ends_at);
        }
    }
}
