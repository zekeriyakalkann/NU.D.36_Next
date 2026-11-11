using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NU36.Tests
{
    /// <summary>
    /// Visual record (explicit, run on demand) of the main menu panels, the Rota07 night HUD with the airspeed dial and the
    /// throttle lever, and the pause menu → Logs/lab7_menu/gorsel/*.png. Overlay canvases are not drawn by a camera, so
    /// for the picture each one is moved for a moment onto a UI camera (no post-processing, transparent background, the
    /// same scale the CanvasScaler would use at that size) and laid over the game camera's image.
    /// </summary>
    [Explicit("Writes images; run with -testFilter NU36.Tests.MenuGorselKayit")]
    public class MenuGorselKayit
    {
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab7_menu/gorsel"));
        static readonly Vector2Int[] Boyutlar = { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(2560, 1080) };

        [TearDown]
        public void Bitir()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (DuraklatmaMenusu.Kalici != null)
                Object.Destroy(DuraklatmaMenusu.Kalici.gameObject);
        }

        static Texture2D Oku(RenderTexture rt, TextureFormat bicim)
        {
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, bicim, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            return tex;
        }

        static void Kaydet(Camera oyun, string ad, int w, int h)
        {
            // Game view (with its post-processing).
            var hdr = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR);
            oyun.targetTexture = hdr;
            oyun.Render();
            oyun.targetTexture = null;
            var ldr = new RenderTexture(w, h, 0);
            Graphics.Blit(hdr, ldr);
            Texture2D zemin = Oku(ldr, TextureFormat.RGB24);

            // UI: every active overlay canvas on a UI camera far from the world.
            var camGo = new GameObject("UiKamera");
            camGo.transform.position = new Vector3(0f, -20000f, 0f);
            var uiCam = camGo.AddComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            uiCam.nearClipPlane = 0.3f;
            uiCam.farClipPlane = 3f;
            var ui = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            uiCam.targetTexture = ui;

            var eski = new List<(Canvas c, CanvasScaler s, CanvasScaler.ScaleMode mod, float olcek)>();
            var tuvaller = new List<Canvas>(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None));
            tuvaller.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
            float plane = 2.5f;
            foreach (Canvas c in tuvaller)
            {
                if (!c.isRootCanvas || !c.isActiveAndEnabled || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                var s = c.GetComponent<CanvasScaler>();
                eski.Add((c, s, s != null ? s.uiScaleMode : default, s != null ? s.scaleFactor : 1f));
                if (s != null && s.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    float m = s.matchWidthOrHeight;
                    float k = Mathf.Pow(w / s.referenceResolution.x, 1f - m) * Mathf.Pow(h / s.referenceResolution.y, m);
                    s.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    s.scaleFactor = k;
                }
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = uiCam;
                c.planeDistance = plane;   // higher sorting order = nearer the camera
                plane -= 0.2f;
            }
            Canvas.ForceUpdateCanvases();
            uiCam.Render();
            Texture2D ust = Oku(ui, TextureFormat.RGBA32);
            foreach (var e in eski)
            {
                e.c.renderMode = RenderMode.ScreenSpaceOverlay;
                if (e.s != null) { e.s.uiScaleMode = e.mod; e.s.scaleFactor = e.olcek; }
            }
            Object.Destroy(camGo);

            // UI over the game: the UI shader leaves colour premultiplied by alpha and alpha squared on a clear target.
            Color[] a = zemin.GetPixels(), b = ust.GetPixels();
            for (int i = 0; i < a.Length; i++)
            {
                float al = Mathf.Sqrt(Mathf.Clamp01(b[i].a));
                a[i] = new Color(b[i].r + a[i].r * (1f - al), b[i].g + a[i].g * (1f - al), b[i].b + a[i].b * (1f - al), 1f);
            }
            zemin.SetPixels(a);
            zemin.Apply();
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(Path.Combine(Dir, $"{ad}_{w}x{h}.png"), zemin.EncodeToPNG());
            foreach (Object o in new Object[] { zemin, ust }) Object.Destroy(o);
            foreach (RenderTexture r in new[] { hdr, ldr, ui }) { r.Release(); Object.Destroy(r); }
        }

        static void HepsiniKaydet(Camera cam, string ad)
        {
            foreach (Vector2Int b in Boyutlar)
                Kaydet(cam, ad, b.x, b.y);
        }

        [UnityTest]
        public IEnumerator Menu_And_NightHud_Images()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("no graphics device (-nographics)");

            yield return SceneManager.LoadSceneAsync("AnaMenu", LoadSceneMode.Single);
            yield return null;
            var menu = Object.FindAnyObjectByType<AnaMenu>();
            menu.SahneYukle = _ => { };
            yield return null;
            Camera mcam = Camera.main;
            foreach (AnaMenu.Panel p in new[] { AnaMenu.Panel.Ana, AnaMenu.Panel.Bolumler, AnaMenu.Panel.Secenekler, AnaMenu.Panel.Kontroller })
            {
                menu.PanelAc(p);
                yield return null;
                HepsiniKaydet(mcam, "menu_" + p.ToString().ToLowerInvariant());
            }

            yield return SceneManager.LoadSceneAsync("Rota07", LoadSceneMode.Single);
            yield return null;
            Object.FindAnyObjectByType<GeceSonuEkrani>().SahneYukle = _ => { };
            var ucus = Object.FindAnyObjectByType<UcusKontrol>();
            Camera cam = Camera.main;
            // On the runway, engine idle.
            float t = 0f;
            while (t < 1.5f) { t += Time.deltaTime; yield return null; }
            HepsiniKaydet(cam, "rota07_pist");
            // In the air: 300 m over the airfield, 250 km/h, throttle 70 %.
            ucus.transform.SetPositionAndRotation(ucus.transform.position + Vector3.up * 300f + ucus.transform.forward * 600f, ucus.transform.rotation);
            ucus.HizAyarla(250f / 3.6f);
            ucus.GazAyarla(0.7f);
            Physics.SyncTransforms();
            t = 0f;
            while (t < 1.2f) { t += Time.deltaTime; yield return null; }
            HepsiniKaydet(cam, "rota07_ucus");

            DuraklatmaMenusu dm = Object.FindAnyObjectByType<DuraklatmaMenusu>();
            dm.Ac();
            yield return null;
            HepsiniKaydet(cam, "rota07_duraklatma");
            dm.Devam();
        }
    }
}
