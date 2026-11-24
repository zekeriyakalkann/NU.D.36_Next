using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Main menu scene AnaMenu: a camera and one object with <see cref="AnaMenu"/> (the menu UI is built in code at start
    /// from the Lab 4 menu background and button sheet). Appended to Build Settings; the earlier order is kept and the game
    /// build (<see cref="BuildCommands.BuildWindowsOyun"/>) puts it first. An existing AnaMenu is never overwritten.
    /// Batch mode: -executeMethod NU36.Editor.MenuSceneBuilder.Create
    /// </summary>
    public static class MenuSceneBuilder
    {
        public const string SceneName = "AnaMenu";
        public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
        public const string ArkaplanYolu = "Assets/UI/menu-arkaplan.png";

        [MenuItem("NU36/Menu/Create AnaMenu Scene")]
        public static void Create()
        {
            bool ok;
            string message;
            try { message = TryCreate(); ok = true; }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Menu] {message}");
            else Debug.LogError($"[NU36 Menu] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static string TryCreate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new System.Exception($"{ScenePath} already exists; not overwritten.");
            var arkaplan = AssetDatabase.LoadAssetAtPath<Texture2D>(ArkaplanYolu);
            var dugmeSeti = AssetDatabase.LoadAssetAtPath<Texture2D>(Lab7Arayuz.DugmeSetiYolu);
            if (arkaplan == null || dugmeSeti == null)
                throw new System.Exception("Lab 4 menu art missing (menu-arkaplan.png / dugme-seti.png).");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.045f, 0.04f);
            cam.cullingMask = 0;   // nothing in the world; the menu is an overlay canvas

            var menuGo = new GameObject("Ana Menu");
            var menu = menuGo.AddComponent<AnaMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("arkaplan").objectReferenceValue = arkaplan;
            so.FindProperty("dugmeSeti").objectReferenceValue = dugmeSeti;
            so.FindProperty("gorevSahnesi").stringValue = Lab9SceneBuilder.SceneName;   // Hafta 9: the current mission
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new System.Exception($"Saving {ScenePath} failed.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            return $"Created {ScenePath} and appended it to Build Settings.";
        }
    }
}
