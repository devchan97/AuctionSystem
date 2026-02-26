using AuctionSystem.Auth;
using AuctionSystem.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class LoginUI : MonoBehaviour
    {
        [Header("입력 필드")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("버튼")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button webAuthButton;
        [SerializeField] private Button cancelOAuthButton;
        [SerializeField] private Button quitButton;

        [Header("피드백")]
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("전환")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject auctionPanel;

        private LoginManager _loginManager;
        private WebBridgeAuth _webBridge;

        void Start()
        {
            loginButton.interactable = true;
            if (webAuthButton != null) webAuthButton.interactable = true;
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            if (cancelOAuthButton != null) cancelOAuthButton.gameObject.SetActive(false);

            _loginManager = Object.FindFirstObjectByType<LoginManager>();
            _webBridge = Object.FindFirstObjectByType<WebBridgeAuth>();

            if (_loginManager == null) Debug.LogError("[LoginUI] LoginManager를 찾을 수 없습니다.");

            loginButton.onClick.AddListener(OnLoginClicked);
            if (webAuthButton != null)
                webAuthButton.onClick.AddListener(OnWebAuthClicked);
            if (cancelOAuthButton != null)
                cancelOAuthButton.onClick.AddListener(OnCancelOAuthClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(() => Application.Quit());

            // Tab 이동: Email → Password
            if (emailInput != null)
                emailInput.onSubmit.AddListener(_ => { passwordInput?.ActivateInputField(); });

            // Enter: Password 필드에서 Enter 시 로그인 시도
            if (passwordInput != null)
                passwordInput.onSubmit.AddListener(_ => TryLoginFromEnter());

            _loginManager.OnLoginSuccess += HandleLoginSuccess;
            _loginManager.OnLoginFailed  += HandleLoginFailed;
            _loginManager.OnLogoutSuccess += HandleLogoutSuccess;

            if (SupabaseManager.Instance.IsLoggedIn)
            {
                ShowAuctionPanel();
                return;
            }

            SetLoading(false);
            ClearError();
        }

        void OnDestroy()
        {
            if (_loginManager == null) return;
            _loginManager.OnLoginSuccess -= HandleLoginSuccess;
            _loginManager.OnLoginFailed  -= HandleLoginFailed;
            _loginManager.OnLogoutSuccess -= HandleLogoutSuccess;
        }

        private void TryLoginFromEnter()
        {
            if (!loginButton.interactable) return;
            OnLoginClicked();
        }

        private void OnLoginClicked()
        {
            string email = emailInput.text.Trim();
            string password = passwordInput.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("이메일과 비밀번호를 입력해주세요.");
                return;
            }

            ClearError();
            SetLoading(true);
            _loginManager.SignInWithEmail(email, password);
        }

        private void OnWebAuthClicked()
        {
            ClearError();
            SetLoading(true);
            if (cancelOAuthButton != null) cancelOAuthButton.gameObject.SetActive(true);
            _loginManager.StartOAuthCallbackServer();
            _webBridge?.OpenSignupForUnity();
        }

        private void OnCancelOAuthClicked()
        {
            // listener.Stop() 호출 → 스레드의 GetContext()가 HttpListenerException으로 즉시 탈출
            _loginManager.StopOAuthCallbackServer();
            if (cancelOAuthButton != null) cancelOAuthButton.gameObject.SetActive(false);
            SetLoading(false);
            ShowError("로그인이 취소되었습니다.");
        }

        private void HandleLoginSuccess()
        {
            if (cancelOAuthButton != null) cancelOAuthButton.gameObject.SetActive(false);
            SetLoading(false);
            ShowAuctionPanel();
        }

        private void HandleLoginFailed(string errorMsg)
        {
            if (cancelOAuthButton != null) cancelOAuthButton.gameObject.SetActive(false);
            SetLoading(false);
            ShowError(errorMsg);
        }

        private void HandleLogoutSuccess()
        {
            ClearInputs();
            ClearError();
            SetLoading(false);
            if (cancelOAuthButton != null) cancelOAuthButton.gameObject.SetActive(false);
        }

        private void ClearInputs()
        {
            if (emailInput != null)    emailInput.text = "";
            if (passwordInput != null) passwordInput.text = "";
        }

        private void ShowAuctionPanel()
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (auctionPanel != null) auctionPanel.SetActive(true);
        }

        private void ShowError(string msg)
        {
            if (errorText != null)
            {
                errorText.text = msg;
                errorText.gameObject.SetActive(true);
            }
        }

        private void ClearError()
        {
            if (errorText != null) errorText.gameObject.SetActive(false);
        }

        private void SetLoading(bool loading)
        {
            loginButton.interactable = !loading;
            if (webAuthButton != null) webAuthButton.interactable = !loading;
            if (loadingIndicator != null) loadingIndicator.SetActive(loading);
        }

        private void CancelAutoLoginLoading()
        {
            if (!SupabaseManager.Instance.IsLoggedIn)
                SetLoading(false);
        }
    }
}
