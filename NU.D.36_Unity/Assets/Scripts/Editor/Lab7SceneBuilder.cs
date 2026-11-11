using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 7 (Hafta 7): the night mission of Bölüm 3. Rota07 = copy of Bolum3_GriKutu ("File → Save As"); the grey box, the
    /// switched-off Bölüm 2 objects and the old HUD are removed from the copy, and the real level is built from
    /// <see cref="GeceHaritasi"/>, which places the Hafta 6 paper map (<see cref="Bolum3Harita"/> v0.1, 1 square = 200 m) in
    /// the night world: terrain with forests, the river and its wide reach, the airfield on row 8, the H5–J5 bridge (the
    /// target), the searchlight towers F4/K5/H7 (optional targets), the AA guns G4/K4/I7 with tracer rounds, the sound
    /// locator H3, the clouds of the single rule (E5–G5), villages, moonlight, fog, night sky, dawn, post-processing, the
    /// aircraft's lights and aerodynamic flight model, the night HUD with gauges and the pause menu.
    /// Earlier scenes stay untouched; an existing Rota07 is never overwritten.
    /// Batch mode: -executeMethod NU36.Editor.Lab7SceneBuilder.Create
    /// </summary>
    public static class Lab7SceneBuilder
    {
        public const string SceneName = "Rota07";
        public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
        public const string VolumeProfilePath = Lab7Varliklar.Klasor + "/GeceVolume.asset";
        public static readonly string[] SilinenKokler = { "GriKutu", "Kronometre", "Çakıl Burnu Arazisi", "Tepeler", "Bolum2", "Seviye", "Us", "HUD" };

        public static readonly Color SisRengi = new Color(0.075f, 0.09f, 0.12f);
        public const float SisYogunlugu = 0.00055f;
        public static readonly Vector3 AyIsigiAcisi = new Vector3(32f, 205f, 0f);
        public const int BombaKapasitesi = 4;
        /// <summary>Seconds from the start to dawn (paper: "12. tur şafak"; see Lab7_Implementation.md §7 for the Unity value).</summary>
        public const float SafakSuresi = 480f;

        // Flight model values (aerodynamic mode); see Lab7_Implementation.md.
        public const float YunuslamaHizi = 45f, YatisHizi = 110f, SapmaHizi = 25f, YatisDengeleme = 1.2f, GazDegisimHizi = 0.6f;
        // Landing limits for this aircraft.
        public const float InisEnFazlaHiz = 60f, InisPitch = 16f, InisYatis = 15f, InisEgim = 12f, InisDusus = 4.5f;

        /// <summary>
        /// Development helper: deletes Rota07 and the generated Lab 7 materials, prefabs, meshes and volume profile (never the
        /// earlier scenes or the textures, unless <paramref name="dokular"/>), then builds the scene again.
        /// Batch mode: -executeMethod NU36.Editor.Lab7SceneBuilder.YenidenKur
        /// </summary>
        [MenuItem("NU36/Lab 7/Rebuild Rota07 (delete and create)")]
        public static void YenidenKur() => YenidenKur(false);

        public static void YenidenKurDokularla() => YenidenKur(true);

        static void YenidenKur(bool dokular)
        {
            AssetDatabase.DeleteAsset(ScenePath);
            foreach (string k in new[] { Lab7Varliklar.Malzemeler, Lab7Varliklar.Prefablar, Lab7Varliklar.Meshler })
                AssetDatabase.DeleteAsset(k);
            AssetDatabase.DeleteAsset(VolumeProfilePath);
            if (dokular)
                AssetDatabase.DeleteAsset(Lab7Varliklar.Dokular);
            AssetDatabase.Refresh();
            Create();
        }

        [MenuItem("NU36/Lab 7/Create Rota07 Scene")]
        public static void Create()
        {
            bool ok;
            string message;
            try { message = TryCreate(); ok = true; }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab7] {message}");
            else Debug.LogError($"[NU36 Lab7] {message}");
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
            int ucakK = Katman("Ucak"), arazi = Katman("Arazi"), yapi = Katman("Yapi"), hedef = Katman("Hedef"), mermi = Katman("Mermi");

            // ---- assets (before the scene is opened) -------------------------------------------------------
            var m = new Lab7Yapilar.Mat();
            Material araziMat = Lab7Varliklar.AraziMalzemesi();
            Material agacMat = Lab7Varliklar.Lit("Agac", Color.white, 0.05f, null, Lab7Varliklar.AgacPaleti());
            GameObject yangin = Lab7Varliklar.YanginPrefab();
            Material bombaMat = AssetDatabase.LoadAssetAtPath<Material>(Lab5SceneBuilder.Folder + "/Materials/Bomba.mat");
            Bomba bombaOn = Lab7Varliklar.BombaPrefab((1 << arazi) | (1 << yapi) | (1 << hedef), (1 << hedef) | (1 << yapi) | (1 << ucakK), bombaMat);
            Mermi mermiOn = AssetDatabase.LoadAssetAtPath<Mermi>(Lab5SceneBuilder.MermiPrefabPath);
            if (mermiOn == null)
                throw new System.Exception("Lab 5 Mermi prefab missing.");
            Lab7Varliklar.IzMermisiPrefab(mermiOn);
            Material bulutMat = Lab7Varliklar.BulutMalzemesi();
            Vector3 ayYonu = -(Quaternion.Euler(AyIsigiAcisi) * Vector3.forward);
            Material gok = Lab7Varliklar.GokyuzuMalzemesi(ayYonu, SisRengi);
            VolumeProfile profil = VolumeProfili();

            // ---- scene -----------------------------------------------------------------------------------------
            if (!AssetDatabase.CopyAsset(Lab6SceneBuilder.ScenePath, ScenePath))
                throw new System.Exception($"Copying {Lab6SceneBuilder.ScenePath} to {ScenePath} failed.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject Kok(string ad) => scene.GetRootGameObjects().SingleOrDefault(g => g.name == ad);
            foreach (string ad in SilinenKokler)
                if (Kok(ad) != null)
                    Object.DestroyImmediate(Kok(ad));
            // Component references to prefab assets do not survive opening the scene (the assets are unloaded): load them again.
            bombaOn = AssetDatabase.LoadAssetAtPath<Bomba>(Lab7Varliklar.Prefablar + "/Bomba.prefab");
            mermiOn = AssetDatabase.LoadAssetAtPath<Mermi>(Lab7Varliklar.IzMermisiYolu);   // tracer rounds
            if (bombaOn == null || mermiOn == null)
                throw new System.Exception("Bomba/IzMermisi prefab could not be loaded after opening the scene.");
            GameObject ucakGo = scene.GetRootGameObjects().Single(g => g.GetComponent<UcusKontrol>() != null);
            var ucus = ucakGo.GetComponent<UcusKontrol>();

            // Terrain + forest (rebuilt at runtime from the height function), water.
            var araziGo = new GameObject("Gece Arazisi");
            araziGo.layer = arazi;
            var ga = araziGo.AddComponent<GeceArazisi>();
            var gso = new SerializedObject(ga);
            gso.FindProperty("araziMalzemesi").objectReferenceValue = araziMat;
            gso.FindProperty("agacMalzemesi").objectReferenceValue = agacMat;
            gso.ApplyModifiedPropertiesWithoutUndo();

            GameObject su = GameObject.CreatePrimitive(PrimitiveType.Plane);
            su.name = "Nehir Suyu";
            Object.DestroyImmediate(su.GetComponent<Collider>());
            su.transform.localScale = new Vector3(GeceHaritasi.Boyut / 10f, 1f, GeceHaritasi.Boyut / 10f);
            su.transform.position = new Vector3(0f, GeceHaritasi.SuSeviyesi, 0f);
            su.GetComponent<MeshRenderer>().sharedMaterial = Lab7Varliklar.SuMalzemesi();
            su.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var suYuzeyi = new GameObject("Su Yuzeyi (zemin)");
            suYuzeyi.transform.SetParent(su.transform, false);
            suYuzeyi.layer = arazi;
            var suKutu = suYuzeyi.AddComponent<BoxCollider>();
            suKutu.center = new Vector3(0f, -0.1f / 1f, 0f);
            suKutu.size = new Vector3(10f, 0.2f, 10f);   // plane is 10 × 10 before scaling

            // Airfield.
            Lab7Yapilar.Havaalani havaalani = Lab7Yapilar.HavaalaniKur(m, arazi, yapi);

            // The paper map's defences and targets: the sound locator (H3), the three searchlight towers (F4, K5, H7; optional
            // targets), the bridge (H5–J5; the target), the AA guns (G4, K4, I7) and the clouds of the single rule (E5–G5).
            var savunma = new GameObject("Savunma");
            SesDinleyici kulak = Lab7Yapilar.SesDinleyiciKur(m, ucus, yapi);
            kulak.transform.SetParent(savunma.transform, true);
            var hedefler = new List<Yikilabilir>();
            var isildaklar = new List<Isildak>();
            var hedefKok = new GameObject("Hedefler");
            for (int i = 0; i < GeceHaritasi.IsildakSayisi; i++)
            {
                Yikilabilir kule = Lab7Yapilar.KuleKur(i, m, yangin, ucakGo.transform, kulak, hedef, yapi, arazi);
                kule.transform.SetParent(hedefKok.transform, true);
                hedefler.Add(kule);
                isildaklar.Add(kule.Isildak);
            }
            Yikilabilir kopru = Lab7Yapilar.KopruKur(m, yangin, hedef, yapi, suKutu);
            kopru.transform.SetParent(hedefKok.transform, true);
            hedefler.Add(kopru);

            // Enemy AA guns: aimed fire while a searchlight holds the aircraft (or within 250 m), fire at the heard point while
            // the locator hears it; every round is a tracer.
            for (int i = 0; i < Bolum3Harita.Ucaksavarlar.Length; i++)
                Lab7Yapilar.UcaksavarKur(i, m, ucus, mermiOn, isildaklar.ToArray(), kulak, yapi, (1 << arazi) | (1 << yapi), (1 << ucakK) | (1 << arazi))
                    .transform.SetParent(savunma.transform, true);

            var bulutlar = new GameObject("Bulutlar (tek kural)");
            foreach (string kare in Bolum3Harita.Bulutlar)
                Lab7Yapilar.BulutKur(kare, bulutMat).transform.SetParent(bulutlar.transform, true);

            var koyler = new GameObject("Koyler");
            for (int k = 0; k < GeceHaritasi.Koyler.Length; k++)
                Lab7Yapilar.KoyKur(k, m, yapi).transform.SetParent(koyler.transform, true);

            // ---- aircraft ------------------------------------------------------------------------------------
            UcagiKur(ucakGo, ucus, bombaOn, m, arazi, yapi, hedef);

            // ---- mission, lights, camera, HUD ----------------------------------------------------------------
            var gorevGo = new GameObject("Gorev");
            var gorev = gorevGo.AddComponent<GeceGorevi>();
            var so = new SerializedObject(gorev);
            Lab7Yapilar.Dizi(so.FindProperty("hedefler"), hedefler.Cast<Object>().ToList());
            so.FindProperty("yuk").objectReferenceValue = ucakGo.GetComponent<BombaYuku>();
            so.FindProperty("ucakSagligi").objectReferenceValue = ucakGo.GetComponent<Saglik>();
            so.FindProperty("algilayici").objectReferenceValue = ucakGo.GetComponent<InisAlgilayici>();
            so.FindProperty("ucus").objectReferenceValue = ucus;
            so.FindProperty("inisBolgesi").objectReferenceValue = havaalani.InisBolgesi;
            so.FindProperty("pistIsiklari").objectReferenceValue = havaalani.PistIsiklari;
            so.FindProperty("anaHedef").objectReferenceValue = kopru;
            so.FindProperty("safakSuresi").floatValue = SafakSuresi;
            so.ApplyModifiedPropertiesWithoutUndo();
            gorevGo.AddComponent<GeceSonuEkrani>().Gorev = gorev;
            havaalani.IkmalTetigi.gameObject.AddComponent<IkmalAlani>().Gorev = gorev;

            GameObject ayGo = Kok("Directional Light");
            AydinlatmaKur(ayGo, gok, profil);
            var safak = gorevGo.AddComponent<Safak>();
            var sso = new SerializedObject(safak);
            sso.FindProperty("gorev").objectReferenceValue = gorev;
            sso.FindProperty("ay").objectReferenceValue = ayGo.GetComponent<Light>();
            sso.ApplyModifiedPropertiesWithoutUndo();
            Camera cam = Kok("Main Camera").GetComponent<Camera>();
            KameraKur(cam);
            HudKur(gorev, cam, ucakGo.GetComponent<UcakIsiklari>(), isildaklar.ToArray(), kulak, ucakGo.GetComponent<BombaYuku>());
            Lab7Arayuz.Uygula(scene);   // airspeed dial, throttle lever, pause menu (2026-11-10)
            Lab7Yardim.Uygula(scene);   // easy flight, brake, targeting, GPS (2026-11-10)

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.Exception($"Saving {ScenePath} failed.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            return $"Created {ScenePath} from {Lab6SceneBuilder.ScenePath} (Bölüm 3 paper map v{Bolum3Harita.Surum}: bridge + {isildaklar.Count} searchlights) and appended it to Build Settings.";
        }

        // ================================================================== aircraft

        static void UcagiKur(GameObject ucakGo, UcusKontrol ucus, Bomba bombaOn, Lab7Yapilar.Mat m, int arazi, int yapi, int hedef)
        {
            var algilayici = ucakGo.GetComponent<InisAlgilayici>();
            float teker = algilayici.TekerOfseti;
            Vector3 bas = GeceHaritasi.PistBasi + GeceHaritasi.PistEkseni * 70f;
            ucakGo.transform.SetPositionAndRotation(new Vector3(bas.x, GeceHaritasi.PistYuksekligi + 0.3f + teker, bas.z),
                Quaternion.Euler(0f, GeceHaritasi.PistYonu, 0f));

            var so = new SerializedObject(ucus);
            so.FindProperty("aerodinamik").boolValue = true;
            so.FindProperty("baslangicHizi").floatValue = 0f;
            so.FindProperty("baslangicGazi").floatValue = 0f;
            so.FindProperty("yunuslamaHizi").floatValue = YunuslamaHizi;
            so.FindProperty("yatisHizi").floatValue = YatisHizi;
            so.FindProperty("sapmaHizi").floatValue = SapmaHizi;
            so.FindProperty("enFazlaYatis").floatValue = 0f;
            so.FindProperty("yatisDengeleme").floatValue = YatisDengeleme;
            so.FindProperty("gazDegisimHizi").floatValue = GazDegisimHizi;
            so.ApplyModifiedPropertiesWithoutUndo();

            var aso = new SerializedObject(algilayici);
            aso.FindProperty("yerdeBasla").boolValue = true;
            aso.FindProperty("enFazlaHiz").floatValue = InisEnFazlaHiz;
            aso.FindProperty("enFazlaPitchHatasi").floatValue = InisPitch;
            aso.FindProperty("enFazlaYatis").floatValue = InisYatis;
            aso.FindProperty("enFazlaEgim").floatValue = InisEgim;
            aso.FindProperty("enFazlaDususHizi").floatValue = InisDusus;
            aso.ApplyModifiedPropertiesWithoutUndo();

            var carp = ucakGo.AddComponent<CarpismaAlgilayici>();
            carp.Engeller = (1 << yapi) | (1 << hedef);

            var yuk = ucakGo.GetComponent<BombaYuku>();
            var yso = new SerializedObject(yuk);
            yso.FindProperty("bombaOn").objectReferenceValue = bombaOn;
            yso.FindProperty("kapasite").intValue = BombaKapasitesi;
            yso.ApplyModifiedPropertiesWithoutUndo();

            // Lights: landing light in the nose, navigation lights on the wing tips and the tail (from the model's bounds).
            var renderers = ucakGo.GetComponentsInChildren<MeshRenderer>();
            bool ilk = true;
            var b = new Bounds();
            foreach (MeshRenderer r in renderers)
                foreach (Vector3 kose in Koseler(r.bounds))
                {
                    Vector3 y = ucakGo.transform.InverseTransformPoint(kose);
                    if (ilk) { b = new Bounds(y, Vector3.zero); ilk = false; }
                    else b.Encapsulate(y);
                }
            Transform isiklar = Lab7Yapilar.Grup("Isiklar", ucakGo.transform);
            Vector3 burun = new Vector3(0f, b.center.y - 0.2f, b.max.z + 0.2f);
            Light fener = Lab7Yapilar.Isik("Inis Feneri", isiklar, LightType.Spot, new Color(1f, 0.95f, 0.86f), 9000f, 420f, burun,
                Quaternion.Euler(4f, 0f, 0f), 40f, 18f);
            var huzme = new GameObject("Fener Huzmesi", typeof(MeshFilter), typeof(MeshRenderer));
            huzme.transform.SetParent(isiklar, false);
            huzme.transform.localPosition = burun;
            huzme.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
            huzme.transform.localScale = Vector3.one * 140f;
            huzme.GetComponent<MeshFilter>().sharedMesh = Lab7Varliklar.HuzmeMeshi(13f);
            huzme.GetComponent<MeshRenderer>().sharedMaterial = m.FarHuzmesi;
            huzme.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Material kirmizi = Lab7Varliklar.Ampul("SeyirKirmizi", new Color(1f, 0.1f, 0.08f), 3.5f);
            Material yesil = Lab7Varliklar.Ampul("SeyirYesil", new Color(0.15f, 1f, 0.3f), 3.5f);
            Material beyaz = Lab7Varliklar.Ampul("SeyirBeyaz", new Color(1f, 1f, 1f), 5f);
            float kanatY = b.center.y, kanatZ = b.center.z + b.extents.z * 0.2f;
            Lab7Yapilar.Ampul("Seyir Sol (kirmizi)", isiklar, kirmizi, new Vector3(b.min.x + 0.15f, kanatY, kanatZ), 0.28f);
            Lab7Yapilar.Ampul("Seyir Sag (yesil)", isiklar, yesil, new Vector3(b.max.x - 0.15f, kanatY, kanatZ), 0.28f);
            GameObject kuyruk = Lab7Yapilar.Ampul("Kuyruk (beyaz)", isiklar, beyaz, new Vector3(0f, b.center.y + 0.3f, b.min.z + 0.1f), 0.3f);
            foreach (Transform t in isiklar.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = ucakGo.layer;

            var ui = ucakGo.AddComponent<UcakIsiklari>();
            var uso = new SerializedObject(ui);
            uso.FindProperty("inisFeneri").objectReferenceValue = fener;
            uso.FindProperty("fenerHuzmesi").objectReferenceValue = huzme;
            uso.FindProperty("kuyrukIsigi").objectReferenceValue = kuyruk.GetComponent<MeshRenderer>();
            uso.ApplyModifiedPropertiesWithoutUndo();
        }

        static IEnumerable<Vector3> Koseler(Bounds b)
        {
            for (int i = 0; i < 8; i++)
                yield return b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
        }

        // ================================================================== lighting

        static void AydinlatmaKur(GameObject isikGo, Material gok, VolumeProfile profil)
        {
            isikGo.name = "Ay Isigi";
            var ay = isikGo.GetComponent<Light>();
            isikGo.transform.rotation = Quaternion.Euler(AyIsigiAcisi);
            ay.color = new Color(0.62f, 0.70f, 0.92f);
            ay.intensity = 0.8f;
            ay.shadows = LightShadows.Soft;
            ay.shadowStrength = 0.75f;

            RenderSettings.skybox = gok;
            RenderSettings.sun = ay;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.19f, 0.22f, 0.32f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.135f, 0.17f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = SisYogunlugu;
            RenderSettings.fogColor = SisRengi;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;

            var vol = new GameObject("Gece Volume");
            vol.AddComponent<GeceOrtami>();
            var v = vol.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 1f;
            v.sharedProfile = profil;
        }

        static VolumeProfile VolumeProfili()
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (mevcut != null) return mevcut;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, VolumeProfilePath);
            var bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(1.4f);
            bloom.scatter.Override(0.72f);
            var ton = p.Add<Tonemapping>(true);
            ton.mode.Override(TonemappingMode.ACES);
            var renk = p.Add<ColorAdjustments>(true);
            renk.postExposure.Override(0.45f);
            renk.contrast.Override(12f);
            renk.saturation.Override(-10f);
            var vin = p.Add<Vignette>(true);
            vin.intensity.Override(0.32f);
            vin.smoothness.Override(0.45f);
            var tane = p.Add<FilmGrain>(true);
            tane.type.Override(FilmGrainLookup.Thin1);
            tane.intensity.Override(0.18f);
            foreach (VolumeComponent c in p.components)
            {
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, p);
            }
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return p;
        }

        static void KameraKur(Camera cam)
        {
            cam.farClipPlane = 4800f;
            cam.nearClipPlane = 0.5f;
            cam.clearFlags = CameraClearFlags.Skybox;
            var ek = cam.GetComponent<UniversalAdditionalCameraData>();
            if (ek == null)
                ek = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            ek.renderPostProcessing = true;
            ek.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        // ================================================================== HUD

        static void HudKur(GeceGorevi gorev, Camera kamera, UcakIsiklari isiklar, Isildak[] isildaklar, SesDinleyici kulak, BombaYuku yuk)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var kok = new GameObject("Gece HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = kok.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var olcek = kok.GetComponent<CanvasScaler>();
            olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            olcek.referenceResolution = new Vector2(1920f, 1080f);
            olcek.matchWidthOrHeight = 0.5f;
            var rt = (RectTransform)kok.transform;

            var isaretler = Rt("Isaretler", rt);
            Doldur(isaretler);
            Text hedefler = Yazi("Hedefler", rt, font, 28, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(34f, -26f), new Vector2(460f, 240f));
            Text gorevYazi = Yazi("Gorev", rt, font, 32, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1300f, 50f));
            Text bildirim = Yazi("Bildirim", rt, font, 26, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(1300f, 80f));
            bildirim.color = new Color(0.75f, 0.95f, 1f, 1f);
            Text ucus = Yazi("Ucus", rt, font, 28, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(34f, 30f), new Vector2(520f, 260f));
            Text uyari = Yazi("Uyari", rt, font, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1300f, 110f));
            uyari.color = new Color(1f, 0.3f, 0.22f, 1f);
            Text tuslar = Yazi("Tuslar", rt, font, 20, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-30f, 26f), new Vector2(900f, 60f));
            tuslar.text = "W/S burun   A/D yatış   Q/E dümen   ↑/↓ gaz   Space bomba   L fener   R yeniden (görev sonunda)";
            tuslar.color = new Color(0.85f, 0.85f, 0.8f, 0.7f);

            var hud = kok.AddComponent<GeceHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("gorev").objectReferenceValue = gorev;
            so.FindProperty("kamera").objectReferenceValue = kamera;
            so.FindProperty("isiklar").objectReferenceValue = isiklar;
            so.FindProperty("kulak").objectReferenceValue = kulak;
            var dizi = so.FindProperty("isildaklar");
            dizi.arraySize = isildaklar.Length;
            for (int i = 0; i < isildaklar.Length; i++)
                dizi.GetArrayElementAtIndex(i).objectReferenceValue = isildaklar[i];
            so.FindProperty("hedefMetni").objectReferenceValue = hedefler;
            so.FindProperty("gorevMetni").objectReferenceValue = gorevYazi;
            so.FindProperty("bildirimMetni").objectReferenceValue = bildirim;
            so.FindProperty("ucusMetni").objectReferenceValue = ucus;
            so.FindProperty("uyariMetni").objectReferenceValue = uyari;
            so.FindProperty("isaretKatmani").objectReferenceValue = isaretler;
            so.FindProperty("yazi").objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Dive panel and bomb aiming ring (the Lab 5 dive indicator).
            var panel = Rt("DalisGostergesi", rt);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
            panel.sizeDelta = new Vector2(300f, 110f);
            panel.anchoredPosition = new Vector2(-30f, -26f);
            var cerceve = panel.gameObject.AddComponent<Image>();
            cerceve.raycastTarget = false;
            var cizgi = panel.gameObject.AddComponent<Outline>();
            cizgi.effectColor = new Color(0.93f, 0.85f, 0.65f, 0.7f);
            cizgi.effectDistance = new Vector2(2f, -2f);
            Text aci = Yazi("Aci", panel, font, 32, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(16f, -8f), new Vector2(280f, 46f));
            Text irtifa = Yazi("Irtifa", panel, font, 32, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(16f, -56f), new Vector2(280f, 46f));
            var halka = Rt("NisanHalkasi", isaretler);
            halka.anchorMin = halka.anchorMax = new Vector2(0.5f, 0.5f);
            halka.sizeDelta = new Vector2(72f, 72f);
            var hi = halka.gameObject.AddComponent<Image>();
            hi.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            hi.raycastTarget = false;
            halka.gameObject.SetActive(false);
            var dg = panel.gameObject.AddComponent<DalisGostergesi>();
            var dso = new SerializedObject(dg);
            dso.FindProperty("yuk").objectReferenceValue = yuk;
            dso.FindProperty("kamera").objectReferenceValue = kamera;
            dso.FindProperty("cerceve").objectReferenceValue = cerceve;
            dso.FindProperty("aciMetni").objectReferenceValue = aci;
            dso.FindProperty("irtifaMetni").objectReferenceValue = irtifa;
            dso.FindProperty("halka").objectReferenceValue = halka;
            dso.ApplyModifiedPropertiesWithoutUndo();
            cerceve.color = dso.FindProperty("disaridaRenk").colorValue;
        }

        static RectTransform Rt(string ad, Transform ust)
        {
            var go = new GameObject(ad, typeof(RectTransform));
            go.transform.SetParent(ust, false);
            return (RectTransform)go.transform;
        }

        static void Doldur(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        static Text Yazi(string ad, RectTransform ust, Font font, int boyut, TextAnchor hiza, Vector2 capa, Vector2 konum, Vector2 alan)
        {
            RectTransform r = Rt(ad, ust);
            r.anchorMin = r.anchorMax = r.pivot = capa;
            r.sizeDelta = alan;
            r.anchoredPosition = konum;
            var t = r.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = boyut;
            t.fontStyle = FontStyle.Bold;
            t.alignment = hiza;
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
