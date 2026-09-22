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
        [SerializeField] float spawnInterval = 12f;
        [SerializeField] float minInterval = 6f;
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

            var interval = Mathf.Max(minInterval, spawnInterval - gm.Speed * 0.15f);
            var shouldSpawn = distanceSinceSpawn >= interval;
            if (!shouldSpawn) return;
            distanceSinceSpawn = 0f;
            SpawnRow();
        }

        void SpawnRow()
        {
            // 必ず 1 レーン以上空ける: 塞ぐレーン数は 1〜2
            var blockCount = Random.value < 0.35f ? 2 : 1;
            var lanes = new List<int> { -1, 0, 1 };
            for (var i = 0; i < blockCount; i++)
            {
                var idx = Random.Range(0, lanes.Count);
                var lane = lanes[idx];
                lanes.RemoveAt(idx);
                Spawn(lane);
            }
        }

        void Spawn(int lane)
        {
            var isTall = Random.value < 0.3f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Obstacle";
            go.tag = "Obstacle";
            var height = isTall ? 2.5f : 1f;
            go.transform.localScale = new Vector3(1.6f, height, 1.6f);
            go.transform.position = new Vector3(lane * laneWidth, height / 2f, spawnZ);
            go.transform.SetParent(transform);
            if (obstacleMaterial != null) go.GetComponent<Renderer>().sharedMaterial = obstacleMaterial;
            obstacles.Add(go.transform);
        }
    }
}
