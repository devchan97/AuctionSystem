using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using AuctionSystem.UI;
using AuctionSystem.Auth;
using AuctionSystem;
using AuctionSystem.Network;

// Tools > Auction > Build Scene
public static class SceneBuilder
{
    // ── Light theme (웹 globals.css 기준) ──────────────────────────────
    // Backgrounds
    static readonly Color BgBase      = Hex("#fafafa"); // --background (web light)
    static readonly Color BgCard      = Hex("#ffffff"); // bg-white (card)
    static readonly Color BgSurface   = Hex("#f3f4f6"); // gray-100 (header, filter bar)
    static readonly Color BgInput     = Hex("#ffffff"); // transparent → white in Unity
    static readonly Color BgHover     = Hex("#f9fafb"); // gray-50 (card hover)

    // Borders
    static readonly Color Border      = Hex("#dbdbdb"); // --border-color light
    static readonly Color BorderFocus = Hex("#9ca3af"); // gray-400 (focus ring)

    // Text
    static readonly Color TextPrimary   = Hex("#111827"); // gray-900
    static readonly Color TextSecondary = Hex("#6b7280"); // gray-500
    static readonly Color TextMuted     = Hex("#9ca3af"); // gray-400 (labels uppercase)
    static readonly Color TextLabel     = Hex("#374151"); // gray-700

    // Accent — 웹 라이트 기준 색상
    static readonly Color AccentBlue    = Hex("#2563eb"); // blue-600 (primary button)
    static readonly Color AccentBlueDk  = Hex("#1d4ed8"); // blue-700 (hover)
    static readonly Color AccentGreen   = Hex("#16a34a"); // green-600 (bid price light)
    static readonly Color AccentGreenDk = Hex("#15803d"); // green-700
    static readonly Color AccentRed     = Hex("#ef4444"); // red-500 (ending soon)
    static readonly Color AccentOrange  = Hex("#f97316"); // orange-500 (buyout)
    static readonly Color AccentGold    = Hex("#d97706"); // amber-600 (gold — readable on white)

    // State (라이트 배경용)
    static readonly Color StateError   = Hex("#dc2626"); // red-600
    static readonly Color StateSuccess = Hex("#16a34a"); // green-600

    // Secondary button (bg-gray-100 text-gray-900)
    static readonly Color BgSecondary     = Hex("#f3f4f6"); // gray-100
    static readonly Color TextOnSecondary = Hex("#111827"); // gray-900

