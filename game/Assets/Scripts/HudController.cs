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
            if (scoreText != null) scoreText.text = $"SCORE {Mathf.FloorToInt(gm.Score):D6}";
            if (messageText == null) return;
            messageText.text = gm.State switch
            {
                GameState.Ready => "DODGE RUNNER\n\nA/D or ←/→ : move   Space : jump\n\nPress SPACE to start",
                GameState.GameOver => $"GAME OVER\nScore {Mathf.FloorToInt(gm.Score)}\n\nPress R to retry",
                _ => "",
            };
        }
    }
}
