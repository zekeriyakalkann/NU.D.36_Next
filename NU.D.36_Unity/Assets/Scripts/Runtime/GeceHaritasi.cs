using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 7 (Hafta 7): the night map of Bölüm 3 — the Hafta 6 paper map (<see cref="Bolum3Harita"/>, v0.1, 12 × 9 squares,
    /// 1 square = 200 m) grown into a night level. The paper map is centred on the world origin (<see cref="HaritaMerkezi"/>);
    /// every element stands on its paper square:
    ///   • runway on row 8 (paper B8–D8, heading 090°/270°), lengthened 600 m to the west for the aerodynamic model;
    ///   • river I1–I4 → H5–J5 (a wide reach under the bridge) → J6–J9, continued beyond the paper map;
    ///   • the bridge H5–J5, 600 m long, deck 25 m over the water (the narrow target);
    ///   • searchlights F4 (135°), K5 (270°), H7 (0°), beam 1200 m; AA guns G4, K4, I7 (Ø 1600 m); sound locator H3 (Ø 2000 m);
    ///   • hills E4, F7, G7, 120 m above the ground; clouds (the single rule) over E5–G5 at Orta (200 m).
    /// Height, colour, forest and roads are pure, deterministic functions of world (x, z), shared by the scene builder, the
    /// runtime terrain/forest generators and the tests. x = east, z = north, metres.
    /// </summary>
    public static class GeceHaritasi
    {
        public const float Boyut = 8000f;               // terrain square, centred on the origin
        public const float YarimBoyut = Boyut / 2f;
        public const float SuSeviyesi = 0f;
        public const float VadiTabani = 6f;
        public const float KanalTabani = -7f;
        public const float KanalYariGenislik = 44f;      // water edge ≈ 30–40 m from the centreline

        /// <summary>Centre of the paper map (12 × 9 squares) in the night world.</summary>
        public static readonly Vector3 HaritaMerkezi = Vector3.zero;

        /// <summary>Centre (y = 0) of a paper square, e.g. "H5".</summary>
        public static Vector3 Kare(string kare) => Bolum3Harita.KareMerkezi(kare, HaritaMerkezi);

        /// <summary>Paper square under world (x, z); null outside the paper map.</summary>
        public static string KareAdi(float x, float z) => Bolum3Harita.KareAdi(x, z, HaritaMerkezi);

        /// <summary>Paper map rectangle (x, z) in the world.</summary>
        public static Rect KagitAlani => new Rect(HaritaMerkezi.x - Bolum3Harita.Sutun * Bolum3Harita.Kare / 2f,
            HaritaMerkezi.z - Bolum3Harita.Satir * Bolum3Harita.Kare / 2f, Bolum3Harita.Sutun * Bolum3Harita.Kare, Bolum3Harita.Satir * Bolum3Harita.Kare);

        // ---- airfield (paper B8–D8) -------------------------------------------------------------------------------------
        public const float PistYonu = 90f;               // take-off towards the east (090°), landing towards the west (270°)
        public const float PistBoyu = 1400f, PistEni = 40f, PistYuksekligi = 22f;
        public const float PistDuzlukPayi = 170f;        // flat margin around the runway
        public const float InisBolgesiBoyu = 750f;       // marked landing zone, from the 27 threshold (east end)
        public const float InisBolgesiEni = 70f;

        /// <summary>East end of the runway = east edge of paper square D8.</summary>
        public static float PistDoguX => Kare("D8").x + Bolum3Harita.Kare / 2f;
        public static Vector2 PistMerkezi => new Vector2(PistDoguX - PistBoyu / 2f, Kare("C8").z);

        /// <summary>Unit vector of the take-off direction (090°).</summary>
        public static Vector3 PistEkseni => Yon(PistYonu);
        /// <summary>West end (threshold 09, take-off start).</summary>
        public static Vector3 PistBasi => Nokta(PistMerkezi - Duz(PistEkseni) * (PistBoyu / 2f), PistYuksekligi);
        /// <summary>East end (threshold 27, landing threshold for the return from the bridge).</summary>
        public static Vector3 PistSonu => Nokta(PistMerkezi + Duz(PistEkseni) * (PistBoyu / 2f), PistYuksekligi);
        /// <summary>Centre of the landing zone (first <see cref="InisBolgesiBoyu"/> m after the 27 threshold; covers paper B8–D8).</summary>
        public static Vector3 InisBolgesiMerkezi => PistSonu - PistEkseni * (InisBolgesiBoyu / 2f);

        // ---- river and bridge (paper I1–I4, H5–J5, J6–J9) ---------------------------------------------------------------
        /// <summary>Wide reach of the river under the bridge (paper H5–J5): an ellipse with these half-axes (m).</summary>
        public const float GolYariX = 250f, GolYariZ = 95f;
        public static Vector2 GolMerkezi => new Vector2(Kare("I5").x, Kare("I5").z);

        /// <summary>River centreline from the north edge of the terrain to the south edge (x, z): column I, the reach, column J.</summary>
        public static readonly Vector2[] NehirHatti = NehirHattiKur();

        static Vector2[] NehirHattiKur()
        {
            Vector3 i1 = Kare("I1"), j9 = Kare("J9");
            return new[]
            {
                new Vector2(i1.x - 60f, 4300f), new Vector2(i1.x + 90f, 3000f), new Vector2(i1.x - 50f, 1900f),
                new Vector2(i1.x, i1.z + 100f), new Vector2(i1.x, 40f),                       // column I into the reach
                new Vector2(j9.x, -40f), new Vector2(j9.x, j9.z - 100f),                       // out of the reach, column J
                new Vector2(j9.x - 70f, -1800f), new Vector2(j9.x + 80f, -2900f), new Vector2(j9.x - 40f, -4300f),
            };
        }

        public const float KopruBoyu = 600f, KopruEni = 9f, KopruGuvertesi = 25f;   // paper: 3 squares, 25 m over the water
        public const int KopruAcikligi = 12;             // twelve 50 m truss spans
        public const float KuleYuksekligi = 28f;
        /// <summary>Paper beam length (6 squares).</summary>
        public const float IsildakMenzili = Bolum3Harita.IsinBoyu;
        /// <summary>Clouds of the single rule: from this height to <see cref="BulutTavani"/> (paper Orta = 200 m).</summary>
        public const float BulutTabani = 140f, BulutTavani = 270f;

        public static Vector3 KopruMerkezi => new Vector3(Kare("I5").x, KopruGuvertesi, Kare("I5").z);
        /// <summary>Bridge axis (west → east, across the reach).</summary>
        public static Vector3 KopruEkseni => Vector3.right;

        public static int IsildakSayisi => Bolum3Harita.Isildaklar.Length;
        public static string IsildakKaresi(int i) => Bolum3Harita.Isildaklar[i].kare;
        /// <summary>Paper starting direction of searchlight i (compass degrees).</summary>
        public static float IsildakYonu(int i) => Bolum3Harita.Isildaklar[i].yon;
        public static Vector3 KuleKonumu(int i) => KaredeZemin(IsildakKaresi(i));
        public static Vector3 UcaksavarKonumu(int i) => KaredeZemin(Bolum3Harita.Ucaksavarlar[i]);
        public static Vector3 SesDinleyiciKonumu => KaredeZemin(Bolum3Harita.SesDinleyici);
        /// <summary>Paper ranges as radii (m).</summary>
        public const float UcaksavarYaricapi = Bolum3Harita.UcaksavarCapi / 2f, SesYaricapi = Bolum3Harita.SesCapi / 2f;

        static Vector3 KaredeZemin(string kare)
        {
            Vector3 k = Kare(kare);
            return Zeminde(k.x, k.z);
        }

        /// <summary>World box of the paper cloud squares at the Orta band.</summary>
        public static Bounds BulutKutusu(string kare)
        {
            Vector3 k = Kare(kare);
            float y = (BulutTabani + BulutTavani) / 2f;
            return new Bounds(new Vector3(k.x, y, k.z), new Vector3(Bolum3Harita.Kare, BulutTavani - BulutTabani, Bolum3Harita.Kare));
        }

        /// <summary>Horizontal distance (m) from (x, z) to the river: to the centreline, or a matching measure for the wide reach.</summary>
        public static float NehirUzakligi(float x, float z)
        {
            var p = new Vector2(x, z);
            float d = float.MaxValue;
            for (int i = 0; i + 1 < NehirHatti.Length; i++)
                d = Mathf.Min(d, ParcaUzakligi(p, NehirHatti[i], NehirHatti[i + 1]));
            Vector2 g = GolMerkezi;
            float e = Mathf.Sqrt(Kare2((x - g.x) / GolYariX) + Kare2((z - g.y) / GolYariZ));
            return Mathf.Min(d, (e - 1f) * GolYariZ + 26f);   // e = 1 at the reach's edge = the start of the bank (26 m)
        }

        static float Kare2(float v) => v * v;

        // ---- hills, villages, roads ------------------------------------------------------------------------------------
        public const float TepeYuksekligi = Bolum3Harita.TepeYuksekligi;

        /// <summary>Height a paper hill adds at (x, z): 120 m on its square, a rounded square foot falling over ~60 m.</summary>
        public static float TepeKatkisi(float x, float z)
        {
            float enCok = 0f;
            foreach (string kare in Bolum3Harita.Tepeler)
            {
                Vector3 k = Kare(kare);
                float qx = (x - k.x) / 105f, qz = (z - k.z) / 105f;
                float q = Mathf.Pow(qx * qx * qx * qx + qz * qz * qz * qz, 0.25f);
                q *= 1f + 0.08f * AraziUretici.Fbm(x / 90f, z / 90f, 2, 431u);
                enCok = Mathf.Max(enCok, TepeYuksekligi * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.5f, q))));
            }
            return enCok;
        }

        /// <summary>Village centres (x, z) and house counts: west end of the bridge (F6–G6), east bank (L6), north-west (C3).</summary>
        public static readonly (Vector2 merkez, int ev, int tohum)[] Koyler =
        {
            (new Vector2(Kare("G6").x - 110f, Kare("G6").z + 30f), 9, 11),
            (new Vector2(Kare("L6").x + 60f, Kare("L6").z + 40f), 7, 23),
            (new Vector2(Kare("C3").x - 50f, Kare("C3").z + 50f), 6, 37),
        };

        /// <summary>Road polylines (x, z): airfield → bridge, the road over the bridge to the east, the west- and east-bank roads.</summary>
        public static List<Vector2[]> Yollar()
        {
            Vector2 bati = new Vector2(KopruMerkezi.x - KopruBoyu / 2f - 12f, KopruMerkezi.z);
            Vector2 dogu = new Vector2(KopruMerkezi.x + KopruBoyu / 2f + 12f, KopruMerkezi.z);
            Vector2 pist = new Vector2(PistSonu.x - 60f, PistSonu.z + 70f);
            Vector2 koyB = Koyler[0].merkez, koyD = Koyler[1].merkez, koyK = Koyler[2].merkez;
            return new List<Vector2[]>
            {
                new[] { pist, new Vector2(-260f, -330f), new Vector2(-140f, -140f), koyB, bati + new Vector2(-90f, -6f), bati },
                new[] { dogu, dogu + new Vector2(150f, 8f), new Vector2(1400f, 120f), new Vector2(2600f, 340f), new Vector2(3900f, 520f) },
                new[] { new Vector2(260f, -3900f), new Vector2(330f, -1500f), new Vector2(240f, -700f), koyB,
                        new Vector2(80f, 420f), new Vector2(-180f, 1300f), new Vector2(-320f, 3900f) },
                new[] { new Vector2(980f, 3900f), new Vector2(930f, 1200f), new Vector2(1000f, 260f), dogu + new Vector2(150f, 8f),
                        koyD, new Vector2(1080f, -1500f), new Vector2(1150f, -3900f) },
                new[] { koyK, new Vector2(-880f, 30f), new Vector2(PistMerkezi.x + 60f, PistMerkezi.y + 150f) },
            };
        }

        static List<Vector2[]> yollarOnbellek;

        /// <summary>Distance (m) from (x, z) to the nearest road centreline.</summary>
        public static float YolUzakligi(float x, float z)
        {
            yollarOnbellek ??= Yollar();
            var p = new Vector2(x, z);
            float enAz = float.MaxValue;
            foreach (Vector2[] yol in yollarOnbellek)
                for (int i = 0; i + 1 < yol.Length; i++)
                    enAz = Mathf.Min(enAz, ParcaUzakligi(p, yol[i], yol[i + 1]));
            return enAz;
        }

        static float ParcaUzakligi(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
            return Vector2.Distance(p, a + ab * t);
        }

        // ---- height --------------------------------------------------------------------------------------------------

        /// <summary>0..1 weight of the airfield flattening (1 on the runway and its margin, falling to 0 over 180 m).</summary>
        public static float PistMaskesi(float x, float z)
        {
            Vector3 e = PistEkseni;
            Vector2 m = PistMerkezi;
            float dx = x - m.x, dz = z - m.y;
            float boy = Mathf.Abs(dx * e.x + dz * e.z) - (PistBoyu / 2f + PistDuzlukPayi);
            float en = Mathf.Abs(-dx * e.z + dz * e.x) - (PistEni / 2f + PistDuzlukPayi + 60f);
            float disari = Mathf.Max(boy, en);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 180f, disari));
        }

        /// <summary>1 inside the paper map (plus 300 m), 0 from 1200 m outside it: the play area is low and calm so the paper hills read.</summary>
        public static float CekirdekAgirligi(float x, float z)
        {
            Rect r = KagitAlani;
            float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax), dz = Mathf.Max(r.yMin - z, 0f, z - r.yMax);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(300f, 1200f, Mathf.Sqrt(dx * dx + dz * dz)));
        }

        /// <summary>Smooth terrain height at (x, z), metres. The rendered mesh samples it every 16 m.</summary>
        public static float Yukseklik(float x, float z)
        {
            // Farmland and low hills; calmer inside the paper map.
            float c = CekirdekAgirligi(x, z);
            float h = 30f + Mathf.Lerp(48f, 12f, c) * AraziUretici.Fbm(x / 1500f + 11.3f, z / 1500f - 4.2f, 4, 401u)
                          + Mathf.Lerp(16f, 5f, c) * AraziUretici.Fbm(x / 420f - 2.7f, z / 420f + 8.1f, 3, 409u);

            // The paper hills (E4, F7, G7): 120 m above the ground around them.
            h += TepeKatkisi(x, z);

            // Mountains closing the map (beyond the fog).
            float r = Mathf.Sqrt(x * x + z * z);
            float dag = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2800f, 3900f, r));
            if (dag > 0f)
            {
                float tepe = 1f - Mathf.Min(1f, Mathf.Abs(AraziUretici.Fbm(x / 900f, z / 900f, 3, 457u)) * 2.2f);
                h += dag * (260f + 420f * tepe * tepe);
            }

            // River valley, then the channel / the reach.
            float d = NehirUzakligi(x, z);
            h = Mathf.Lerp(VadiTabani + 3f * AraziUretici.Fbm(x / 200f, z / 200f, 2, 463u), h, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(50f, 330f, d)));
            h = Mathf.Lerp(KanalTabani, h, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(26f, KanalYariGenislik, d)));

            // Approach corridors beyond both runway ends (a 3° approach and the climb-out stay clear).
            h = KoridorSiniri(x, z, h);

            // Airfield.
            float pist = PistMaskesi(x, z);
            if (pist > 0f)
                h = Mathf.Lerp(h, PistYuksekligi, pist);
            return h;
        }

        public const float KoridorEgimi = 2.2f;   // degrees

        /// <summary>Caps the height h at (x, z) under the approach/departure plane beyond the runway ends (narrow, so the F7–G7 ridge beside the approach stays).</summary>
        public static float KoridorSiniri(float x, float z, float h)
        {
            Vector3 e = PistEkseni;
            Vector2 m = PistMerkezi;
            float dx = x - m.x, dz = z - m.y;
            float boy = Mathf.Abs(dx * e.x + dz * e.z) - PistBoyu / 2f;
            if (boy <= 0f)
                return h;
            float yan = Mathf.Abs(-dx * e.z + dz * e.x);
            float tavan = PistYuksekligi - 6f + boy * Mathf.Tan(KoridorEgimi * Mathf.Deg2Rad) + yan * 0.12f;
            if (h <= tavan)
                return h;
            float agirlik = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(60f, 140f, yan)))
                          * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3600f, 4600f, boy)));
            return Mathf.Lerp(h, tavan, agirlik);
        }

        public static Vector3 Zeminde(float x, float z) => new Vector3(x, Yukseklik(x, z), z);

        /// <summary>Terrain normal from central differences of the height function.</summary>
        public static Vector3 Normal(float x, float z, float e = 4f)
        {
            float hx = Yukseklik(x + e, z) - Yukseklik(x - e, z);
            float hz = Yukseklik(x, z + e) - Yukseklik(x, z - e);
            return new Vector3(-hx, 2f * e, -hz).normalized;
        }

        // ---- masks for colour and forest -------------------------------------------------------------------------------

        public static float KoyUzakligi(float x, float z)
        {
            float enAz = float.MaxValue;
            foreach (var k in Koyler)
                enAz = Mathf.Min(enAz, Vector2.Distance(new Vector2(x, z), k.merkez));
            return enAz;
        }

        /// <summary>Distance (m) from (x, z) to the nearest searchlight, gun or the locator (kept clear of trees).</summary>
        public static float MevziUzakligi(float x, float z)
        {
            float enAz = float.MaxValue;
            var p = new Vector2(x, z);
            foreach (var (kare, _) in Bolum3Harita.Isildaklar) enAz = Mathf.Min(enAz, Vector2.Distance(p, Duz(Kare(kare))));
            foreach (string kare in Bolum3Harita.Ucaksavarlar) enAz = Mathf.Min(enAz, Vector2.Distance(p, Duz(Kare(kare))));
            return Mathf.Min(enAz, Vector2.Distance(p, Duz(Kare(Bolum3Harita.SesDinleyici))));
        }

        /// <summary>True where a forest grows: noise patches (and the paper hills) on dry, not too steep ground away from the river, roads, villages, positions and the airfield.</summary>
        public static bool OrmanMi(float x, float z, float h, float egimY)
        {
            if (h < 9f || h > 430f || egimY < 0.72f) return false;
            if (PistMaskesi(x, z) > 0.01f || NehirUzakligi(x, z) < 75f || KoyUzakligi(x, z) < 190f || MevziUzakligi(x, z) < 45f) return false;
            float f = AraziUretici.Fbm(x / 650f + 3.1f, z / 650f - 7.7f, 3, 503u);
            float tepe = TepeKatkisi(x, z) > 25f ? 0.35f : 0f; // the hills are wooded
            return f + tepe > 0.16f && YolUzakligi(x, z) > 14f;
        }

        static readonly Color Cayir = new Color(0.20f, 0.29f, 0.12f);
        static readonly Color AcikCayir = new Color(0.30f, 0.36f, 0.16f);
        static readonly Color OrmanZemini = new Color(0.09f, 0.14f, 0.07f);
        static readonly Color Kaya = new Color(0.34f, 0.33f, 0.31f);
        static readonly Color Kar = new Color(0.82f, 0.85f, 0.90f);
        static readonly Color Kum = new Color(0.40f, 0.36f, 0.27f);
        static readonly Color Camur = new Color(0.16f, 0.14f, 0.11f);
        static readonly Color Yol = new Color(0.43f, 0.38f, 0.29f);
        static readonly Color PistCimi = new Color(0.27f, 0.34f, 0.16f);
        static readonly Color[] Tarlalar =
        {
            new Color(0.42f, 0.38f, 0.20f), new Color(0.30f, 0.33f, 0.14f), new Color(0.36f, 0.28f, 0.18f),
            new Color(0.24f, 0.31f, 0.13f), new Color(0.47f, 0.42f, 0.24f), new Color(0.33f, 0.24f, 0.15f),
        };

        /// <summary>Albedo of the terrain at (x, z) for the baked colour map (sRGB).</summary>
        public static Color Renk(float x, float z)
        {
            float h = Yukseklik(x, z);
            Vector3 n = Normal(x, z);
            float d = NehirUzakligi(x, z);
            float yama = AraziUretici.Fbm(x / 160f + 5.5f, z / 160f - 3.1f, 3, 521u);
            float ince = AraziUretici.Fbm(x / 23f, z / 23f, 2, 523u);

            Color c = Color.Lerp(Cayir, AcikCayir, Mathf.InverseLerp(-0.5f, 0.6f, yama));

            // Fields on the flat valley and lowlands: a rotated grid of strips with their own crops and furrows.
            if (h < 90f && n.y > 0.985f && d > 70f)
            {
                const float aci = 0.42f;
                float u = x * Mathf.Cos(aci) + z * Mathf.Sin(aci), w = -x * Mathf.Sin(aci) + z * Mathf.Cos(aci);
                int iu = Mathf.FloorToInt(u / 210f), iw = Mathf.FloorToInt(w / 95f);
                float tarlaVar = AraziUretici.Fbm(x / 900f - 9f, z / 900f + 2f, 2, 541u);
                if (tarlaVar > -0.05f)
                {
                    Color t = Tarlalar[(int)(AraziUretici.Rastgele01(iu, iw, 547u) * Tarlalar.Length) % Tarlalar.Length];
                    float cizgi = 0.92f + 0.08f * Mathf.Sin(w * 1.6f);      // furrows
                    float sinir = Mathf.Min(Mathf.Repeat(u, 210f), Mathf.Repeat(w, 95f));
                    t *= cizgi;
                    c = sinir < 3f ? Color.Lerp(t, Cayir * 0.8f, 0.7f) : t; // hedgerow between fields
                }
            }

            if (OrmanMi(x, z, h, n.y))
                c = OrmanZemini;
            c = Color.Lerp(c, Kaya, Mathf.InverseLerp(0.80f, 0.62f, n.y));
            c = Color.Lerp(c, Kaya, Mathf.InverseLerp(260f, 420f, h) * 0.8f);
            c = Color.Lerp(c, Kar, Mathf.InverseLerp(560f, 680f, h + ince * 30f) * Mathf.InverseLerp(0.6f, 0.85f, n.y));

            // Riverbanks.
            c = Color.Lerp(c, Kum, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(46f, 66f, d)));
            if (h < SuSeviyesi + 0.5f)
                c = Camur;

            // Airfield grass, roads, village yards.
            float pist = PistMaskesi(x, z);
            c = Color.Lerp(c, PistCimi * (0.93f + 0.07f * Mathf.Sin((x * 0.5f + z * 0.866f) * 0.35f)), pist);
            float yol = YolUzakligi(x, z);
            c = Color.Lerp(c, Yol, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3f, 6.5f, yol)));
            float koy = KoyUzakligi(x, z);
            c = Color.Lerp(c, Yol * 0.85f, (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(60f, 150f, koy))) * 0.35f);

            float parlak = 1f + ince * 0.12f;
            return new Color(c.r * parlak, c.g * parlak, c.b * parlak, 1f);
        }

        // ---- trees --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Deterministic tree positions (x, z, height, palette index 0..3) inside the play area: forests on a jittered
        /// 26 m grid, single trees along hedgerows and roads. Same list every run.
        /// </summary>
        public static List<Vector4> Agaclar(float alanYariCapi = 3300f, float aralik = 26f)
        {
            int n = Mathf.CeilToInt(alanYariCapi / aralik);
            YolUzakligi(0f, 0f);   // build the road cache before the parallel rows
            var satirlar = new List<Vector4>[2 * n + 1];
            Parallel.For(-n, n + 1, j =>
            {
                var liste = new List<Vector4>();
                satirlar[j + n] = liste;
                for (int i = -n; i <= n; i++)
                {
                    float x = (i + AraziUretici.Rastgele01(i, j, 601u) * 0.8f + 0.1f) * aralik;
                    float z = (j + AraziUretici.Rastgele01(i, j, 607u) * 0.8f + 0.1f) * aralik;
                    if (x * x + z * z > alanYariCapi * alanYariCapi)
                        continue;
                    float h = Yukseklik(x, z);
                    float ny = Normal(x, z, 6f).y;
                    bool orman = OrmanMi(x, z, h, ny);
                    bool tek = !orman && h > 9f && ny > 0.9f && PistMaskesi(x, z) <= 0f && NehirUzakligi(x, z) > 70f
                               && KoyUzakligi(x, z) > 60f && YolUzakligi(x, z) > 9f && MevziUzakligi(x, z) > 45f
                               && AraziUretici.Rastgele01(i, j, 613u) < 0.012f;
                    if (!orman && !tek)
                        continue;
                    float boy = 9f + AraziUretici.Rastgele01(i, j, 617u) * 9f;
                    float renk = Mathf.Floor(AraziUretici.Rastgele01(i, j, 619u) * 3.999f);
                    liste.Add(new Vector4(x, z, boy, renk));
                }
            });
            var hepsi = new List<Vector4>();
            foreach (var s in satirlar)
                hepsi.AddRange(s);   // same order every run
            return hepsi;
        }

        // ---- helpers ------------------------------------------------------------------------------------------------

        /// <summary>Horizontal unit vector for a compass heading (0 = north = +z, 90 = east = +x).</summary>
        public static Vector3 Yon(float derece) =>
            new Vector3(Mathf.Sin(derece * Mathf.Deg2Rad), 0f, Mathf.Cos(derece * Mathf.Deg2Rad));

        static Vector2 Duz(Vector3 v) => new Vector2(v.x, v.z);
        static Vector3 Nokta(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        /// <summary>Texture coordinate of (x, z) on the colour map (0..1 across the terrain square).</summary>
        public static Vector2 Uv(float x, float z) => new Vector2((x + YarimBoyut) / Boyut, (z + YarimBoyut) / Boyut);
    }
}
