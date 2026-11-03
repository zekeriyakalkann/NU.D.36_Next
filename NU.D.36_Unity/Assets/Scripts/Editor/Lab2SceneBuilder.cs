using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Creates the Lab 2 scene Rota02 reproducibly. Refuses to overwrite an existing scene,
    /// so manual edits made afterwards in the Editor are never lost.
    /// Batch mode: -executeMethod NU36.Editor.Lab2SceneBuilder.CreateRota02
    /// </summary>
    public static class Lab2SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Rota02.unity";

        public const string UcakAdi = "Uçak";
        public const string BalonAdi = "Balon";
        public const string UcaksavarAdi = "Uçaksavar";

        // Values from the Lab 2 worksheet task list.
        static readonly Vector3 UcakKonumu = new Vector3(0f, 20f, 0f);
        static readonly Vector3 BalonKonumu = new Vector3(40f, 30f, 60f);
        static readonly Vector3 BalonOlcegi = new Vector3(6f, 6f, 6f);
        static readonly Vector3 UcaksavarKonumu = Vector3.zero;

        // Not specified by the worksheet: presentation only (DecisionLog D-021, D-023). Side view of
        // the flight path: the aircraft is about 120 px wide at both the start and the stop (720p),
        // every object stays at least 10 % inside the frame edges (no corner distortion), and at the
        // stop the aircraft is drawn beside Balon rather than over it.
        // Earlier: (-60, 60, -50) → (20, 20, 30), then (-35, 40, 10) → (18, 10, 28).
        public static readonly Vector3 KameraKonumu = new Vector3(60f, 35f, 0f);
        public static readonly Vector3 KameraBakisNoktasi = new Vector3(20f, 15f, 30f);

        [MenuItem("NU36/Lab 2/Create Rota02 Scene")]
        public static void CreateRota02()
        {
            bool ok = TryCreate(out string message);
            if (ok)
                Debug.Log($"[NU36 Lab2] {message}");
            else
                Debug.LogError($"[NU36 Lab2] {message}");

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>
        /// Updates only the Main Camera transform of the existing Rota02 scene to the values above.
        /// Batch mode: -executeMethod NU36.Editor.Lab2SceneBuilder.ApplyCameraToRota02
        /// </summary>
        [MenuItem("NU36/Lab 2/Apply Camera To Rota02")]
        public static void ApplyCameraToRota02()
        {
            bool ok = TryApplyCamera(out string message);
            if (ok)
                Debug.Log($"[NU36 Lab2] {message}");
            else
                Debug.LogError($"[NU36 Lab2] {message}");

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool TryApplyCamera(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                message = $"{ScenePath} not found.";
                return false;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = scene.GetRootGameObjects()
                .Select(g => g.GetComponent<Camera>())
                .SingleOrDefault(c => c != null && c.CompareTag("MainCamera"));
            if (camera == null)
            {
                message = "Exactly one root Main Camera expected in Rota02.";
                return false;
            }

            Undo.RecordObject(camera.transform, "Apply Rota02 camera");
            camera.transform.position = KameraKonumu;
            camera.transform.LookAt(KameraBakisNoktasi);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                message = $"Saving {ScenePath} failed.";
                return false;
            }

            message = $"Camera set to {KameraKonumu} looking at {KameraBakisNoktasi}; " +
                      $"euler {camera.transform.eulerAngles:F2}, rotation {camera.transform.rotation:F6}.";
            return true;
        }

        static bool TryCreate(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                message = $"{ScenePath} already exists; not overwritten.";
                return false;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AircraftModelPostprocessor.AircraftModelPath);
            if (model == null)
            {
                message = $"Aircraft model not found at {AircraftModelPostprocessor.AircraftModelPath}.";
                return false;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.transform.position = KameraKonumu;
                camera.transform.LookAt(KameraBakisNoktasi);
            }

            GameObject balon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            balon.name = BalonAdi;
            balon.transform.SetPositionAndRotation(BalonKonumu, Quaternion.identity);
            balon.transform.localScale = BalonOlcegi;

            // The root carries position and heading; the model child carries the visual roll.
            var ucak = new GameObject(UcakAdi);
            ucak.transform.SetPositionAndRotation(UcakKonumu, Quaternion.identity);
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            modelInstance.transform.SetParent(ucak.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;

            var hedefeGit = ucak.AddComponent<HedefeGit>();
            hedefeGit.Hedef = balon.transform;
            var kanatYatisi = ucak.AddComponent<KanatYatisi>();
            kanatYatisi.Hedef = balon.transform;
            kanatYatisi.Gorsel = modelInstance.transform;

            // Identity rotation: transform.forward is world +Z, as the worksheet specifies.
            GameObject ucaksavar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ucaksavar.name = UcaksavarAdi;
            ucaksavar.transform.SetPositionAndRotation(UcaksavarKonumu, Quaternion.identity);
            var radar = ucaksavar.AddComponent<Radar>();
            radar.Hedef = ucak.transform;

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                message = $"Saving {ScenePath} failed.";
                return false;
            }

            AddToBuildSettingsFirst(ScenePath);
            message = $"Created {ScenePath} and added it to Build Settings.";
            return true;
        }

        // Rota02 becomes the first (startup) scene of the build; existing entries are kept.
        static void AddToBuildSettingsFirst(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
