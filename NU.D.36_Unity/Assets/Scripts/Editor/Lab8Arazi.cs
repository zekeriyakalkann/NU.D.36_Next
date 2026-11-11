using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using H = NU36.KursunDaglariHaritasi;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8 (Bölüm 4): the mountains (plan decision 2: the shape is ours, the surfaces come from assets). A Unity Terrain
    /// of 8 × 8 km built from a height function laid around the railway:
    /// <list type="bullet">
    /// <item>steep ridged mountains (300–1200 m), a low plain in the south (station, base, safe zone) and a ring of high
    /// peaks at the map edge;</item>
    /// <item>a valley carved along the line: a flat bed under the rails and walls rising to the mountains; in Dar Vadi the
    /// walls are 330 m high and only 80 m wide (a canyon), on Son Viraj a wide shelf;</item>
    /// <item>the river gorge under the viaduct, flat pads for the tank pairs and AA guns, the airfield plateau.</item>
    /// </list>
    /// Surfaces: Poly Haven grass/rock, rocky slope, cliff and gravel (rail bed); pine trees and rocks from Quaternius.
    /// </summary>
    public static class Lab8Arazi
    {
        public const int Cozunurluk = 1025, AlfaCozunurluk = 1024;
        public const string VeriYolu = Lab8Varliklar.AraziKlasoru + "/KursunDaglari.asset";
        const float Etki = 760f;        // corridor influence radius around the line
        const float OrnekAdimi = 4f;
        /// <summary>Half width of the rail bed that nothing may rise above, and the width of its blend.</summary>
        const float RayYatagi = 10f, RayYatagiGecisi = 12f;

        public static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static float Smoother(float t) { t = Mathf.Clamp01(t); return t * t * t * (t * (t * 6f - 15f) + 10f); }

        /// <summary>Weight (0…1) of the stretch [bas, son] (fractions of the line) at s, with soft ramps.</summary>
        static float Aralik(float s, float L, float bas, float son, float rampa = 120f) =>
            Smooth(bas * L - rampa, bas * L, s) * (1f - Smooth(son * L, son * L + rampa, s));

        /// <summary>Mountain field before the railway is carved in.</summary>
        public static float Dag(float x, float z)
        {
            float n = 0f, a = 1f, f = 1f / 1500f, top = 0f;
            for (int o = 0; o < 5; o++)
            {
                float v = Mathf.PerlinNoise(x * f + 37.1f + o * 17.3f, z * f - 11.7f + o * 9.1f);
                float r = 1f - Mathf.Abs(2f * v - 1f);
                n += a * r * r;
                top += a;
                a *= 0.5f;
                f *= 2.05f;
            }
            n /= top;
            float dag = 300f + 950f * Mathf.Pow(Mathf.Clamp01(n * 1.15f), 1.25f);

            // Low plain in the south (station, base, safe zone), gently rolling.
            float ova = 128f + 45f * Mathf.PerlinNoise(x / 700f + 3.3f, z / 700f + 7.1f);
            float ovaAgirligi = Smooth(-430f, -980f, z) * Smooth(-2900f, -2400f, x) * (1f - Smooth(1500f, 2100f, x));
            float h = Mathf.Lerp(dag, ova, ovaAgirligi);

            // High ring at the map edge.
            float r0 = Mathf.Sqrt(x * x + z * z);
            h = Mathf.Lerp(h, Mathf.Max(h, 1250f + 120f * n), Smooth(3000f, 3600f, r0));
            return h;
        }

        static float DuzYariGenislik(float s, float L) =>
            Mathf.Max(8f,
                45f * (1f - Smooth(H.IstasyonBoyu + 40f, H.IstasyonBoyu + 120f, s)),
                60f * Smooth(L - H.GuvenliBolgeBoyu - 60f, L - H.GuvenliBolgeBoyu, s),
                45f * Aralik(s, L, H.Bolgeler[0].Bas, H.Bolgeler[0].Son),
                75f * Aralik(s, L, H.Bolgeler[2].Bas, H.Bolgeler[2].Son));

        static float DuvarGenisligi(float s, float L)
        {
            float w = 240f;
            w = Mathf.Lerp(w, 80f, Aralik(s, L, H.Bolgeler[0].Bas, H.Bolgeler[0].Son));
            w = Mathf.Lerp(w, 150f, Aralik(s, L, H.Bolgeler[2].Bas, H.Bolgeler[2].Son));
            return w;
        }

        public static float NehirUzakligi(float x, float z)
        {
            float best = float.MaxValue;
            var p = new Vector2(x, z);
            for (int i = 0; i < H.NehirHatti.Length - 1; i++)
            {
                Vector2 a = H.NehirHatti[i], b = H.NehirHatti[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, (a + ab * t - p).sqrMagnitude);
            }
            return Mathf.Sqrt(best);
        }

        /// <summary>Distance outside the airfield plateau (0 inside).</summary>
        public static float UsDisi(float x, float z)
        {
            Vector3 d = new Vector3(x, 0f, z) - H.UsMerkezi;
            float u = Vector3.Dot(d, H.PistEkseni), v = Vector3.Dot(d, Vector3.Cross(Vector3.up, H.PistEkseni));
            float du = Mathf.Max(0f, Mathf.Abs(u) - H.UsDuzlugu.x), dv = Mathf.Max(0f, Mathf.Abs(v) - H.UsDuzlugu.y);
            return Mathf.Sqrt(du * du + dv * dv);
        }

        /// <summary>Final ground height at (x, z), given the plan distance d to the line and the nearest s.</summary>
        public static float Yukseklik(float x, float z, float d, float s, RayEgrisi e)
        {
            float L = e.Uzunluk;
            float h = Dag(x, z);
            if (d < Etki)
            {
                float yatak = e.Nokta(s).y - 0.25f;
                float w = DuzYariGenislik(s, L), W = DuvarGenisligi(s, L);
                float kanyon = Aralik(s, L, H.Bolgeler[0].Bas, H.Bolgeler[0].Son) * Smooth(720f, 420f, d);
                float duvar = Mathf.Lerp(h, Mathf.Max(h, yatak + 330f), kanyon);
                float t = (d - w) / W;
                float kor = d <= w ? yatak : t >= 1f ? duvar : Mathf.Lerp(yatak, duvar, Smoother(t));
                h = Mathf.Lerp(kor, h, Smooth(Etki - 120f, Etki, d));
            }
            // Pads for the tank pairs and AA guns.
            foreach (H.Mevzi m in H.Mevziler)
            {
                Vector3 p = H.MevziKonumu(m, e);
                float r = m.Tur == H.MevziTuru.Tank ? 26f : 14f;
                float dm = Mathf.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z));
                h = Mathf.Lerp(p.y - 0.2f, h, Smooth(r, r + 45f, dm));
            }
            // The rail bed wins over the pads: a pad beside the line must never bury the rails (it may only end in a
            // low bank beyond the ballast shoulder).
            if (d < RayYatagi + RayYatagiGecisi)
                h = Mathf.Lerp(Mathf.Min(h, e.Nokta(s).y - 0.25f), h, Smooth(RayYatagi, RayYatagi + RayYatagiGecisi, d));
            // Airfield plateau.
            h = Mathf.Lerp(H.UsMerkezi.y, h, Smooth(0f, 160f, UsDisi(x, z)));
            // River gorge (cuts everything, so the line crosses it on the viaduct).
            float dr = NehirUzakligi(x, z);
            h = Mathf.Min(h, H.NehirTabani + Mathf.Max(0f, dr - 16f) * 1.15f);
            return Mathf.Clamp(h, 1f, H.AraziYuksekligi - 1f);
        }

        // ---------------------------------------------------------------- build

        public class Sonuc
        {
            public TerrainData Veri;
            public float[,] Yukseklikler;      // metres, [z, x]
            public float[] RayUzakligi;        // plan distance to the line per height sample (Etki+ if far)
        }

        public static Sonuc Olustur(RayEgrisi egri, TerrainLayer[] katmanlar, GameObject[] agaclar, GameObject[] kayalar)
        {
            int n = Cozunurluk;
            float hucre = H.AraziBoyutu / (n - 1);
            float x0 = H.AraziKosesi.x, z0 = H.AraziKosesi.z;

            // Nearest line sample per height sample (stamping, radius Etki).
            var dmin = new float[n * n];
            var smin = new float[n * n];
            for (int i = 0; i < dmin.Length; i++) dmin[i] = float.MaxValue;
            int r = Mathf.CeilToInt(Etki / hucre);
            for (float s = 0f; s <= egri.Uzunluk; s += OrnekAdimi)
            {
                Vector3 p = egri.Nokta(s);
                int cx = Mathf.RoundToInt((p.x - x0) / hucre), cz = Mathf.RoundToInt((p.z - z0) / hucre);
                for (int iz = Mathf.Max(0, cz - r); iz <= Mathf.Min(n - 1, cz + r); iz++)
                {
                    float dz = z0 + iz * hucre - p.z;
                    for (int ix = Mathf.Max(0, cx - r); ix <= Mathf.Min(n - 1, cx + r); ix++)
                    {
                        float dx = x0 + ix * hucre - p.x;
                        float d = dx * dx + dz * dz;
                        int k = iz * n + ix;
                        if (d < dmin[k]) { dmin[k] = d; smin[k] = s; }
                    }
                }
            }

            var yuk = new float[n, n];
            var norm = new float[n, n];
            for (int iz = 0; iz < n; iz++)
                for (int ix = 0; ix < n; ix++)
                {
                    int k = iz * n + ix;
                    float d = dmin[k] == float.MaxValue ? float.MaxValue : Mathf.Sqrt(dmin[k]);
                    dmin[k] = d;
                    float h = Yukseklik(x0 + ix * hucre, z0 + iz * hucre, d, smin[k], egri);
                    yuk[iz, ix] = h;
                    norm[iz, ix] = h / H.AraziYuksekligi;
                }

            var veri = new TerrainData { heightmapResolution = n };
            veri.size = new Vector3(H.AraziBoyutu, H.AraziYuksekligi, H.AraziBoyutu);
            veri.SetHeights(0, 0, norm);
            veri.alphamapResolution = AlfaCozunurluk;
            veri.baseMapResolution = 1024;
            veri.terrainLayers = katmanlar;
            veri.SetAlphamaps(0, 0, Alfa(yuk, dmin, smin, egri, hucre));

            var protos = new List<TreePrototype>();
            foreach (GameObject g in agaclar) protos.Add(new TreePrototype { prefab = g });
            foreach (GameObject g in kayalar) protos.Add(new TreePrototype { prefab = g });
            veri.treePrototypes = protos.ToArray();
            veri.SetTreeInstances(Agaclar(yuk, dmin, hucre, agaclar.Length, kayalar.Length).ToArray(), true);
            return new Sonuc { Veri = veri, Yukseklikler = yuk, RayUzakligi = dmin };
        }

        static float Egim(float[,] h, int ix, int iz, float hucre)
        {
            int n = h.GetLength(0);
            int a = Mathf.Max(0, ix - 1), b = Mathf.Min(n - 1, ix + 1), c = Mathf.Max(0, iz - 1), e = Mathf.Min(n - 1, iz + 1);
            float gx = (h[iz, b] - h[iz, a]) / ((b - a) * hucre), gz = (h[e, ix] - h[c, ix]) / ((e - c) * hucre);
            return Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;
        }

        static float[,,] Alfa(float[,] yuk, float[] dmin, float[] smin, RayEgrisi egri, float hucre)
        {
            int n = yuk.GetLength(0), m = AlfaCozunurluk;
            float L = egri.Uzunluk;
            var a = new float[m, m, 4];
            for (int az = 0; az < m; az++)
                for (int ax = 0; ax < m; ax++)
                {
                    int ix = Mathf.RoundToInt(ax * (n - 1) / (float)(m - 1)), iz = Mathf.RoundToInt(az * (n - 1) / (float)(m - 1));
                    float h = yuk[iz, ix], egim = Egim(yuk, ix, iz, hucre);
                    float d = dmin[iz * n + ix], s = smin[iz * n + ix];
                    float x = H.AraziKosesi.x + ix * hucre, z = H.AraziKosesi.z + iz * hucre;
                    float ucurum = Smooth(34f, 48f, egim);
                    float kaya = Mathf.Max(Smooth(20f, 32f, egim), Smooth(720f, 920f, h)) * (1f - ucurum);
                    float cim = (1f - ucurum) * (1f - kaya);
                    // Rail bed gravel where the ground carries the line (not under the viaduct).
                    float cakil = 0f;
                    if (d < 12f && Mathf.Abs(h - (egri.Nokta(s).y - 0.25f)) < 1.5f)
                        cakil = Smooth(12f, 5f, d);
                    if (d < DuzYariGenislik(s, L) && (s < H.IstasyonBoyu + 60f || s > L - H.GuvenliBolgeBoyu - 20f))
                        cakil = Mathf.Max(cakil, 0.55f);
                    cakil = Mathf.Max(cakil, 0.45f * (1f - Smooth(0f, 40f, UsDisi(x, z))));
                    float k = 1f - cakil;
                    a[az, ax, 0] = cim * k;
                    a[az, ax, 1] = kaya * k;
                    a[az, ax, 2] = ucurum * k;
                    a[az, ax, 3] = cakil;
                }
            return a;
        }

        static List<TreeInstance> Agaclar(float[,] yuk, float[] dmin, float hucre, int agacTuru, int kayaTuru)
        {
            int n = yuk.GetLength(0);
            var rnd = new System.Random(808);
            var liste = new List<TreeInstance>();
            int agac = 0, kaya = 0;
            for (int deneme = 0; deneme < 200000 && (agac < 1700 || kaya < 320); deneme++)
            {
                float x = (float)(rnd.NextDouble() * 6400.0 - 3200.0), z = (float)(rnd.NextDouble() * 6400.0 - 3200.0);
                int ix = Mathf.RoundToInt((x - H.AraziKosesi.x) / hucre), iz = Mathf.RoundToInt((z - H.AraziKosesi.z) / hucre);
                float h = yuk[iz, ix], egim = Egim(yuk, ix, iz, hucre), d = dmin[iz * n + ix];
                if (d < 26f || UsDisi(x, z) < 70f || NehirUzakligi(x, z) < 30f)
                    continue;
                bool yakin = d < 1400f;
                bool agacMi = rnd.NextDouble() < 0.82;
                if (agacMi)
                {
                    if (agac >= 1700 || egim > 30f || h < 130f || h > 980f || rnd.NextDouble() > (yakin ? 0.75 : 0.18)) continue;
                    agac++;
                }
                else
                {
                    if (kaya >= 320 || egim < 12f || egim > 42f || rnd.NextDouble() > (yakin ? 0.8 : 0.3)) continue;
                    kaya++;
                }
                float olcek = (float)(0.75 + rnd.NextDouble() * 0.6);
                liste.Add(new TreeInstance
                {
                    position = new Vector3((x - H.AraziKosesi.x) / H.AraziBoyutu, (h - 0.3f) / H.AraziYuksekligi, (z - H.AraziKosesi.z) / H.AraziBoyutu),
                    prototypeIndex = agacMi ? rnd.Next(agacTuru) : agacTuru + rnd.Next(kayaTuru),
                    widthScale = olcek,
                    heightScale = olcek * (float)(0.9 + rnd.NextDouble() * 0.25),
                    rotation = (float)(rnd.NextDouble() * Mathf.PI * 2.0),
                    color = Color.white,
                    lightmapColor = Color.white,
                });
            }
            return liste;
        }

        // ---------------------------------------------------------------- layers

        public static TerrainLayer[] Katmanlar()
        {
            Lab8Varliklar.Klasor(Lab8Varliklar.AraziKlasoru);
            return new[]
            {
                Katman("Cim", "aerial_grass_rock", 26f, new Color(0.78f, 0.92f, 0.70f)),
                Katman("Kaya", "aerial_rocks_02", 32f, new Color(0.86f, 0.88f, 0.86f)),
                Katman("Ucurum", "cliff_side", 22f, new Color(0.90f, 0.90f, 0.90f)),
                Katman("Cakil", "gravel_road", 6f, Color.white),
            };
        }

        /// <summary>One layer; <paramref name="ton"/> tints the diffuse (phase 4: the grass texture read too yellow).</summary>
        static TerrainLayer Katman(string ad, string doku, float boyut, Color ton)
        {
            string yol = $"{Lab8Varliklar.AraziKlasoru}/{ad}.terrainlayer";
            var k = AssetDatabase.LoadAssetAtPath<TerrainLayer>(yol);
            if (k == null)
            {
                k = new TerrainLayer();
                AssetDatabase.CreateAsset(k, yol);
            }
            k.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Lab8Varliklar.Dokular}/{doku}_diff_2k.jpg");
            k.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Lab8Varliklar.Dokular}/{doku}_nor_gl_2k.jpg");
            k.normalScale = 1f;
            k.tileSize = new Vector2(boyut, boyut);
            k.smoothness = 0.05f;
            k.diffuseRemapMin = Vector4.zero;
            k.diffuseRemapMax = new Vector4(ton.r, ton.g, ton.b, 1f);
            k.metallic = 0f;
            if (k.diffuseTexture == null)
                throw new System.Exception($"Texture missing for terrain layer {ad}: {doku}");
            EditorUtility.SetDirty(k);
            return k;
        }
    }
}
