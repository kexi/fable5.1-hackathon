using UnityEngine;
using UnityEngine.UI;

namespace DodgeRunner
{
    // uGUI Text でスコアと状態メッセージを表示する。
    public class HudController : MonoBehaviour
    {
        [SerializeField] Text scoreText;
        [SerializeField] Text messageText;

        CanvasScaler scaler;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
        }

        void Update()
        {
            FitOrientation();
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (scoreText != null) scoreText.text = $"TOKEN {Mathf.FloorToInt(gm.Score):D6}   {gm.Profile.Label}";
            if (messageText == null) return;
            // タッチ端末では操作説明をスワイプ / タップに置き換える
            var isTouch = TouchInput.IsAvailable && Application.isMobilePlatform;
            messageText.text = gm.State switch
            {
                GameState.Ready when isTouch => "FABLE 5.1\nDODGE RUNNER\n\nSwipe Left / Right : move\nTap or Swipe Up : jump\n\n"
                                                + $"Difficulty  < swipe >\n> {gm.Profile.Label} <\n\nTap to start",
                GameState.Ready => "FABLE 5.1\nDODGE RUNNER\n\nA / D or Left / Right : move    Space : jump\n\n"
                                   + $"Difficulty  [1] Haiku  [2] Sonnet  [3] Opus  [4] Fable 5.1\n> {gm.Profile.Label} <\n\nPress SPACE to start",
                GameState.GameOver when isTouch => $"GAME OVER\n{gm.Profile.Label}  TOKEN {Mathf.FloorToInt(gm.Score)}\n\nTap to retry",
                GameState.GameOver => $"GAME OVER\n{gm.Profile.Label}  TOKEN {Mathf.FloorToInt(gm.Score)}\n\nPress R to retry",
                _ => "",
            };
        }

        // 縦持ちでは基準解像度を縦長に入れ替え、文字が極端に小さくならないようにする。
        void FitOrientation()
        {
            if (scaler == null) return;
            var isPortrait = Screen.height > Screen.width;
            var wanted = isPortrait ? new Vector2(720f, 1280f) : new Vector2(1280f, 720f);
            if (scaler.referenceResolution != wanted) scaler.referenceResolution = wanted;
            // 縦持ちでは画面中央に機体が来るので、メッセージを上に逃がして重ならないようにする
            if (messageText == null) return;
            var offset = isPortrait ? new Vector2(0f, 200f) : Vector2.zero;
            if (messageText.rectTransform.anchoredPosition != offset) messageText.rectTransform.anchoredPosition = offset;
        }
    }
}
