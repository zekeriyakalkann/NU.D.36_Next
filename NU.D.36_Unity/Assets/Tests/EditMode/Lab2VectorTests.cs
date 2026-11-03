using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 2, Tasks 4–7: deterministic checks of HedefeGit, Radar, KanatYatisi and their
    /// Gizmo code, stepping the components manually with a fixed time step.
    /// </summary>
    public class Lab2VectorTests
    {
        const float Dt = 0.02f;
        const float Tolerance = 1e-4f;

        GameObject root;

        [SetUp]
        public void CreateRoot()
        {
            root = new GameObject("Lab2TestRoot");
        }

        [TearDown]
        public void DestroyRoot()
        {
            Object.DestroyImmediate(root);
        }

        Transform Create(string name, Vector3 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root.transform, false);
            t.position = position;
            return t;
        }

        // ---------- Task 4: HedefeGit ----------

        [TestCase(1f, 0f, 0f)]
        [TestCase(0f, 0f, 1000f)]
        [TestCase(40f, 10f, 60f)]
        [TestCase(-0.001f, 0.002f, 0f)]
        public void HedefYonu_IsUnitLength(float x, float y, float z)
        {
            Vector3 yon = HedefeGit.HedefYonu(Vector3.zero, new Vector3(x, y, z));
            Assert.That(yon.magnitude, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void HedefYonu_IsZeroForCoincidentPoints()
        {
            Assert.AreEqual(Vector3.zero, HedefeGit.HedefYonu(Vector3.one, Vector3.one));
        }

        // Normalization test: the step length depends only on speed, not on target distance.
        [TestCase(10f)]
        [TestCase(1000f)]
        public void Adim_MovesSpeedTimesDtRegardlessOfDistance(float distance)
        {
            Transform ucak = Create("Ucak", Vector3.zero);
            var git = ucak.gameObject.AddComponent<HedefeGit>();
            git.Hedef = Create("Hedef", new Vector3(0f, 0f, distance));
            git.Hiz = 20f;
            git.VarisMesafesi = 1f;

            git.Adim(Dt);

            Assert.That(ucak.position.magnitude, Is.EqualTo(git.Hiz * Dt).Within(Tolerance));
        }

        [Test]
        public void Adim_TurnIsBoundedByTurnRate()
        {
            Transform ucak = Create("Ucak", Vector3.zero);
            var git = ucak.gameObject.AddComponent<HedefeGit>();
            git.Hedef = Create("Hedef", new Vector3(0f, 0f, -100f)); // directly behind
            git.DonusHizi = 90f;

            for (int i = 0; i < 10; i++)
            {
                Quaternion before = ucak.rotation;
                git.Adim(Dt);
                Assert.That(Quaternion.Angle(before, ucak.rotation),
                    Is.LessThanOrEqualTo(git.DonusHizi * Dt + 1e-3f), $"step {i}");
            }
        }

        [Test]
        public void Adim_ReachesRota02TargetAndStopsWithinArrivalDistance()
        {
            Transform ucak = Create("Ucak", new Vector3(0f, 20f, 0f));
            var git = ucak.gameObject.AddComponent<HedefeGit>();
            Transform balon = Create("Balon", new Vector3(40f, 30f, 60f));
            git.Hedef = balon;

            const int maxSteps = 3000; // 60 simulated seconds
            int steps = 0;
            while (!git.HedefeVardi && steps++ < maxSteps)
                git.Adim(Dt);

            Assert.IsTrue(git.HedefeVardi, "Did not reach the target within 60 simulated seconds");
            Assert.That(Vector3.Distance(ucak.position, balon.position),
                Is.LessThanOrEqualTo(git.VarisMesafesi + git.Hiz * Dt));
            TestContext.WriteLine($"Arrived after {steps * Dt:F2} s at {ucak.position}");

            Vector3 stopped = ucak.position;
            for (int i = 0; i < 50; i++)
                git.Adim(Dt);
            Assert.AreEqual(stopped, ucak.position, "Must not move while within the arrival distance");
        }

        // ---------- Task 5: Radar ----------

        const float HalfAngle = 45f;
        const float Range = 100f;

        static Vector3 AtAngle(float degrees, float distance)
        {
            // Rotate +Z around +Y: 0° is straight ahead, 180° is directly behind.
            return Quaternion.Euler(0f, degrees, 0f) * Vector3.forward * distance;
        }

        [TestCase(0f, 50f, true)]
        [TestCase(44.5f, 50f, true)]
        [TestCase(45.5f, 50f, false)]
        [TestCase(-44.5f, 50f, true)]
        [TestCase(-45.5f, 50f, false)]
        [TestCase(90f, 50f, false)]
        [TestCase(180f, 50f, false)]
        [TestCase(0f, 99.9f, true)]
        [TestCase(0f, 100f, true)]
        [TestCase(0f, 100.1f, false)]
        [TestCase(180f, 0f, true)]
        public void GorusAlanindaMi_RespectsHalfAngleAndRange(float degrees, float distance, bool expected)
        {
            bool visible = Radar.GorusAlanindaMi(Vector3.zero, Vector3.forward, AtAngle(degrees, distance), HalfAngle, Range);
            Assert.AreEqual(expected, visible);
        }

        [Test]
        public void GorusAlanindaMi_UsesVerticalAngleToo()
        {
            Vector3 up40 = Quaternion.Euler(-40f, 0f, 0f) * Vector3.forward * 50f;
            Vector3 up50 = Quaternion.Euler(-50f, 0f, 0f) * Vector3.forward * 50f;
            Assert.IsTrue(Radar.GorusAlanindaMi(Vector3.zero, Vector3.forward, up40, HalfAngle, Range));
            Assert.IsFalse(Radar.GorusAlanindaMi(Vector3.zero, Vector3.forward, up50, HalfAngle, Range));
        }

        [Test]
        public void Hesapla_FollowsObserverRotation()
        {
            Transform ucaksavar = Create("Ucaksavar", Vector3.zero);
            var radar = ucaksavar.gameObject.AddComponent<Radar>();
            radar.Hedef = Create("Hedef", new Vector3(50f, 0f, 0f));

            Assert.IsFalse(radar.Hesapla(), "Target at 90° should be outside the view");
            ucaksavar.rotation = Quaternion.Euler(0f, 90f, 0f);
            Assert.IsTrue(radar.Hesapla(), "After turning toward +X the target should be inside the view");
        }

        // ---------- Task 6: Cross-product side and wing roll ----------

        [TestCase(0f)]
        [TestCase(37f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(-120f)]
        public void YanIsareti_MatchesUnityRightVector(float heading)
        {
            Transform t = Create("Heading", Vector3.zero);
            t.rotation = Quaternion.Euler(0f, heading, 0f);

            Vector3 rightTarget = t.right * 10f + t.forward * 10f;
            Vector3 leftTarget = -t.right * 10f + t.forward * 10f;
            Vector3 aheadTarget = t.forward * 10f;

            Assert.AreEqual(1, KanatYatisi.YanIsareti(t.forward, rightTarget, 0.05f), "right");
            Assert.AreEqual(-1, KanatYatisi.YanIsareti(t.forward, leftTarget, 0.05f), "left");
            Assert.AreEqual(0, KanatYatisi.YanIsareti(t.forward, aheadTarget, 0.05f), "ahead");
        }

        static (KanatYatisi yatis, Transform gorsel) CreateRollRig(Transform owner, Transform target)
        {
            Transform gorsel = new GameObject("Gorsel").transform;
            gorsel.SetParent(owner, false);
            var yatis = owner.gameObject.AddComponent<KanatYatisi>();
            yatis.Hedef = target;
            yatis.Gorsel = gorsel;
            return (yatis, gorsel);
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void Adim_BanksTowardTargetSide(float side)
        {
            Transform ucak = Create("Ucak", Vector3.zero);
            var (yatis, gorsel) = CreateRollRig(ucak, Create("Hedef", new Vector3(side * 50f, 0f, 50f)));

            for (int i = 0; i < 200; i++) // 4 s, enough to reach the maximum bank
                yatis.Adim(Dt);

            Assert.That(yatis.YatisAcisi, Is.EqualTo(side * yatis.MaksYatisAcisi).Within(Tolerance));
            // Wingtip points on the pilot's right (+X) and left (-X) of the visual child.
            float rightY = gorsel.TransformPoint(new Vector3(4.87f, 0f, 0f)).y;
            float leftY = gorsel.TransformPoint(new Vector3(-4.87f, 0f, 0f)).y;
            if (side > 0)
                Assert.That(rightY, Is.LessThan(leftY), "Target on the right: right wing must go down");
            else
                Assert.That(leftY, Is.LessThan(rightY), "Target on the left: left wing must go down");
        }

        [Test]
        public void Adim_LevelsOutWhenTargetIsAhead()
        {
            Transform ucak = Create("Ucak", Vector3.zero);
            Transform hedef = Create("Hedef", new Vector3(50f, 0f, 50f));
            var (yatis, gorsel) = CreateRollRig(ucak, hedef);
            for (int i = 0; i < 200; i++)
                yatis.Adim(Dt);

            hedef.position = new Vector3(0f, 0f, 100f);
            for (int i = 0; i < 200; i++)
                yatis.Adim(Dt);

            Assert.That(yatis.YatisAcisi, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(Quaternion.Angle(Quaternion.identity, gorsel.localRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void Roll_IsNotErasedByHeadingControl()
        {
            Transform ucak = Create("Ucak", new Vector3(0f, 20f, 0f));
            Transform balon = Create("Balon", new Vector3(40f, 30f, 60f));
            var git = ucak.gameObject.AddComponent<HedefeGit>();
            git.Hedef = balon;
            var (yatis, gorsel) = CreateRollRig(ucak, balon);

            for (int i = 0; i < 25; i++) // 0.5 s into the initial right turn
            {
                git.Adim(Dt);
                yatis.Adim(Dt);
            }

            Assert.That(yatis.YatisAcisi, Is.GreaterThan(0f), "Initial turn toward Balon is to the right");
            Assert.That(Quaternion.Angle(ucak.rotation, gorsel.rotation), Is.GreaterThan(1f),
                "Visual child must keep its bank relative to the moving root");
            git.Adim(Dt); // heading update after the last roll update
            Assert.That(Quaternion.Angle(Quaternion.Euler(0f, 0f, -yatis.YatisAcisi), gorsel.localRotation),
                Is.LessThan(0.01f), "Heading control must leave the child's local bank untouched");
        }

        // ---------- Task 7: Gizmos ----------

        [Test]
        public void GizmoRengi_DistinguishesVisibility()
        {
            Assert.AreEqual(Radar.GorunurRengi, Radar.GizmoRengi(true));
            Assert.AreEqual(Radar.GorunmezRengi, Radar.GizmoRengi(false));
            Assert.AreNotEqual(Radar.GizmoRengi(true), Radar.GizmoRengi(false));
        }

        // Gizmos.* can only be called by Unity inside the Gizmo callbacks, so the drawing itself
        // is checked visually in the Scene view. These tests cover what the drawing relies on.
        [TestCase(typeof(HedefeGit))]
        [TestCase(typeof(Radar))]
        public void Component_ImplementsOnDrawGizmos(System.Type type)
        {
            MethodInfo m = type.GetMethod("OnDrawGizmos", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(m, $"{type.Name} must implement OnDrawGizmos");
        }

        [Test]
        public void Hesapla_UsedByGizmos_DoesNotChangeRuntimeState()
        {
            Transform ucak = Create("Ucak", new Vector3(0f, 0f, 50f));
            var radar = Create("Ucaksavar", Vector3.zero).gameObject.AddComponent<Radar>();
            radar.Hedef = ucak;

            Assert.IsTrue(radar.Hesapla(), "Target straight ahead should be visible");
            Assert.IsFalse(radar.HedefGorunur, "Only Update may change HedefGorunur");
        }
    }
}
