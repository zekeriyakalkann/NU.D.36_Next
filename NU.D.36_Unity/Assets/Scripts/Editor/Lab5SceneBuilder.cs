using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 5 (Bölüm 2 — Sınır Hattı): project layers, physics matrix, prefabs and the Rota05 scene.
    /// Rota05 = copy of Rota04Hud (the "File → Save As" step); Rota04Hud and every earlier scene stay untouched and an
    /// existing Rota05 is never overwritten. Batch mode: -executeMethod NU36.Editor.Lab5SceneBuilder.Create
    /// Layout, values and reasons: Documentation/Lab5_Implementation.md.
    /// </summary>
    public static class Lab5SceneBuilder
    {
        public const string SceneName = "Rota05";
        public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
        public const string Folder = "Assets/Lab5";
        public const string MermiPrefabPath = Folder + "/Prefabs/Mermi.prefab";
        public const string BombaPrefabPath = Folder + "/Prefabs/Bomba.prefab";
        public const string PatlamaPrefabPath = Folder + "/Prefabs/Patlama.prefab";
        const string TerrainMaterialPath = "Assets/Environment/Materials/Rota03_Arazi.mat";

        // Worksheet Step 01 layers, in this order from user layer 8.
        public static readonly string[] Katmanlar = { "Ucak", "Arazi", "Yapi", "Hedef", "Mermi" };
        public const int IlkKatman = 8;

        // Layout (x, z) in metres; the flat plain east of the base (Logs/lab5/arazi_haritasi.txt).
        public static readonly Vector2[] MevziKonumlari = { new Vector2(1000f, 650f), new Vector2(1800f, 1000f), new Vector2(2200f, -100f), new Vector2(1400f, -600f) };
        public static readonly Vector2[] UcaksavarKonumlari = { new Vector2(1650f, 1150f), new Vector2(2050f, 50f), new Vector2(1150f, 800f) };
        public static readonly Vector2 SesDinleyiciKonumu = new Vector2(1700f, -150f);
        public static readonly (string ad, Vector2 konum, float yukseklik, float yaricap, int tohum)[] Tepeler =
        {
            ("Tepe 1", new Vector2(2550f, 650f), 120f, 380f, 1),
            ("Tepe 2", new Vector2(850f, -450f), 170f, 420f, 2),
            ("Tepe 3", new Vector2(1600f, 350f), 230f, 500f, 3),
        };
        public static readonly Vector3 UcakBaslangici = new Vector3(0f, 150f, 0f); // more height above the rising ground ahead (80 m reached 5 m AGL after ~18 s hands-off)
        public const float UcakBaslangicYonu = 57f; // towards the first dump

        // Hurricane Mk IIB stand-in values for UcusKontrol (INFERRED, game scale: ~85 m/s in the dive, about 4.5 g pull-out).
        public const float EnAzHiz = 30f, EnCokHiz = 90f, BaslangicGazi = 0.6f, YunuslamaHizi = 30f, HizIvmesi = 8f, EgimHizKaybi = 25f;
        public const float GovdeYaricapi = 2.4f;

        [MenuItem("NU36/Lab 5/Set Up Layers And Physics")]
        public static void KatmanlariKurMenu() => Run(() => { KatmanlariKur(); return "Layers and collision matrix set."; });

        [MenuItem("NU36/Lab 5/Create Rota05 Scene")]
        public static void Create() => Run(TryCreate);

        static void Run(System.Func<string> is_)
        {
            bool ok;
            string message;
            try { message = is_(); ok = true; }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab5] {message}");
            else Debug.LogError($"[NU36 Lab5] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        // ------------------------------------------------------------------ project settings

        /// <summary>Creates the five layers (if missing) and turns off Mermi×Mermi and Hedef×Arazi collisions.</summary>
        public static void KatmanlariKur()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 0; i < Katmanlar.Length; i++)
            {
                string ad = Katmanlar[i];
                if (LayerMask.NameToLayer(ad) >= 0)
                    continue;
                SerializedProperty el = layers.GetArrayElementAtIndex(IlkKatman + i);
                if (!string.IsNullOrEmpty(el.stringValue))
                    throw new System.Exception($"User layer {IlkKatman + i} is already '{el.stringValue}'.");
                el.stringValue = ad;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            Physics.IgnoreLayerCollision(Katman("Mermi"), Katman("Mermi"), true);
            Physics.IgnoreLayerCollision(Katman("Hedef"), Katman("Arazi"), true);
            AssetDatabase.SaveAssets();
        }

        static int Katman(string ad)
        {
            int k = LayerMask.NameToLayer(ad);
            if (k < 0)
                throw new System.Exception($"Layer {ad} is missing.");
            return k;
        }

        static int Maske(params string[] adlar) => adlar.Aggregate(0, (m, a) => m | (1 << Katman(a)));

        // ------------------------------------------------------------------ scene

        static string TryCreate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new System.Exception($"{ScenePath} already exists; not overwritten.");
            KatmanlariKur();
            var mat = new Malzemeler();
            Patlama patlamaOn = PatlamaPrefab(mat);
            Mermi mermiOn = MermiPrefab(mat);
            Bomba bombaOn = BombaPrefab(mat, patlamaOn);

            if (!AssetDatabase.CopyAsset(Lab4HudSceneBuilder.ScenePath, ScenePath))
                throw new System.Exception($"Copying {Lab4HudSceneBuilder.ScenePath} to {ScenePath} failed.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject Kok(string ad) => scene.GetRootGameObjects().SingleOrDefault(g => g.name == ad);

            GameObject ucakGo = scene.GetRootGameObjects().Single(g => g.GetComponent<UcusKontrol>() != null);
            var ucus = ucakGo.GetComponent<UcusKontrol>();

            // Lab 4 mission objects are replaced by the Bölüm 2 loop; the Lab 2 balloon and gun are not part of this level.
            foreach (string ad in new[] { "Oyun", "Hedefler", "Balon", "Uçaksavar" })
                if (Kok(ad) != null)
                    Object.DestroyImmediate(Kok(ad));

            UcagiKur(ucakGo, ucus, bombaOn);

            // Terrain: Mesh Colliders on layer Arazi, named as in the worksheet.
            GameObject arazi = Kok("Arazi");
            arazi.name = "Çakıl Burnu Arazisi";
            KatmanAta(arazi, Katman("Arazi"));
            var aso = new SerializedObject(arazi.GetComponent<SonsuzArazi>());
            aso.FindProperty("carpisici").boolValue = true;
            aso.ApplyModifiedPropertiesWithoutUndo();

            var tepeler = new GameObject("Tepeler");
            var araziMat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            foreach (var t in Tepeler)
            {
                var go = new GameObject(t.ad, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(tepeler.transform, false);
                go.transform.position = new Vector3(t.konum.x, 0f, t.konum.y); // the mesh samples the terrain itself
                go.GetComponent<MeshRenderer>().sharedMaterial = araziMat;
                var tepe = go.AddComponent<Tepe>();
                var so = new SerializedObject(tepe);
                so.FindProperty("yukseklik").floatValue = t.yukseklik;
                so.FindProperty("yaricap").floatValue = t.yaricap;
                so.FindProperty("tohum").intValue = t.tohum;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            KatmanAta(tepeler, Katman("Arazi"));

            // Enemy: sound locator + 3 AA positions; targets: 4 ammunition dumps.
            var bolum = new GameObject("Bolum2");
            SesDinleyici kulak = SesDinleyiciKur(bolum.transform, ucus, mat);
            for (int i = 0; i < UcaksavarKonumlari.Length; i++)
                UcaksavarKur(bolum.transform, i + 1, UcaksavarKonumlari[i], ucus, kulak, mermiOn, mat);
            var mevziler = new MuhimmatMevzii[MevziKonumlari.Length];
            for (int i = 0; i < mevziler.Length; i++)
                mevziler[i] = MevziKur(bolum.transform, i + 1, MevziKonumlari[i], patlamaOn, mat);

            // Level loop.
            var seviyeGo = new GameObject("Seviye");
            var seviye = seviyeGo.AddComponent<SeviyeYoneticisi>();
            var sso = new SerializedObject(seviye);
            var arr = sso.FindProperty("mevziler");
            arr.arraySize = mevziler.Length;
            for (int i = 0; i < mevziler.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = mevziler[i];
            sso.FindProperty("yuk").objectReferenceValue = ucakGo.GetComponent<BombaYuku>();
            sso.FindProperty("ucakSagligi").objectReferenceValue = ucakGo.GetComponent<Saglik>();
            sso.FindProperty("algilayici").objectReferenceValue = ucakGo.GetComponent<InisAlgilayici>();
            sso.FindProperty("ucus").objectReferenceValue = ucus;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var son = seviyeGo.AddComponent<SeviyeSonuEkrani>();
            son.Yonetici = seviye;

            // Base: resupply trigger instead of the Lab 4 mission trigger; always active.
            GameObject us = Kok("Us");
            Object.DestroyImmediate(us.GetComponent<UsGirisi>());
            us.AddComponent<IkmalNoktasi>().Yonetici = seviye;
            us.SetActive(true);
            KatmanAta(us, Katman("Yapi"));

            HudKur(Kok("HUD"), seviye, ucakGo.GetComponent<BombaYuku>(), Kok("Main Camera").GetComponent<Camera>());

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.Exception($"Saving {ScenePath} failed.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            return $"Created {ScenePath} from {Lab4HudSceneBuilder.ScenePath} and appended it to Build Settings.";
        }

        static void UcagiKur(GameObject ucakGo, UcusKontrol ucus, Bomba bombaOn)
        {
            ucakGo.transform.SetPositionAndRotation(UcakBaslangici, Quaternion.Euler(0f, UcakBaslangicYonu, 0f));
            foreach (var c in ucakGo.GetComponents<HedefeGit>()) Object.DestroyImmediate(c);
            foreach (var c in ucakGo.GetComponents<KanatYatisi>()) Object.DestroyImmediate(c);

            var so = new SerializedObject(ucus);
            so.FindProperty("enAzHiz").floatValue = EnAzHiz;
            so.FindProperty("enCokHiz").floatValue = EnCokHiz;
            so.FindProperty("baslangicGazi").floatValue = BaslangicGazi;
            so.FindProperty("yunuslamaHizi").floatValue = YunuslamaHizi;
            so.FindProperty("hizIvmesi").floatValue = HizIvmesi;
            so.FindProperty("egimHizKaybi").floatValue = EgimHizKaybi;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Body sphere for the tunnelling lesson (govdeYaricapi = 2.4 m) instead of the Lab 4 box.
            foreach (var c in ucakGo.GetComponents<BoxCollider>()) Object.DestroyImmediate(c);
            var kure = ucakGo.AddComponent<SphereCollider>();
            kure.radius = GovdeYaricapi;
            kure.center = Vector3.zero;

            if (ucakGo.GetComponent<ZeminBaglayici>() == null)
                ucakGo.AddComponent<ZeminBaglayici>();
            var yuk = ucakGo.AddComponent<BombaYuku>();
            yuk.BombaOn = bombaOn;
            yuk.Ucus = ucus;
            KatmanAta(ucakGo, Katman("Ucak"));
        }

        static SesDinleyici SesDinleyiciKur(Transform ust, UcusKontrol ucus, Malzemeler mat)
        {
            var go = new GameObject("SesDinleyici");
            go.transform.SetParent(ust, false);
            go.transform.position = Zeminde(SesDinleyiciKonumu);
            Parca(PrimitiveType.Cylinder, "Ayak", go.transform, mat.Metal, new Vector3(0f, 1.5f, 0f), new Vector3(0.5f, 1.5f, 0.5f));
            // Four horns (2 × 2), as on the period locator.
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * 1.1f, y = 3.4f + (i / 2) * 1.6f;
                GameObject huni = Parca(PrimitiveType.Cylinder, $"Huni {i + 1}", go.transform, mat.Metal, new Vector3(x, y, 0.8f), new Vector3(0.9f, 1.2f, 0.9f));
                huni.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            var kutu = go.AddComponent<BoxCollider>();
            kutu.center = new Vector3(0f, 2.5f, 0f);
            kutu.size = new Vector3(4f, 5f, 3f);
            var kulak = go.AddComponent<SesDinleyici>();
            kulak.Ucak = ucus;
            KatmanAta(go, Katman("Yapi"));
            return kulak;
        }

        static void UcaksavarKur(Transform ust, int no, Vector2 konum, UcusKontrol ucus, SesDinleyici kulak, Mermi mermiOn, Malzemeler mat)
        {
            var go = new GameObject($"Ucaksavar {no}");
            go.transform.SetParent(ust, false);
            go.transform.position = Zeminde(konum);
            // Faces the base (the expected approach); the cone covers 240° and leaves the back blind.
            go.transform.rotation = Quaternion.LookRotation(new Vector3(-konum.x, 0f, -konum.y));
            Parca(PrimitiveType.Cylinder, "Siper", go.transform, mat.Kum, new Vector3(0f, 0.6f, 0f), new Vector3(7f, 0.6f, 7f));
            Parca(PrimitiveType.Cylinder, "Kaide", go.transform, mat.Metal, new Vector3(0f, 1.3f, 0f), new Vector3(1f, 0.7f, 1f));
            var namlu = new GameObject("Namlu").transform;
            namlu.SetParent(go.transform, false);
            namlu.localPosition = new Vector3(0f, 2.1f, 0f);
            GameObject boru = Parca(PrimitiveType.Cylinder, "Boru", namlu, mat.Metal, new Vector3(0f, 0f, 1.5f), new Vector3(0.22f, 1.5f, 0.22f));
            boru.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var siper = go.AddComponent<BoxCollider>(); // sandbags only, below the 3 m eye
            siper.center = new Vector3(0f, 0.6f, 0f);
            siper.size = new Vector3(7f, 1.2f, 7f);

            var radar = go.AddComponent<Radar>();
            radar.Hedef = ucus.transform;
            radar.YariAci = 120f;
            radar.Menzil = 1500f;
            radar.EngelKatmani = Maske("Arazi", "Yapi");
            radar.GozYuksekligi = 3f;

            var ua = go.AddComponent<UcaksavarMevzii>();
            var so = new SerializedObject(ua);
            so.FindProperty("ucak").objectReferenceValue = ucus;
            so.FindProperty("goz").objectReferenceValue = radar;
            so.FindProperty("kulak").objectReferenceValue = kulak;
            so.FindProperty("mermiOn").objectReferenceValue = mermiOn;
            so.FindProperty("namlu").objectReferenceValue = namlu;
            so.FindProperty("vurulacak").intValue = Maske("Ucak", "Arazi");
            so.FindProperty("menzil").floatValue = 1000f;
            so.FindProperty("tohum").intValue = no;
            so.ApplyModifiedPropertiesWithoutUndo();
            KatmanAta(go, Katman("Yapi"));
        }

        static MuhimmatMevzii MevziKur(Transform ust, int no, Vector2 konum, Patlama patlamaOn, Malzemeler mat)
        {
            var go = new GameObject($"Muhimmat Mevzii {no}");
            go.transform.SetParent(ust, false);
            go.transform.position = Zeminde(konum);
            go.transform.rotation = Quaternion.Euler(0f, no * 37f, 0f);
            var saglam = new GameObject("Saglam").transform;
            saglam.SetParent(go.transform, false);
            Parca(PrimitiveType.Cube, "Sandik 1", saglam, mat.Sandik, new Vector3(-4f, 1.5f, 0f), new Vector3(5f, 3f, 8f));
            Parca(PrimitiveType.Cube, "Sandik 2", saglam, mat.Sandik, new Vector3(3f, 1.2f, -1f), new Vector3(5f, 2.4f, 6f));
            Parca(PrimitiveType.Cube, "Sandik 3", saglam, mat.Sandik, new Vector3(2f, 3.4f, 0f), new Vector3(3f, 2f, 4f));
            Parca(PrimitiveType.Cylinder, "Direk", saglam, mat.Isaret, new Vector3(0f, 20f, 5f), new Vector3(0.8f, 20f, 0.8f));
            Parca(PrimitiveType.Cube, "Bayrak", saglam, mat.Isaret, new Vector3(2.5f, 37f, 5f), new Vector3(5f, 3f, 0.2f));
            var enkaz = new GameObject("Enkaz").transform;
            enkaz.SetParent(go.transform, false);
            Parca(PrimitiveType.Cube, "Kalinti 1", enkaz, mat.Is, new Vector3(-3f, 0.4f, 1f), new Vector3(7f, 0.8f, 6f));
            Parca(PrimitiveType.Cube, "Kalinti 2", enkaz, mat.Is, new Vector3(3f, 0.3f, -2f), new Vector3(5f, 0.6f, 5f));
            enkaz.gameObject.SetActive(false);

            var kutu = go.AddComponent<BoxCollider>();
            kutu.center = new Vector3(0f, 2.5f, 0f);
            kutu.size = new Vector3(14f, 5f, 10f);
            go.AddComponent<Saglik>();
            var m = go.AddComponent<MuhimmatMevzii>();
            var so = new SerializedObject(m);
            so.FindProperty("saglam").objectReferenceValue = saglam.gameObject;
            so.FindProperty("enkaz").objectReferenceValue = enkaz.gameObject;
            so.FindProperty("ikincilPatlama").objectReferenceValue = patlamaOn;
            so.ApplyModifiedPropertiesWithoutUndo();
            KatmanAta(go, Katman("Hedef"));
            return m;
        }

        static void HudKur(GameObject hud, SeviyeYoneticisi seviye, BombaYuku yuk, Camera kamera)
        {
            // Lab 4 status texts now show the level (the Lab 4 HudMetin read the removed mission manager).
            var eski = hud.GetComponent<HudMetin>();
            Text durum = eski.GorevMetni, sure = eski.SureMetni;
            Object.DestroyImmediate(eski);
            var sh = hud.AddComponent<SeviyeHud>();
            sh.Yonetici = seviye;
            sh.DurumMetni = durum;
            sh.SureMetni = sure;
            durum.text = SeviyeHud.Metin(0, MevziKonumlari.Length, 2, 2, 100, 0);
            sure.text = HudHesap.SureMetni(180f);

            // The airspeed dial face is printed 0-50 m/s; it is left as it is (the needle stops at 50), see Lab5_Implementation.md.

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var panel = Rt("DalisGostergesi", hud.transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
            panel.sizeDelta = new Vector2(330f, 120f);
            panel.anchoredPosition = new Vector2(-150f, -26f); // left of the throttle plate
            var cerceve = panel.gameObject.AddComponent<Image>();
            cerceve.raycastTarget = false;
            var cizgi = panel.gameObject.AddComponent<Outline>();
            cizgi.effectColor = new Color(0.93f, 0.85f, 0.65f, 0.9f);
            cizgi.effectDistance = new Vector2(3f, -3f);
            Text aci = Yazi("Aci", panel, font, new Vector2(18f, -10f));
            Text irtifa = Yazi("Irtifa", panel, font, new Vector2(18f, -60f));

            var katman = Rt("NisanKatmani", hud.transform);
            katman.anchorMin = Vector2.zero;
            katman.anchorMax = Vector2.one;
            katman.offsetMin = katman.offsetMax = Vector2.zero;
            var halka = Rt("NisanHalkasi", katman);
            halka.anchorMin = halka.anchorMax = new Vector2(0.5f, 0.5f);
            halka.sizeDelta = new Vector2(72f, 72f);
            var hi = halka.gameObject.AddComponent<Image>();
            hi.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            hi.raycastTarget = false;
            halka.gameObject.SetActive(false);

            var dg = panel.gameObject.AddComponent<DalisGostergesi>();
            var so = new SerializedObject(dg);
            so.FindProperty("yuk").objectReferenceValue = yuk;
            so.FindProperty("kamera").objectReferenceValue = kamera;
            so.FindProperty("cerceve").objectReferenceValue = cerceve;
            so.FindProperty("aciMetni").objectReferenceValue = aci;
            so.FindProperty("irtifaMetni").objectReferenceValue = irtifa;
            so.FindProperty("halka").objectReferenceValue = halka;
            so.ApplyModifiedPropertiesWithoutUndo();
            cerceve.color = so.FindProperty("disaridaRenk").colorValue;
        }

        // ------------------------------------------------------------------ prefabs

        static Patlama PatlamaPrefab(Malzemeler mat)
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<Patlama>(PatlamaPrefabPath);
            if (mevcut != null) return mevcut;
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Patlama";
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.GetComponent<MeshRenderer>().sharedMaterial = mat.Patlama;
            g.AddComponent<Patlama>();
            return Kaydet(g, PatlamaPrefabPath).GetComponent<Patlama>();
        }

        static Mermi MermiPrefab(Malzemeler mat)
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<Mermi>(MermiPrefabPath);
            if (mevcut != null) return mevcut;
            var g = new GameObject("Mermi");
            GameObject iz = Parca(PrimitiveType.Capsule, "Iz", g.transform, mat.Izli, Vector3.zero, new Vector3(0.35f, 1.6f, 0.35f));
            iz.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // tracer streak along the flight direction
            g.AddComponent<Mermi>();
            KatmanAta(g, Katman("Mermi"));
            return Kaydet(g, MermiPrefabPath).GetComponent<Mermi>();
        }

        static Bomba BombaPrefab(Malzemeler mat, Patlama patlamaOn)
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<Bomba>(BombaPrefabPath);
            if (mevcut != null) return mevcut;
            var g = new GameObject("Bomba");
            GameObject govde = Parca(PrimitiveType.Capsule, "Govde", g.transform, mat.Bomba, Vector3.zero, new Vector3(0.4f, 0.75f, 0.4f));
            govde.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var b = g.AddComponent<Bomba>();
            var so = new SerializedObject(b);
            so.FindProperty("carpacak").intValue = Maske("Arazi", "Yapi", "Hedef");
            so.FindProperty("etkilenen").intValue = Maske("Hedef", "Yapi", "Ucak");
            so.FindProperty("patlamaOn").objectReferenceValue = patlamaOn;
            so.ApplyModifiedPropertiesWithoutUndo();
            KatmanAta(g, Katman("Mermi"));
            return Kaydet(g, BombaPrefabPath).GetComponent<Bomba>();
        }

        static GameObject Kaydet(GameObject g, string yol)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(yol));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(g, yol);
            Object.DestroyImmediate(g);
            return prefab;
        }

        // ------------------------------------------------------------------ helpers

        sealed class Malzemeler
        {
            public readonly Material Metal = Lit("Metal", new Color(0.28f, 0.31f, 0.27f));
            public readonly Material Kum = Lit("KumTorbasi", new Color(0.62f, 0.55f, 0.38f));
            public readonly Material Sandik = Lit("Sandik", new Color(0.42f, 0.33f, 0.18f));
            public readonly Material Isaret = Lit("Isaret", new Color(0.85f, 0.12f, 0.08f), 0.8f);
            public readonly Material Is = Lit("Is", new Color(0.08f, 0.07f, 0.06f));
            public readonly Material Bomba = Lit("Bomba", new Color(0.22f, 0.26f, 0.17f));
            public readonly Material Izli = Unlit("Izli", new Color(1f, 0.85f, 0.3f));
            public readonly Material Patlama = Unlit("Patlama", new Color(1f, 0.5f, 0.1f));
        }

        static Material Lit(string ad, Color renk, float isima = 0f)
        {
            string yol = $"{Folder}/Materials/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m != null) return m;
            Directory.CreateDirectory($"{Folder}/Materials");
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", renk);
            m.SetFloat("_Smoothness", 0.2f);
            if (isima > 0f)
            {
                m.SetColor("_EmissionColor", renk * isima);
                m.EnableKeyword("_EMISSION");
            }
            AssetDatabase.CreateAsset(m, yol);
            return m;
        }

        static Material Unlit(string ad, Color renk)
        {
            string yol = $"{Folder}/Materials/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m != null) return m;
            Directory.CreateDirectory($"{Folder}/Materials");
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", renk);
            AssetDatabase.CreateAsset(m, yol);
            return m;
        }

        static Vector3 Zeminde(Vector2 p) => new Vector3(p.x, AraziUretici.YuzeyYuksekligi(p.x, p.y), p.y);

        static GameObject Parca(PrimitiveType tur, string ad, Transform ust, Material mat, Vector3 konum, Vector3 olcek)
        {
            GameObject g = GameObject.CreatePrimitive(tur);
            g.name = ad;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(ust, false);
            g.transform.localPosition = konum;
            g.transform.localScale = olcek;
            g.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }

        static void KatmanAta(GameObject go, int katman)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = katman;
        }

        static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static Text Yazi(string ad, RectTransform ust, Font font, Vector2 konum)
        {
            RectTransform r = Rt(ad, ust);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(300f, 50f);
            r.anchoredPosition = konum;
            var t = r.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = 38;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(0.93f, 0.85f, 0.65f, 1f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = r.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.02f, 0.015f, 0.01f, 0.95f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }
    }
}
