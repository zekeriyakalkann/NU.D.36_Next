using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 4 phase 1: creates the target prefab and Rota04 as a copy of Rota03 (Save As equivalent),
    /// then adds the mission objects to the copy. Never overwrites an existing Rota04 and never
    /// modifies Rota02, Rota03 or SampleScene.
    /// Batch mode: -executeMethod NU36.Editor.Lab4SceneBuilder.CreateRota04
    /// </summary>
    public static class Lab4SceneBuilder
    {
        public const string SourceScenePath = Lab3SceneBuilder.ScenePath;
        public const string ScenePath = "Assets/Scenes/Rota04.unity";
        public const string PrefabPath = "Assets/Lab4/Prefabs/KesifHedefi.prefab";
        const string MaterialFolder = "Assets/Lab4/Materials";

        // Reconnaissance targets (x, z) around the start (0, 0), 400-430 m away in three directions.
        // The ground height at each point comes from the terrain function.
        static readonly Vector2[] TargetPositions = { new Vector2(150f, 400f), new Vector2(-350f, 250f), new Vector2(300f, -300f) };

        [MenuItem("NU36/Lab 4/Create Rota04 Scene")]
        public static void CreateRota04()
        {
            bool ok;
            string message;
            try { ok = TryCreate(out message); }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab4] {message}");
            else Debug.LogError($"[NU36 Lab4] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool TryCreate(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                message = $"{ScenePath} already exists; not overwritten.";
                return false;
            }

            GameObject prefab = CreateTargetPrefab();
            Material baseMaterial = LoadOrCreateMaterial("Us", new Color(0.2f, 0.55f, 1f));

            if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
            {
                message = $"Copying {SourceScenePath} to {ScenePath} failed.";
                return false;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject ucak = scene.GetRootGameObjects().SingleOrDefault(g => g.GetComponent<UcusKontrol>() != null);
            if (ucak == null)
            {
                message = "Rota04 copy has no aircraft with UcusKontrol.";
                return false;
            }
            AddAircraftPhysics(ucak);

            // Mission objects.
            var oyun = new GameObject("Oyun");
            var cuzdan = oyun.AddComponent<PuanCuzdani>();
            var yonetici = oyun.AddComponent<GorevYoneticisi>();
            var ekran = oyun.AddComponent<GorevSonuEkrani>();

            var hedefler = new GameObject("Hedefler").transform;
            var list = new KesifHedefi[TargetPositions.Length];
            for (int i = 0; i < list.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.name = $"Hedef_{i + 1}";
                go.transform.SetParent(hedefler, false);
                Vector2 p = TargetPositions[i];
                go.transform.position = new Vector3(p.x, AraziUretici.Yukseklik(p.x, p.y), p.y);
                list[i] = go.GetComponent<KesifHedefi>(); // prefab defaults: required, 100 points
            }

            var us = new GameObject("Us");
            us.transform.position = Vector3.zero;
            var box = us.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 70f, 0f);
            box.size = new Vector3(120f, 160f, 120f);
            var usGirisi = us.AddComponent<UsGirisi>();
            usGirisi.Yonetici = yonetici;
            var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.name = "UsIsareti";
            Object.DestroyImmediate(disk.GetComponent<Collider>());
            disk.transform.SetParent(us.transform, false);
            disk.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            disk.transform.localScale = new Vector3(60f, 0.1f, 60f);
            disk.GetComponent<MeshRenderer>().sharedMaterial = baseMaterial;

            var so = new SerializedObject(yonetici);
            var arr = so.FindProperty("hedefler");
            arr.arraySize = list.Length;
            for (int i = 0; i < list.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.FindProperty("ussu").objectReferenceValue = usGirisi;
            so.ApplyModifiedPropertiesWithoutUndo();

            var so2 = new SerializedObject(ekran);
            so2.FindProperty("yonetici").objectReferenceValue = yonetici;
            so2.FindProperty("cuzdan").objectReferenceValue = cuzdan;
            so2.ApplyModifiedPropertiesWithoutUndo();

            // The base starts disabled; the manager enables it for the return phase.
            us.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                message = $"Saving {ScenePath} failed.";
                return false;
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            message = $"Created {ScenePath} from {SourceScenePath} with {list.Length} targets and appended it to Build Settings.";
            return true;
        }

        /// <summary>
        /// Triggers need a Rigidbody on one side. The aircraft moves by Transform, so it gets a
        /// kinematic Rigidbody (no gravity) and a box collider around its model. Existing components are reused.
        /// </summary>
        static void AddAircraftPhysics(GameObject ucak)
        {
            var rb = ucak.GetComponent<Rigidbody>();
            if (rb == null)
                rb = ucak.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            if (ucak.GetComponentInChildren<Collider>() == null)
            {
                Renderer[] renderers = ucak.GetComponentsInChildren<Renderer>();
                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers)
                    b.Encapsulate(r.bounds);
                var box = ucak.AddComponent<BoxCollider>();
                box.center = ucak.transform.InverseTransformPoint(b.center);
                box.size = b.size;
            }
        }

        static GameObject CreateTargetPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
                return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Material mat = LoadOrCreateMaterial("Hedef", new Color(1f, 0.55f, 0.1f));

            var root = new GameObject("KesifHedefi");
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 90f, 0f);
            box.size = new Vector3(30f, 180f, 30f);

            var gorsel = new GameObject("Gorsel");
            gorsel.transform.SetParent(root.transform, false);
            GameObject pole = Primitive(PrimitiveType.Cylinder, "Direk", gorsel.transform, mat);
            pole.transform.localPosition = new Vector3(0f, 90f, 0f);
            pole.transform.localScale = new Vector3(3f, 90f, 3f); // cylinder is 2 high: 180 m
            GameObject ball = Primitive(PrimitiveType.Sphere, "Isaret", gorsel.transform, mat);
            ball.transform.localPosition = new Vector3(0f, 30f, 0f);
            ball.transform.localScale = Vector3.one * 14f;

            var hedef = root.AddComponent<KesifHedefi>();
            hedef.Gorsel = gorsel;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material mat)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }

        static Material LoadOrCreateMaterial(string name, Color color)
        {
            Directory.CreateDirectory(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null)
                return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetColor("_EmissionColor", color * 0.6f);
            m.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
