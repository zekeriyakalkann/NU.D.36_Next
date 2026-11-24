using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using H = NU36.KartalGecidiHaritasi;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 9 (Bölüm 5): the terrain of Kartal Geçidi — a Unity Terrain of 8 × 8 km built from
    /// <see cref="KartalGecidiHaritasi.Yukseklik"/> (start valley, ridge, the pass, coastal plain, beach, bay floor).
    /// Surfaces: Bölüm 4's grass/rock, rocky slope, cliff and gravel layers plus Poly Haven sand for the beach and the
    /// shallow sea floor; pines and rocks are Bölüm 4's terrain-tree prefabs (Quaternius, CC0).
    /// </summary>
    public static class Lab9Arazi
    {
        public const int Cozunurluk = 1025, AlfaCozunurluk = 1024;
        public const string VeriYolu = Lab9Varliklar.AraziKlasoru + "/KartalGecidi.asset";

        public sealed class Sonuc
        {
            public TerrainData Veri;
            public float[,] Yukseklikler;   // metres, [z, x]
            public int Agac, Kaya;
        }

        public static TerrainLayer[] Katmanlar()
        {
            string k = Lab8Varliklar.AraziKlasoru;
            TerrainLayer Yukle(string ad) => AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{k}/{ad}.terrainlayer")
                                             ?? throw new System.Exception($"Bölüm 4 terrain layer missing: {k}/{ad}.terrainlayer");
            return new[] { Yukle("Cim"), Yukle("Kaya"), Yukle("Ucurum"), Yukle("Cakil"), Lab9Varliklar.KumKatmani() };
        }

        static float Egim(float[,] h, int ix, int iz, float hucre)
        {
            int n = h.GetLength(0);
            int a = Mathf.Max(0, ix - 1), b = Mathf.Min(n - 1, ix + 1), c = Mathf.Max(0, iz - 1), e = Mathf.Min(n - 1, iz + 1);
            float gx = (h[iz, b] - h[iz, a]) / ((b - a) * hucre), gz = (h[e, ix] - h[c, ix]) / ((e - c) * hucre);
            return Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;
        }

        public static Sonuc Olustur(TerrainLayer[] katmanlar, GameObject[] agaclar, GameObject[] kayalar)
        {
            int n = Cozunurluk;
            float hucre = H.AraziBoyutu / (n - 1);
            float x0 = H.AraziKosesi.x, z0 = H.AraziKosesi.z, y0 = H.AraziKosesi.y;
            var yuk = new float[n, n];
            var norm = new float[n, n];
            for (int iz = 0; iz < n; iz++)
                for (int ix = 0; ix < n; ix++)
                {
                    float h = H.Yukseklik(x0 + ix * hucre, z0 + iz * hucre);
                    yuk[iz, ix] = h;
                    norm[iz, ix] = (h - y0) / H.AraziYuksekligi;
                }

            var veri = new TerrainData { heightmapResolution = n };
            veri.size = new Vector3(H.AraziBoyutu, H.AraziYuksekligi, H.AraziBoyutu);
            veri.SetHeights(0, 0, norm);
            veri.alphamapResolution = AlfaCozunurluk;
            veri.baseMapResolution = 1024;
            veri.terrainLayers = katmanlar;
            veri.SetAlphamaps(0, 0, Alfa(yuk, hucre));

            var protos = new List<TreePrototype>();
            foreach (GameObject g in agaclar) protos.Add(new TreePrototype { prefab = g });
            foreach (GameObject g in kayalar) protos.Add(new TreePrototype { prefab = g });
            veri.treePrototypes = protos.ToArray();
            var sonuc = new Sonuc { Veri = veri, Yukseklikler = yuk };
            veri.SetTreeInstances(Agaclar(yuk, hucre, agaclar.Length, kayalar.Length, sonuc).ToArray(), true);
            return sonuc;
        }

        /// <summary>Layers: 0 grass, 1 rock, 2 cliff, 3 gravel (pass floor), 4 sand (beach and sea floor).</summary>
        static float[,,] Alfa(float[,] yuk, float hucre)
        {
            int n = yuk.GetLength(0), m = AlfaCozunurluk;
            var a = new float[m, m, 5];
            for (int az = 0; az < m; az++)
                for (int ax = 0; ax < m; ax++)
                {
                    int ix = Mathf.RoundToInt(ax * (n - 1) / (float)(m - 1)), iz = Mathf.RoundToInt(az * (n - 1) / (float)(m - 1));
                    float h = yuk[iz, ix], egim = Egim(yuk, ix, iz, hucre);
                    float x = H.AraziKosesi.x + ix * hucre, z = H.AraziKosesi.z + iz * hucre;
                    float ds = H.DenizUzakligi(x, z);
                    float ucurum = H.Smooth(34f, 48f, egim);
                    float kaya = Mathf.Max(H.Smooth(20f, 32f, egim), H.Smooth(750f, 950f, h)) * (1f - ucurum);
                    float cim = (1f - ucurum) * (1f - kaya);
                    // Sand: the beach, the sea floor and a soft edge into the plain.
                    float kum = Mathf.Max(H.Smooth(-H.KumsalGenisligi - 40f, -H.KumsalGenisligi + 10f, ds), h < 1.5f ? 1f : 0f) * (1f - H.Smooth(18f, 30f, egim));
                    // Gravel: the pass floor (a mule track of loose stones).
                    H.GecitUzakligi(x, z, out float d);
                    float cakil = ds < -150f ? 0.6f * H.Smooth(H.GecitTabanYariGenisligi, H.GecitTabanYariGenisligi * 0.35f, d) * (1f - ucurum) : 0f;
                    float k = (1f - kum) * (1f - cakil);
                    a[az, ax, 0] = cim * k;
                    a[az, ax, 1] = kaya * k;
                    a[az, ax, 2] = ucurum * k;
                    a[az, ax, 3] = cakil * (1f - kum);
                    a[az, ax, 4] = kum;
                }
            return a;
        }

        /// <summary>True on the paths of the landing (beach, the tanks' parking rows): no trees there.</summary>
        public static bool IndirmeAlani(float x, float z)
        {
            float ds = H.DenizUzakligi(x, z);
            return ds > -320f && Mathf.Abs(z) < 650f;
        }

        static List<TreeInstance> Agaclar(float[,] yuk, float hucre, int agacTuru, int kayaTuru, Sonuc sonuc)
        {
            var rnd = new System.Random(909);
            var liste = new List<TreeInstance>();
            int agac = 0, kaya = 0;
            const int enCokAgac = 2200, enCokKaya = 380;
            for (int deneme = 0; deneme < 250000 && (agac < enCokAgac || kaya < enCokKaya); deneme++)
            {
                float x = (float)(rnd.NextDouble() * 7000.0 - 3500.0), z = (float)(rnd.NextDouble() * 7000.0 - 3500.0);
                int ix = Mathf.RoundToInt((x - H.AraziKosesi.x) / hucre), iz = Mathf.RoundToInt((z - H.AraziKosesi.z) / hucre);
                float h = yuk[iz, ix], egim = Egim(yuk, ix, iz, hucre);
                float ds = H.DenizUzakligi(x, z);
                if (ds > -H.KumsalGenisligi - 40f || IndirmeAlani(x, z)) continue;
                H.GecitUzakligi(x, z, out float d);
                if (d < H.GecitTabanYariGenisligi + 15f) continue;   // the pass floor stays open
                bool yakin = Mathf.Abs(z) < 2200f && x > -2800f;
                bool agacMi = rnd.NextDouble() < 0.84;
                if (agacMi)
                {
                    if (agac >= enCokAgac || egim > 32f || h < 6f || h > 1000f || rnd.NextDouble() > (yakin ? 0.8 : 0.2)) continue;
                    agac++;
                }
                else
                {
                    if (kaya >= enCokKaya || egim < 12f || egim > 44f || rnd.NextDouble() > (yakin ? 0.8 : 0.3)) continue;
                    kaya++;
                }
                float olcek = (float)(0.75 + rnd.NextDouble() * 0.6);
                liste.Add(new TreeInstance
                {
                    position = new Vector3((x - H.AraziKosesi.x) / H.AraziBoyutu, (h - 0.3f - H.AraziKosesi.y) / H.AraziYuksekligi, (z - H.AraziKosesi.z) / H.AraziBoyutu),
                    prototypeIndex = agacMi ? rnd.Next(agacTuru) : agacTuru + rnd.Next(kayaTuru),
                    widthScale = olcek,
                    heightScale = olcek * (float)(0.9 + rnd.NextDouble() * 0.25),
                    rotation = (float)(rnd.NextDouble() * Mathf.PI * 2.0),
                    color = Color.white,
                    lightmapColor = Color.white,
                });
            }
            sonuc.Agac = agac;
            sonuc.Kaya = kaya;
            return liste;
        }
    }
}
