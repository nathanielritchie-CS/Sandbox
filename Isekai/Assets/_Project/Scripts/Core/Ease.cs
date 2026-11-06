using System;
using System.Collections;
using UnityEngine;

namespace Isekai.Core
{
    // Easing curves plus a coroutine tween. Tweens run on unscaled time unless told otherwise.
    public static class Ease
    {
        public static float OutCubic(float t)
        {
            var u = 1f - t;
            return 1f - u * u * u;
        }

        // Standard back-out with the classic 1.70158 overshoot (peaks ~1.1).
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            var u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        // Calls apply(ease(progress)) every frame for duration seconds, ending exactly on apply(1).
        public static IEnumerator Tween(float duration, Action<float> apply, Func<float, float> ease, bool unscaled = true)
        {
            for (var t = 0f; t < 1f; t += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / duration)
            {
                apply(ease(t));
                yield return null;
            }
            apply(1f);
        }
    }
}
