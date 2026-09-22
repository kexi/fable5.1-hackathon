using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DodgeRunner
{
    public enum GameState { Ready, Playing, GameOver }

    // ゲーム全体の状態機械。速度・スコアの単一ソースとして他コンポーネントが参照する。
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] float startSpeed = 8f;
        [SerializeField] float acceleration = 0.35f;
        [SerializeField] float maxSpeed = 30f;

        public GameState State { get; private set; } = GameState.Ready;
        public float Speed { get; private set; }
        public float Score { get; private set; }
        public float Distance { get; private set; }

        void Awake()
        {
            Instance = this;
            Speed = 0f;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var isReady = State == GameState.Ready;
            var isGameOver = State == GameState.GameOver;
            var pressedStart = keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
            var pressedRetry = keyboard.rKey.wasPressedThisFrame || pressedStart;

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

            Speed = Mathf.Min(maxSpeed, Speed + acceleration * Time.deltaTime);
            Distance += Speed * Time.deltaTime;
            Score = Distance;
        }

        public void StartGame()
        {
            State = GameState.Playing;
            Speed = startSpeed;
            Debug.Log("{\"event\":\"game_start\"}");
        }

        public void GameOver()
        {
            if (State != GameState.Playing) return;
            State = GameState.GameOver;
            Speed = 0f;
            Debug.Log($"{{\"event\":\"game_over\",\"score\":{Mathf.FloorToInt(Score)}}}");
        }

        public void Retry()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
