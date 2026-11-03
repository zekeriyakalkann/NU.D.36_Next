using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 2, Tasks 1 and 3: Rota02 exists, is in Build Settings, and contains the
    /// worksheet objects with the specified transforms and wiring.
    /// </summary>
    public class Lab2SceneTests
    {
        const string ScenePath = "Assets/Scenes/Rota02.unity";
        const float Tolerance = 1e-4f;

        Scene scene;

        [SetUp]
        public void OpenScene()
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void CloseScene()
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        GameObject Root(string name)
        {
            GameObject[] matches = scene.GetRootGameObjects().Where(g => g.name == name).ToArray();
            Assert.AreEqual(1, matches.Length, $"Expected exactly one root object named '{name}'");
            return matches[0];
        }

        static void AssertVector(Vector3 expected, Vector3 actual, string what)
        {
            Assert.That((expected - actual).magnitude, Is.LessThan(Tolerance), $"{what}: expected {expected}, got {actual}");
        }

        [Test]
        public void Rota02_IsEnabledInBuildSettings()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled));
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == "Assets/Scenes/SampleScene.unity"),
                "SampleScene must be preserved in Build Settings");
        }

        [Test]
        public void Ucak_IsAtSpecifiedPositionWithModelChild()
        {
            GameObject ucak = Root("Uçak");
            AssertVector(new Vector3(0f, 20f, 0f), ucak.transform.position, "Uçak position");
            AssertVector(Vector3.forward, ucak.transform.forward, "Uçak forward");

            Transform model = ucak.transform.Cast<Transform>().Single();
            Assert.AreEqual("NU_D_36", model.name);
            Assert.AreEqual(29, model.GetComponentsInChildren<MeshFilter>().Length, "Aircraft model meshes");
            AssertVector(ucak.transform.position, model.position, "Model world position");
        }

        [Test]
        public void Balon_IsSphereWithSpecifiedScaleAndPosition()
        {
            GameObject balon = Root("Balon");
            Assert.AreEqual("Sphere", balon.GetComponent<MeshFilter>().sharedMesh.name);
            AssertVector(new Vector3(40f, 30f, 60f), balon.transform.position, "Balon position");
            AssertVector(new Vector3(6f, 6f, 6f), balon.transform.localScale, "Balon scale");
        }

        [Test]
        public void Ucaksavar_IsCylinderAtOriginFacingPositiveZ()
        {
            GameObject ucaksavar = Root("Uçaksavar");
            Assert.AreEqual("Cylinder", ucaksavar.GetComponent<MeshFilter>().sharedMesh.name);
            AssertVector(Vector3.zero, ucaksavar.transform.position, "Uçaksavar position");
            AssertVector(Vector3.forward, ucaksavar.transform.forward, "Uçaksavar forward");
        }

        [Test]
        public void Components_AreWiredToTheIntendedTargets()
        {
            GameObject ucak = Root("Uçak");
            GameObject balon = Root("Balon");
            GameObject ucaksavar = Root("Uçaksavar");

            Assert.AreSame(balon.transform, ucak.GetComponent<HedefeGit>().Hedef);
            var yatis = ucak.GetComponent<KanatYatisi>();
            Assert.AreSame(balon.transform, yatis.Hedef);
            Assert.AreSame(ucak.transform.GetChild(0), yatis.Gorsel, "Roll must act on the model child, not the moving root");
            Assert.AreSame(ucak.transform, ucaksavar.GetComponent<Radar>().Hedef);
        }

        const float BalonRadius = 3f; // Unity sphere radius 0.5 × scale 6
        const float StepDt = 0.02f;

        // Flies the scene's own aircraft (in memory; the scene is closed without saving)
        // with its configured HedefeGit values until it arrives, and returns the stop position.
        Vector3 SimulateStop(GameObject ucak)
        {
            var git = ucak.GetComponent<HedefeGit>();
            for (int i = 0; i < 3000 && !git.HedefeVardi; i++)
                git.Adim(StepDt);
            Assert.IsTrue(git.HedefeVardi, "Aircraft did not arrive in the simulation");
            return ucak.transform.position;
        }

        // Distance from a point to a mesh's oriented bounding box (conservative: the box
        // contains the mesh, so a positive distance proves the mesh does not reach the point).
        static float DistanceToMeshBox(MeshFilter mf, Vector3 world)
        {
            Bounds b = mf.sharedMesh.bounds;
            Vector3 local = mf.transform.InverseTransformPoint(world);
            Vector3 closest = Vector3.Max(b.min, Vector3.Min(b.max, local));
            return Vector3.Distance(mf.transform.TransformPoint(closest), world);
        }

        [Test]
        public void Ucak_StopsOutsideBalon()
        {
            GameObject ucak = Root("Uçak");
            Vector3 balon = Root("Balon").transform.position;
            Vector3 stop = SimulateStop(ucak);

            float clearance = ucak.GetComponentsInChildren<MeshFilter>().Min(mf => DistanceToMeshBox(mf, balon)) - BalonRadius;
            TestContext.WriteLine($"Stop {stop:F2}, root-to-centre {Vector3.Distance(stop, balon):F2} m, " +
                                  $"closest aircraft part to Balon surface ≥ {clearance:F2} m");
            Assert.That(clearance, Is.GreaterThan(0.5f), "Aircraft must stop clear of Balon");
        }

        // Presentation check (D-021, D-023): the start point, the simulated stopping point (with
        // wingtip, nose and tail extents) and both targets stay inside the 16:9 frame with a 10 %
        // margin, and at the stop the aircraft is drawn beside Balon, not over it.
        [Test]
        public void MainCamera_FramesAircraftPathBalonAndUcaksavar()
        {
            Camera cam = Root("Main Camera").GetComponent<Camera>();
            cam.aspect = 16f / 9f;
            const float margin = 0.10f;
            const float wingspan = 9.74f;
            const int frameHeight = 720;

            GameObject ucak = Root("Uçak");
            Vector3 start = ucak.transform.position;
            Vector3 stop = SimulateStop(ucak);
            var extents = new[] { Vector3.zero, Vector3.right * 5f, Vector3.left * 5f, Vector3.forward * 4f, Vector3.back * 4f };

            var points = new System.Collections.Generic.List<(string name, Vector3 world)>
            {
                ("Balon top", Root("Balon").transform.position + Vector3.up * BalonRadius),
                ("Balon bottom", Root("Balon").transform.position - Vector3.up * BalonRadius),
                ("Uçaksavar base", Root("Uçaksavar").transform.position - Vector3.up),
                ("Uçaksavar top", Root("Uçaksavar").transform.position + Vector3.up),
            };
            foreach (Vector3 e in extents)
            {
                points.Add(($"Uçak start {e}", start + e));
                points.Add(($"Uçak stop {e}", stop + e));
            }
            foreach (var (name, world) in points)
            {
                Vector3 v = cam.WorldToViewportPoint(world);
                TestContext.WriteLine($"{name}: viewport ({v.x:F3}, {v.y:F3}) depth {v.z:F1} m, " +
                                      $"wingspan ≈ {wingspan * frameHeight / (2f * v.z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)):F0} px at 720p");
                Assert.That(v.z, Is.GreaterThan(cam.nearClipPlane), $"{name} is behind the camera");
                Assert.That(v.x, Is.InRange(margin, 1f - margin), $"{name} x outside frame");
                Assert.That(v.y, Is.InRange(margin, 1f - margin), $"{name} y outside frame");
            }

            // Screen-space gap at the stop: distance between the aircraft and Balon centres minus
            // Balon's projected radius and half the aircraft's projected wingspan.
            Vector3 balonCentre = Root("Balon").transform.position;
            Vector3 s = cam.WorldToViewportPoint(stop);
            Vector3 b = cam.WorldToViewportPoint(balonCentre);
            float pxPerUnitAtDepth(float depth) => frameHeight / (2f * depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            float centreDistancePx = Vector2.Distance(new Vector2(s.x * frameHeight * cam.aspect, s.y * frameHeight),
                                                      new Vector2(b.x * frameHeight * cam.aspect, b.y * frameHeight));
            float gapPx = centreDistancePx - BalonRadius * pxPerUnitAtDepth(b.z) - 0.5f * wingspan * pxPerUnitAtDepth(s.z);
            TestContext.WriteLine($"Screen gap between stopped aircraft and Balon ≈ {gapPx:F0} px at 720p");
            Assert.That(gapPx, Is.GreaterThan(0f), "Stopped aircraft overlaps Balon on screen");
        }

        [Test]
        public void ParentChild_ChildFollowsParentTransform()
        {
            Transform ucak = Root("Uçak").transform;
            Transform model = ucak.GetChild(0);
            Vector3 localBefore = model.localPosition;

            ucak.position += new Vector3(5f, -3f, 2f);
            ucak.rotation = Quaternion.Euler(0f, 90f, 0f);

            AssertVector(localBefore, model.localPosition, "Child local position");
            AssertVector(ucak.TransformPoint(localBefore), model.position, "Child world position");
            AssertVector(ucak.forward, model.forward, "Child forward follows parent");
        }
    }
}
