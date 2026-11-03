using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NU36.Tests
{
    /// <summary>
    /// Flight model of UcusKontrol (opt-in parameters) and the horizon-locked chase camera. Deterministic: UcusKontrol.Adim with a fixed dt,
    /// no scene, no rendering. The Lab 3 defaults (all new parameters zero) are covered by Lab3UcusKontrolTests and the defaults test below.
    /// </summary>
    public class Lab4FlightModelTests
    {
        GameObject go, camGo;
        Scene scene;

        [TearDown]
        public void Destroy()
        {
            if (go != null) Object.DestroyImmediate(go);
            if (camGo != null) Object.DestroyImmediate(camGo);
            if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        UcusKontrol Create()
        {
            go = new GameObject("Ucak");
            return go.AddComponent<UcusKontrol>();
        }

        /// <summary>The flight model as set in Rota04Hud.</summary>
        UcusKontrol CreateModel()
        {
            UcusKontrol u = Create();
            u.HizIvmesi = 6f;
            u.EgimHizKaybi = 12f;
            u.YatisDengeleme = 1.5f;
            u.EnFazlaYatis = 65f;
            u.YatisDonusu = 1f;
            u.EnFazlaDonusHizi = 45f;
            return u;
        }

        static void Run(UcusKontrol u, float seconds, int fps = 60)
        {
            int n = Mathf.RoundToInt(seconds * fps);
            for (int i = 0; i < n; i++)
                u.Adim(1f / fps);
        }

        /// <summary>Sets a bank in degrees, positive = right wing down (the direction of Yatis = +1).</summary>
        static void SetBank(UcusKontrol u, float deg)
        {
            u.transform.rotation = Quaternion.Euler(0f, 0f, -deg);
            if (Mathf.Sign(u.YatisAcisi) != Mathf.Sign(deg))
                u.transform.rotation = Quaternion.Euler(0f, 0f, deg);
            Assert.AreEqual(deg, u.YatisAcisi, 0.01f);
        }

        static float Pitch(Transform t) => Mathf.Asin(Mathf.Clamp(t.forward.y, -1f, 1f)) * Mathf.Rad2Deg;   // + = nose up

        // ---------------------------------------------------------------- defaults
        [Test]
        public void Defaults_KeepTheLab3Behaviour()
        {
            UcusKontrol u = Create();
            Assert.AreEqual(0f, u.HizIvmesi);
            Assert.AreEqual(0f, u.EgimHizKaybi);
            Assert.AreEqual(0f, u.YatisDengeleme);
            Assert.AreEqual(0f, u.EnFazlaYatis);
            Assert.AreEqual(0f, u.YatisDonusu);
            Assert.IsFalse(u.YunuslamaTers);
            // a banked aircraft stays banked and does not turn; the speed jumps to the throttle speed
            SetBank(u, 40f);
            u.GazAyarla(0.5f);
            Vector3 f0 = u.transform.forward;
            u.GazGirdisi = 1f;
            Run(u, 0.5f);
            Assert.AreEqual(42f, u.Hiz, 1e-3f, "throttle 1 at once gives 42 m/s");
            Assert.AreEqual(40f, u.YatisAcisi, 0.05f);
            Assert.AreEqual(0f, Vector3.Angle(f0, u.transform.forward), 0.01f);
        }

        [Test]
        public void LongFrames_AreCutToTheMaximumStep()
        {
            UcusKontrol u = Create();
            Assert.AreEqual(0.1f, u.AdimSuresi(0.333f), 1e-6f);
            Assert.AreEqual(0.016f, u.AdimSuresi(0.016f), 1e-6f);
            u.EnFazlaAdim = 0.05f;
            Assert.AreEqual(0.05f, u.AdimSuresi(0.1f), 1e-6f);
        }

        // ---------------------------------------------------------------- speed
        [Test]
        public void Speed_FollowsTheThrottleWithALimitedAcceleration()
        {
            UcusKontrol u = CreateModel();
            u.GazAyarla(0.5f);
            u.GazGirdisi = 1f;
            float previous = u.Hiz;
            for (int i = 0; i < 60; i++)
            {
                u.Adim(1f / 60f);
                Assert.LessOrEqual(u.Hiz - previous, 6f / 60f + 1e-4f, "never faster than 6 m/s²");
                Assert.GreaterOrEqual(u.Hiz, previous, "no dip while accelerating");
                previous = u.Hiz;
            }
            Assert.AreEqual(31f, u.Hiz, 0.1f, "25 + 6 m/s after one second");
            Run(u, 4f);
            Assert.AreEqual(42f, u.Hiz, 1e-3f, "stops at the top speed");
        }

        [Test]
        public void Speed_FallsControlled_AndStopsAtTheMinimum()
        {
            UcusKontrol u = CreateModel();
            u.GazAyarla(1f);
            u.GazGirdisi = -1f;
            Run(u, 1f);
            Assert.AreEqual(36f, u.Hiz, 0.1f, "42 - 6 m/s after one second");
            float previous = u.Hiz;
            for (int i = 0; i < 6 * 60; i++)
            {
                u.Adim(1f / 60f);
                Assert.LessOrEqual(u.Hiz, previous + 1e-4f);
                Assert.GreaterOrEqual(u.Hiz, u.EnAzHiz - 1e-4f, "never below the minimum speed");
                previous = u.Hiz;
            }
            Assert.AreEqual(8f, u.Hiz, 1e-3f);
        }

        [Test]
        public void Speed_ClimbCostsAndDiveGainsSpeed_WithinTheLimits()
        {
            UcusKontrol u = CreateModel();
            u.GazAyarla(1f);
            u.transform.rotation = Quaternion.Euler(-45f, 0f, 0f);   // nose up 45 degrees
            Assert.AreEqual(45f, Pitch(u.transform), 0.01f);
            u.GazGirdisi = 0f;
            Run(u, 6f);
            Assert.AreEqual(42f - 12f * Mathf.Sin(45f * Mathf.Deg2Rad), u.Hiz, 0.15f, "climb at full throttle");

            UcusKontrol d = null;
            Object.DestroyImmediate(go);
            d = CreateModel();
            d.GazAyarla(0.5f);
            d.transform.rotation = Quaternion.Euler(45f, 0f, 0f);   // nose down 45 degrees
            Run(d, 6f);
            Assert.AreEqual(25f + 12f * Mathf.Sin(45f * Mathf.Deg2Rad), d.Hiz, 0.15f, "dive at half throttle");

            Object.DestroyImmediate(go);
            d = CreateModel();
            d.GazAyarla(1f);
            d.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // straight down at full throttle
            Run(d, 3f);
            Assert.AreEqual(42f, d.Hiz, 1e-3f, "the top speed is not exceeded");
        }

        [Test]
        public void Speed_AtTheStart_MatchesTheDistanceFlown()
        {
            UcusKontrol u = CreateModel();
            u.GazAyarla(0.5f);
            u.GazGirdisi = 1f;
            Vector3 p = u.transform.position;
            float toplam = 0f;
            for (int i = 0; i < 240; i++)
            {
                u.Adim(1f / 60f);
                toplam += u.Hiz / 60f;
            }
            Assert.AreEqual(toplam, (u.transform.position - p).magnitude, 0.01f, "the HUD speed is the real speed along the path");
        }

        // ---------------------------------------------------------------- pitch, roll, yaw directions
        [Test]
        public void ControlDirections_AreConsistent()
        {
            UcusKontrol u = CreateModel();
            u.Yunuslama = 1f;
            Run(u, 1f);
            Assert.Less(Pitch(u.transform), -30f, "W (+1) = nose down");
            Object.DestroyImmediate(go);

            u = CreateModel();
            u.Yunuslama = -1f;
            Run(u, 1f);
            Assert.Greater(Pitch(u.transform), 30f, "S (-1) = nose up");
            Object.DestroyImmediate(go);

            u = CreateModel();
            u.YunuslamaTers = true;
            u.Yunuslama = 1f;
            Run(u, 1f);
            Assert.Greater(Pitch(u.transform), 30f, "W = nose up when reversed");
            Object.DestroyImmediate(go);

            u = CreateModel();
            u.Yatis = 1f;
            Run(u, 0.5f);
            Assert.Greater(u.YatisAcisi, 25f, "D (+1) = right wing down");
            Assert.Less(u.transform.right.y, -0.35f);
            Object.DestroyImmediate(go);

            u = CreateModel();
            u.Yatis = -1f;
            Run(u, 0.5f);
            Assert.Less(u.YatisAcisi, -25f, "A (-1) = left wing down");
            Object.DestroyImmediate(go);

            u = CreateModel();
            u.Sapma = 1f;
            Run(u, 1f);
            Assert.Greater(Mathf.DeltaAngle(0f, u.transform.eulerAngles.y), 10f, "E (+1) = nose right");
        }

        // ---------------------------------------------------------------- coordinated turn
        [TestCase(30f, 25f)]
        [TestCase(-30f, 25f)]
        [TestCase(45f, 42f)]
        public void BankedAircraft_TurnsAtTheCoordinatedRate_AndHoldsItsAltitude(float bank, float speed)
        {
            UcusKontrol u = Create();
            u.YatisDonusu = 1f;
            u.GazAyarla((speed - 8f) / 34f);
            SetBank(u, bank);
            float y0 = u.transform.position.y;
            float h0 = u.transform.eulerAngles.y;
            Run(u, 1f);
            float expected = 9.81f * Mathf.Tan(bank * Mathf.Deg2Rad) / speed * Mathf.Rad2Deg;
            Assert.AreEqual(expected, Mathf.DeltaAngle(h0, u.transform.eulerAngles.y), 0.4f, "heading change in one second");
            Assert.AreEqual(bank, u.YatisAcisi, 0.3f, "the bank is kept");
            Assert.AreEqual(y0, u.transform.position.y, 0.05f, "a level turn does not climb or sink");
        }

        [Test]
        public void TurnRate_IsCapped_AndFasterAtLowSpeed()
        {
            UcusKontrol slow = Create();
            slow.YatisDonusu = 1f;
            slow.EnFazlaDonusHizi = 45f;
            slow.GazAyarla(0f);   // 8 m/s
            SetBank(slow, 65f);
            float h0 = slow.transform.eulerAngles.y;
            Run(slow, 1f);
            Assert.AreEqual(45f, Mathf.DeltaAngle(h0, slow.transform.eulerAngles.y), 0.6f, "capped at 45 degrees/s");
            Object.DestroyImmediate(go);

            UcusKontrol a = Create();
            a.YatisDonusu = 1f;
            a.GazAyarla(0.4f);
            SetBank(a, 30f);
            float ha = a.transform.eulerAngles.y;
            Run(a, 1f);
            float rateA = Mathf.DeltaAngle(ha, a.transform.eulerAngles.y);
            Object.DestroyImmediate(go);
            UcusKontrol b = Create();
            b.YatisDonusu = 1f;
            b.GazAyarla(1f);
            SetBank(b, 30f);
            float hb = b.transform.eulerAngles.y;
            Run(b, 1f);
            Assert.Greater(rateA, Mathf.DeltaAngle(hb, b.transform.eulerAngles.y) * 1.3f, "the same bank turns faster when slow");
        }

        // ---------------------------------------------------------------- leveling and bank limit
        [Test]
        public void Bank_ReturnsToLevelWhenTheKeyIsReleased_WithoutChangingThePitch()
        {
            UcusKontrol u = CreateModel();
            u.transform.rotation = Quaternion.Euler(-10f, 0f, 0f);
            float pitch0 = Pitch(u.transform);
            u.transform.rotation = u.transform.rotation * Quaternion.Euler(0f, 0f, -40f);
            if (u.YatisAcisi < 0f)
                u.transform.rotation = Quaternion.Euler(-10f, 0f, 0f) * Quaternion.Euler(0f, 0f, 40f);
            Assert.AreEqual(40f, u.YatisAcisi, 1.5f);
            Run(u, 3f);
            Assert.Less(Mathf.Abs(u.YatisAcisi), 1.5f, "level after three seconds");
            Assert.AreEqual(pitch0, Pitch(u.transform), 2.5f, "the nose keeps its attitude");
        }

        [Test]
        public void Bank_IsNotLevelledWhileTheKeyIsHeld_AndStopsAtTheLimit()
        {
            UcusKontrol u = CreateModel();
            u.YatisDonusu = 0f;
            u.Yatis = 1f;
            Run(u, 5f);
            Assert.AreEqual(65f, u.YatisAcisi, 0.6f, "right bank stops at the limit and stays there while the key is held");
            u.Yatis = -1f;
            Run(u, 5f);
            Assert.AreEqual(-65f, u.YatisAcisi, 0.6f, "left bank stops at the limit");
            u.Yatis = 0f;
            Run(u, 4f);
            Assert.Less(Mathf.Abs(u.YatisAcisi), 2f, "released: back to level");
        }

        [Test]
        public void Loop_IsNotRolledUprightOrTurned_ByTheStabilityAssist()
        {
            UcusKontrol u = CreateModel();
            u.Yunuslama = -1f;   // nose up, held
            float maxRight = 0f, maxSide = 0f, minUp = 1f;
            for (int i = 0; i < 7 * 60; i++)
            {
                u.Adim(1f / 60f);
                maxRight = Mathf.Max(maxRight, Mathf.Abs(u.transform.right.y));
                maxSide = Mathf.Max(maxSide, Mathf.Abs(u.transform.forward.x));
                minUp = Mathf.Min(minUp, u.transform.up.y);
            }
            Assert.Less(maxRight, 0.03f, "wings stay level through the loop");
            Assert.Less(maxSide, 0.03f, "the loop stays in its plane");
            Assert.Less(minUp, -0.9f, "it went over the top");
        }

        // ---------------------------------------------------------------- smoothness
        [Test]
        public void ReleasingAKey_NeverCausesAJump()
        {
            UcusKontrol u = CreateModel();
            u.Yunuslama = -1f;
            u.Yatis = 1f;
            Quaternion q = u.transform.rotation;
            Vector3 p = u.transform.position;
            float lastAngle = 0f;
            float worstStep = 0f;
            for (int i = 0; i < 4 * 60; i++)
            {
                if (i == 60)
                {
                    u.Yunuslama = 0f;
                    u.Yatis = 0f;
                }
                u.Adim(1f / 60f);
                float angle = Quaternion.Angle(q, u.transform.rotation);
                worstStep = Mathf.Max(worstStep, angle);
                if (i > 0)
                    Assert.LessOrEqual(Mathf.Abs(angle - lastAngle), 0.45f, "the rotation rate changes smoothly: no step when keys are pressed, released or the leveling starts (frame " + i + ")");
                lastAngle = angle;
                Assert.LessOrEqual((u.transform.position - p).magnitude, u.EnCokHiz / 60f + 1e-3f, "no positional jump");
                q = u.transform.rotation;
                p = u.transform.position;
            }
            // pitch 55 + roll 90 degrees/s at full smoothing: about 105 degrees/s of combined rotation, at 60 FPS under 2 degrees per frame
            Assert.Less(worstStep, 2.2f);
        }

        [Test]
        public void ScriptedManoeuvre_IsAlmostTheSameAt30_60_And144Fps()
        {
            var pos = new Vector3[3];
            var fwd = new Vector3[3];
            int[] fps = { 30, 60, 144 };
            for (int k = 0; k < 3; k++)
            {
                UcusKontrol u = CreateModel();
                u.Yunuslama = -1f; Run(u, 1f, fps[k]);
                u.Yunuslama = 0f; u.Yatis = 1f; Run(u, 1f, fps[k]);
                u.Yatis = 0f; u.Sapma = 1f; u.GazGirdisi = 1f; Run(u, 1f, fps[k]);
                u.Sapma = 0f; u.GazGirdisi = -1f; Run(u, 2f, fps[k]);
                pos[k] = u.transform.position;
                fwd[k] = u.transform.forward;
                Object.DestroyImmediate(go);
            }
            float path = pos[1].magnitude;
            Assert.Less((pos[0] - pos[1]).magnitude, path * 0.03f, "30 vs 60 FPS position");
            Assert.Less((pos[2] - pos[1]).magnitude, path * 0.03f, "144 vs 60 FPS position");
            Assert.Less(Vector3.Angle(fwd[0], fwd[1]), 3f, "30 vs 60 FPS direction");
            Assert.Less(Vector3.Angle(fwd[2], fwd[1]), 3f, "144 vs 60 FPS direction");
        }

        // ---------------------------------------------------------------- chase camera
        TakipKamerasi CreateCamera(Transform target, bool horizon)
        {
            camGo = new GameObject("Cam", typeof(Camera));
            var cam = camGo.AddComponent<TakipKamerasi>();
            cam.Hedef = target;
            cam.UfukKilitli = horizon;
            return cam;
        }

        [Test]
        public void ChaseCamera_Default_TurnsWithTheWholeAircraft_AsInLab3()
        {
            UcusKontrol u = Create();
            TakipKamerasi cam = CreateCamera(u.transform, false);
            u.transform.rotation = Quaternion.Euler(10f, 30f, -70f);
            Assert.AreEqual(0f, (cam.IstenenKonum - u.transform.TransformPoint(cam.Uzaklik)).magnitude, 1e-4f);
        }

        [Test]
        public void ChaseCamera_HorizonLocked_IgnoresRollAndPitch_ButFollowsTheHeading()
        {
            UcusKontrol u = Create();
            TakipKamerasi cam = CreateCamera(u.transform, true);
            Vector3 Offset(Quaternion rotation)
            {
                u.transform.rotation = rotation;
                cam.YonuGuncelle(float.PositiveInfinity);
                return cam.IstenenKonum - u.transform.position;
            }
            Vector3 level = Offset(Quaternion.Euler(0f, 40f, 0f));
            Assert.AreEqual(0f, (Offset(Quaternion.Euler(0f, 40f, -80f)) - level).magnitude, 1e-3f, "roll does not move the camera");
            Assert.AreEqual(0f, (Offset(Quaternion.Euler(-45f, 40f, 0f)) - level).magnitude, 1e-3f, "pitch does not move the camera");
            Assert.AreEqual(3.2f, level.y, 1e-3f, "stays 3.2 m above");
            Assert.AreEqual(11f, new Vector2(level.x, level.z).magnitude, 1e-3f, "stays 11 m behind, horizontally");
            Vector3 turned = Offset(Quaternion.Euler(0f, 130f, 0f));
            Assert.AreEqual(90f, Vector3.Angle(new Vector3(level.x, 0f, level.z), new Vector3(turned.x, 0f, turned.z)), 0.1f, "the offset follows the heading");
        }

        [Test]
        public void ChaseCamera_HorizonLocked_SwingsInsteadOfJumping_OverAWholeLoop()
        {
            UcusKontrol u = Create();
            TakipKamerasi cam = CreateCamera(u.transform, true);
            u.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            cam.YonuGuncelle(float.PositiveInfinity);
            Vector3 previous = cam.IstenenKonum - u.transform.position;
            float worst = 0f;
            // pitch 1 degree per step (60 degrees/s at 60 FPS) from level, over the vertical, to level again
            for (int pitch = 1; pitch <= 360; pitch++)
            {
                u.transform.rotation = Quaternion.Euler(0f, 25f, 0f) * Quaternion.Euler(-pitch, 0f, 0f);
                cam.YonuGuncelle(1f / 60f);
                Vector3 offset = cam.IstenenKonum - u.transform.position;
                Assert.IsFalse(float.IsNaN(offset.x) || float.IsNaN(offset.y) || float.IsNaN(offset.z));
                worst = Mathf.Max(worst, (offset - previous).magnitude);
                previous = offset;
            }
            // 180 degrees/s swing limit: at most 3 degrees per step of an 11 m horizontal offset = 0.58 m
            Assert.Less(worst, 0.7f, "the camera position never moves more than the swing limit allows per step");
        }

        // ---------------------------------------------------------------- scenes
        [Test]
        public void Rota04Hud_HasTheFlightModel_AndOnlyUcusKontrolMovesTheAircraft()
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/Rota04Hud.unity", OpenSceneMode.Additive);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject ucak = roots.Single(g => g.GetComponent<UcusKontrol>() != null);
            var so = new SerializedObject(ucak.GetComponent<UcusKontrol>());
            Assert.AreEqual(6f, so.FindProperty("hizIvmesi").floatValue);
            Assert.AreEqual(12f, so.FindProperty("egimHizKaybi").floatValue);
            Assert.AreEqual(1.5f, so.FindProperty("yatisDengeleme").floatValue);
            Assert.AreEqual(65f, so.FindProperty("enFazlaYatis").floatValue);
            Assert.AreEqual(1f, so.FindProperty("yatisDonusu").floatValue);
            Assert.AreEqual(45f, so.FindProperty("enFazlaDonusHizi").floatValue);
            Assert.IsFalse(so.FindProperty("yunuslamaTers").boolValue, "W stays nose down until the user decides (D-029)");
            var cam = roots.Single(g => g.GetComponent<TakipKamerasi>() != null).GetComponent<TakipKamerasi>();
            Assert.IsTrue(new SerializedObject(cam).FindProperty("ufukKilitli").boolValue);

            var hedefe = ucak.GetComponent<HedefeGit>();
            var yatisSc = ucak.GetComponent<KanatYatisi>();
            Assert.IsTrue(hedefe == null || !hedefe.enabled, "HedefeGit must not move the aircraft");
            Assert.IsTrue(yatisSc == null || !yatisSc.enabled, "KanatYatisi must not roll the aircraft");
            Assert.AreEqual(1, ucak.GetComponentsInChildren<Rigidbody>().Length);
            Assert.IsTrue(ucak.GetComponent<Rigidbody>().isKinematic);
        }

        [TestCase("Assets/Scenes/Rota03.unity")]
        [TestCase("Assets/Scenes/Rota04.unity")]
        public void OtherFlyingScenes_KeepTheLab3FlightValues(string path)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            GameObject[] roots = scene.GetRootGameObjects();
            var so = new SerializedObject(roots.Single(g => g.GetComponent<UcusKontrol>() != null).GetComponent<UcusKontrol>());
            foreach (string f in new[] { "hizIvmesi", "egimHizKaybi", "yatisDengeleme", "enFazlaYatis", "yatisDonusu" })
                Assert.AreEqual(0f, so.FindProperty(f).floatValue, f);
            Assert.IsFalse(new SerializedObject(roots.Single(g => g.GetComponent<TakipKamerasi>() != null).GetComponent<TakipKamerasi>())
                .FindProperty("ufukKilitli").boolValue);
        }
    }
}
