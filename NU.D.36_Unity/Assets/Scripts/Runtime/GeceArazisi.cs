using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36
{
    /// <summary>
    /// Lab 7: builds the night-map terrain from <see cref="GeceHaritasi.Yukseklik"/> as a grid of chunk meshes (smooth normals,
    /// world-mapped UVs for the baked colour map) and, while playing, a Mesh Collider per chunk on this object's layer (Arazi),
    /// so <see cref="ZeminOlcer"/>, bombs and line-of-sight rays hit the visible surface. Nothing is stored in the scene: the
    /// meshes are rebuilt on enable (also in the Editor, without colliders, so the Scene view shows the map). The forest is
    /// built the same way: one combined mesh per chunk, two colours from a small palette texture.
    /// </summary>
    [ExecuteAlways]
    public class GeceArazisi : MonoBehaviour
    {
        [SerializeField] Material araziMalzemesi;
        [SerializeField] Material agacMalzemesi;
        [Tooltip("Chunks per side.")]
        [SerializeField, Range(1, 32)] int parcaSayisi = 10;
        [Tooltip("Grid cells per chunk side (the cell is Boyut / (parcaSayisi · hucre) m).")]
        [SerializeField, Range(4, 120)] int hucre = 50;
        [SerializeField] bool agaclar = true;

        readonly List<GameObject> uretilen = new List<GameObject>();
        static List<Vector4> agacOnbellek;

        public float HucreBoyutu => GeceHaritasi.Boyut / (parcaSayisi * hucre);
        public int ParcaSayisi => parcaSayisi;
        public bool Hazir => uretilen.Count > 0;

        void OnEnable() => Olustur();
        void OnDisable() => Temizle();

        public void Olustur()
        {
            Temizle();
            var sure = System.Diagnostics.Stopwatch.StartNew();
            bool oyun = Application.isPlaying;
            float parca = GeceHaritasi.Boyut / parcaSayisi;
            int n = parcaSayisi * hucre;
            float adim = GeceHaritasi.Boyut / n;
            float[] izgara = YukseklikIzgarasi(n, adim);
            var agacListesi = agaclar ? AgacParcalari(parca) : null;
            int agacSayisi = 0;
            for (int j = 0; j < parcaSayisi; j++)
                for (int i = 0; i < parcaSayisi; i++)
                {
                    Mesh m = ParcaMesh(izgara, n, i * hucre, j * hucre, hucre, adim);
                    GameObject go = Yeni($"Arazi {i},{j}", m, araziMalzemesi, oyun);
                    go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                    if (oyun)
                        go.AddComponent<MeshCollider>().sharedMesh = m;
                    if (agacListesi != null && agacListesi[j * parcaSayisi + i].Count > 0)
                    {
                        GameObject a = Yeni($"Orman {i},{j}", AgacMesh(agacListesi[j * parcaSayisi + i]), agacMalzemesi, oyun);
                        a.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                        agacSayisi += agacListesi[j * parcaSayisi + i].Count;
                    }
                }
            if (oyun)
                Debug.Log($"[GeceArazisi] {parcaSayisi * parcaSayisi} parça ({n + 1}² köşe), {agacSayisi} ağaç: {sure.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Heights of the whole grid plus a one-cell border (for the normals), computed in parallel rows:
        /// index (j + 1) · (n + 3) + (i + 1) holds vertex (i, j).
        /// </summary>
        public static float[] YukseklikIzgarasi(int n, float adim)
        {
            int m = n + 3;
            var h = new float[m * m];
            Parallel.For(0, m, j =>
            {
                float z = -GeceHaritasi.YarimBoyut + (j - 1) * adim;
                for (int i = 0; i < m; i++)
                    h[j * m + i] = GeceHaritasi.Yukseklik(-GeceHaritasi.YarimBoyut + (i - 1) * adim, z);
            });
            return h;
        }

        GameObject Yeni(string ad, Mesh m, Material mat, bool oyun)
        {
            var go = new GameObject(ad, typeof(MeshFilter), typeof(MeshRenderer));
            if (!oyun)
                go.hideFlags = HideFlags.HideAndDontSave;
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = m;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            uretilen.Add(go);
            return go;
        }

        void Temizle()
        {
            foreach (GameObject go in uretilen)
            {
                if (go == null) continue;
                Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
                if (Application.isPlaying) { Destroy(m); Destroy(go); }
                else { DestroyImmediate(m); DestroyImmediate(go); }
            }
            uretilen.Clear();
        }

        /// <summary>
        /// Chunk mesh with its origin at the world origin (vertices in world x/z), from the shared height grid; smooth normals
        /// from central differences of the grid, so neighbouring chunks have identical edge vertices and normals (no seams).
        /// </summary>
        public static Mesh ParcaMesh(float[] izgara, int toplam, int i0, int j0, int hucre, float adim)
        {
            int n = hucre + 1, m = toplam + 3;
            float H(int i, int j) => izgara[(j + 1) * m + (i + 1)];
            var v = new Vector3[n * n];
            var nr = new Vector3[n * n];
            var uv = new Vector2[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int gi = i0 + i, gj = j0 + j;
                    float x = -GeceHaritasi.YarimBoyut + gi * adim, z = -GeceHaritasi.YarimBoyut + gj * adim;
                    int k = j * n + i;
                    v[k] = new Vector3(x, H(gi, gj), z);
                    nr[k] = new Vector3(H(gi - 1, gj) - H(gi + 1, gj), 2f * adim, H(gi, gj - 1) - H(gi, gj + 1)).normalized;
                    uv[k] = GeceHaritasi.Uv(x, z);
                }
            var t = new int[hucre * hucre * 6];
            int q = 0;
            for (int j = 0; j < hucre; j++)
                for (int i = 0; i < hucre; i++)
                {
                    int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
                    // split along the shorter diagonal of the quad: fewer folds on ridges and banks
                    if (Mathf.Abs(v[a].y - v[d].y) <= Mathf.Abs(v[b].y - v[c].y))
                    { t[q++] = a; t[q++] = c; t[q++] = d; t[q++] = a; t[q++] = d; t[q++] = b; }
                    else
                    { t[q++] = a; t[q++] = c; t[q++] = b; t[q++] = b; t[q++] = c; t[q++] = d; }
                }
            var mesh = new Mesh { name = "GeceArazisi", indexFormat = n * n > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = v;
            mesh.normals = nr;
            mesh.uv = uv;
            mesh.triangles = t;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>Trees sorted into the chunks they stand in (cached: the list is the same every time).</summary>
        List<Vector4>[] AgacParcalari(float parca)
        {
            agacOnbellek ??= GeceHaritasi.Agaclar();
            var kutular = new List<Vector4>[parcaSayisi * parcaSayisi];
            for (int i = 0; i < kutular.Length; i++)
                kutular[i] = new List<Vector4>();
            foreach (Vector4 a in agacOnbellek)
            {
                int i = Mathf.Clamp(Mathf.FloorToInt((a.x + GeceHaritasi.YarimBoyut) / parca), 0, parcaSayisi - 1);
                int j = Mathf.Clamp(Mathf.FloorToInt((a.y + GeceHaritasi.YarimBoyut) / parca), 0, parcaSayisi - 1);
                kutular[j * parcaSayisi + i].Add(a);
            }
            return kutular;
        }

        const int Kenar = 7;

        /// <summary>
        /// Low-poly conifers combined into one mesh: two stacked 7-sided cones on a short trunk. UV.x picks the palette column
        /// (0–3 foliage greens, the trunk uses the last column), so one material gives every tree its own shade.
        /// </summary>
        public static Mesh AgacMesh(List<Vector4> agaclar)
        {
            var v = new List<Vector3>(agaclar.Count * 70);
            var nr = new List<Vector3>(agaclar.Count * 70);
            var uv = new List<Vector2>(agaclar.Count * 70);
            var t = new List<int>(agaclar.Count * 90);
            foreach (Vector4 a in agaclar)
            {
                float y0 = GeceHaritasi.Yukseklik(a.x, a.y) - 0.5f;
                var taban = new Vector3(a.x, y0, a.y);
                float boy = a.z;
                float renk = (a.w + 0.5f) / 5f;
                float govde = (4f + 0.5f) / 5f;
                float donus = (a.x * 0.37f + a.y * 0.11f) % 6.283f;
                Koni(v, nr, uv, t, taban, 0.45f, boy * 0.3f, govde, donus, kapali: false);
                Koni(v, nr, uv, t, taban + Vector3.up * boy * 0.18f, boy * 0.30f, boy * 0.55f, renk, donus, kapali: true);
                Koni(v, nr, uv, t, taban + Vector3.up * boy * 0.45f, boy * 0.22f, boy * 0.55f, renk, donus + 0.4f, kapali: true);
            }
            var mesh = new Mesh { name = "GeceOrmani", indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(v);
            mesh.SetNormals(nr);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Koni(List<Vector3> v, List<Vector3> nr, List<Vector2> uv, List<int> t, Vector3 taban, float yaricap, float boy,
            float u, float donus, bool kapali)
        {
            Vector3 tepe = taban + Vector3.up * boy;
            for (int i = 0; i < Kenar; i++)
            {
                float a0 = donus + i * Mathf.PI * 2f / Kenar, a1 = donus + (i + 1) * Mathf.PI * 2f / Kenar;
                Vector3 p0 = taban + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * yaricap;
                Vector3 p1 = taban + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * yaricap;
                Vector3 n = Vector3.Cross(tepe - p0, p1 - p0).normalized;
                if (n.y < 0f) n = -n;
                int b = v.Count;
                v.Add(p0); v.Add(tepe); v.Add(p1);
                nr.Add(n); nr.Add(n); nr.Add(n);
                float k = 0.25f + 0.5f * ((i * 7) % Kenar) / Kenar;   // shade variation per face (V of the palette)
                uv.Add(new Vector2(u, k)); uv.Add(new Vector2(u, k)); uv.Add(new Vector2(u, k));
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
                if (kapali)
                {
                    int c = v.Count;
                    v.Add(p0); v.Add(p1); v.Add(taban);
                    nr.Add(Vector3.down); nr.Add(Vector3.down); nr.Add(Vector3.down);
                    uv.Add(new Vector2(u, 0.1f)); uv.Add(new Vector2(u, 0.1f)); uv.Add(new Vector2(u, 0.1f));
                    t.Add(c); t.Add(c + 1); t.Add(c + 2);
                }
            }
        }
    }
}
