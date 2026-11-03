using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 3, TakipKamerasi: local-space offset, SmoothDamp follow, look-at, throttle-driven
    /// field of view, null safety, LateUpdate usage, and the smoothing experiment (0.02 vs 0.6).
    /// </summary>
    public class Lab3KameraTests
    {
        const int Fps = 60;
        const float Dt = 1f / Fps;

        GameObject targetGo, cameraGo;
        UcusKontrol ucus;
        TakipKamerasi takip;
        Camera cam;

        [SetUp]
        public void Create()
        {
            targetGo = new GameObject("Hedef");
            ucus = targetGo.AddComponent<UcusKontrol>();
            cameraGo = new GameObject("Kamera");
            cam = cameraGo.AddComponent<Camera>();
            takip = cameraGo.AddComponent<TakipKamerasi>();
            takip.Hedef = targetGo.transform;
        }

        [TearDown]
        public void Destroy()
        {
            Object.DestroyImmediate(cameraGo);
            Object.DestroyImmediate(targetGo);
        }

        [TestCase(0f, 55f)]
        [TestCase(0.5f, 63.5f)]
        [TestCase(1f, 72f)]
        [TestCase(-1f, 55f)]
        [TestCase(2f, 72f)]
        public void FieldOfView_FollowsThrottleWithinLimits(float gaz, float expected)
        {
            Assert.AreEqual(expected, takip.GorusAcisiHesapla(gaz), 1e-3f);
        }

        [Test]
        public void Takip_SetsCameraFieldOfViewFromThrottle()
        {
            ucus.GazAyarla(0f);
            takip.Takip(Dt);
            Assert.AreEqual(55f, cam.fieldOfView, 1e-3f);
            ucus.GazAyarla(1f);
            takip.Takip(Dt);
            Assert.AreEqual(72f, cam.fieldOfView, 1e-3f);
        }

        [Test]
        public void Takip_ConvergesToLocalOffsetAndLooksAtTarget()
        {
            targetGo.transform.SetPositionAndRotation(new Vector3(10f, 20f, 30f), Quaternion.Euler(0f, 90f, 0f));
            cameraGo.transform.position = new Vector3(-50f, 0f, -50f);
            for (int i = 0; i < 3 * Fps; i++)
                takip.Takip(Dt);

            Vector3 expected = targetGo.transform.TransformPoint(new Vector3(0f, 3.2f, -11f));
            Assert.That(Vector3.Distance(expected, cameraGo.transform.position), Is.LessThan(0.01f));
            Assert.That(expected.x, Is.LessThan(targetGo.transform.position.x), "Offset is behind the target in its local space (heading +X)");
            Vector3 toTarget = (targetGo.transform.position - cameraGo.transform.position).normalized;
            Assert.That(Vector3.Angle(cameraGo.transform.forward, toTarget), Is.LessThan(0.01f));
        }

        [Test]
        public void Yerles_PlacesCameraWithoutSmoothing()
        {
            targetGo.transform.position = new Vector3(5f, 6f, 7f);
            takip.Yerles();
            Assert.That(Vector3.Distance(takip.IstenenKonum, cameraGo.transform.position), Is.LessThan(1e-4f));
        }

        [Test]
        public void MissingTarget_WarnsOnceAndDoesNotThrow()
        {
            takip.Hedef = null;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[TakipKamerasi\].*no target"));
            Assert.DoesNotThrow(() => takip.Takip(Dt));
            Assert.DoesNotThrow(() => takip.Takip(Dt)); // second call: no second warning
            Assert.DoesNotThrow(() => takip.Yerles());
        }

        [Test]
        public void Camera_FollowsInLateUpdateNotUpdate()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Assert.IsNotNull(typeof(TakipKamerasi).GetMethod("LateUpdate", flags));
            Assert.IsNull(typeof(TakipKamerasi).GetMethod("Update", flags));
            Assert.IsNotNull(typeof(UcusKontrol).GetMethod("Update", flags), "Flight moves in Update, before the camera");
        }

        // Experiment: SmoothDamp smooth time 0.02 vs 0.6 (0.18 = worksheet default).
        // Aircraft at full throttle (42 m/s): 3 s straight, then 3 s of full right yaw.
        [Test]
        public void Experiment_CameraSmoothing002Vs06()
        {
            float[] smoothTimes = { 0.02f, 0.18f, 0.6f };
            var lag = new float[smoothTimes.Length][];
            for (int s = 0; s < smoothTimes.Length; s++)
            {
                targetGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                ucus.GazAyarla(1f);
                ucus.Sapma = 0f;
                ucus.Yumusatma = 6f;
                takip.Yumusatma = smoothTimes[s];
                takip.Yerles();
                lag[s] = new float[6 * Fps];
                for (int i = 0; i < 6 * Fps; i++)
                {
                    if (i == 3 * Fps) ucus.Sapma = 1f;
                    ucus.Adim(Dt);
                    takip.Takip(Dt);
                    lag[s][i] = Vector3.Distance(cameraGo.transform.position, takip.IstenenKonum);
                }
                // Reset smoothed input for the next run.
                Object.DestroyImmediate(ucus);
                ucus = targetGo.AddComponent<UcusKontrol>();
                takip.Hedef = targetGo.transform;
            }

            var csv = new StringBuilder("t,lag_0.02,lag_0.18,lag_0.6\n");
            for (int i = 0; i < 6 * Fps; i++)
                csv.AppendLine($"{Lab3Kayit.F((i + 1) * Dt, "F3")},{Lab3Kayit.F(lag[0][i])},{Lab3Kayit.F(lag[1][i])},{Lab3Kayit.F(lag[2][i])}");
            Lab3Kayit.Yaz("kamera_yumusatma_deneyi.csv", csv.ToString());

            var summary = new StringBuilder("smoothTime,straight_lag_m (t=2.5-3 s),max_lag_in_turn_m,continuous_theory_v*T_m\n");
            var straight = new float[smoothTimes.Length];
            for (int s = 0; s < smoothTimes.Length; s++)
            {
                float turnMax = 0f;
                for (int i = (int)(2.5f * Fps); i < 3 * Fps; i++) straight[s] += lag[s][i];
                straight[s] /= 0.5f * Fps;
                for (int i = 3 * Fps; i < 6 * Fps; i++) turnMax = Mathf.Max(turnMax, lag[s][i]);
                summary.AppendLine($"{Lab3Kayit.F(smoothTimes[s], "F2")},{Lab3Kayit.F(straight[s], "F3")},{Lab3Kayit.F(turnMax, "F3")},{Lab3Kayit.F(42f * smoothTimes[s], "F3")}");
            }
            Lab3Kayit.Yaz("kamera_yumusatma_deneyi_ozet.csv", summary.ToString());
            TestContext.WriteLine(summary.ToString());

            Assert.That(straight[0], Is.LessThan(straight[1]));
            Assert.That(straight[1], Is.LessThan(straight[2]));
            // Critically damped follow of a ramp: steady lag ≈ velocity × smooth time. This continuous
            // result holds when the smooth time is several frames long (0.18 s, 0.6 s at 60 FPS);
            // 0.02 s is about one frame, where SmoothDamp's discrete step deviates, so it is only measured.
            for (int s = 1; s < smoothTimes.Length; s++)
                Assert.AreEqual(42f * smoothTimes[s], straight[s], 0.15f * 42f * smoothTimes[s], $"smoothTime {smoothTimes[s]}");
        }
    }
}
