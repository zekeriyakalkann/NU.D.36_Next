using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8: offscreen renders for checking the work without the Editor GUI (camera → RenderTexture → PNG in
    /// <c>Logs/lab8/gorsel/</c>). <see cref="ModelYonleri"/> shows each Bölüm 4 model from the side and from above with axis
    /// markers (+X red, +Z blue) to find where its front is.
    /// Batch mode: -executeMethod NU36.Editor.Lab8Gorsel.ModelYonleri
    /// </summary>
    public static class Lab8Gorsel
    {
        public static string Klasor => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Logs", "lab8", "gorsel"));

        public static void Kaydet(Camera cam, string ad, int en = 1600, int boy = 900)
        {
            Directory.CreateDirectory(Klasor);
            var rt = new RenderTexture(en, boy, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(en, boy, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, en, boy), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Klasor, ad + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// Views of the Bölüm 4 scene (edit time, offscreen): the map from above, the start behind the aircraft, the train,
        /// the three fire zones, the viaduct, the base and the safe zone. Batch mode: -executeMethod NU36.Editor.Lab8Gorsel.Sahne
        /// </summary>
        [MenuItem("NU36/Lab 8/Render Scene Views")]
        public static void Sahne()
        {
            var scene = EditorSceneManager.OpenScene(Lab8SceneBuilder.ScenePath, OpenSceneMode.Single);
            DynamicGI.UpdateEnvironment();
            Camera cam = Object.FindFirstObjectByType<Camera>();
            var hud = Object.FindFirstObjectByType<Canvas>();
            if (hud != null) hud.enabled = false;
            RayEgrisi e = KursunDaglariHaritasi.Egri;
            float L = e.Uzunluk;
            cam.orthographic = true;
            cam.orthographicSize = 1900f;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 4000f, -80f), Quaternion.Euler(90f, 0f, 0f));
            float uzak = cam.farClipPlane;
            cam.farClipPlane = 6000f;
            bool sis = RenderSettings.fog;
            RenderSettings.fog = false;
            Kaydet(cam, "sahne_01_ust_harita", 1600, 1600);
            RenderSettings.fog = sis;
            cam.farClipPlane = uzak;
            cam.orthographic = false;
            cam.fieldOfView = 60f;

            Transform ucak = Object.FindFirstObjectByType<UcusKontrol>().transform;
            cam.transform.SetPositionAndRotation(ucak.position - ucak.forward * 28f + Vector3.up * 8f, Quaternion.LookRotation(ucak.forward * 28f - Vector3.up * 8f + ucak.forward * 60f));
            Kaydet(cam, "sahne_02_baslangic");

            void Bak(string ad, float s, float yan, float yukseklik, float geri, float hedefIleri = 0f)
            {
                Vector3 p = e.Nokta(s), ileri = e.YatayYon(s), sag = e.Sag(s);
                Vector3 göz = p + sag * yan + Vector3.up * yukseklik - ileri * geri;
                Vector3 hedef = p + ileri * hedefIleri;
                cam.transform.SetPositionAndRotation(göz, Quaternion.LookRotation(hedef - göz));
                Kaydet(cam, ad);
            }
            float tren = KursunDaglariHaritasi.TrenBaslangici;
            Bak("sahne_03_tren", tren - 10f, 22f, 7f, 18f, 5f);
            Bak("sahne_04_istasyon_us", 60f, -260f, 140f, 240f, 0f);
            var b = KursunDaglariHaritasi.Bolgeler;
            Bak("sahne_05_dar_vadi", b[0].Bas * L + 120f, 0f, 120f, 260f, 200f);
            Bak("sahne_06_viyaduk", (b[1].Bas + b[1].Son) * 0.5f * L, 380f, 90f, 120f, 0f);
            Bak("sahne_07_son_viraj", 0.73f * L, -240f, 260f, 300f, 60f);
            Bak("sahne_08_guvenli_bolge", L - 260f, 120f, 70f, 160f, 120f);

            // Phase 2–4: close-ups of the enemies (edit mode: the zones have not hidden them yet).
            void Yakin(string ad, Component c, Vector3 fark, float hedefYukseklik)
            {
                if (c == null) return;
                Vector3 h = c.transform.position + Vector3.up * hedefYukseklik;
                Vector3 göz = h + fark;
                cam.transform.SetPositionAndRotation(göz, Quaternion.LookRotation(h - göz));
                Kaydet(cam, ad);
            }
            var tanklar = Object.FindObjectsByType<Tank>(FindObjectsSortMode.InstanceID);
            if (tanklar.Length > 0)
            {
                Transform t = tanklar[0].transform;
                Yakin("sahne_09_tank_cifti", tanklar[0], -t.forward * 26f + t.right * 22f + Vector3.up * 9f, 1.5f);
            }
            var aa = Object.FindFirstObjectByType<UcaksavarMevzii>();
            if (aa != null) Yakin("sahne_10_ucaksavar", aa, aa.transform.forward * 14f + aa.transform.right * 10f + Vector3.up * 7f, 1.5f);
            var du = Object.FindFirstObjectByType<DusmanUcagi>();
            if (du != null) Yakin("sahne_11_dusman_ucagi", du, new Vector3(14f, 6f, 18f), 0f);
            Transform lok = Object.FindFirstObjectByType<Tren>().Araclar[0].transform;
            cam.transform.SetPositionAndRotation(lok.position + lok.right * 14f + lok.forward * 10f + Vector3.up * 5f,
                Quaternion.LookRotation(lok.position - (lok.position + lok.right * 14f + lok.forward * 10f + Vector3.up * 5f)));
            Kaydet(cam, "sahne_12_lokomotif");
            Debug.Log("[NU36 Lab8] scene views in " + Klasor);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        [MenuItem("NU36/Lab 8/Render Model Axes")]
        public static void ModelYonleri()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            string[] yollar =
            {
                "Assets/Bolum4/Modeller/Tren/Locomotive_Front.fbx", "Assets/Bolum4/Modeller/Tren/Locomotive_CoalTender.fbx",
                "Assets/Bolum4/Modeller/Tren/Locomotive_Wagon.fbx", "Assets/Bolum4/Modeller/Tank/Tank.fbx",
                "Assets/Bolum4/Modeller/DusmanUcagi/DusmanUcagi.fbx", "Assets/Bolum4/Modeller/Ray/RailwayTrack_Straight.fbx",
            };
            var cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.75f, 0.8f, 0.85f);
            foreach (string yol in yollar)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(yol));
                Bounds b = Lab8Inceleme.Sinirlar(go);
                // world-space bounds
                var rs = go.GetComponentsInChildren<Renderer>();
                Bounds w = rs[0].bounds;
                foreach (var r in rs) w.Encapsulate(r.bounds);
                float boy = w.size.magnitude;
                GameObject Isaret(Color c, Vector3 p)
                {
                    var k = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    k.transform.position = p;
                    k.transform.localScale = Vector3.one * boy * 0.06f;
                    var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = c };
                    k.GetComponent<Renderer>().sharedMaterial = m;
                    return k;
                }
                var x = Isaret(Color.red, w.center + Vector3.right * (w.extents.x + boy * 0.1f));
                var z = Isaret(Color.blue, w.center + Vector3.forward * (w.extents.z + boy * 0.1f));
                string ad = Path.GetFileNameWithoutExtension(yol);
                cam.orthographic = true;
                cam.orthographicSize = boy * 0.45f;
                cam.transform.SetPositionAndRotation(w.center + Vector3.back * boy * 2f + Vector3.up * boy * 0.3f, Quaternion.Euler(8f, 0f, 0f));
                Kaydet(cam, "yon_" + ad + "_yan", 900, 600);
                cam.transform.SetPositionAndRotation(w.center + Vector3.up * boy * 2f, Quaternion.Euler(90f, 0f, 0f));
                Kaydet(cam, "yon_" + ad + "_ust", 900, 600);
                Debug.Log($"[NU36 Lab8] {ad}: world bounds centre {w.center:F2} size {w.size:F2}");
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(x);
                Object.DestroyImmediate(z);
            }
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
    }
}
