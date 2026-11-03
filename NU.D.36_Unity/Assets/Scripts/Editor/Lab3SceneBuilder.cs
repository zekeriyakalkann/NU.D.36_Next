using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 3 setup: adds the "Ucus" action map to the existing Input Actions asset (other maps are
    /// kept) and creates Rota03 as a copy of Rota02 (equivalent to Save As), then wires the Lab 3
    /// components. Never overwrites an existing Rota03 and never modifies Rota02.
    /// Batch mode: -executeMethod NU36.Editor.Lab3SceneBuilder.CreateRota03
    /// </summary>
    public static class Lab3SceneBuilder
    {
        public const string SourceScenePath = Lab2SceneBuilder.ScenePath;
        public const string ScenePath = "Assets/Scenes/Rota03.unity";
        public const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        public const string UcusMap = "Ucus";
        const string KeyboardScheme = "Keyboard&Mouse";

        // Action name, positive key, negative key (Lab 3 worksheet table).
        static readonly (string action, string positive, string negative)[] UcusActions =
        {
            ("Yunuslama", "<Keyboard>/w", "<Keyboard>/s"),
            ("Yatis", "<Keyboard>/d", "<Keyboard>/a"),
            ("Sapma", "<Keyboard>/e", "<Keyboard>/q"),
            ("Gaz", "<Keyboard>/upArrow", "<Keyboard>/downArrow"),
        };

        [MenuItem("NU36/Lab 3/Create Rota03 Scene")]
        public static void CreateRota03()
        {
            bool ok = EnsureUcusActionMap(out string inputMessage) & TryCreateScene(out string sceneMessage);
            string message = $"{inputMessage} {sceneMessage}";
            if (ok)
                Debug.Log($"[NU36 Lab3] {message}");
            else
                Debug.LogError($"[NU36 Lab3] {message}");

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Adds the Ucus map with four 1D Axis actions if it is not already present.</summary>
        public static bool EnsureUcusActionMap(out string message)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                message = $"{InputActionsPath} not found.";
                return false;
            }
            if (asset.FindActionMap(UcusMap) != null)
            {
                message = $"'{UcusMap}' map already present; asset not changed.";
                return true;
            }

            // The asset is the project-wide actions asset and is enabled even in the Editor, so it
            // cannot be modified in place. Edit an in-memory copy and write that back.
            InputActionAsset copy = InputActionAsset.FromJson(asset.ToJson());
            InputActionMap map = copy.AddActionMap(UcusMap);
            foreach (var (actionName, positive, negative) in UcusActions)
            {
                InputAction action = map.AddAction(actionName, InputActionType.Value, expectedControlLayout: "Axis");
                action.AddCompositeBinding("1DAxis")
                    .With("Negative", negative, KeyboardScheme)
                    .With("Positive", positive, KeyboardScheme);
            }

            // Same serialization the Input Actions editor uses when it saves the asset.
            File.WriteAllText(InputActionsPath, copy.ToJson());
            Object.DestroyImmediate(copy);
            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate);
            message = $"Added '{UcusMap}' map with {UcusActions.Length} actions to {InputActionsPath}.";
            return true;
        }

        static bool TryCreateScene(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                message = $"{ScenePath} already exists; not overwritten.";
                return false;
            }
            if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
            {
                message = $"Copying {SourceScenePath} to {ScenePath} failed.";
                return false;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject ucak = roots.SingleOrDefault(g => g.GetComponent<HedefeGit>() != null);
            Camera camera = roots.Select(g => g.GetComponent<Camera>()).SingleOrDefault(c => c != null && c.CompareTag("MainCamera"));
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (ucak == null || camera == null || actions == null)
            {
                message = "Rota03 copy is missing the aircraft, the Main Camera or the Input Actions asset.";
                return false;
            }

            // Lab 2 automation stays on the aircraft but must not steer it in Lab 3.
            ucak.GetComponent<HedefeGit>().enabled = false;
            // KanatYatisi banks the model toward Balon; with player roll control this would add a
            // second, unrelated bank, so it is disabled here too (Rota03 only).
            var kanatYatisi = ucak.GetComponent<KanatYatisi>();
            if (kanatYatisi != null)
            {
                kanatYatisi.enabled = false;
                if (kanatYatisi.Gorsel != null)
                    kanatYatisi.Gorsel.localRotation = Quaternion.identity;
            }

            var ucus = ucak.AddComponent<UcusKontrol>();
            var playerInput = ucak.AddComponent<PlayerInput>();
            playerInput.actions = actions;
            playerInput.defaultActionMap = UcusMap;
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;

            var saglik = ucak.AddComponent<Saglik>();
            var canYazisi = ucak.AddComponent<CanYazisi>();
            canYazisi.Saglik = saglik;

            var takip = camera.gameObject.AddComponent<TakipKamerasi>();
            takip.Hedef = ucak.transform;
            var so = new SerializedObject(takip);
            so.FindProperty("ucus").objectReferenceValue = ucus;
            so.ApplyModifiedPropertiesWithoutUndo();
            takip.Yerles(); // start pose = follow offset, so Play does not begin with a long swoop

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                message = $"Saving {ScenePath} failed.";
                return false;
            }

            // Appended: Rota02 (index 0) and SampleScene keep their order.
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            message = $"Created {ScenePath} from {SourceScenePath} and appended it to Build Settings.";
            return true;
        }
    }
}
