using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 4 phase 2: adds the ground-contact detector and the failure restart handler to Rota04 and
    /// wires them to the mission manager. Re-runnable; opens and saves only Rota04.
    /// Batch mode: -executeMethod NU36.Editor.Lab4Phase2SceneBuilder.Apply
    /// </summary>
    public static class Lab4Phase2SceneBuilder
    {
        [MenuItem("NU36/Lab 4/Add Phase 2 To Rota04")]
        public static void Apply()
        {
            bool ok;
            string message;
            try { ok = TryApply(out message); }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab4] {message}");
            else Debug.LogError($"[NU36 Lab4] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool TryApply(out string message)
        {
            var scene = EditorSceneManager.OpenScene(Lab4SceneBuilder.ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject ucak = roots.SingleOrDefault(g => g.GetComponent<UcusKontrol>() != null);
            GameObject oyun = roots.SingleOrDefault(g => g.name == "Oyun");
            if (ucak == null || oyun == null || oyun.GetComponent<GorevYoneticisi>() == null)
            {
                message = "Rota04 is missing the aircraft or the Oyun mission object.";
                return false;
            }

            var ucus = ucak.GetComponent<UcusKontrol>();
            var yonetici = oyun.GetComponent<GorevYoneticisi>();

            var algilayici = ucak.GetComponent<InisAlgilayici>();
            if (algilayici == null)
                algilayici = ucak.AddComponent<InisAlgilayici>();
            algilayici.Ucus = ucus;
            // Pivot-to-lowest-point distance from the aircraft collider (aircraft is level at the scene start).
            var col = ucak.GetComponent<Collider>();
            if (col != null)
                algilayici.TekerOfseti = Mathf.Round((ucak.transform.position.y - col.bounds.min.y) * 100f) / 100f;
            EditorUtility.SetDirty(algilayici);

            var so = new SerializedObject(yonetici);
            so.FindProperty("ucakAlgilayici").objectReferenceValue = algilayici;
            so.ApplyModifiedPropertiesWithoutUndo();

            var yenidenBasla = oyun.GetComponent<YenidenBasla>();
            if (yenidenBasla == null)
                yenidenBasla = oyun.AddComponent<YenidenBasla>();
            var so2 = new SerializedObject(yenidenBasla);
            so2.FindProperty("yonetici").objectReferenceValue = yonetici;
            so2.FindProperty("ucus").objectReferenceValue = ucus;
            so2.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                message = "Saving Rota04 failed.";
                return false;
            }
            message = $"Phase 2 added to Rota04 (wheel offset {algilayici.TekerOfseti} m).";
            return true;
        }
    }
}
