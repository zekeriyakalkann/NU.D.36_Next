using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NU36.Editor
{
    /// <summary>
    /// Rota07 cockpit gauges and pause menu (added after Lab 7, 2026-11-10):
    ///   • airspeed dial 0–500 km/h (Blender face Exports/Lab7_UI/gece-hiz-kadrani.png, Lab 4 needle) and the Lab 4 throttle
    ///     lever, bottom left of the night HUD, each with a digital read-out; the flight text moves right of them;
    ///   • the scene's pause menu (Esc) and the end screen's "Ana Menü" button target.
    /// Idempotent: run again, it rebuilds the gauges. Used by <see cref="Lab7SceneBuilder"/> for a new Rota07 and by the menu
    /// item / batch method below for the existing scene (only the HUD objects named here and the pause menu object change).
    /// Batch mode: -executeMethod NU36.Editor.Lab7Arayuz.Guncelle
    /// </summary>
    public static class Lab7Arayuz
    {
        public const string Klasor = "Assets/Lab7/UI";
        public const string KadranYolu = Klasor + "/gece-hiz-kadrani.png";
        public const string KadranKaynagi = "../Exports/Lab7_UI/gece-hiz-kadrani.png";   // from the Unity project folder
        public const string IbreYolu = "Assets/UI/gosterge-ibre.png";
        public const string PlakaYolu = "Assets/UI/gaz-kolu-plakasi.png";
        public const string TopuzYolu = "Assets/UI/gaz-kolu-topuz.png";
        public const string DugmeSetiYolu = "Assets/UI/dugme-seti.png";

        /// <summary>Dial range: 500 km/h (the face is drawn 0–500 km/h; HudGosterge works in m/s).</summary>
        public const float KadranAzamiKmSa = 500f;
        public static float KadranAzamiMs => KadranAzamiKmSa / 3.6f;

        // Layout, reference px of the night HUD canvas (1920x1080), from the bottom-left corner.
        public const float KadranBoyu = 264f, PlakaOlcek = 0.28f;
        public static readonly Vector2 KadranMerkezi = new Vector2(150f, 196f), PlakaMerkezi = new Vector2(340f, 206f);
        /// <summary>Knob travel in plate px: the plate's 0 % and 100 % marks are 400 px below/above its centre (Lab 4: ±200 at scale 0.5).</summary>
        public const float KolYolu = 400f;
        public static readonly Color GeceTonu = new Color(0.86f, 0.84f, 0.78f, 1f);   // instruments slightly dimmed for the night
        public static readonly Vector2 UcusMetniKonumu = new Vector2(450f, 30f);
        public const string TusYazisi = Lab7Yardim.TusYazisi;   // includes the assists' keys (2026-11-10)

        [MenuItem("NU36/Lab 7/Add Gauges And Pause Menu To Rota07")]
        public static void Guncelle()
        {
            bool ok;
            string message;
            try { message = TryGuncelle(); ok = true; }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab7 Arayuz] {message}");
            else Debug.LogError($"[NU36 Lab7 Arayuz] {message}");
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

        /// <summary>Imports the dial face from Exports/Lab7_UI if it is not in the project yet, and sets its import rules.</summary>
        public static Sprite KadranYukle()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(KadranYolu) == null)
            {
                string kaynak = Path.GetFullPath(KadranKaynagi);
                if (!File.Exists(kaynak))
                    throw new System.Exception("Dial face missing: " + kaynak + " (Blender: build_ui_assets.py -- night_speed Exports/Lab7_UI)");
                Directory.CreateDirectory(Klasor);
                File.Copy(kaynak, KadranYolu);
                AssetDatabase.ImportAsset(KadranYolu, ImportAssetOptions.ForceSynchronousImport);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(KadranYolu);
            if (imp.textureType != TextureImporterType.Sprite || imp.mipmapEnabled || imp.maxTextureSize != 512 ||
                imp.textureCompression != TextureImporterCompression.Uncompressed)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.maxTextureSize = 512;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
            return Yukle<Sprite>(KadranYolu);
        }

        /// <summary>Adds (or rebuilds) the gauges and the pause menu in an open Rota07. Returns a summary.</summary>
        public static string Uygula(Scene scene)
        {
            GameObject[] kokler = scene.GetRootGameObjects();
            GameObject hudGo = kokler.SingleOrDefault(g => g.name == "Gece HUD");
            if (hudGo == null)
                throw new System.Exception("Rota07 has no 'Gece HUD'.");
            var hud = hudGo.GetComponent<GeceHud>();
            UcusKontrol ucus = kokler.SelectMany(g => g.GetComponentsInChildren<UcusKontrol>(true)).Single();
            var hudKok = (RectTransform)hudGo.transform;

            var (panel, gosterge, gaz, hizMetni, gazMetni) = GostergeleriKur(hudKok, ucus);

            // Flight text right of the gauges (airspeed and throttle are no longer written there), two-line key help.
            Transform ucusMetni = hudKok.Find("Ucus");
            if (ucusMetni != null)
                ((RectTransform)ucusMetni).anchoredPosition = UcusMetniKonumu;
            Transform tuslar = hudKok.Find("Tuslar");
            if (tuslar != null)
            {
                tuslar.GetComponent<Text>().text = TusYazisi;
                ((RectTransform)tuslar).sizeDelta = new Vector2(1000f, 70f);
            }

            var so = new SerializedObject(hud);
            so.FindProperty("gostergeler").objectReferenceValue = panel.gameObject;
            so.FindProperty("hizMetni").objectReferenceValue = hizMetni;
            so.FindProperty("gazMetni").objectReferenceValue = gazMetni;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Pause menu (scene object) and the end screen's menu target.
            GameObject menuGo = kokler.SingleOrDefault(g => g.name == "Duraklatma Menusu");
            if (menuGo == null)
                menuGo = new GameObject("Duraklatma Menusu");
            DuraklatmaMenusu menu = DuraklatmaMenusuKur(menuGo);
            foreach (GeceSonuEkrani son in kokler.SelectMany(g => g.GetComponentsInChildren<GeceSonuEkrani>(true)))
            {
                var sso = new SerializedObject(son);
                sso.FindProperty("menuSahnesi").stringValue = MenuSceneBuilder.SceneName;
                sso.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (Object o in new Object[] { hud, gosterge, gaz, menu })
                EditorUtility.SetDirty(o);
            return $"Gauges (airspeed 0–{KadranAzamiKmSa:F0} km/h, throttle) and pause menu set in {scene.path}.";
        }

        /// <summary>
        /// The airspeed dial (0–500 km/h, Blender face, Lab 4 needle) and the throttle lever with their digital readouts,
        /// in a "Gostergeler" panel at the bottom left of <paramref name="hudKok"/> (replaced if present). Shared by Rota07
        /// and Bölüm 4 (Lab 8).
        /// </summary>
        public static (RectTransform panel, HudGosterge gosterge, HudGazGostergesi gaz, Text hizMetni, Text gazMetni)
            GostergeleriKur(RectTransform hudKok, UcusKontrol ucus)
        {
            Sprite kadran = KadranYukle(), ibre = Yukle<Sprite>(IbreYolu), plaka = Yukle<Sprite>(PlakaYolu), topuz = Yukle<Sprite>(TopuzYolu);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Transform eski = hudKok.Find("Gostergeler");
            if (eski != null)
                Object.DestroyImmediate(eski.gameObject);
            RectTransform panel = Rt("Gostergeler", hudKok);
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
            panel.sizeDelta = new Vector2(420f, 340f);
            panel.anchoredPosition = new Vector2(20f, 16f);
            panel.SetSiblingIndex(1);   // above the marker layer, below the texts

            // Airspeed dial with the Lab 4 needle (pivot = sprite centre = dial centre).
            RectTransform kadranRt = Merkez("HizKadrani", panel, KadranMerkezi, Vector2.one * KadranBoyu);
            Resim(kadranRt.gameObject, kadran);
            RectTransform ibreRt = Rt("Ibre", kadranRt);
            ibreRt.anchorMin = ibreRt.anchorMax = ibreRt.pivot = new Vector2(0.5f, 0.5f);
            ibreRt.sizeDelta = Vector2.one * KadranBoyu;
            ibreRt.anchoredPosition = Vector2.zero;
            Resim(ibreRt.gameObject, ibre);
            var gosterge = kadranRt.gameObject.AddComponent<HudGosterge>();
            gosterge.Ucus = ucus;
            gosterge.Kaynak = HudKaynak.Hiz;
            gosterge.Ibre = ibreRt;
            gosterge.Azami = KadranAzamiMs;
            Text hizMetni = Yazi("HizMetni", panel, font, 28, new Vector2(KadranMerkezi.x, 36f), new Vector2(260f, 40f));

            // Throttle lever: Lab 4 plate and knob, scaled down.
            RectTransform plakaRt = Merkez("GazPlakasi", panel, PlakaMerkezi, new Vector2(256f, 1024f) * PlakaOlcek);
            Resim(plakaRt.gameObject, plaka);
            RectTransform kol = Rt("GazKolu", plakaRt);
            kol.anchorMin = kol.anchorMax = kol.pivot = new Vector2(0.5f, 0.5f);
            kol.sizeDelta = Vector2.one * 512f * 0.145f * (PlakaOlcek / 0.5f);
            kol.anchoredPosition = new Vector2((100f - 128f) * PlakaOlcek, -KolYolu * PlakaOlcek);
            Resim(kol.gameObject, topuz);
            var gaz = plakaRt.gameObject.AddComponent<HudGazGostergesi>();
            var gso = new SerializedObject(gaz);
            gso.FindProperty("ucus").objectReferenceValue = ucus;
            gso.FindProperty("kol").objectReferenceValue = kol;
            gso.FindProperty("altY").floatValue = -KolYolu * PlakaOlcek;
            gso.FindProperty("ustY").floatValue = KolYolu * PlakaOlcek;
            gso.ApplyModifiedPropertiesWithoutUndo();
            Text gazMetni = Yazi("GazMetni", panel, font, 26, new Vector2(PlakaMerkezi.x, 36f), new Vector2(170f, 40f));
            return (panel, gosterge, gaz, hizMetni, gazMetni);
        }

        /// <summary>Sets the pause menu's button sheet and main-menu scene (creates the component if missing).</summary>
        public static DuraklatmaMenusu DuraklatmaMenusuKur(GameObject menuGo)
        {
            var menu = menuGo.GetComponent<DuraklatmaMenusu>();
            if (menu == null)
                menu = menuGo.AddComponent<DuraklatmaMenusu>();
            var mso = new SerializedObject(menu);
            mso.FindProperty("dugmeSeti").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(DugmeSetiYolu);
            mso.FindProperty("anaMenuSahnesi").stringValue = MenuSceneBuilder.SceneName;
            mso.ApplyModifiedPropertiesWithoutUndo();
            return menu;
        }

        // ---------------------------------------------------------------- helpers

        static T Yukle<T>(string yol) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(yol);
            if (a == null)
                throw new System.Exception("Missing asset " + yol);
            return a;
        }

        static RectTransform Rt(string ad, Transform ust)
        {
            var go = new GameObject(ad, typeof(RectTransform));
            go.transform.SetParent(ust, false);
            go.layer = ust.gameObject.layer;
            return (RectTransform)go.transform;
        }

        static RectTransform Merkez(string ad, RectTransform ust, Vector2 merkez, Vector2 boyut)
        {
            RectTransform r = Rt(ad, ust);
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = boyut;
            r.anchoredPosition = merkez;
            return r;
        }

        static void Resim(GameObject go, Sprite s)
        {
            var img = go.AddComponent<Image>();
            img.sprite = s;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = GeceTonu;
        }

        static Text Yazi(string ad, RectTransform ust, Font font, int boyut, Vector2 merkez, Vector2 alan)
        {
            RectTransform r = Merkez(ad, ust, merkez, alan);
            var t = r.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = boyut;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.93f, 0.87f, 0.7f, 1f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = r.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }
    }
}
