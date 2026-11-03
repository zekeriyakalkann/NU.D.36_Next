using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 3, UcusKontrol: speed and throttle, deltaTime-based movement, rotation directions and
    /// rates, and the input smoothing experiment (yumusatma 1 vs 20). Stepped with fixed dt.
    /// </summary>
    public class Lab3UcusKontrolTests
    {
        const float Tolerance = 1e-3f;
        GameObject go;

        UcusKontrol Create(string name = "Ucak")
        {
            go = new GameObject(name);
            return go.AddComponent<UcusKontrol>();
        }

        [TearDown]
        public void Destroy()
        {
            if (go != null)
                Object.DestroyImmediate(go);
        }

        static void Run(UcusKontrol u, float seconds, int fps)
        {
            int steps = Mathf.RoundToInt(seconds * fps);
            for (int i = 0; i < steps; i++)
                u.Adim(1f / fps);
        }

        [Test]
        public void Defaults_ThrottleHalf_SpeedBetweenLimits()
        {
            UcusKontrol u = Create();
            Assert.AreEqual(0.5f, u.GazOrani, Tolerance);
            Assert.AreEqual(8f, u.HizHesapla(0f), Tolerance);
            Assert.AreEqual(42f, u.HizHesapla(1f), Tolerance);
            u.Adim(0.01f);
            Assert.AreEqual(25f, u.Hiz, Tolerance, "Lerp(8, 42, 0.5)");
        }

        [Test]
        public void Gaz_HoldIncreasesAtRate_ReleaseHolds_Clamps()
        {
            UcusKontrol u = Create();
            u.GazGirdisi = 1f;
            Run(u, 0.25f, 60);
            Assert.AreEqual(0.8f, u.GazOrani, Tolerance, "0.5 + 1.2/s × 0.25 s");
            Assert.AreEqual(8f + 34f * 0.8f, u.Hiz, Tolerance);

            u.GazGirdisi = 0f;
            Run(u, 1f, 60);
            Assert.AreEqual(0.8f, u.GazOrani, Tolerance, "Released: throttle must stay");

            u.GazGirdisi = 1f;
            Run(u, 2f, 60);
            Assert.AreEqual(1f, u.GazOrani, Tolerance);
            Assert.AreEqual(42f, u.Hiz, Tolerance);

            u.GazGirdisi = -1f;
            Run(u, 2f, 60);
            Assert.AreEqual(0f, u.GazOrani, Tolerance);
            Assert.AreEqual(8f, u.Hiz, Tolerance);
        }

        [Test]
        public void Gaz_ChangeIsFrameRateIndependent()
        {
            UcusKontrol a = Create("A");
            GameObject goA = go;
            UcusKontrol b = Create("B");
            a.GazGirdisi = b.GazGirdisi = 0.5f;
            // 0.5 s is a whole number of frames at both rates (15 and 72), so both simulate the same time.
            Run(a, 0.5f, 30);
            Run(b, 0.5f, 144);
            Assert.AreEqual(0.8f, a.GazOrani, Tolerance, "0.5 + 0.5 × 1.2/s × 0.5 s");
            Assert.AreEqual(a.GazOrani, b.GazOrani, Tolerance);
            Object.DestroyImmediate(goA);
        }

        [Test]
        public void Movement_UsesDeltaTime_SameDistanceAt30And144Fps()
        {
            UcusKontrol a = Create("A");
            GameObject goA = go;
            UcusKontrol b = Create("B");
            Run(a, 2f, 30);
            Run(b, 2f, 144);
            Assert.AreEqual(50f, a.transform.position.z, 1e-2f, "25 m/s × 2 s");
            Assert.AreEqual(a.transform.position.z, b.transform.position.z, 1e-2f);
            Object.DestroyImmediate(goA);
        }

        // Directions: W nose down, D right wing down, E nose right (flight-stick convention).
        [TestCase("Yunuslama", 1f)]
        [TestCase("Yunuslama", -1f)]
        [TestCase("Yatis", 1f)]
        [TestCase("Yatis", -1f)]
        [TestCase("Sapma", 1f)]
        [TestCase("Sapma", -1f)]
        public void Input_RotatesInExpectedDirection(string axis, float value)
        {
            UcusKontrol u = Create();
            if (axis == "Yunuslama") u.Yunuslama = value;
            if (axis == "Yatis") u.Yatis = value;
            if (axis == "Sapma") u.Sapma = value;
            Run(u, 0.5f, 60);

            Transform t = u.transform;
            if (axis == "Yunuslama")
                Assert.That(Mathf.Sign(t.forward.y), Is.EqualTo(-value), $"forward {t.forward}");
            if (axis == "Yatis")
                Assert.That(Mathf.Sign(t.right.y), Is.EqualTo(-value), $"right {t.right}");
            if (axis == "Sapma")
                Assert.That(Mathf.Sign(t.forward.x), Is.EqualTo(value), $"forward {t.forward}");
        }

        [TestCase("Yunuslama", 55f)]
        [TestCase("Yatis", 90f)]
        [TestCase("Sapma", 30f)]
        public void SteadyRotationRate_MatchesParameter(string axis, float degreesPerSecond)
        {
            UcusKontrol u = Create();
            u.Yumusatma = 10000f; // smoothed input reaches the raw input immediately
            if (axis == "Yunuslama") u.Yunuslama = 1f;
            if (axis == "Yatis") u.Yatis = 1f;
            if (axis == "Sapma") u.Sapma = 1f;
            Quaternion before = u.transform.rotation;
            Run(u, 0.5f, 120);
            Assert.AreEqual(degreesPerSecond * 0.5f, Quaternion.Angle(before, u.transform.rotation), 0.2f);
        }

        [Test]
        public void ZeroOrNegativeDt_DoesNothing()
        {
            UcusKontrol u = Create();
            u.Sapma = 1f;
            u.Adim(0f);
            u.Adim(-1f);
            Assert.AreEqual(Vector3.zero, u.transform.position);
            Assert.AreEqual(Quaternion.identity, u.transform.rotation);
        }

        // Experiment: yumusatma 1 vs 20 (6 = worksheet default). Step input 0 → 1 at 60 FPS.
        [Test]
        public void Experiment_Smoothing1Vs20_StepResponse()
        {
            float[] ks = { 1f, 6f, 20f };
            var curves = new UcusKontrol[ks.Length];
            var objects = new GameObject[ks.Length];
            for (int i = 0; i < ks.Length; i++)
            {
                objects[i] = new GameObject($"k{ks[i]}");
                curves[i] = objects[i].AddComponent<UcusKontrol>();
                curves[i].Yumusatma = ks[i];
                curves[i].Sapma = 1f;
            }

            const int fps = 60;
            var t90 = new float[ks.Length];
            var t63 = new float[ks.Length];
            var csv = new StringBuilder("t,yumusatma_1,yumusatma_6,yumusatma_20\n");
            for (int step = 1; step <= 3 * fps; step++)
            {
                float t = step / (float)fps;
                csv.Append(Lab3Kayit.F(t, "F3"));
                for (int i = 0; i < ks.Length; i++)
                {
                    curves[i].Adim(1f / fps);
                    float v = curves[i].YumusakGirdi.y;
                    if (t63[i] == 0f && v >= 1f - 1f / Mathf.Exp(1f)) t63[i] = t;
                    if (t90[i] == 0f && v >= 0.9f) t90[i] = t;
                    csv.Append(',').Append(Lab3Kayit.F(v));
                }
                csv.Append('\n');
            }
            Lab3Kayit.Yaz("yumusatma_deneyi.csv", csv.ToString());

            var summary = new StringBuilder("yumusatma,t63_s,t90_s,theory_t90_s\n");
            for (int i = 0; i < ks.Length; i++)
                summary.AppendLine($"{ks[i]},{Lab3Kayit.F(t63[i], "F3")},{Lab3Kayit.F(t90[i], "F3")},{Lab3Kayit.F(Mathf.Log(10f) / ks[i], "F3")}");
            Lab3Kayit.Yaz("yumusatma_deneyi_ozet.csv", summary.ToString());
            TestContext.WriteLine(summary.ToString());

            for (int i = 0; i < ks.Length; i++)
            {
                Assert.AreEqual(Mathf.Log(10f) / ks[i], t90[i], 1f / fps + 1e-4f, $"k={ks[i]}");
                Object.DestroyImmediate(objects[i]);
            }
            Assert.That(t90[2], Is.LessThan(t90[1]));
            Assert.That(t90[1], Is.LessThan(t90[0]));
        }
    }
}
