using System.Collections;
using AuctionSystem.Models;
using AuctionSystem.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuctionSystem.UI
{
    public class DebugPanel : MonoBehaviour
    {
        [Header("Gold 충전 버튼")]
        [SerializeField] private Button addGold1000Button;
        [SerializeField] private Button addGold10000Button;
        [SerializeField] private Button addGold100000Button;

        [Header("표시")]
        [SerializeField] private TMP_Text currentGoldText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text userIdText;

        private bool _isBusy;

        void Start()
        {
            if (addGold1000Button   != null) addGold1000Button.onClick.AddListener(  () => OnAddGold(1_000));
            if (addGold10000Button  != null) addGold10000Button.onClick.AddListener( () => OnAddGold(10_000));
            if (addGold100000Button != null) addGold100000Button.onClick.AddListener(() => OnAddGold(100_000));

            SupabaseManager.Instance.OnProfileLoaded += RefreshDisplay;
            if (SupabaseManager.Instance.CurrentProfile != null)
                RefreshDisplay(SupabaseManager.Instance.CurrentProfile);

            ClearFeedback();
        }

        void OnDestroy()
        {
            if (SupabaseManager.Instance != null)
                SupabaseManager.Instance.OnProfileLoaded -= RefreshDisplay;
        }

        private void OnAddGold(long amount)
        {
            if (_isBusy) return;
            if (!SupabaseManager.Instance.IsLoggedIn)
            {
                ShowFeedback("로그인이 필요합니다.", false);
                return;
            }
            StartCoroutine(AddGoldCoroutine(amount));
        }

        private IEnumerator AddGoldCoroutine(long amount)
        {
            _isBusy = true;
            SetButtonsInteractable(false);
            ShowFeedback("처리 중...", true);

            string userId = SupabaseManager.Instance.Session.user.id;

            string url  = $"{SupabaseConfig.RestUrl}/profiles?id=eq.{userId}";
            long currentGold = SupabaseManager.Instance.CurrentProfile?.gold ?? 0;
            long newGold     = currentGold + amount;

            string body = $"{{\"gold\":{newGold}}}";

            bool ok    = false;
            string msg = "";

            yield return PatchRequest(url, body,
                onSuccess: _ =>
                {
                    ok  = true;
                    msg = $"+{amount:N0}G 충전 완료! 현재: {newGold:N0}G";
                },
                onError: err =>
                {
                    ok  = false;
                    msg = "오류: " + err;
                });

            if (ok)
            {
                StartCoroutine(SupabaseManager.Instance.LoadProfile());
            }

            ShowFeedback(msg, ok);
            _isBusy = false;
            SetButtonsInteractable(true);
        }

        private IEnumerator PatchRequest(string url, string body,
            System.Action<string> onSuccess, System.Action<string> onError)
        {
            byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
            using var req = new UnityEngine.Networking.UnityWebRequest(url, "PATCH");
            req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
            req.SetRequestHeader("Accept-Profile", "public");
            req.SetRequestHeader("Content-Profile", "public");
            req.SetRequestHeader("Prefer", "return=minimal");
            if (SupabaseManager.Instance.IsLoggedIn)
                req.SetRequestHeader("Authorization",
                    "Bearer " + SupabaseManager.Instance.Session.access_token);

            yield return req.SendWebRequest();

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                onSuccess?.Invoke(req.downloadHandler.text);
            else
                onError?.Invoke(req.downloadHandler.text ?? req.error);
        }

        private void RefreshDisplay(Profile profile)
        {
            if (currentGoldText != null)
                currentGoldText.text = $"Gold: {profile.gold:N0}G";
            if (userIdText != null)
                userIdText.text = $"[DEBUG] {profile.username ?? profile.id.Substring(0, 8)}";
        }

        private void ShowFeedback(string msg, bool isSuccess)
        {
            if (feedbackText == null) return;
            feedbackText.text  = msg;
            feedbackText.color = isSuccess ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
            feedbackText.gameObject.SetActive(true);
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 3f);
        }

        private void ClearFeedback()
        {
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (addGold1000Button   != null) addGold1000Button.interactable   = value;
            if (addGold10000Button  != null) addGold10000Button.interactable  = value;
            if (addGold100000Button != null) addGold100000Button.interactable = value;
        }

        private IEnumerator PostRequest(string url, string body, string prefer,
            System.Action<string> onSuccess, System.Action<string> onError)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);
            using var req = new UnityEngine.Networking.UnityWebRequest(url, "POST");
            req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(bytes);
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
            req.SetRequestHeader("Prefer", prefer);
            if (SupabaseManager.Instance.IsLoggedIn)
                req.SetRequestHeader("Authorization",
                    "Bearer " + SupabaseManager.Instance.Session.access_token);
            yield return req.SendWebRequest();
            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                onSuccess?.Invoke(req.downloadHandler.text);
            else
                onError?.Invoke(req.downloadHandler.text ?? req.error);
        }

    }
}
