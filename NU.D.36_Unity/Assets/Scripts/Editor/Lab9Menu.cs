using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 9 (plan §5.1, INFERRED, as Lab 8 D-118): the main menu opens Bölüm 5. In AnaMenu, "GÖREVE BAŞLA" loads
    /// Bolum5_KartalGecidi, the subtitle names Bölüm 5 and "Bölüm Seç" gets it as its last entry (Bölüm 4 stays in the list).
    /// Idempotent. Batch mode: -executeMethod NU36.Editor.Lab9Menu.Guncelle
    /// </summary>
    public static class Lab9Menu
    {
        public const string AltBaslik = "Bölüm 5 · Kartal Geçidi", BolumAdi = "Hafta 9 · Kartal Geçidi";

        [MenuItem("NU36/Lab 9/Point Main Menu To Bolum5")]
        public static void Guncelle()
        {
            string mesaj = Uygula();
            Debug.Log($"[NU36 Lab9] {mesaj}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        /// <summary>Opens AnaMenu, updates it and saves it (the previously open scenes are not restored).</summary>
        public static string Uygula()
        {
            var scene = EditorSceneManager.OpenScene(MenuSceneBuilder.ScenePath, OpenSceneMode.Single);
            AnaMenu menu = Object.FindAnyObjectByType<AnaMenu>();
            if (menu == null)
                throw new System.Exception("AnaMenu component not found in the menu scene.");
            var so = new SerializedObject(menu);
            so.FindProperty("gorevSahnesi").stringValue = Lab9SceneBuilder.SceneName;
            so.FindProperty("altBaslik").stringValue = AltBaslik;
            SerializedProperty liste = so.FindProperty("bolumler");
            bool var = false;
            for (int i = 0; i < liste.arraySize; i++)
                if (liste.GetArrayElementAtIndex(i).FindPropertyRelative("sahne").stringValue == Lab9SceneBuilder.SceneName)
                    var = true;
            if (!var)
            {
                liste.arraySize++;
                SerializedProperty yeni = liste.GetArrayElementAtIndex(liste.arraySize - 1);
                yeni.FindPropertyRelative("ad").stringValue = BolumAdi;
                yeni.FindPropertyRelative("sahne").stringValue = Lab9SceneBuilder.SceneName;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.Exception("Saving AnaMenu failed.");
            return $"AnaMenu: GÖREVE BAŞLA → {Lab9SceneBuilder.SceneName}; Bölüm Seç has {liste.arraySize} entries.";
        }
    }
}
