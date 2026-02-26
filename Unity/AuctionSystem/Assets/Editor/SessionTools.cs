using UnityEditor;
using UnityEngine;

namespace AuctionSystem.Editor
{
    public static class SessionTools
    {
        private const string SessionKey = "supabase_session";

        [MenuItem("Tools/Auction/Clear Saved Session")]
        public static void ClearSavedSession()
        {
            if (PlayerPrefs.HasKey(SessionKey))
            {
                PlayerPrefs.DeleteKey(SessionKey);
                PlayerPrefs.Save();
                Debug.Log("[SessionTools] 저장된 Supabase 세션을 삭제했습니다. 이제 Play Mode에서 새로 로그인하세요.");
            }
            else
            {
                Debug.Log("[SessionTools] 저장된 세션 없음.");
            }
        }
    }
}
