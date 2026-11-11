using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4 · Kurşun Dağları): the map data. The worksheet's map drawing was not available (only its labels), so
    /// the layout is INFERRED (Documentation/Lab8_Plan.md §4): a single line shaped like a horseshoe through steep
    /// mountains, ~5.2 km long, departure station in the south-west, the airbase ~750 m west of it, the safe zone in the
    /// south-east (~2.5 km from the base, so the flight home is short). Fire zones are given as fractions of the line
    /// length so the 5-minute timeline holds for the actual curve length.
    /// </summary>
    public static class KursunDaglariHaritasi
    {
        // ---- railway ---------------------------------------------------------------------------------------
        /// <summary>Plan-view scale applied to the drawn control points (fixes the line at ~5.2 km).</summary>
        public const float Olcek = 0.86f;

        // (x, height, z) before scaling x and z. Station → west leg (Dar Vadi) → north leg (viaduct over the gorge)
        // → tight bend (Son Viraj) → south to the safe zone.
        static readonly Vector3[] Cizim =
        {
            new Vector3(-1150f, 118f, -1250f), new Vector3(-1150f, 120f, -950f), new Vector3(-1180f, 126f, -560f),
            new Vector3(-1120f, 138f, -170f), new Vector3(-1170f, 152f, 220f), new Vector3(-1080f, 166f, 590f),
            new Vector3(-830f, 180f, 880f), new Vector3(-430f, 195f, 1030f), new Vector3(0f, 200f, 1060f),
            new Vector3(420f, 200f, 1030f), new Vector3(760f, 196f, 1000f), new Vector3(1000f, 190f, 890f),
            new Vector3(1100f, 182f, 680f), new Vector3(1040f, 174f, 440f), new Vector3(900f, 165f, 170f),
            new Vector3(820f, 155f, -230f), new Vector3(800f, 148f, -640f), new Vector3(800f, 145f, -950f),
        };

        public static Vector3[] RayNoktalari()
        {
            var p = new Vector3[Cizim.Length];
            for (int i = 0; i < p.Length; i++)
                p[i] = new Vector3(Cizim[i].x * Olcek, Cizim[i].y, Cizim[i].z * Olcek);
            return p;
        }

        static RayEgrisi egri;
        /// <summary>The line built from <see cref="RayNoktalari"/> (cached).</summary>
        public static RayEgrisi Egri => egri ??= new RayEgrisi(RayNoktalari());

        /// <summary>Rail top above the line's centre (the rail model is 0.16 m high at the project scale).</summary>
        public const float RayUstu = 0.16f;

        // ---- timeline ---------------------------------------------------------------------------------------
        public const float VarisSuresi = 300f;        // worksheet: 5 minutes (±15 s with the enemies off)
        public const float KalkisSuresi = 20f;
        /// <summary>Locomotive centre at the start (the whole train stands on the line) and its distance from the end on arrival.</summary>
        public const float TrenBaslangici = 80f, TrenBitisPayi = 45f;

        public readonly struct Bolge
        {
            public readonly string Ad;
            public readonly float Bas, Son;            // fractions of the line length
            public readonly int Tank, Ucaksavar, Ucak;
            public Bolge(string ad, float bas, float son, int tank, int ucaksavar, int ucak)
            { Ad = ad; Bas = bas; Son = son; Tank = tank; Ucaksavar = ucaksavar; Ucak = ucak; }
        }

        /// <summary>Worksheet: 1 · Dar Vadi (2 tanks, 1 AA), 2 · Viyadük (2 AA, 2 aircraft), 3 · Son Viraj (2 tanks, 1 AA,
        /// 2 aircraft); calm gaps between them.</summary>
        public static readonly Bolge[] Bolgeler =
        {
            new Bolge("1 · Dar Vadi", 0.20f, 0.36f, 2, 1, 0),
            new Bolge("2 · Viyadük", 0.46f, 0.60f, 0, 2, 2),
            new Bolge("3 · Son Viraj", 0.70f, 0.86f, 2, 1, 2),
        };

        public enum MevziTuru { Tank, Ucaksavar }

        /// <summary>A ground position beside the line: zone index, kind, distance along the line (fraction), side
        /// (+1 = right of the direction of travel), sideways distance and the pad height above the rail.</summary>
        public readonly struct Mevzi
        {
            public readonly int Bolge;
            public readonly MevziTuru Tur;
            public readonly float S, Uzaklik, Yukselti;
            public readonly int Taraf;
            public Mevzi(int bolge, MevziTuru tur, float s, int taraf, float uzaklik, float yukselti)
            { Bolge = bolge; Tur = tur; S = s; Taraf = taraf; Uzaklik = uzaklik; Yukselti = yukselti; }
        }

        /// <summary>Tank pairs (worksheet: "tanklar ikişer durur") and AA guns. Tanks fire at the train from beside the
        /// line; AA guns sit a little away from it. The terrain gets a flat pad under each.</summary>
        public static readonly Mevzi[] Mevziler =
        {
            new Mevzi(0, MevziTuru.Tank, 0.275f, +1, 32f, 1f),
            new Mevzi(0, MevziTuru.Ucaksavar, 0.315f, -1, 62f, 5f),
            new Mevzi(1, MevziTuru.Ucaksavar, 0.475f, +1, 70f, 2f),
            new Mevzi(1, MevziTuru.Ucaksavar, 0.56f, -1, 70f, 2f),
            new Mevzi(2, MevziTuru.Tank, 0.775f, +1, 52f, 2f),
            new Mevzi(2, MevziTuru.Ucaksavar, 0.735f, -1, 68f, 3f),
        };

        /// <summary>Distance between the two tanks of a pair (a 25 m bomb blast between them reaches both).</summary>
        public const float TankCiftiAraligi = 16f;

        /// <summary>Worksheet 05 (limit INFERRED): AA guns hold fire at an aircraft more than this above the gun; bombs cannot
        /// be released more than this above the ground. High up you are safe, but you cannot hit a tank from there.</summary>
        public const float UcaksavarTavani = 300f, BombaTavani = 300f;

        /// <summary>Enemies of a zone show up when the locomotive is this far before the zone (m).</summary>
        public const float BolgeOnden = 150f;

        /// <summary>Entry points of zone <paramref name="bolge"/>'s aircraft: outwards from the map centre beyond the zone,
        /// 1.5 km away, fanned ±25°, inside the mountain ring. Height comes from the terrain (builder).</summary>
        public static Vector3 UcakGirisi(int bolge, int sira, RayEgrisi e)
        {
            Bolge b = Bolgeler[bolge];
            Vector3 orta = e.Nokta((b.Bas + b.Son) * 0.5f * e.Uzunluk);
            Vector3 disari = new Vector3(orta.x, 0f, orta.z);
            disari = disari.sqrMagnitude > 1f ? disari.normalized : Vector3.forward;
            disari = Quaternion.Euler(0f, sira == 0 ? -25f : 25f, 0f) * disari;
            Vector3 p = orta + disari * 1500f;
            Vector2 yatay = new Vector2(p.x, p.z);
            if (yatay.magnitude > 2500f) yatay = yatay.normalized * 2500f;
            return new Vector3(yatay.x, 0f, yatay.y);
        }

        public static Vector3 MevziKonumu(Mevzi m, RayEgrisi e)
        {
            float s = m.S * e.Uzunluk;
            return e.Nokta(s) + e.Sag(s) * (m.Taraf * m.Uzaklik) + Vector3.up * m.Yukselti;
        }

        // ---- terrain ----------------------------------------------------------------------------------------
        public const float AraziBoyutu = 8000f, AraziYuksekligi = 1500f;
        public static readonly Vector3 AraziKosesi = new Vector3(-4000f, 0f, -4000f);
        public static readonly Vector3 HaritaMerkezi = Vector3.zero;
        /// <summary>The flight assist turns the aircraft back beyond this radius; the boundary mountains start at 3000 m.</summary>
        public const float SinirYaricapi = 2700f;

        /// <summary>The gorge under the viaduct: river bed height and its course (plan view, north to south).</summary>
        public const float NehirTabani = 75f;
        public static readonly Vector2[] NehirHatti =
        {
            new Vector2(-60f, 3600f), new Vector2(-20f, 2200f), new Vector2(0f, 1500f), new Vector2(5f, 910f),
            new Vector2(40f, 300f), new Vector2(90f, -500f), new Vector2(160f, -1500f), new Vector2(240f, -2600f), new Vector2(300f, -3800f),
        };
        public const float SuSeviyesi = NehirTabani + 2.5f;

        // ---- base (Üs) --------------------------------------------------------------------------------------
        public static readonly Vector3 UsMerkezi = new Vector3(-1800f, 112f, -1210f);
        public const float PistYonu = 90f, PistBoyu = 1000f, PistEni = 40f;
        public static Vector3 PistEkseni => Quaternion.Euler(0f, PistYonu, 0f) * Vector3.forward;
        public static Vector3 PistBasi => UsMerkezi - PistEkseni * (PistBoyu / 2f);
        public static Vector3 PistSonu => UsMerkezi + PistEkseni * (PistBoyu / 2f);
        /// <summary>Flat ground around the airfield (half sizes along / across the runway).</summary>
        public static readonly Vector2 UsDuzlugu = new Vector2(620f, 170f);
        /// <summary>Plan decision 3: reaching the base = entering this radius below this height above the ground.</summary>
        public const float UsYaricapi = 500f, UsTavani = 200f;

        // ---- stations ---------------------------------------------------------------------------------------
        /// <summary>Departure station: platform along s = 0 … 200 m. Safe zone: the last 320 m of the line.</summary>
        public const float IstasyonBoyu = 200f, GuvenliBolgeBoyu = 320f;
        public static float GuvenliBolgeBasi(float uzunluk) => uzunluk - GuvenliBolgeBoyu;
    }
}
