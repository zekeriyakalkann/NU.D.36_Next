using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 4 menu background, button states and score table with REAL uGUI text, rendered in Play Mode at three resolutions
    /// (Logs/lab4_ui_review/menu_text_*.png, scoretable_text_*.png). Legibility is measured on the rendered text, not on the
    /// background brightness: the 1st and 99th percentile luminance inside each label rectangle give the contrast actually drawn.
    /// A throw-away test scene only; the menu is not part of the game (no menu scene exists and none is required yet).
    /// </summary>
    public class Lab4MenuPlayTests
    {
        static readonly Color Cream = new Color(0.93f, 0.85f, 0.65f), Ink = new Color(0.09f, 0.06f, 0.04f);

        class Label
        {
            public string text;
            public Rect rect;       // reference px, y from the top
            public Text ui;
            public bool disabled;
        }

        static Texture2D LoadPng(string file)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath, "UI", file))), file);
            return tex;
        }

        static RectTransform Child(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(w, h);
            r.anchoredPosition = new Vector2(x, 1080f - y);
            return r;
        }

        static RawImage Raw(RectTransform r, Texture tex, Rect uv)
        {
            var raw = r.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.uvRect = uv;
            raw.raycastTarget = false;
            return raw;
        }

        static Label AddLabel(RectTransform kok, string text, float x, float y, float w, float h, int size, Color color, bool disabled = false)
        {
            RectTransform r = Child(kok, text, x, y, w, h);
            var t = r.gameObject.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.color = color;
            t.raycastTarget = false;
            return new Label { text = text, rect = new Rect(x - w * 0.5f, y - h * 0.5f, w, h), ui = t, disabled = disabled };
        }

        static float Lum(Color c)
        {
            static float F(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * F(c.r) + 0.7152f * F(c.g) + 0.0722f * F(c.b);
        }

        /// <summary>Contrast drawn inside a label rectangle: (p99 + 0.05) / (p1 + 0.05) of the pixel luminance, plus the text-free background mean.</summary>
        static float Measure(Texture2D withText, Texture2D bare, Rect refRect, float s, out float bgMean)
        {
            int w = withText.width, h = withText.height;
            int x0 = Mathf.RoundToInt(w / 2f + (refRect.xMin - 960f) * s), x1 = Mathf.RoundToInt(w / 2f + (refRect.xMax - 960f) * s);
            int y0 = Mathf.RoundToInt((1080f - refRect.yMax) * s), y1 = Mathf.RoundToInt((1080f - refRect.yMin) * s);   // from the bottom
            var lums = new List<float>();
            double bg = 0;
            int n = 0;
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y += 2)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x += 2)
                {
                    lums.Add(Lum(withText.GetPixel(x, y)));
                    bg += Lum(bare.GetPixel(x, y));
                    n++;
                }
            lums.Sort();
            bgMean = n > 0 ? (float)(bg / n) : 0f;
            float lo = lums[(int)(lums.Count * 0.01f)], hi = lums[Mathf.Min(lums.Count - 1, (int)(lums.Count * 0.99f))];
            return (hi + 0.05f) / (lo + 0.05f);
        }

        static Texture2D Read(RenderTexture rt)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            return tex;
        }

        [UnityTest]
        public IEnumerator MenuAndScoreTable_WithRealText_AreLegible_AtThreeResolutions()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("no graphics device (-nographics)");
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab4_ui_review"));
            Directory.CreateDirectory(dir);
            var report = new StringBuilder();

            Scene scene = SceneManager.CreateScene("MenuTextTest");
            SceneManager.SetActiveScene(scene);
            var camGo = new GameObject("Cam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);   // stand-in for the game view behind the table
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            Texture2D bgTex = LoadPng("menu-arkaplan.png"), buttons = LoadPng("dugme-seti.png"), table = LoadPng("puan-tablosu.png");

            foreach (bool menu in new[] { true, false })
                foreach (Vector2Int r in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(2560, 1080) })
                {
                    if (!menu && r.x == 2560)
                        continue;
                    // menu: cover rule (background fills the screen); table: fit rule
                    float s = menu ? Mathf.Max(r.x / 1920f, r.y / 1080f) : Mathf.Min(r.x / 1920f, r.y / 1080f);
                    scaler.scaleFactor = s;
                    var rt = new RenderTexture(r.x, r.y, 24, RenderTextureFormat.ARGB32);
                    cam.targetTexture = rt;

                    var kokGo = new GameObject("Kok", typeof(RectTransform));
                    kokGo.transform.SetParent(canvasGo.transform, false);
                    var kok = (RectTransform)kokGo.transform;
                    kok.anchorMin = kok.anchorMax = new Vector2(0.5f, 0f);
                    kok.pivot = new Vector2(0.5f, 0f);
                    kok.sizeDelta = new Vector2(1920f, 1080f);
                    kok.anchoredPosition = Vector2.zero;
                    var labels = new List<Label>();
                    if (menu)
                    {
                        Raw(Child(kok, "Arkaplan", 960f, 540f, 1920f, 1080f), bgTex, new Rect(0f, 0f, 1f, 1f));
                        string[] names = { "BAŞLA", "SEÇENEKLER", "ÇIKIŞ", "GERİ" };
                        for (int i = 0; i < 4; i++)   // rows of dugme-seti, top to bottom: normal, hover, pressed, disabled
                            Raw(Child(kok, "Dugme" + i, 330f, 350f + i * 130f, 384f, 128f), buttons, new Rect(0f, 1f - (i + 1) * 0.25f, 1f, 0.25f));
                        for (int i = 0; i < 4; i++)
                            labels.Add(AddLabel(kok, names[i], 330f, 350f + i * 130f, 300f, 60f, 44, i == 3 ? new Color(0.62f, 0.60f, 0.52f) : Cream, i == 3));
                    }
                    else
                    {
                        Raw(Child(kok, "Tablo", 1500f, 560f, 1024f * 0.62f, 1280f * 0.62f), table, new Rect(0f, 0f, 1f, 1f));
                        Vector2 R(float tx, float ty) => new Vector2(1500f + (tx - 512f) * 0.62f, 560f + (ty - 640f) * 0.62f);
                        labels.Add(AddLabel(kok, "PUAN TABLOSU", R(512f, 150f).x, R(512f, 150f).y, 330f, 44f, 36, Cream));
                        labels.Add(AddLabel(kok, "1", R(122f, 308f).x, R(122f, 308f).y, 40f, 40f, 34, Ink));
                        labels.Add(AddLabel(kok, "Puan: 300", R(372f, 308f).x, R(372f, 308f).y, 200f, 40f, 34, Ink));
                        labels.Add(AddLabel(kok, "Süre: 1:12", R(372f, 404f).x, R(372f, 404f).y, 200f, 40f, 34, Ink));
                    }

                    foreach (Label l in labels) l.ui.enabled = false;
                    yield return null;
                    yield return null;
                    Texture2D bare = Read(rt);
                    foreach (Label l in labels) l.ui.enabled = true;
                    yield return null;
                    yield return null;
                    Texture2D withText = Read(rt);

                    string kind = menu ? "menu" : "scoretable";
                    File.WriteAllBytes(Path.Combine(dir, $"{kind}_text_{r.x}x{r.y}.png"), withText.EncodeToPNG());
                    foreach (Label l in labels)
                    {
                        float ratio = Measure(withText, bare, l.rect, s, out float bg);
                        report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}x{2} '{3}': drawn contrast {4:F1}:1 (background mean luminance {5:F3}){6}",
                            kind, r.x, r.y, l.text, ratio, bg, l.disabled ? " [disabled control, exempt]" : ""));
                        if (!l.disabled)
                            Assert.That(ratio, Is.GreaterThanOrEqualTo(4.5f), $"{kind} {r.x}x{r.y} '{l.text}' drawn contrast {ratio:F1}");
                    }

                    Object.Destroy(bare);
                    Object.Destroy(withText);
                    Object.Destroy(kokGo);
                    cam.targetTexture = null;
                    rt.Release();
                    Object.Destroy(rt);
                    yield return null;
                }
            File.WriteAllText(Path.Combine(dir, "menu_text_contrast.txt"), report.ToString(), new UTF8Encoding(false));
            Object.Destroy(bgTex);
            Object.Destroy(buttons);
            Object.Destroy(table);
        }
    }
}
