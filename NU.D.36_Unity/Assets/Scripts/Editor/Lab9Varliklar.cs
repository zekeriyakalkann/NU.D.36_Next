using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using D = NU36.Editor.Lab8Dusmanlar;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 9 (Bölüm 5 · Kartal Geçidi): the generated assets under <c>Assets/Bolum5</c>.
    /// <list type="bullet">
    /// <item><b>CikarmaTanki.prefab</b>: the Quaternius tank of Bölüm 4 (CC0), 7 m hull, unarmed, <see cref="CikarmaTanki"/>;</item>
    /// <item><b>CikarmaGemisi.prefab</b>: a tank landing ship built from primitives (46 × 10 m hull, open well deck, stern
    /// deckhouse, hinged bow ramp) with three tanks on the deck; no crew figure, no flag or emblem (worksheet: no people);</item>
    /// <item>sea material with a generated ripple normal map, the sand terrain layer (Poly Haven coast_sand_01, CC0),
    /// beach obstacles material.</item>
    /// </list>
    /// </summary>
    public static class Lab9Varliklar
    {
        public const string Kok = "Assets/Bolum5";
        public const string Malzemeler = Kok + "/Malzemeler", Prefablar = Kok + "/Prefablar", Dokular = Kok + "/Dokular";
        public const string AraziKlasoru = Kok + "/Arazi", Uretilen = Kok + "/Uretilen";
        public const string TankYolu = Prefablar + "/CikarmaTanki.prefab", GemiYolu = Prefablar + "/CikarmaGemisi.prefab";
        public const string SicramaYolu = Prefablar + "/Sicrama.prefab";
        public const float TankBoyu = 7f, GemiBoyu = 46f, GemiEni = 10f;
        public const int GemiCani = 240;
        /// <summary>Local z of the three tanks on the deck, front first.</summary>
        public static readonly float[] TankYerleri = { 13.5f, 5f, -3.5f };
        /// <summary>Well deck height over the waterline, bow edge (local z), ramp length, top of the side walls.</summary>
        public const float GuverteY = 1.6f, PruvaZ = 20.3f, RampaBoyu = 5.5f, BordaUstu = 3.7f;

        // ---------------------------------------------------------------- materials

        public static Material Lit(string ad, Color renk, float parlaklik = 0.2f, float metal = 0f, Texture doku = null, Texture normal = null, Vector2? tekrar = null)
        {
            Lab8Varliklar.Klasor(Malzemeler);
            string yol = $"{Malzemeler}/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, yol);
            }
            m.SetColor("_BaseColor", renk);
            m.SetFloat("_Smoothness", parlaklik);
            m.SetFloat("_Metallic", metal);
            m.SetTexture("_BaseMap", doku);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 0.6f);
                m.EnableKeyword("_NORMALMAP");
            }
            if (tekrar.HasValue) m.SetTextureScale("_BaseMap", tekrar.Value);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Parcacik(string ad, Texture2D doku, Color renk, bool eklemeli)
        {
            Lab8Varliklar.Klasor(Malzemeler);
            string yol = $"{Malzemeler}/{ad}.mat";
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

        public sealed class Mat
        {
            public Material Govde, Guverte, Kirmizi, Koyu, Cam, Engel;
            public Mat()
            {
                Govde = Lit("Gemi_Govde", new Color(0.36f, 0.39f, 0.41f), 0.35f, 0.25f);
                Guverte = Lit("Gemi_Guverte", new Color(0.22f, 0.23f, 0.23f), 0.15f, 0.1f);
                Kirmizi = Lit("Gemi_Karina", new Color(0.42f, 0.13f, 0.1f), 0.2f);
                Koyu = Lit("Gemi_Koyu", new Color(0.1f, 0.1f, 0.1f), 0.3f, 0.3f);
                Cam = Lit("Gemi_Cam", new Color(0.06f, 0.1f, 0.14f), 0.92f);
                Engel = Lit("Kumsal_Engel", new Color(0.27f, 0.24f, 0.21f), 0.3f, 0.4f);
            }
        }

        /// <summary>A tileable ripple normal map (RGB = normal, linear), generated from periodic noise.</summary>
        public static Texture2D DalgaDokusu()
        {
            string yol = $"{Uretilen}/DalgaNormal.asset";
            var mevcut = AssetDatabase.LoadAssetAtPath<Texture2D>(yol);
            if (mevcut != null) return mevcut;
            Lab8Varliklar.Klasor(Uretilen);
            const int n = 256;
            var h = new float[n, n];
            var rnd = new System.Random(905);
            var dalgalar = Enumerable.Range(0, 14).Select(_ => (kx: rnd.Next(1, 9) * (rnd.NextDouble() < 0.5 ? -1 : 1), kz: rnd.Next(1, 9), faz: rnd.NextDouble() * 6.283, gen: 1.0 / (1 + rnd.Next(1, 6)))).ToArray();
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    double v = 0;
                    foreach (var d in dalgalar)
                        v += d.gen * System.Math.Sin(6.283 * (d.kx * i + d.kz * j) / n + d.faz);
                    h[i, j] = (float)v;
                }
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float dx = h[(i + 1) % n, j] - h[(i + n - 1) % n, j], dz = h[i, (j + 1) % n] - h[i, (j + n - 1) % n];
                    Vector3 nn = new Vector3(-dx * 0.35f, -dz * 0.35f, 1f).normalized;
                    px[j * n + i] = new Color(nn.x * 0.5f + 0.5f, nn.y * 0.5f + 0.5f, nn.z * 0.5f + 0.5f, 1f);
                }
            t.SetPixels(px);
            t.Apply(true);
            AssetDatabase.CreateAsset(t, yol);
            return t;
        }

        public static Material DenizMalzemesi()
        {
            Texture2D normal = DalgaDokusu();
            // The plane is 30 km wide: the ripple tile repeats every 20 m.
            return Lit("Deniz", new Color(0.05f, 0.18f, 0.24f), 0.94f, 0f, null, normal, new Vector2(1500f, 1500f));
        }

        public static TerrainLayer KumKatmani()
        {
            Lab8Varliklar.Klasor(AraziKlasoru);
            string yol = $"{AraziKlasoru}/Kum.terrainlayer";
            var k = AssetDatabase.LoadAssetAtPath<TerrainLayer>(yol);
            if (k == null)
            {
                k = new TerrainLayer();
                AssetDatabase.CreateAsset(k, yol);
            }
            k.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dokular}/coast_sand_01_diff_2k.jpg");
            k.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dokular}/coast_sand_01_nor_gl_2k.jpg");
            if (k.diffuseTexture == null)
                throw new System.Exception("Sand texture missing: Assets/Bolum5/Dokular/coast_sand_01_diff_2k.jpg");
            k.tileSize = new Vector2(8f, 8f);
            k.smoothness = 0.05f;
            k.diffuseRemapMin = Vector4.zero;
            k.diffuseRemapMax = new Vector4(1f, 0.97f, 0.9f, 1f);
            EditorUtility.SetDirty(k);
            return k;
        }

        // ---------------------------------------------------------------- landing tank

        /// <summary>The Bölüm 4 tank model, 7 m hull, front +Z, base at y = 0; box collider (Hedef layer); unarmed.</summary>
        public static GameObject TankPrefabi(int hedef)
        {
            var kok = new GameObject("CikarmaTanki");
            var model = new GameObject("Model").transform;
            model.SetParent(kok.transform, false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(Lab8Varliklar.Model("Tank", "Tank"));
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.SetParent(model, false);
            model.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Transform govde = inst.transform.Find("Tank_body");
            if (govde == null) throw new System.Exception("Tank model part Tank_body not found.");
            model.localScale = Vector3.one * (TankBoyu / govde.GetComponent<Renderer>().bounds.size.z);
            Bounds b = D.DunyaSiniri(inst);
            model.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            b = D.DunyaSiniri(inst);
            var kutu = kok.AddComponent<BoxCollider>();
            kutu.center = b.center;
            kutu.size = b.size;
            D.KatmanAyarla(kok, hedef);
            ParticleSystem toz = Toz(kok.transform, new Vector3(0f, 0.4f, -b.extents.z));
            D.Ekle<CikarmaTanki>(kok, so =>
            {
                so.FindProperty("toz").objectReferenceValue = toz;
                so.FindProperty("sicramaOn").objectReferenceValue = SicramaPrefabi();
            });
            Lab8Varliklar.Klasor(Prefablar);
            D.Kaydet(kok, TankYolu);
            return AssetDatabase.LoadAssetAtPath<GameObject>(TankYolu);
        }

        // ---------------------------------------------------------------- landing ship

        static GameObject Kutu(string ad, Transform ust, Material mat, Vector3 konum, Vector3 olcek, Quaternion? donus = null) =>
            Lab7Yapilar.Parca(PrimitiveType.Cube, ad, ust, mat, konum, olcek, donus);

        static GameObject Silindir(string ad, Transform ust, Material mat, Vector3 konum, Vector3 olcek) =>
            Lab7Yapilar.Parca(PrimitiveType.Cylinder, ad, ust, mat, konum, olcek);

        /// <summary>
        /// The landing ship. Local +Z = bow, y = 0 the waterline. Red bottom 1.5 m under the water, grey hull up to the well
        /// deck at <see cref="GuverteY"/>, low side walls (tank turrets show over them), the stern block with deckhouse,
        /// bridge, mast and funnel; the bow ramp ("Rampa", <see cref="RampaBoyu"/> m, rails and treads) hinges at the front
        /// edge of the deck: −80° closed, lowered at run time until its tip rests on the sand. Three tanks on the deck.
        /// Kinematic body, box collider on Hedef, health 240 (enemy side), silenced → smoke; wake, splash and fire effects.
        /// </summary>
        public static GameObject GemiPrefabi(GameObject tankOn, int hedef)
        {
            var m = new Mat();
            var kok = new GameObject("CikarmaGemisi");
            Transform t = kok.transform;
            float L = GemiBoyu, B = GemiEni, Y = GuverteY, U = BordaUstu;
            // Hull: red bottom under the waterline, grey sides to the deck, deck floor, side walls.
            Kutu("Karina", t, m.Kirmizi, new Vector3(0f, -0.6f, 0f), new Vector3(B, 1.8f, L));
            Kutu("Govde", t, m.Govde, new Vector3(0f, (0.3f + Y) / 2f, 0f), new Vector3(B, Y - 0.3f, L));
            Kutu("Guverte", t, m.Guverte, new Vector3(0f, Y - 0.1f, -0.5f), new Vector3(B - 1f, 0.22f, L - 1f));
            Kutu("Borda Sol", t, m.Govde, new Vector3(-B / 2f + 0.25f, (Y + U) / 2f, -0.5f), new Vector3(0.5f, U - Y, L - 1f));
            Kutu("Borda Sag", t, m.Govde, new Vector3(B / 2f - 0.25f, (Y + U) / 2f, -0.5f), new Vector3(0.5f, U - Y, L - 1f));
            Kutu("Borda Bandi Sol", t, m.Koyu, new Vector3(-B / 2f - 0.02f, 0.32f, 0f), new Vector3(0.06f, 0.22f, L));
            Kutu("Borda Bandi Sag", t, m.Koyu, new Vector3(B / 2f + 0.02f, 0.32f, 0f), new Vector3(0.06f, 0.22f, L));
            Kutu("Kupeste Sol", t, m.Koyu, new Vector3(-B / 2f + 0.25f, U + 0.05f, -0.5f), new Vector3(0.6f, 0.1f, L - 1f));
            Kutu("Kupeste Sag", t, m.Koyu, new Vector3(B / 2f - 0.25f, U + 0.05f, -0.5f), new Vector3(0.6f, 0.1f, L - 1f));
            // Bow corners beside the ramp, with the ramp's hoisting posts.
            Kutu("Pruva Sol", t, m.Govde, new Vector3(-4.3f, (Y + U) / 2f + 0.2f, PruvaZ - 0.6f), new Vector3(1.4f, U - Y + 0.4f, 1.6f));
            Kutu("Pruva Sag", t, m.Govde, new Vector3(4.3f, (Y + U) / 2f + 0.2f, PruvaZ - 0.6f), new Vector3(1.4f, U - Y + 0.4f, 1.6f));
            Silindir("Rampa Diregi Sol", t, m.Koyu, new Vector3(-4.3f, U + 1.4f, PruvaZ - 1f), new Vector3(0.3f, 1.2f, 0.3f));
            Silindir("Rampa Diregi Sag", t, m.Koyu, new Vector3(4.3f, U + 1.4f, PruvaZ - 1f), new Vector3(0.3f, 1.2f, 0.3f));
            // Stern block, deckhouse, bridge (dark windows; nobody behind them), mast, funnel.
            Kutu("Kic", t, m.Govde, new Vector3(0f, (Y + 4.1f) / 2f, -19.5f), new Vector3(B - 0.4f, 4.1f - Y, 7f));
            Kutu("Kamara", t, m.Govde, new Vector3(0f, 5.3f, -19.8f), new Vector3(6.2f, 2.4f, 4.6f));
            Kutu("Kopru", t, m.Govde, new Vector3(0f, 7.2f, -19.1f), new Vector3(4.4f, 1.4f, 2.8f));
            Kutu("Pencereler", t, m.Cam, new Vector3(0f, 7.35f, -17.68f), new Vector3(3.9f, 0.5f, 0.06f));
            Kutu("Pencereler Sol", t, m.Cam, new Vector3(-2.22f, 7.35f, -19.1f), new Vector3(0.06f, 0.5f, 2.2f));
            Kutu("Pencereler Sag", t, m.Cam, new Vector3(2.22f, 7.35f, -19.1f), new Vector3(0.06f, 0.5f, 2.2f));
            Silindir("Direk", t, m.Koyu, new Vector3(0f, 10.3f, -19.6f), new Vector3(0.22f, 2.4f, 0.22f));
            Kutu("Direk Capraz", t, m.Koyu, new Vector3(0f, 11.5f, -19.6f), new Vector3(2.6f, 0.12f, 0.12f));
            Silindir("Baca", t, m.Koyu, new Vector3(0f, 7.1f, -22f), new Vector3(1.3f, 1.3f, 1.3f));
            Kutu("Kic Korkulugu", t, m.Koyu, new Vector3(0f, 4.45f, -22.9f), new Vector3(B - 0.6f, 0.7f, 0.1f));
            // Bow ramp: pivot at the hinge, plate along local +Z with side rails and treads, tip at its end.
            var rampa = Lab7Yapilar.Grup("Rampa", t, new Vector3(0f, Y, PruvaZ));
            Kutu("Rampa Plakasi", rampa, m.Govde, new Vector3(0f, 0f, RampaBoyu / 2f), new Vector3(7.6f, 0.3f, RampaBoyu));
            Kutu("Rampa Kenari Sol", rampa, m.Koyu, new Vector3(-3.7f, 0.3f, RampaBoyu / 2f), new Vector3(0.22f, 0.5f, RampaBoyu));
            Kutu("Rampa Kenari Sag", rampa, m.Koyu, new Vector3(3.7f, 0.3f, RampaBoyu / 2f), new Vector3(0.22f, 0.5f, RampaBoyu));
            for (int k = 0; k < 6; k++)
                Kutu("Rampa Citasi", rampa, m.Koyu, new Vector3(0f, 0.18f, 0.5f + k * 0.9f), new Vector3(7.0f, 0.07f, 0.14f));
            var uc = Lab7Yapilar.Grup("Rampa Ucu", rampa, new Vector3(0f, 0f, RampaBoyu));
            rampa.localRotation = Quaternion.Euler(-80f, 0f, 0f);

            // Tanks on the well deck, front first.
            var tanklar = new CikarmaTanki[TankYerleri.Length];
            for (int i = 0; i < TankYerleri.Length; i++)
            {
                var tk = (GameObject)PrefabUtility.InstantiatePrefab(tankOn, t);
                tk.name = $"Tank {i + 1}";
                tk.transform.localPosition = new Vector3(0f, GuverteY, TankYerleri[i]);
                tk.transform.localRotation = Quaternion.identity;
                tanklar[i] = tk.GetComponent<CikarmaTanki>();
            }

            // Effects: wake at the stern and the bow, smoke and fire when stopped.
            Texture2D dumanDoku = Lab7Varliklar.DumanDokusu();
            var kopukMat = Parcacik("Kopuk", dumanDoku, new Color(0.92f, 0.95f, 0.97f, 0.75f), false);
            ParticleSystem kopuk = Kopuk(t, kopukMat, new Vector3(0f, 0.15f, -L / 2f - 1f), 3f);
            ParticleSystem pruvaKopugu = Kopuk(t, kopukMat, new Vector3(0f, 0.15f, L / 2f), 2f);
            pruvaKopugu.transform.SetParent(kopuk.transform, true);
            ParticleSystem duman = D.Duman(t, new Vector3(0f, 4.5f, -6f), 7f, false);
            ParticleSystem yangin = Yangin(t, Parcacik("GemiYangini", Lab7Varliklar.AtesDokusu(), new Color(3.2f, 1.5f, 0.45f, 1f), true));

            var kutu = kok.AddComponent<BoxCollider>();
            kutu.center = new Vector3(0f, (U - 1.5f) / 2f, 0f);
            kutu.size = new Vector3(B, U + 1.5f, L);
            var govde = kok.AddComponent<Rigidbody>();
            govde.isKinematic = true;
            govde.useGravity = false;
            govde.interpolation = RigidbodyInterpolation.Interpolate;
            D.KatmanAyarla(kok, hedef);

            var saglik = kok.AddComponent<Saglik>();
            D.SaglikAyarla(saglik, GemiCani, false);
            D.SusturulabilirAyarla(kok, DusmanTuru.Gemi, saglik, new Behaviour[0], duman, null, Vector3.zero);
            var sus = kok.GetComponent<Susturulabilir>();
            sus.SerbestHedef = true;
            D.Ekle<CikarmaGemisi>(kok, so =>
            {
                so.FindProperty("saglik").objectReferenceValue = saglik;
                so.FindProperty("susturulabilir").objectReferenceValue = sus;
                so.FindProperty("rampa").objectReferenceValue = rampa;
                so.FindProperty("rampaUcu").objectReferenceValue = uc;
                Lab7Yapilar.Dizi(so.FindProperty("tanklar"), tanklar);
                so.FindProperty("kopuk").objectReferenceValue = kopuk;
                so.FindProperty("yangin").objectReferenceValue = yangin;
                so.FindProperty("pruvaZ").floatValue = PruvaZ;
                so.FindProperty("guverteY").floatValue = GuverteY;
                so.FindProperty("sicramaOn").objectReferenceValue = SicramaPrefabi();
            });
            Lab8Varliklar.Klasor(Prefablar);
            D.Kaydet(kok, GemiYolu);
            return AssetDatabase.LoadAssetAtPath<GameObject>(GemiYolu);
        }

        /// <summary>White foam lying on the water (horizontal billboards), world space, spreading and fading.</summary>
        static ParticleSystem Kopuk(Transform ust, Material mat, Vector3 yerel, float boy)
        {
            ParticleSystem ps = D.Sistem("Kopuk", ust, mat, 400, true);
            ps.transform.localPosition = yerel;
            var ana = ps.main;
            ana.loop = true;
            ana.duration = 5f;
            ana.playOnAwake = false;
            ana.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
            ana.startSize = new ParticleSystem.MinMaxCurve(boy * 0.8f, boy * 1.3f);
            ana.startSpeed = 0f;
            ana.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var e = ps.emission;
            e.rateOverTime = 0f;
            e.rateOverDistance = 1.2f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(GemiEni * 0.8f, 0.1f, 1f);
            var b = ps.sizeOverLifetime;
            b.enabled = true;
            b.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.8f, 1f, 3f));
            var c = ps.colorOverLifetime;
            c.enabled = true;
            c.color = D.Renkler((0f, new Color(1f, 1f, 1f, 0f)), (0.1f, new Color(1f, 1f, 1f, 0.8f)), (1f, new Color(1f, 1f, 1f, 0f)));
            ps.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            return ps;
        }

        /// <summary>Flames on the deck of a stopped ship (inactive child, started when it is hit).</summary>
        static ParticleSystem Yangin(Transform ust, Material mat)
        {
            ParticleSystem ps = D.Sistem("Yangin", ust, mat, 120, true);
            ps.transform.localPosition = new Vector3(0f, GuverteY + 0.8f, -4f);
            var ana = ps.main;
            ana.loop = true;
            ana.duration = 2f;
            ana.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            ana.startSize = new ParticleSystem.MinMaxCurve(2f, 4f);
            ana.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            ana.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var e = ps.emission;
            e.rateOverTime = 30f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(6f, 0.5f, 16f);
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var b = ps.sizeOverLifetime;
            b.enabled = true;
            b.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
            var c = ps.colorOverLifetime;
            c.enabled = true;
            c.color = D.Renkler((0f, new Color(1f, 1f, 1f, 0f)), (0.15f, new Color(1f, 1f, 1f, 1f)), (1f, new Color(1f, 0.6f, 0.3f, 0f)));
            ps.gameObject.SetActive(false);
            return ps;
        }

        /// <summary>Sand dust behind a tank's tracks (world space; the tank switches the emission on while it drives on land).</summary>
        static ParticleSystem Toz(Transform ust, Vector3 yerel)
        {
            var mat = Parcacik("Toz", Lab7Varliklar.DumanDokusu(), new Color(0.80f, 0.72f, 0.55f, 0.6f), false);
            ParticleSystem ps = D.Sistem("Toz", ust, mat, 160, true);
            ps.transform.localPosition = yerel;
            var ana = ps.main;
            ana.loop = true;
            ana.duration = 5f;
            ana.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3f);
            ana.startSize = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
            ana.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            ana.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ana.gravityModifier = -0.02f;
            var e = ps.emission;
            e.rateOverTime = 0f;
            e.rateOverDistance = 1.4f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(3.2f, 0.2f, 0.5f);
            var b = ps.sizeOverLifetime;
            b.enabled = true;
            b.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 2.4f));
            var c = ps.colorOverLifetime;
            c.enabled = true;
            c.color = D.Renkler((0f, new Color(1f, 1f, 1f, 0f)), (0.12f, new Color(1f, 1f, 1f, 0.7f)), (1f, new Color(1f, 1f, 1f, 0f)));
            return ps;
        }

        /// <summary>A burst of water and wet sand (bow beaching, ramp tip, tank off the ramp); removes itself.</summary>
        public static GameObject SicramaPrefabi()
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<GameObject>(SicramaYolu);
            if (mevcut != null) return mevcut;
            var mat = Parcacik("Sicrama", Lab7Varliklar.DumanDokusu(), new Color(0.93f, 0.95f, 0.97f, 0.85f), false);
            ParticleSystem ps = D.Sistem("Sicrama", null, mat, 60, true);
            var ana = ps.main;
            ana.loop = false;
            ana.duration = 0.4f;
            ana.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            ana.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            ana.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            ana.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ana.gravityModifier = 1.1f;
            ana.stopAction = ParticleSystemStopAction.Destroy;
            var e = ps.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 35f;
            sh.radius = 1.5f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var b = ps.sizeOverLifetime;
            b.enabled = true;
            b.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.8f));
            var c = ps.colorOverLifetime;
            c.enabled = true;
            c.color = D.Renkler((0f, new Color(1f, 1f, 1f, 0.9f)), (1f, new Color(1f, 1f, 1f, 0f)));
            Lab8Varliklar.Klasor(Prefablar);
            D.Kaydet(ps.gameObject, SicramaYolu);
            return AssetDatabase.LoadAssetAtPath<GameObject>(SicramaYolu);
        }

        // ---------------------------------------------------------------- beach obstacles

        /// <summary>A steel beach obstacle ("Czech hedgehog"): three crossed beams, 2 m. Decoration, no collider.</summary>
        public static GameObject Engel(Transform ust, Material mat, Vector3 konum, float yon)
        {
            var g = new GameObject("Kumsal Engeli");
            g.transform.SetParent(ust, false);
            g.transform.SetPositionAndRotation(konum, Quaternion.Euler(0f, yon, 0f));
            Kutu("Kiris", g.transform, mat, new Vector3(0f, 0.75f, 0f), new Vector3(0.18f, 0.18f, 2.4f), Quaternion.Euler(35f, 0f, 0f));
            Kutu("Kiris", g.transform, mat, new Vector3(0f, 0.75f, 0f), new Vector3(0.18f, 0.18f, 2.4f), Quaternion.Euler(35f, 120f, 0f));
            Kutu("Kiris", g.transform, mat, new Vector3(0f, 0.75f, 0f), new Vector3(0.18f, 0.18f, 2.4f), Quaternion.Euler(35f, 240f, 0f));
            return g;
        }
    }
}
