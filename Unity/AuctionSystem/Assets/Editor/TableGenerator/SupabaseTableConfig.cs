using UnityEngine;

namespace AuctionSystem.Editor.TableGenerator
{
    [CreateAssetMenu(
        menuName = "Auction/Supabase Table Config",
        fileName = "SupabaseTableConfig")]
    public class SupabaseTableConfig : ScriptableObject
    {
        [Header("Supabase 프로젝트")]
        public string supabaseUrl = "https://YOUR_PROJECT_REF.supabase.co";

        [Header("Service Role Key (관리자 전용 — 절대 공개 금지)")]
        [TextArea(2, 4)]
        public string serviceRoleKey = "";

        public bool IsValid =>
            !string.IsNullOrEmpty(supabaseUrl) &&
            !string.IsNullOrEmpty(serviceRoleKey) &&
            supabaseUrl.StartsWith("https://");
    }
}
