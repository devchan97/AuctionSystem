using AuctionSystem.Network;
using UnityEngine;

namespace AuctionSystem.Auth
{
    public class WebBridgeAuth : MonoBehaviour
    {
        public void OpenSignupForUnity()
        {
            Application.OpenURL(SupabaseConfig.WebSignupUrl + "?from=unity");
        }
    }
}
