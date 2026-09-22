using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DodgeRunner
{
    public enum GameState { Ready, Playing, GameOver }

    // 難易度。Fable 5.1 が最難（モデル階層と同じ順）。
    public enum Difficulty { Haiku, Sonnet, Opus, Fable51 }

    public readonly struct DifficultyProfile
    {
        public readonly string Label;
        public readonly float StartSpeed;
        public readonly float Acceleration;
        public readonly float MaxSpeed;
        public readonly float WallChance;   // 全レーン封鎖の確率
        public readonly float DoubleChance; // 2 レーン封鎖の累積確率（WallChance を含む）
        public readonly float IntervalScale; // 障害物間隔の倍率（小さいほど密）

        public DifficultyProfile(string label, float startSpeed, float acceleration, float maxSpeed, float wallChance, float doubleChance, float intervalScale)
        {
            Label = label; StartSpeed = startSpeed; Acceleration = acceleration; MaxSpeed = maxSpeed;
            WallChance = wallChance; DoubleChance = doubleChance; IntervalScale = intervalScale;
        }

        public static DifficultyProfile Of(Difficulty d) => d switch
        {
            Difficulty.Haiku => new DifficultyProfile("HAIKU 4.5", 9f, 0.3f, 24f, 0.05f, 0.35f, 1.4f),
            Difficulty.Sonnet => new DifficultyProfile("SONNET 5", 12f, 0.6f, 34f, 0.15f, 0.55f, 1.15f),
            Difficulty.Opus => new DifficultyProfile("OPUS 5", 14f, 0.9f, 45f, 0.25f, 0.70f, 1.0f),
            _ => new DifficultyProfile("FABLE 5.1", 16f, 1.3f, 55f, 0.35f, 0.80f, 0.85f),
        };
    }

    // ゲーム全体の状態機械。速度・スコアの単一ソースとして他コンポーネントが参照する。
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // シーン再ロード後も選択を保つため static に置く
        public static Difficulty SelectedDifficulty = Difficulty.Fable51;
        public DifficultyProfile Profile => DifficultyProfile.Of(SelectedDifficulty);

        public GameState State { get; private set; } = GameState.Ready;
        public float Speed { get; private set; }
        public float Score { get; private set; }
        public float Distance { get; private set; }

        void Awake()
        {
            Instance = this;
            Speed = 0f;
            Application.targetFrameRate = 60;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var isReady = State == GameState.Ready;
            var isGameOver = State == GameState.GameOver;
            var pressedStart = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
            var pressedRetry = keyboard.rKey.wasPressedThisFrame || pressedStart;

            if (isReady)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) SelectedDifficulty = Difficulty.Haiku;
                if (keyboard.digit2Key.wasPressedThisFrame) SelectedDifficulty = Difficulty.Sonnet;
                if (keyboard.digit3Key.wasPressedThisFrame) SelectedDifficulty = Difficulty.Opus;
                if (keyboard.digit4Key.wasPressedThisFrame) SelectedDifficulty = Difficulty.Fable51;
            }
            if (isReady && pressedStart)
            {
                StartGame();
                return;
            }
            if (isGameOver && pressedRetry)
            {
                Retry();
                return;
            }
            if (State != GameState.Playing) return;

            var profile = Profile;
            Speed = Mathf.Min(profile.MaxSpeed, Speed + profile.Acceleration * Time.deltaTime);
            Distance += Speed * Time.deltaTime;
            Score = Distance;
        }

        public void StartGame()
        {
            State = GameState.Playing;
            Speed = Profile.StartSpeed;
            Debug.Log($"{{\"event\":\"game_start\",\"difficulty\":\"{SelectedDifficulty}\"}}");
        }

        public void GameOver()
        {
            if (State != GameState.Playing) return;
            State = GameState.GameOver;
            Speed = 0f;
            Debug.Log($"{{\"event\":\"game_over\",\"difficulty\":\"{SelectedDifficulty}\",\"score\":{Mathf.FloorToInt(Score)}}}");
        }

        public void Retry()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
