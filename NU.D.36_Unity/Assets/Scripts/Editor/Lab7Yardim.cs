using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NU36.Editor
{
    /// <summary>
    /// Rota07 assists (user request 2026-11-10: brake, easier flight, automatic hits, a GPS):
    ///   • the aircraft's easy flight law on (<see cref="UcusKontrol.KolayUcus"/>, K toggles it in the game) with the air brake;
    ///   • <see cref="UcusYardimcisi"/> on the aircraft: brake key (F/B), terrain floor, landing sink envelope, runway box;
    ///   • <see cref="NisanSistemi"/> on the aircraft: target selection (T), lock, guided bombs, automatic release (O);
    ///   • the GPS moving map (<see cref="GpsHaritasi"/>) in the night HUD, bottom right, above the key help;
    ///   • the HUD's references and the two-line key help.
    /// Idempotent: run again, it rebuilds the GPS panel and resets the values below; nothing else in the scene changes.
    /// Used by <see cref="Lab7SceneBuilder"/> for a new Rota07 and by the menu item / batch method for the existing scene.
    /// Batch mode: -executeMethod NU36.Editor.Lab7Yardim.Guncelle
    /// </summary>
    public static class Lab7Yardim
    {
        public const string TusYazisi =
            "W/S burun   A/D yatış   Q/E dümen   ↑/↓ gaz   F fren   K kolay uçuş\n" +
            "Space bomba   T hedef   O oto atış   M harita   L fener   Esc menü";
        public static readonly Vector2 GpsBoyutu = new Vector2(380f, 452f);
        public static readonly Vector2 GpsKonumu = new Vector2(-24f, 104f);   // from the bottom-right corner, above the key help
        /// <summary>Touchdown speed the landing assist aims below (m/s): 3 m/s under the landing limit.</summary>
        public static float InisYardimHizi => Lab7SceneBuilder.InisEnFazlaHiz - 3f;

        [MenuItem("NU36/Lab 7/Add Flight Assist, Targeting And GPS To Rota07")]
        public static void Guncelle()
        {
            bool ok;
            string message;
            try { message = TryGuncelle(); ok = true; }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab7 Yardim] {message}");
            else Debug.LogError($"[NU36 Lab7 Yardim] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static string TryGuncelle()
        {
            var scene = EditorSceneManager.OpenScene(Lab7SceneBuilder.ScenePath, OpenSceneMode.Single);
            string sonuc = Uygula(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.Exception("Saving Rota07 failed.");
            return sonuc;
        }

        /// <summary>Adds (or resets) the assists in an open Rota07. Returns a summary.</summary>
        public static string Uygula(Scene scene)
        {
            GameObject[] kokler = scene.GetRootGameObjects();
            UcusKontrol ucus = kokler.SelectMany(g => g.GetComponentsInChildren<UcusKontrol>(true)).Single();
            GameObject ucak = ucus.gameObject;
            GeceGorevi gorev = kokler.SelectMany(g => g.GetComponentsInChildren<GeceGorevi>(true)).Single();
            GameObject hudGo = kokler.SingleOrDefault(g => g.name == "Gece HUD");
            if (hudGo == null)
                throw new System.Exception("Rota07 has no 'Gece HUD'.");
            var hud = hudGo.GetComponent<GeceHud>();
            var algilayici = ucak.GetComponent<InisAlgilayici>();
            var yuk = ucak.GetComponent<BombaYuku>();

            // Flight model: easy flight on (the Lab 7 stick law stays one key away), its law at the code's defaults.
            Undo.RecordObject(ucus, "Lab7 Yardim");
            typeof(UcusKontrol).GetField("yardim", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(ucus, new UcusYardimi());
            var so = new SerializedObject(ucus);
            so.FindProperty("kolayUcus").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Assistant: brake, terrain floor, landing envelope; the runway box from the night map.
            // Re-added each time, so the scene always carries the code's current defaults (the HUD and the GPS are re-wired below).
            foreach (var eskiBilesen in ucak.GetComponents<UcusYardimcisi>())
                Object.DestroyImmediate(eskiBilesen);
            foreach (var eskiBilesen in ucak.GetComponents<NisanSistemi>())
                Object.DestroyImmediate(eskiBilesen);
            var yardimci = ucak.AddComponent<UcusYardimcisi>();
            var yso = new SerializedObject(yardimci);
            yso.FindProperty("ucus").objectReferenceValue = ucus;
            yso.FindProperty("algilayici").objectReferenceValue = algilayici;
            yso.FindProperty("pistBasi").vector3Value = GeceHaritasi.PistBasi;
            yso.FindProperty("pistSonu").vector3Value = GeceHaritasi.PistSonu;
            yso.FindProperty("inisHizi").floatValue = InisYardimHizi;
            yso.FindProperty("pistEni").floatValue = GeceHaritasi.PistEni;
            yso.FindProperty("haritaMerkezi").vector3Value = GeceHaritasi.HaritaMerkezi;
            yso.ApplyModifiedPropertiesWithoutUndo();

            // Targeting: lock, guided bombs, automatic release.
            var nisan = ucak.AddComponent<NisanSistemi>();
            var nso = new SerializedObject(nisan);
            nso.FindProperty("gorev").objectReferenceValue = gorev;
            nso.FindProperty("yuk").objectReferenceValue = yuk;
            nso.FindProperty("ucus").objectReferenceValue = ucus;
            nso.ApplyModifiedPropertiesWithoutUndo();

            // GPS panel in the night HUD.
            var hudKok = (RectTransform)hudGo.transform;
            Transform eski = hudKok.Find("GPS");
            if (eski != null)
                Object.DestroyImmediate(eski.gameObject);
            var gpsGo = new GameObject("GPS", typeof(RectTransform));
            gpsGo.layer = hudGo.layer;
            var gps = (RectTransform)gpsGo.transform;
            gps.SetParent(hudKok, false);
            gps.anchorMin = gps.anchorMax = gps.pivot = new Vector2(1f, 0f);
            gps.sizeDelta = GpsBoyutu;
            gps.anchoredPosition = GpsKonumu;
            var harita = gpsGo.AddComponent<GpsHaritasi>();
            var gso = new SerializedObject(harita);
            gso.FindProperty("gorev").objectReferenceValue = gorev;
            gso.FindProperty("ucus").objectReferenceValue = ucus;
            gso.FindProperty("nisan").objectReferenceValue = nisan;
            gso.FindProperty("haritaDokusu").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Lab7Varliklar.RenkHaritasiYolu);
            gso.FindProperty("yazi").objectReferenceValue = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gso.ApplyModifiedPropertiesWithoutUndo();

            // HUD references and the key help.
            var hso = new SerializedObject(hud);
            hso.FindProperty("nisan").objectReferenceValue = nisan;
            hso.FindProperty("yardimci").objectReferenceValue = yardimci;
            hso.ApplyModifiedPropertiesWithoutUndo();
            Transform tuslar = hudKok.Find("Tuslar");
            if (tuslar != null)
            {
                tuslar.GetComponent<Text>().text = TusYazisi;
                ((RectTransform)tuslar).sizeDelta = new Vector2(1000f, 70f);
            }

            foreach (Object o in new Object[] { ucus, yardimci, nisan, harita, hud })
                EditorUtility.SetDirty(o);
            return $"Easy flight, brake, targeting (guided bombs, automatic release) and the GPS map set in {scene.path}.";
        }
    }
}
