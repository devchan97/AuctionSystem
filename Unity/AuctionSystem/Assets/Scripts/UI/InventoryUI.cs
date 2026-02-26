using AuctionSystem.Auction;
using AuctionSystem.Models;
using AuctionSystem.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("목록")]
        [SerializeField] private Transform itemListContent;

        [Header("피드백")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("버튼")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button closeButton;

        [Header("아이템 카드 프리팹")]
        [SerializeField] private GameObject inventoryCardPrefab;

        void OnEnable()
        {
            LoadInventory();
        }

        void Start()
        {
            if (refreshButton != null) refreshButton.onClick.AddListener(LoadInventory);
            if (closeButton != null)   closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void LoadInventory()
        {
            SetLoading(true);
            SetStatus("");

            BidHandler.Instance.GetInventory(
                onSuccess: items =>
                {
                    SetLoading(false);
                    ClearList();

                    if (items.Length == 0)
                    {
                        SetStatus("인벤토리가 비어있습니다.");
                        return;
                    }

                    foreach (var item in items)
                        CreateCard(item);
                },
                onError: err =>
                {
                    SetLoading(false);
                    SetStatus("오류: " + err);
                }
            );
        }

        private void ClearList()
        {
            foreach (Transform child in itemListContent)
                Destroy(child.gameObject);
        }

        private void CreateCard(InventoryItem item)
        {
            if (inventoryCardPrefab == null || itemListContent == null) return;
            var card = Instantiate(inventoryCardPrefab, itemListContent);

            var nameText = card.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = item.DisplayName;

            var catText = card.transform.Find("CategoryText")?.GetComponent<TMP_Text>();
            if (catText != null) catText.text = item.DisplayCategory;

            // 상태 (owned / listed)
            var statusT = card.transform.Find("StatusText")?.GetComponent<TMP_Text>();
            if (statusT != null)
            {
                statusT.text  = item.status == "listed" ? "경매 중" : "보유 중";
                statusT.color = item.status == "listed"
                    ? new Color(0.96f, 0.61f, 0.09f)  // amber — 경매 중
                    : new Color(0.09f, 0.64f, 0.29f); // green — 보유 중
            }

            // 이미지 로드
            string imageUrl = item.DisplayImageUrl;
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var rawImage = card.transform.Find("ImageArea/ItemImage")?.GetComponent<RawImage>();
                if (rawImage != null)
                    ImageCacheManager.Instance.LoadImage(imageUrl, tex => { if (rawImage != null) rawImage.texture = tex; });
            }
        }

        private void SetLoading(bool loading)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(loading);
            if (refreshButton != null)    refreshButton.interactable = !loading;
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
            {
                statusText.text = msg;
                statusText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
            }
        }
    }
}
