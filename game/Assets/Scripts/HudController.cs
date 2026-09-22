using UnityEngine;
using UnityEngine.UI;

namespace DodgeRunner
{
    // uGUI Text でスコアと状態メッセージを表示する。
    public class HudController : MonoBehaviour
    {
        [SerializeField] Text scoreText;
        [SerializeField] Text messageText;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (scoreText != null) scoreText.text = $"TOKEN {Mathf.FloorToInt(gm.Score):D6}   {gm.Profile.Label}";
            if (messageText == null) return;
            messageText.text = gm.State switch
            {
                GameState.Ready => "FABLE 5.1\nDODGE RUNNER\n\nA / D or Left / Right : move    Space : jump\n\n"
                                   + $"Difficulty  [1] Haiku  [2] Sonnet  [3] Opus  [4] Fable 5.1\n> {gm.Profile.Label} <\n\nPress SPACE to start",
                GameState.GameOver => $"GAME OVER\n{gm.Profile.Label}  TOKEN {Mathf.FloorToInt(gm.Score)}\n\nPress R to retry",
                _ => "",
            };
        }
    }
}
