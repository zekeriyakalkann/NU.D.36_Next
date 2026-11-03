using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 4 UI check on a real game view. Opens Rota04 (never saved), places the ten UI sprites with SpriteRenderers in front of the
    /// Main Camera using a "fit the 1920x1080 reference, anchored at the bottom" rule, and renders PNGs at several resolutions into
    /// Logs/lab4_ui_review/. This is a visual verification only: it is NOT the Phase 4 HUD (no Canvas, nothing is saved, no scene
    /// or script is changed). Layout positions and sample values are preview guesses, not requirements.
    /// Batch (needs graphics, no -nographics): -executeMethod NU36.Editor.Lab4UiPreview.Run
    /// </summary>
    public static class Lab4UiPreview
    {
        const string ScenePath = "Assets/Scenes/Rota04.unity";
        const string UiFolder = "Assets/UI/";
        const float Ppu = 100f;

        static readonly Vector2Int[] Resolutions =
        {
            new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1024, 768),
        };

        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab4_ui_review"));

        [MenuItem("NU36/Lab 4/Preview UI On Rota04")]
        public static void Run()
        {
            Directory.CreateDirectory(OutDir);
            string scenePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + ScenePath));
            string hashBefore = Md5(scenePath);
            var report = new StringBuilder();
            bool ok = true;
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Camera cam = Camera.main;
                if (cam == null)
                    throw new System.Exception("Rota04 has no Main Camera");
                RenderDiscard(cam, 640, 360);   // first render of a freshly opened scene is not representative

                foreach (Vector2Int r in Resolutions)
                {
                    GameObject root = BuildHud(cam, r.x, r.y);
                    Save(Render(cam, r.x, r.y), $"hud_{r.x}x{r.y}.png");
                    Object.DestroyImmediate(root);
                    report.AppendLine($"hud_{r.x}x{r.y}.png rendered");
                }
                // score table over the game view (1920x1080)
                GameObject t = BuildTable(cam, 1920, 1080);
                Save(Render(cam, 1920, 1080), "scoretable_1920x1080.png");
                Object.DestroyImmediate(t);
                report.AppendLine("scoretable_1920x1080.png rendered");

                // menu: background plus button states on an empty scene
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var mcam = new GameObject("MenuCamera").AddComponent<Camera>();
                mcam.clearFlags = CameraClearFlags.SolidColor;
                mcam.backgroundColor = Color.black;
                mcam.nearClipPlane = 0.05f;
                mcam.fieldOfView = 40f;
                RenderDiscard(mcam, 640, 360);
                foreach (Vector2Int r in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(2560, 1080) })
                {
                    GameObject m = BuildMenu(mcam, r.x, r.y);
                    Save(Render(mcam, r.x, r.y), $"menu_{r.x}x{r.y}.png");
                    Object.DestroyImmediate(m);
                    report.AppendLine($"menu_{r.x}x{r.y}.png rendered");
                }
            }
            catch (System.Exception e)
            {
                ok = false;
                report.AppendLine("FAILED: " + e);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);   // discard everything, nothing saved
            }
            string hashAfter = Md5(scenePath);
            bool same = hashBefore == hashAfter;
            report.AppendLine($"Rota04.unity MD5 before {hashBefore} after {hashAfter}: {(same ? "unchanged" : "CHANGED")}");
            File.WriteAllText(Path.Combine(OutDir, "unity_preview_report.txt"), report.ToString());
            Debug.Log("[NU36 Lab4 UI preview]\n" + report);
            if (Application.isBatchMode)
                EditorApplication.Exit(ok && same ? 0 : 1);
        }

        // ---------------------------------------------------------------- scene builders
        /// <summary>Root anchored at the bottom centre of the camera view, scaled to fit a 1920x1080 reference. Children use reference px, origin bottom-centre.</summary>
        static GameObject NewRoot(Camera cam, int w, int h, bool coverInsteadOfFit = false)
        {
            float d = h / (2f * Ppu * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            var root = new GameObject("UiPreviewRoot");
            root.transform.SetParent(cam.transform, false);
            root.transform.localPosition = new Vector3(0f, -h / (2f * Ppu), d);
            float fit = coverInsteadOfFit ? Mathf.Max(w / 1920f, h / 1080f) : Mathf.Min(w / 1920f, h / 1080f);
            root.transform.localScale = Vector3.one * fit;
            return root;
        }

        static GameObject BuildHud(Camera cam, int w, int h)
        {
            GameObject root = NewRoot(cam, w, h);
            Sprite frame = Load("hud-cerceve.png");
            Place(root, frame, 960, 540, 1f, 0, 0);
            Sprite dials = Load("gosterge-kadranlari.png");
            Sprite speed = Sub(dials, new Rect(0, 0, 512, 512));
            Sprite alt = Sub(dials, new Rect(512, 0, 512, 512));
            Sprite needle = Load("gosterge-ibre.png");
            const float dialScale = 0.58f;
            // sample values: 25 m/s of 0-50 and 120 m of 0-500; angle = -135 + v/max*270 clockwise, so Unity z = -angle
            Place(root, speed, 200, 830, dialScale, 0, 1);
            Place(root, needle, 200, 830, dialScale, -(-135f + 25f / 50f * 270f), 2);
            Place(root, alt, 1720, 830, dialScale, 0, 1);
            Place(root, needle, 1720, 830, dialScale, -(-135f + 120f / 500f * 270f), 2);
            // throttle: plate 256x1024 at 0.5, knob follows 912 (0 %) .. 112 (100 %) in plate px; sample 50 %
            Place(root, Load("gaz-kolu-plakasi.png"), 1850, 330, 0.5f, 0, 1);
            float plateY = 912f - 0.5f * 800f;
            Place(root, Load("gaz-kolu-topuz.png"), 1850 + (100f - 128f) * 0.5f, 330 + (plateY - 512f) * 0.5f, 0.145f, 0, 2);
            // compass: Repeat texture on a quad, window = half the ribbon, centred on a sample heading of 40 degrees
            BuildCompass(root, 960, 1010, 40f);
            // markers
            Sprite marks = Load("hedef-isaretleri.png");
            Place(root, Sub(marks, new Rect(0, 512, 512, 512)), 960, 330, 0.3f, 0, 3);
            Place(root, Sub(marks, new Rect(512, 512, 512, 512)), 1250, 260, 0.25f, 0, 3);
            Place(root, Sub(marks, new Rect(0, 0, 512, 512)), 700, 260, 0.25f, 0, 3);
            Place(root, Sub(marks, new Rect(512, 0, 512, 512)), 960, 150, 0.2f, 0, 3);
            return root;
        }

        static void BuildCompass(GameObject root, float x, float y, float heading)
        {
            Texture2D tex = Load("pusula-seridi.png").texture;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(root.transform, false);
            float scale = 0.7f;
            quad.transform.localScale = new Vector3(1536f * 0.5f * scale / Ppu, 128f * scale / Ppu, 1f);
            quad.transform.localPosition = new Vector3((x - 960f) / Ppu, (1080f - y) / Ppu, -0.02f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetTexture("_BaseMap", tex);
            mat.mainTextureScale = new Vector2(0.5f, 1f);
            mat.mainTextureOffset = new Vector2(heading / 360f - 0.25f, 0f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // the pointer is a separate element, not part of the texture
            var white = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            white.SetPixel(0, 0, new Color(0.9f, 0.2f, 0.12f));
            white.Apply();
            Sprite px = Sprite.Create(white, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            var p = new GameObject("Pointer");
            p.transform.SetParent(root.transform, false);
            p.transform.localPosition = new Vector3((x - 960f) / Ppu, (1080f - y) / Ppu, -0.05f);
            p.transform.localScale = new Vector3(0.04f, 0.78f, 1f);   // 4 x 78 reference px (the sprite has 1 px per unit)
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = px;
            sr.sortingOrder = 4;
        }

        static GameObject BuildTable(Camera cam, int w, int h)
        {
            GameObject root = NewRoot(cam, w, h);
            Place(root, Load("puan-tablosu.png"), 1500, 560, 0.62f, 0, 1);
            return root;
        }

        static GameObject BuildMenu(Camera cam, int w, int h)
        {
            GameObject root = NewRoot(cam, w, h, coverInsteadOfFit: true);
            Place(root, Load("menu-arkaplan.png"), 960, 540, 1f, 0, 0);
            Sprite sheet = Load("dugme-seti.png");
            for (int i = 0; i < 4; i++)
                Place(root, Sub(sheet, new Rect(0, 768 - 256 * i, 768, 256)), 330, 350 + i * 130, 0.5f, 0, 1);
            return root;
        }

        // ---------------------------------------------------------------- helpers
        static Sprite Load(string file)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(UiFolder + file);
            if (s == null)
                throw new System.Exception("Missing sprite " + UiFolder + file);
            return s;
        }

        static Sprite Sub(Sprite whole, Rect r) => Sprite.Create(whole.texture, r, new Vector2(0.5f, 0.5f), Ppu);

        /// <summary>Places a sprite at reference px (x from the left, y from the top) of a 1920x1080 frame.</summary>
        static void Place(GameObject root, Sprite s, float x, float y, float scale, float zDegrees, int order)
        {
            var g = new GameObject(s.name);
            g.transform.SetParent(root.transform, false);
            g.transform.localPosition = new Vector3((x - 960f) / Ppu, (1080f - y) / Ppu, -0.001f * order);
            g.transform.localRotation = Quaternion.Euler(0f, 0f, zDegrees);
            g.transform.localScale = Vector3.one * scale;
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
        }

        static Texture2D Render(Camera cam, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                RenderPipeline.SubmitRenderRequest(cam, request);
            }
            else
            {
                RenderTexture previous = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = previous;
            }
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            rt.Release();
            Object.DestroyImmediate(rt);
            return tex;
        }

        static void RenderDiscard(Camera cam, int w, int h) => Object.DestroyImmediate(Render(cam, w, h));

        static void Save(Texture2D tex, string name, bool keep = false)
        {
            File.WriteAllBytes(Path.Combine(OutDir, name), tex.EncodeToPNG());
            if (!keep)
                Object.DestroyImmediate(tex);
        }

        static string Md5(string path)
        {
            using var md5 = MD5.Create();
            using FileStream fs = File.OpenRead(path);
            return System.BitConverter.ToString(md5.ComputeHash(fs)).Replace("-", "");
        }
    }
}
