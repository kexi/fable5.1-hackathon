using UnityEngine;
using UnityEngine.InputSystem;

namespace DodgeRunner
{
    // 3 レーン移動とジャンプ。前進はワールド側（Track/Obstacle）が後方へ流すので、プレイヤーは X/Y のみ動く。
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float laneWidth = 2.5f;
        [SerializeField] float laneChangeSpeed = 18f;
        [SerializeField] float jumpVelocity = 9.5f;

        int lane = 0; // -1, 0, 1
        Rigidbody body;
        bool isGrounded = true;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var keyboard = Keyboard.current;
            var canControl = gm != null && gm.State == GameState.Playing && keyboard != null;
            if (!canControl) return;

            var pressedLeft = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
            var pressedRight = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
            var pressedJump = keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;

            if (pressedLeft) lane = Mathf.Max(-1, lane - 1);
            if (pressedRight) lane = Mathf.Min(1, lane + 1);
            if (pressedJump && isGrounded)
            {
                body.linearVelocity = new Vector3(body.linearVelocity.x, jumpVelocity, 0f);
                isGrounded = false;
            }
        }

        void FixedUpdate()
        {
            var targetX = lane * laneWidth;
            var pos = body.position;
            var newX = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * Time.fixedDeltaTime);
            body.MovePosition(new Vector3(newX, pos.y, pos.z));
        }

        void Explode()
        {
            var rend = GetComponentInChildren<Renderer>();
            var mat = rend != null ? rend.sharedMaterial : null;
            DeathBurst.Play(transform.position, new Color(0.3f, 1f, 1f), mat);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var t in GetComponentsInChildren<TrailRenderer>()) t.emitting = false;
        }

        void OnCollisionEnter(Collision collision)
        {
            var hitObstacle = collision.gameObject.CompareTag("Obstacle");
            if (hitObstacle)
            {
                var wasPlaying = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
                GameManager.Instance?.GameOver();
                if (wasPlaying) Explode();
                return;
            }
            var hitGround = collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f;
            if (hitGround) isGrounded = true;
        }
    }
}