    // Google brand
    static readonly Color GoogleRed    = Hex("#ea4335");

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    [MenuItem("Tools/Auction/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("Bootstrap");
        DestroyIfExists("Canvas");
        DestroyIfExists("EventSystem");

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        var bootstrapGO = new GameObject("Bootstrap");
        bootstrapGO.AddComponent<GameBootstrap>();
        bootstrapGO.AddComponent<ImageCacheManager>();

        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var loginPanel = CreateFullscreenPanel(canvasGO.transform, "LoginPanel", BgBase);

        var loginCard = CreateRoundedCard(loginPanel.transform, "LoginCard",
            Vector2.zero, new Vector2(480, 600), BgCard, Border);

        CreateTMPText(loginCard.transform, "TitleText", "AuctionHouse",
            new Vector2(0, 235), new Vector2(420, 60), 36, FontStyles.Bold, TextPrimary);

        CreateTMPText(loginCard.transform, "EmailLabel", "EMAIL",
            new Vector2(0, 170), new Vector2(400, 24), 14, FontStyles.Bold, TextMuted);
        var emailInput = CreateInputField(loginCard.transform, "EmailInput",
            new Vector2(0, 125), new Vector2(400, 48), "Enter your email", TMP_InputField.ContentType.EmailAddress);

        CreateTMPText(loginCard.transform, "PasswordLabel", "PASSWORD",
            new Vector2(0, 80), new Vector2(400, 24), 14, FontStyles.Bold, TextMuted);
        var pwInput = CreateInputField(loginCard.transform, "PasswordInput",
            new Vector2(0, 35), new Vector2(400, 48), "Enter your password", TMP_InputField.ContentType.Password);

        var errorText = CreateTMPText(loginCard.transform, "ErrorText", "",
            new Vector2(0, -70), new Vector2(400, 36), 14, FontStyles.Normal, StateError);
        errorText.GetComponent<TMP_Text>().gameObject.SetActive(false);

        var loadingIndicator = CreatePanel(loginCard.transform, "LoadingIndicator",
            new Vector2(0, 10), new Vector2(40, 40), new Color(0, 0, 0, 0));
        AddLoadingSpinner(loadingIndicator);
        loadingIndicator.SetActive(false);

        var loginBtn = CreateButton(loginCard.transform, "LoginButton", "Log In",
            new Vector2(0, -140), new Vector2(400, 48), AccentBlue);

        var webAuthBtn = CreateButton(loginCard.transform, "WebAuthButton", "회원가입 / Google 로그인",
            new Vector2(0, -200), new Vector2(400, 48), BgCard, Border, GoogleRed);

        // 취소 버튼 — OAuth 대기 중 표시, 초기엔 숨김
        var cancelOAuthBtn = CreateButton(loginCard.transform, "CancelOAuthButton", "취소",
            new Vector2(0, -80), new Vector2(400, 40), BgCard, Border, StateError);
        cancelOAuthBtn.SetActive(false);

        // 종료 버튼 — 로그인 카드 하단
        var quitBtn = CreateButton(loginCard.transform, "QuitButton", "종료",
            new Vector2(0, -260), new Vector2(400, 40), BgCard, Border, TextSecondary);

        var loginUI = loginPanel.AddComponent<LoginUI>();
        SetPrivateField(loginUI, "emailInput",       emailInput.GetComponent<TMP_InputField>());
        SetPrivateField(loginUI, "passwordInput",    pwInput.GetComponent<TMP_InputField>());
        SetPrivateField(loginUI, "loginButton",      loginBtn.GetComponent<Button>());
        SetPrivateField(loginUI, "webAuthButton",    webAuthBtn.GetComponent<Button>());
        SetPrivateField(loginUI, "cancelOAuthButton", cancelOAuthBtn.GetComponent<Button>());
        SetPrivateField(loginUI, "quitButton",       quitBtn.GetComponent<Button>());
        SetPrivateField(loginUI, "errorText",        errorText.GetComponent<TMP_Text>());
        SetPrivateField(loginUI, "loadingIndicator", loadingIndicator);

        var auctionPanel = CreateFullscreenPanel(canvasGO.transform, "AuctionPanel", BgBase);
        auctionPanel.SetActive(false);

        var header = CreatePanel(auctionPanel.transform, "Header",
            new Vector2(0, 515), new Vector2(1920, 64), BgSurface);
        AddBorderBottom(header, Border);

        // 헤더 폭 1920 — anchorMin/Max=(0.5,0.5) 기준, x범위: -960 ~ +960
        // 왼쪽: 타이틀 / 오른쪽: 유저명 → Gold → 내인벤토리 → 로그아웃 (겹침 없도록 간격 배치)
        CreateTMPText(header.transform, "TitleText", "AuctionHouse",
            new Vector2(-820, 0), new Vector2(260, 48), 24, FontStyles.Bold, TextPrimary);
        var usernameText = CreateTMPText(header.transform, "UsernameText", "유저명",
            new Vector2(155, 0), new Vector2(220, 48), 18, FontStyles.Normal, TextLabel);
        var goldText = CreateTMPText(header.transform, "GoldText", "0G",
            new Vector2(370, 0), new Vector2(160, 48), 22, FontStyles.Bold, AccentGold);
        var notifBtn = CreateButton(header.transform, "NotificationButton", "알림",
            new Vector2(560, 0), new Vector2(100, 40), BgSurface, Border, TextSecondary);
        var inventoryBtn = CreateButton(header.transform, "InventoryButton", "내 인벤토리",
            new Vector2(700, 0), new Vector2(150, 40), BgSurface, Border, TextSecondary);
        var logoutBtn = CreateButton(header.transform, "LogoutButton", "로그아웃",
            new Vector2(862, 0), new Vector2(120, 40), BgSurface, Border, TextSecondary);

        var filterBar = CreatePanel(auctionPanel.transform, "FilterBar",
            new Vector2(-200, 450), new Vector2(820, 48), BgSurface);

        var categoryDrop = CreateDropdown(filterBar.transform, "CategoryDropdown",
            new Vector2(-200, 0), new Vector2(220, 38),
            new string[] { "전체", "무기", "방어구", "소비", "기타" }); // AuctionUtils.CategoryValues와 index 1:1 대응
        var sortDrop = CreateDropdown(filterBar.transform, "SortDropdown",
            new Vector2(60, 0), new Vector2(220, 38),
            new string[] { "최신순", "입찰순", "마감순" });

        var refreshBtn = CreateButton(filterBar.transform, "RefreshButton", "새로고침",
            new Vector2(275, 0), new Vector2(130, 38), BgSurface, Border, TextSecondary);

        var listItemBtn = CreateButton(auctionPanel.transform, "ListItemButton", "+ 경매 등록",
            new Vector2(820, 450), new Vector2(180, 48), AccentBlue);

        var scrollView = CreateScrollView(auctionPanel.transform, "ItemScrollView",
            new Vector2(-320, -50), new Vector2(1200, 880));

        var itemListContent = scrollView.transform.Find("Viewport/Content");

        var statusText = CreateTMPText(auctionPanel.transform, "StatusText", "",
            new Vector2(-320, 420), new Vector2(1200, 40), 16, FontStyles.Normal, TextMuted);

        var bidPanel = CreateRoundedCard(auctionPanel.transform, "BidPanel",
            new Vector2(0, 0), new Vector2(600, 880), BgCard, Border);
        bidPanel.AddComponent<AuctionSystem.UI.DraggablePanel>();
        bidPanel.SetActive(false);

        var itemNameText = CreateTMPText(bidPanel.transform, "ItemNameText", "아이템 이름",
            new Vector2(0, 380), new Vector2(560, 52), 28, FontStyles.Bold, TextPrimary);
        var itemDescText = CreateTMPText(bidPanel.transform, "ItemDescText", "아이템 설명",
            new Vector2(0, 300), new Vector2(560, 80), 16, FontStyles.Normal, TextSecondary);
        var currentBidText = CreateTMPText(bidPanel.transform, "CurrentBidText", "현재 최고가: 0G",
            new Vector2(0, 220), new Vector2(560, 48), 26, FontStyles.Bold, AccentGreen);
        var buyoutPriceText = CreateTMPText(bidPanel.transform, "BuyoutPriceText", "즉시구매: -",
            new Vector2(0, 162), new Vector2(560, 36), 18, FontStyles.Normal, AccentOrange);
        var timeLeftText = CreateTMPText(bidPanel.transform, "TimeLeftText", "남은 시간: --",
            new Vector2(0, 114), new Vector2(560, 36), 18, FontStyles.Normal, AccentRed);
        var myGoldText = CreateTMPText(bidPanel.transform, "MyGoldText", "내 Gold: 0G",
            new Vector2(0, 68), new Vector2(560, 36), 18, FontStyles.Bold, AccentGold);

        var viewerCountText = CreateTMPText(bidPanel.transform, "ViewerCountText", "0명 보는 중",
            new Vector2(170, 68), new Vector2(200, 28), 13, FontStyles.Normal, TextSecondary);
        viewerCountText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineRight;

        var divider = CreatePanel(bidPanel.transform, "Divider",
            new Vector2(0, 38), new Vector2(540, 1), Border);

        var bidAmountInput = CreateInputField(bidPanel.transform, "BidAmountInput",
            new Vector2(0, -20), new Vector2(360, 48), "입찰 금액 입력", TMP_InputField.ContentType.IntegerNumber);
        var bidBtn = CreateButton(bidPanel.transform, "BidButton", "입찰",
            new Vector2(-120, -88), new Vector2(240, 48), AccentBlue);
        var buyoutBtn = CreateButton(bidPanel.transform, "BuyoutButton", "즉시구매",
            new Vector2(150, -88), new Vector2(240, 48), AccentOrange);

        // 경매 취소 버튼 (본인 아이템에만 표시, 초기 비활성)
        var cancelAuctionBtn = CreateButton(bidPanel.transform, "CancelAuctionButton", "경매 취소",
            new Vector2(0, -148), new Vector2(300, 40), AccentRed);
        cancelAuctionBtn.SetActive(false);

        var bidFeedbackText = CreateTMPText(bidPanel.transform, "FeedbackText", "",
            new Vector2(0, -200), new Vector2(540, 36), 15, FontStyles.Normal, StateSuccess);
        bidFeedbackText.GetComponent<TMP_Text>().gameObject.SetActive(false);

        var bidLoadingIndicator = CreatePanel(bidPanel.transform, "LoadingIndicator",
            new Vector2(0, -248), new Vector2(40, 40), new Color(0, 0, 0, 0));
        AddLoadingSpinner(bidLoadingIndicator);
        bidLoadingIndicator.SetActive(false);

        var closeBidBtn = CreateButton(bidPanel.transform, "CloseButton", "X",
            new Vector2(265, 410), new Vector2(44, 44), new Color(0, 0, 0, 0), Border, AccentRed);

        var bidUI = bidPanel.AddComponent<BidUI>();
        SetPrivateField(bidUI, "itemNameText",      itemNameText.GetComponent<TMP_Text>());
        SetPrivateField(bidUI, "itemDescText",      itemDescText.GetComponent<TMP_Text>());
        SetPrivateField(bidUI, "currentBidText",    currentBidText.GetComponent<TMP_Text>());
        SetPrivateField(bidUI, "buyoutPriceText",   buyoutPriceText.GetComponent<TMP_Text>());
        SetPrivateField(bidUI, "timeLeftText",      timeLeftText.GetComponent<TMP_Text>());
        SetPrivateField(bidUI, "myGoldText",        myGoldText.GetComponent<TMP_Text>());
        SetPrivateField(bidUI, "bidAmountInput",    bidAmountInput.GetComponent<TMP_InputField>());
        SetPrivateField(bidUI, "bidButton",              bidBtn.GetComponent<Button>());
        SetPrivateField(bidUI, "buyoutButton",           buyoutBtn.GetComponent<Button>());
        SetPrivateField(bidUI, "cancelAuctionButton",    cancelAuctionBtn.GetComponent<Button>());
        SetPrivateField(bidUI, "closeButton",            closeBidBtn.GetComponent<Button>());
        SetPrivateField(bidUI, "feedbackText",      bidFeedbackText.GetComponent<TMP_Text>());
        SetPrivateField(bidUI, "loadingIndicator",  bidLoadingIndicator);
        SetPrivateField(bidUI, "viewerCountText",   viewerCountText.GetComponent<TMP_Text>());

        var listItemPanel = CreateRoundedCard(auctionPanel.transform, "ListItemPanel",
            new Vector2(0, 30), new Vector2(700, 960), BgCard, Border);
        listItemPanel.AddComponent<AuctionSystem.UI.DraggablePanel>();
        listItemPanel.SetActive(false);

        // ── 타이틀 ──────────────────────────────────────────────────────────
        CreateTMPText(listItemPanel.transform, "TitleText", "경매 등록",
            new Vector2(0, 450), new Vector2(640, 52), 28, FontStyles.Bold, TextPrimary);

        // ── 탭 버튼 (인벤토리 / 직접 등록) ──────────────────────────────────
        var tabBg = CreatePanel(listItemPanel.transform, "TabBar",
            new Vector2(0, 400), new Vector2(600, 44), BgSurface);

        var tabInvBtn = CreateButton(tabBg.transform, "TabInventoryButton", "인벤토리 등록",
            new Vector2(-150, 0), new Vector2(290, 40), AccentBlue); // 기본 active
        var tabDirBtn = CreateButton(tabBg.transform, "TabDirectButton", "직접 등록",
            new Vector2(150, 0), new Vector2(290, 40), BgSurface, Border, TextSecondary);

        // ── 인벤토리 탭 루트 ─────────────────────────────────────────────────
        var inventoryTabRoot = CreatePanel(listItemPanel.transform, "InventoryTabRoot",
            new Vector2(0, 340), new Vector2(620, 72), new Color(0, 0, 0, 0));
        var invTabRT = inventoryTabRoot.GetComponent<RectTransform>();

        CreateTMPText(inventoryTabRoot.transform, "InventoryLabel", "MY INVENTORY",
            new Vector2(0, 20), new Vector2(600, 22), 12, FontStyles.Bold, TextMuted);
        var inventoryDrop = CreateDropdown(inventoryTabRoot.transform, "InventoryDropdown",
            new Vector2(0, -14), new Vector2(600, 44),
            new string[] { "인벤토리 불러오는 중..." });
        var inventoryStatusTxt = CreateTMPText(listItemPanel.transform, "InventoryStatusText", "",
            new Vector2(0, 265), new Vector2(600, 24), 12, FontStyles.Normal, TextSecondary);

        // ── 직접 등록 탭 루트 (이미지 업로드 미지원 — 안내 텍스트만) ──────────
        var directTabRoot = CreatePanel(listItemPanel.transform, "DirectTabRoot",
            new Vector2(0, 340), new Vector2(620, 72), new Color(0, 0, 0, 0));

        CreateTMPText(directTabRoot.transform, "DirectNoticeText", "※ 이미지 업로드는 웹에서만 지원됩니다.",
            new Vector2(0, 0), new Vector2(600, 44), 13, FontStyles.Normal, TextSecondary);

        directTabRoot.SetActive(false); // 초기에 숨김

        // ── 공통 필드 (InventoryStatusText 기준 -40 하향) ────────────────────
        CreateTMPText(listItemPanel.transform, "NameLabel", "ITEM NAME",
            new Vector2(0, 223), new Vector2(600, 22), 12, FontStyles.Bold, TextMuted);
        var nameInput = CreateInputField(listItemPanel.transform, "NameInput",
            new Vector2(0, 188), new Vector2(600, 44), "아이템 이름", TMP_InputField.ContentType.Standard);

        CreateTMPText(listItemPanel.transform, "DescLabel", "DESCRIPTION",
            new Vector2(0, 142), new Vector2(600, 22), 12, FontStyles.Bold, TextMuted);
        var descInput = CreateInputField(listItemPanel.transform, "DescriptionInput",
            new Vector2(0, 100), new Vector2(600, 56), "설명 (선택)", TMP_InputField.ContentType.Standard);

        CreateTMPText(listItemPanel.transform, "CategoryLabel", "CATEGORY",
            new Vector2(0, 56), new Vector2(600, 22), 12, FontStyles.Bold, TextMuted);
        var categoryItemDrop = CreateDropdown(listItemPanel.transform, "CategoryDropdown",
            new Vector2(0, 18), new Vector2(300, 40),
            new string[] { "무기", "방어구", "소비", "기타" }); // AuctionUtils.ListCategoryValues와 index 1:1 대응

        CreateTMPText(listItemPanel.transform, "StartPriceLabel", "START PRICE",
            new Vector2(-150, -30), new Vector2(260, 22), 12, FontStyles.Bold, TextMuted);
        var startPriceInput = CreateInputField(listItemPanel.transform, "StartPriceInput",
            new Vector2(-150, -68), new Vector2(260, 44), "시작가", TMP_InputField.ContentType.IntegerNumber);

        CreateTMPText(listItemPanel.transform, "BuyoutPriceLabel", "BUYOUT PRICE",
            new Vector2(150, -30), new Vector2(260, 22), 12, FontStyles.Bold, TextMuted);
        var buyoutPriceInput = CreateInputField(listItemPanel.transform, "BuyoutPriceInput",
            new Vector2(150, -68), new Vector2(260, 44), "즉시구매가 (선택)", TMP_InputField.ContentType.IntegerNumber);

        CreateTMPText(listItemPanel.transform, "DurationLabel", "DURATION",
            new Vector2(0, -122), new Vector2(600, 22), 12, FontStyles.Bold, TextMuted);
        var durationDrop = CreateDropdown(listItemPanel.transform, "DurationDropdown",
            new Vector2(0, -160), new Vector2(300, 40),
            new string[] { "1시간", "6시간", "12시간", "24시간", "48시간", "168시간(7일)" });

        // ── 피드백 / 로딩 / 버튼 ─────────────────────────────────────────────
        var listFeedbackText = CreateTMPText(listItemPanel.transform, "FeedbackText", "",
            new Vector2(0, -218), new Vector2(600, 30), 14, FontStyles.Normal, StateSuccess);
        listFeedbackText.GetComponent<TMP_Text>().gameObject.SetActive(false);

        var listLoadingGO = CreatePanel(listItemPanel.transform, "LoadingIndicator",
            new Vector2(0, -260), new Vector2(40, 40), new Color(0, 0, 0, 0));
        AddLoadingSpinner(listLoadingGO);
        listLoadingGO.SetActive(false);

        var submitListBtn = CreateButton(listItemPanel.transform, "SubmitButton", "경매 등록",
            new Vector2(-130, -336), new Vector2(240, 48), AccentBlue);
        var cancelListBtn = CreateButton(listItemPanel.transform, "CancelButton", "취소",
            new Vector2(130, -336), new Vector2(200, 48), BgCard, Border, TextSecondary);

        // ── ListItemUI 연결 ───────────────────────────────────────────────────
        var listItemUI = listItemPanel.AddComponent<AuctionSystem.UI.ListItemUI>();
        SetPrivateField(listItemUI, "tabInventoryButton",  tabInvBtn.GetComponent<Button>());
        SetPrivateField(listItemUI, "tabDirectButton",     tabDirBtn.GetComponent<Button>());
        SetPrivateField(listItemUI, "inventoryTabRoot",    inventoryTabRoot);
        SetPrivateField(listItemUI, "directTabRoot",       directTabRoot);
        SetPrivateField(listItemUI, "inventoryDropdown",   inventoryDrop.GetComponent<TMP_Dropdown>());
        SetPrivateField(listItemUI, "inventoryStatusText", inventoryStatusTxt.GetComponent<TMP_Text>());
        SetPrivateField(listItemUI, "nameInput",           nameInput.GetComponent<TMP_InputField>());
        SetPrivateField(listItemUI, "descriptionInput",    descInput.GetComponent<TMP_InputField>());
        SetPrivateField(listItemUI, "categoryDropdown",    categoryItemDrop.GetComponent<TMP_Dropdown>());
        SetPrivateField(listItemUI, "startPriceInput",     startPriceInput.GetComponent<TMP_InputField>());
        SetPrivateField(listItemUI, "buyoutPriceInput",    buyoutPriceInput.GetComponent<TMP_InputField>());
        SetPrivateField(listItemUI, "durationDropdown",    durationDrop.GetComponent<TMP_Dropdown>());
        SetPrivateField(listItemUI, "feedbackText",        listFeedbackText.GetComponent<TMP_Text>());
        SetPrivateField(listItemUI, "loadingIndicator",    listLoadingGO);
        SetPrivateField(listItemUI, "submitButton",        submitListBtn.GetComponent<Button>());
        SetPrivateField(listItemUI, "cancelButton",        cancelListBtn.GetComponent<Button>());

        // ── InventoryPanel ────────────────────────────────────────────────────
        var inventoryPanel = CreateRoundedCard(auctionPanel.transform, "InventoryPanel",
            new Vector2(0, 30), new Vector2(700, 820), BgCard, Border);
        inventoryPanel.AddComponent<AuctionSystem.UI.DraggablePanel>();
        inventoryPanel.SetActive(false);

        CreateTMPText(inventoryPanel.transform, "TitleText", "내 인벤토리",
            new Vector2(0, 375), new Vector2(640, 52), 28, FontStyles.Bold, TextPrimary);

        var invStatusText = CreateTMPText(inventoryPanel.transform, "StatusText", "",
            new Vector2(0, 320), new Vector2(620, 28), 14, FontStyles.Normal, TextSecondary);
        invStatusText.GetComponent<TMP_Text>().gameObject.SetActive(false);

        var invLoadingGO = CreatePanel(inventoryPanel.transform, "LoadingIndicator",
            new Vector2(0, 280), new Vector2(40, 40), new Color(0, 0, 0, 0));
        AddLoadingSpinner(invLoadingGO);
        invLoadingGO.SetActive(false);

        var invScrollView = CreateScrollView(inventoryPanel.transform, "ItemScrollView",
            new Vector2(0, -30), new Vector2(640, 620));
        var invListContent = invScrollView.transform.Find("Viewport/Content");

        var invRefreshBtn = CreateButton(inventoryPanel.transform, "RefreshButton", "새로고침",
            new Vector2(-130, -370), new Vector2(160, 44), BgSurface, Border, TextSecondary);
        var invCloseBtn = CreateButton(inventoryPanel.transform, "CloseButton", "닫기",
            new Vector2(80, -370), new Vector2(120, 44), BgCard, Border, TextSecondary);

        var inventoryUI = inventoryPanel.AddComponent<AuctionSystem.UI.InventoryUI>();
        SetPrivateField(inventoryUI, "itemListContent",      invListContent);
        SetPrivateField(inventoryUI, "statusText",           invStatusText.GetComponent<TMP_Text>());
        SetPrivateField(inventoryUI, "loadingIndicator",     invLoadingGO);
        SetPrivateField(inventoryUI, "refreshButton",        invRefreshBtn.GetComponent<Button>());
        SetPrivateField(inventoryUI, "closeButton",          invCloseBtn.GetComponent<Button>());
        SetPrivateField(inventoryUI, "inventoryCardPrefab",  BuildInventoryCardPrefab());

        var auctionUI = auctionPanel.AddComponent<AuctionSystem.UI.AuctionUI>();
        SetPrivateField(auctionUI, "itemListContent",  itemListContent);
        SetPrivateField(auctionUI, "categoryDropdown", categoryDrop.GetComponent<TMP_Dropdown>());
        SetPrivateField(auctionUI, "sortDropdown",     sortDrop.GetComponent<TMP_Dropdown>());
        SetPrivateField(auctionUI, "bidUI",            bidUI);
        SetPrivateField(auctionUI, "goldText",         goldText.GetComponent<TMP_Text>());
        SetPrivateField(auctionUI, "usernameText",     usernameText.GetComponent<TMP_Text>());
        SetPrivateField(auctionUI, "statusText",       statusText.GetComponent<TMP_Text>());
        SetPrivateField(auctionUI, "refreshButton",    refreshBtn.GetComponent<Button>());
        SetPrivateField(auctionUI, "logoutButton",    logoutBtn.GetComponent<Button>());
        SetPrivateField(auctionUI, "listItemButton",  listItemBtn.GetComponent<Button>());
        SetPrivateField(auctionUI, "listItemPanel",   listItemPanel);
        SetPrivateField(auctionUI, "inventoryButton", inventoryBtn.GetComponent<Button>());
        SetPrivateField(auctionUI, "inventoryPanel",  inventoryPanel);

        var notifPopupGO = new GameObject("NotificationPopup");
        notifPopupGO.transform.SetParent(canvasGO.transform, false);
        var notifRT = notifPopupGO.AddComponent<RectTransform>();
        notifRT.anchorMin        = new Vector2(1, 1);
        notifRT.anchorMax        = new Vector2(1, 1);
        notifRT.pivot            = new Vector2(1, 1);
        notifRT.anchoredPosition = new Vector2(-20, -80);
        notifRT.sizeDelta        = new Vector2(340, 80);
        var notifBg = notifPopupGO.AddComponent<Image>();
        notifBg.color = BgCard; // 흰색 배경
        AddOutline(notifPopupGO, AccentBlue, 1.5f); // 파란 테두리로 눈에 띄게

        // 왼쪽 강조 바 (blue-600)
        var accentBar = new GameObject("AccentBar");
        accentBar.transform.SetParent(notifPopupGO.transform, false);
        var abRT = accentBar.AddComponent<RectTransform>();
        abRT.anchorMin        = new Vector2(0, 0);
        abRT.anchorMax        = new Vector2(0, 1);
        abRT.pivot            = new Vector2(0, 0.5f);
        abRT.anchoredPosition = Vector2.zero;
        abRT.sizeDelta        = new Vector2(5, 0);
        accentBar.AddComponent<Image>().color = AccentBlue;

        var notifMsgGO = new GameObject("MessageText");
        notifMsgGO.transform.SetParent(notifPopupGO.transform, false);
        var notifMsgRT = notifMsgGO.AddComponent<RectTransform>();
        notifMsgRT.anchorMin = Vector2.zero;
        notifMsgRT.anchorMax = Vector2.one;
        notifMsgRT.offsetMin = new Vector2(18, 8);
        notifMsgRT.offsetMax = new Vector2(-12, -8);
        var notifMsgTMP = notifMsgGO.AddComponent<TextMeshProUGUI>();
        notifMsgTMP.font      = GetKoreanFont();
        notifMsgTMP.fontSize  = 15;
        notifMsgTMP.color     = TextPrimary; // gray-900
        notifMsgTMP.fontStyle = FontStyles.Bold;
        notifMsgTMP.alignment = TextAlignmentOptions.MidlineLeft;
        notifMsgTMP.textWrappingMode = TextWrappingModes.Normal;

        var notifPopupUI = notifPopupGO.AddComponent<AuctionSystem.UI.NotificationPopupUI>();
        SetPrivateField(notifPopupUI, "messageText", notifMsgTMP);
        SetPrivateField(notifPopupUI, "bgImage",     notifBg);

        SetPrivateField(auctionUI, "notificationPopup", notifPopupUI);

        // ── 알림 목록 패널 (NotificationListUI) ──────────────────────────────
        var notifListPanel = CreateRoundedCard(auctionPanel.transform, "NotificationListPanel",
            new Vector2(620, 55), new Vector2(500, 680), BgCard, Border);
        notifListPanel.AddComponent<AuctionSystem.UI.DraggablePanel>();
        notifListPanel.SetActive(false);

        // 헤더 영역
        var nlHeader = CreatePanel(notifListPanel.transform, "NLHeader",
            new Vector2(0, 306), new Vector2(460, 52), BgSurface);
        AddBorderBottom(nlHeader, Border);
        var nlTitle = CreateTMPText(nlHeader.transform, "TitleText", "알림",
            new Vector2(-160, 0), new Vector2(260, 44), 18, FontStyles.Bold, TextPrimary);
        var nlCloseBtn = CreateButton(nlHeader.transform, "CloseButton", "×",
            new Vector2(200, 0), new Vector2(40, 40), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), AccentRed);

        // 빈 상태 텍스트
        var nlEmptyText = CreateTMPText(notifListPanel.transform, "EmptyText", "알림이 없습니다.",
            new Vector2(0, 0), new Vector2(440, 40), 16, FontStyles.Normal, TextMuted);
        nlEmptyText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // 스크롤 뷰 (목록)
        var nlScroll = CreateScrollView(notifListPanel.transform, "NLScrollView",
            new Vector2(0, -30), new Vector2(480, 580));

        var nlContent = nlScroll.transform.Find("Viewport/Content");

        // NotificationListUI 컴포넌트 추가
        var notifListUI = notifListPanel.AddComponent<AuctionSystem.UI.NotificationListUI>();
        SetPrivateField(notifListUI, "panelRoot",   notifListPanel);
        SetPrivateField(notifListUI, "listContent", nlContent);
        SetPrivateField(notifListUI, "titleText",   nlTitle.GetComponent<TMP_Text>());
        SetPrivateField(notifListUI, "closeButton", nlCloseBtn.GetComponent<Button>());
        SetPrivateField(notifListUI, "emptyText",   nlEmptyText.GetComponent<TMP_Text>());

        // 알림 아이템 프리팹 빌드 후 연결
        var notifItemPrefab = BuildNotificationItemPrefab();
        SetPrivateField(notifListUI, "itemPrefab", notifItemPrefab);

        // AuctionUI에 연결
        SetPrivateField(auctionUI, "notificationButton",  notifBtn.GetComponent<Button>());
        SetPrivateField(auctionUI, "notificationListUI",  notifListUI);
        // ────────────────────────────────────────────────────────────────────

        var debugPanel = new GameObject("DebugPanel");
        debugPanel.transform.SetParent(auctionPanel.transform, false);
        var dpRT = debugPanel.AddComponent<RectTransform>();
        dpRT.anchorMin = new Vector2(1, 0);
        dpRT.anchorMax = new Vector2(1, 0);
        dpRT.pivot     = new Vector2(1, 0);
        dpRT.anchoredPosition = new Vector2(-16, 16);
        dpRT.sizeDelta = new Vector2(300, 260);
        var dpBg = debugPanel.AddComponent<Image>();
        dpBg.color = new Color(0.97f, 0.97f, 0.97f, 0.97f); // off-white panel
        AddOutline(debugPanel, Border);
        var korFont = GetKoreanFont();

        var dpTitle = new GameObject("TitleText");
        dpTitle.transform.SetParent(debugPanel.transform, false);
        var dpTitleRT = dpTitle.AddComponent<RectTransform>();
        dpTitleRT.anchorMin = new Vector2(0, 1); dpTitleRT.anchorMax = new Vector2(1, 1);
        dpTitleRT.pivot = new Vector2(0.5f, 1);
        dpTitleRT.anchoredPosition = new Vector2(0, -8);
        dpTitleRT.sizeDelta = new Vector2(-16, 28);
        var dpTitleTMP = dpTitle.AddComponent<TextMeshProUGUI>();
        dpTitleTMP.font = korFont;
        dpTitleTMP.text = "DEBUG";
        dpTitleTMP.fontSize = 14; dpTitleTMP.fontStyle = FontStyles.Bold;
        dpTitleTMP.color = new Color(0.55f, 0.35f, 0.0f); // amber-800 (readable on light bg)
        dpTitleTMP.alignment = TextAlignmentOptions.Center;

        var dpUserText = new GameObject("UserIdText");
        dpUserText.transform.SetParent(debugPanel.transform, false);
        var dpUserRT = dpUserText.AddComponent<RectTransform>();
        dpUserRT.anchorMin = new Vector2(0, 1); dpUserRT.anchorMax = new Vector2(1, 1);
        dpUserRT.pivot = new Vector2(0.5f, 1);
        dpUserRT.anchoredPosition = new Vector2(0, -40);
        dpUserRT.sizeDelta = new Vector2(-16, 22);
        var dpUserTMP = dpUserText.AddComponent<TextMeshProUGUI>();
        dpUserTMP.font = korFont;
        dpUserTMP.text = "[DEBUG] 로그인 전";
        dpUserTMP.fontSize = 12;
        dpUserTMP.color = TextSecondary; // gray-500
        dpUserTMP.alignment = TextAlignmentOptions.Center;

        var dpGoldText = new GameObject("CurrentGoldText");
        dpGoldText.transform.SetParent(debugPanel.transform, false);
        var dpGoldRT = dpGoldText.AddComponent<RectTransform>();
        dpGoldRT.anchorMin = new Vector2(0, 1); dpGoldRT.anchorMax = new Vector2(1, 1);
        dpGoldRT.pivot = new Vector2(0.5f, 1);
        dpGoldRT.anchoredPosition = new Vector2(0, -64);
        dpGoldRT.sizeDelta = new Vector2(-16, 24);
        var dpGoldTMP = dpGoldText.AddComponent<TextMeshProUGUI>();
        dpGoldTMP.font = korFont;
        dpGoldTMP.text = "Gold: 0G";
        dpGoldTMP.fontSize = 15; dpGoldTMP.fontStyle = FontStyles.Bold;
        dpGoldTMP.color = AccentGold; // amber-600
        dpGoldTMP.alignment = TextAlignmentOptions.Center;

        var btn1k  = CreateButton(debugPanel.transform, "AddGold1000Button",   "+1K",   new Vector2(-88, -105), new Vector2(80, 36), AccentGreenDk);
        var btn10k = CreateButton(debugPanel.transform, "AddGold10000Button",  "+10K",  new Vector2(0,   -105), new Vector2(80, 36), AccentGreenDk);
        var btn100k= CreateButton(debugPanel.transform, "AddGold100000Button", "+100K", new Vector2(88,  -105), new Vector2(80, 36), AccentBlue);

        var dpFeedback = new GameObject("FeedbackText");
        dpFeedback.transform.SetParent(debugPanel.transform, false);
        var dpFbRT = dpFeedback.AddComponent<RectTransform>();
        dpFbRT.anchorMin = new Vector2(0, 1); dpFbRT.anchorMax = new Vector2(1, 1);
        dpFbRT.pivot = new Vector2(0.5f, 1);
        dpFbRT.anchoredPosition = new Vector2(0, -152);
        dpFbRT.sizeDelta = new Vector2(-16, 36);
        var dpFbTMP = dpFeedback.AddComponent<TextMeshProUGUI>();
        dpFbTMP.font = korFont;
        dpFbTMP.text = "";
        dpFbTMP.fontSize = 13;
        dpFbTMP.color = StateSuccess; // green-600 on light bg
        dpFbTMP.alignment = TextAlignmentOptions.Center;
        dpFbTMP.gameObject.SetActive(false);

        var debugUI = debugPanel.AddComponent<AuctionSystem.UI.DebugPanel>();
        SetPrivateField(debugUI, "addGold1000Button",   btn1k.GetComponent<Button>());
        SetPrivateField(debugUI, "addGold10000Button",  btn10k.GetComponent<Button>());
        SetPrivateField(debugUI, "addGold100000Button", btn100k.GetComponent<Button>());
        SetPrivateField(debugUI, "currentGoldText",     dpGoldTMP);
        SetPrivateField(debugUI, "feedbackText",        dpFbTMP);
        SetPrivateField(debugUI, "userIdText",          dpUserTMP);

        SetPrivateField(loginUI, "loginPanel",   loginPanel);
        SetPrivateField(loginUI, "auctionPanel", auctionPanel);
        SetPrivateField(auctionUI, "loginPanel",   loginPanel);
        SetPrivateField(auctionUI, "auctionPanel", auctionPanel);

        BuildItemCardPrefab(auctionUI);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Debug.Log("[SceneBuilder] 씬 빌드 완료!");
        EditorUtility.DisplayDialog("SceneBuilder",
            "씬 빌드가 완료됐습니다!\n\n남은 작업:\n• SupabaseConfig.cs에 실제 URL/Key 입력\n• DebugPanel은 배포 전 비활성화(AuctionPanel/DebugPanel GameObject)", "확인");
    }

