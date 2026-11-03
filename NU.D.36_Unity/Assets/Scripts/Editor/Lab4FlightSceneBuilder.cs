using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Switches the opt-in flight model of UcusKontrol and the horizon-locked chase camera on in Rota04Hud only. Rota02, Rota03 and Rota04
    /// keep the Lab 3 values (all new parameters zero / false). Re-runnable: it just writes the same values again.
    /// Batch mode: -executeMethod NU36.Editor.Lab4FlightSceneBuilder.Apply
    /// </summary>
    public static class Lab4FlightSceneBuilder
    {
        // Values and reasons: Documentation/Lab4_Flight_Model.md.
        public const float HizIvmesi = 6f;          // m/s²: 8 -> 42 m/s in 5.7 s (was 40.8 m/s² = 0.8 s)
        public const float EgimHizKaybi = 12f;      // m/s per unit of forward.y
        public const float YatisDengeleme = 1.5f;   // 1/s
        public const float EnFazlaYatis = 65f;      // degrees
        public const float YatisDonusu = 1f;        // physical coordinated turn
        public const float EnFazlaDonusHizi = 45f;  // degrees/s

        [MenuItem("NU36/Lab 4/Apply Flight Model To Rota04Hud")]
        public static void Apply()
        {
            bool ok;
            string message;
            try { ok = TryApply(out message); }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab4 Flight] {message}");
            else Debug.LogError($"[NU36 Lab4 Flight] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool TryApply(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Lab4HudSceneBuilder.ScenePath) == null)
            {
                message = Lab4HudSceneBuilder.ScenePath + " does not exist.";
                return false;
            }
            var scene = EditorSceneManager.OpenScene(Lab4HudSceneBuilder.ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject ucak = roots.SingleOrDefault(g => g.GetComponent<UcusKontrol>() != null);
            GameObject kamera = roots.SingleOrDefault(g => g.GetComponent<TakipKamerasi>() != null);
            if (ucak == null || kamera == null)
            {
                message = "Rota04Hud has no aircraft or no chase camera.";
                return false;
            }

            var so = new SerializedObject(ucak.GetComponent<UcusKontrol>());
            so.FindProperty("hizIvmesi").floatValue = HizIvmesi;
            so.FindProperty("egimHizKaybi").floatValue = EgimHizKaybi;
            so.FindProperty("yatisDengeleme").floatValue = YatisDengeleme;
            so.FindProperty("enFazlaYatis").floatValue = EnFazlaYatis;
            so.FindProperty("yatisDonusu").floatValue = YatisDonusu;
            so.FindProperty("enFazlaDonusHizi").floatValue = EnFazlaDonusHizi;
            so.ApplyModifiedPropertiesWithoutUndo();

            var co = new SerializedObject(kamera.GetComponent<TakipKamerasi>());
            co.FindProperty("ufukKilitli").boolValue = true;
            co.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                message = "Saving Rota04Hud failed.";
                return false;
            }
            message = "Flight model and horizon-locked camera set in Rota04Hud.";
            return true;
        }
    }
}
