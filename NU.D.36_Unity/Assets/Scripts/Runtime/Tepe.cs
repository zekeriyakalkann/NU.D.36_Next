using UnityEngine;
using UnityEngine.Rendering;

namespace NU36
{
    /// <summary>
    /// Lab 5: a named hill (Tepe 1..3) standing on the endless terrain. It builds a flat-shaded low-poly mesh in the same
    /// colours as the terrain and a Mesh Collider, so line-of-sight rays, bombs, bullets and the ground probe all see it.
    /// The profile is a smooth bump h = H·(1 − u²)², u = r / R(θ), with an irregular outline R(θ), added ON TOP of the
    /// terrain under every vertex, so the hill follows uneven ground; a short skirt outside R goes <c>etek</c> metres below
    /// the terrain to hide small gaps between the foot vertices.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Tepe : MonoBehaviour
    {
        [Tooltip("Height of the summit above the terrain under it (m).")]
        [SerializeField, Min(1f)] float yukseklik = 230f;
        [Tooltip("Mean foot radius (m).")]
        [SerializeField, Min(10f)] float yaricap = 650f;
        [Tooltip("Outline irregularity (0 = round).")]
        [SerializeField, Range(0f, 0.4f)] float duzensizlik = 0.15f;
        [SerializeField] int tohum = 1;
        [SerializeField, Min(0f)] float etek = 12f;
        [SerializeField, Range(4, 40)] int halka = 16;
        [SerializeField, Range(8, 96)] int dilim = 56;

        Mesh mesh;

        public float Yukseklik => yukseklik;
        public float Yaricap => yaricap;

        /// <summary>Outline radius in direction <paramref name="aci"/> (radians).</summary>
        public float KenarYaricapi(float aci) =>
            yaricap * (1f + duzensizlik * (0.6f * Mathf.Sin(3f * aci + tohum * 1.7f) + 0.4f * Mathf.Sin(5f * aci + tohum * 0.9f)));

        /// <summary>Height above the terrain of the smooth profile at local (x, z); 0 outside the outline.</summary>
        public float ProfilYuksekligi(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float u = r / KenarYaricapi(Mathf.Atan2(z, x));
            if (u >= 1f)
                return 0f;
            float v = 1f - u * u;
            return yukseklik * v * v;
        }

        void Awake() => Olustur();

        void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
        }

        /// <summary>Builds the mesh and the collider. Called in Awake; public for tests.</summary>
        public void Olustur()
        {
            int n = halka + 1;          // ring 0 = summit point, ring halka = foot, plus one skirt ring
            var p = new Vector3[n + 1, dilim];
            for (int j = 0; j < dilim; j++)
            {
                float aci = j * Mathf.PI * 2f / dilim;
                float kenar = KenarYaricapi(aci);
                for (int i = 0; i <= n; i++)
                {
                    float r = i <= halka ? kenar * i / halka : kenar * 1.06f;
                    float x = Mathf.Cos(aci) * r, z = Mathf.Sin(aci) * r;
                    // Terrain mesh height under the vertex, relative to this object.
                    float zemin = AraziUretici.YuzeyYuksekligi(transform.position.x + x, transform.position.z + z) - transform.position.y;
                    float y = i < halka ? zemin + ProfilYuksekligi(x, z)
                            : i == halka ? zemin - 1.5f   // foot slightly below the ground
                            : zemin - etek;               // skirt
                    if (i > 0 && i < halka) // a little jitter so the slopes are not perfectly smooth
                        y += yukseklik * 0.025f * Mathf.Sin(i * 2.3f + j * 1.1f + tohum);
                    p[i, j] = new Vector3(x, y, z);
                }
            }

            // Rings 0..n: summit fan between rings 0 and 1, then two triangles per cell between rings i and i+1.
            int ucgen = dilim * (1 + 2 * (n - 1));
            var koseler = new Vector3[ucgen * 3];
            var normaller = new Vector3[ucgen * 3];
            var renkler = new Color32[ucgen * 3];
            var indeks = new int[ucgen * 3];
            int k = 0;
            float tabanY = transform.position.y;
            for (int j = 0; j < dilim; j++)
            {
                int j2 = (j + 1) % dilim;
                Yuz(p[0, 0], p[1, j], p[1, j2], tabanY, j, 0, koseler, normaller, renkler, ref k);
                for (int i = 1; i < n; i++)
                {
                    Yuz(p[i, j], p[i, j2], p[i + 1, j2], tabanY, j, i * 2, koseler, normaller, renkler, ref k);
                    Yuz(p[i, j], p[i + 1, j2], p[i + 1, j], tabanY, j, i * 2 + 1, koseler, normaller, renkler, ref k);
                }
            }
            for (int i = 0; i < indeks.Length; i++)
                indeks[i] = i;

            if (mesh == null)
                mesh = new Mesh { name = name + " Mesh", indexFormat = IndexFormat.UInt32 };
            mesh.Clear();
            mesh.vertices = koseler;
            mesh.normals = normaller;
            mesh.colors32 = renkler;
            mesh.SetIndices(indeks, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            var r2 = GetComponent<MeshRenderer>();
            r2.shadowCastingMode = ShadowCastingMode.Off;
            var mc = GetComponent<MeshCollider>();
            if (mc == null)
                mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }

        static void Yuz(Vector3 a, Vector3 b, Vector3 c, float tabanY, int j, int no,
            Vector3[] koseler, Vector3[] normaller, Color32[] renkler, ref int k)
        {
            // Front face = the side Cross(b - a, c - a) points to (same rule as the terrain chunks); make it face up.
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.y < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }
            normal = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
            float ort = (a.y + b.y + c.y) / 3f + tabanY;
            float titresim = Mathf.Sin(j * 12.9898f + no * 78.233f) * 0.5f;
            Color32 renk = AraziUretici.Renk(ort, normal.y, Mathf.Sin(j * 0.7f + no * 0.3f) * 0.5f, titresim);
            koseler[k] = a; koseler[k + 1] = b; koseler[k + 2] = c;
            normaller[k] = normaller[k + 1] = normaller[k + 2] = normal;
            renkler[k] = renkler[k + 1] = renkler[k + 2] = renk;
            k += 3;
        }
    }
}
