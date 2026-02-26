using UnityEngine;

namespace AuctionSystem.UI
{
    public class LoadingSpinner : MonoBehaviour
    {
        [SerializeField] private Transform spinTarget;
        [SerializeField] private float degreesPerSecond = 360f;

        private Coroutine _routine;

        void OnEnable()
        {
            var target = spinTarget != null ? spinTarget : transform;
            _routine = StartCoroutine(UIAnimator.SpinLoop(target, degreesPerSecond));
        }

        void OnDisable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            var target = spinTarget != null ? spinTarget : transform;
            target.localRotation = Quaternion.identity;
        }
    }
}
