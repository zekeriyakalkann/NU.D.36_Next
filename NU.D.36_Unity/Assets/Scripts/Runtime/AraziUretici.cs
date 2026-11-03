using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Deterministic terrain: a pure height function of world (x, z) plus flat-shaded chunk mesh
    /// generation. Chunks sample the same function at the same world positions, so neighbouring
    /// chunks share identical edge heights (no gaps) and the same chunk always looks the same.
    /// </summary>
    public static class AraziUretici
    {
        /// <summary>Ground height at the world origin; the Uçaksavar capsule (radius 0.5, height 2) stands on it.</summary>
        public const float BaslangicYuksekligi = -1f;

        // ---- noise ------------------------------------------------------------------------

        static uint Hash(int x, int z, uint tohum)
        {
            uint h = (uint)x * 374761393u + (uint)z * 668265263u + tohum * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }

        static float Rastgele01(int x, int z, uint tohum) => (Hash(x, z, tohum) & 0xFFFFFFu) / 16777215f;

        static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        static float Egim(uint h, float dx, float dz)
        {
            switch (h & 7u)
            {
                case 0: return dx + dz;
                case 1: return dx - dz;
                case 2: return -dx + dz;
                case 3: return -dx - dz;
                case 4: return dx * 1.4f;
                case 5: return -dx * 1.4f;
                case 6: return dz * 1.4f;
                default: return -dz * 1.4f;
            }
        }

        /// <summary>Gradient noise, roughly in -1..1.</summary>
        static float Gurultu(float x, float z, uint tohum)
        {
            int xi = Mathf.FloorToInt(x), zi = Mathf.FloorToInt(z);
            float xf = x - xi, zf = z - zi;
            float u = Fade(xf), v = Fade(zf);
            float n00 = Egim(Hash(xi, zi, tohum), xf, zf);
            float n10 = Egim(Hash(xi + 1, zi, tohum), xf - 1f, zf);
            float n01 = Egim(Hash(xi, zi + 1, tohum), xf, zf - 1f);
            float n11 = Egim(Hash(xi + 1, zi + 1, tohum), xf - 1f, zf - 1f);
            return Mathf.Lerp(Mathf.Lerp(n00, n10, u), Mathf.Lerp(n01, n11, u), v) * 0.8f;
        }

        static float Fbm(float x, float z, int oktav, uint tohum)
        {
            float toplam = 0f, genlik = 1f, norm = 0f;
            for (int i = 0; i < oktav; i++)
            {
                toplam += Gurultu(x, z, tohum + (uint)i * 101u) * genlik;
                norm += genlik;
                x = x * 2.03f + 17.3f;
                z = z * 2.03f - 9.1f;
                genlik *= 0.5f;
            }
            return toplam / norm;
        }

        // ---- height -----------------------------------------------------------------------

        // Offsets that place the first mountain ranges ahead of (+Z) the start; chosen by sampling
        // (see Lab3_Terrain_Implementation.md).
        const float DagBolgeX = -780f, DagBolgeZ = -1880f;

        /// <summary>
        /// Rolling hills everywhere; far from the start (more than about 650 m) ridged mountain
        /// ranges can rise in some regions. Flat and level around the origin so the Lab 2 objects
        /// stay readable.
        /// </summary>
        public static float Yukseklik(float x, float z)
        {
            float yuvarlak = Fbm(x / 520f, z / 520f, 4, 11u);
            float ayrinti = Fbm(x / 150f, z / 150f, 2, 23u);
            float tepeler = 6f + yuvarlak * 15f + ayrinti * 4f;

            float r = Mathf.Sqrt(x * x + z * z);
            float kapi = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(650f, 1500f, r));
            if (kapi > 0f)
            {
                float bolge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.06f, 0.10f,
                    Fbm((x + DagBolgeX) / 2600f, (z + DagBolgeZ) / 2600f, 2, 53u)));
                float m = bolge * kapi;
                if (m > 0f)
                {
                    float sirt = 1f - Mathf.Min(1f, Mathf.Abs(Fbm(x / 780f, z / 780f, 3, 71u)) * 2.6f);
                    tepeler += m * (35f + 330f * sirt * sirt);
                }
            }

            float duzluk = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30f, 230f, r));
            return Mathf.Lerp(BaslangicYuksekligi, tepeler, duzluk);
        }

        /// <summary>
        /// Height of the terrain MESH (not the smooth function) at (x, z): the same triangles
        /// <see cref="ChunkUret"/> builds, including its checkerboard diagonals. Used for ground contact.
        /// </summary>
        public static float YuzeyYuksekligi(float x, float z, float hucreBoyutu = 16f)
        {
            float gx = x / hucreBoyutu, gz = z / hucreBoyutu;
            int i = Mathf.FloorToInt(gx), j = Mathf.FloorToInt(gz);
            float fx = gx - i, fz = gz - j;
            float y00 = Yukseklik(i * hucreBoyutu, j * hucreBoyutu);
            float y10 = Yukseklik((i + 1) * hucreBoyutu, j * hucreBoyutu);
            float y01 = Yukseklik(i * hucreBoyutu, (j + 1) * hucreBoyutu);
            float y11 = Yukseklik((i + 1) * hucreBoyutu, (j + 1) * hucreBoyutu);
            if (((i + j) & 1) == 0) // diagonal p00-p11
                return fz >= fx ? y00 + fz * (y01 - y00) + fx * (y11 - y01)
                                : y00 + fx * (y10 - y00) + fz * (y11 - y10);
            // diagonal p10-p01
            return fx + fz <= 1f ? y00 + fx * (y10 - y00) + fz * (y01 - y00)
                                 : y11 + (1f - fx) * (y01 - y11) + (1f - fz) * (y10 - y11);
        }

        // ---- colour -----------------------------------------------------------------------

        static readonly Color32 Cayir = new Color32(86, 150, 58, 255);
        static readonly Color32 AcikCayir = new Color32(132, 176, 66, 255);
        static readonly Color32 Kuru = new Color32(168, 172, 84, 255);
        static readonly Color32 Orman = new Color32(52, 110, 52, 255);
        static readonly Color32 Kaya = new Color32(122, 112, 100, 255);
        static readonly Color32 Kar = new Color32(240, 244, 250, 255);

        static Color Mix(Color32 a, Color32 b, float t) => Color.Lerp(a, b, t);

        /// <summary>Face colour from height, steepness and a large-scale patch value (-1..1); all deterministic.</summary>
        static Color32 Renk(float yukseklik, float yuzeyY, float yama, float titresim)
        {
            Color c = Mix(Cayir, AcikCayir, Mathf.InverseLerp(-0.4f, 0.6f, yama));
            c = Color.Lerp(c, Mix(Orman, Kuru, Mathf.InverseLerp(-0.2f, 0.2f, yama)), Mathf.InverseLerp(0.1f, 0.7f, Mathf.Abs(yama)) * 0.45f);
            c = Color.Lerp(c, Kuru, Mathf.InverseLerp(14f, 40f, yukseklik) * 0.6f);
            c = Color.Lerp(c, Kaya, Mathf.InverseLerp(55f, 110f, yukseklik));
            c = Color.Lerp(c, Kaya, Mathf.InverseLerp(0.86f, 0.70f, yuzeyY));
            c = Color.Lerp(c, Kar, Mathf.InverseLerp(190f, 250f, yukseklik) * Mathf.InverseLerp(0.55f, 0.85f, yuzeyY + 0.15f));
            float parlaklik = 1f + titresim * 0.07f;
            return new Color(c.r * parlaklik, c.g * parlaklik, c.b * parlaklik, 1f);
        }

        // ---- mesh -------------------------------------------------------------------------

        public static int KoseSayisi(int hucre) => hucre * hucre * 6;

        /// <summary>
        /// Fills the supplied buffers with the flat-shaded mesh of chunk (cx, cz). Vertices are in
        /// chunk-local space (origin at cx·chunkBoyutu, 0, cz·chunkBoyutu); triangles are
        /// consecutive vertex triples. <paramref name="ornek"/> must hold (hucre+1)² floats.
        /// </summary>
        public static void ChunkUret(int cx, int cz, float chunkBoyutu, int hucre,
            Vector3[] koseler, Vector3[] normaller, Color32[] renkler, float[] ornek,
            out float enAlcak, out float enYuksek)
        {
            float adim = chunkBoyutu / hucre;
            int n = hucre + 1;
            float ox = cx * chunkBoyutu, oz = cz * chunkBoyutu;
            enAlcak = float.MaxValue;
            enYuksek = float.MinValue;

            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    // Integer grid index → world coordinate, identical for both neighbours.
                    float h = Yukseklik((cx * hucre + i) * adim, (cz * hucre + j) * adim);
                    ornek[j * n + i] = h;
                    if (h < enAlcak) enAlcak = h;
                    if (h > enYuksek) enYuksek = h;
                }

            int k = 0;
            for (int j = 0; j < hucre; j++)
                for (int i = 0; i < hucre; i++)
                {
                    int gi = cx * hucre + i, gj = cz * hucre + j; // global cell index
                    var p00 = new Vector3(i * adim, ornek[j * n + i], j * adim);
                    var p10 = new Vector3((i + 1) * adim, ornek[j * n + i + 1], j * adim);
                    var p01 = new Vector3(i * adim, ornek[(j + 1) * n + i], (j + 1) * adim);
                    var p11 = new Vector3((i + 1) * adim, ornek[(j + 1) * n + i + 1], (j + 1) * adim);

                    // Alternate the diagonal in a checkerboard so no direction is favoured.
                    bool capraza = ((gi + gj) & 1) == 0;
                    float wx = ox + (i + 0.5f) * adim, wz = oz + (j + 0.5f) * adim;
                    float yama = Fbm(wx / 130f + 5.5f, wz / 130f - 3.1f, 2, 91u);
                    if (capraza)
                    {
                        Yuz(p00, p01, p11, yama, gi, gj, 0u, k, koseler, normaller, renkler); k += 3;
                        Yuz(p00, p11, p10, yama, gi, gj, 1u, k, koseler, normaller, renkler); k += 3;
                    }
                    else
                    {
                        Yuz(p00, p01, p10, yama, gi, gj, 0u, k, koseler, normaller, renkler); k += 3;
                        Yuz(p10, p01, p11, yama, gi, gj, 1u, k, koseler, normaller, renkler); k += 3;
                    }
                }
        }

        static void Yuz(Vector3 a, Vector3 b, Vector3 c, float yama, int gi, int gj, uint yuzNo, int k,
            Vector3[] koseler, Vector3[] normaller, Color32[] renkler)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            float ortYukseklik = (a.y + b.y + c.y) / 3f;
            float titresim = Rastgele01(gi, gj, 7u + yuzNo) * 2f - 1f;
            Color32 renk = Renk(ortYukseklik, normal.y, yama, titresim);
            koseler[k] = a; koseler[k + 1] = b; koseler[k + 2] = c;
            normaller[k] = normaller[k + 1] = normaller[k + 2] = normal;
            renkler[k] = renkler[k + 1] = renkler[k + 2] = renk;
        }
    }
}
