using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 4 HUD: creates Rota04Hud as a copy of Rota04 (Save As equivalent) and adds a display-only HUD Canvas built from the
    /// ten Blender UI sprites. Rota04 and every earlier scene stay untouched; an existing Rota04Hud is never overwritten.
    /// The HUD canvas uses Scale With Screen Size, 1920x1080 reference, match mode Expand (scale = min(width/1920, height/1080)),
    /// so the 16:9 frame is never stretched. Batch mode: -executeMethod NU36.Editor.Lab4HudSceneBuilder.Create
    /// </summary>
    public static class Lab4HudSceneBuilder
    {
        public const string SceneName = "Rota04Hud";
        public const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
        const string Ui = "Assets/UI/";
        const float RefW = 1920f, RefH = 1080f;
        const float DialScale = 0.58f;       // 512 px dial -> 297 px, fits the frame's 150 px socket radius
        const float PlateScale = 0.5f;
        const float KnobScale = 0.145f;      // ball of the knob sprite fits the slot of the plate

        [MenuItem("NU36/Lab 4/Create HUD Scene (Rota04Hud)")]
        public static void Create()
        {
            bool ok;
            string message;
            try { ok = TryCreate(out message); }
            catch (System.Exception e) { ok = false; message = e.ToString(); }
            if (ok) Debug.Log($"[NU36 Lab4 HUD] {message}");
            else Debug.LogError($"[NU36 Lab4 HUD] {message}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool TryCreate(out string message)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                message = $"{ScenePath} already exists; not overwritten.";
                return false;
            }
            if (!AssetDatabase.CopyAsset(Lab4SceneBuilder.ScenePath, ScenePath))
            {
                message = $"Copying {Lab4SceneBuilder.ScenePath} to {ScenePath} failed.";
                return false;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject ucakGo = roots.SingleOrDefault(g => g.GetComponent<UcusKontrol>() != null);
            GameObject oyun = roots.SingleOrDefault(g => g.name == "Oyun");
            if (ucakGo == null || oyun == null)
            {
                message = "The Rota04 copy has no aircraft or no Oyun object.";
                return false;
            }
            var ucus = ucakGo.GetComponent<UcusKontrol>();
            var yonetici = oyun.GetComponent<GorevYoneticisi>();
            var cuzdan = oyun.GetComponent<PuanCuzdani>();
            var ekran = oyun.GetComponent<GorevSonuEkrani>();
            var yenidenBasla = oyun.GetComponent<YenidenBasla>();

            // Restarts must reload this scene, not Rota04; the HUD replaces the OnGUI status line (end screens stay).
            var so = new SerializedObject(ekran);
            so.FindProperty("yenidenBaslatSahnesi").stringValue = SceneName;
            so.FindProperty("durumSatiriGoster").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            var so2 = new SerializedObject(yenidenBasla);
            so2.FindProperty("sahne").stringValue = SceneName;
            so2.ApplyModifiedPropertiesWithoutUndo();

            BuildHud(ucus, yonetici, cuzdan);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                message = $"Saving {ScenePath} failed.";
                return false;
            }
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            message = $"Created {ScenePath} from {Lab4SceneBuilder.ScenePath} with the HUD and appended it to Build Settings.";
            return true;
        }

        static void BuildHud(UcusKontrol ucus, GorevYoneticisi yonetici, PuanCuzdani cuzdan)
        {
            var hud = new GameObject("HUD", typeof(RectTransform));
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefW, RefH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            // Side panels: continue the frame's pod body past the 16:9 area on wider screens (zero width at 16:9 and narrower).
            // Solid colours sampled from the frame, no new art.
            Color olive = Sample("hud-cerceve.png", 6, 700, 26, 720);
            Color brass = Sample("hud-cerceve.png", 100, 600, 300, 606);
            const float panelH = RefH - 596f;
            SidePanel(hud.transform, "YanPanelSol", new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(-RefW / 2f, panelH), olive, brass);
            SidePanel(hud.transform, "YanPanelSag", new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(RefW / 2f, 0f), new Vector2(0f, panelH), olive, brass);

            // Frame-sized root, bottom centre, never stretched.
            RectTransform kok = Rt("Kok", hud.transform);
            kok.anchorMin = kok.anchorMax = new Vector2(0.5f, 0f);
            kok.pivot = new Vector2(0.5f, 0f);
            kok.sizeDelta = new Vector2(RefW, RefH);
            kok.anchoredPosition = Vector2.zero;

            RectTransform cerceve = Rt("Cerceve", kok);
            Stretch(cerceve);
            AddImage(cerceve.gameObject, Load("hud-cerceve.png"));

            Texture dials = Load("gosterge-kadranlari.png").texture;
            Sprite needle = Load("gosterge-ibre.png");
            float dialSize = 512f * DialScale;
            HudGosterge hiz = Dial(kok, "HizKadrani", dials, new Rect(0f, 0f, 0.5f, 1f), 200f, 830f, dialSize, needle, ucus, HudKaynak.Hiz, 50f);
            HudGosterge irtifa = Dial(kok, "IrtifaKadrani", dials, new Rect(0.5f, 0f, 0.5f, 1f), 1720f, 830f, dialSize, needle, ucus, HudKaynak.Irtifa, 500f);

            // Throttle plate with its separate knob (child). Knob x offset: slot centre (plate px 100) minus plate centre (128).
            RectTransform plate = Centered("GazPlakasi", kok, 1850f, 330f, 256f * PlateScale, 1024f * PlateScale);
            AddImage(plate.gameObject, Load("gaz-kolu-plakasi.png"));
            RectTransform knob = Rt("GazKolu", plate);
            knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.pivot = new Vector2(0.5f, 0.5f);
            knob.sizeDelta = Vector2.one * 512f * KnobScale;
            knob.anchoredPosition = new Vector2((100f - 128f) * PlateScale, -200f);
            AddImage(knob.gameObject, Load("gaz-kolu-topuz.png"));
            var gaz = plate.gameObject.AddComponent<HudGazGostergesi>();
            gaz.Ucus = ucus;
            gaz.Kol = knob;

            // Compass: a window of the Repeat ribbon plus a separate pointer.
            RectTransform pusula = Centered("Pusula", kok, 960f, 1010f, 1536f * 0.5f * 0.7f, 128f * 0.7f);
            var serit = pusula.gameObject.AddComponent<RawImage>();
            serit.texture = Load("pusula-seridi.png").texture;
            serit.uvRect = new Rect(0f, 0f, 0.5f, 1f);
            serit.raycastTarget = false;
            var hp = pusula.gameObject.AddComponent<HudPusula>();
            hp.Ucus = ucus;
            hp.Serit = serit;
            RectTransform isaretci = Rt("Isaretci", pusula);
            isaretci.anchorMin = isaretci.anchorMax = new Vector2(0.5f, 0.5f);
            isaretci.sizeDelta = new Vector2(4f, 78f);
            isaretci.anchoredPosition = Vector2.zero;
            var pi = isaretci.gameObject.AddComponent<Image>();
            pi.color = new Color(0.9f, 0.2f, 0.12f, 1f);
            pi.raycastTarget = false;

            // Mission text and countdown, top left (anchored to the canvas corner, not to the 16:9 frame).
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text gorev = NewText("GorevMetni", hud.transform, font, 34, new Vector2(34f, -26f), new Vector2(900f, 48f));
            Text sure = NewText("SureMetni", hud.transform, font, 48, new Vector2(34f, -76f), new Vector2(520f, 64f));
            var metin = hud.AddComponent<HudMetin>();
            metin.Yonetici = yonetici;
            metin.Cuzdan = cuzdan;
            metin.GorevMetni = gorev;
            metin.SureMetni = sure;
            gorev.color = sure.color = metin.NormalRenk;

            foreach (Component c in new Component[] { hiz, irtifa, gaz, hp, metin })
                EditorUtility.SetDirty(c);
        }

        static HudGosterge Dial(RectTransform kok, string name, Texture face, Rect uv, float x, float y, float size, Sprite needle,
            UcusKontrol ucus, HudKaynak kaynak, float azami)
        {
            RectTransform dial = Centered(name, kok, x, y, size, size);
            var raw = dial.gameObject.AddComponent<RawImage>();
            raw.texture = face;
            raw.uvRect = uv;
            raw.raycastTarget = false;
            RectTransform ibre = Rt("Ibre", dial);
            ibre.anchorMin = ibre.anchorMax = new Vector2(0.5f, 0.5f);
            ibre.pivot = new Vector2(0.5f, 0.5f);
            ibre.sizeDelta = new Vector2(size, size);
            ibre.anchoredPosition = Vector2.zero;
            AddImage(ibre.gameObject, needle);
            var g = dial.gameObject.AddComponent<HudGosterge>();
            g.Ucus = ucus;
            g.Kaynak = kaynak;
            g.Ibre = ibre;
            g.Azami = azami;
            return g;
        }

        static void SidePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color body, Color trim)
        {
            RectTransform r = Rt(name, parent);
            r.anchorMin = aMin;
            r.anchorMax = aMax;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = offMin;
            r.offsetMax = offMax;
            var img = r.gameObject.AddComponent<Image>();
            img.color = body;
            img.raycastTarget = false;
            RectTransform edge = Rt("Kenar", r);
            edge.anchorMin = new Vector2(0f, 1f);
            edge.anchorMax = new Vector2(1f, 1f);
            edge.pivot = new Vector2(0.5f, 1f);
            edge.sizeDelta = new Vector2(0f, 16f);
            edge.anchoredPosition = Vector2.zero;
            var e = edge.gameObject.AddComponent<Image>();
            e.color = trim;
            e.raycastTarget = false;
        }

        static Text NewText(string name, Transform parent, Font font, int size, Vector2 pos, Vector2 box)
        {
            RectTransform r = Rt(name, parent);
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = box;
            r.anchoredPosition = pos;
            var t = r.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = r.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.02f, 0.015f, 0.01f, 0.95f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        // ---------------------------------------------------------------- helpers
        static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Child centred at reference px (x from the left, y from the top) of the 1920x1080 root.</summary>
        static RectTransform Centered(string name, Transform parent, float x, float y, float w, float h)
        {
            RectTransform r = Rt(name, parent);
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(w, h);
            r.anchoredPosition = new Vector2(x, RefH - y);
            return r;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        static void AddImage(GameObject go, Sprite sprite)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        static Sprite Load(string file)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Ui + file);
            if (s == null)
                throw new System.Exception("Missing UI sprite " + Ui + file);
            return s;
        }

        /// <summary>Mean opaque colour of a region (reference px, y from the top) of a UI PNG, read from the file.</summary>
        static Color Sample(string file, int x0, int y0, int x1, int y1)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "../" + Ui + file))));
            Color sum = Color.black;
            int n = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    Color c = tex.GetPixel(x, tex.height - 1 - y);
                    if (c.a < 0.9f)
                        continue;
                    sum += c;
                    n++;
                }
            Object.DestroyImmediate(tex);
            if (n == 0)
                throw new System.Exception($"No opaque pixels in {file} at {x0},{y0}");
            sum /= n;
            sum.a = 1f;
            return sum;
        }
    }
}
