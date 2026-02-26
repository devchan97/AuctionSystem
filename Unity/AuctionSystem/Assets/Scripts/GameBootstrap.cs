using AuctionSystem.Auction;
using AuctionSystem.Auth;
using AuctionSystem.Network;
using UnityEngine;

namespace AuctionSystem
{
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            // 창모드로 시작 (1280x720)
            if (!Screen.fullScreen)
            {
                Screen.SetResolution(1280, 720, false);
            }
            else
            {
                Screen.fullScreen = false;
                Screen.SetResolution(1280, 720, false);
            }

            EnsureManager<SupabaseManager>("SupabaseManager");
            EnsureManager<LoginManager>("LoginManager");
            EnsureManager<WebBridgeAuth>("WebBridgeAuth");
            EnsureManager<AuctionManager>("AuctionManager");
            EnsureManager<BidHandler>("BidHandler");
            EnsureManager<RealtimeManager>("RealtimeManager");
        }

        private static T EnsureManager<T>(string goName) where T : Component
        {
            var existing = Object.FindFirstObjectByType<T>();
            if (existing != null) return existing;

            var go = new GameObject(goName);
            DontDestroyOnLoad(go);
            return go.AddComponent<T>();
        }
    }
}
