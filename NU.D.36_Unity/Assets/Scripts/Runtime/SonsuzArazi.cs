using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36
{
    /// <summary>
    /// Effectively endless low-poly terrain (Lab 3 environment). A fixed pool of
    /// (2·yaricap+1)² chunk objects is kept in a square window centred on the target chunk. When
    /// the target enters another chunk, the chunks that left the window are re-assigned to the
    /// newly visible coordinates and rebuilt (a few per frame, nearest first). Terrain shape is a
    /// pure function of world position (<see cref="AraziUretici"/>), so it never changes and never
    /// depends on the flight path. No colliders: the terrain is visual only.
    /// </summary>
    [DefaultExecutionOrder(100)] // after the flight controller has moved the aircraft this frame
    public class SonsuzArazi : MonoBehaviour
    {
        [Tooltip("Followed transform (the aircraft). If empty, the Main Camera is used.")]
        [SerializeField] Transform hedef;
        [SerializeField] Material materyal;
        [Tooltip("Edge length of one chunk in metres.")]
        [SerializeField, Min(16f)] float chunkBoyutu = 256f;
        [Tooltip("Quads per chunk edge. Cell size = chunk size / this.")]
        [SerializeField, Range(4, 64)] int hucre = 16;
        [Tooltip("Window radius in chunks: (2r+1)² chunks are kept alive.")]
        [SerializeField, Range(1, 12)] int yaricap = 6;
        [Tooltip("Chunks rebuilt per frame while the window moves (the initial window is built at once).")]
        [SerializeField, Range(1, 16)] int kareBasinaInsa = 3;

        sealed class Chunk
        {
            public GameObject nesne;
            public MeshRenderer renderer;
            public Mesh mesh;
            public Vector2Int koord;
            public bool hazir;
        }

        Chunk[] havuz;
        readonly Dictionary<Vector2Int, Chunk> aktif = new Dictionary<Vector2Int, Chunk>();
        readonly List<Chunk> bosta = new List<Chunk>();
        readonly List<Vector2Int> eksik = new List<Vector2Int>();
        readonly List<Chunk> bekleyen = new List<Chunk>();
        Vector3[] koseler, normaller;
        Color32[] renkler;
        float[] ornek;
        int[] indeksler;
        Vector2Int merkez;
        bool merkezBelirli;
        bool uyariVerildi;

        public Transform Hedef { get => hedef; set => hedef = value; }
        public Material Materyal { get => materyal; set => materyal = value; }
        public float ChunkBoyutu => chunkBoyutu;
        public int Hucre => hucre;
        public int Yaricap => yaricap;
        public int HavuzBoyutu => havuz == null ? 0 : havuz.Length;
        public int HazirChunkSayisi { get; private set; }
        public int BekleyenChunkSayisi => bekleyen.Count;
        public Vector2Int MerkezChunk => merkez;

        /// <summary>Chunk index containing a world position.</summary>
        public static Vector2Int ChunkKoordu(Vector3 konum, float chunkBoyutu) =>
            new Vector2Int(Mathf.FloorToInt(konum.x / chunkBoyutu), Mathf.FloorToInt(konum.z / chunkBoyutu));

        /// <summary>True if a built chunk covers the world x/z position.</summary>
        public bool KapsiyorMu(float x, float z)
        {
            return aktif.TryGetValue(ChunkKoordu(new Vector3(x, 0f, z), chunkBoyutu), out Chunk c) && c.hazir;
        }

        void Awake()
        {
            int n = 2 * yaricap + 1;
            int kose = AraziUretici.KoseSayisi(hucre);
            koseler = new Vector3[kose];
            normaller = new Vector3[kose];
            renkler = new Color32[kose];
            ornek = new float[(hucre + 1) * (hucre + 1)];
            indeksler = new int[kose];
            for (int i = 0; i < kose; i++)
                indeksler[i] = i;

            havuz = new Chunk[n * n];
            for (int i = 0; i < havuz.Length; i++)
            {
                var go = new GameObject("Chunk");
                go.transform.SetParent(transform, false);
                go.isStatic = false;
                var mesh = new Mesh { name = "ChunkMesh", indexFormat = IndexFormat.UInt32 };
                mesh.MarkDynamic();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = materyal;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = LightProbeUsage.Off;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                r.enabled = false;
                havuz[i] = new Chunk { nesne = go, renderer = r, mesh = mesh };
                bosta.Add(havuz[i]);
            }
        }

        void Start() => Guncelle(true);

        void LateUpdate() => Guncelle(false);

        void OnDestroy()
        {
            if (havuz == null)
                return;
            foreach (Chunk c in havuz)
                if (c != null && c.mesh != null)
                    Destroy(c.mesh);
        }

        Vector3 HedefKonumu()
        {
            if (hedef == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                    return cam.transform.position;
                if (!uyariVerildi)
                {
                    uyariVerildi = true;
                    Debug.LogWarning("[SonsuzArazi] Hedef atanmamış ve Main Camera yok; arazi kendi konumunda kalıyor.", this);
                }
                return transform.position;
            }
            return hedef.position;
        }

        /// <summary>Re-centres the window on the target. <paramref name="hepsiniInsaEt"/> builds every missing chunk now.</summary>
        public void Guncelle(bool hepsiniInsaEt)
        {
            if (havuz == null)
                return;

            Vector2Int yeni = ChunkKoordu(HedefKonumu(), chunkBoyutu);
            if (!merkezBelirli || yeni != merkez)
            {
                merkez = yeni;
                merkezBelirli = true;
                Yeniden();
            }

            int butce = hepsiniInsaEt ? int.MaxValue : kareBasinaInsa;
            while (bekleyen.Count > 0 && butce-- > 0)
            {
                Chunk c = bekleyen[bekleyen.Count - 1]; // list is sorted far→near, so the end is nearest
                bekleyen.RemoveAt(bekleyen.Count - 1);
                Insa(c);
            }
        }

        void Yeniden()
        {
            // Release chunks outside the new window.
            foreach (Chunk c in havuz)
            {
                if (!aktif.TryGetValue(c.koord, out Chunk sahibi) || sahibi != c)
                    continue; // free chunk: its stale coordinate belongs to another chunk or to nobody
                if (Mathf.Abs(c.koord.x - merkez.x) > yaricap || Mathf.Abs(c.koord.y - merkez.y) > yaricap)
                {
                    aktif.Remove(c.koord);
                    if (c.hazir)
                        HazirChunkSayisi--;
                    c.hazir = false;
                    c.renderer.enabled = false;
                    bosta.Add(c);
                    bekleyen.Remove(c);
                }
            }

            // Coordinates in the window without a chunk.
            eksik.Clear();
            for (int dz = -yaricap; dz <= yaricap; dz++)
                for (int dx = -yaricap; dx <= yaricap; dx++)
                {
                    var k = new Vector2Int(merkez.x + dx, merkez.y + dz);
                    if (!aktif.ContainsKey(k))
                        eksik.Add(k);
                }

            foreach (Vector2Int k in eksik)
            {
                Chunk c = bosta[bosta.Count - 1];
                bosta.RemoveAt(bosta.Count - 1);
                c.koord = k;
                c.hazir = false;
                aktif[k] = c;
                bekleyen.Add(c);
            }

            // Farthest first, so that the nearest chunk is at the end and is built first.
            Vector2Int m = merkez;
            bekleyen.Sort((a, b) => Uzaklik(b.koord, m).CompareTo(Uzaklik(a.koord, m)));
        }

        static int Uzaklik(Vector2Int a, Vector2Int m)
        {
            int dx = a.x - m.x, dz = a.y - m.y;
            return dx * dx + dz * dz;
        }

        void Insa(Chunk c)
        {
            AraziUretici.ChunkUret(c.koord.x, c.koord.y, chunkBoyutu, hucre, koseler, normaller, renkler, ornek,
                out float alcak, out float yuksek);
            Mesh mesh = c.mesh;
            mesh.Clear();
            mesh.SetVertices(koseler);
            mesh.SetNormals(normaller);
            mesh.SetColors(renkler);
            mesh.SetIndices(indeksler, MeshTopology.Triangles, 0, false);
            var merkezLokal = new Vector3(chunkBoyutu * 0.5f, (alcak + yuksek) * 0.5f, chunkBoyutu * 0.5f);
            mesh.bounds = new Bounds(merkezLokal, new Vector3(chunkBoyutu, yuksek - alcak + 0.1f, chunkBoyutu));
            c.nesne.transform.position = new Vector3(c.koord.x * chunkBoyutu, 0f, c.koord.y * chunkBoyutu);
            c.nesne.name = $"Chunk {c.koord.x},{c.koord.y}";
            c.renderer.enabled = true;
            c.hazir = true;
            HazirChunkSayisi++;
        }
    }
}
