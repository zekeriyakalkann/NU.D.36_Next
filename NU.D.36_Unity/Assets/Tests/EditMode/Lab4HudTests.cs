using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NU36.Tests
{
    /// <summary>Lab 4 HUD: display math, the Rota04Hud scene wiring, and proof that Rota04 and the earlier scenes are untouched.</summary>
    public class Lab4HudTests
    {
        const string HudPath = "Assets/Scenes/Rota04Hud.unity";
        // MD5 of Rota04.unity before the HUD work (recorded 2026-10-20, after Lab 4 phase 2).
        const string Rota04Md5 = "af087d445491ebe41ba67b8854e84d5b";

        Scene scene;

        [TearDown]
        public void Close()
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        static string Md5(string path)
        {
            using var md5 = MD5.Create();
            return string.Concat(md5.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2")));
        }

        GameObject[] Open()
        {
            scene = EditorSceneManager.OpenScene(HudPath, OpenSceneMode.Additive);
            return scene.GetRootGameObjects();
        }

        // ---------------------------------------------------------------- display math
        [Test]
        public void DialAngle_MapsTheValueRangeToMinus135Plus135_AndClamps()
        {
            Assert.AreEqual(-135f, HudHesap.KadranAcisi(0f, 50f), 1e-4f);
            Assert.AreEqual(0f, HudHesap.KadranAcisi(25f, 50f), 1e-4f);
            Assert.AreEqual(135f, HudHesap.KadranAcisi(50f, 50f), 1e-4f);
            Assert.AreEqual(135f, HudHesap.KadranAcisi(80f, 50f), 1e-4f, "above the scale stays at the end stop");
            Assert.AreEqual(-135f, HudHesap.KadranAcisi(-5f, 50f), 1e-4f, "below zero stays at the start stop");
            Assert.AreEqual(-135f + 0.24f * 270f, HudHesap.KadranAcisi(120f, 500f), 1e-3f);
        }

        [Test]
        public void NeedleRotation_IsTheNegatedDialAngle_BecauseUnityRotatesCounterClockwise()
        {
            Assert.AreEqual(135f, HudHesap.IbreZ(0f, 50f), 1e-4f);
            Assert.AreEqual(0f, HudHesap.IbreZ(25f, 50f), 1e-4f);
            Assert.AreEqual(-135f, HudHesap.IbreZ(50f, 50f), 1e-4f);
            // Lab 3 speed range 8..42 m/s on the 0..50 dial
            Assert.AreEqual(-(-135f + 8f / 50f * 270f), HudHesap.IbreZ(8f, 50f), 1e-3f);
            Assert.AreEqual(-(-135f + 42f / 50f * 270f), HudHesap.IbreZ(42f, 50f), 1e-3f);
        }

        [Test]
        public void ThrottleKnob_SpansTheSlotAndClamps()
        {
            Assert.AreEqual(-200f, HudHesap.KolY(0f, -200f, 200f), 1e-4f);
            Assert.AreEqual(0f, HudHesap.KolY(0.5f, -200f, 200f), 1e-4f);
            Assert.AreEqual(200f, HudHesap.KolY(1f, -200f, 200f), 1e-4f);
            Assert.AreEqual(200f, HudHesap.KolY(2f, -200f, 200f), 1e-4f);
            Assert.AreEqual(-200f, HudHesap.KolY(-1f, -200f, 200f), 1e-4f);
        }

        [Test]
        public void Heading_WrapsAndCentresTheCompassWindow()
        {
            Assert.AreEqual(0f, HudHesap.Yon(0f), 1e-4f);
            Assert.AreEqual(10f, HudHesap.Yon(370f), 1e-4f);
            Assert.AreEqual(350f, HudHesap.Yon(-10f), 1e-4f);
            Assert.AreEqual(0f, HudHesap.Yon(10f, 10f), 1e-4f);
            // window 0.5 wide centred on the heading: x = h/360 - 0.25, wrapped into 0..1 (the texture repeats)
            Assert.AreEqual(0.75f, HudHesap.PusulaUvX(0f, 0.5f), 1e-4f);
            Assert.AreEqual(0f, HudHesap.PusulaUvX(90f, 0.5f), 1e-4f);
            Assert.AreEqual(0.25f, HudHesap.PusulaUvX(180f, 0.5f), 1e-4f);
            Assert.AreEqual(HudHesap.PusulaUvX(359.999f, 0.5f), HudHesap.PusulaUvX(-0.001f, 0.5f), 1e-3f);
        }

        [Test]
        public void CountdownText_MatchesTheOnGuiFormat()
        {
            Assert.AreEqual("Süre: 2:30", HudHesap.SureMetni(150f));
            Assert.AreEqual("Süre: 0:10", HudHesap.SureMetni(9.2f));
            Assert.AreEqual("Süre: 0:00", HudHesap.SureMetni(-1f));
        }

        [Test]
        public void HudText_ColoursHaveStrongContrastAgainstTheirOutline()
        {
            // WCAG relative luminance of the text fill against the near-black outline used in the scene.
            static float Lum(Color c)
            {
                float F(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
                return 0.2126f * F(c.r) + 0.7152f * F(c.g) + 0.0722f * F(c.b);
            }
            Color outline = new Color(0.02f, 0.015f, 0.01f);
            foreach (Color text in new[] { new Color(0.93f, 0.85f, 0.65f), new Color(1f, 0.3f, 0.22f) })
            {
                float a = Lum(text), b = Lum(outline);
                float ratio = (Mathf.Max(a, b) + 0.05f) / (Mathf.Min(a, b) + 0.05f);
                Assert.That(ratio, Is.GreaterThanOrEqualTo(4.5f), $"contrast {ratio:F1}");
            }
        }

        // ---------------------------------------------------------------- scene
        [Test]
        public void HudScene_IsAppendedToBuildSettings_AndRota04IsUntouched()
        {
            string[] paths = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            Assert.AreEqual(HudPath, paths[4], "Rota04Hud is the fifth scene");
            Assert.AreEqual(Rota04Md5, Md5("Assets/Scenes/Rota04.unity"), "Rota04 must stay byte-identical");
            Assert.AreEqual("fee5968598add072519e4abffc067647", Md5("Assets/Scenes/Rota02.unity"));
            Assert.AreEqual("8d210ec5e8591cda4b0f48473db09e96", Md5("Assets/Scenes/Rota03.unity"));
            Assert.AreEqual("d6526b592015a080816b5f6ef43545b9", Md5("Assets/Scenes/SampleScene.unity"));
        }

        [Test]
        public void HudScene_HasNoMissingScripts_AndKeepsTheRota04Objects()
        {
            GameObject[] roots = Open();
            var all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            Assert.AreEqual(0, all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount), "Missing scripts");

            Scene rota04 = EditorSceneManager.OpenScene("Assets/Scenes/Rota04.unity", OpenSceneMode.Additive);
            string[] expected = rota04.GetRootGameObjects().Select(g => g.name).OrderBy(n => n).ToArray();
            EditorSceneManager.CloseScene(rota04, true);
            string[] actual = roots.Select(g => g.name).Where(n => n != "HUD").OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(expected, actual, "same root objects as Rota04 plus HUD");
            Assert.AreEqual(1, roots.Count(g => g.name == "HUD"));
        }

        [Test]
        public void HudScene_RestartsItself_AndTheOnGuiStatusLineIsOff()
        {
            GameObject oyun = Open().Single(g => g.name == "Oyun");
            Assert.AreEqual("Rota04Hud", new SerializedObject(oyun.GetComponent<YenidenBasla>()).FindProperty("sahne").stringValue);
            var eso = new SerializedObject(oyun.GetComponent<GorevSonuEkrani>());
            Assert.AreEqual("Rota04Hud", eso.FindProperty("yenidenBaslatSahnesi").stringValue);
            Assert.IsFalse(eso.FindProperty("durumSatiriGoster").boolValue, "the HUD shows the mission line");
        }

        [Test]
        public void HudCanvas_UsesTheFitScaling_AndEveryComponentIsWired()
        {
            GameObject[] roots = Open();
            GameObject hud = roots.Single(g => g.name == "HUD");
            var canvas = hud.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            var scaler = hud.GetComponent<CanvasScaler>();
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, scaler.screenMatchMode, "Expand = min(w/1920, h/1080): the 16:9 frame is never stretched");

            var ucus = roots.Single(g => g.GetComponent<UcusKontrol>() != null).GetComponent<UcusKontrol>();
            var oyun = roots.Single(g => g.name == "Oyun");
            HudGosterge[] dials = hud.GetComponentsInChildren<HudGosterge>();
            Assert.AreEqual(2, dials.Length);
            CollectionAssert.AreEquivalent(new[] { HudKaynak.Hiz, HudKaynak.Irtifa }, dials.Select(d => d.Kaynak));
            foreach (HudGosterge d in dials)
            {
                Assert.AreSame(ucus, d.Ucus);
                Assert.IsNotNull(d.Ibre);
                Assert.IsTrue(d.Ibre.IsChildOf(d.transform), "needle is a separate child of its dial");
                Assert.IsNotNull(d.Ibre.GetComponent<Image>().sprite);
                Assert.IsNotNull(d.GetComponent<RawImage>().texture);
            }
            Assert.AreEqual(50f, dials.Single(d => d.Kaynak == HudKaynak.Hiz).Azami);
            Assert.AreEqual(500f, dials.Single(d => d.Kaynak == HudKaynak.Irtifa).Azami);

            var gaz = hud.GetComponentInChildren<HudGazGostergesi>();
            Assert.AreSame(ucus, gaz.Ucus);
            Assert.IsTrue(gaz.Kol.IsChildOf(gaz.transform), "knob is a separate child of the plate");
            Assert.AreEqual(-200f, gaz.AltY);
            Assert.AreEqual(200f, gaz.UstY);

            var pusula = hud.GetComponentInChildren<HudPusula>();
            Assert.AreSame(ucus, pusula.Ucus);
            Assert.IsNotNull(pusula.Serit.texture);
            Assert.AreEqual(TextureWrapMode.Repeat, pusula.Serit.texture.wrapMode);
            Assert.IsNotNull(pusula.transform.Find("Isaretci"), "the pointer is a separate element");

            var metin = hud.GetComponent<HudMetin>();
            Assert.AreSame(oyun.GetComponent<GorevYoneticisi>(), metin.Yonetici);
            Assert.AreSame(oyun.GetComponent<PuanCuzdani>(), metin.Cuzdan);
            Assert.IsNotNull(metin.GorevMetni.font);
            Assert.IsNotNull(metin.SureMetni.font);
        }
    }
}
