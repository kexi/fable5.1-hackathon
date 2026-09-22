using UnityEngine;
using UnityEngine.InputSystem;

namespace DodgeRunner
{
    // タッチ操作をスワイプ / タップに解釈する。GameManager と PlayerController の両方から同じフレームで読めるよう、
    // フレーム番号でキャッシュした静的クラスにしている（シーンにコンポーネントを置かずに済ませるため）。
    public static class TouchInput
    {
        // スワイプ判定の閾値（画面の短辺に対する割合）。DPI が違う端末でも同じ指の動きで反応させる。
        const float SwipeRatio = 0.06f;
        // 指を離すまでの時間がこれより長ければタップとみなさない（長押しの誤爆を避ける）
        const float TapMaxSeconds = 0.4f;

        public static bool SwipedLeft { get; private set; }
        public static bool SwipedRight { get; private set; }
        public static bool SwipedUp { get; private set; }
        public static bool Tapped { get; private set; }

        static int polledFrame = -1;
        static bool isTracking;
        static bool isConsumed; // このタッチで既にスワイプを発火したか
        static Vector2 startPos;
        static float startTime;

        public static bool IsAvailable => Touchscreen.current != null;

        public static void Poll()
        {
            var alreadyPolled = polledFrame == Time.frameCount;
            if (alreadyPolled) return;
            polledFrame = Time.frameCount;
            SwipedLeft = SwipedRight = SwipedUp = Tapped = false;

            var touch = Touchscreen.current?.primaryTouch;
            if (touch == null) return;

            var pos = touch.position.ReadValue();
            if (touch.press.wasPressedThisFrame)
            {
                isTracking = true;
                isConsumed = false;
                startPos = pos;
                startTime = Time.unscaledTime;
                return;
            }
            if (!isTracking) return;

            var delta = pos - startPos;
            var threshold = Mathf.Min(Screen.width, Screen.height) * SwipeRatio;
            var isSwipe = !isConsumed && delta.magnitude >= threshold;
            if (isSwipe)
            {
                // 指を離す前に発火させる（レーン移動の反応を早くするため）
                isConsumed = true;
                var isHorizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
                if (isHorizontal && delta.x < 0f) SwipedLeft = true;
                else if (isHorizontal) SwipedRight = true;
                else if (delta.y > 0f) SwipedUp = true;
            }

            var released = touch.press.wasReleasedThisFrame || !touch.press.isPressed;
            if (!released) return;
            isTracking = false;
            var isTap = !isConsumed && Time.unscaledTime - startTime <= TapMaxSeconds;
            if (isTap) Tapped = true;
        }
    }
}