    private static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    private static TMP_FontAsset GetKoreanFont()
    {
        const string knownPath = "Assets/TextMesh Pro/Fonts/NanumGothic SDF.asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(knownPath);
        if (font != null) return font;

        foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("NanumGothic"))
            {
                var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (f != null) return f;
            }
        }

        Debug.LogWarning("[SceneBuilder] NanumGothic SDF not found, falling back to LiberationSans.");
        return TMP_Settings.defaultFontAsset;
    }

    private static GameObject CreateRoundedCard(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size, Color bgColor, Color borderColor)
    {
        var go = CreatePanel(parent, name, anchoredPos, size, bgColor);
        AddOutline(go, borderColor, 1.5f);
        return go;
    }

    private static void AddOutline(GameObject go, Color borderColor, float thickness = 1f)
    {
        var outline = go.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(thickness, -thickness);
        outline.useGraphicAlpha = false;
    }

    private static void AddLoadingSpinner(GameObject indicator)
    {
        var existing = indicator.transform.Find("LoadingText");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var spinGO = new GameObject("SpinnerRing");
        spinGO.transform.SetParent(indicator.transform, false);
        var spinRT = spinGO.AddComponent<RectTransform>();
        spinRT.anchorMin = new Vector2(0.5f, 0.5f);
        spinRT.anchorMax = new Vector2(0.5f, 0.5f);
        spinRT.anchoredPosition = Vector2.zero;
        spinRT.sizeDelta = new Vector2(28, 28);
        var spinImg = spinGO.AddComponent<UnityEngine.UI.Image>();
        spinImg.color = AccentBlue;
        spinImg.type  = UnityEngine.UI.Image.Type.Filled;
        spinImg.fillMethod  = UnityEngine.UI.Image.FillMethod.Radial360;
        spinImg.fillAmount  = 0.75f;
        spinImg.fillClockwise = true;

        var spinner = indicator.AddComponent<AuctionSystem.UI.LoadingSpinner>();
        SetPrivateField(spinner, "spinTarget", spinGO.transform);
    }

    private static void AddBorderBottom(GameObject parent, Color borderColor)
    {
        var line = new GameObject("BorderBottom");
        line.transform.SetParent(parent.transform, false);
        var rt = line.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 1);
        line.AddComponent<Image>().color = borderColor;
    }

    private static GameObject CreateFullscreenPanel(Transform parent, string name, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        return go;
    }

    private static GameObject CreateTMPText(Transform parent, string name, string text,
        Vector2 anchoredPos, Vector2 size, float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = GetKoreanFont();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static GameObject CreateInputField(Transform parent, string name, Vector2 anchoredPos, Vector2 size,
        string placeholder, TMP_InputField.ContentType contentType)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = anchoredPos;
        rootRT.sizeDelta = size;
        var rootImg = root.AddComponent<Image>();
        rootImg.color = BgInput;
        var inputOutline = root.AddComponent<UnityEngine.UI.Outline>();
        inputOutline.effectColor = Border;
        inputOutline.effectDistance = new Vector2(1.5f, -1.5f);
        inputOutline.useGraphicAlpha = false;
        var inputField = root.AddComponent<TMP_InputField>();

        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(root.transform, false);
        var taRT = textArea.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(10, 4);
        taRT.offsetMax = new Vector2(-10, -4);
        var mask = textArea.AddComponent<RectMask2D>();

        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(textArea.transform, false);
        var phRT = placeholderGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;
        var phTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
        phTMP.font = GetKoreanFont();
        phTMP.text = placeholder;
        phTMP.color = new Color(0.6f, 0.6f, 0.6f, 0.9f); // gray-400 placeholder
        phTMP.fontSize = 16;
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;
        var tTMP = textGO.AddComponent<TextMeshProUGUI>();
        tTMP.font = GetKoreanFont();
        tTMP.color = TextPrimary; // gray-900 (라이트 테마)
        tTMP.fontSize = 16;
        tTMP.alignment = TextAlignmentOptions.MidlineLeft;

        inputField.textViewport = taRT;
        inputField.textComponent = tTMP;
        inputField.placeholder = phTMP;
        inputField.contentType = contentType;
        inputField.caretColor = TextPrimary;

        return root;
    }

    private static GameObject CreateButton(Transform parent, string name, string labelText,
        Vector2 anchoredPos, Vector2 size, Color bgColor, Color borderColor, Color textColor)
    {
        var go = CreateButton(parent, name, labelText, anchoredPos, size, bgColor);
        AddOutline(go, borderColor);
        var lTMP = go.GetComponentInChildren<TextMeshProUGUI>();
        if (lTMP != null) lTMP.color = textColor;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string labelText,
        Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        var cb = new ColorBlock();
        cb.normalColor      = bgColor;
        cb.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.1f, bgColor.b + 0.1f, 1f);
        cb.pressedColor     = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f, 1f);
        cb.selectedColor    = bgColor;
        cb.disabledColor    = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        btn.colors = cb;
        btn.targetGraphic = img;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lRT = labelGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = Vector2.zero;
        lRT.offsetMax = Vector2.zero;
        var lTMP = labelGO.AddComponent<TextMeshProUGUI>();
        lTMP.font = GetKoreanFont();
        lTMP.text = labelText;
        lTMP.fontSize = 17;
        lTMP.fontStyle = FontStyles.Bold;
        lTMP.color = Color.white; // 기본값 — 오버로드에서 덮어씌움
        lTMP.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private static GameObject CreateDropdown(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string[] options)
    {
        // ── 루트 ──────────────────────────────────────────────────────────
        // Canvas/GraphicRaycaster는 루트에도 Template에도 붙이지 않는다.
        // TMP_Dropdown.SetupTemplate()이 자체적으로 Template에 Canvas(sortingOrder=30000)를
        // 붙이고, Show()가 Dropdown List를 Template의 형제(sibling)로 배치한다.
        // 우리가 Canvas를 추가하면 rootCanvas 탐색이 꼬여 위치 계산이 깨진다.
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        var rootImg = go.AddComponent<Image>();
        rootImg.color = BgInput;
        var dropOutline = go.AddComponent<UnityEngine.UI.Outline>();
        dropOutline.effectColor     = Border;
        dropOutline.effectDistance  = new Vector2(1.5f, -1.5f);
        dropOutline.useGraphicAlpha = false;

        var drop = go.AddComponent<TMP_Dropdown>();
        drop.targetGraphic = rootImg;
        drop.options.Clear();
        foreach (var opt in options)
            drop.options.Add(new TMP_Dropdown.OptionData(opt));

        // ── Caption Label ─────────────────────────────────────────────────
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lRT = labelGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(10, 2);
        lRT.offsetMax = new Vector2(-28, -2);
        var lTMP = labelGO.AddComponent<TextMeshProUGUI>();
        lTMP.font         = GetKoreanFont();
        lTMP.text         = options.Length > 0 ? options[0] : "";
        lTMP.fontSize     = 15;
        lTMP.color        = TextPrimary;
        lTMP.alignment    = TextAlignmentOptions.MidlineLeft;
        lTMP.overflowMode = TextOverflowModes.Ellipsis;
        drop.captionText = lTMP;

        // ── Arrow ─────────────────────────────────────────────────────────
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        var arRT = arrowGO.AddComponent<RectTransform>();
        arRT.anchorMin        = new Vector2(1, 0.5f);
        arRT.anchorMax        = new Vector2(1, 0.5f);
        arRT.pivot            = new Vector2(1, 0.5f);
        arRT.anchoredPosition = new Vector2(-6, 0);
        arRT.sizeDelta        = new Vector2(20, 20);
        var arTMP = arrowGO.AddComponent<TextMeshProUGUI>();
        arTMP.font      = GetKoreanFont();
        arTMP.text      = "▼";
        arTMP.fontSize  = 11;
        arTMP.color     = TextSecondary;
        arTMP.alignment = TextAlignmentOptions.Center;

        // ── Template ──────────────────────────────────────────────────────
        var templateGO = new GameObject("Template");
        templateGO.transform.SetParent(go.transform, false);
        var tRT = templateGO.AddComponent<RectTransform>();
        // anchorY=0, pivot Y=1 → Dropdown 하단 기준으로 아래로 펼침
        // FlipLayoutOnAxis가 화면 밖일 때만 자동으로 위로 뒤집음
        tRT.anchorMin        = new Vector2(0, 0);
        tRT.anchorMax        = new Vector2(1, 0);
        tRT.pivot            = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = Vector2.zero;
        // 최대 높이 = 36 * 10 = 360. TMP_Dropdown.Show()가 실제 항목 수에 맞게 줄여줌
        tRT.sizeDelta        = new Vector2(0, 360);
        templateGO.AddComponent<Image>().color = BgCard;
        AddOutline(templateGO, Border, 1f);
        var scrollRect = templateGO.AddComponent<ScrollRect>();
        scrollRect.horizontal   = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        drop.template = tRT;

        // ── Viewport ──────────────────────────────────────────────────────
        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(templateGO.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        // RectMask2D: Image 불필요, Stencil 없이 Rect 기반 클리핑
        // Canvas(sortingOrder=30000)가 Template에 붙으면 Stencil 기반 Mask는 깨짐 → RectMask2D 사용
        vpGO.AddComponent<RectMask2D>();
        scrollRect.viewport = vpRT;

        // ── Content ───────────────────────────────────────────────────────
        // LayoutGroup/ContentSizeFitter 없음.
        // TMP_Dropdown Show()가 line 926에서 직접 sizeDelta.y를 계산해 설정한다.
        // LayoutGroup이 있으면 Show() 시점(rect 미계산)에 itemSize.y=0이 돼 항목이 안 보임.
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        // anchor bottom, pivot bottom — TMP_Dropdown이 item을 양수 y(위쪽)로 배치하므로
        // Content가 아래에서 위로 자라야 item들이 Content 안에 위치함
        contentRT.anchorMin        = new Vector2(0, 0);
        contentRT.anchorMax        = new Vector2(1, 0);
        contentRT.pivot            = new Vector2(0.5f, 0f);
        contentRT.anchoredPosition = Vector2.zero;
        // sizeDelta.y = item 1개 높이(36) 로 초기화해야 TMP_Dropdown Show()의
        // offsetMin/offsetMax 계산이 맞아 동적 크기(36*N)가 정확히 나옴
        contentRT.sizeDelta        = new Vector2(0, 36);
        scrollRect.content = contentRT;

        // ── Item ──────────────────────────────────────────────────────────
        // sizeDelta.y=36 을 직접 지정 → Show() 시 itemTemplate.rectTransform.rect.size.y=36
        // anchorMin/Max=(0,0.5f)~(1,0.5f): 좌우 스트레치, Y는 pivot 기준 고정 크기
        var itemGO = new GameObject("Item");
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRT = itemGO.AddComponent<RectTransform>();
        itemRT.anchorMin        = new Vector2(0, 0.5f);
        itemRT.anchorMax        = new Vector2(1, 0.5f);
        itemRT.pivot            = new Vector2(0.5f, 0.5f);
        itemRT.anchoredPosition = Vector2.zero;
        itemRT.sizeDelta        = new Vector2(0, 36);
        var toggleImg = itemGO.AddComponent<Image>();
        toggleImg.color = Color.white;
        var toggle = itemGO.AddComponent<Toggle>();
        toggle.targetGraphic = toggleImg;
        toggle.isOn = false;
        // Navigation.None: toggle.Select()가 ScrollRect 자동 스크롤을 유발하지 않도록 차단
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        var cb = new ColorBlock();
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(0.93f, 0.96f, 1f, 1f);
        cb.pressedColor     = new Color(0.85f, 0.90f, 1f, 1f);
        cb.selectedColor    = new Color(0.93f, 0.96f, 1f, 1f);
        cb.disabledColor    = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        toggle.colors = cb;

        // Item Checkmark — 선택된 항목에 ✓ 표시 (TMP_Dropdown이 active 토글)
        var checkGO = new GameObject("Item Checkmark");
        checkGO.transform.SetParent(itemGO.transform, false);
        var ckRT = checkGO.AddComponent<RectTransform>();
        ckRT.anchorMin        = new Vector2(0, 0.5f);
        ckRT.anchorMax        = new Vector2(0, 0.5f);
        ckRT.pivot            = new Vector2(0, 0.5f);
        ckRT.anchoredPosition = new Vector2(4, 0);
        ckRT.sizeDelta        = new Vector2(20, 20);
        var ckTMP = checkGO.AddComponent<TextMeshProUGUI>();
        ckTMP.font      = GetKoreanFont();
        ckTMP.text      = "v";
        ckTMP.fontSize  = 13;
        ckTMP.color     = AccentBlue;
        ckTMP.alignment = TextAlignmentOptions.Center;
        // toggle.graphic = ckTMP → TMP_Dropdown이 선택 항목만 활성화
        toggle.graphic = ckTMP;

        // Item Label — 체크마크(24px) 오른쪽부터 시작
        var itemLabelGO = new GameObject("Item Label");
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        var ilRT = itemLabelGO.AddComponent<RectTransform>();
        ilRT.anchorMin = Vector2.zero;
        ilRT.anchorMax = Vector2.one;
        ilRT.offsetMin = new Vector2(28, 2);
        ilRT.offsetMax = new Vector2(-8, -2);
        var ilTMP = itemLabelGO.AddComponent<TextMeshProUGUI>();
        ilTMP.font         = GetKoreanFont();
        ilTMP.fontSize     = 14;
        ilTMP.color        = TextPrimary;
        ilTMP.alignment    = TextAlignmentOptions.MidlineLeft;
        ilTMP.overflowMode = TextOverflowModes.Ellipsis;
        drop.itemText = ilTMP;

        // 모든 자식 구성 완료 후 Template 비활성화
        templateGO.SetActive(false);

        drop.value = 0;
        drop.RefreshShownValue();

        // 런타임에 열릴 때 스크롤을 item 0(최상단)으로 강제 이동
        go.AddComponent<AuctionSystem.Utils.DropdownHelper>();

        return go;
    }

    private static GameObject CreateScrollView(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var sv = new GameObject(name);
        sv.transform.SetParent(parent, false);
        var svRT = sv.AddComponent<RectTransform>();
        svRT.anchorMin = new Vector2(0.5f, 0.5f);
        svRT.anchorMax = new Vector2(0.5f, 0.5f);
        svRT.anchoredPosition = anchoredPos;
        svRT.sizeDelta = size;
        sv.AddComponent<Image>().color = new Color(BgBase.r, BgBase.g, BgBase.b, 0.01f);
        var scrollRect = sv.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(sv.transform, false);
        var vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1f);
        cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(0, 0);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpRT;
        scrollRect.content = cRT;

        return sv;
    }

    private static void BuildItemCardPrefab(AuctionSystem.UI.AuctionUI auctionUI)
    {
        string prefabDir = "Assets/Prefabs/AuctionPanel";
        System.IO.Directory.CreateDirectory(
            System.IO.Path.Combine(Application.dataPath, "../" + prefabDir));

        var card = new GameObject("ItemCard");
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(0, 128);

        var bg = card.AddComponent<Image>();
        bg.color = BgCard;
        var cardOutline = card.AddComponent<UnityEngine.UI.Outline>();
        cardOutline.effectColor = Border;
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
        cardOutline.useGraphicAlpha = false;

        var btn = card.AddComponent<Button>();
        btn.targetGraphic = bg;
        var cb = new ColorBlock();
        cb.normalColor      = BgCard;
        cb.highlightedColor = BgHover;
        cb.pressedColor     = BgSurface;
        cb.selectedColor    = BgHover;
        cb.disabledColor    = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        btn.colors = cb;

        // LayoutElement
        var le = card.AddComponent<LayoutElement>();
        le.minHeight = 128;
        le.preferredHeight = 128;

        var imageAreaGO = new GameObject("ImageArea");
        imageAreaGO.transform.SetParent(card.transform, false);
        var iaRT = imageAreaGO.AddComponent<RectTransform>();
        iaRT.anchorMin        = new Vector2(0, 0);
        iaRT.anchorMax        = new Vector2(0, 1);
        iaRT.pivot            = new Vector2(0, 0.5f);
        iaRT.anchoredPosition = new Vector2(8, 0);
        iaRT.sizeDelta        = new Vector2(112, -16);
        var iaMask = imageAreaGO.AddComponent<RectMask2D>();

        var itemImageGO = new GameObject("ItemImage");
        itemImageGO.transform.SetParent(imageAreaGO.transform, false);
        var iiRT = itemImageGO.AddComponent<RectTransform>();
        iiRT.anchorMin = Vector2.zero;
        iiRT.anchorMax = Vector2.one;
        iiRT.offsetMin = Vector2.zero;
        iiRT.offsetMax = Vector2.zero;
        var rawImage = itemImageGO.AddComponent<RawImage>();
        rawImage.color = new Color(0.9f, 0.9f, 0.9f, 1f); // gray-100 placeholder (light)

        // NameText: 이미지(8+112+8=128px) 오른쪽 ~ 카드 우측 절반 전까지
        var nameGO = CreateTMPText(card.transform, "NameText", "아이템 이름",
            Vector2.zero, new Vector2(0, 36), 20, FontStyles.Bold, TextPrimary);
        nameGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin        = new Vector2(0, 0.5f);
        nameRT.anchorMax        = new Vector2(0.55f, 0.5f);
        nameRT.offsetMin        = new Vector2(128, 4);
        nameRT.offsetMax        = new Vector2(0, 40);

        // CategoryText: NameText 아래
        var catGO = CreateTMPText(card.transform, "CategoryText", "무기",
            Vector2.zero, new Vector2(0, 26), 13, FontStyles.Bold, AccentGreen);
        var catRT = catGO.GetComponent<RectTransform>();
        catRT.anchorMin        = new Vector2(0, 0.5f);
        catRT.anchorMax        = new Vector2(0.55f, 0.5f);
        catRT.offsetMin        = new Vector2(128, -22);
        catRT.offsetMax        = new Vector2(0, 4);
        catGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

        var bidGO = CreateTMPText(card.transform, "CurrentBidText", "현재가: 0G",
            Vector2.zero, new Vector2(260, 30), 18, FontStyles.Bold, AccentGreen);
        var bidRT = bidGO.GetComponent<RectTransform>();
        bidRT.anchorMin        = new Vector2(1, 0.5f);
        bidRT.anchorMax        = new Vector2(1, 0.5f);
        bidRT.anchoredPosition = new Vector2(-152, 28);
        bidGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineRight;

        var buyGO = CreateTMPText(card.transform, "BuyoutText", "즉시구매: -",
            Vector2.zero, new Vector2(260, 28), 14, FontStyles.Normal, AccentOrange);
        var buyRT = buyGO.GetComponent<RectTransform>();
        buyRT.anchorMin        = new Vector2(1, 0.5f);
        buyRT.anchorMax        = new Vector2(1, 0.5f);
        buyRT.anchoredPosition = new Vector2(-153, -2);
        buyGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineRight;

        var timeGO = CreateTMPText(card.transform, "TimeLeftText", "--분 --초 남음",
            Vector2.zero, new Vector2(200, 28), 14, FontStyles.Normal, AccentRed);
        var timeRT = timeGO.GetComponent<RectTransform>();
        timeRT.anchorMin        = new Vector2(1, 0.5f);
        timeRT.anchorMax        = new Vector2(1, 0.5f);
        timeRT.anchoredPosition = new Vector2(-125, -28);
        timeGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineRight;

        var cardUI = card.AddComponent<ItemCardUI>();
        SetPrivateField(cardUI, "itemImage",      rawImage);
        SetPrivateField(cardUI, "nameText",       nameGO.GetComponent<TMP_Text>());
        SetPrivateField(cardUI, "currentBidText", bidGO.GetComponent<TMP_Text>());
        SetPrivateField(cardUI, "buyoutText",     buyGO.GetComponent<TMP_Text>());
        SetPrivateField(cardUI, "timeLeftText",   timeGO.GetComponent<TMP_Text>());
        SetPrivateField(cardUI, "categoryText",   catGO.GetComponent<TMP_Text>());
        SetPrivateField(cardUI, "selectButton",   btn);

        string prefabPath = prefabDir + "/ItemCard.prefab";
        bool success;
        var prefabAsset = PrefabUtility.SaveAsPrefabAsset(card, prefabPath, out success);
        Object.DestroyImmediate(card);

        if (success && prefabAsset != null)
        {
            Debug.Log($"[SceneBuilder] ItemCard Prefab saved: {prefabPath}");
            SetPrivateField(auctionUI, "itemCardPrefab", prefabAsset);
            EditorUtility.SetDirty(auctionUI);
        }
        else
        {
            Debug.LogWarning("[SceneBuilder] ItemCard Prefab 저장 실패 — Inspector에서 수동 연결 필요");
        }
    }

    private static GameObject BuildNotificationItemPrefab()
    {
        string prefabDir = "Assets/Prefabs/AuctionPanel";
        System.IO.Directory.CreateDirectory(
            System.IO.Path.Combine(Application.dataPath, "../" + prefabDir));

        // 행 루트 — 높이 80px
        var row = new GameObject("NotificationItem");
        var rowRT = row.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(460, 80);
        var rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.98f, 0.98f, 0.98f); // ReadBg (is_read=true 기본)
        AddOutline(row, Border, 1f);

        // 메시지 텍스트 (왼쪽)
        var msgGO = CreateTMPText(row.transform, "MessageText", "알림 내용",
            new Vector2(-20, 10), new Vector2(340, 44), 14, FontStyles.Normal, TextPrimary);
        var msgRT = msgGO.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0, 0.5f);
        msgRT.anchorMax = new Vector2(0, 0.5f);
        msgRT.pivot = new Vector2(0, 0.5f);
        msgRT.anchoredPosition = new Vector2(12, 10);
        msgRT.sizeDelta = new Vector2(350, 44);
        msgGO.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;
        msgGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.TopLeft;

        // 시간 텍스트 (왼쪽 하단)
        var timeGO = CreateTMPText(row.transform, "TimeText", "방금 전",
            Vector2.zero, new Vector2(200, 22), 12, FontStyles.Normal, TextMuted);
        var timeRT = timeGO.GetComponent<RectTransform>();
        timeRT.anchorMin = new Vector2(0, 0);
        timeRT.anchorMax = new Vector2(0, 0);
        timeRT.pivot = new Vector2(0, 0);
        timeRT.anchoredPosition = new Vector2(12, 8);
        timeRT.sizeDelta = new Vector2(200, 22);

        // 삭제 버튼 (오른쪽)
        var delBtn = CreateButton(row.transform, "DeleteButton", "×",
            new Vector2(0, 0), new Vector2(36, 36), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), AccentRed);
        var delRT = delBtn.GetComponent<RectTransform>();
        delRT.anchorMin = new Vector2(1, 0.5f);
        delRT.anchorMax = new Vector2(1, 0.5f);
        delRT.pivot = new Vector2(1, 0.5f);
        delRT.anchoredPosition = new Vector2(-8, 0);
        delRT.sizeDelta = new Vector2(36, 36);
        delBtn.GetComponentInChildren<TMP_Text>().fontSize = 20;

        // LayoutElement — 스크롤 뷰 아이템 높이 고정
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 80;
        le.flexibleWidth  = 1;

        var itemUI = row.AddComponent<AuctionSystem.UI.NotificationItemUI>();
        SetPrivateField(itemUI, "messageText",  msgGO.GetComponent<TMP_Text>());
        SetPrivateField(itemUI, "timeText",     timeGO.GetComponent<TMP_Text>());
        SetPrivateField(itemUI, "deleteButton", delBtn.GetComponent<Button>());
        SetPrivateField(itemUI, "bgImage",      rowBg);

        string prefabPath = prefabDir + "/NotificationItem.prefab";
        bool success;
        var prefabAsset = PrefabUtility.SaveAsPrefabAsset(row, prefabPath, out success);
        Object.DestroyImmediate(row);

        if (success && prefabAsset != null)
        {
            Debug.Log($"[SceneBuilder] NotificationItem Prefab saved: {prefabPath}");
            return prefabAsset;
        }
        Debug.LogWarning("[SceneBuilder] NotificationItem Prefab 저장 실패");
        return null;
    }

    private static GameObject BuildInventoryCardPrefab()
    {
        string prefabDir = "Assets/Prefabs/AuctionPanel";
        System.IO.Directory.CreateDirectory(
            System.IO.Path.Combine(Application.dataPath, "../" + prefabDir));

        var card = new GameObject("InventoryCard");
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(0, 80);

        var bg = card.AddComponent<Image>();
        bg.color = BgCard;
        var outline = card.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor    = Border;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = false;

        var le = card.AddComponent<LayoutElement>();
        le.minHeight       = 80;
        le.preferredHeight = 80;

        // 아이템 이미지 — 왼쪽 고정 64px
        var imageAreaGO = new GameObject("ImageArea");
        imageAreaGO.transform.SetParent(card.transform, false);
        var iaRT = imageAreaGO.AddComponent<RectTransform>();
        iaRT.anchorMin        = new Vector2(0, 0);
        iaRT.anchorMax        = new Vector2(0, 1);
        iaRT.pivot            = new Vector2(0, 0.5f);
        iaRT.anchoredPosition = new Vector2(8, 0);
        iaRT.sizeDelta        = new Vector2(64, -12);
        imageAreaGO.AddComponent<RectMask2D>();

        var itemImageGO = new GameObject("ItemImage");
        itemImageGO.transform.SetParent(imageAreaGO.transform, false);
        var iiRT = itemImageGO.AddComponent<RectTransform>();
        iiRT.anchorMin = Vector2.zero;
        iiRT.anchorMax = Vector2.one;
        iiRT.offsetMin = Vector2.zero;
        iiRT.offsetMax = Vector2.zero;
        var rawImage = itemImageGO.AddComponent<RawImage>();
        rawImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        // 이름 — 이미지(80px) 오른쪽부터 상태뱃지(90px) 왼쪽까지 stretch
        var nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(card.transform, false);
        var nameRT = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.5f);
        nameRT.anchorMax = new Vector2(1, 0.5f);
        nameRT.offsetMin = new Vector2(82, 4);   // 왼쪽 여백: 이미지(8+64+10)
        nameRT.offsetMax = new Vector2(-96, 40); // 오른쪽 여백: 상태뱃지+패딩
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.font      = GetKoreanFont();
        nameTMP.text      = "아이템 이름";
        nameTMP.fontSize  = 16;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color     = TextPrimary;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
        nameTMP.overflowMode = TextOverflowModes.Ellipsis;

        // 카테고리 — 이름과 동일 가로 범위, 아래쪽 절반
        var catGO = new GameObject("CategoryText");
        catGO.transform.SetParent(card.transform, false);
        var catRT = catGO.AddComponent<RectTransform>();
        catRT.anchorMin = new Vector2(0, 0);
        catRT.anchorMax = new Vector2(1, 0.5f);
        catRT.offsetMin = new Vector2(82, 4);
        catRT.offsetMax = new Vector2(-96, 0);
        var catTMP = catGO.AddComponent<TextMeshProUGUI>();
        catTMP.font      = GetKoreanFont();
        catTMP.text      = "카테고리";
        catTMP.fontSize  = 13;
        catTMP.color     = TextSecondary;
        catTMP.alignment = TextAlignmentOptions.MidlineLeft;
        catTMP.overflowMode = TextOverflowModes.Ellipsis;

        // 상태뱃지 — 오른쪽 고정 80px
        var statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(card.transform, false);
        var statusRT = statusGO.AddComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(1, 0);
        statusRT.anchorMax = new Vector2(1, 1);
        statusRT.pivot     = new Vector2(1, 0.5f);
        statusRT.offsetMin = new Vector2(-88, 0);
        statusRT.offsetMax = new Vector2(-8, 0);
        var statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.font      = GetKoreanFont();
        statusTMP.text      = "보유 중";
        statusTMP.fontSize  = 14;
        statusTMP.fontStyle = FontStyles.Bold;
        statusTMP.color     = AccentGreen;
        statusTMP.alignment = TextAlignmentOptions.MidlineRight;
        statusTMP.overflowMode = TextOverflowModes.Ellipsis;

        string prefabPath = prefabDir + "/InventoryCard.prefab";
        bool success;
        var prefabAsset = PrefabUtility.SaveAsPrefabAsset(card, prefabPath, out success);
        Object.DestroyImmediate(card);

        if (success && prefabAsset != null)
        {
            Debug.Log($"[SceneBuilder] InventoryCard Prefab saved: {prefabPath}");
            return prefabAsset;
        }

        Debug.LogWarning("[SceneBuilder] InventoryCard Prefab 저장 실패");
        return null;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }
            type = type.BaseType;
        }
        Debug.LogWarning($"[SceneBuilder] 필드를 찾을 수 없음: {fieldName} on {target.GetType().Name}");
    }
}
