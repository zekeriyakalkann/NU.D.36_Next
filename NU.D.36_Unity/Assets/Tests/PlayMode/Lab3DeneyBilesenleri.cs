using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace NU36.Tests
{
    // Test-only components for the Lab 3 experiments. They live in the test assembly, so they
    // are never part of production code or of a delivered scene.

    /// <summary>Experiment A: moves a fixed 0.2 units per frame (frame-rate dependent).</summary>
    public class KareBasinaHareket : MonoBehaviour
    {
        void Update() => transform.position += transform.forward * 0.2f;
    }

    /// <summary>Experiment B: moves 12 units per second (frame-rate independent).</summary>
    public class ZamanaBagliHareket : MonoBehaviour
    {
        void Update() => transform.position += transform.forward * 12f * Time.deltaTime;
    }

    /// <summary>
    /// Camera experiment: drives TakipKamerasi.Takip from Update, ordered before UcusKontrol,
    /// i.e. the camera uses the aircraft pose from the previous frame.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class UpdateIleTakip : MonoBehaviour
    {
        public TakipKamerasi Takip;
        void Update() => Takip.Takip(Time.deltaTime);
    }

    /// <summary>
    /// Camera experiment: runs after every other LateUpdate (including TakipKamerasi) and records
    /// where the target appears in the camera's viewport. Also sets the next frame's time step.
    /// (WaitForEndOfFrame is not available in batch mode, so sampling happens here.)
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class KareSonuOrnekleyici : MonoBehaviour
    {
        public Camera Kamera;
        public Transform Hedef;
        public bool Degisken; // alternate 1/30 s and 1/144 s frames, else constant 1/60 s
        public readonly List<Vector2> Ornekler = new List<Vector2>();
        int frame;

        void LateUpdate()
        {
            Vector3 v = Kamera.WorldToViewportPoint(Hedef.position);
            Ornekler.Add(new Vector2(v.x, v.y));
            frame++;
            Time.captureDeltaTime = Degisken ? (frame % 2 == 0 ? 1f / 30f : 1f / 144f) : 1f / 60f;
        }
    }

    /// <summary>Receives the same Send Messages callback as UcusKontrol and records each call.</summary>
    public class GazCagriSayaci : MonoBehaviour
    {
        public readonly List<(int frame, float value)> Cagrilar = new List<(int, float)>();
        void OnGaz(InputValue v) => Cagrilar.Add((Time.frameCount, v.Get<float>()));
    }

    static class Lab3Kayit
    {
        public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Klasor
        {
            get
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab3"));
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static void Yaz(string dosya, string icerik) => File.WriteAllText(Path.Combine(Klasor, dosya), icerik);

        public static string F(float v, string format = "F4") => v.ToString(format, Inv);

        /// <summary>Renders a camera to a PNG in Logs/lab3 without touching the Game view.</summary>
        public static void Goruntu(Camera cam, string dosya, int width = 1280, int height = 720)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
                RenderPipeline.SubmitRenderRequest(cam, request);
            else
            {
                RenderTexture previous = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = previous;
            }
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(Path.Combine(Klasor, dosya), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }
    }
}
