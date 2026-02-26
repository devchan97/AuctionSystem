using System;
using System.Collections.Generic;
using AuctionSystem.Auction;
using AuctionSystem.Models;
using AuctionSystem.Network;
using AuctionSystem.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class ListItemUI : MonoBehaviour
    {
        // ── 탭 버튼 ─────────────────────────────────────────────────────────
        [Header("탭")]
        [SerializeField] private Button tabInventoryButton;
        [SerializeField] private Button tabDirectButton;

        // ── 인벤토리 탭 전용 ─────────────────────────────────────────────────
        [Header("인벤토리 탭")]
        [SerializeField] private GameObject inventoryTabRoot;
        [SerializeField] private TMP_Dropdown   inventoryDropdown;
        [SerializeField] private TMP_Text       inventoryStatusText;

        // ── 직접 등록 탭 전용 ────────────────────────────────────────────────
        [Header("직접 등록 탭")]
        [SerializeField] private GameObject directTabRoot;

        // ── 공통 입력 필드 ───────────────────────────────────────────────────
        [Header("공통 입력")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField descriptionInput;
        [SerializeField] private TMP_Dropdown   categoryDropdown;
        [SerializeField] private TMP_InputField startPriceInput;
        [SerializeField] private TMP_InputField buyoutPriceInput;
        [SerializeField] private TMP_Dropdown   durationDropdown;

        // ── 버튼 / 피드백 ────────────────────────────────────────────────────
        [Header("버튼")]
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;

        [Header("피드백")]
        [SerializeField] private TMP_Text   feedbackText;
        [SerializeField] private GameObject loadingIndicator;

        // ── 내부 상태 ────────────────────────────────────────────────────────
        private readonly int[] _durationHours = { 1, 6, 12, 24, 48, 168 };
        private Coroutine _feedbackRoutine;
        private List<InventoryItem> _inventoryItems = new List<InventoryItem>();
        private bool _isInventoryMode = true;   // 기본: 인벤토리 탭

        // ── 탭 색상 (라이트 테마) ─────────────────────────────────────────────
        private static readonly Color TabActiveColor   = new Color(0.145f, 0.388f, 0.922f);  // blue-600  (선택)
        private static readonly Color TabInactiveColor = new Color(0.93f, 0.94f, 0.96f);     // gray-200  (미선택)
        private static readonly Color TabActiveText    = Color.white;                          // 흰색
        private static readonly Color TabInactiveText  = new Color(0.42f, 0.45f, 0.50f);     // gray-500

        void Start()
        {
            tabInventoryButton?.onClick.AddListener(() => SwitchTab(true));
            tabDirectButton?.onClick.AddListener(() => SwitchTab(false));
            submitButton?.onClick.AddListener(OnSubmitClicked);
            cancelButton?.onClick.AddListener(() => gameObject.SetActive(false));

            // Tab 이동: Name → Description → StartPrice → BuyoutPrice
            if (nameInput != null)
                nameInput.onSubmit.AddListener(_ => descriptionInput?.ActivateInputField());
            if (descriptionInput != null)
                descriptionInput.onSubmit.AddListener(_ => startPriceInput?.ActivateInputField());
            if (startPriceInput != null)
                startPriceInput.onSubmit.AddListener(_ => buyoutPriceInput?.ActivateInputField());
            // BuyoutPrice에서 Enter → 제출
            if (buyoutPriceInput != null)
                buyoutPriceInput.onSubmit.AddListener(_ => TrySubmitFromEnter());

            ClearFeedback();
            SetLoading(false);
        }

        private void TrySubmitFromEnter()
        {
            if (submitButton == null || !submitButton.interactable) return;
            OnSubmitClicked();
        }

        void OnEnable()
        {
            ClearInputs();
            SwitchTab(true);  // 항상 인벤토리 탭으로 초기화
        }

        // ── 탭 전환 ──────────────────────────────────────────────────────────
        private void SwitchTab(bool inventoryMode)
        {
            _isInventoryMode = inventoryMode;

            SetTabVisual(tabInventoryButton,  inventoryMode);
            SetTabVisual(tabDirectButton,    !inventoryMode);

            if (inventoryTabRoot != null) inventoryTabRoot.SetActive(inventoryMode);
            if (directTabRoot    != null) directTabRoot.SetActive(!inventoryMode);

            // 카테고리: 인벤토리 탭에서는 아이템 원본 카테고리 고정, 직접 등록만 수정 가능
            if (categoryDropdown != null) categoryDropdown.interactable = !inventoryMode;

            ClearFeedback();

            if (inventoryMode)
            {
                LoadInventory();
            }
            else
            {
                // 직접 등록 탭 — 인벤토리 없이 항상 제출 가능
                if (submitButton != null) submitButton.interactable = true;
            }
        }

        private void SetTabVisual(Button btn, bool active)
        {
            if (btn == null) return;

            // Image 직접 색상 변경
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? TabActiveColor : TabInactiveColor;

            // ColorBlock normalColor도 동기화 — Button이 상태 전환 시 되돌리는 것 방지
            var cb = btn.colors;
            cb.normalColor      = active ? TabActiveColor   : TabInactiveColor;
            cb.highlightedColor = active ? new Color(0.18f, 0.44f, 0.97f) : new Color(0.86f, 0.88f, 0.91f);
            cb.pressedColor     = active ? new Color(0.11f, 0.31f, 0.78f) : new Color(0.78f, 0.80f, 0.84f);
            cb.selectedColor    = cb.normalColor;
            btn.colors = cb;

            var txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.color = active ? TabActiveText : TabInactiveText;
        }

        // ── 인벤토리 로드 ────────────────────────────────────────────────────
        private void LoadInventory()
        {
            if (inventoryDropdown == null) return;
            if (AuctionManager.Instance == null) return;
            if (inventoryStatusText != null) inventoryStatusText.gameObject.SetActive(true);
            if (inventoryStatusText != null) inventoryStatusText.text = "인벤토리 불러오는 중...";
            inventoryDropdown.interactable = false;
            if (submitButton != null) submitButton.interactable = false;

            AuctionManager.Instance.GetMyInventory(
                onSuccess: items =>
                {
                    _inventoryItems = new List<InventoryItem>(items);
                    inventoryDropdown.ClearOptions();

                    if (_inventoryItems.Count == 0)
                    {
                        inventoryDropdown.options.Add(new TMP_Dropdown.OptionData("보유 아이템 없음"));
                        inventoryDropdown.interactable = false;
                        if (inventoryStatusText != null) inventoryStatusText.text = "등록 가능한 아이템이 없습니다.";
                    }
                    else
                    {
                        foreach (var inv in _inventoryItems)
                            inventoryDropdown.options.Add(new TMP_Dropdown.OptionData($"{inv.item_name} [{inv.item_category}]"));
                        inventoryDropdown.value = 0;
                        inventoryDropdown.RefreshShownValue();
                        inventoryDropdown.interactable = true;
                        if (inventoryStatusText != null) inventoryStatusText.gameObject.SetActive(false);
                        if (submitButton != null) submitButton.interactable = true;
                        OnInventorySelected(0);
                    }
                    inventoryDropdown.onValueChanged.RemoveAllListeners();
                    inventoryDropdown.onValueChanged.AddListener(OnInventorySelected);
                },
                onError: err =>
                {
                    if (inventoryStatusText != null) inventoryStatusText.text = "불러오기 실패: " + err;
                    inventoryDropdown.interactable = false;
                }
            );
        }

        private void OnInventorySelected(int index)
        {
            if (index < 0 || index >= _inventoryItems.Count) return;
            var inv = _inventoryItems[index];
            if (nameInput != null && string.IsNullOrEmpty(nameInput.text))
                nameInput.text = inv.item_name;
            if (categoryDropdown != null)
            {
                int catIdx = Array.IndexOf(AuctionUtils.ListCategoryValues, inv.item_category);
                if (catIdx >= 0) categoryDropdown.value = catIdx;
            }
        }

        // ── 제출 ─────────────────────────────────────────────────────────────
        private void OnSubmitClicked()
        {
            ClearFeedback();

            if (_isInventoryMode)
                SubmitInventory();
            else
                SubmitDirect();
        }

        private void SubmitInventory()
        {
            int invIdx = inventoryDropdown != null ? inventoryDropdown.value : -1;
            if (invIdx < 0 || invIdx >= _inventoryItems.Count)
            {
                ShowFeedback("등록할 인벤토리 아이템을 선택해주세요.", true);
                return;
            }
            string inventoryItemId = _inventoryItems[invIdx].id;

            string itemName = nameInput != null ? nameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(itemName))
                itemName = _inventoryItems[invIdx].item_name;

            if (!ValidateCommonFields(out long startPrice, out long buyoutPrice, out string category, out int durationHours))
                return;

            var req = new ListItemRequest
            {
                inventory_item_id = inventoryItemId,
                name              = itemName,
                description       = descriptionInput != null ? descriptionInput.text.Trim() : "",
                category          = category,
                start_price       = startPrice,
                buyout_price      = buyoutPrice,
                duration_hours    = durationHours,
            };

            SetLoading(true);
            BidHandler.Instance.ListItem(req,
                onSuccess: _ => { SetLoading(false); ShowFeedback("경매 등록 완료!", false); ClearInputs(); UnityEngine.Object.FindFirstObjectByType<AuctionUI>()?.RefreshItems(); Invoke(nameof(Close), 2f); },
                onError: err => { SetLoading(false); ShowFeedback("등록 실패: " + err, true); }
            );
        }

        private void SubmitDirect()
        {
            string itemName = nameInput != null ? nameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(itemName))
            {
                ShowFeedback("아이템 이름을 입력해주세요.", true);
                return;
            }

            if (!ValidateCommonFields(out long startPrice, out long buyoutPrice, out string category, out int durationHours))
                return;

            string endsAt = DateTime.UtcNow.AddHours(durationHours).ToString("o");

            var req = new DirectListItemRequest
            {
                name          = itemName,
                description   = descriptionInput != null ? descriptionInput.text.Trim() : "",
                image_url     = null,
                category      = category,
                start_price   = startPrice,
                buyout_price  = buyoutPrice,
                ends_at       = endsAt,
            };

            SetLoading(true);
            BidHandler.Instance.ListItemDirect(req,
                onSuccess: _ => { SetLoading(false); ShowFeedback("경매 등록 완료!", false); ClearInputs(); UnityEngine.Object.FindFirstObjectByType<AuctionUI>()?.RefreshItems(); Invoke(nameof(Close), 2f); },
                onError: err => { SetLoading(false); ShowFeedback("등록 실패: " + err, true); }
            );
        }

        // ── 공통 필드 검증 ────────────────────────────────────────────────────
        private bool ValidateCommonFields(out long startPrice, out long buyoutPrice, out string category, out int durationHours)
        {
            startPrice = 0; buyoutPrice = 0; category = ""; durationHours = 24;

            if (!long.TryParse(startPriceInput != null ? startPriceInput.text : "", out startPrice) || startPrice <= 0)
            {
                ShowFeedback("시작가는 1 이상의 숫자여야 합니다.", true);
                return false;
            }

            string buyoutText = buyoutPriceInput != null ? buyoutPriceInput.text.Trim() : "";
            if (!string.IsNullOrEmpty(buyoutText))
            {
                if (!long.TryParse(buyoutText, out buyoutPrice) || buyoutPrice <= startPrice)
                {
                    ShowFeedback("즉시구매가는 시작가보다 커야 합니다.", true);
                    return false;
                }
            }

            int catIndex = categoryDropdown != null ? categoryDropdown.value : 0;
            int durIndex = durationDropdown != null ? durationDropdown.value : 3;
            category      = catIndex < AuctionUtils.ListCategoryValues.Length ? AuctionUtils.ListCategoryValues[catIndex] : AuctionUtils.ListCategoryValues[0];
            durationHours = durIndex < _durationHours.Length ? _durationHours[durIndex] : 24;
            return true;
        }

        private void Close() => gameObject.SetActive(false);

        private void ClearInputs()
        {
            if (nameInput != null)          nameInput.text = "";
            if (descriptionInput != null)   descriptionInput.text = "";
            if (startPriceInput != null)    startPriceInput.text = "";
            if (buyoutPriceInput != null)   buyoutPriceInput.text = "";
            if (categoryDropdown != null)   categoryDropdown.value = 0;
            if (durationDropdown != null)   durationDropdown.value = 3;
        }

        // ── 피드백 / 로딩 ─────────────────────────────────────────────────────
        private void ShowFeedback(string msg, bool isError)
        {
            if (feedbackText == null) return;
            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            var color = isError
                ? new Color(0.86f, 0.15f, 0.15f)   // red-600
                : new Color(0.086f, 0.639f, 0.29f); // green-600
            _feedbackRoutine = StartCoroutine(UIAnimator.FeedbackRoutine(feedbackText, msg, color));
        }

        private void ClearFeedback()
        {
            if (_feedbackRoutine != null) { StopCoroutine(_feedbackRoutine); _feedbackRoutine = null; }
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void SetLoading(bool loading)
        {
            if (submitButton != null)     submitButton.interactable = !loading;
            if (loadingIndicator != null) loadingIndicator.SetActive(loading);
        }
    }
}
