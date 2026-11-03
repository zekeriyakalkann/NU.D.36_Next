using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 6 (Hafta 6, Step 05): Bölüm 3 grey box. Bolum3_GriKutu = copy of Rota05 ("File → Save As"); Rota05 and every
    /// earlier scene stay untouched; an existing Bolum3_GriKutu is never overwritten. The map comes from
    /// <see cref="Bolum3Harita"/> at 1 square = 200 m. The Bölüm 2 level (terrain, hills, guns, dumps, level manager,
    /// base) is switched off in the copy, so only the grey box remains; the aircraft starts over the runway and a
    /// stopwatch measures runway → bridge → runway. Also exports the map as JSON for the Blender paper map.
    /// Batch mode: -executeMethod NU36.Editor.Lab6SceneBuilder.Create
    /// </summary>
    public static class Lab6SceneBuilder
    {
        public const string SceneName = "Bolum3_GriKutu";
        public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
        public const string Folder = "Assets/Bolum3";
        public const string HalkaMeshPath = Folder + "/Halka.asset";
        public static readonly string[] KapatilanKokler = { "Çakıl Burnu Arazisi", "Tepeler", "Bolum2", "Seviye", "Us" };

        // Grey box grid colours (worksheet: blue river, yellow bridge, grey hill, white searchlight, red gun, purple locator, blue runway).
        public static readonly Color ZeminRengi = new Color(0.55f, 0.57f, 0.52f);
        public static readonly Color NehirRengi = new Color(0.15f, 0.45f, 0.9f);
        public static readonly Color KopruRengi = new Color(1f, 0.85f, 0.1f);
        public static readonly Color TepeRengi = new Color(0.45f, 0.45f, 0.45f);
        public static readonly Color IsildakRengi = Color.white;
        public static readonly Color UcaksavarRengi = new Color(0.9f, 0.12f, 0.1f);
        public static readonly Color SesRengi = new Color(0.6f, 0.2f, 0.85f);
        public static readonly Color PistRengi = new Color(0.1f, 0.2f, 0.55f);

        [MenuItem("NU36/Lab 6/Create Bolum3_GriKutu Scene")]
        public static void Create()
        {
            bool ok;
            string message;
            try { message = TryCreate(); ok = true; }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab6] {message}");
            else Debug.LogError($"[NU36 Lab6] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        [MenuItem("NU36/Lab 6/Export Map JSON (for the Blender paper map)")]
        public static void HaritaJsonMenu() => Debug.Log("[NU36 Lab6] " + HaritaJsonYaz());

        static string TryCreate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new System.Exception($"{ScenePath} already exists; not overwritten.");
            int arazi = LayerMask.NameToLayer("Arazi"), yapi = LayerMask.NameToLayer("Yapi");
            if (arazi < 0 || yapi < 0)
                throw new System.Exception("Layers Arazi/Yapi missing: run NU36 → Lab 5 → Set Up Layers And Physics first.");
            var m = new Malzemeler();
            Mesh halka = HalkaMesh();

            if (!AssetDatabase.CopyAsset(Lab5SceneBuilder.ScenePath, ScenePath))
                throw new System.Exception($"Copying {Lab5SceneBuilder.ScenePath} to {ScenePath} failed.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject Kok(string ad) => scene.GetRootGameObjects().SingleOrDefault(g => g.name == ad);

            foreach (string ad in KapatilanKokler)
                if (Kok(ad) != null)
                    Kok(ad).SetActive(false);

            // ---- the grey box ----------------------------------------------------------------------
            var griKutu = new GameObject("GriKutu");
            griKutu.transform.position = Bolum3Harita.Merkez;
            Transform kok = griKutu.transform;

            GameObject zemin = GameObject.CreatePrimitive(PrimitiveType.Plane); // 10 × 10 m → scale 240 × 180 = 2400 × 1800 m
            zemin.name = "Zemin";
            zemin.transform.SetParent(kok, false);
            zemin.transform.localScale = new Vector3(240f, 1f, 180f);
            Boya(zemin, m.Zemin);
            zemin.layer = arazi;

            Transform nehir = Grup("Nehir", kok);
            foreach (string k in Bolum3Harita.Nehir)
                Kutu(PrimitiveType.Cube, $"Nehir {k}", nehir, m.Nehir, Yerel(k, 0f), new Vector3(200f, 2f, 200f));

            Transform kopru = Grup("Kopru", kok);
            foreach (string k in Bolum3Harita.Kopru)
                Kutu(PrimitiveType.Cube, $"Kopru {k}", kopru, m.Kopru, Yerel(k, Bolum3Harita.KopruYuksekligi), new Vector3(200f, 20f, 60f)).layer = arazi;

            Transform tepeler = Grup("Tepeler", kok);
            foreach (string k in Bolum3Harita.Tepeler)
                Kutu(PrimitiveType.Cube, $"Tepe {k}", tepeler, m.Tepe, Yerel(k, Bolum3Harita.TepeYuksekligi / 2f),
                    new Vector3(200f, Bolum3Harita.TepeYuksekligi, 200f)).layer = arazi;

            Transform pist = Grup("Pist", kok);
            Vector3 pistMerkez = (Yerel(Bolum3Harita.Pist.First(), 0.5f) + Yerel(Bolum3Harita.Pist.Last(), 0.5f)) / 2f;
            Kutu(PrimitiveType.Cube, "Pist " + string.Join("-", Bolum3Harita.Pist), pist, m.Pist, pistMerkez,
                new Vector3(Bolum3Harita.Pist.Length * 200f, 1f, 80f));

            Transform isiklar = Grup("Isildaklar", kok);
            for (int i = 0; i < Bolum3Harita.Isildaklar.Length; i++)
            {
                var (k, yon) = Bolum3Harita.Isildaklar[i];
                Transform g = Grup($"Isildak {i + 1} {k}", isiklar);
                g.localPosition = Yerel(k, 0f);
                GameObject govde = Kutu(PrimitiveType.Cylinder, "Govde", g, m.Isildak, new Vector3(0f, 5f, 0f), new Vector3(16f, 5f, 16f));
                govde.layer = yapi;
                // Beam: one end at the searchlight, 1200 m (6 squares) along the arrow.
                Vector3 d = Bolum3Harita.Yon(yon);
                GameObject isin = Kutu(PrimitiveType.Cube, "Isin", g, m.Isin, d * (Bolum3Harita.IsinBoyu / 2f) + new Vector3(0f, 10f, 0f),
                    new Vector3(8f, 8f, Bolum3Harita.IsinBoyu), carpisici: false);
                isin.transform.localRotation = Quaternion.LookRotation(d);
            }

            Transform toplar = Grup("Ucaksavarlar", kok);
            for (int i = 0; i < Bolum3Harita.Ucaksavarlar.Length; i++)
            {
                string k = Bolum3Harita.Ucaksavarlar[i];
                Transform g = Grup($"Ucaksavar {i + 1} {k}", toplar);
                g.localPosition = Yerel(k, 0f);
                Kutu(PrimitiveType.Cylinder, "Govde", g, m.Ucaksavar, new Vector3(0f, 4f, 0f), new Vector3(14f, 4f, 14f)).layer = yapi;
                Kutu(PrimitiveType.Cylinder, "Menzil", g, m.UcaksavarMenzili, new Vector3(0f, 0.6f, 0f),
                    new Vector3(Bolum3Harita.UcaksavarCapi, 0.1f, Bolum3Harita.UcaksavarCapi), carpisici: false);
            }

            Transform ses = Grup("SesDinleyici " + Bolum3Harita.SesDinleyici, kok);
            ses.localPosition = Yerel(Bolum3Harita.SesDinleyici, 0f);
            Kutu(PrimitiveType.Cylinder, "Govde", ses, m.Ses, new Vector3(0f, 6f, 0f), new Vector3(12f, 6f, 12f)).layer = yapi;
            var halkaGo = new GameObject("Menzil", typeof(MeshFilter), typeof(MeshRenderer));
            halkaGo.transform.SetParent(ses, false);
            halkaGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            halkaGo.transform.localScale = new Vector3(Bolum3Harita.SesCapi, 1f, Bolum3Harita.SesCapi);
            halkaGo.GetComponent<MeshFilter>().sharedMesh = halka;
            Boya(halkaGo, m.SesMenzili);

            Transform bulutlar = Grup("Bulutlar (tek kural)", kok);
            foreach (string k in Bolum3Harita.Bulutlar)
                Kutu(PrimitiveType.Cube, $"Bulut {k}", bulutlar, m.Bulut, Yerel(k, Bolum3Harita.Orta), new Vector3(200f, 60f, 200f), carpisici: false);

            // ---- aircraft, camera, light distance, HUD ------------------------------------------------
            GameObject ucak = scene.GetRootGameObjects().Single(g => g.GetComponent<UcusKontrol>() != null);
            ucak.transform.SetPositionAndRotation(Bolum3Harita.KareMerkezi(Bolum3Harita.Baslangic) + Vector3.up * Bolum3Harita.Alcak,
                Quaternion.Euler(0f, Bolum3Harita.BaslangicYonu, 0f));

            Camera cam = Kok("Main Camera").GetComponent<Camera>();
            cam.farClipPlane = 5000f;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 1800f;
            RenderSettings.fogEndDistance = 4000f;

            GameObject hud = Kok("HUD");
            var seviyeHud = hud.GetComponent<SeviyeHud>();
            Text durum = seviyeHud.DurumMetni, sure = seviyeHud.SureMetni;
            seviyeHud.enabled = false;
            var kronoGo = new GameObject("Kronometre");
            var krono = kronoGo.AddComponent<Bolum3Kronometre>();
            var so = new SerializedObject(krono);
            so.FindProperty("ucak").objectReferenceValue = ucak.transform;
            Ata(so.FindProperty("kopru"), kopru.GetComponentsInChildren<Renderer>());
            Ata(so.FindProperty("pist"), pist.GetComponentsInChildren<Renderer>());
            so.FindProperty("durumMetni").objectReferenceValue = durum;
            so.FindProperty("sureMetni").objectReferenceValue = sure;
            so.ApplyModifiedPropertiesWithoutUndo();
            durum.text = "Köprü: --   Dönüş: --   (1 tur = 15 s)";
            sure.text = "Kronometre: 0.0 s";

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.Exception($"Saving {ScenePath} failed.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            string json = HaritaJsonYaz();
            return $"Created {ScenePath} from {Lab5SceneBuilder.ScenePath}, appended it to Build Settings; {json}";
        }

        static void Ata(SerializedProperty dizi, Renderer[] r)
        {
            dizi.arraySize = r.Length;
            for (int i = 0; i < r.Length; i++)
                dizi.GetArrayElementAtIndex(i).objectReferenceValue = r[i];
        }

        /// <summary>Local position (relative to GriKutu) of a square centre at height y.</summary>
        public static Vector3 Yerel(string kare, float y) => Bolum3Harita.KareMerkezi(kare) - Bolum3Harita.Merkez + Vector3.up * y;

        // ---- JSON for the Blender map ------------------------------------------------------------------

        public static string JsonYolu => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Exports/Lab6/bolum3_harita.json"));

        public static string HaritaJsonYaz()
        {
            var c = CultureInfo.InvariantCulture;
            string Liste(string[] a) => "[" + string.Join(", ", a.Select(x => $"\"{x}\"")) + "]";
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"surum\": \"{Bolum3Harita.Surum}\",");
            sb.AppendLine($"  \"sutun\": {Bolum3Harita.Sutun}, \"satir\": {Bolum3Harita.Satir}, \"kare_m\": {Bolum3Harita.Kare.ToString(c)},");
            sb.AppendLine($"  \"pist\": {Liste(Bolum3Harita.Pist)},");
            sb.AppendLine($"  \"nehir\": {Liste(Bolum3Harita.Nehir)},");
            sb.AppendLine($"  \"kopru\": {Liste(Bolum3Harita.Kopru)},");
            sb.AppendLine($"  \"tepeler\": {Liste(Bolum3Harita.Tepeler)},");
            sb.AppendLine("  \"isildaklar\": [" + string.Join(", ", Bolum3Harita.Isildaklar.Select(i => $"{{\"kare\": \"{i.kare}\", \"yon\": {i.yon.ToString(c)}}}")) + "],");
            sb.AppendLine($"  \"ucaksavarlar\": {Liste(Bolum3Harita.Ucaksavarlar)},");
            sb.AppendLine($"  \"ses_dinleyici\": \"{Bolum3Harita.SesDinleyici}\",");
            sb.AppendLine($"  \"bulutlar\": {Liste(Bolum3Harita.Bulutlar)},");
            sb.AppendLine($"  \"baslangic\": \"{Bolum3Harita.Baslangic}\", \"baslangic_yonu\": {Bolum3Harita.BaslangicYonu.ToString(c)},");
            sb.AppendLine($"  \"yol1\": {Liste(Bolum3Harita.Yol1TepeArkasi)},");
            sb.AppendLine($"  \"yol2\": {Liste(Bolum3Harita.Yol2Acik)},");
            sb.AppendLine($"  \"isin_kare\": {(Bolum3Harita.IsinBoyu / Bolum3Harita.Kare).ToString(c)}, \"ucaksavar_yaricap_kare\": {(Bolum3Harita.UcaksavarCapi / 2f / Bolum3Harita.Kare).ToString(c)}, \"ses_yaricap_kare\": {(Bolum3Harita.SesCapi / 2f / Bolum3Harita.Kare).ToString(c)},");
            sb.AppendLine($"  \"tek_kural\": \"{Bolum3Harita.TekKural}\"");
            sb.AppendLine("}");
            Directory.CreateDirectory(Path.GetDirectoryName(JsonYolu));
            File.WriteAllText(JsonYolu, sb.ToString(), new UTF8Encoding(false));
            return "map JSON written to " + JsonYolu;
        }

        // ---- helpers ----------------------------------------------------------------------------------

        sealed class Malzemeler
        {
            public readonly Material Zemin = Opak("Zemin", ZeminRengi);
            public readonly Material Nehir = Opak("Nehir", NehirRengi);
            public readonly Material Kopru = Opak("Kopru", KopruRengi);
            public readonly Material Tepe = Opak("Tepe", TepeRengi);
            public readonly Material Isildak = Opak("Isildak", IsildakRengi);
            public readonly Material Ucaksavar = Opak("Ucaksavar", UcaksavarRengi);
            public readonly Material Ses = Opak("SesDinleyici", SesRengi);
            public readonly Material Pist = Opak("Pist", PistRengi);
            public readonly Material Isin = Saydam("Isin", new Color(1f, 0.97f, 0.75f, 0.45f));
            public readonly Material UcaksavarMenzili = Saydam("UcaksavarMenzili", new Color(0.9f, 0.1f, 0.1f, 0.18f));
            public readonly Material SesMenzili = Saydam("SesMenzili", new Color(0.6f, 0.2f, 0.85f, 0.6f));
            public readonly Material Bulut = Saydam("Bulut", new Color(1f, 1f, 1f, 0.35f));
        }

        static Material Opak(string ad, Color renk)
        {
            string yol = $"{Folder}/Materials/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m != null) return m;
            Directory.CreateDirectory($"{Folder}/Materials");
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", renk);
            m.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(m, yol);
            return m;
        }

        /// <summary>URP Unlit, transparent, alpha blended, both sides visible.</summary>
        static Material Saydam(string ad, Color renk)
        {
            string yol = $"{Folder}/Materials/{ad}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
            if (m != null) return m;
            Directory.CreateDirectory($"{Folder}/Materials");
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", renk);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, yol);
            return m;
        }

        /// <summary>Flat ring (annulus) of outer diameter 1 and a 2 % wide band, facing up; scaled to the range.</summary>
        static Mesh HalkaMesh()
        {
            var mevcut = AssetDatabase.LoadAssetAtPath<Mesh>(HalkaMeshPath);
            if (mevcut != null) return mevcut;
            const int n = 128;
            const float dis = 0.5f, ic = 0.48f;
            var v = new Vector3[n * 2];
            var t = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v[i * 2] = d * ic;
                v[i * 2 + 1] = d * dis;
                int j = (i + 1) % n;
                // clockwise seen from above = front face up
                t[i * 6] = i * 2; t[i * 6 + 1] = j * 2; t[i * 6 + 2] = i * 2 + 1;
                t[i * 6 + 3] = i * 2 + 1; t[i * 6 + 4] = j * 2; t[i * 6 + 5] = j * 2 + 1;
            }
            var mesh = new Mesh { name = "Halka", vertices = v, triangles = t };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Directory.CreateDirectory(Folder);
            AssetDatabase.CreateAsset(mesh, HalkaMeshPath);
            return mesh;
        }

        static Transform Grup(string ad, Transform ust)
        {
            var g = new GameObject(ad).transform;
            g.SetParent(ust, false);
            return g;
        }

        static void Boya(GameObject g, Material mat)
        {
            var r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = mat.renderQueue >= (int)RenderQueue.Transparent ? ShadowCastingMode.Off : ShadowCastingMode.On;
        }

        static GameObject Kutu(PrimitiveType tur, string ad, Transform ust, Material mat, Vector3 konum, Vector3 olcek, bool carpisici = true)
        {
            GameObject g = GameObject.CreatePrimitive(tur);
            g.name = ad;
            if (!carpisici)
                Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(ust, false);
            g.transform.localPosition = konum;
            g.transform.localScale = olcek;
            Boya(g, mat);
            return g;
        }
    }
}
