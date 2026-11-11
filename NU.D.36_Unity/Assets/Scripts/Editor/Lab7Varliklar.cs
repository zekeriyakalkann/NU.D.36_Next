using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 7 assets made by code (deterministic, no downloads): textures (terrain colour map, detail noise, asphalt, tree palette,
    /// smoke/fire/spark sprites), materials, beam meshes and the effect prefabs (bomb blast with smoke, burning wreck).
    /// Each asset is created once under Assets/Lab7 and reused when it already exists.
    /// </summary>
    public static class Lab7Varliklar
    {
        public const string Klasor = "Assets/Lab7";
        public const string Dokular = Klasor + "/Textures";
        public const string Malzemeler = Klasor + "/Materials";
        public const string Prefablar = Klasor + "/Prefabs";
        public const string Meshler = Klasor + "/Meshes";
        public const string RenkHaritasiYolu = Dokular + "/Arazi_Renk.jpg";
        public const int RenkHaritasiCozunurlugu = 2048;

        static void Klasorler()
        {
            foreach (string k in new[] { Klasor, Dokular, Malzemeler, Prefablar, Meshler })
                Directory.CreateDirectory(k);
        }

        // ================================================================== textures

        /// <summary>Writes a texture file into the project and imports it with the given settings.</summary>
        static Texture2D DokuKaydet(string yol, byte[] veri, bool srgb, TextureWrapMode sarma, FilterMode suzme, bool alfa, int enBuyuk = 2048, bool mip = true)
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<Texture2D>(yol);
            if (mevcut != null) return mevcut;
            Klasorler();
            File.WriteAllBytes(yol, veri);
            AssetDatabase.ImportAsset(yol, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(yol);
            imp.sRGBTexture = srgb;
            imp.wrapMode = sarma;
            imp.filterMode = suzme;
            imp.alphaSource = alfa ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            imp.alphaIsTransparency = alfa;
            imp.mipmapEnabled = mip;
            imp.anisoLevel = 4;
            imp.maxTextureSize = enBuyuk;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(yol);
        }

        /// <summary>Terrain colour map: <see cref="GeceHaritasi.Renk"/> sampled at the centre of every texel over the 8 km square.</summary>
        public static Texture2D RenkHaritasi()
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<Texture2D>(RenkHaritasiYolu);
            if (mevcut != null) return mevcut;
            int n = RenkHaritasiCozunurlugu;
            var px = new Color32[n * n];
            GeceHaritasi.YolUzakligi(0f, 0f);   // build the road cache before the parallel loop
            Parallel.For(0, n, j =>
            {
                for (int i = 0; i < n; i++)
                {
                    float x = -GeceHaritasi.YarimBoyut + (i + 0.5f) * GeceHaritasi.Boyut / n;
                    float z = -GeceHaritasi.YarimBoyut + (j + 0.5f) * GeceHaritasi.Boyut / n;
                    px[j * n + i] = GeceHaritasi.Renk(x, z);
                }
            });
            var t = new Texture2D(n, n, TextureFormat.RGB24, false);
            t.SetPixels32(px);
            t.Apply();
            byte[] jpg = t.EncodeToJPG(92);
            Object.DestroyImmediate(t);
            return DokuKaydet(RenkHaritasiYolu, jpg, true, TextureWrapMode.Clamp, FilterMode.Trilinear, false);
        }

        /// <summary>Periodic value noise in 0..1 on an n×n tile (lattice <paramref name="hucre"/>), smooth, tileable.</summary>
        static float PeriyodikGurultu(float x, float y, int hucre, uint tohum)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            float H(int a, int b) => AraziUretici.Rastgele01(((a % hucre) + hucre) % hucre, ((b % hucre) + hucre) % hucre, tohum);
            float u = fx * fx * (3f - 2f * fx), v = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(H(x0, y0), H(x0 + 1, y0), u), Mathf.Lerp(H(x0, y0 + 1), H(x0 + 1, y0 + 1), u), v);
        }

        static float PeriyodikFbm(float u, float v, int taban, int oktav, uint tohum)
        {
            float t = 0f, g = 1f, norm = 0f;
            int h = taban;
            for (int o = 0; o < oktav; o++)
            {
                t += PeriyodikGurultu(u * h, v * h, h, tohum + (uint)o * 31u) * g;
                norm += g;
                g *= 0.5f;
                h *= 2;
            }
            return t / norm;
        }

        static byte[] GriDoku(int n, System.Func<float, float, float> f, bool png = true)
        {
            var t = new Texture2D(n, n, TextureFormat.RGB24, false);
            var px = new Color32[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(f((float)i / n, (float)j / n) * 255f), 0, 255);
                    px[j * n + i] = new Color32(g, g, g, 255);
                }
            t.SetPixels32(px);
            t.Apply();
            byte[] veri = png ? t.EncodeToPNG() : t.EncodeToJPG(92);
            Object.DestroyImmediate(t);
            return veri;
        }

        /// <summary>Grey detail texture around 0.5 (URP Lit "×2" detail): close-up grain on the terrain.</summary>
        public static Texture2D AyrintiDokusu() => DokuKaydet(Dokular + "/Arazi_Ayrinti.png",
            GriDoku(256, (u, v) => 0.5f + (PeriyodikFbm(u, v, 8, 4, 701u) - 0.5f) * 0.9f), false, TextureWrapMode.Repeat, FilterMode.Trilinear, false, 256);

        public static Texture2D AsfaltDokusu() => DokuKaydet(Dokular + "/Asfalt.png",
            GriDoku(512, (u, v) =>
            {
                float a = 0.33f + (PeriyodikFbm(u, v, 16, 4, 711u) - 0.5f) * 0.18f;
                float leke = PeriyodikFbm(u, v, 4, 2, 719u);
                return a * (leke > 0.62f ? 0.85f : 1f) + (AraziUretici.Rastgele01((int)(u * 512), (int)(v * 512), 727u) - 0.5f) * 0.05f;
            }), true, TextureWrapMode.Repeat, FilterMode.Trilinear, false, 512);

        /// <summary>5 × 4 palette for the trees: columns 0–3 foliage greens, column 4 trunk; rows darker → lighter.</summary>
        public static Texture2D AgacPaleti()
        {
            string yol = Dokular + "/Agac_Paleti.png";
            var mevcut = AssetDatabase.LoadAssetAtPath<Texture2D>(yol);
            if (mevcut != null) return mevcut;
            Color[] yesil = { new Color(0.09f, 0.17f, 0.08f), new Color(0.12f, 0.20f, 0.09f), new Color(0.07f, 0.14f, 0.09f), new Color(0.14f, 0.19f, 0.07f) };
            Color govde = new Color(0.20f, 0.14f, 0.09f);
            var t = new Texture2D(5, 4, TextureFormat.RGB24, false);
            for (int j = 0; j < 4; j++)
                for (int i = 0; i < 5; i++)
                    t.SetPixel(i, j, (i < 4 ? yesil[i] : govde) * (0.75f + 0.15f * j));
            t.Apply();
            byte[] png = t.EncodeToPNG();
            Object.DestroyImmediate(t);
            return DokuKaydet(yol, png, true, TextureWrapMode.Clamp, FilterMode.Point, false, 32, false);
        }

        static Texture2D SpriteDoku(string ad, System.Func<float, float, Color> f, int n = 128)
        {
            string yol = $"{Dokular}/{ad}.png";
            var mevcut = AssetDatabase.LoadAssetAtPath<Texture2D>(yol);
            if (mevcut != null) return mevcut;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                    t.SetPixel(i, j, f((i + 0.5f) / n * 2f - 1f, (j + 0.5f) / n * 2f - 1f));
            t.Apply();
            byte[] png = t.EncodeToPNG();
            Object.DestroyImmediate(t);
            return DokuKaydet(yol, png, true, TextureWrapMode.Clamp, FilterMode.Bilinear, true, 256);
        }

        /// <summary>Soft, lumpy smoke puff (white; the particle colour tints it).</summary>
        public static Texture2D DumanDokusu() => SpriteDoku("Duman", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float n = PeriyodikFbm((x + 1f) * 0.5f, (y + 1f) * 0.5f, 4, 4, 733u);
            float a = Mathf.Clamp01(1f - r * (1.05f - 0.35f * n)) ;
            a = a * a * (0.55f + 0.6f * n);
            float g = 0.75f + 0.25f * n;
            return new Color(g, g, g, Mathf.Clamp01(a));
        });

        /// <summary>Fire blob: white-hot centre, soft edge.</summary>
        public static Texture2D AtesDokusu() => SpriteDoku("Ates", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float n = PeriyodikFbm((x + 1f) * 0.5f, (y + 1f) * 0.5f, 3, 3, 739u);
            float a = Mathf.Clamp01(1f - r * (1.1f - 0.4f * n));
            return new Color(1f, 1f, 1f, a * a);
        });

        public static Texture2D KivilcimDokusu() => SpriteDoku("Kivilcim", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Clamp01(1f - r);
            return new Color(1f, 1f, 1f, Mathf.Pow(a, 3f));
        }, 32);

        // ================================================================== materials

        public static Material Lit(string ad, Color renk, float puruzsuzluk = 0.15f, Color? isima = null, Texture2D doku = null, Vector2? doseme = null, float metal = 0f)
        {
            string yol = $"{Malzemeler}/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m != null) return m;
            Klasorler();
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", renk);
            m.SetFloat("_Smoothness", puruzsuzluk);
            m.SetFloat("_Metallic", metal);
            if (doku != null)
            {
                m.SetTexture("_BaseMap", doku);
                if (doseme.HasValue)
                    m.SetTextureScale("_BaseMap", doseme.Value);
            }
            if (isima.HasValue)
            {
                m.SetColor("_EmissionColor", isima.Value);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            m.enableInstancing = true;
            AssetDatabase.CreateAsset(m, yol);
            return m;
        }

        /// <summary>
        /// Glowing bulb or lit window: URP Unlit with an HDR colour above 1, so bloom makes it glow. (An emissive Lit material
        /// needs the _EMISSION keyword, which the material import validation dropped in batch mode; Unlit needs no keyword.)
        /// </summary>
        public static Material Ampul(string ad, Color renk, float guc) => Ozel(ad, "Universal Render Pipeline/Unlit", m =>
        {
            m.SetColor("_BaseColor", new Color(renk.r * guc, renk.g * guc, renk.b * guc, 1f));
            m.enableInstancing = true;
        });

        public static Material Ozel(string ad, string shader, System.Action<Material> ayarla)
        {
            string yol = $"{Malzemeler}/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m != null) return m;
            Klasorler();
            var s = Shader.Find(shader);
            if (s == null)
                throw new System.Exception($"Shader {shader} not found (not compiled?).");
            m = new Material(s);
            ayarla(m);
            AssetDatabase.CreateAsset(m, yol);
            return m;
        }

        public static Material Huzme(string ad, Color renk, float yogunluk, float kenar = 1.6f) => Ozel(ad, "NU36/Huzme", m =>
        {
            m.SetColor("_Renk", renk);
            m.SetFloat("_Yogunluk", yogunluk);
            m.SetFloat("_Kenar", kenar);
        });

        public static Material Parcacik(string ad, Texture2D doku, Color renk, bool eklemeli) => Ozel(ad, "NU36/Parcacik", m =>
        {
            m.SetTexture("_MainTex", doku);
            m.SetColor("_Renk", renk);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(eklemeli ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_Eklemeli", eklemeli ? 1f : 0f);
            m.renderQueue = (int)RenderQueue.Transparent;
        });

        public static Material AraziMalzemesi() => Ozel("Arazi", "Universal Render Pipeline/Lit", m =>
        {
            m.SetTexture("_BaseMap", RenkHaritasi());
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.08f);
            m.SetTexture("_DetailAlbedoMap", AyrintiDokusu());
            m.SetTextureScale("_DetailAlbedoMap", new Vector2(900f, 900f));   // ≈ 9 m per tile
            m.SetFloat("_DetailAlbedoMapScale", 1f);
            m.EnableKeyword("_DETAIL_MULX2");
        });

        public static Material SuMalzemesi() => Lit("Su", new Color(0.015f, 0.03f, 0.04f), 0.93f);

        public static Material GokyuzuMalzemesi(Vector3 ayYonu, Color ufuk) => Ozel("GeceGokyuzu", "NU36/GeceGokyuzu", m =>
        {
            m.SetVector("_AyYonu", ayYonu);
            m.SetColor("_Ufuk", ufuk);
        });

        // ================================================================== meshes

        /// <summary>
        /// Open cone along +z, apex at the origin, length 1, half-angle <paramref name="aci"/>: the visible beam. Vertex alpha 1 at
        /// the apex falling to 0 at the end; normals point away from the axis (the beam shader uses them for its soft edge).
        /// </summary>
        public static Mesh HuzmeMeshi(float aci, int kenar = 24, int dilim = 8)
        {
            string yol = $"{Meshler}/Huzme_{aci:0.#}.asset".Replace(',', '_');
            var mevcut = AssetDatabase.LoadAssetAtPath<Mesh>(yol);
            if (mevcut != null) return mevcut;
            Klasorler();
            float r = Mathf.Tan(aci * Mathf.Deg2Rad);
            var v = new Vector3[(kenar + 1) * (dilim + 1)];
            var n = new Vector3[v.Length];
            var c = new Color[v.Length];
            for (int j = 0; j <= dilim; j++)
            {
                float z = (float)j / dilim;
                z = z * z;   // more rings near the lamp where the beam is brightest
                for (int i = 0; i <= kenar; i++)
                {
                    float a = i * Mathf.PI * 2f / kenar;
                    var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    int k = j * (kenar + 1) + i;
                    v[k] = d * (r * z) + Vector3.forward * z;
                    n[k] = d;
                    c[k] = new Color(1f, 1f, 1f, Mathf.Pow(1f - z, 1.6f));
                }
            }
            var t = new int[kenar * dilim * 6];
            int q = 0;
            for (int j = 0; j < dilim; j++)
                for (int i = 0; i < kenar; i++)
                {
                    int a = j * (kenar + 1) + i, b = a + 1, cc = a + kenar + 1, d = cc + 1;
                    t[q++] = a; t[q++] = cc; t[q++] = b;
                    t[q++] = b; t[q++] = cc; t[q++] = d;
                }
            var mesh = new Mesh { name = $"Huzme {aci}°", vertices = v, normals = n, colors = c, triangles = t };
            mesh.bounds = new Bounds(new Vector3(0f, 0f, 0.5f), new Vector3(2f * r, 2f * r, 1f));
            AssetDatabase.CreateAsset(mesh, yol);
            return mesh;
        }

        // ================================================================== effect prefabs

        sealed class Efekt
        {
            public Material duman, toz, ates, kivilcim;
            public Efekt()
            {
                duman = Parcacik("Duman", DumanDokusu(), new Color(0.42f, 0.43f, 0.47f, 1f), false);
                toz = Parcacik("Toz", DumanDokusu(), new Color(0.22f, 0.18f, 0.13f, 1f), false);
                ates = Parcacik("Ates", AtesDokusu(), new Color(3.2f, 1.6f, 0.55f, 1f), true);
                kivilcim = Parcacik("Kivilcim", KivilcimDokusu(), new Color(4f, 2.2f, 0.8f, 1f), true);
            }
        }

        static ParticleSystem Sistem(string ad, Transform ust, Material mat, int enCok)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(ust, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var ana = ps.main;
            ana.simulationSpace = ParticleSystemSimulationSpace.World;
            ana.maxParticles = enCok;
            ana.playOnAwake = true;
            ana.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = 0f;
            return ps;
        }

        static ParticleSystem.MinMaxCurve Ara(float a, float b) => new ParticleSystem.MinMaxCurve(a, b);

        static Gradient Renkler(params (float t, Color c)[] noktalar)
        {
            var g = new Gradient();
            var cs = new GradientColorKey[noktalar.Length];
            var al = new GradientAlphaKey[noktalar.Length];
            for (int i = 0; i < noktalar.Length; i++)
            {
                cs[i] = new GradientColorKey(noktalar[i].c, noktalar[i].t);
                al[i] = new GradientAlphaKey(noktalar[i].c.a, noktalar[i].t);
            }
            g.SetKeys(cs, al);
            return g;
        }

        static void Boyut(ParticleSystem ps, float bas, float son)
        {
            var b = ps.sizeOverLifetime;
            b.enabled = true;
            b.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, bas, 1f, son));
        }

        static void Renk(ParticleSystem ps, Gradient g)
        {
            var c = ps.colorOverLifetime;
            c.enabled = true;
            c.color = g;
        }

        static void Gurultu(ParticleSystem ps, float guc, float frekans)
        {
            var n = ps.noise;
            n.enabled = true;
            n.strength = guc;
            n.frequency = frekans;
            n.scrollSpeed = 0.2f;
            n.damping = true;
        }

        static void Ruzgar(ParticleSystem ps, Vector3 hiz)
        {
            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = hiz.x; v.y = hiz.y; v.z = hiz.z;
        }

        static void Patla(ParticleSystem ps, int adet, float zaman = 0f)
        {
            var e = ps.emission;
            e.enabled = true;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(zaman, (short)adet) });
        }

        /// <summary>Duman column + flames + embers + flickering light: a burning wreck that keeps burning.</summary>
        public static GameObject YanginPrefab()
        {
            string yol = Prefablar + "/Yangin.prefab";
            var mevcut = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
            if (mevcut != null) return mevcut;
            var e = new Efekt();
            var kok = new GameObject("Yangin");

            var isikGo = new GameObject("Isik");
            isikGo.transform.SetParent(kok.transform, false);
            isikGo.transform.localPosition = new Vector3(0f, 4f, 0f);
            var isik = isikGo.AddComponent<Light>();
            isik.type = LightType.Point;
            isik.color = new Color(1f, 0.52f, 0.2f);
            isik.range = 70f;
            isik.intensity = 30f;
            isik.shadows = LightShadows.None;
            var ei = kok.AddComponent<EfektIsigi>();
            var so = new SerializedObject(ei);
            so.FindProperty("isik").objectReferenceValue = isik;
            so.FindProperty("temelParlaklik").floatValue = 30f;
            so.FindProperty("titreme").floatValue = 0.45f;
            so.ApplyModifiedPropertiesWithoutUndo();

            ParticleSystem alev = Sistem("Alev", kok.transform, e.ates, 160);
            var a = alev.main;
            a.loop = true; a.duration = 2f;
            a.startLifetime = Ara(0.5f, 1.1f); a.startSpeed = Ara(2f, 5f); a.startSize = Ara(3f, 6.5f);
            a.startRotation = Ara(0f, 6.28f);
            var ae = alev.emission; ae.rateOverTime = 34f;
            var ash = alev.shape; ash.shapeType = ParticleSystemShapeType.Cone; ash.angle = 12f; ash.radius = 2.5f;
            ash.rotation = new Vector3(-90f, 0f, 0f);
            Renk(alev, Renkler((0f, new Color(1f, 0.85f, 0.5f, 0f)), (0.15f, new Color(1f, 0.6f, 0.2f, 1f)), (0.6f, new Color(0.9f, 0.25f, 0.05f, 0.7f)), (1f, new Color(0.3f, 0.05f, 0f, 0f))));
            Boyut(alev, 1f, 0.35f);
            Gurultu(alev, 0.8f, 0.6f);

            ParticleSystem duman = Sistem("Duman", kok.transform, e.duman, 140);
            var d = duman.main;
            d.loop = true; d.duration = 5f;
            d.startLifetime = Ara(10f, 15f); d.startSpeed = Ara(4f, 7f); d.startSize = Ara(6f, 10f);
            d.startRotation = Ara(0f, 6.28f);
            var de = duman.emission; de.rateOverTime = 6f;
            var dsh = duman.shape; dsh.shapeType = ParticleSystemShapeType.Cone; dsh.angle = 8f; dsh.radius = 2.5f;
            dsh.rotation = new Vector3(-90f, 0f, 0f);
            duman.transform.localPosition = new Vector3(0f, 3f, 0f);
            Renk(duman, Renkler((0f, new Color(1f, 0.62f, 0.36f, 0f)), (0.07f, new Color(0.9f, 0.72f, 0.6f, 0.6f)), (0.3f, new Color(0.75f, 0.75f, 0.78f, 0.5f)), (1f, new Color(0.7f, 0.7f, 0.74f, 0f))));
            Boyut(duman, 1f, 4.5f);
            Gurultu(duman, 1.2f, 0.15f);
            Ruzgar(duman, new Vector3(1.6f, 0.6f, 0.8f));
            var drot = duman.rotationOverLifetime; drot.enabled = true; drot.z = Ara(-0.3f, 0.3f);

            ParticleSystem kor = Sistem("Korlar", kok.transform, e.kivilcim, 60);
            var k = kor.main;
            k.loop = true; k.duration = 2f;
            k.startLifetime = Ara(1.5f, 3.5f); k.startSpeed = Ara(3f, 8f); k.startSize = Ara(0.25f, 0.5f);
            k.gravityModifier = -0.15f;
            var ke = kor.emission; ke.rateOverTime = 9f;
            var ksh = kor.shape; ksh.shapeType = ParticleSystemShapeType.Cone; ksh.angle = 25f; ksh.radius = 2f;
            ksh.rotation = new Vector3(-90f, 0f, 0f);
            Renk(kor, Renkler((0f, new Color(1f, 0.7f, 0.3f, 1f)), (1f, new Color(1f, 0.3f, 0.05f, 0f))));
            Gurultu(kor, 2f, 0.8f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(kok, yol);
            Object.DestroyImmediate(kok);
            return prefab;
        }

        /// <summary>
        /// Bomb impact: a flash of light, a fireball, sparks and thrown earth, then a smoke plume that rises and drifts for
        /// ~20 s; the whole object removes itself after 40 s. Spawned by <see cref="Patlama"/> (its kaliciEfekt).
        /// </summary>
        public static GameObject PatlamaEfektiPrefab()
        {
            string yol = Prefablar + "/PatlamaEfekti.prefab";
            var mevcut = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
            if (mevcut != null) return mevcut;
            var e = new Efekt();
            var kok = new GameObject("PatlamaEfekti");

            var isikGo = new GameObject("Parlama");
            isikGo.transform.SetParent(kok.transform, false);
            isikGo.transform.localPosition = new Vector3(0f, 6f, 0f);
            var isik = isikGo.AddComponent<Light>();
            isik.type = LightType.Point;
            isik.color = new Color(1f, 0.6f, 0.28f);
            isik.range = 180f;
            isik.intensity = 400f;
            isik.shadows = LightShadows.None;
            var ei = kok.AddComponent<EfektIsigi>();
            var so = new SerializedObject(ei);
            so.FindProperty("isik").objectReferenceValue = isik;
            so.FindProperty("temelParlaklik").floatValue = 400f;
            so.FindProperty("sonmeSuresi").floatValue = 0.9f;
            so.FindProperty("titreme").floatValue = 0.2f;
            so.FindProperty("omur").floatValue = 40f;
            so.ApplyModifiedPropertiesWithoutUndo();

            ParticleSystem top = Sistem("AtesTopu", kok.transform, e.ates, 40);
            var t = top.main;
            t.loop = false; t.duration = 1f;
            t.startLifetime = Ara(0.45f, 1f); t.startSpeed = Ara(6f, 22f); t.startSize = Ara(8f, 16f);
            t.startRotation = Ara(0f, 6.28f);
            Patla(top, 28);
            var tsh = top.shape; tsh.shapeType = ParticleSystemShapeType.Hemisphere; tsh.radius = 3f;
            tsh.rotation = new Vector3(-90f, 0f, 0f);
            Renk(top, Renkler((0f, new Color(1f, 0.95f, 0.75f, 1f)), (0.25f, new Color(1f, 0.55f, 0.15f, 0.9f)), (1f, new Color(0.25f, 0.05f, 0.02f, 0f))));
            Boyut(top, 0.6f, 1.6f);
            var tlim = top.limitVelocityOverLifetime; tlim.enabled = true; tlim.limit = 4f; tlim.dampen = 0.25f;

            ParticleSystem kiv = Sistem("Kivilcimlar", kok.transform, e.kivilcim, 90);
            var kv = kiv.main;
            kv.loop = false; kv.duration = 1f;
            kv.startLifetime = Ara(0.7f, 1.8f); kv.startSpeed = Ara(25f, 60f); kv.startSize = Ara(0.35f, 0.8f);
            kv.gravityModifier = 1f;
            Patla(kiv, 70);
            var ksh = kiv.shape; ksh.shapeType = ParticleSystemShapeType.Hemisphere; ksh.radius = 1.5f;
            ksh.rotation = new Vector3(-90f, 0f, 0f);
            Renk(kiv, Renkler((0f, new Color(1f, 0.85f, 0.5f, 1f)), (1f, new Color(1f, 0.3f, 0.05f, 0f))));
            var kr = kiv.GetComponent<ParticleSystemRenderer>();
            kr.renderMode = ParticleSystemRenderMode.Stretch;
            kr.velocityScale = 0.04f;
            kr.lengthScale = 1.5f;

            ParticleSystem toprak = Sistem("Toprak", kok.transform, e.toz, 50);
            var tp = toprak.main;
            tp.loop = false; tp.duration = 1f;
            tp.startLifetime = Ara(1.2f, 2.6f); tp.startSpeed = Ara(14f, 32f); tp.startSize = Ara(1.2f, 3f);
            tp.gravityModifier = 1.3f;
            tp.startRotation = Ara(0f, 6.28f);
            Patla(toprak, 40);
            var tsh2 = toprak.shape; tsh2.shapeType = ParticleSystemShapeType.Cone; tsh2.angle = 35f; tsh2.radius = 2f;
            tsh2.rotation = new Vector3(-90f, 0f, 0f);
            Renk(toprak, Renkler((0f, new Color(1f, 1f, 1f, 0.95f)), (1f, new Color(1f, 1f, 1f, 0f))));

            ParticleSystem duman = Sistem("Duman", kok.transform, e.duman, 70);
            var d = duman.main;
            d.loop = false; d.duration = 4f;
            d.startLifetime = Ara(12f, 20f); d.startSpeed = Ara(2f, 8f); d.startSize = Ara(9f, 16f);
            d.startRotation = Ara(0f, 6.28f);
            var de = duman.emission;
            de.enabled = true;
            de.rateOverTime = new ParticleSystem.MinMaxCurve(10f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            de.SetBursts(new[] { new ParticleSystem.Burst(0.05f, (short)16) });
            var dsh = duman.shape; dsh.shapeType = ParticleSystemShapeType.Hemisphere; dsh.radius = 6f;
            dsh.rotation = new Vector3(-90f, 0f, 0f);
            Renk(duman, Renkler((0f, new Color(1f, 0.6f, 0.35f, 0f)), (0.04f, new Color(0.85f, 0.65f, 0.5f, 0.7f)), (0.35f, new Color(0.62f, 0.62f, 0.65f, 0.55f)), (1f, new Color(0.55f, 0.55f, 0.6f, 0f))));
            Boyut(duman, 0.7f, 3.5f);
            Gurultu(duman, 1.4f, 0.12f);
            Ruzgar(duman, new Vector3(1.6f, 2.2f, 0.8f));
            var dlim = duman.limitVelocityOverLifetime; dlim.enabled = true; dlim.limit = 3f; dlim.dampen = 0.08f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(kok, yol);
            Object.DestroyImmediate(kok);
            return prefab;
        }

        static GameObject Kaydet(GameObject g, string yol)
        {
            Klasorler();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(g, yol);
            Object.DestroyImmediate(g);
            return prefab;
        }

        /// <summary>The explosion "visual" for <see cref="Patlama.Yarat"/>: no sphere any more, only the particle effect.</summary>
        public static Patlama PatlamaPrefab()
        {
            string yol = Prefablar + "/Patlama.prefab";
            var mevcut = AssetDatabase.LoadAssetAtPath<Patlama>(yol);
            if (mevcut != null) return mevcut;
            var g = new GameObject("Patlama");
            var p = g.AddComponent<Patlama>();
            var so = new SerializedObject(p);
            so.FindProperty("sure").floatValue = 0.1f;
            so.FindProperty("kaliciEfekt").objectReferenceValue = PatlamaEfektiPrefab();
            so.ApplyModifiedPropertiesWithoutUndo();
            return Kaydet(g, yol).GetComponent<Patlama>();
        }

        /// <summary>The Lab 7 bomb: the Lab 5 bomb with the new explosion (same falling physics, blast and fragment rule).</summary>
        public static Bomba BombaPrefab(int carpacak, int etkilenen, Material govdeMat)
        {
            string yol = Prefablar + "/Bomba.prefab";
            var mevcut = AssetDatabase.LoadAssetAtPath<Bomba>(yol);
            if (mevcut != null) return mevcut;
            var g = new GameObject("Bomba");
            GameObject govde = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(govde.GetComponent<Collider>());
            govde.name = "Govde";
            govde.transform.SetParent(g.transform, false);
            govde.transform.localScale = new Vector3(0.4f, 0.75f, 0.4f);
            govde.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            govde.GetComponent<MeshRenderer>().sharedMaterial = govdeMat;
            var b = g.AddComponent<Bomba>();
            var so = new SerializedObject(b);
            so.FindProperty("carpacak").intValue = carpacak;
            so.FindProperty("etkilenen").intValue = etkilenen;
            so.FindProperty("patlamaOn").objectReferenceValue = PatlamaPrefab();
            so.ApplyModifiedPropertiesWithoutUndo();
            g.layer = LayerMask.NameToLayer("Mermi");
            govde.layer = g.layer;
            return Kaydet(g, yol).GetComponent<Bomba>();
        }

        public const string IzMermisiYolu = Prefablar + "/IzMermisi.prefab";

        /// <summary>
        /// Tracer round for the night guns: the Lab 5 round (same sweep physics, speed, damage; values copied from
        /// <paramref name="lab5Mermi"/>) with a glowing HDR core and a short additive trail, so every burst draws lines of fire
        /// in the dark. The Lab 5 prefab itself is not changed.
        /// </summary>
        public static Mermi IzMermisiPrefab(Mermi lab5Mermi)
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<Mermi>(IzMermisiYolu);
            if (mevcut != null) return mevcut;
            var g = new GameObject("IzMermisi");
            var m = g.AddComponent<Mermi>();
            if (lab5Mermi != null)
                EditorUtility.CopySerialized(lab5Mermi, m);
            GameObject cekirdek = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(cekirdek.GetComponent<Collider>());
            cekirdek.name = "Cekirdek";
            cekirdek.transform.SetParent(g.transform, false);
            cekirdek.transform.localScale = new Vector3(0.35f, 1.6f, 0.35f);
            cekirdek.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var cr = cekirdek.GetComponent<MeshRenderer>();
            cr.sharedMaterial = Ampul("IzMermisi", new Color(1f, 0.55f, 0.18f), 14f);
            cr.shadowCastingMode = ShadowCastingMode.Off;
            cr.receiveShadows = false;
            var iz = g.AddComponent<TrailRenderer>();
            iz.time = 0.16f;
            iz.minVertexDistance = 2f;
            iz.startWidth = 0.8f;
            iz.endWidth = 0f;
            iz.numCapVertices = 0;
            iz.alignment = LineAlignment.View;
            iz.shadowCastingMode = ShadowCastingMode.Off;
            iz.receiveShadows = false;
            iz.sharedMaterial = Parcacik("IzMermisiIzi", null, new Color(3.2f, 1.5f, 0.45f, 1f), true);
            var renk = new Gradient();
            renk.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.08f), 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            iz.colorGradient = renk;
            int k = LayerMask.NameToLayer("Mermi");
            g.layer = cekirdek.layer = k;
            return Kaydet(g, IzMermisiYolu).GetComponent<Mermi>();
        }

        /// <summary>Soft, moon-lit cloud puffs (alpha-blended, fogged) for the clouds of the Bölüm 3 single rule.</summary>
        public static Material BulutMalzemesi() => Parcacik("Bulut", DumanDokusu(), new Color(0.30f, 0.33f, 0.40f, 0.55f), false);
    }
}
