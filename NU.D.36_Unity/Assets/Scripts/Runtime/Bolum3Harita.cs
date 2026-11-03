using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 6 (Hafta 6): the Bölüm 3 paper map, version 0.1, as data. One source for the Unity grey box
    /// (<c>Lab6SceneBuilder</c>), the printable map (Blender, via the exported JSON) and the tests.
    /// Grid 12 × 9 squares (A–L west→east, 1–9 north→south), 1 square = 200 m (worksheet), so the map is
    /// 2400 × 1800 m, centred on the GriKutu object at (3000, 0, 0). North = +Z, east = +X.
    /// The design itself (positions, the single rule, the routes) is a proposal (see Lab6_Bolum3_Tasarim.md).
    /// </summary>
    public static class Bolum3Harita
    {
        public const string Surum = "0.1";
        public const int Sutun = 12, Satir = 9;
        public const float Kare = 200f;
        public static readonly Vector3 Merkez = new Vector3(3000f, 0f, 0f);

        // Paper altitudes (worksheet): Alçak 30 m, Orta 200 m.
        public const float Alcak = 30f, Orta = 200f;
        // Worksheet: 1 paper turn = 15 s.
        public const float TurSuresi = 15f;

        public static readonly string[] Pist = { "B8", "C8", "D8" };
        public static readonly string[] Nehir = { "I1", "I2", "I3", "I4", "H5", "I5", "J5", "J6", "J7", "J8", "J9" };
        public static readonly string[] Kopru = { "H5", "I5", "J5" };
        public static readonly string[] Tepeler = { "E4", "F7", "G7" };
        /// <summary>Searchlights: square and starting direction in compass degrees (0 = N, 90 = E).</summary>
        public static readonly (string kare, float yon)[] Isildaklar = { ("F4", 135f), ("K5", 270f), ("H7", 0f) };
        public static readonly string[] Ucaksavarlar = { "G4", "K4", "I7" };
        public const string SesDinleyici = "H3";
        /// <summary>The single rule ("Bulut"): in these 3 squares a searchlight cannot catch an aircraft at Orta altitude.</summary>
        public static readonly string[] Bulutlar = { "E5", "F5", "G5" };
        public const string TekKural = "E5, F5 ve G5 bulut karesidir: Orta irtifadaki uçağı bu 3 karede ışıldak yakalayamaz.";

        public const string Baslangic = "C8";
        public const float BaslangicYonu = 45f; // nose north-east (worksheet warm-up)

        /// <summary>Route 1: low, behind the F7–G7 ridge, last leg over the H7 searchlight to the bridge.</summary>
        public static readonly string[] Yol1TepeArkasi = { "C8", "D8", "E8", "F8", "G8", "H8", "H7", "H6", "H5" };
        /// <summary>Route 2: open, diagonal, through the clouds.</summary>
        public static readonly string[] Yol2Acik = { "C8", "D7", "E6", "F5", "G5", "H5" };

        // Ranges from the worksheet: beam 1200 m (6 squares), gun range Ø 1600 m, sound locator Ø 2000 m, hill 120 m.
        public const float IsinBoyu = 1200f, UcaksavarCapi = 1600f, SesCapi = 2000f, TepeYuksekligi = 120f;
        public const float KopruYuksekligi = 25f;

        /// <summary>Column (0 = A) and row (0 = row 1) of a square name such as "C8".</summary>
        public static (int sutun, int satir) Coz(string kare)
        {
            if (string.IsNullOrEmpty(kare) || kare.Length < 2)
                throw new ArgumentException("Kare adı A1 … L9 biçiminde olmalı: " + kare);
            int s = char.ToUpperInvariant(kare[0]) - 'A';
            int r = int.Parse(kare.Substring(1)) - 1;
            if (s < 0 || s >= Sutun || r < 0 || r >= Satir)
                throw new ArgumentOutOfRangeException(nameof(kare), kare);
            return (s, r);
        }

        /// <summary>World position (y = 0) of the centre of a square.</summary>
        public static Vector3 KareMerkezi(string kare)
        {
            var (s, r) = Coz(kare);
            return new Vector3(Merkez.x - Sutun * Kare / 2f + Kare * (s + 0.5f), 0f,
                               Merkez.z + Satir * Kare / 2f - Kare * (r + 0.5f));
        }

        /// <summary>Unit vector (y = 0) for a compass direction in degrees.</summary>
        public static Vector3 Yon(float pusulaDerece) =>
            new Vector3(Mathf.Sin(pusulaDerece * Mathf.Deg2Rad), 0f, Mathf.Cos(pusulaDerece * Mathf.Deg2Rad));

        /// <summary>Length of a route through square centres (m).</summary>
        public static float YolUzunlugu(string[] yol)
        {
            float l = 0f;
            for (int i = 1; i < yol.Length; i++)
                l += Vector3.Distance(KareMerkezi(yol[i - 1]), KareMerkezi(yol[i]));
            return l;
        }
    }
}
