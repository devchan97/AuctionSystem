using AuctionSystem.Auction;
using AuctionSystem.Models;
using AuctionSystem.Network;
using AuctionSystem.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class BidUI : MonoBehaviour
    {
        [Header("아이템 정보")]
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescText;
        [SerializeField] private TMP_Text currentBidText;
        [SerializeField] private TMP_Text buyoutPriceText;
        [SerializeField] private TMP_Text timeLeftText;
        [SerializeField] private TMP_Text myGoldText;

        [Header("입찰")]
        [SerializeField] private TMP_InputField bidAmountInput;
        [SerializeField] private Button bidButton;

        [Header("즉시구매")]
        [SerializeField] private Button buyoutButton;

        [Header("경매 취소 (본인 아이템)")]
        [SerializeField] private Button cancelAuctionButton;

        [Header("닫기")]
        [SerializeField] private Button closeButton;

        [Header("피드백")]
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("열람자 수")]
        [SerializeField] private TMP_Text viewerCountText;

        private AuctionItem _currentItem;
        private Coroutine   _feedbackRoutine;

        void Start()
        {
            bidButton?.onClick.AddListener(OnBidClicked);
            buyoutButton?.onClick.AddListener(OnBuyoutClicked);
            cancelAuctionButton?.onClick.AddListener(OnCancelAuctionClicked);
            closeButton?.onClick.AddListener(OnCloseClicked);

            // Enter 키로 입찰 실행
            if (bidAmountInput != null)
                bidAmountInput.onSubmit.AddListener(_ => TryBidFromEnter());

            SupabaseManager.Instance.OnProfileLoaded += HandleProfileLoaded;
            RealtimeManager.Instance.OnBidUpdated += OnRealtimeBidUpdate;
            RealtimeManager.Instance.OnViewerCountChanged += OnViewerCountChanged;

            gameObject.SetActive(false);
        }

        private void TryBidFromEnter()
        {
            if (bidButton != null && bidButton.interactable)
                OnBidClicked();
        }

        void OnDestroy()
        {
            if (SupabaseManager.Instance != null)
                SupabaseManager.Instance.OnProfileLoaded -= HandleProfileLoaded;
            if (RealtimeManager.Instance != null)
            {
                RealtimeManager.Instance.OnBidUpdated -= OnRealtimeBidUpdate;
                RealtimeManager.Instance.OnViewerCountChanged -= OnViewerCountChanged;
            }
        }

        private void HandleProfileLoaded(Profile profile) => UpdateMyGold(profile.gold);

        public void Open(AuctionItem item)
        {
            _currentItem = item;
            gameObject.SetActive(true);
            RefreshDisplay();
            RealtimeManager.Instance.SubscribeToAuction(item.id);
            RealtimeManager.Instance.SubscribeItemPresence(item.id);
            ClearFeedback();
        }

        private void OnCloseClicked()
        {
            RealtimeManager.Instance.UnsubscribeItemPresence();
            gameObject.SetActive(false);
        }

        private void OnViewerCountChanged(int count)
        {
            if (viewerCountText != null)
                viewerCountText.text = $"{count}명 보는 중";
        }

        private void RefreshDisplay()
        {
            if (_currentItem == null) return;
            if (itemNameText != null)   itemNameText.text   = _currentItem.name;
            if (itemDescText != null)   itemDescText.text   = _currentItem.description;
            if (currentBidText != null) currentBidText.text = $"현재 최고가: {_currentItem.current_bid:N0}G";

            if (buyoutPriceText != null)
                buyoutPriceText.text = _currentItem.buyout_price > 0
                    ? $"즉시구매: {_currentItem.buyout_price:N0}G"
                    : "즉시구매 불가";

            string myId = SupabaseManager.Instance.Session?.user?.id ?? "";
            string sellerId = _currentItem.seller_id ?? "";
            bool isMine = !string.IsNullOrEmpty(myId) && myId == sellerId;
            Debug.Log($"[BidUI] isMine={isMine} | myId={myId} | sellerId={sellerId}");

            // 본인 아이템이면 입찰/즉시구매 비활성화, 취소 버튼 활성화
            if (buyoutButton != null)
                buyoutButton.interactable = !isMine && _currentItem.buyout_price > 0;
            if (bidButton != null)
                bidButton.interactable = !isMine;
            if (cancelAuctionButton != null)
                cancelAuctionButton.gameObject.SetActive(isMine);

            if (bidAmountInput != null && _currentItem.current_bid > 0)
                bidAmountInput.text = (_currentItem.current_bid + _currentItem.current_bid / 10).ToString();
            else if (bidAmountInput != null)
                bidAmountInput.text = _currentItem.start_price.ToString();

            if (SupabaseManager.Instance.CurrentProfile != null)
                UpdateMyGold(SupabaseManager.Instance.CurrentProfile.gold);
        }

        private void OnBidClicked()
        {
            if (!long.TryParse(bidAmountInput.text, out long amount))
            {
                ShowFeedback("올바른 금액을 입력하세요.", true);
                return;
            }
            SetLoading(true);
            BidHandler.Instance.PlaceBid(_currentItem.id, amount,
                onSuccess: _ =>
                {
                    SetLoading(false);
                    ShowFeedback("입찰 성공!", false);
                    AuctionManager.Instance.GetItem(_currentItem.id,
                        item => { _currentItem = item; RefreshDisplay(); }, _ => { });
                },
                onError: err =>
                {
                    SetLoading(false);
                    ShowFeedback("입찰 실패: " + err, true);
                }
            );
        }

        private void OnBuyoutClicked()
        {
            SetLoading(true);
            BidHandler.Instance.Buyout(_currentItem.id,
                onSuccess: _ =>
                {
                    SetLoading(false);
                    ShowFeedback("즉시구매 완료! 인벤토리를 확인하세요.", false);
                    buyoutButton.interactable = false;
                    bidButton.interactable = false;
                },
                onError: err =>
                {
                    SetLoading(false);
                    ShowFeedback("즉시구매 실패: " + err, true);
                }
            );
        }

        private void OnCancelAuctionClicked()
        {
            SetLoading(true);
            BidHandler.Instance.CancelAuction(_currentItem.id,
                onSuccess: _ =>
                {
                    SetLoading(false);
                    ShowFeedback("경매가 취소되었습니다. 아이템이 인벤토리로 반환됩니다.", false);
                    if (cancelAuctionButton != null) cancelAuctionButton.interactable = false;
                    if (bidButton != null) bidButton.interactable = false;
                    if (buyoutButton != null) buyoutButton.interactable = false;
                },
                onError: err =>
                {
                    SetLoading(false);
                    ShowFeedback("취소 실패: " + err, true);
                }
            );
        }

        private void OnRealtimeBidUpdate(AuctionItem updatedItem)
        {
            if (_currentItem == null || updatedItem.id != _currentItem.id) return;
            _currentItem = updatedItem;
            RefreshDisplay();
            ShowFeedback("새 입찰이 들어왔습니다!", false);
        }

        private void UpdateMyGold(long gold)
        {
            if (myGoldText != null) myGoldText.text = $"내 Gold: {gold:N0}G";
        }

        private void ShowFeedback(string msg, bool isError)
        {
            if (feedbackText == null) return;
            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            var color = isError
                ? new Color(0.94f, 0.27f, 0.27f)   // red-400
                : new Color(0.53f, 0.94f, 0.67f);   // green-300
            _feedbackRoutine = StartCoroutine(UIAnimator.FeedbackRoutine(feedbackText, msg, color));
        }

        private void ClearFeedback()
        {
            if (_feedbackRoutine != null) { StopCoroutine(_feedbackRoutine); _feedbackRoutine = null; }
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void SetLoading(bool loading)
        {
            bool isMine = SupabaseManager.Instance.Session?.user?.id == _currentItem?.seller_id;
            if (bidButton != null)    bidButton.interactable    = !loading && !isMine;
            if (buyoutButton != null) buyoutButton.interactable = !loading && !isMine && _currentItem?.buyout_price > 0;
            if (cancelAuctionButton != null) cancelAuctionButton.interactable = !loading && isMine;
            if (loadingIndicator != null) loadingIndicator.SetActive(loading);
        }

        void Update()
        {
            if (!gameObject.activeSelf || _currentItem == null || timeLeftText == null) return;
            timeLeftText.text = AuctionUtils.GetTimeLeftShort(_currentItem.ends_at);
        }
    }
}
