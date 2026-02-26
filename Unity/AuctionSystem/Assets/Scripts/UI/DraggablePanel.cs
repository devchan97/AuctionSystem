using UnityEngine;
using UnityEngine.EventSystems;

namespace AuctionSystem.UI
{
    public class DraggablePanel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private RectTransform _rt;
        private Canvas        _canvas;
        private Vector2       _dragOffset;

        void Awake()
        {
            _rt     = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt, e.position, e.pressEventCamera, out var local);
            _dragOffset = local;
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            if (_canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rt.parent as RectTransform,
                e.position,
                e.pressEventCamera,
                out var parentLocal);

            _rt.anchoredPosition = parentLocal - _dragOffset;
        }
    }
}
