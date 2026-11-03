using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NU36.Tests
{
    /// <summary>Lab 4 HUD in the real Rota04Hud scene: the gauges follow the flight data, limits hold, and screenshots at four resolutions.</summary>
    public class Lab4HudPlayTests
    {
        UcusKontrol ucus;
        HudGosterge hiz, irtifa;
        HudGazGostergesi gaz;
        HudPusula pusula;
        HudMetin metin;
        GorevYoneticisi yonetici;
        PuanCuzdani cuzdan;
        Canvas canvas;

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Rota04Hud", LoadSceneMode.Single);
            yield return null;
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            HudGosterge[] dials = Object.FindObjectsByType<HudGosterge>(FindObjectsSortMode.None);
            hiz = dials.Single(d => d.Kaynak == HudKaynak.Hiz);
            irtifa = dials.Single(d => d.Kaynak == HudKaynak.Irtifa);
            gaz = Object.FindAnyObjectByType<HudGazGostergesi>();
            pusula = Object.FindAnyObjectByType<HudPusula>();
            metin = Object.FindAnyObjectByType<HudMetin>();
            yonetici = Object.FindAnyObjectByType<GorevYoneticisi>();
            cuzdan = Object.FindAnyObjectByType<PuanCuzdani>();
            canvas = metin.GetComponent<Canvas>();
            // a failure must not reload the scene while a test runs
            Object.FindAnyObjectByType<YenidenBasla>().SahneYukle = _ => { };
            Assert.IsNotNull(ucus);
        }

        static float Z(RectTransform r) => Mathf.DeltaAngle(0f, r.localEulerAngles.z);

        [UnityTest]
        public IEnumerator Gauges_FollowSpeedThrottleAltitudeAndHeading()
        {
            yield return Yukle();
            ucus.GazAyarla(1f);   // 42 m/s, full throttle
            ucus.transform.position = new Vector3(0f, 120f, 0f);
            ucus.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return null;
            yield return null;

            Assert.That(hiz.Deger, Is.EqualTo(ucus.Hiz).Within(1e-3f), "the dial reads UcusKontrol.Hiz");
            Assert.That(Z(hiz.Ibre), Is.EqualTo(HudHesap.IbreZ(ucus.Hiz, 50f)).Within(0.01f));
            Assert.That(irtifa.Deger, Is.EqualTo(ucus.transform.position.y).Within(1e-3f));
            Assert.That(Z(irtifa.Ibre), Is.EqualTo(HudHesap.IbreZ(ucus.transform.position.y, 500f)).Within(0.01f));
            Assert.That(gaz.Kol.anchoredPosition.y, Is.EqualTo(gaz.UstY).Within(0.01f), "full throttle: knob at the top end");
            Assert.That(pusula.Yon, Is.EqualTo(HudHesap.Yon(ucus.transform.eulerAngles.y)).Within(0.01f));
            Assert.That(pusula.Serit.uvRect.x, Is.EqualTo(HudHesap.PusulaUvX(pusula.Yon, pusula.Pencere)).Within(1e-4f));
            Assert.That(pusula.Serit.uvRect.width, Is.EqualTo(pusula.Pencere).Within(1e-4f));
        }

        [UnityTest]
        public IEnumerator Gauges_StopAtTheEndsOfTheirScales()
        {
            yield return Yukle();
            ucus.GazAyarla(0f);
            ucus.transform.position = new Vector3(0f, 900f, 0f);   // above the 500 m scale
            yield return null;
            yield return null;
            Assert.That(Z(irtifa.Ibre), Is.EqualTo(-135f).Within(0.01f), "needle stops at the end of the altimeter scale");
            Assert.That(gaz.Kol.anchoredPosition.y, Is.EqualTo(gaz.AltY).Within(0.01f), "zero throttle: knob at the bottom end");
        }

        [UnityTest]
        public IEnumerator Text_FollowsTheScoreMissionAndCountdown_AndHidesWhenTheMissionEnds()
        {
            yield return Yukle();
            yield return null;
            Assert.AreEqual($"Kalan hedef: {yonetici.Kalan}   Puan: 0", metin.GorevMetni.text);
            Assert.That(metin.SureMetni.text, Does.StartWith("Süre: "));
            Assert.IsTrue(metin.GorevMetni.enabled && metin.SureMetni.enabled);

            cuzdan.Ekle(100);
            yield return null;
            Assert.AreEqual($"Kalan hedef: {yonetici.Kalan}   Puan: 100", metin.GorevMetni.text);

            yonetici.SureAyarla(8f);   // inside the warning window
            yield return null;
            Assert.That(metin.SureMetni.color.r, Is.GreaterThan(0.9f));
            Assert.That(metin.SureMetni.color.g, Is.LessThan(0.4f), "warning colour at 10 s or less");

            yonetici.GoreviBasarisizYap("test");
            yield return null;
            Assert.IsFalse(metin.GorevMetni.enabled, "the failure screen takes over");
            Assert.IsFalse(metin.SureMetni.enabled);
        }

        [UnityTest]
        public IEnumerator Screenshots_AtFourResolutions_ShowTheFrameAndTheSidePanels()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("no graphics device (-nographics)");
            yield return Yukle();
            ucus.GazAyarla(0.7f);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab4_hud"));
            Directory.CreateDirectory(dir);
            Camera cam = Camera.main;
            var scaler = canvas.GetComponent<CanvasScaler>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;   // render into a texture; the equivalent of Expand is set by hand below
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            foreach (Vector2Int r in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1024, 768) })
            {
                scaler.scaleFactor = Mathf.Min(r.x / 1920f, r.y / 1080f);   // CanvasScaler Expand
                var rt = new RenderTexture(r.x, r.y, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;

                canvas.enabled = true;
                yield return null;
                yield return null;
                yield return null;
                Texture2D on = Read(rt);
                canvas.enabled = false;
                yield return null;
                yield return null;
                Texture2D off = Read(rt);
                canvas.enabled = true;

                File.WriteAllBytes(Path.Combine(dir, $"hud_{r.x}x{r.y}.png"), on.EncodeToPNG());
                Assert.AreEqual(r.x, on.width);
                Assert.AreEqual(r.y, on.height);

                // frame body: reference point (50, 900) of the 16:9 frame
                float s = Mathf.Min(r.x / 1920f, r.y / 1080f);
                int fx = Mathf.RoundToInt(r.x / 2f + (50f - 960f) * s), fy = Mathf.RoundToInt((1080f - 900f) * s);
                Assert.Greater(Diff(on.GetPixel(fx, fy), off.GetPixel(fx, fy)), 0.08f, $"frame visible at {r.x}x{r.y}");
                // dial face: reference point (200, 830) is the speed dial centre
                int dx0 = Mathf.RoundToInt(r.x / 2f + (170f - 960f) * s), dy0 = Mathf.RoundToInt((1080f - 790f) * s);
                Assert.Greater(Diff(on.GetPixel(dx0, dy0), off.GetPixel(dx0, dy0)), 0.15f, $"dial drawn at {r.x}x{r.y}");
                // left screen edge, just above the bottom: a side panel on wide screens, the frame itself otherwise
                Assert.Greater(Diff(on.GetPixel(6, 100), off.GetPixel(6, 100)), 0.08f, $"left edge covered at {r.x}x{r.y}");
                Assert.Greater(Diff(on.GetPixel(r.x - 7, 100), off.GetPixel(r.x - 7, 100)), 0.08f, $"right edge covered at {r.x}x{r.y}");
                // mission text, top left: the HUD adds pixels there
                int tx = Mathf.RoundToInt(60f * s), ty = r.y - Mathf.RoundToInt(48f * s);
                float textDiff = 0f;
                for (int dx = 0; dx < 120; dx += 3)
                    textDiff = Mathf.Max(textDiff, Diff(on.GetPixel(tx + dx, ty), off.GetPixel(tx + dx, ty)));
                Assert.Greater(textDiff, 0.15f, $"mission text drawn at {r.x}x{r.y}");

                Object.Destroy(on);
                Object.Destroy(off);
                cam.targetTexture = null;
                rt.Release();
                Object.Destroy(rt);
            }
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

        static float Diff(Color a, Color b) => (Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b)) / 3f;
    }
}
