using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 3 experiments in Play Mode: movement with and without deltaTime at different frame
    /// rates (real targetFrameRate and a controlled Time.captureDeltaTime simulation), and the
    /// follow camera in Update vs LateUpdate. Frame-rate and VSync settings are restored.
    /// </summary>
    public class Lab3Deneyler
    {
        int savedVSync, savedTargetFrameRate;
        float savedCaptureDeltaTime;
        readonly List<GameObject> created = new List<GameObject>();

        [SetUp]
        public void SaveSettings()
        {
            savedVSync = QualitySettings.vSyncCount;
            savedTargetFrameRate = Application.targetFrameRate;
            savedCaptureDeltaTime = Time.captureDeltaTime;
        }

        [TearDown]
        public void RestoreSettings()
        {
            QualitySettings.vSyncCount = savedVSync;
            Application.targetFrameRate = savedTargetFrameRate;
            Time.captureDeltaTime = savedCaptureDeltaTime;
            foreach (GameObject go in created)
                if (go != null)
                    Object.Destroy(go);
            created.Clear();
        }

        T New<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            created.Add(go);
            return go.AddComponent<T>();
        }

        // ---------- deltaTime experiment with real frame-rate caps ----------

        static readonly int[] TargetFps = { 30, 60, 144 };
        const float MeasureSeconds = 2f;

        [UnityTest]
        public IEnumerator Experiment_DeltaTime_RealFrameRates()
        {
            var csv = new StringBuilder("vSyncCount_before,target_fps,measured_fps,applied,frames,real_seconds,game_seconds,distance_A_per_frame,distance_B_deltaTime,A_m_per_s,B_m_per_s\n");
            var bRates = new List<float>();
            var aRates = new List<(float fps, float rate, bool applied)>();

            foreach (int fps in TargetFps)
            {
                QualitySettings.vSyncCount = 0; // targetFrameRate is ignored while VSync is on
                Application.targetFrameRate = fps;
                Transform a = New<KareBasinaHareket>($"A_{fps}").transform;
                Transform b = New<ZamanaBagliHareket>($"B_{fps}").transform;

                float warmUntil = Time.realtimeSinceStartup + 0.5f;
                while (Time.realtimeSinceStartup < warmUntil)
                    yield return null;

                a.position = Vector3.zero;
                b.position = Vector3.zero;
                int frames = 0;
                float r0 = Time.realtimeSinceStartup, g0 = Time.time;
                while (Time.realtimeSinceStartup - r0 < MeasureSeconds)
                {
                    yield return null;
                    frames++;
                }
                float real = Time.realtimeSinceStartup - r0, game = Time.time - g0;
                float distA = a.position.z, distB = b.position.z;
                float measuredFps = frames / real;
                bool applied = Mathf.Abs(measuredFps - fps) / fps < 0.10f;

                csv.AppendLine($"{savedVSync},{fps},{Lab3Kayit.F(measuredFps, "F1")},{(applied ? "yes" : "NO")},{frames},{Lab3Kayit.F(real, "F3")},{Lab3Kayit.F(game, "F3")},{Lab3Kayit.F(distA, "F2")},{Lab3Kayit.F(distB, "F2")},{Lab3Kayit.F(distA / real, "F2")},{Lab3Kayit.F(distB / real, "F2")}");
                bRates.Add(distB / real);
                aRates.Add((measuredFps, distA / real, applied));

                Object.Destroy(a.gameObject);
                Object.Destroy(b.gameObject);
                // A moves exactly 0.2 per frame; B moves 12 × game time. (The last frame may
                // have run after the timer check, so allow one frame of slack for A.)
                Assert.AreEqual(0.2f * frames, distA, 0.2f + 1e-3f * frames, $"A at {fps}");
                Assert.AreEqual(12f * game, distB, 0.25f, $"B at {fps}");
            }
            Lab3Kayit.Yaz("deltatime_deneyi_gercek.csv", csv.ToString());
            TestContext.WriteLine(csv.ToString());

            // B: speed per real second is the same at every frame rate.
            foreach (float r in bRates)
                Assert.AreEqual(12f, r, 12f * 0.05f, "deltaTime movement must be about 12 m/s at every frame rate");
            // A: speed per real second follows the frame rate (0.2 × fps).
            foreach (var (measuredFps, rate, _) in aRates)
                Assert.AreEqual(0.2f * measuredFps, rate, 0.2f * measuredFps * 0.05f + 0.2f);
        }

        // ---------- deltaTime experiment, controlled: fixed simulated frame time ----------

        [UnityTest]
        public IEnumerator Experiment_DeltaTime_ControlledFrameTimes()
        {
            var csv = new StringBuilder("simulated_fps,frames,game_seconds,distance_A_per_frame,distance_B_deltaTime\n");
            foreach (int fps in TargetFps)
            {
                Transform a = New<KareBasinaHareket>($"A_{fps}").transform;
                Transform b = New<ZamanaBagliHareket>($"B_{fps}").transform;
                Time.captureDeltaTime = 1f / fps; // every frame advances game time by exactly 1/fps
                yield return null;
                a.position = Vector3.zero;
                b.position = Vector3.zero;
                int frames = Mathf.RoundToInt(MeasureSeconds * fps);
                float g0 = Time.time;
                for (int i = 0; i < frames; i++)
                    yield return null;
                float game = Time.time - g0;
                csv.AppendLine($"{fps},{frames},{Lab3Kayit.F(game, "F3")},{Lab3Kayit.F(a.position.z, "F3")},{Lab3Kayit.F(b.position.z, "F3")}");

                Assert.AreEqual(MeasureSeconds, game, 1e-3f);
                Assert.AreEqual(0.2f * frames, a.position.z, 1e-2f, "A: 0.2 per frame → distance depends on fps");
                Assert.AreEqual(12f * MeasureSeconds, b.position.z, 1e-2f, "B: 12 m/s × 2 s at every fps");
                Object.Destroy(a.gameObject);
                Object.Destroy(b.gameObject);
            }
            Time.captureDeltaTime = 0f;
            Lab3Kayit.Yaz("deltatime_deneyi_kontrollu.csv", csv.ToString());
            TestContext.WriteLine(csv.ToString());
        }

        // ---------- follow camera: Update vs LateUpdate ----------

        // Uneven frame times (alternating 1/30 s and 1/144 s) make a one-frame lag visible as a
        // varying on-screen position of the aircraft. Measured at the end of each frame.
        [UnityTest]
        public IEnumerator Experiment_FollowCamera_UpdateVsLateUpdate()
        {
            var csv = new StringBuilder("variant,frame_times,frames,viewport_x_std,viewport_y_std,mean_frame_to_frame_viewport_jump\n");
            var results = new Dictionary<string, float>();
            foreach (bool alternating in new[] { false, true })
            {
                foreach (bool lateUpdate in new[] { true, false })
                {
                    var ucus = New<UcusKontrol>("Ucak");
                    ucus.GazAyarla(1f);
                    var cam = New<Camera>("Kamera");
                    var takip = cam.gameObject.AddComponent<TakipKamerasi>();
                    takip.Hedef = ucus.transform;
                    if (!lateUpdate)
                    {
                        takip.enabled = false;
                        cam.gameObject.AddComponent<UpdateIleTakip>().Takip = takip;
                    }
                    takip.Yerles();
                    var sampler = cam.gameObject.AddComponent<KareSonuOrnekleyici>();
                    sampler.Kamera = cam;
                    sampler.Hedef = ucus.transform;
                    sampler.Degisken = alternating;

                    const int frames = 180;
                    for (int i = 0; i < frames; i++)
                        yield return null;
                    Time.captureDeltaTime = 0f;

                    var xs = new List<float>();
                    var ys = new List<float>();
                    for (int i = 60; i < sampler.Ornekler.Count; i++) // skip the settling frames
                    {
                        xs.Add(sampler.Ornekler[i].x);
                        ys.Add(sampler.Ornekler[i].y);
                    }

                    float jump = 0f;
                    for (int i = 1; i < ys.Count; i++)
                        jump += Vector2.Distance(new Vector2(xs[i], ys[i]), new Vector2(xs[i - 1], ys[i - 1]));
                    jump /= ys.Count - 1;
                    string variant = lateUpdate ? "LateUpdate" : "Update(before flight)";
                    string times = alternating ? "alternating 1/30,1/144" : "constant 1/60";
                    csv.AppendLine($"{variant},{times},{ys.Count},{Lab3Kayit.F(Std(xs), "F6")},{Lab3Kayit.F(Std(ys), "F6")},{Lab3Kayit.F(jump, "F6")}");
                    results[$"{lateUpdate}_{alternating}"] = jump;

                    Object.Destroy(ucus.gameObject);
                    Object.Destroy(cam.gameObject);
                    yield return null;
                }
            }
            Lab3Kayit.Yaz("kamera_update_lateupdate_deneyi.csv", csv.ToString());
            TestContext.WriteLine(csv.ToString());

            Assert.That(results["True_True"], Is.LessThan(results["False_True"]),
                "With uneven frame times, LateUpdate must jitter less than Update");
        }

        static float Std(List<float> values)
        {
            float mean = 0f;
            foreach (float v in values) mean += v;
            mean /= values.Count;
            float var = 0f;
            foreach (float v in values) var += (v - mean) * (v - mean);
            return Mathf.Sqrt(var / values.Count);
        }
    }
}
