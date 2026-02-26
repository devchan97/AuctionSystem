namespace AuctionSystem.Network
{
    public static class SupabaseConfig
    {
        public const string Url     = "https://<your-project-ref>.supabase.co";
        public const string AnonKey = "<your-anon-key>";

        // 웹 사이트 URL (회원가입 버튼 클릭 시 이동)
        public const string WebSignupUrl = "https://auction-system-7xkxhuke8-chanwoolims-projects.vercel.app/signup";

        // Google OAuth 콜백을 받을 로컬 포트 (PC 빌드용)
        // 빌드에서는 사용 안 함
        public const int OAuthCallbackPort = 7654;

        // OAuth 콜백 redirect_uri (Supabase Dashboard > Auth > URL Configuration에 등록 필요)
        public static string OAuthRedirectUri => $"http://localhost:{OAuthCallbackPort}/callback";

        // REST API 엔드포인트
        public static string RestUrl => Url + "/rest/v1";
        public static string AuthUrl => Url + "/auth/v1";
        public static string FunctionsUrl => Url + "/functions/v1";
        public static string RealtimeUrl => Url.Replace("https://", "wss://") + "/realtime/v1/websocket";
    }
}
