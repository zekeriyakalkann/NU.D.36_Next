using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5 · Kartal Geçidi): the map data. The worksheet gives no drawing, so the layout is INFERRED
    /// (Documentation/Lab9_Plan.md §4): a start valley in the west, a high north–south ridge cut by one narrow winding pass
    /// (Kartal Geçidi), a coastal plain with a sandy beach and the bay (Kartal Koyu) to the east, closed by two headlands
    /// and open to the sea in the east. Sea level is y = 0. Pure functions: the editor builds the terrain from
    /// <see cref="Yukseklik"/> and the tests check the same numbers.
    /// </summary>
    public static class KartalGecidiHaritasi
    {
        // ---- terrain ---------------------------------------------------------------------------------------
        public const float AraziBoyutu = 8000f, AraziYuksekligi = 1700f;
        /// <summary>The terrain starts 70 m below the sea, so the bay has a floor.</summary>
        public static readonly Vector3 AraziKosesi = new Vector3(-4000f, -70f, -4000f);
        public const float DenizSeviyesi = 0f;
        /// <summary>Lowest sea floor (m).</summary>
        public const float EnDerin = -45f;

        // ---- the pass ----------------------------------------------------------------------------------------
        /// <summary>Pass centre line (x, floor height, z), west to east: valley → saddle → down to the coastal plain.</summary>
        static readonly Vector3[] GecitCizimi =
        {
            new Vector3(-2600f, 200f, 120f), new Vector3(-2000f, 205f, 60f), new Vector3(-1500f, 215f, 0f),
            new Vector3(-1050f, 232f, -90f), new Vector3(-650f, 252f, -40f), new Vector3(-250f, 272f, 110f),
            new Vector3(150f, 268f, 130f), new Vector3(480f, 235f, 10f), new Vector3(780f, 150f, -60f),
            new Vector3(1050f, 45f, -40f), new Vector3(1250f, 8f, 0f),
        };

        static RayEgrisi gecit;
        /// <summary>The pass centre line as an arc-length curve (cached).</summary>
        public static RayEgrisi Gecit => gecit ??= new RayEgrisi(GecitCizimi, 2f);

        /// <summary>Half width of the flat pass floor, and the width over which the walls rise to the ridge.</summary>
        public const float GecitTabanYariGenisligi = 55f, GecitDuvarGenisligi = 260f, GecitEtkisi = 520f;
        /// <summary>The ridge (x range, plan view) the pass cuts through; it is at least <see cref="SirtEnAz"/> high.</summary>
        public const float SirtBati = -1000f, SirtDogu = 700f, SirtEnAz = 950f;

        // ---- start -------------------------------------------------------------------------------------------
        /// <summary>The aircraft starts on the pass line this far from its west end, this high over the floor.</summary>
        public const float BaslangicS = 950f, BaslangicYuksekligi = 150f;
        public static Vector3 BaslangicNoktasi => Gecit.Nokta(BaslangicS) + Vector3.up * BaslangicYuksekligi;
        public static Vector3 BaslangicYonu => Gecit.YatayYon(BaslangicS);

        // ---- coast and bay -------------------------------------------------------------------------------------
        /// <summary>Coastline x at z: a concave beach (x ≈ 1 350 m at z = 0) between two headlands that run east beyond |z| ≈ 1.5 km.</summary>
        public static float KiyiX(float z)
        {
            float a = Mathf.Abs(z);
            return 1350f + 230f * (z / 1000f) * (z / 1000f) + 1700f * Smooth(1200f, 1900f, a);
        }

        /// <summary>Signed distance to the coast along x: &gt; 0 at sea, &lt; 0 inland.</summary>
        public static float DenizUzakligi(float x, float z) => x - KiyiX(z);

        public static bool Denizde(float x, float z) => DenizUzakligi(x, z) > 0f;

        public const float KumsalGenisligi = 80f;
        public static readonly Vector3 KoyMerkezi = new Vector3(2150f, 0f, 0f);

        // ---- landing lanes -------------------------------------------------------------------------------------
        /// <summary>Four lanes along the beach, 300 m apart (worksheet: four ships at once, spaced).</summary>
        public static readonly float[] SeritZ = { -450f, -150f, 150f, 450f };
        /// <summary>Bow distance from the ship's centre; the ship stops with its bow this far out from the waterline
        /// (about 0.3 m of water under the bow, so the ramp reaches down to the sand at the waterline).</summary>
        public const float PruvaUzakligi = 23f, PruvaPayi = 6f;
        /// <summary>Where a lane's ship stops (centre, at sea level).</summary>
        public static Vector3 YanasmaNoktasi(int serit) => new Vector3(KiyiX(SeritZ[serit]) + PruvaUzakligi + PruvaPayi, DenizSeviyesi, SeritZ[serit]);
        /// <summary>Where a tank, off the ramp, is out of the water on the wet sand: it has landed (worksheet count).</summary>
        public static Vector3 KaraNoktasi(int serit) => new Vector3(KiyiX(SeritZ[serit]) - 4f, 0f, SeritZ[serit]);
        /// <summary>Where the tanks leave the beach, side by side towards their parking rows.</summary>
        public static Vector3 KumsalCikisi(int serit, float z) => new Vector3(KiyiX(SeritZ[serit]) - 55f, 0f, Mathf.Lerp(SeritZ[serit], z, 0.5f));
        /// <summary>Towards the beach (ships sail west).</summary>
        public static readonly Vector3 KiyiyaDogru = Vector3.left;
        /// <summary>Start distances of the first four ships from their stop point (staggered: they arrive one by one).</summary>
        public static readonly float[] IlkUzakliklar = { 1000f, 700f, 1300f, 1600f };
        public const float YeniGemiUzakligi = 1400f, YenilemeGecikmesi = 6f;
        public const int GemiSayisi = 12, GemiBasinaTank = 3, KiyiDusmeSiniri = 12;

        /// <summary>Parking spot of the n-th tank landed in a lane: rows of three behind the beach.</summary>
        public static Vector3 ParkYeri(int serit, int n)
        {
            float z0 = SeritZ[serit];
            int sira = n / 3, sutun = n % 3;
            return new Vector3(KiyiX(z0) - 125f - sira * 17f, 0f, z0 + (sutun - 1) * 13f);
        }

        // ---- air ---------------------------------------------------------------------------------------------
        public static readonly Vector3 HaritaMerkezi = new Vector3(300f, 0f, 0f);
        /// <summary>The flight assist turns the aircraft back beyond this radius from <see cref="HaritaMerkezi"/>.</summary>
        public const float SinirYaricapi = 3300f;
        /// <summary>Guard flights: patrol circle over the bay, attack radius, entry points (over the open sea), exit radius.</summary>
        public const float DevriyeYaricapi = 800f, DevriyeYuksekligi = 350f, KorumaYaricapi = 2400f, CikisYaricapi = 4200f;
        public static readonly Vector3[] KorumaGirisleri = { new Vector3(3700f, 420f, -700f), new Vector3(3700f, 460f, 700f), new Vector3(3800f, 500f, 0f) };
        public const int KorumaAyniAnda = 2, KorumaToplam = 6;
        public const float KorumaYenileme = 30f;
        /// <summary>Bölüm 4's rule kept: no bomb release more than this above the ground (or the sea).</summary>
        public const float BombaTavani = 300f;

        // ---- height function ------------------------------------------------------------------------------------

        public static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static float Smoother(float t) { t = Mathf.Clamp01(t); return t * t * t * (t * (t * 6f - 15f) + 10f); }

        /// <summary>Ridged noise 0…1.</summary>
        public static float Gurultu(float x, float z)
        {
            float n = 0f, a = 1f, f = 1f / 1400f, top = 0f;
            for (int o = 0; o < 5; o++)
            {
                float v = Mathf.PerlinNoise(x * f + 71.3f + o * 13.7f, z * f + 19.9f + o * 7.3f);
                float r = 1f - Mathf.Abs(2f * v - 1f);
                n += a * r * r;
                top += a;
                a *= 0.5f;
                f *= 2.05f;
            }
            return n / top;
        }

        /// <summary>
        /// 1 inside the ridge band, ramping to 0 over 400 m on both sides. Away from the pass (|z| &gt; 300 m) the band's edges
        /// wander up to ±300 m with the noise, so the mountain fronts are not straight lines.
        /// </summary>
        public static float SirtAgirligi(float x, float z)
        {
            float w = 300f * (Mathf.PerlinNoise(z / 900f + 3.7f, 1.3f) - 0.5f) * 2f * Smooth(300f, 800f, Mathf.Abs(z));
            float bati = SirtBati + w, dogu = SirtDogu + 0.8f * w;
            return Smooth(bati - 400f, bati, x) * (1f - Smooth(dogu, dogu + 400f, x));
        }

        /// <summary>Mountains, valley and ridge before the coast and the pass.</summary>
        public static float Dag(float x, float z)
        {
            float n = Gurultu(x, z);
            float dag = 320f + 900f * Mathf.Pow(Mathf.Clamp01(n * 1.15f), 1.2f);
            // The ridge: continuous north–south, at least SirtEnAz high.
            float sirt = SirtAgirligi(x, z);
            dag = Mathf.Lerp(dag, Mathf.Max(dag, SirtEnAz + 450f * n), sirt);
            // Start valley in the west, around the pass axis.
            float ex = (x + 2000f) / 1100f, ez = (z - 60f) / 900f;
            float r = Mathf.Sqrt(ex * ex + ez * ez) + 0.18f * (Mathf.PerlinNoise(x / 600f + 4.1f, z / 600f + 9.3f) - 0.5f);
            float vadi = Smooth(1f, 0.78f, r);   // an oval basin with a ragged edge
            float taban = 185f + 40f * Mathf.PerlinNoise(x / 500f + 4.1f, z / 500f + 2.7f);
            dag = Mathf.Lerp(dag, taban, vadi);
            // High ring at the west, north and south map edges (the sea side stays open).
            float kenar = Mathf.Max(Mathf.Abs(z), -x);
            dag = Mathf.Lerp(dag, Mathf.Max(dag, 1300f + 150f * n), Smooth(3100f, 3700f, kenar));
            return dag;
        }

        /// <summary>Ground height of the coastal strip: beach and plain inland, the sloping bay floor at sea.</summary>
        public static float KiyiProfili(float ds, float x, float z)
        {
            if (ds >= 0f)
                return Mathf.Max(EnDerin, 0.4f - Mathf.Min(ds, 30f) * 0.12f - Mathf.Max(0f, ds - 30f) * 0.07f);
            float u = -ds;
            if (u <= KumsalGenisligi) return 0.4f + u * 0.03f;
            float ova = 2.8f + 28f * Smooth(KumsalGenisligi, 650f, u);
            return ova + 7f * Mathf.PerlinNoise(x / 260f + 8.2f, z / 260f + 1.3f) * Smooth(KumsalGenisligi, 260f, u);
        }

        /// <summary>
        /// Distance along the pass line of the point nearest to (x, z) in plan view, and the plan distance to it. Fast for the
        /// million terrain samples: a coarse search every 10 samples, then the fine samples and segments around the best.
        /// Far from the pass (outside its bounding box plus the influence) it returns at once with a large distance.
        /// </summary>
        public static float GecitUzakligi(float x, float z, out float d)
        {
            RayEgrisi e = Gecit;
            if (z < -350f - GecitEtkisi || z > 350f + GecitEtkisi || x < -2800f - GecitEtkisi || x > 1450f + GecitEtkisi)
            {
                d = float.MaxValue;
                return 0f;
            }
            int n = e.OrnekSayisi, enIyiI = 0;
            float enIyi = float.MaxValue;
            for (int i = 0; i < n; i += 10)
            {
                Vector3 o = e.Ornek(i);
                float dd = (o.x - x) * (o.x - x) + (o.z - z) * (o.z - z);
                if (dd < enIyi) { enIyi = dd; enIyiI = i; }
            }
            int bas = Mathf.Max(0, enIyiI - 12), son = Mathf.Min(n - 1, enIyiI + 12);
            for (int i = bas; i <= son; i++)
            {
                Vector3 o = e.Ornek(i);
                float dd = (o.x - x) * (o.x - x) + (o.z - z) * (o.z - z);
                if (dd < enIyi) { enIyi = dd; enIyiI = i; }
            }
            float s = enIyiI * e.Adim;
            for (int i = Mathf.Max(0, enIyiI - 1); i <= Mathf.Min(enIyiI, n - 2); i++)
            {
                Vector3 a3 = e.Ornek(i), b3 = e.Ornek(i + 1);
                Vector2 a = new Vector2(a3.x, a3.z), ab = new Vector2(b3.x, b3.z) - a, q = new Vector2(x, z);
                float u = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
                float dd = (a + ab * u - q).sqrMagnitude;
                if (dd < enIyi) { enIyi = dd; s = (i + u) * e.Adim; }
            }
            d = Mathf.Sqrt(enIyi);
            return Mathf.Min(s, e.Uzunluk);
        }

        /// <summary>Final ground height at (x, z).</summary>
        public static float Yukseklik(float x, float z)
        {
            float ds = DenizUzakligi(x, z);
            float kiyi = KiyiProfili(ds, x, z);
            float h;
            if (ds >= 0f)
                h = kiyi;
            else
            {
                float u = -ds;
                float dag = Dag(x, z);
                // Near the coast the plain wins; inland the mountains. The ridge stays whole up to its east foot.
                h = Mathf.Lerp(kiyi, Mathf.Max(kiyi, dag), Mathf.Max(Smooth(420f, 1100f, u), SirtAgirligi(x, z)));
            }
            // The pass: a flat floor along the centre line, walls rising to the ridge; it never raises lower ground.
            float s = GecitUzakligi(x, z, out float d);
            if (d < GecitEtkisi)
            {
                float taban = Gecit.Nokta(s).y;
                float t = (d - GecitTabanYariGenisligi) / GecitDuvarGenisligi;
                float kor = d <= GecitTabanYariGenisligi ? taban : Mathf.Lerp(taban, h, Smoother(t));
                kor = Mathf.Lerp(kor, h, Smooth(GecitEtkisi - 150f, GecitEtkisi, d));
                if (ds < -40f) h = Mathf.Min(h, kor);
            }
            return Mathf.Clamp(h, AraziKosesi.y + 1f, AraziKosesi.y + AraziYuksekligi - 1f);
        }
    }
}
