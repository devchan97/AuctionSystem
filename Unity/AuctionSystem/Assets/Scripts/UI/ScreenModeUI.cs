using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuctionSystem.UI
{
    public class ScreenModeUI : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private TMP_Text buttonLabel;

        private static readonly int WindowedWidth  = 1280;
        private static readonly int WindowedHeight = 720;

        void Start()
        {
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleScreenMode);
            UpdateLabel();
        }

        private void ToggleScreenMode()
        {
            if (Screen.fullScreen)
            {
                Screen.SetResolution(WindowedWidth, WindowedHeight, false);
            }
            else
            {
                Screen.SetResolution(
                    Display.main.systemWidth,
                    Display.main.systemHeight,
                    true);
            }
            // 한 프레임 후 라벨 갱신 (fullScreen 값 반영 딜레이)
            Invoke(nameof(UpdateLabel), 0.1f);
        }

        private void UpdateLabel()
        {
            if (buttonLabel != null)
                buttonLabel.text = Screen.fullScreen ? "창모드" : "전체화면";
        }
    }
}
