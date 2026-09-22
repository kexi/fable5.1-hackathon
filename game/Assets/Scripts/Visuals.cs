using System.Collections.Generic;
using UnityEngine;

namespace DodgeRunner
{
    // 両脇に流れるネオン・スカイライン（ランダムな高さの発光ビル）。
    public class Skyline : MonoBehaviour
    {
        [SerializeField] int countPerSide = 12;
        [SerializeField] float spacing = 13f;
        [SerializeField] float sideX = 11f;
        [SerializeField] float recycleZ = -40f;
        [SerializeField] Material material;

        readonly List<Transform> blocks = new();

        void Start()
        {
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < countPerSide; i++)
                {
                    var z = -30f + i * spacing;
                    blocks.Add(MakeBlock(side, z));
                }
            }
        }

        Transform MakeBlock(int side, float z)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Building";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform);
            Randomize(go.transform, side, z);
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        static void Randomize(Transform t, int side, float z)
        {
            var h = Random.Range(3f, 16f);
            var w = Random.Range(2f, 6f);
            t.localScale = new Vector3(w, h, Random.Range(2f, 6f));
            t.position = new Vector3(side * (11f + Random.Range(0f, 10f)), h / 2f - 0.5f, z);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var dz = gm.Speed * Time.deltaTime * 0.9f; // わずかに遅く流して奥行き感を出す
            var maxZ = float.MinValue;
            foreach (var b in blocks) maxZ = Mathf.Max(maxZ, b.position.z);
            foreach (var b in blocks)
            {
                b.position += Vector3.back * dz;
                var passed = b.position.z < recycleZ;
                if (!passed) continue;
                var side = b.position.x < 0 ? -1 : 1;
                Randomize(b, side, maxZ + spacing);
                maxZ = b.position.z;
            }
        }
    }

    // カメラをプレイヤーの横位置へ緩く追従させ、速度で FOV を広げて疾走感を出す。
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float followRatio = 0.35f;
        [SerializeField] float baseFov = 60f;
        [SerializeField] float fovPerSpeed = 0.5f;

        Camera cam;
        Vector3 basePos;

        void Awake()
        {
            cam = GetComponent<Camera>();
            basePos = transform.position;
        }

        void LateUpdate()
        {
            var gm = GameManager.Instance;
            var hasTarget = target != null && gm != null;
            if (!hasTarget) return;
            var wantedX = target.position.x * followRatio;
            var p = transform.position;
            p.x = Mathf.Lerp(p.x, wantedX, Time.deltaTime * 6f);
            transform.position = p;
            if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + gm.Speed * fovPerSpeed, Time.deltaTime * 3f);
        }
    }

    // 障害物の発光を脈動させ、危険を視覚的に伝える。
    public class Pulse : MonoBehaviour
    {
        public Color emission = Color.red;
        Renderer rend;
        MaterialPropertyBlock block;
        float phase;

        void Awake()
        {
            rend = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            var k = 0.7f + 0.5f * Mathf.Sin(Time.time * 6f + phase);
            rend.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", emission * k);
            block.SetColor("_BaseColor", emission * 0.6f);
            rend.SetPropertyBlock(block);
        }
    }

    // 死亡時の破片バースト。ParticleSystem を実行時に組み立てるので Prefab 不要。
    public static class DeathBurst
    {
        public static void Play(Vector3 position, Color color, Material material)
        {
            var go = new GameObject("DeathBurst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startColor = color;
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            renderer.sharedMaterial = material;
            ps.Play();
            Object.Destroy(go, 2.5f);
        }
    }
}
