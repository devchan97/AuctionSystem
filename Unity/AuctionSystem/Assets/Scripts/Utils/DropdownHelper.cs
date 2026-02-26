using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AuctionSystem.Utils
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class DropdownHelper : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Dropdown _drop;

        void Awake()
        {
            _drop = GetComponent<TMP_Dropdown>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            StartCoroutine(ResetScrollNextFrame());
        }

        private IEnumerator ResetScrollNextFrame()
        {
            // 1프레임: TMP_Dropdown.Show() 실행 완료
            yield return null;
            // 2프레임: Toggle.Select()로 인한 ScrollRect 자동 스크롤 완료 후 덮어씀
            yield return null;

            if (_drop == null || _drop.template == null) yield break;

            var parent = _drop.template.parent;
            if (parent == null) yield break;

            var listTransform = parent.Find("Dropdown List");
            if (listTransform == null) yield break;

            var sr = listTransform.GetComponent<ScrollRect>();
            if (sr == null) yield break;

            // TMP_Dropdown은 item을 bottom-up으로 배치:
            // item 0 → 가장 높은 y (Content 최상단)
            // normalizedPosition=1 → ScrollRect가 Content 최상단을 표시 → item 0이 맨 위
            sr.verticalNormalizedPosition = 1f;
        }
    }
}
