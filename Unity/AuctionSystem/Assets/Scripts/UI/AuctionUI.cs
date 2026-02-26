using System;
using AuctionSystem.Auction;
using AuctionSystem.Auth;
using AuctionSystem.Models;
using AuctionSystem.Network;
using AuctionSystem.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class AuctionUI : MonoBehaviour
    {
        [Header("목록")]
        [SerializeField] private Transform itemListContent;
        [SerializeField] private GameObject itemCardPrefab;

        [Header("필터/정렬")]
        [SerializeField] private TMP_Dropdown categoryDropdown;
        [SerializeField] private TMP_Dropdown sortDropdown;

        [Header("상세 패널")]
        [SerializeField] private BidUI bidUI;

        [Header("유저 정보")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text usernameText;

        [Header("기타")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button listItemButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button notificationButton;
        [SerializeField] private NotificationPopupUI notificationPopup;
        [SerializeField] private NotificationListUI notificationListUI;

        [Header("패널 전환")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject auctionPanel;
        [SerializeField] private GameObject listItemPanel;
        [SerializeField] private GameObject inventoryPanel;

        private string[] _sortKeys = { "created_at", "current_bid", "ends_at" };

        void Start()
        {
            if (refreshButton != null)      refreshButton.onClick.AddListener(RefreshItems);
            if (logoutButton != null)       logoutButton.onClick.AddListener(OnLogoutClicked);
            if (listItemButton != null)     listItemButton.onClick.AddListener(ToggleListItemPanel);
            if (inventoryButton != null)    inventoryButton.onClick.AddListener(ToggleInventoryPanel);
            if (notificationButton != null) notificationButton.onClick.AddListener(ToggleNotificationPanel);
            if (categoryDropdown != null) categoryDropdown.onValueChanged.AddListener(_ => RefreshItems());
            if (sortDropdown != null) sortDropdown.onValueChanged.AddListener(_ => RefreshItems());

            SupabaseManager.Instance.OnProfileLoaded += UpdateGoldDisplay;
            if (SupabaseManager.Instance.CurrentProfile != null)
                UpdateGoldDisplay(SupabaseManager.Instance.CurrentProfile);

            RealtimeManager.Instance.OnLobbyUpdated += RefreshItems;
            RealtimeManager.Instance.OnNotificationReceived += OnNotificationReceived;
            RealtimeManager.Instance.SubscribeToLobby();
            RealtimeManager.Instance.SubscribeToUserNotifications();

            RefreshItems();
        }

        void OnDestroy()
        {
            if (SupabaseManager.Instance != null)
                SupabaseManager.Instance.OnProfileLoaded -= UpdateGoldDisplay;
            if (RealtimeManager.Instance != null)
            {
                RealtimeManager.Instance.OnLobbyUpdated -= RefreshItems;
                RealtimeManager.Instance.OnNotificationReceived -= OnNotificationReceived;
            }
        }

        public void RefreshItems()
        {
            SetStatus("불러오는 중...");
            int catIdx = categoryDropdown != null ? categoryDropdown.value : 0;
            string category = catIdx < AuctionUtils.CategoryValues.Length ? AuctionUtils.CategoryValues[catIdx] : "";
            string sortBy   = _sortKeys[sortDropdown != null ? sortDropdown.value : 0];

            AuctionManager.Instance.GetActiveItems(category, sortBy,
                onSuccess: items =>
                {
                    ClearItemList();
                    if (items.Length == 0) { SetStatus("경매 중인 아이템이 없습니다."); return; }
                    SetStatus("");
                    foreach (var item in items)
                        CreateItemCard(item);
                },
                onError: err => SetStatus("오류: " + err)
            );
        }

        private void ClearItemList()
        {
            foreach (Transform child in itemListContent)
                Destroy(child.gameObject);
        }

        private void CreateItemCard(AuctionItem item)
        {
            if (itemCardPrefab == null || itemListContent == null) return;
            var card = Instantiate(itemCardPrefab, itemListContent);
            var cardUI = card.GetComponent<ItemCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(item, () => OpenBidPanel(item));
            }
            else
            {
                var text = card.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = $"{item.name}\n현재가: {item.current_bid:N0}G\n즉시: {item.buyout_price:N0}G";
                var btn = card.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    var capturedItem = item;
                    btn.onClick.AddListener(() => OpenBidPanel(capturedItem));
                }
            }
        }

        private void OpenBidPanel(AuctionItem item)
        {
            if (bidUI != null) bidUI.Open(item);
        }

        private void UpdateGoldDisplay(Profile profile)
        {
            if (goldText != null)    goldText.text    = $"Gold: {profile.gold:N0}G";
            if (usernameText != null) usernameText.text = profile.username ?? profile.id;
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        private void ToggleListItemPanel()
        {
            if (listItemPanel == null) return;
            bool opening = !listItemPanel.activeSelf;
            if (opening)
            {
                // 다른 패널 닫기
                if (inventoryPanel != null && inventoryPanel.activeSelf)
                    inventoryPanel.SetActive(false);
                if (bidUI != null && bidUI.gameObject.activeSelf)
                    bidUI.gameObject.SetActive(false);
                if (notificationListUI != null && notificationListUI.gameObject.activeSelf)
                    notificationListUI.gameObject.SetActive(false);
            }
            listItemPanel.SetActive(opening);
        }

        private void ToggleInventoryPanel()
        {
            if (inventoryPanel == null) return;
            bool opening = !inventoryPanel.activeSelf;
            if (opening)
            {
                // 다른 패널 닫기
                if (listItemPanel != null && listItemPanel.activeSelf)
                    listItemPanel.SetActive(false);
                if (bidUI != null && bidUI.gameObject.activeSelf)
                    bidUI.gameObject.SetActive(false);
                if (notificationListUI != null && notificationListUI.gameObject.activeSelf)
                    notificationListUI.gameObject.SetActive(false);
            }
            inventoryPanel.SetActive(opening);
        }

        private void ToggleNotificationPanel()
        {
            if (notificationListUI == null) return;
            bool opening = !notificationListUI.gameObject.activeSelf;
            if (opening)
            {
                // 다른 패널 닫기
                if (listItemPanel != null && listItemPanel.activeSelf)
                    listItemPanel.SetActive(false);
                if (inventoryPanel != null && inventoryPanel.activeSelf)
                    inventoryPanel.SetActive(false);
                if (bidUI != null && bidUI.gameObject.activeSelf)
                    bidUI.gameObject.SetActive(false);
            }
            notificationListUI.TogglePanel();
        }

        private void OnNotificationReceived(Models.Notification notif)
        {
            if (notificationPopup != null)
                notificationPopup.ShowNotification(notif);
        }

        private void OnLogoutClicked()
        {
            // 모든 서브 패널 닫기
            if (listItemPanel != null)    listItemPanel.SetActive(false);
            if (inventoryPanel != null)   inventoryPanel.SetActive(false);
            if (bidUI != null)            bidUI.gameObject.SetActive(false);
            if (notificationListUI != null) notificationListUI.gameObject.SetActive(false);

            LoginManager.Instance.SignOut();
            if (auctionPanel != null) auctionPanel.SetActive(false);
            if (loginPanel != null)   loginPanel.SetActive(true);
        }
    }
}
