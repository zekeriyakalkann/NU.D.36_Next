using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 3 terrain: creates the terrain and sky materials and adds the endless terrain, sky, fog
    /// and ambient light to Rota03 only. Safe to run again (it updates what it created). It never
    /// opens or saves any other scene.
    /// Batch mode: -executeMethod NU36.Editor.Lab3TerrainSceneBuilder.Apply
    /// </summary>
    public static class Lab3TerrainSceneBuilder
    {
        const string TerrainMaterialPath = "Assets/Environment/Materials/Rota03_Arazi.mat";
        const string SkyMaterialPath = "Assets/Environment/Materials/Rota03_Gokyuzu.mat";
        public const string TerrainObjectName = "Arazi";

        // sRGB values as entered in the Inspector. Fog and sky horizon share one colour, so distant
        // terrain fades into the sky.
        public static readonly Color Ufuk = new Color(0.74f, 0.85f, 0.95f);
        static readonly Color Tepe = new Color(0.14f, 0.38f, 0.80f);
        static readonly Color AmbientGok = new Color(0.55f, 0.66f, 0.82f);
        static readonly Color AmbientEkvator = new Color(0.60f, 0.66f, 0.66f);
        static readonly Color AmbientZemin = new Color(0.36f, 0.40f, 0.28f);

        public const float SisBaslangic = 600f;
        public const float SisBitis = 1400f;
        public const float KameraFar = 2200f;

        [MenuItem("NU36/Lab 3/Add Terrain To Rota03")]
        public static void Apply()
        {
            bool ok = TryApply(out string message);
            if (ok) Debug.Log($"[NU36 Terrain] {message}");
            else Debug.LogError($"[NU36 Terrain] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool TryApply(out string message)
        {
            var terrainShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Environment/Shaders/LowPolyTerrain.shader");
            var skyShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Environment/Shaders/GradientSky.shader");
            if (terrainShader == null || skyShader == null)
            {
                message = "Environment shaders not found or not compiled.";
                return false;
            }

            Directory.CreateDirectory("Assets/Environment/Materials");
            Material terrain = LoadOrCreate(TerrainMaterialPath, terrainShader);
            terrain.SetColor("_AmbientSky", AmbientGok);
            terrain.SetColor("_AmbientGround", AmbientZemin);
            terrain.SetFloat("_SunStrength", 0.9f);
            Material sky = LoadOrCreate(SkyMaterialPath, skyShader);
            sky.SetColor("_Zenith", Tepe);
            sky.SetColor("_Horizon", Ufuk);
            sky.SetColor("_Ground", Ufuk);
            EditorUtility.SetDirty(terrain);
            EditorUtility.SetDirty(sky);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene(Lab3SceneBuilder.ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject ucak = roots.SingleOrDefault(g => g.GetComponent<UcusKontrol>() != null);
            Camera camera = roots.Select(g => g.GetComponent<Camera>()).SingleOrDefault(c => c != null && c.CompareTag("MainCamera"));
            if (ucak == null || camera == null)
            {
                message = "Rota03 has no flying aircraft or Main Camera.";
                return false;
            }

            GameObject arazi = roots.FirstOrDefault(g => g.name == TerrainObjectName);
            if (arazi == null)
                arazi = new GameObject(TerrainObjectName);
            arazi.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var sonsuz = arazi.GetComponent<SonsuzArazi>();
            if (sonsuz == null)
                sonsuz = arazi.AddComponent<SonsuzArazi>();
            sonsuz.Hedef = ucak.transform;
            sonsuz.Materyal = terrain;

            RenderSettings.skybox = sky;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Ufuk;
            RenderSettings.fogStartDistance = SisBaslangic;
            RenderSettings.fogEndDistance = SisBitis;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientGok;
            RenderSettings.ambientEquatorColor = AmbientEkvator;
            RenderSettings.ambientGroundColor = AmbientZemin;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.farClipPlane = KameraFar;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                message = $"Saving {Lab3SceneBuilder.ScenePath} failed.";
                return false;
            }
            message = $"Terrain added to {Lab3SceneBuilder.ScenePath}.";
            return true;
        }

        static Material LoadOrCreate(string path, Shader shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else
                m.shader = shader;
            return m;
        }
    }
}
