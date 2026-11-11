using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8 (Bölüm 4) phase 2: the enemy prefabs (worksheet 04: "her düşman türü bir prefab'tan gelir"), the rounds
    /// and the small effects. Tank = Quaternius tank with a turning turret and a muzzle; AA gun = the Hafta 7 position
    /// (sandbags, mount, barrel, shield) with a daylight eye; enemy aircraft = the Poly Pizza "Aeroplane" with its texture.
    /// Rounds: tracers (player yellow-white, enemy red-orange, tank shell large). Effects: muzzle flash, sparks, smoke.
    /// All under Assets/Bolum4/Prefablar; rebuilt by the scene builder.
    /// </summary>
    public static class Lab8Dusmanlar
    {
        public const string Klasor = Lab8Varliklar.Prefablar + "/Dusmanlar";
        public const string EfektKlasoru = Lab8Varliklar.Prefablar + "/Efektler";
        public const string TankYolu = Klasor + "/Tank.prefab", UcaksavarYolu = Klasor + "/Ucaksavar.prefab", DusmanUcagiYolu = Klasor + "/DusmanUcagi.prefab";
        public const string OyuncuMermisiYolu = EfektKlasoru + "/OyuncuMermisi.prefab";
        /// <summary>Tank hull length (worksheet 01: realistic sizes), enemy aircraft wing span.</summary>
        public const float TankBoyu = 7f, DusmanKanatAcikligi = 11f;

        public sealed class Set
        {
            public GameObject Tank, Ucaksavar, DusmanUcagi;
            public Mermi OyuncuMermisi;
            public GameObject KucukAlev;
        }

        /// <summary>Builds (or rebuilds) every phase-2 prefab. Layers: Hedef for the enemies.</summary>
        public static Set Kur(int hedef, int ucak, int arazi, int yapi)
        {
            Lab8Varliklar.Klasor(Klasor);
            Lab8Varliklar.Klasor(EfektKlasoru);
            GameObject kucukAlev = NamluAlevi("NamluAlevi", 1.4f);
            GameObject buyukAlev = NamluAlevi("TankNamluAlevi", 4.5f);
            GameObject kivilcim = Kivilcim();
            Mermi oyuncu = MermiPrefabi("OyuncuMermisi", new Color(1f, 0.92f, 0.55f), 1.3f, 0.45f, 0.10f, kivilcim);
            Mermi dusman = MermiPrefabi("DusmanMermisi", new Color(1f, 0.32f, 0.12f), 1.3f, 0.5f, 0.12f, null);
            Mermi tankMermisi = MermiPrefabi("TankMermisi", new Color(1f, 0.6f, 0.2f), 2.2f, 1.1f, 0.25f, null);
            return new Set
            {
                Tank = TankPrefabi(tankMermisi, buyukAlev, hedef, (1 << yapi) | (1 << arazi)),
                Ucaksavar = UcaksavarPrefabi(dusman, kucukAlev, hedef, 1 << arazi, (1 << ucak) | (1 << arazi)),
                DusmanUcagi = DusmanUcagiPrefabi(dusman, kucukAlev, hedef, (1 << ucak) | (1 << yapi) | (1 << arazi)),
                OyuncuMermisi = oyuncu,
                KucukAlev = kucukAlev,
            };
        }

        // ================================================================== materials and particles

        static Material Parcacik(string ad, Texture2D doku, Color renk, bool eklemeli)
        {
            Lab8Varliklar.Klasor(Lab8Varliklar.Malzemeler);
            string yol = $"{Lab8Varliklar.Malzemeler}/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m != null) return m;
            m = new Material(Shader.Find("NU36/Parcacik"));
            m.SetTexture("_MainTex", doku);
            m.SetColor("_Renk", renk);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(eklemeli ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_Eklemeli", eklemeli ? 1f : 0f);
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, yol);
            return m;
        }

        static ParticleSystem Sistem(string ad, Transform ust, Material mat, int enCok, bool dunya)
        {
            var go = new GameObject(ad);
            if (ust != null) go.transform.SetParent(ust, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var ana = ps.main;
            ana.simulationSpace = dunya ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            ana.maxParticles = enCok;
            ana.playOnAwake = true;
            ana.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static Gradient Renkler(params (float t, Color c)[] n)
        {
            var g = new Gradient();
            g.SetKeys(n.Select(k => new GradientColorKey(k.c, k.t)).ToArray(), n.Select(k => new GradientAlphaKey(k.c.a, k.t)).ToArray());
            return g;
        }

        static void Kaydet(GameObject g, string yol)
        {
            PrefabUtility.SaveAsPrefabAsset(g, yol);
            Object.DestroyImmediate(g);
        }

        /// <summary>Muzzle flash: two bright puffs for 0.07 s, then the object removes itself.</summary>
        public static GameObject NamluAlevi(string ad, float boy)
        {
            var ps = Sistem(ad, null, Parcacik("NamluAlevi", Lab7Varliklar.AtesDokusu(), new Color(4f, 2.6f, 1.1f, 1f), true), 4, false);
            var ana = ps.main;
            ana.duration = 0.1f;
            ana.loop = false;
            ana.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.08f);
            ana.startSize = new ParticleSystem.MinMaxCurve(boy * 0.7f, boy * 1.2f);
            ana.startSpeed = 0f;
            ana.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ana.stopAction = ParticleSystemStopAction.Destroy;
            var e = ps.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });
            var sh = ps.shape;
            sh.enabled = false;
            string yol = $"{EfektKlasoru}/{ad}.prefab";
            Kaydet(ps.gameObject, yol);
            return AssetDatabase.LoadAssetAtPath<GameObject>(yol);
        }

        /// <summary>Sparks and a puff of dust where a round strikes.</summary>
        public static GameObject Kivilcim()
        {
            var ps = Sistem("Kivilcim", null, Parcacik("Kivilcim", Lab7Varliklar.KivilcimDokusu(), new Color(3f, 1.8f, 0.7f, 1f), true), 12, true);
            var ana = ps.main;
            ana.duration = 0.3f;
            ana.loop = false;
            ana.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            ana.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            ana.startSpeed = new ParticleSystem.MinMaxCurve(6f, 16f);
            ana.gravityModifier = 1f;
            ana.stopAction = ParticleSystemStopAction.Destroy;
            var e = ps.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 7) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 40f;
            sh.radius = 0.1f;
            string yol = $"{EfektKlasoru}/Kivilcim.prefab";
            Kaydet(ps.gameObject, yol);
            return AssetDatabase.LoadAssetAtPath<GameObject>(yol);
        }

        /// <summary>Black smoke: a rising column (silenced ground unit) or a trail (hit aircraft). Inactive child.</summary>
        public static ParticleSystem Duman(Transform ust, Vector3 yerel, float boyut, bool iz)
        {
            var mat = Parcacik("SiyahDuman", Lab7Varliklar.DumanDokusu(), new Color(0.09f, 0.085f, 0.08f, 0.85f), false);
            var ps = Sistem(iz ? "Duman Izi" : "Duman", ust, mat, iz ? 300 : 120, true);
            ps.transform.localPosition = yerel;
            var ana = ps.main;
            ana.loop = true;
            ana.duration = 5f;
            ana.startLifetime = new ParticleSystem.MinMaxCurve(iz ? 3f : 5f, iz ? 4.5f : 7f);
            ana.startSize = new ParticleSystem.MinMaxCurve(boyut * 0.7f, boyut * 1.2f);
            ana.startSpeed = new ParticleSystem.MinMaxCurve(iz ? 0f : 1f, iz ? 1f : 3f);
            ana.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var e = ps.emission;
            if (iz) { e.rateOverTime = 6f; e.rateOverDistance = 0.35f; }
            else e.rateOverTime = 7f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = boyut * 0.2f;
            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = 1.2f; v.y = iz ? 0.8f : 3.5f; v.z = 0.5f;
            var b = ps.sizeOverLifetime;
            b.enabled = true;
            b.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 3.2f));
            var c = ps.colorOverLifetime;
            c.enabled = true;
            c.color = Renkler((0f, new Color(1f, 1f, 1f, 0f)), (0.08f, new Color(1f, 1f, 1f, 0.9f)), (0.6f, new Color(1f, 1f, 1f, 0.45f)), (1f, new Color(1f, 1f, 1f, 0f)));
            ps.gameObject.SetActive(false);
            return ps;
        }

        /// <summary>A tracer round: <see cref="Mermi"/> with a glowing core and a short additive trail, on the Mermi layer.</summary>
        public static Mermi MermiPrefabi(string ad, Color renk, float cekirdek, float izGenisligi, float izSuresi, GameObject isabet)
        {
            var g = new GameObject(ad);
            var m = g.AddComponent<Mermi>();
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.name = "Cekirdek";
            c.transform.SetParent(g.transform, false);
            c.transform.localScale = new Vector3(0.18f * cekirdek, cekirdek, 0.18f * cekirdek);
            c.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var cr = c.GetComponent<MeshRenderer>();
            cr.sharedMaterial = Lab8Varliklar.Unlit(ad + "_Cekirdek", new Color(renk.r * 6f, renk.g * 6f, renk.b * 6f, 1f));
            cr.shadowCastingMode = ShadowCastingMode.Off;
            cr.receiveShadows = false;
            var iz = g.AddComponent<TrailRenderer>();
            iz.time = izSuresi;
            iz.minVertexDistance = 1.5f;
            iz.startWidth = izGenisligi;
            iz.endWidth = 0f;
            iz.alignment = LineAlignment.View;
            iz.shadowCastingMode = ShadowCastingMode.Off;
            iz.receiveShadows = false;
            iz.sharedMaterial = Parcacik(ad + "_Iz", null, new Color(renk.r * 3f, renk.g * 3f, renk.b * 3f, 1f), true);
            iz.colorGradient = Renkler((0f, new Color(1f, 1f, 1f, 1f)), (1f, new Color(renk.r, renk.g, renk.b, 0f)));
            var so = new SerializedObject(m);
            so.FindProperty("isabetEfekti").objectReferenceValue = isabet;
            so.ApplyModifiedPropertiesWithoutUndo();
            g.layer = c.layer = LayerMask.NameToLayer("Mermi");
            string yol = $"{EfektKlasoru}/{ad}.prefab";
            Kaydet(g, yol);
            return AssetDatabase.LoadAssetAtPath<Mermi>(yol);
        }

        static Bounds DunyaSiniri(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void KatmanAyarla(GameObject go, int katman)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                if (t.GetComponent<ParticleSystem>() == null)
                    t.gameObject.layer = katman;
        }

        static T Ekle<T>(GameObject go, System.Action<SerializedObject> ayarla) where T : Component
        {
            var c = go.AddComponent<T>();
            var so = new SerializedObject(c);
            ayarla(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            return c;
        }

        static void SaglikAyarla(Saglik s, int can, bool yalnizPatlama)
        {
            var so = new SerializedObject(s);
            so.FindProperty("baslangicCani").intValue = can;
            so.FindProperty("taraf").enumValueIndex = (int)Taraf.Dusman;
            so.FindProperty("yalnizPatlama").boolValue = yalnizPatlama;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SusturulabilirAyarla(GameObject go, DusmanTuru tur, Saglik s, Behaviour[] ates, ParticleSystem duman, Transform egilen, Vector3 egilme)
        {
            Ekle<Susturulabilir>(go, so =>
            {
                so.FindProperty("tur").enumValueIndex = (int)tur;
                so.FindProperty("saglik").objectReferenceValue = s;
                Lab7Yapilar.Dizi(so.FindProperty("atesBilesenleri"), ates);
                so.FindProperty("duman").objectReferenceValue = duman;
                so.FindProperty("egilen").objectReferenceValue = egilen;
                so.FindProperty("egilme").vector3Value = egilme;
            });
        }

        // ================================================================== tank

        /// <summary>
        /// The Quaternius tank, scaled to a 7 m hull, front turned from the model's +X to +Z. Turret and gun are moved under
        /// a "Kule" pivot over the turret ring (the tank turns it), "Namlu Ucu" sits at the gun's tip. Health 100, only
        /// explosions hurt (worksheet 06: a tank is silenced only by a bomb).
        /// </summary>
        public static GameObject TankPrefabi(Mermi mermi, GameObject alev, int hedef, int vurulacak)
        {
            var kok = new GameObject("Tank");
            var model = new GameObject("Model").transform;
            model.SetParent(kok.transform, false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(Lab8Varliklar.Model("Tank", "Tank"));
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.SetParent(model, false);
            model.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Transform govde = inst.transform.Find("Tank_body"), kuleM = inst.transform.Find("Tank_Turret"), topM = inst.transform.Find("Tank_Gun");
            if (govde == null || kuleM == null || topM == null)
                throw new System.Exception("Tank model parts (Tank_body, Tank_Turret, Tank_Gun) not found.");
            float olcek = TankBoyu / govde.GetComponent<Renderer>().bounds.size.z;
            model.localScale = Vector3.one * olcek;
            Bounds tum = DunyaSiniri(inst);
            model.localPosition = new Vector3(-tum.center.x, -tum.min.y, -tum.center.z);
            tum = DunyaSiniri(inst);

            Bounds kb = kuleM.GetComponent<Renderer>().bounds;
            var kule = new GameObject("Kule").transform;
            kule.SetParent(kok.transform, false);
            kule.position = new Vector3(kb.center.x, kb.min.y, kb.center.z);
            kuleM.SetParent(kule, true);
            topM.SetParent(kule, true);
            Bounds tb = topM.GetComponent<Renderer>().bounds;
            var uc = new GameObject("Namlu Ucu").transform;
            uc.SetParent(kule, false);
            uc.position = new Vector3(tb.center.x, tb.center.y, tb.max.z + 0.3f);

            var kutu = kok.AddComponent<BoxCollider>();
            kutu.center = tum.center;
            kutu.size = new Vector3(tum.size.x, tum.size.y, tum.size.z * 0.85f);
            ParticleSystem duman = Duman(kok.transform, new Vector3(0f, tum.size.y * 0.8f, 0f), 3.5f, false);
            KatmanAyarla(kok, hedef);

            var saglik = kok.AddComponent<Saglik>();
            SaglikAyarla(saglik, 100, true);
            var tank = Ekle<Tank>(kok, so =>
            {
                so.FindProperty("kule").objectReferenceValue = kule;
                so.FindProperty("namluUcu").objectReferenceValue = uc;
                so.FindProperty("mermiOn").objectReferenceValue = mermi;
                so.FindProperty("namluAlevi").objectReferenceValue = alev;
                so.FindProperty("vurulacak").intValue = vurulacak;
            });
            SusturulabilirAyarla(kok, DusmanTuru.Tank, saglik, new Behaviour[] { tank }, duman, kule, new Vector3(6f, 0f, 9f));
            Kaydet(kok, TankYolu);
            return AssetDatabase.LoadAssetAtPath<GameObject>(TankYolu);
        }

        // ================================================================== AA gun

        /// <summary>The Hafta 7 AA position (sandbag ring, mount, barrel with shield) in daylight: it sees the aircraft
        /// with its own eye (Radar, all round, terrain blocks the view), holds fire above 300 m over the gun (worksheet 05),
        /// health 60, silenced by the machine gun.</summary>
        public static GameObject UcaksavarPrefabi(Mermi mermi, GameObject alev, int hedef, int engel, int vurulacak)
        {
            var celik = Lab8Varliklar.Lit("Ucaksavar_Celik", new Color(0.24f, 0.26f, 0.22f), 0.4f, 0.5f);
            var kum = Lab8Varliklar.Lit("Ucaksavar_Kum", new Color(0.55f, 0.48f, 0.34f), 0.05f);
            var go = new GameObject("Ucaksavar");
            Transform t = go.transform;
            for (int k = 0; k < 12; k++)
            {
                float a = k * 30f;
                Lab7Yapilar.Parca(PrimitiveType.Cube, "Kum Torbasi", t, kum, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.55f, 3.6f), new Vector3(2f, 1.1f, 1f), Quaternion.Euler(0f, a, 0f));
            }
            Lab7Yapilar.Parca(PrimitiveType.Cylinder, "Kaide", t, celik, new Vector3(0f, 1.1f, 0f), new Vector3(1.1f, 0.6f, 1.1f));
            var namlu = Lab7Yapilar.Grup("Namlu", t, new Vector3(0f, 2f, 0f));
            Lab7Yapilar.Parca(PrimitiveType.Cylinder, "Boru", namlu, celik, new Vector3(0f, 0f, 1.6f), new Vector3(0.22f, 1.6f, 0.22f), Quaternion.Euler(90f, 0f, 0f));
            Lab7Yapilar.Parca(PrimitiveType.Cube, "Kalkan", namlu, celik, new Vector3(0f, 0.2f, 0.5f), new Vector3(1.8f, 1.2f, 0.08f));
            var kutu = go.AddComponent<BoxCollider>();
            kutu.center = new Vector3(0f, 1.3f, 0f);
            kutu.size = new Vector3(8f, 2.6f, 8f);
            ParticleSystem duman = Duman(t, new Vector3(0f, 2.5f, 0f), 3f, false);
            KatmanAyarla(go, hedef);

            var radar = go.AddComponent<Radar>();
            radar.YariAci = 180f;
            radar.Menzil = 1000f;
            radar.EngelKatmani = engel;
            radar.GozYuksekligi = 3f;
            radar.DenetimAraligi = 0.2f;
            var saglik = go.AddComponent<Saglik>();
            SaglikAyarla(saglik, 60, false);
            var ua = Ekle<UcaksavarMevzii>(go, so =>
            {
                so.FindProperty("goz").objectReferenceValue = radar;
                so.FindProperty("mermiOn").objectReferenceValue = mermi;
                so.FindProperty("namlu").objectReferenceValue = namlu;
                so.FindProperty("vurulacak").intValue = vurulacak;
                so.FindProperty("menzil").floatValue = 1000f;
                so.FindProperty("atisAraligi").floatValue = 0.35f;
                so.FindProperty("mermiHasari").intValue = 3;
                so.FindProperty("atesTavani").floatValue = KursunDaglariHaritasi.UcaksavarTavani;
                so.FindProperty("taraf").enumValueIndex = (int)Taraf.Dusman;
            });
            SusturulabilirAyarla(go, DusmanTuru.Ucaksavar, saglik, new Behaviour[] { ua, radar }, duman, namlu, new Vector3(35f, 0f, 0f));
            Kaydet(go, UcaksavarYolu);
            return AssetDatabase.LoadAssetAtPath<GameObject>(UcaksavarYolu);
        }

        // ================================================================== enemy aircraft

        /// <summary>The Poly Pizza "Aeroplane" (Gilang Romadhan, CC BY 3.0) with its texture, 11 m span, nose +Z; kinematic
        /// body with a sphere collider on Hedef; two wing guns; a smoke trail when hit. Health 80.</summary>
        public static GameObject DusmanUcagiPrefabi(Mermi mermi, GameObject alev, int hedef, int vurulacak)
        {
            var doku = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Lab8Varliklar.Modeller}/DusmanUcagi/DusmanUcagi_Doku.png");
            var mat = Lab8Varliklar.Lit("DusmanUcagi", Color.white, 0.35f, 0f, doku);
            var kok = new GameObject("DusmanUcagi");
            var model = new GameObject("Model").transform;
            model.SetParent(kok.transform, false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(Lab8Varliklar.Model("DusmanUcagi", "DusmanUcagi"));
            inst.transform.SetParent(model, false);
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
            Bounds b = DunyaSiniri(inst);
            model.localScale = Vector3.one * (DusmanKanatAcikligi / b.size.x);
            b = DunyaSiniri(inst);
            model.localPosition = -b.center;
            b = DunyaSiniri(inst);

            var kure = kok.AddComponent<SphereCollider>();
            kure.radius = DusmanKanatAcikligi * 0.42f;
            var govde = kok.AddComponent<Rigidbody>();
            govde.isKinematic = true;
            govde.useGravity = false;
            govde.interpolation = RigidbodyInterpolation.Interpolate;
            var n1 = Lab7Yapilar.Grup("Namlu Sol", kok.transform, new Vector3(-2.2f, 0f, 1.5f));
            var n2 = Lab7Yapilar.Grup("Namlu Sag", kok.transform, new Vector3(2.2f, 0f, 1.5f));
            ParticleSystem iz = Duman(kok.transform, new Vector3(0f, 0.3f, b.extents.z * 0.5f), 2.2f, true);
            KatmanAyarla(kok, hedef);

            var saglik = kok.AddComponent<Saglik>();
            SaglikAyarla(saglik, 80, false);
            Ekle<DusmanUcagi>(kok, so =>
            {
                so.FindProperty("saglik").objectReferenceValue = saglik;
                so.FindProperty("mermiOn").objectReferenceValue = mermi;
                so.FindProperty("namluAlevi").objectReferenceValue = alev;
                Lab7Yapilar.Dizi(so.FindProperty("namlular"), new[] { n1, n2 });
                so.FindProperty("dumanIzi").objectReferenceValue = iz;
                so.FindProperty("vurulacak").intValue = vurulacak;
            });
            SusturulabilirAyarla(kok, DusmanTuru.Ucak, saglik, new Behaviour[0], null, null, Vector3.zero);
            Kaydet(kok, DusmanUcagiYolu);
            return AssetDatabase.LoadAssetAtPath<GameObject>(DusmanUcagiYolu);
        }
    }
}
