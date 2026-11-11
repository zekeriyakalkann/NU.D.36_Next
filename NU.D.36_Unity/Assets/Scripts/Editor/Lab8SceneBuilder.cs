using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using H = NU36.KursunDaglariHaritasi;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8 (Hafta 8): Bölüm 4 · Kurşun Dağları. The new scene is a copy of Rota07 (worksheet step 02: "Bölüm 3'teki
    /// uçağını bu sahneye getir … kontroller ve kamera Bölüm 3'teki gibi"): the aircraft, its flight model, assists,
    /// camera and pause menu come over unchanged; the night world is removed and the mountain level is built:
    /// terrain, the railway (one line from the departure station to the safe zone), the viaduct, the airbase near the
    /// station, the supply train and the HUD. The aircraft starts in the air above the train.
    /// Phase 1 of Documentation/Lab8_Plan.md; enemies, weapons and the mission flow follow in later phases.
    /// Earlier scenes stay untouched; an existing scene is never overwritten.
    /// Batch mode: -executeMethod NU36.Editor.Lab8SceneBuilder.Create
    /// </summary>
    public static class Lab8SceneBuilder
    {
        public const string SceneName = "Bolum4_KursunDaglari";
        public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
        public const string VolumeYolu = Lab8Varliklar.Kok + "/GunduzVolume.asset";
        public static readonly string[] SilinenKokler =
            { "Havaalani", "Hedefler", "Gorev", "Bulutlar (tek kural)", "Gece Volume", "Savunma", "Nehir Suyu", "Gece HUD", "Koyler", "Gece Arazisi" };

        /// <summary>Start: 150 m above the line, 120 m behind the locomotive, heading along the line, 220 km/h.</summary>
        public const float BaslangicYuksekligi = 150f, BaslangicGerisi = 120f, BaslangicHizi = 61f, BaslangicGazi = 0.6f;
        public const int BombaKapasitesi = 2;
        public static readonly Vector3 GunesAcisi = new Vector3(38f, 140f, 0f);
        public static readonly Color SisRengi = new Color(0.69f, 0.75f, 0.83f);
        public const float SisYogunlugu = 0.00016f;

        [MenuItem("NU36/Lab 8/Rebuild Bolum4 (delete and create)")]
        public static void YenidenKur()
        {
            AssetDatabase.DeleteAsset(ScenePath);
            foreach (string k in new[] { Lab8Varliklar.Malzemeler, Lab8Varliklar.Prefablar, Lab8Varliklar.Meshler, Lab8Varliklar.AraziKlasoru, VolumeYolu })
                AssetDatabase.DeleteAsset(k);
            AssetDatabase.Refresh();
            Create();
        }

        [MenuItem("NU36/Lab 8/Create Bolum4 Scene")]
        public static void Create()
        {
            bool ok;
            string message;
            try { message = TryCreate(); ok = true; }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab8] {message}");
            else Debug.LogError($"[NU36 Lab8] {message}");
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
            int arazi = Katman("Arazi"), yapi = Katman("Yapi"), hedef = Katman("Hedef"), ucakK = Katman("Ucak");
            RayEgrisi egri = H.Egri;

            // ---- assets (before the scene is opened) -------------------------------------------------------
            var m = new Lab8Yapilar.Mat();
            Lab8Varliklar.TrenPrefablari(yapi);
            Material[] doga = Lab8Varliklar.DogaMalzemeleri();
            List<GameObject> agaclar = Lab8Varliklar.DogaPrefablari(Lab8Varliklar.Agaclar, doga);
            List<GameObject> kayalar = Lab8Varliklar.DogaPrefablari(Lab8Varliklar.Kayalar, doga);
            TerrainLayer[] katmanlar = Lab8Arazi.Katmanlar();
            var zaman = System.Diagnostics.Stopwatch.StartNew();
            Lab8Arazi.Sonuc arazisonuc = Lab8Arazi.Olustur(egri, katmanlar, agaclar.ToArray(), kayalar.ToArray());
            AssetDatabase.DeleteAsset(Lab8Arazi.VeriYolu);
            AssetDatabase.CreateAsset(arazisonuc.Veri, Lab8Arazi.VeriYolu);
            Debug.Log($"[NU36 Lab8] terrain built in {zaman.Elapsed.TotalSeconds:F1} s, {arazisonuc.Veri.treeInstanceCount} trees/rocks");
            VolumeProfile profil = VolumeProfili();
            Material[] rayMalzemesi = Lab8Varliklar.RayMalzemeleri();
            Lab8Dusmanlar.Kur(hedef, ucakK, arazi, yapi);
            AssetDatabase.SaveAssets();

            // ---- scene -----------------------------------------------------------------------------------------
            if (!AssetDatabase.CopyAsset(Lab7SceneBuilder.ScenePath, ScenePath))
                throw new System.Exception($"Copying {Lab7SceneBuilder.ScenePath} to {ScenePath} failed.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject Kok(string ad) => scene.GetRootGameObjects().SingleOrDefault(g => g.name == ad);
            foreach (string ad in SilinenKokler)
                if (Kok(ad) != null)
                    Object.DestroyImmediate(Kok(ad));
            m = new Lab8Yapilar.Mat();   // asset references do not survive opening the scene: load again
            rayMalzemesi = Lab8Varliklar.RayMalzemeleri();
            GameObject ucakGo = scene.GetRootGameObjects().Single(g => g.GetComponent<UcusKontrol>() != null);
            var ucus = ucakGo.GetComponent<UcusKontrol>();

            // Terrain.
            var veri = AssetDatabase.LoadAssetAtPath<TerrainData>(Lab8Arazi.VeriYolu);
            GameObject araziGo = Terrain.CreateTerrainGameObject(veri);
            araziGo.name = "Kursun Daglari Arazisi";
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
            float Zemin(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;

            Lab8Yapilar.SuKur(m);

            // Railway: the line, the bent rail chunks, the viaduct over the gorge, both stations, the base.
            var hatGo = new GameObject("Ray Hatti");
            var hat = hatGo.AddComponent<RayHatti>();
            hat.KontrolNoktalari = H.RayNoktalari();
            Lab8Yapilar.RayKur(egri, rayMalzemesi).SetParent(hatGo.transform, false);
            var (kopruBas, kopruSon) = Lab8Yapilar.KopruAraligi(egri, Zemin);
            Lab8Yapilar.ViyadukKur(egri, kopruBas, kopruSon, Zemin, m, yapi);
            Lab8Yapilar.IstasyonKur(egri, m, yapi);
            Lab8Yapilar.GuvenliBolgeKur(egri, m, yapi);
            Lab8Yapilar.UsKur(m, yapi);

            // Train: locomotive + 4 wagons from the prefabs, one health for the whole train.
            var trenGo = new GameObject("Tren");
            trenGo.layer = yapi;
            var trenSagligi = trenGo.AddComponent<Saglik>();
            trenSagligi.Taraf = Taraf.Dost;   // the player's rounds and bombs do not hurt the train
            var tren = trenGo.AddComponent<Tren>();
            var araclar = new List<TrenAraci>();
            foreach (var (ad, _) in Lab8Varliklar.Araclar)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{Lab8Varliklar.Prefablar}/Tren/{ad}.prefab");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(p, trenGo.transform);
                araclar.Add(inst.GetComponent<TrenAraci>());
            }
            var tso = new SerializedObject(tren);
            tso.FindProperty("hat").objectReferenceValue = hat;
            tso.FindProperty("saglik").objectReferenceValue = trenSagligi;
            Lab7Yapilar.Dizi(tso.FindProperty("araclar"), araclar);
            tso.ApplyModifiedPropertiesWithoutUndo();
            tren.BaslangicaKoy();

            Makineli makineli = UcagiKur(ucakGo, ucus, egri, arazi, yapi, hedef);

            // Light, sky, fog, post-processing, camera.
            GameObject gunesGo = Kok("Ay Isigi");
            AydinlatmaKur(gunesGo);
            // The scene is built without a lighting bake: recompute the ambient probe at start (Lab 7 D-080).
            new GameObject("Ortam Isigi").AddComponent<GeceOrtami>();
            var volGo = new GameObject("Gunduz Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeYolu);
            Camera cam = Kok("Main Camera").GetComponent<Camera>();
            cam.farClipPlane = 9000f;
            cam.nearClipPlane = 0.5f;
            cam.clearFlags = CameraClearFlags.Skybox;
            if (!cam.TryGetComponent(out UniversalAdditionalCameraData ek))
                ek = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            ek.renderPostProcessing = true;

            Bolum4Hud hud = HudKur(ucus, ucakGo.GetComponent<Saglik>(), tren, ucakGo.GetComponent<BombaYuku>(), makineli);
            AtesBolgesi[] bolgeler = DusmanlariKur(egri, Zemin, tren, ucus);
            KursunDaglariGorevi gorev = GorevKur(tren, ucus, makineli, hud, bolgeler);
            var sesGo = new GameObject("Savas Sesleri");
            var ses = sesGo.AddComponent<SavasSesleri>();
            var sso = new SerializedObject(ses);
            sso.FindProperty("makineli").objectReferenceValue = makineli;
            sso.ApplyModifiedPropertiesWithoutUndo();
            TrenIsigiKur(tren);
            Faz3Bagla(hud, gorev, ucakGo.GetComponent<Bolum4Nisan>());
            GameObject menuGo = Kok("Duraklatma Menusu") ?? new GameObject("Duraklatma Menusu");
            Lab7Arayuz.DuraklatmaMenusuKur(menuGo);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.Exception($"Saving {ScenePath} failed.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            string mesaj = $"Created {ScenePath} from {Lab7SceneBuilder.ScenePath}: line {egri.Uzunluk:F0} m, viaduct s {kopruBas:F0}–{kopruSon:F0} m, " +
                           $"train cruise {tren.SeyirHizi * 3.6f:F0} km/h; appended to Build Settings.";
            return $"{mesaj} {Lab8Menu.Uygula()}";   // opens AnaMenu: the Bölüm 4 objects are gone after this
        }

        // ================================================================== aircraft

        static Makineli UcagiKur(GameObject ucakGo, UcusKontrol ucus, RayEgrisi egri, int arazi, int yapi, int hedef)
        {
            float s = H.TrenBaslangici;
            Vector3 ileri = egri.YatayYon(s);
            Vector3 p = egri.Nokta(s) - ileri * BaslangicGerisi + Vector3.up * BaslangicYuksekligi;
            ucakGo.transform.SetPositionAndRotation(p, Quaternion.LookRotation(ileri, Vector3.up));

            var so = new SerializedObject(ucus);
            so.FindProperty("baslangicHizi").floatValue = BaslangicHizi;
            so.FindProperty("baslangicGazi").floatValue = BaslangicGazi;
            so.ApplyModifiedPropertiesWithoutUndo();

            var aso = new SerializedObject(ucakGo.GetComponent<InisAlgilayici>());
            aso.FindProperty("yerdeBasla").boolValue = false;
            aso.ApplyModifiedPropertiesWithoutUndo();

            var yso = new SerializedObject(ucakGo.GetComponent<BombaYuku>());
            yso.FindProperty("kapasite").intValue = BombaKapasitesi;
            yso.FindProperty("birakmaTavani").floatValue = H.BombaTavani;
            yso.ApplyModifiedPropertiesWithoutUndo();

            // Worksheet 06: the machine gun; the aircraft is on the train's side.
            var ucakSagligi = ucakGo.GetComponent<Saglik>();
            ucakSagligi.Taraf = Taraf.Dost;
            EditorUtility.SetDirty(ucakSagligi);
            var makineli = ucakGo.AddComponent<Makineli>();
            var mso = new SerializedObject(makineli);
            mso.FindProperty("mermiOn").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Mermi>(Lab8Dusmanlar.OyuncuMermisiYolu);
            mso.FindProperty("namluAlevi").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.EfektKlasoru + "/NamluAlevi.prefab");
            mso.FindProperty("vurulacak").intValue = (1 << arazi) | (1 << yapi) | (1 << hedef);
            mso.ApplyModifiedPropertiesWithoutUndo();

            // Phase 3: targeting and firing assists (T, O), engine smoke (worksheet 07).
            ucakGo.AddComponent<Bolum4Nisan>();
            MotorDumaniKur(ucakGo);

            // 2026-11-11 (user: the controls are still too hard): arcade flight by default; K cycles arcade → easy → realistic.
            var aso2 = new SerializedObject(ucus);
            aso2.FindProperty("arkad").boolValue = true;
            aso2.FindProperty("arkadSecenegi").boolValue = true;
            aso2.FindProperty("aerodinamik").boolValue = false;
            aso2.FindProperty("arkadAyar.sinirYaricapi").floatValue = H.SinirYaricapi;
            aso2.FindProperty("arkadAyar.haritaMerkezi").vector3Value = H.HaritaMerkezi;
            aso2.ApplyModifiedPropertiesWithoutUndo();
            // The project had no sound: engine (synthesised on the audio thread).
            if (!ucakGo.TryGetComponent(out AudioSource kaynak)) kaynak = ucakGo.AddComponent<AudioSource>();
            kaynak.playOnAwake = true;
            var ms = ucakGo.AddComponent<MotorSesi>();
            var mss = new SerializedObject(ms);
            mss.FindProperty("ucus").objectReferenceValue = ucus;
            mss.ApplyModifiedPropertiesWithoutUndo();

            // Targeting is tied to the night mission's targets; Bölüm 4 adds its own in phase 3.
            foreach (var n in ucakGo.GetComponents<NisanSistemi>())
                Object.DestroyImmediate(n);

            var yardimci = ucakGo.GetComponent<UcusYardimcisi>();
            var hso = new SerializedObject(yardimci);
            hso.FindProperty("pistBasi").vector3Value = H.PistBasi;
            hso.FindProperty("pistSonu").vector3Value = H.PistSonu;
            hso.FindProperty("pistEni").floatValue = H.PistEni;
            hso.FindProperty("haritaMerkezi").vector3Value = H.HaritaMerkezi;
            hso.FindProperty("sinirYaricapi").floatValue = H.SinirYaricapi;
            hso.ApplyModifiedPropertiesWithoutUndo();
            return makineli;
        }

        /// <summary>Thin black smoke from the engine (worksheet 07), a world-space trail; <see cref="MotorDumani"/> thickens it
        /// as the health drops.</summary>
        static void MotorDumaniKur(GameObject ucakGo)
        {
            Renderer[] rs = ucakGo.GetComponentsInChildren<Renderer>().Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            Vector3 burun = ucakGo.transform.InverseTransformPoint(b.center) + new Vector3(0f, -0.25f, b.extents.z * 0.55f);
            ParticleSystem ps = Lab8Dusmanlar.Duman(ucakGo.transform, burun, 0.6f, true);
            ps.name = "Motor Dumani";
            var e = ps.emission;
            e.rateOverDistance = 0f;
            var ana = ps.main;
            ana.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
            ps.gameObject.SetActive(true);
            var md = ucakGo.AddComponent<MotorDumani>();
            var so = new SerializedObject(md);
            so.FindProperty("saglik").objectReferenceValue = ucakGo.GetComponent<Saglik>();
            so.FindProperty("duman").objectReferenceValue = ps;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================== enemies (phase 2)

        /// <summary>
        /// The three fire zones of worksheet 04 with their enemies, all prefab instances: tank pairs 16 m apart beside the
        /// line (facing it), AA guns on their pads, the aircraft waves waiting outside (entry points 1.5 km out, at least
        /// 350 m above the ground).
        /// </summary>
        static AtesBolgesi[] DusmanlariKur(RayEgrisi e, System.Func<float, float, float> zemin, Tren tren, UcusKontrol ucus)
        {
            var tankOn = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.TankYolu);
            var aaOn = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.UcaksavarYolu);
            var ucakOn = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.DusmanUcagiYolu);
            var kok = new GameObject("Ates Bolgeleri");
            var sonuc = new List<AtesBolgesi>();
            float L = e.Uzunluk;
            int tohum = 1;
            for (int i = 0; i < H.Bolgeler.Length; i++)
            {
                H.Bolge b = H.Bolgeler[i];
                var go = new GameObject($"Bolge {b.Ad}");
                go.transform.SetParent(kok.transform, false);
                go.transform.position = e.Nokta((b.Bas + b.Son) * 0.5f * L);
                var yer = new List<GameObject>();
                int aaSayisi = 0;
                foreach (H.Mevzi m in H.Mevziler.Where(x => x.Bolge == i))
                {
                    Vector3 p = H.MevziKonumu(m, e);
                    float s = m.S * L;
                    Vector3 ileri = e.YatayYon(s);
                    Vector3 hattaBak = e.Nokta(s) - p;
                    hattaBak.y = 0f;
                    Quaternion yon = Quaternion.LookRotation(hattaBak.sqrMagnitude > 1f ? hattaBak : ileri, Vector3.up);
                    if (m.Tur == H.MevziTuru.Tank)
                    {
                        foreach (int k in new[] { -1, 1 })
                        {
                            Vector3 q = p + ileri * (k * H.TankCiftiAraligi * 0.5f);
                            q.y = zemin(q.x, q.z);
                            var t = (GameObject)PrefabUtility.InstantiatePrefab(tankOn, go.transform);
                            t.name = $"Tank {i + 1}{(k < 0 ? "a" : "b")}";
                            t.transform.SetPositionAndRotation(q, yon);
                            Tohum(t.GetComponent<Tank>(), tohum++);
                            yer.Add(t);
                        }
                    }
                    else
                    {
                        p.y = zemin(p.x, p.z);
                        var a = (GameObject)PrefabUtility.InstantiatePrefab(aaOn, go.transform);
                        a.name = $"Ucaksavar {i + 1}{"abc"[aaSayisi++]}";
                        a.transform.SetPositionAndRotation(p, yon);
                        Tohum(a.GetComponent<UcaksavarMevzii>(), 70 + tohum++);
                        yer.Add(a);
                    }
                }
                var ucaklar = new List<DusmanUcagi>();
                var girisler = new List<Vector3>();
                for (int k = 0; k < b.Ucak; k++)
                {
                    Vector3 g = H.UcakGirisi(i, k, e);
                    g.y = Mathf.Max(zemin(g.x, g.z) + 350f, go.transform.position.y + 450f);
                    var u = (GameObject)PrefabUtility.InstantiatePrefab(ucakOn, go.transform);
                    u.name = $"Dusman Ucagi {i + 1}{"ab"[k]}";
                    u.transform.position = g;
                    Tohum(u.GetComponent<DusmanUcagi>(), 100 + tohum++);
                    ucaklar.Add(u.GetComponent<DusmanUcagi>());
                    girisler.Add(g);
                }
                var bolge = go.AddComponent<AtesBolgesi>();
                var so = new SerializedObject(bolge);
                so.FindProperty("ad").stringValue = b.Ad;
                so.FindProperty("tren").objectReferenceValue = tren;
                so.FindProperty("ucus").objectReferenceValue = ucus;
                so.FindProperty("bas").floatValue = b.Bas * L;
                so.FindProperty("son").floatValue = b.Son * L;
                so.FindProperty("onden").floatValue = H.BolgeOnden;
                Lab7Yapilar.Dizi(so.FindProperty("yerBirimleri"), yer);
                Lab7Yapilar.Dizi(so.FindProperty("ucaklar"), ucaklar);
                var gp = so.FindProperty("ucakGirisleri");
                gp.arraySize = girisler.Count;
                for (int k = 0; k < girisler.Count; k++) gp.GetArrayElementAtIndex(k).vector3Value = girisler[k];
                so.ApplyModifiedPropertiesWithoutUndo();
                sonuc.Add(bolge);
            }
            return sonuc.ToArray();
        }

        static void Tohum(Component c, int deger)
        {
            var so = new SerializedObject(c);
            so.FindProperty("tohum").intValue = deger;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static KursunDaglariGorevi GorevKur(Tren tren, UcusKontrol ucus, Makineli makineli, Bolum4Hud hud, AtesBolgesi[] bolgeler)
        {
            var go = new GameObject("Kursun Daglari Gorevi");
            var g = go.AddComponent<KursunDaglariGorevi>();
            var so = new SerializedObject(g);
            so.FindProperty("tren").objectReferenceValue = tren;
            so.FindProperty("ucus").objectReferenceValue = ucus;
            so.FindProperty("ucakSagligi").objectReferenceValue = ucus.GetComponent<Saglik>();
            so.FindProperty("algilayici").objectReferenceValue = ucus.GetComponent<InisAlgilayici>();
            so.FindProperty("makineli").objectReferenceValue = makineli;
            so.FindProperty("yuk").objectReferenceValue = ucus.GetComponent<BombaYuku>();
            so.FindProperty("hud").objectReferenceValue = hud;
            Lab7Yapilar.Dizi(so.FindProperty("bolgeler"), bolgeler);
            so.ApplyModifiedPropertiesWithoutUndo();
            return g;
        }

        /// <summary>A tall, half-transparent green light column over the locomotive: the train can be found among the
        /// mountains from far away (user: "treni göremiyorum"). No collider, not in any raycast layer.</summary>
        static void TrenIsigiKur(Tren tren)
        {
            Transform lok = tren.Araclar[0].transform;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Tren Isik Sutunu";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(lok, false);
            go.transform.localPosition = new Vector3(0f, 80f, 0f);
            go.transform.localScale = new Vector3(2.2f, 75f, 2.2f);
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Lab8Varliklar.Unlit("TrenIsikSutunu", new Color(0.35f, 1f, 0.45f, 0.28f), true);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        /// <summary>Phase 3: the HUD and the GPS get the mission and the targeting assist.</summary>
        static void Faz3Bagla(Bolum4Hud hud, KursunDaglariGorevi gorev, Bolum4Nisan nisan)
        {
            var so = new SerializedObject(hud);
            so.FindProperty("gorevAkisi").objectReferenceValue = gorev;
            so.FindProperty("nisan").objectReferenceValue = nisan;
            so.ApplyModifiedPropertiesWithoutUndo();
            var gps = hud.GetComponentInChildren<Bolum4Gps>(true);
            var gso = new SerializedObject(gps);
            gso.FindProperty("gorev").objectReferenceValue = gorev;
            gso.FindProperty("nisan").objectReferenceValue = nisan;
            gso.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================== lighting

        static void AydinlatmaKur(GameObject isikGo)
        {
            isikGo.name = "Gunes";
            var gunes = isikGo.GetComponent<Light>();
            isikGo.transform.rotation = Quaternion.Euler(GunesAcisi);
            gunes.color = new Color(1f, 0.95f, 0.86f);
            gunes.intensity = 1.6f;
            gunes.shadows = LightShadows.Soft;
            gunes.shadowStrength = 0.8f;

            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.sun = gunes;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.67f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.52f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.27f, 0.25f, 0.21f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = SisYogunlugu;
            RenderSettings.fogColor = SisRengi;
        }

        static VolumeProfile VolumeProfili()
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeYolu);
            if (mevcut != null) return mevcut;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, VolumeYolu);
            var ton = p.Add<Tonemapping>(true);
            ton.mode.Override(TonemappingMode.ACES);
            var bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.35f);
            var renk = p.Add<ColorAdjustments>(true);
            renk.postExposure.Override(0.25f);
            renk.contrast.Override(8f);
            renk.saturation.Override(-6f);
            var vin = p.Add<Vignette>(true);
            vin.intensity.Override(0.18f);
            foreach (VolumeComponent c in p.components)
            {
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, p);
            }
            EditorUtility.SetDirty(p);
            return p;
        }

        // ================================================================== HUD

        public const string TusYazisi =
            "W/S burun · A/D yatış · Q/E dümen · ↑/↓ gaz · F fren · K kolay uçuş · Ctrl/sol tık makineli · Space bomba · T hedef · O atış yardımı · M GPS · Esc menü";

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

        static Bolum4Hud HudKur(UcusKontrol ucus, Saglik ucakSagligi, Tren tren, BombaYuku yuk, Makineli makineli)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var kok = new GameObject("Bolum4 HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            kok.layer = LayerMask.NameToLayer("UI");
            var canvas = kok.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var olcek = kok.GetComponent<CanvasScaler>();
            olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            olcek.referenceResolution = new Vector2(1920f, 1080f);
            olcek.matchWidthOrHeight = 0.5f;
            var rt = (RectTransform)kok.transform;

            var isaretler = new GameObject("Isaretler", typeof(RectTransform));
            ((RectTransform)isaretler.transform).SetParent(rt, false);
            var (panel, _, _, hizMetni, gazMetni) = Lab7Arayuz.GostergeleriKur(rt, ucus);

            Text durum = Yazi("Durum", rt, font, 28, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(700f, 200f));
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
            var isaretKoku = new GameObject("Hedef Isaretleri", typeof(RectTransform)).GetComponent<RectTransform>();
            isaretKoku.SetParent(rt, false);
            isaretKoku.anchorMin = Vector2.zero;
            isaretKoku.anchorMax = Vector2.one;
            isaretKoku.offsetMin = isaretKoku.offsetMax = Vector2.zero;
            isaretKoku.SetAsFirstSibling();
            Text halka = Yazi("Oncu Halka", isaretKoku, font, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 50f));
            halka.text = "( + )";
            halka.gameObject.SetActive(false);

            var tamam = new GameObject("Gorev Tamam Paneli", typeof(RectTransform), typeof(Image));
            var trt = (RectTransform)tamam.transform;
            trt.SetParent(rt, false);
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(980f, 470f);
            tamam.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 0.8f);
            tamam.GetComponent<Image>().raycastTarget = false;
            Text tamamMetni = Yazi("Metin", trt, font, 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 440f));
            tamamMetni.lineSpacing = 1.15f;
            tamam.SetActive(false);

            GpsKur(rt, font, ucus, tren);

            var hud = kok.AddComponent<Bolum4Hud>();
            var so = new SerializedObject(hud);
            so.FindProperty("ucus").objectReferenceValue = ucus;
            so.FindProperty("ucakSagligi").objectReferenceValue = ucakSagligi;
            so.FindProperty("tren").objectReferenceValue = tren;
            so.FindProperty("yuk").objectReferenceValue = yuk;
            so.FindProperty("makineli").objectReferenceValue = makineli;
            so.FindProperty("durumMetni").objectReferenceValue = durum;
            so.FindProperty("gorevMetni").objectReferenceValue = gorev;
            so.FindProperty("bildirimMetni").objectReferenceValue = bildirim;
            so.FindProperty("ucusMetni").objectReferenceValue = ucusMetni;
            so.FindProperty("hizMetni").objectReferenceValue = hizMetni;
            so.FindProperty("gazMetni").objectReferenceValue = gazMetni;
            so.FindProperty("gostergeler").objectReferenceValue = panel.gameObject;
            so.FindProperty("uyariMetni").objectReferenceValue = uyari;
            so.FindProperty("isaretKoku").objectReferenceValue = isaretKoku;
            so.FindProperty("oncuHalka").objectReferenceValue = halka;
            so.FindProperty("tamamPaneli").objectReferenceValue = tamam;
            so.FindProperty("tamamMetni").objectReferenceValue = tamamMetni;
            so.FindProperty("font").objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        /// <summary>The GPS panel (bottom right): map, marker layer, one line of text. <see cref="Bolum4Gps"/> draws the map.</summary>
        static void GpsKur(RectTransform ust, Font font, UcusKontrol ucus, Tren tren)
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
            var gps = panel.AddComponent<Bolum4Gps>();
            var so = new SerializedObject(gps);
            so.FindProperty("panel").objectReferenceValue = prt;
            so.FindProperty("harita").objectReferenceValue = harita.GetComponent<RawImage>();
            so.FindProperty("isaretKoku").objectReferenceValue = isaret;
            so.FindProperty("bilgi").objectReferenceValue = bilgi;
            so.FindProperty("ucus").objectReferenceValue = ucus;
            so.FindProperty("tren").objectReferenceValue = tren;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
