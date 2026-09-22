using System.Collections.Generic;
using UnityEngine;

namespace DodgeRunner
{
    // 一定距離ごとにランダムレーンへ障害物を置き、ゲーム速度で後方へ流す。通過分は破棄。
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] float laneWidth = 2.5f;
        [SerializeField] float spawnZ = 70f;
        [SerializeField] float despawnZ = -15f;
        [SerializeField] float spawnInterval = 11f;
        [SerializeField] float minInterval = 5f;
        [SerializeField] Material obstacleMaterial;

        readonly List<Transform> obstacles = new();
        float distanceSinceSpawn;

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;

            var dz = gm.Speed * Time.deltaTime;
            distanceSinceSpawn += dz;

            for (var i = obstacles.Count - 1; i >= 0; i--)
            {
                var o = obstacles[i];
                o.position += Vector3.back * dz;
                var passed = o.position.z < despawnZ;
                if (!passed) continue;
                obstacles.RemoveAt(i);
                Destroy(o.gameObject);
            }

            var interval = Mathf.Max(minInterval, spawnInterval - gm.Speed * 0.12f) * gm.Profile.IntervalScale;
            var shouldSpawn = distanceSinceSpawn >= interval;
            if (!shouldSpawn) return;
            distanceSinceSpawn = 0f;
            SpawnRow();
        }

        enum Kind { Low, Normal, Tall }

        // 難易度プロファイルに従い、全レーン封鎖（1 本だけ跳べる低ハードル）/ 2 レーン / 1 レーンを配分する。
        void SpawnRow()
        {
            var profile = GameManager.Instance.Profile;
            var roll = Random.value;
            var isWall = roll < profile.WallChance;
            var isDouble = roll < profile.DoubleChance;
            if (isWall)
            {
                var openLane = Random.Range(-1, 2);
                for (var lane = -1; lane <= 1; lane++)
                {
                    var kind = lane == openLane ? Kind.Low : (Random.value < 0.5f ? Kind.Tall : Kind.Normal);
                    Spawn(lane, kind);
                }
                return;
            }

            var blockCount = isDouble ? 2 : 1;
            var lanes = new List<int> { -1, 0, 1 };
            for (var i = 0; i < blockCount; i++)
            {
                var idx = Random.Range(0, lanes.Count);
                var lane = lanes[idx];
                lanes.RemoveAt(idx);
                Spawn(lane, RandomKind());
            }
        }

        static Kind RandomKind()
        {
            var r = Random.value;
            if (r < 0.3f) return Kind.Low;
            if (r < 0.7f) return Kind.Normal;
            return Kind.Tall;
        }

        void Spawn(int lane, Kind kind)
        {
            // Low: ジャンプで跳べる。Normal: ギリギリ跳べる。Tall: 横に避けるしかない。
            var height = kind switch { Kind.Low => 0.5f, Kind.Normal => 1.4f, _ => 4f };
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Obstacle_{kind}";
            go.tag = "Obstacle";
            go.transform.localScale = new Vector3(1.8f, height, kind == Kind.Low ? 0.6f : 1.6f);
            go.transform.position = new Vector3(lane * laneWidth, height / 2f, spawnZ);
            go.transform.SetParent(transform);
            if (obstacleMaterial != null) go.GetComponent<Renderer>().sharedMaterial = obstacleMaterial;
            var pulse = go.AddComponent<Pulse>();
            pulse.emission = kind switch
            {
                Kind.Low => new Color(1f, 0.6f, 0.1f) * 2f,
                Kind.Normal => new Color(1f, 0.15f, 0.3f) * 2f,
                _ => new Color(0.8f, 0.1f, 1f) * 2f,
            };
            obstacles.Add(go.transform);
        }
    }
}
