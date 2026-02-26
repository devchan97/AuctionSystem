using System.Collections;
using TMPro;
using UnityEngine;

namespace AuctionSystem.UI
{
    public static class UIAnimator
    {
        public static IEnumerator FeedbackRoutine(
            TMP_Text text, string msg, Color color,
            float holdTime = 2.0f, float fadeTime = 0.6f)
        {
            if (text == null) yield break;

            text.text  = msg;
            text.color = color;
            var c = color;
            c.a = 1f;
            text.color = c;
            text.gameObject.SetActive(true);

            yield return new WaitForSeconds(holdTime);

            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                text.color = c;
                yield return null;
            }

            text.gameObject.SetActive(false);
            c.a = 1f;
            text.color = c;
        }

        public static IEnumerator SpinLoop(Transform spinTarget, float degreesPerSecond = 360f)
        {
            if (spinTarget == null) yield break;
            while (spinTarget != null && spinTarget.gameObject.activeInHierarchy)
            {
                spinTarget.Rotate(0f, 0f, -degreesPerSecond * Time.deltaTime);
                yield return null;
            }
        }
    }
}
