using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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

            var playerMat = MakeMaterial("Player", new Color(0.1f, 0.6f, 0.9f), new Color(0.2f, 1.2f, 1.6f));
            var groundMat = MakeMaterial("Ground", new Color(0.04f, 0.03f, 0.08f), Color.black, MakeGridTexture());
            var obstacleMat = MakeMaterial("Obstacle", new Color(0.6f, 0.1f, 0.2f), new Color(2f, 0.3f, 0.6f));
            var laneMat = MakeMaterial("Lane", new Color(0.2f, 0.9f, 1f), new Color(0.3f, 2.5f, 3f));
            var buildingMat = MakeMaterial("Building", new Color(0.05f, 0.02f, 0.1f), new Color(0.6f, 0.1f, 1.2f) * 0.35f);

            // 背景・フォグ（ネオン夜景）
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.2f, 0.4f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.05f, 0.02f, 0.12f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 95f;

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 6f, -10f);
            camGo.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.02f, 0.12f);
            cam.fieldOfView = 60f;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;
            var rig = camGo.AddComponent<CameraRig>();

            // Global Volume（Bloom + Vignette）
            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = MakeVolumeProfile();

            // Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.None; // WebGL の負荷を抑える
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Game manager
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();

            // Player
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.5f, 0f);
            var playerCol = player.AddComponent<BoxCollider>();
            playerCol.size = new Vector3(1.2f, 0.7f, 1.6f);
            BuildShip(player.transform, playerMat, laneMat);
            var rigSo = new SerializedObject(rig);
            rigSo.FindProperty("target").objectReferenceValue = player.transform;
            rigSo.ApplyModifiedPropertiesWithoutUndo();
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

            // Skyline
            var skylineGo = new GameObject("Skyline");
            var skyline = skylineGo.AddComponent<Skyline>();
            var skySo = new SerializedObject(skyline);
            skySo.FindProperty("material").objectReferenceValue = buildingMat;
            skySo.ApplyModifiedPropertiesWithoutUndo();

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
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        // 船体 + 翼 + コックピット + エンジン発光 + 翼端トレイル。すべてプリミティブ。
        static void BuildShip(Transform root, Material bodyMat, Material glowMat)
        {
            GameObject Part(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null)
            {
                var go = GameObject.CreatePrimitive(type);
                go.name = name;
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(root, false);
                go.transform.localPosition = pos;
                go.transform.localScale = scale;
                if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                return go;
            }

            Part(PrimitiveType.Cube, "Hull", new Vector3(0f, 0f, 0f), new Vector3(0.7f, 0.35f, 1.6f), bodyMat);
            Part(PrimitiveType.Cube, "Nose", new Vector3(0f, 0f, 0.95f), new Vector3(0.35f, 0.2f, 0.5f), bodyMat, new Vector3(0f, 45f, 0f));
            Part(PrimitiveType.Cube, "WingL", new Vector3(-0.65f, -0.05f, -0.2f), new Vector3(0.9f, 0.06f, 0.7f), bodyMat, new Vector3(0f, 0f, 12f));
            Part(PrimitiveType.Cube, "WingR", new Vector3(0.65f, -0.05f, -0.2f), new Vector3(0.9f, 0.06f, 0.7f), bodyMat, new Vector3(0f, 0f, -12f));
            Part(PrimitiveType.Cube, "Fin", new Vector3(0f, 0.3f, -0.5f), new Vector3(0.06f, 0.4f, 0.5f), bodyMat);
            Part(PrimitiveType.Sphere, "Cockpit", new Vector3(0f, 0.2f, 0.25f), new Vector3(0.3f, 0.25f, 0.5f), glowMat);
            Part(PrimitiveType.Sphere, "EngineL", new Vector3(-0.22f, 0f, -0.85f), new Vector3(0.22f, 0.22f, 0.3f), glowMat);
            Part(PrimitiveType.Sphere, "EngineR", new Vector3(0.22f, 0f, -0.85f), new Vector3(0.22f, 0.22f, 0.3f), glowMat);

            foreach (var x in new[] { -1.05f, 1.05f })
            {
                var tipGo = new GameObject("WingTip");
                tipGo.transform.SetParent(root, false);
                tipGo.transform.localPosition = new Vector3(x, -0.05f, -0.4f);
                var trail = tipGo.AddComponent<TrailRenderer>();
                trail.time = 0.35f;
                trail.startWidth = 0.12f;
                trail.endWidth = 0f;
                trail.sharedMaterial = glowMat;
                trail.minVertexDistance = 0.05f;
            }
        }

        static VolumeProfile MakeVolumeProfile()
        {
            const string path = "Assets/Settings/GameVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.4f);
            bloom.highQualityFiltering.Override(false);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.75f);
            if (!profile.TryGet<Vignette>(out var vignette)) vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.5f);
            if (profile.TryGet<ChromaticAberration>(out var ca)) ca.active = false; // WebGL では重いので無効
            EditorUtility.SetDirty(profile);
            return profile;
        }

        // 床用のグリッドテクスチャを生成する（外部画像を使わない）。
        static Texture2D MakeGridTexture()
        {
            const string path = "Assets/Materials/Grid.png";
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var line = new Color(0.25f, 0.9f, 1f, 1f);
            var bg = new Color(0.06f, 0.04f, 0.12f, 1f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var onLine = x % 64 < 3 || y % 64 < 3;
                tex.SetPixel(x, y, onLine ? line : bg);
            }
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material MakeMaterial(string name, Color color, Color emission, Texture2D texture = null)
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
            mat.SetFloat("_Smoothness", 0.6f);
            var hasEmission = emission.maxColorComponent > 0.01f;
            if (hasEmission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (texture != null)
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetTextureScale("_BaseMap", new Vector2(4f, 10f));
            }
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
