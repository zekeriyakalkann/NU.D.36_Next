using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8 (Bölüm 4): generated assets built from the downloaded models in <c>Assets/Bolum4/Modeller</c>:
    /// materials, the train's vehicles as prefabs (locomotive + tender = 15 m, worksheet step 01), the rail model bent
    /// along the line in chunks, and the trees and rocks baked upright at their real size for the terrain.
    /// </summary>
    public static class Lab8Varliklar
    {
        public const string Kok = "Assets/Bolum4";
        public const string Modeller = Kok + "/Modeller", Dokular = Kok + "/Dokular";
        public const string Malzemeler = Kok + "/Malzemeler", Prefablar = Kok + "/Prefablar", Meshler = Kok + "/Meshler";
        public const string AraziKlasoru = Kok + "/Arazi";

        /// <summary>Rolling stock scale: locomotive (8.09) + gap (0.3) + tender (5.21) = 13.6 model units → 15.0 m.</summary>
        public const float TrenOlcegi = 1.10f;
        /// <summary>Rail scale: the model's gauge 1.26 → 1.435 m (standard gauge).</summary>
        public const float RayOlcegi = 1.139f;
        public const float AracBoslugu = 0.3f;
        const int RayParcasiPaketi = 12;

        public static readonly (string ad, string[] modeller)[] Araclar =
        {
            ("Lokomotif", new[] { "Locomotive_Front", "Locomotive_CoalTender" }),
            ("Vagon_1_Yuk", new[] { "Locomotive_Wagon" }),
            ("Vagon_2_Konteyner", new[] { "CargoTrain_Container" }),
            ("Vagon_3_Komur", new[] { "CargoTrain_CoalContainer" }),
            ("Vagon_4_Yuk", new[] { "Locomotive_Wagon" }),
        };

        // ---------------------------------------------------------------- folders, materials

        public static void Klasor(string yol)
        {
            if (AssetDatabase.IsValidFolder(yol)) return;
            string ust = System.IO.Path.GetDirectoryName(yol).Replace('\\', '/');
            Klasor(ust);
            AssetDatabase.CreateFolder(ust, System.IO.Path.GetFileName(yol));
        }

        public static Material Lit(string ad, Color renk, float parlaklik = 0.2f, float metal = 0f, Texture doku = null, Vector2? tekrar = null)
        {
            Klasor(Malzemeler);
            string yol = $"{Malzemeler}/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, yol);
            }
            m.SetColor("_BaseColor", renk);
            m.SetFloat("_Smoothness", parlaklik);
            m.SetFloat("_Metallic", metal);
            m.SetTexture("_BaseMap", doku);
            if (tekrar.HasValue) m.SetTextureScale("_BaseMap", tekrar.Value);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Unlit(string ad, Color renk, bool saydam = false)
        {
            Klasor(Malzemeler);
            string yol = $"{Malzemeler}/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, yol);
            }
            m.SetColor("_BaseColor", renk);
            if (saydam)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = (int)RenderQueue.Transparent;
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        public static GameObject Model(string klasor, string ad) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{Modeller}/{klasor}/{ad}.fbx")
            ?? throw new System.Exception($"Model missing: {Modeller}/{klasor}/{ad}.fbx");

        static void KatmanAyarla(GameObject go, int katman)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = katman;
        }

        static GameObject PrefabKaydet(GameObject go, string yol)
        {
            Klasor(System.IO.Path.GetDirectoryName(yol).Replace('\\', '/'));
            GameObject p = PrefabUtility.SaveAsPrefabAsset(go, yol);
            Object.DestroyImmediate(go);
            return p;
        }

        // ---------------------------------------------------------------- train

        /// <summary>Builds (or rebuilds) the vehicle prefabs; returns them front to back.</summary>
        public static List<GameObject> TrenPrefablari(int yapiKatmani)
        {
            var sonuc = new List<GameObject>();
            foreach (var (ad, modeller) in Araclar)
                sonuc.Add(AracPrefabi(ad, modeller, yapiKatmani));
            return sonuc;
        }

        static GameObject AracPrefabi(string ad, string[] modeller, int katman)
        {
            // Lay the parts out along the models' +X (their front), first part in front, then scale and turn +X → +Z.
            var kok = new GameObject(ad);
            var model = new GameObject("Model");
            model.transform.SetParent(kok.transform, false);
            var parcalar = new List<(GameObject go, Bounds b)>();
            float toplam = 0f;
            foreach (string m in modeller)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(Model("Tren", m));
                go.transform.SetParent(model.transform, false);
                Bounds b = Lab8Inceleme.Sinirlar(go);
                parcalar.Add((go, b));
                toplam += b.size.x;
            }
            toplam += AracBoslugu * (parcalar.Count - 1);
            float on = toplam * 0.5f;
            Bounds tum = new Bounds();
            for (int i = 0; i < parcalar.Count; i++)
            {
                var (go, b) = parcalar[i];
                float dx = on - b.max.x;
                go.transform.localPosition = new Vector3(dx, -b.min.y, -b.center.z);
                Bounds kayik = new Bounds(b.center + go.transform.localPosition, b.size);
                if (i == 0) tum = kayik; else tum.Encapsulate(kayik);
                on -= b.size.x + AracBoslugu;
            }
            TrenRenkleri(model);
            model.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);   // model +X (front) → +Z
            model.transform.localScale = Vector3.one * TrenOlcegi;

            float uzunluk = toplam * TrenOlcegi;
            var arac = kok.AddComponent<TrenAraci>();
            arac.Uzunluk = uzunluk;
            arac.BojiAraligi = uzunluk * 0.33f;
            var govde = kok.AddComponent<Rigidbody>();
            govde.isKinematic = true;
            govde.useGravity = false;
            govde.interpolation = RigidbodyInterpolation.Interpolate;
            var kutu = kok.AddComponent<BoxCollider>();
            kutu.center = new Vector3(0f, tum.center.y * TrenOlcegi, 0f);
            kutu.size = new Vector3(tum.size.z * TrenOlcegi, tum.size.y * TrenOlcegi, uzunluk);
            KatmanAyarla(kok, katman);
            return PrefabKaydet(kok, $"{Prefablar}/Tren/{ad}.prefab");
        }

        /// <summary>
        /// The Quaternius train colours are nearly black (0.03–0.16) and read as silhouettes in daylight. Phase 4: the parts
        /// keep their geometry and material slots, the colours are lifted to a period palette by material name
        /// (VARLIKLAR.md: "malzeme renkleri değiştirildi").
        /// </summary>
        static void TrenRenkleri(GameObject model)
        {
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
            {
                Material[] m = r.sharedMaterials;
                for (int i = 0; i < m.Length; i++)
                {
                    if (m[i] == null) continue;
                    string ad = m[i].name;
                    Color c = m[i].HasProperty("_BaseColor") ? m[i].GetColor("_BaseColor") : m[i].color;
                    (Color renk, float parlak, float metal) = ad switch
                    {
                        "Black" => (new Color(0.16f, 0.16f, 0.17f), 0.45f, 0.55f),
                        "Grey" => (new Color(0.42f, 0.42f, 0.40f), 0.3f, 0.4f),
                        "Base" => (new Color(0.36f, 0.16f, 0.10f), 0.2f, 0f),
                        "Container" => (new Color(0.27f, 0.36f, 0.20f), 0.2f, 0.1f),
                        _ => (new Color(Mathf.Min(1f, c.r * 3.5f + 0.06f), Mathf.Min(1f, c.g * 3.5f + 0.06f), Mathf.Min(1f, c.b * 3.5f + 0.06f)), 0.25f, 0.1f),
                    };
                    m[i] = Lit("Tren_" + ad, renk, parlak, metal);
                }
                r.sharedMaterials = m;
            }
        }

        // ---------------------------------------------------------------- rail

        /// <summary>
        /// Bends the rail model (one 18-unit straight with sleepers) along the line from <paramref name="sBas"/> to
        /// <paramref name="sSon"/>: each piece is stretched slightly so a whole number of pieces fits, each vertex goes to
        /// the curve point at its distance plus its sideways and vertical offset. Pieces are packed into chunk meshes.
        /// </summary>
        public static List<Mesh> RayMeshleri(RayEgrisi egri, float sBas, float sSon)
        {
            GameObject model = Model("Ray", "RailwayTrack_Straight");
            MeshFilter mf = model.GetComponentInChildren<MeshFilter>();
            Mesh kaynak = mf.sharedMesh;
            Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, mf.transform.localRotation, mf.transform.localScale);
            Vector3[] kv = kaynak.vertices.Select(v => m.MultiplyPoint3x4(v)).ToArray();
            Vector3[] kn = kaynak.normals.Select(n => (mf.transform.localRotation * n).normalized).ToArray();
            Vector2[] kuv = kaynak.uv;
            float xMin = kv.Min(v => v.x), xMax = kv.Max(v => v.x), yMin = kv.Min(v => v.y);
            float parcaModel = xMax - xMin;
            float hedef = parcaModel * RayOlcegi;
            int adet = Mathf.Max(1, Mathf.RoundToInt((sSon - sBas) / hedef));
            float parca = (sSon - sBas) / adet;
            float uzatma = parca / parcaModel;
            var altlar = Enumerable.Range(0, kaynak.subMeshCount).Select(kaynak.GetTriangles).ToArray();

            Klasor(Meshler);
            var sonuc = new List<Mesh>();
            for (int bas = 0, paket = 0; bas < adet; bas += RayParcasiPaketi, paket++)
            {
                int son = Mathf.Min(adet, bas + RayParcasiPaketi);
                var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>();
                var tri = altlar.Select(_ => new List<int>()).ToArray();
                for (int j = bas; j < son; j++)
                {
                    int taban = v.Count;
                    float s0 = sBas + j * parca;
                    for (int i = 0; i < kv.Length; i++)
                    {
                        float s = s0 + (kv[i].x - xMin) * uzatma;
                        Vector3 t = egri.YatayYon(s), sag = egri.Sag(s);
                        v.Add(egri.Nokta(s) + t * 0f - sag * (kv[i].z * RayOlcegi) + Vector3.up * ((kv[i].y - yMin) * RayOlcegi));
                        n.Add((t * kn[i].x + Vector3.up * kn[i].y - sag * kn[i].z).normalized);
                        if (kuv != null && kuv.Length == kv.Length) uv.Add(kuv[i]);
                    }
                    for (int k = 0; k < altlar.Length; k++)
                        foreach (int ix in altlar[k]) tri[k].Add(taban + ix);
                }
                var mesh = new Mesh { name = $"Ray_{paket:00}", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(v);
                mesh.SetNormals(n);
                if (uv.Count == v.Count) mesh.SetUVs(0, uv);
                mesh.subMeshCount = tri.Length;
                for (int k = 0; k < tri.Length; k++) mesh.SetTriangles(tri[k], k);
                mesh.RecalculateBounds();
                string yol = $"{Meshler}/Ray_{paket:00}.asset";
                AssetDatabase.DeleteAsset(yol);
                AssetDatabase.CreateAsset(mesh, yol);
                sonuc.Add(mesh);
            }
            return sonuc;
        }

        /// <summary>Rail materials, in the order of the model's sub-meshes (Steel, Wood).</summary>
        public static Material[] RayMalzemeleri() => new[]
        {
            Lit("RayCelik", new Color(0.30f, 0.29f, 0.28f), 0.55f, 0.85f),
            Lit("RayTraversi", new Color(0.30f, 0.21f, 0.14f), 0.1f),
        };

        // ---------------------------------------------------------------- nature (baked for terrain trees)

        public static readonly (string ad, float olcek)[] Agaclar = { ("PineTree_1", 5.5f), ("PineTree_2", 5f), ("PineTree_3", 5.5f) };
        public static readonly (string ad, float olcek)[] Kayalar = { ("Rock_1", 11f), ("Rock_2", 9f), ("Rock_Moss_1", 10f), ("Rock_Moss_3", 8f) };

        /// <summary>Upright, real-size copies (mesh asset + prefab with the renderer on the root, as terrain trees need).</summary>
        public static List<GameObject> DogaPrefablari((string ad, float olcek)[] liste, Material[] malzemeler)
        {
            var sonuc = new List<GameObject>();
            foreach (var (ad, olcek) in liste)
            {
                GameObject model = Model("Doga", ad);
                MeshFilter mf = model.GetComponentInChildren<MeshFilter>();
                Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, mf.transform.localRotation, mf.transform.localScale * olcek);
                Mesh k = mf.sharedMesh;
                var mesh = new Mesh { name = ad };
                Vector3[] v = k.vertices.Select(p => m.MultiplyPoint3x4(p)).ToArray();
                float taban = v.Min(p => p.y);
                mesh.vertices = v.Select(p => p - Vector3.up * taban).ToArray();
                mesh.normals = k.normals.Select(nn => (mf.transform.localRotation * nn).normalized).ToArray();
                mesh.subMeshCount = k.subMeshCount;
                for (int i = 0; i < k.subMeshCount; i++) mesh.SetTriangles(k.GetTriangles(i), i);
                mesh.RecalculateBounds();
                Klasor(Meshler);
                string myol = $"{Meshler}/{ad}.asset";
                AssetDatabase.DeleteAsset(myol);
                AssetDatabase.CreateAsset(mesh, myol);

                var go = new GameObject(ad, typeof(MeshFilter), typeof(MeshRenderer));
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                // Map the model's material names (Wood/Green/Rock) to our instancing-enabled materials.
                var adlar = model.GetComponentInChildren<MeshRenderer>().sharedMaterials.Select(x => x.name).ToArray();
                go.GetComponent<MeshRenderer>().sharedMaterials = adlar.Select(a => malzemeler.FirstOrDefault(x => x.name.EndsWith(a)) ?? malzemeler[0]).ToArray();
                sonuc.Add(PrefabKaydet(go, $"{Prefablar}/Doga/{ad}.prefab"));
            }
            return sonuc;
        }

        public static Material[] DogaMalzemeleri() => new[]
        {
            Lit("Doga_Wood", new Color(0.24f, 0.15f, 0.10f), 0.05f),
            Lit("Doga_Green", new Color(0.16f, 0.25f, 0.12f), 0.05f),
            Lit("Doga_Rock", new Color(0.42f, 0.42f, 0.44f), 0.1f),
        };
    }
}
