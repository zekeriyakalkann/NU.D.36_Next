using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using H = NU36.KartalGecidiHaritasi;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 9 (Hafta 9): Bölüm 5 · Kartal Geçidi. The new scene is a copy of Bölüm 4 (worksheet: "Bölüm 4'ten ayrı, yeni bir
    /// sahne"): the aircraft with its arcade flight, machine gun, bombs, targeting assist, engine smoke and sound, the camera,
    /// the daylight and the pause menu come over; the mountain world of Bölüm 4 is removed and the coast is built: terrain
    /// with the pass and the bay, the sea, the landing fleet, the guard flights, the mission, the HUD and the GPS.
    /// Earlier scenes stay untouched; an existing scene is never overwritten.
    /// Batch mode: -executeMethod NU36.Editor.Lab9SceneBuilder.Create
    /// </summary>
    public static class Lab9SceneBuilder
    {
        public const string SceneName = "Bolum5_KartalGecidi";
        public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
        /// <summary>Bölüm 4's world (everything else of the copy is kept).</summary>
        public static readonly string[] SilinenKokler =
        {
            "Kursun Daglari Arazisi", "Kursun Daglari Gorevi", "Ray Hatti", "Tren", "Guvenli Bolge", "Viyaduk", "Bolum4 HUD",
            "Ates Bolgeleri", "Nehir Suyu", "Kalkis Istasyonu", "Us",
        };
        public const float BaslangicHizi = 61f, BaslangicGazi = 0.6f;
        /// <summary>INFERRED (plan §6): 12 ships, so more bombs than Bölüm 4's two.</summary>
        public const int BombaKapasitesi = 6;

        [MenuItem("NU36/Lab 9/Rebuild Bolum5 (delete and create)")]
        public static void YenidenKur()
        {
            AssetDatabase.DeleteAsset(ScenePath);
            foreach (string k in new[] { Lab9Varliklar.Malzemeler, Lab9Varliklar.Prefablar, Lab9Varliklar.Uretilen, Lab9Varliklar.AraziKlasoru })
                AssetDatabase.DeleteAsset(k);
            AssetDatabase.Refresh();
            Create();
        }

        [MenuItem("NU36/Lab 9/Create Bolum5 Scene")]
        public static void Create()
        {
            bool ok;
            string message;
            try { message = TryCreate(); ok = true; }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab9] {message}");
            else Debug.LogError($"[NU36 Lab9] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static int Katman(string ad)
        {
            int k = LayerMask.NameToLayer(ad);
            if (k < 0)
                throw new System.Exception($"Layer {ad} missing: run NU36 → Lab 5 → Set Up Layers And Physics first.");
            return k;
        }

        static string TryCreate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new System.Exception($"{ScenePath} already exists; not overwritten.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Lab8SceneBuilder.ScenePath) == null)
                throw new System.Exception($"{Lab8SceneBuilder.ScenePath} missing: build Bölüm 4 first.");
            int arazi = Katman("Arazi"), hedef = Katman("Hedef");

            // ---- assets (before the scene is opened) -------------------------------------------------------
            AssetDatabase.ImportAsset($"{Lab9Varliklar.Dokular}/coast_sand_01_nor_gl_2k.jpg", ImportAssetOptions.ForceUpdate);
            GameObject tankOn = Lab9Varliklar.TankPrefabi(hedef);
            Lab9Varliklar.GemiPrefabi(tankOn, hedef);
            Lab9Varliklar.DenizMalzemesi();
            var agaclar = Lab8Varliklar.Agaclar.Select(a => Yukle<GameObject>($"{Lab8Varliklar.Prefablar}/Doga/{a.ad}.prefab")).ToArray();
            var kayalar = Lab8Varliklar.Kayalar.Select(a => Yukle<GameObject>($"{Lab8Varliklar.Prefablar}/Doga/{a.ad}.prefab")).ToArray();
            var zaman = System.Diagnostics.Stopwatch.StartNew();
            Lab9Arazi.Sonuc sonuc = Lab9Arazi.Olustur(Lab9Arazi.Katmanlar(), agaclar, kayalar);
            AssetDatabase.DeleteAsset(Lab9Arazi.VeriYolu);
            AssetDatabase.CreateAsset(sonuc.Veri, Lab9Arazi.VeriYolu);
            Debug.Log($"[NU36 Lab9] terrain built in {zaman.Elapsed.TotalSeconds:F1} s, {sonuc.Agac} trees, {sonuc.Kaya} rocks");
            AssetDatabase.SaveAssets();

            // ---- scene -----------------------------------------------------------------------------------------
            if (!AssetDatabase.CopyAsset(Lab8SceneBuilder.ScenePath, ScenePath))
                throw new System.Exception($"Copying {Lab8SceneBuilder.ScenePath} to {ScenePath} failed.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject Kok(string ad) => scene.GetRootGameObjects().SingleOrDefault(g => g.name == ad);
            foreach (string ad in SilinenKokler)
                if (Kok(ad) != null)
                    Object.DestroyImmediate(Kok(ad));
            GameObject ucakGo = scene.GetRootGameObjects().Single(g => g.GetComponent<UcusKontrol>() != null);
            var ucus = ucakGo.GetComponent<UcusKontrol>();

            // Terrain.
            var veri = Yukle<TerrainData>(Lab9Arazi.VeriYolu);
            GameObject araziGo = Terrain.CreateTerrainGameObject(veri);
            araziGo.name = "Kartal Gecidi Arazisi";
            araziGo.transform.position = H.AraziKosesi;
            araziGo.layer = arazi;
            var terrain = araziGo.GetComponent<Terrain>();
            terrain.materialTemplate = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.defaultTerrainMaterial : null;
            terrain.heightmapPixelError = 4f;
            terrain.basemapDistance = 1600f;
            terrain.treeDistance = 2600f;
            terrain.treeBillboardDistance = 3000f;
            terrain.treeMaximumFullLODCount = 4000;
            terrain.drawInstanced = true;

            DenizKur(arazi);
            KumsalKur();
            Makineli makineli = UcagiKur(ucakGo, ucus);

            Camera cam = Kok("Main Camera").GetComponent<Camera>();
            cam.farClipPlane = 12000f;
            GameObject gunes = Kok("Gunes");
            if (gunes != null) gunes.transform.rotation = Quaternion.Euler(34f, 235f, 0f);   // afternoon sun over the sea, from the south-west

            var filoGo = new GameObject("Cikarma Filosu");
            var filo = filoGo.AddComponent<CikarmaFilosu>();
            Ayarla(filo, so => so.FindProperty("gemiOn").objectReferenceValue = Yukle<CikarmaGemisi>(Lab9Varliklar.GemiYolu));
            KorumaUcuslari koruma = KorumaKur(ucus);

            Bolum5Hud hud = HudKur(ucus, ucakGo.GetComponent<BombaYuku>(), makineli, filo, koruma, ucakGo.GetComponent<Bolum4Nisan>());
            var gorevGo = new GameObject("Kartal Gecidi Gorevi");
            var gorev = gorevGo.AddComponent<KartalGecidiGorevi>();
            Ayarla(gorev, so =>
            {
                so.FindProperty("ucus").objectReferenceValue = ucus;
                so.FindProperty("ucakSagligi").objectReferenceValue = ucakGo.GetComponent<Saglik>();
                so.FindProperty("algilayici").objectReferenceValue = ucakGo.GetComponent<InisAlgilayici>();
                so.FindProperty("makineli").objectReferenceValue = makineli;
                so.FindProperty("yuk").objectReferenceValue = ucakGo.GetComponent<BombaYuku>();
                so.FindProperty("filo").objectReferenceValue = filo;
                so.FindProperty("koruma").objectReferenceValue = koruma;
                so.FindProperty("hud").objectReferenceValue = hud;
            });
            Ayarla(hud, so => so.FindProperty("gorevAkisi").objectReferenceValue = gorev);
            Ayarla(hud.GetComponentInChildren<Bolum5Gps>(true), so => so.FindProperty("gorev").objectReferenceValue = gorev);

            GameObject menuGo = Kok("Duraklatma Menusu") ?? new GameObject("Duraklatma Menusu");
            Lab7Arayuz.DuraklatmaMenusuKur(menuGo);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.Exception($"Saving {ScenePath} failed.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            string mesaj = $"Created {ScenePath} from {Lab8SceneBuilder.ScenePath}: pass {H.Gecit.Uzunluk:F0} m, {H.SeritZ.Length} lanes, " +
                           $"{H.GemiSayisi} ships; appended to Build Settings.";
            return $"{mesaj} {Lab9Menu.Uygula()}";   // opens AnaMenu
        }

        static T Yukle<T>(string yol) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(yol) ?? throw new System.Exception($"Asset missing: {yol}");

        static void Ayarla(Object o, System.Action<SerializedObject> ayarla)
        {
            var so = new SerializedObject(o);
            ayarla(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================== world

        /// <summary>The sea: a 30 km plane with the ripple material, and a thin collider at sea level on the "Arazi" layer
        /// (rounds and bombs strike the water, the ground probes and the flight assist see it as the ground).</summary>
        static void DenizKur(int arazi)
        {
            var su = GameObject.CreatePrimitive(PrimitiveType.Plane);
            su.name = "Deniz";
            Object.DestroyImmediate(su.GetComponent<Collider>());
            su.transform.position = new Vector3(0f, H.DenizSeviyesi, 0f);
            su.transform.localScale = new Vector3(3000f, 1f, 3000f);
            var r = su.GetComponent<MeshRenderer>();
            r.sharedMaterial = Yukle<Material>($"{Lab9Varliklar.Malzemeler}/Deniz.mat");
            r.shadowCastingMode = ShadowCastingMode.Off;
            su.AddComponent<SuYuzeyi>();

            var yuzey = new GameObject("Deniz Yuzeyi");
            yuzey.layer = arazi;
            yuzey.transform.SetParent(su.transform.parent, false);
            var kutu = yuzey.AddComponent<BoxCollider>();
            kutu.center = new Vector3(0f, H.DenizSeviyesi - 0.5f, 0f);
            kutu.size = new Vector3(30000f, 1f, 30000f);
        }

        /// <summary>Steel beach obstacles in two staggered rows along the waterline, clear of the four landing lanes.</summary>
        static void KumsalKur()
        {
            var m = new Lab9Varliklar.Mat();
            var kok = new GameObject("Kumsal Engelleri");
            var rnd = new System.Random(55);
            int sira = 0;
            for (float z = -700f; z <= 700f; z += 24f)
            {
                if (H.SeritZ.Any(s => Mathf.Abs(z - s) < 45f)) continue;
                float ds = -18f - (sira++ % 2) * 14f;
                float x = H.KiyiX(z) + ds;
                Vector3 p = new Vector3(x, H.Yukseklik(x, z), z);
                Lab9Varliklar.Engel(kok.transform, m.Engel, p, (float)rnd.NextDouble() * 120f);
            }
        }

        // ================================================================== aircraft

        static Makineli UcagiKur(GameObject ucakGo, UcusKontrol ucus)
        {
            Vector3 ileri = H.BaslangicYonu;
            ucakGo.transform.SetPositionAndRotation(H.BaslangicNoktasi, Quaternion.LookRotation(ileri, Vector3.up));
            Ayarla(ucus, so =>
            {
                so.FindProperty("baslangicHizi").floatValue = BaslangicHizi;
                so.FindProperty("baslangicGazi").floatValue = BaslangicGazi;
                so.FindProperty("arkadAyar.sinirYaricapi").floatValue = H.SinirYaricapi;
                so.FindProperty("arkadAyar.haritaMerkezi").vector3Value = H.HaritaMerkezi;
            });
            Ayarla(ucakGo.GetComponent<BombaYuku>(), so =>
            {
                so.FindProperty("kapasite").intValue = BombaKapasitesi;
                so.FindProperty("birakmaTavani").floatValue = H.BombaTavani;
            });
            // No airfield in Bölüm 5: the easy-flight runway rule is parked in a far corner of the mountains.
            Ayarla(ucakGo.GetComponent<UcusYardimcisi>(), so =>
            {
                so.FindProperty("pistBasi").vector3Value = new Vector3(-3700f, 0f, -3700f);
                so.FindProperty("pistSonu").vector3Value = new Vector3(-3690f, 0f, -3700f);
                so.FindProperty("haritaMerkezi").vector3Value = H.HaritaMerkezi;
                so.FindProperty("sinirYaricapi").floatValue = H.SinirYaricapi;
            });
            return ucakGo.GetComponent<Makineli>() ?? throw new System.Exception("Makineli missing on the Bölüm 4 aircraft.");
        }

        /// <summary>Six instances of Bölüm 4's enemy aircraft prefab, inactive, handed to <see cref="KorumaUcuslari"/>.</summary>
        static KorumaUcuslari KorumaKur(UcusKontrol ucus)
        {
            var on = Yukle<GameObject>(Lab8Dusmanlar.DusmanUcagiYolu);
            var go = new GameObject("Koruma Ucuslari");
            var liste = new List<DusmanUcagi>();
            for (int i = 0; i < H.KorumaToplam; i++)
            {
                var u = (GameObject)PrefabUtility.InstantiatePrefab(on, go.transform);
                u.name = $"Koruma Ucagi {i + 1}";
                u.transform.position = H.KorumaGirisleri[i % H.KorumaGirisleri.Length];
                Ayarla(u.GetComponent<DusmanUcagi>(), so => so.FindProperty("tohum").intValue = 300 + i);
                Ayarla(u.GetComponent<Susturulabilir>(), so => so.FindProperty("serbestHedef").boolValue = true);
                u.SetActive(false);
                liste.Add(u.GetComponent<DusmanUcagi>());
            }
            var k = go.AddComponent<KorumaUcuslari>();
            Ayarla(k, so =>
            {
                so.FindProperty("ucus").objectReferenceValue = ucus;
                Lab7Yapilar.Dizi(so.FindProperty("ucaklar"), liste);
            });
            return k;
        }

        // ================================================================== HUD

        public const string TusYazisi =
            "W/S burun · A/D dönüş · ↑/↓ gaz · F fren · K uçuş modu · Ctrl/sol tık makineli · Space bomba · T hedef · O atış yardımı · M GPS · C kıyı kamerası · Esc menü";

        static Text Yazi(string ad, RectTransform ust, Font font, int boy, TextAnchor hiza, Vector2 capa, Vector2 konum, Vector2 boyut)
        {
            var go = new GameObject(ad, typeof(RectTransform), typeof(Text), typeof(Outline));
            var rt = (RectTransform)go.transform;
            rt.SetParent(ust, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = capa;
            rt.anchoredPosition = konum;
            rt.sizeDelta = boyut;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = boy;
            t.alignment = hiza;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = go.GetComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.75f);
            o.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        static Bolum5Hud HudKur(UcusKontrol ucus, BombaYuku yuk, Makineli makineli, CikarmaFilosu filo, KorumaUcuslari koruma, Bolum4Nisan nisan)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var kok = new GameObject("Bolum5 HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            kok.layer = LayerMask.NameToLayer("UI");
            var canvas = kok.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var olcek = kok.GetComponent<CanvasScaler>();
            olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            olcek.referenceResolution = new Vector2(1920f, 1080f);
            olcek.matchWidthOrHeight = 0.5f;
            var rt = (RectTransform)kok.transform;

            var isaretKoku = new GameObject("Hedef Isaretleri", typeof(RectTransform)).GetComponent<RectTransform>();
            isaretKoku.SetParent(rt, false);
            isaretKoku.anchorMin = Vector2.zero;
            isaretKoku.anchorMax = Vector2.one;
            isaretKoku.offsetMin = isaretKoku.offsetMax = Vector2.zero;
            var (_, _, _, hizMetni, gazMetni) = Lab7Arayuz.GostergeleriKur(rt, ucus);

            Text durum = Yazi("Durum", rt, font, 28, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(760f, 200f));
            durum.lineSpacing = 1.1f;
            Text gorev = Yazi("Gorev", rt, font, 34, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(1000f, 50f));
            Text bildirim = Yazi("Bildirim", rt, font, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1400f, 60f));
            bildirim.color = new Color(1f, 0.85f, 0.35f);
            Text ucusMetni = Yazi("Ucus", rt, font, 24, TextAnchor.LowerLeft, Vector2.zero, Lab7Arayuz.UcusMetniKonumu, new Vector2(700f, 40f));
            Text tuslar = Yazi("Tuslar", rt, font, 20, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1400f, 30f));
            tuslar.text = TusYazisi;
            tuslar.color = new Color(1f, 1f, 1f, 0.8f);
            Text uyari = Yazi("Uyari", rt, font, 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(1600f, 44f));
            uyari.color = new Color(1f, 0.45f, 0.25f);
            Text halka = Yazi("Oncu Halka", isaretKoku, font, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 50f));
            halka.text = "( + )";
            halka.gameObject.SetActive(false);

            var son = new GameObject("Son Paneli", typeof(RectTransform), typeof(Image));
            var srt = (RectTransform)son.transform;
            srt.SetParent(rt, false);
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(980f, 500f);
            son.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 0.8f);
            son.GetComponent<Image>().raycastTarget = false;
            Text sonMetni = Yazi("Metin", srt, font, 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 470f));
            sonMetni.lineSpacing = 1.15f;
            son.SetActive(false);

            GpsKur(rt, font, ucus, filo, koruma, nisan);
            KiyiKamerasiKur(rt, font, ucus, filo);

            var hud = kok.AddComponent<Bolum5Hud>();
            Ayarla(hud, so =>
            {
                so.FindProperty("ucus").objectReferenceValue = ucus;
                so.FindProperty("ucakSagligi").objectReferenceValue = ucus.GetComponent<Saglik>();
                so.FindProperty("yuk").objectReferenceValue = yuk;
                so.FindProperty("makineli").objectReferenceValue = makineli;
                so.FindProperty("filo").objectReferenceValue = filo;
                so.FindProperty("koruma").objectReferenceValue = koruma;
                so.FindProperty("nisan").objectReferenceValue = nisan;
                so.FindProperty("durumMetni").objectReferenceValue = durum;
                so.FindProperty("gorevMetni").objectReferenceValue = gorev;
                so.FindProperty("bildirimMetni").objectReferenceValue = bildirim;
                so.FindProperty("ucusMetni").objectReferenceValue = ucusMetni;
                so.FindProperty("hizMetni").objectReferenceValue = hizMetni;
                so.FindProperty("gazMetni").objectReferenceValue = gazMetni;
                so.FindProperty("uyariMetni").objectReferenceValue = uyari;
                so.FindProperty("isaretKoku").objectReferenceValue = isaretKoku;
                so.FindProperty("oncuHalka").objectReferenceValue = halka;
                so.FindProperty("sonPaneli").objectReferenceValue = son;
                so.FindProperty("sonMetni").objectReferenceValue = sonMetni;
                so.FindProperty("font").objectReferenceValue = font;
            });
            return hud;
        }

        /// <summary>The beach camera window (top right): a frame, the live picture and a caption; the camera beside the
        /// landing is a separate object rendering into a texture (no audio listener).</summary>
        static void KiyiKamerasiKur(RectTransform ust, Font font, UcusKontrol ucus, CikarmaFilosu filo)
        {
            var panel = new GameObject("Kiyi Kamerasi Paneli", typeof(RectTransform), typeof(Image));
            var prt = (RectTransform)panel.transform;
            prt.SetParent(ust, false);
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 1f);
            prt.anchoredPosition = new Vector2(-24f, -24f);
            prt.sizeDelta = new Vector2(500f, 318f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            panel.GetComponent<Image>().raycastTarget = false;
            var ekran = new GameObject("Goruntu", typeof(RectTransform), typeof(RawImage));
            var ert = (RectTransform)ekran.transform;
            ert.SetParent(prt, false);
            ert.anchorMin = ert.anchorMax = ert.pivot = new Vector2(0.5f, 1f);
            ert.anchoredPosition = new Vector2(0f, -6f);
            ert.sizeDelta = new Vector2(488f, 274.5f);
            ekran.GetComponent<RawImage>().raycastTarget = false;
            Text yazi = Yazi("Yazi", prt, font, 17, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 19f), new Vector2(490f, 26f));
            yazi.color = new Color(1f, 0.85f, 0.4f);

            var camGo = new GameObject("Kiyi Kamerasi");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 4000f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.depth = -5f;
            cam.enabled = false;
            var ek = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            ek.renderPostProcessing = true;
            var kk = camGo.AddComponent<KiyiKamerasi>();
            Ayarla(kk, so =>
            {
                so.FindProperty("filo").objectReferenceValue = filo;
                so.FindProperty("ucus").objectReferenceValue = ucus;
                so.FindProperty("kamera").objectReferenceValue = cam;
                so.FindProperty("panel").objectReferenceValue = panel;
                so.FindProperty("ekran").objectReferenceValue = ekran.GetComponent<RawImage>();
                so.FindProperty("yazi").objectReferenceValue = yazi;
            });
            panel.SetActive(false);
        }

        static void GpsKur(RectTransform ust, Font font, UcusKontrol ucus, CikarmaFilosu filo, KorumaUcuslari koruma, Bolum4Nisan nisan)
        {
            var panel = new GameObject("GPS", typeof(RectTransform), typeof(Image));
            var prt = (RectTransform)panel.transform;
            prt.SetParent(ust, false);
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 0f);
            prt.anchoredPosition = new Vector2(-24f, 60f);
            prt.sizeDelta = new Vector2(352f, 392f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            panel.GetComponent<Image>().raycastTarget = false;
            var harita = new GameObject("Harita", typeof(RectTransform), typeof(RawImage));
            var hrt = (RectTransform)harita.transform;
            hrt.SetParent(prt, false);
            hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0f);
            hrt.anchoredPosition = new Vector2(0f, 16f);
            hrt.sizeDelta = new Vector2(320f, 320f);
            harita.GetComponent<RawImage>().raycastTarget = false;
            var isaret = new GameObject("Isaretler", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            isaret.SetParent(hrt, false);
            isaret.anchorMin = Vector2.zero;
            isaret.anchorMax = Vector2.one;
            isaret.offsetMin = isaret.offsetMax = Vector2.zero;
            Text bilgi = Yazi("Bilgi", prt, font, 17, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(340f, 30f));
            var gps = panel.AddComponent<Bolum5Gps>();
            Ayarla(gps, so =>
            {
                so.FindProperty("panel").objectReferenceValue = prt;
                so.FindProperty("harita").objectReferenceValue = harita.GetComponent<RawImage>();
                so.FindProperty("isaretKoku").objectReferenceValue = isaret;
                so.FindProperty("bilgi").objectReferenceValue = bilgi;
                so.FindProperty("ucus").objectReferenceValue = ucus;
                so.FindProperty("filo").objectReferenceValue = filo;
                so.FindProperty("koruma").objectReferenceValue = koruma;
                so.FindProperty("nisan").objectReferenceValue = nisan;
                so.FindProperty("merkez").vector3Value = H.HaritaMerkezi;
                so.FindProperty("yariBoy").floatValue = H.SinirYaricapi;
            });
        }
    }
}
