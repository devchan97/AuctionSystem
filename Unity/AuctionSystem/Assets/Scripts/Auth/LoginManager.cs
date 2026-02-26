using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AuctionSystem.Models;
using AuctionSystem.Network;
using AuctionSystem.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace AuctionSystem.Auth
{
    public class LoginManager : MonoBehaviour
    {
        public static LoginManager Instance { get; private set; }

        public event Action OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action OnLogoutSuccess;

        private string _pendingOAuthToken;
        private readonly object _oauthLock = new object();
        private volatile bool _oauthServerRunning;
        private HttpListener _oauthListener;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            // PlayMode 종료 시 OAuth 서버 스레드를 깨끗이 종료
            StopOAuthCallbackServer();
        }

        void Update()
        {
            string pending = null;
            lock (_oauthLock)
            {
                if (_pendingOAuthToken != null)
                {
                    pending = _pendingOAuthToken;
                    _pendingOAuthToken = null;
                }
            }
            if (pending != null)
            {
                int sep = pending.IndexOf('|');
                string accessToken  = sep >= 0 ? pending.Substring(0, sep) : pending;
                string refreshToken = sep >= 0 ? pending.Substring(sep + 1) : null;
                StartCoroutine(ExchangeOAuthToken(accessToken, refreshToken));
            }
        }

        public void SignInWithEmail(string email, string password)
        {
            StartCoroutine(SignInCoroutine(email, password));
        }

        private IEnumerator SignInCoroutine(string email, string password)
        {
            string url = $"{SupabaseConfig.AuthUrl}/token?grant_type=password";
            string json = $"{{\"email\":\"{email}\",\"password\":\"{password}\"}}";

            byte[] bodyBytes = Encoding.UTF8.GetBytes(json);
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", SupabaseConfig.AnonKey);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var session = JsonUtility.FromJson<UserSession>(req.downloadHandler.text);
                SupabaseManager.Instance.SetSession(session);
                OnLoginSuccess?.Invoke();
            }
            else
            {
                string errorMsg = AuctionUtils.ParseError(req.downloadHandler.text, "이메일 또는 비밀번호가 올바르지 않습니다.");
                OnLoginFailed?.Invoke(errorMsg);
            }
        }

        public void StartOAuthCallbackServer()
        {
            StopOAuthCallbackServer();
            _oauthServerRunning = true;
            var thread = new Thread(OAuthCallbackServerThread) { IsBackground = true };
            thread.Start();
            Debug.Log("[LoginManager] OAuth 콜백 서버 시작 (포트: " + SupabaseConfig.OAuthCallbackPort + ")");
        }

        public void StopOAuthCallbackServer()
        {
            _oauthServerRunning = false;
            try { _oauthListener?.Stop(); } catch { }
            _oauthListener = null;
        }

        private void OAuthCallbackServerThread()
        {
            _oauthListener = new HttpListener();
            _oauthListener.Prefixes.Add($"http://localhost:{SupabaseConfig.OAuthCallbackPort}/");
            try
            {
                _oauthListener.Start();

                // GetContext()는 블로킹 — StopOAuthCallbackServer()에서 listener.Stop() 호출 시
                // HttpListenerException이 발생하며 즉시 탈출
                var context  = _oauthListener.GetContext();
                var request  = context.Request;
                var response = context.Response;

                string html = "<html><body style='font-family:sans-serif;text-align:center;padding-top:80px'>" +
                              "<h2>로그인 완료!</h2><p>Unity로 돌아가세요.</p></body></html>";
                byte[] htmlBytes = Encoding.UTF8.GetBytes(html);
                response.ContentLength64 = htmlBytes.Length;
                response.OutputStream.Write(htmlBytes, 0, htmlBytes.Length);
                response.OutputStream.Close();

                // request.Url.Query는 + 를 공백으로 변환하는 버그가 있어 RawUrl 사용
                string rawQuery = request.RawUrl ?? "";
                int qIdx = rawQuery.IndexOf('?');
                string query = qIdx >= 0 ? rawQuery.Substring(qIdx) : "";
                string accessToken  = ExtractParam(query, "access_token");
                string refreshToken = ExtractParam(query, "refresh_token");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    string payload = string.IsNullOrEmpty(refreshToken)
                        ? accessToken
                        : accessToken + "|" + refreshToken;
                    lock (_oauthLock) { _pendingOAuthToken = payload; }
                }
                else
                {
                    Debug.LogWarning("[LoginManager] OAuth 콜백에서 access_token을 찾지 못했습니다. " +
                                     "Supabase Auth 설정에서 Implicit Flow를 사용하는지 확인하세요.");
                    lock (_oauthLock) { _pendingOAuthToken = "__error__no_token"; }
                }
            }
            catch (HttpListenerException) when (!_oauthServerRunning)
            {
                // listener.Stop() 호출로 인한 정상 종료 — 오류 로그 억제
                Debug.Log("[LoginManager] OAuth 서버 정상 종료");
            }
            catch (Exception e)
            {
                if (_oauthServerRunning)
                    Debug.LogError("[LoginManager] OAuth 서버 오류: " + e.Message);
            }
            finally
            {
                try { _oauthListener?.Stop(); } catch { }
                _oauthListener = null;
                _oauthServerRunning = false;
            }
        }

        private static string ExtractParam(string query, string key)
        {
            // query: ?access_token=xxx&refresh_token=yyy&...
            var match = Regex.Match(query, $@"[?&]{key}=([^&]+)");
            return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
        }

        // refresh_token이 있으면 Supabase HS256 세션으로 교환 (Google ES256은 Edge Function에서 오류)
        private IEnumerator ExchangeOAuthToken(string accessToken, string refreshToken = null)
        {
            if (accessToken == "__error__timeout")
            {
                OnLoginFailed?.Invoke("회원가입 창이 닫혔거나 응답이 없어 로그인이 취소되었습니다.");
                yield break;
            }

            if (accessToken == "__error__no_token")
            {
                OnLoginFailed?.Invoke("Google 로그인 실패: 토큰을 받지 못했습니다.");
                yield break;
            }

            if (!string.IsNullOrEmpty(refreshToken))
            {
                string refreshUrl  = $"{SupabaseConfig.AuthUrl}/token?grant_type=refresh_token";
                string refreshBody = $"{{\"refresh_token\":\"{refreshToken}\"}}";
                byte[] refreshBytes = Encoding.UTF8.GetBytes(refreshBody);

                using var refreshReq = new UnityWebRequest(refreshUrl, "POST");
                refreshReq.uploadHandler   = new UploadHandlerRaw(refreshBytes);
                refreshReq.downloadHandler = new DownloadHandlerBuffer();
                refreshReq.SetRequestHeader("Content-Type", "application/json");
                refreshReq.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
                yield return refreshReq.SendWebRequest();

                if (refreshReq.result == UnityWebRequest.Result.Success)
                {
                    var newSession = JsonUtility.FromJson<UserSession>(refreshReq.downloadHandler.text);
                    if (newSession != null && !string.IsNullOrEmpty(newSession.access_token))
                    {
                        Debug.Log("[LoginManager] OAuth → Supabase HS256 세션 교환 성공");
                        SupabaseManager.Instance.SetSession(newSession);
                        OnLoginSuccess?.Invoke();
                        yield break;
                    }
                }
                // refresh_token 교환 실패 — ES256 토큰은 Edge Function에서 Invalid JWT 오류 발생
                // 재로그인 요구로 처리 (ES256 토큰 그대로 저장하지 않음)
                string respText = refreshReq.downloadHandler.text;
                Debug.LogWarning("[LoginManager] refresh_token 교환 실패 — 재로그인 필요\n" + respText);
                OnLoginFailed?.Invoke("Google 로그인 세션 교환 실패. 다시 로그인해 주세요.\n" + respText);
                yield break;
            }

            // refresh_token이 없는 경우 (이메일 로그인 폴백 — HS256 토큰이므로 안전)
            string userUrl = $"{SupabaseConfig.AuthUrl}/user";
            using var userReq = UnityWebRequest.Get(userUrl);
            userReq.SetRequestHeader("apikey", SupabaseConfig.AnonKey);
            userReq.SetRequestHeader("Authorization", "Bearer " + accessToken);
            yield return userReq.SendWebRequest();

            if (userReq.result == UnityWebRequest.Result.Success)
            {
                var userInfo = JsonUtility.FromJson<UserInfo>(userReq.downloadHandler.text);
                var session  = new UserSession
                {
                    access_token  = accessToken,
                    refresh_token = refreshToken,
                    token_type    = "bearer",
                    user          = userInfo,
                };
                SupabaseManager.Instance.SetSession(session);
                OnLoginSuccess?.Invoke();
            }
            else
            {
                OnLoginFailed?.Invoke("Google 로그인 세션 교환 실패: " + userReq.downloadHandler.text);
            }
        }

        public void SignOut()
        {
            StartCoroutine(SignOutCoroutine());
        }

        private IEnumerator SignOutCoroutine()
        {
            if (SupabaseManager.Instance.IsLoggedIn)
            {
                string url = $"{SupabaseConfig.AuthUrl}/logout";
                yield return SupabaseManager.Instance.Post(url, "{}", null, null);
            }
            SupabaseManager.Instance.ClearSession();
            OnLogoutSuccess?.Invoke();
        }

    }
}
