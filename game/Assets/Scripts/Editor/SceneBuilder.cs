using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DodgeRunner.EditorTools
{
    // シーンを C# で再現可能に構築する。手作業で壊れても Build() の再実行で元に戻せる。
    public static class SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string MaterialDir = "Assets/Materials";
        const float LaneWidth = 2.5f;

        [MenuItem("DodgeRunner/Build Main Scene")]
        public static void Build()
        {
            EnsureTag("Obstacle");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var playerMat = MakeMaterial("Player", new Color(0.2f, 0.5f, 1f));
            var groundMat = MakeMaterial("Ground", new Color(0.18f, 0.18f, 0.2f));
            var obstacleMat = MakeMaterial("Obstacle", new Color(0.95f, 0.2f, 0.2f));
            var laneMat = MakeMaterial("Lane", new Color(0.35f, 0.35f, 0.4f));

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 6f, -10f);
            camGo.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;

            // Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Game manager
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();

            // Player
            var player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.5f, 0f);
            player.GetComponent<Renderer>().sharedMaterial = playerMat;
            var rb = player.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            player.AddComponent<PlayerController>();

            // Track (無限地面: 6 セグメント × 20m)
            var track = new GameObject("Track");
            track.AddComponent<TrackScroller>();
            const int segmentCount = 6;
            const float segmentLength = 20f;
            for (var i = 0; i < segmentCount; i++)
            {
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"Segment{i}";
                seg.transform.SetParent(track.transform);
                seg.transform.localScale = new Vector3(LaneWidth * 3f + 1f, 1f, segmentLength);
                seg.transform.position = new Vector3(0f, -0.5f, -20f + i * segmentLength);
                seg.GetComponent<Renderer>().sharedMaterial = groundMat;
                // レーン境界線（見た目のみ、コライダー無し）
                foreach (var lane in new[] { -3, -1, 1, 3 })
                {
                    var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    line.name = "LaneLine";
                    Object.DestroyImmediate(line.GetComponent<Collider>());
                    line.transform.SetParent(seg.transform);
                    line.transform.localPosition = new Vector3(lane * LaneWidth / 2f / (LaneWidth * 3f + 1f), 0.51f, 0f);
                    line.transform.localScale = new Vector3(0.08f / (LaneWidth * 3f + 1f), 0.02f, 1f);
                    line.GetComponent<Renderer>().sharedMaterial = laneMat;
                }
            }

            // Spawner
            var spawnerGo = new GameObject("ObstacleSpawner");
            var spawner = spawnerGo.AddComponent<ObstacleSpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("obstacleMaterial").objectReferenceValue = obstacleMat;
            so.ApplyModifiedPropertiesWithoutUndo();

            // HUD
            BuildHud();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneBuilder] Built {ScenePath}");
        }

        static void BuildHud()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var score = MakeText("ScoreText", canvasGo.transform, font, 36, TextAnchor.UpperLeft);
            var scoreRect = score.rectTransform;
            scoreRect.anchorMin = new Vector2(0f, 1f);
            scoreRect.anchorMax = new Vector2(0f, 1f);
            scoreRect.pivot = new Vector2(0f, 1f);
            scoreRect.anchoredPosition = new Vector2(24f, -24f);
            scoreRect.sizeDelta = new Vector2(500f, 60f);

            var message = MakeText("MessageText", canvasGo.transform, font, 40, TextAnchor.MiddleCenter);
            var msgRect = message.rectTransform;
            msgRect.anchorMin = new Vector2(0.5f, 0.5f);
            msgRect.anchorMax = new Vector2(0.5f, 0.5f);
            msgRect.pivot = new Vector2(0.5f, 0.5f);
            msgRect.anchoredPosition = Vector2.zero;
            msgRect.sizeDelta = new Vector2(1000f, 400f);

            var hud = canvasGo.AddComponent<HudController>();
            var so = new SerializedObject(hud);
            so.FindProperty("scoreText").objectReferenceValue = score;
            so.FindProperty("messageText").objectReferenceValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Text MakeText(string name, Transform parent, Font font, int size, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        static Material MakeMaterial(string name, Color color)
        {
            if (!AssetDatabase.IsValidFolder(MaterialDir)) AssetDatabase.CreateFolder("Assets", "Materials");
            var path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureTag(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = tagManager.FindProperty("tags");
            for (var i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            }
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
