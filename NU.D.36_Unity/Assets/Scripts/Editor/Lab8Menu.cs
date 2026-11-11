using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8 phase 3 (plan §5.1, INFERRED, small): the main menu opens Bölüm 4. In the AnaMenu scene, "GÖREVE BAŞLA" loads
    /// Bolum4_KursunDaglari, the subtitle names Bölüm 4 and the "Bölüm Seç" list gets it as its last entry. Idempotent.
    /// Batch mode: -executeMethod NU36.Editor.Lab8Menu.Guncelle
    /// </summary>
    public static class Lab8Menu
    {
        public const string AltBaslik = "Bölüm 4 · Kurşun Dağları", BolumAdi = "Hafta 8 · Kurşun Dağları";

        [MenuItem("NU36/Lab 8/Point Main Menu To Bolum4")]
        public static void Guncelle()
        {
            string mesaj = Uygula();
            Debug.Log($"[NU36 Lab8] {mesaj}");
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
            so.FindProperty("gorevSahnesi").stringValue = Lab8SceneBuilder.SceneName;
            so.FindProperty("altBaslik").stringValue = AltBaslik;
            SerializedProperty liste = so.FindProperty("bolumler");
            bool var = false;
            for (int i = 0; i < liste.arraySize; i++)
                if (liste.GetArrayElementAtIndex(i).FindPropertyRelative("sahne").stringValue == Lab8SceneBuilder.SceneName)
                    var = true;
            if (!var)
            {
                liste.arraySize++;
                SerializedProperty yeni = liste.GetArrayElementAtIndex(liste.arraySize - 1);
                yeni.FindPropertyRelative("ad").stringValue = BolumAdi;
                yeni.FindPropertyRelative("sahne").stringValue = Lab8SceneBuilder.SceneName;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.Exception("Saving AnaMenu failed.");
            return $"AnaMenu: GÖREVE BAŞLA → {Lab8SceneBuilder.SceneName}; Bölüm Seç has {liste.arraySize} entries.";
        }
    }
}
