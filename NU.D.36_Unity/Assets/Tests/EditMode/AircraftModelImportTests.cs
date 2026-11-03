using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36.Tests
{
    /// <summary>
    /// Verifies that the FBX exported from the protected Blender source imports into
    /// Unity with the expected content, scale and orientation. Reference values come
    /// from Tools/Blender/inspect_scene.py (Logs/blender_inspect.json).
    /// </summary>
    public class AircraftModelImportTests
    {
        const string ModelPath = "Assets/Models/NU_D_36.fbx";

        // MESH objects in the Blender "NU.D.36" collection.
        const int ExpectedMeshCount = 29;

        // Overall extents in Blender (X span, Z height, Y length), in metres,
        // mapped to Unity axes (X, Y, Z).
        static readonly Vector3 ExpectedSize = new Vector3(9.74f, 2.44f, 7.73f);
        const float SizeTolerance = 0.02f;
        const float TransformTolerance = 1e-4f;

        GameObject model;

        [OneTimeSetUp]
        public void LoadModel()
        {
            model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        }

        static Bounds WorldBounds(MeshFilter mf)
        {
            Bounds local = mf.sharedMesh.bounds;
            Matrix4x4 m = mf.transform.localToWorldMatrix;
            var b = new Bounds(m.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? local.min.x : local.max.x,
                    (i & 2) == 0 ? local.min.y : local.max.y,
                    (i & 4) == 0 ? local.min.z : local.max.z);
                b.Encapsulate(m.MultiplyPoint3x4(corner));
            }
            return b;
        }

        Bounds PartBounds(string name)
        {
            MeshFilter mf = model.GetComponentsInChildren<MeshFilter>().Single(f => f.name == name);
            return WorldBounds(mf);
        }

        [Test]
        public void ModelAsset_IsImported()
        {
            Assert.IsNotNull(model, $"No model asset at {ModelPath}");
        }

        [Test]
        public void Model_ContainsAllAircraftMeshesAndNothingElse()
        {
            MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>();
            Assert.AreEqual(ExpectedMeshCount, filters.Length);
            Assert.That(filters.All(f => f.sharedMesh != null), "Every MeshFilter needs a mesh");
            Assert.That(filters.All(f => f.name.StartsWith("NU36_")), "Unexpected non-aircraft mesh");
            Assert.IsEmpty(model.GetComponentsInChildren<Camera>());
            Assert.IsEmpty(model.GetComponentsInChildren<Light>());
            Assert.IsNull(model.GetComponentInChildren<Animator>());
        }

        [Test]
        public void Model_TransformsAreUnrotatedAndUnscaled()
        {
            foreach (Transform t in model.GetComponentsInChildren<Transform>())
            {
                Assert.That(Quaternion.Angle(t.localRotation, Quaternion.identity), Is.LessThan(0.01f),
                    $"{t.name} has rotation {t.localEulerAngles}");
                Assert.That((t.localScale - Vector3.one).magnitude, Is.LessThan(TransformTolerance),
                    $"{t.name} has scale {t.localScale}");
            }
        }

        [Test]
        public void Model_OverallSizeMatchesBlenderSourceInMetres()
        {
            MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>();
            Bounds total = WorldBounds(filters[0]);
            foreach (MeshFilter mf in filters.Skip(1))
                total.Encapsulate(WorldBounds(mf));

            TestContext.WriteLine($"Combined bounds: center={total.center:F3} size={total.size:F3}");
            Assert.That(total.size.x, Is.EqualTo(ExpectedSize.x).Within(SizeTolerance), "Wingspan (X)");
            Assert.That(total.size.y, Is.EqualTo(ExpectedSize.y).Within(SizeTolerance), "Height (Y)");
            Assert.That(total.size.z, Is.EqualTo(ExpectedSize.z).Within(SizeTolerance), "Length (Z)");
        }

        [Test]
        public void Model_NosePointsAlongPositiveZAndWingsStackAlongY()
        {
            Bounds propeller = PartBounds("NU36_Propeller");
            Bounds rudder = PartBounds("NU36_Control_Rudder");
            Bounds upperWing = PartBounds("NU36_Upper_Wing");
            Bounds lowerWing = PartBounds("NU36_Lower_Wing");

            Assert.That(propeller.center.z, Is.GreaterThan(rudder.center.z), "Propeller must be ahead of rudder");
            Assert.That(propeller.center.z, Is.GreaterThan(0f), "Nose should be on +Z");
            Assert.That(upperWing.center.y, Is.GreaterThan(lowerWing.center.y), "Upper wing must be above lower wing");
        }

        [Test]
        public void Model_LeftAndRightPartsAreMirroredAcrossX()
        {
            string[] pairs = { "NU36_Control_Aileron", "NU36_Control_Elevator", "NU36_Gear_Wheel" };
            foreach (string pair in pairs)
            {
                Bounds r = PartBounds(pair + "_R");
                Bounds l = PartBounds(pair + "_L");
                TestContext.WriteLine($"{pair}: _R x={r.center.x:F3}, _L x={l.center.x:F3}");
                Assert.That(r.center.x, Is.EqualTo(-l.center.x).Within(SizeTolerance), pair);
                Assert.That(r.center.x, Is.Not.EqualTo(0f).Within(SizeTolerance), pair);
            }
        }

        [Test]
        public void Model_EveryRendererHasOneMaterialPerSubmesh()
        {
            foreach (MeshRenderer r in model.GetComponentsInChildren<MeshRenderer>())
            {
                Mesh mesh = r.GetComponent<MeshFilter>().sharedMesh;
                Assert.AreEqual(mesh.subMeshCount, r.sharedMaterials.Length, r.name);
                Assert.That(r.sharedMaterials.All(m => m != null), $"{r.name} has a missing material");
                foreach (Material m in r.sharedMaterials)
                    TestContext.WriteLine($"{r.name}: {m.name} shader={m.shader.name} renderQueue={m.renderQueue}");
            }
        }

        // Blender: NU36_MAT_Glass_Transparent, Principled BSDF alpha 0.15, used by the two
        // windscreen meshes (Logs/blender_inspect.json). Checks how URP imported it.
        [Test]
        public void GlassMaterial_IsImportedAsTransparentOnWindscreens()
        {
            string[] windscreens = { "NU36_Cockpit_Forward_Windscreen", "NU36_Cockpit_Rear_Windscreen" };
            MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
            MeshRenderer[] glassUsers = renderers
                .Where(r => r.sharedMaterials.Any(m => m != null && m.name == "NU36_MAT_Glass_Transparent")).ToArray();
            CollectionAssert.AreEquivalent(windscreens, glassUsers.Select(r => r.name).ToArray());

            Material glass = glassUsers[0].sharedMaterials.First(m => m.name == "NU36_MAT_Glass_Transparent");
            Bounds size = glassUsers[0].GetComponent<MeshFilter>().sharedMesh.bounds;
            float surface = glass.HasProperty("_Surface") ? glass.GetFloat("_Surface") : -1f;
            Color baseColor = glass.HasProperty("_BaseColor") ? glass.GetColor("_BaseColor") : Color.clear;
            TestContext.WriteLine($"shader={glass.shader.name} _Surface={surface} (0 opaque, 1 transparent) " +
                                  $"_BaseColor={baseColor} renderQueue={glass.renderQueue} " +
                                  $"keywords=[{string.Join(", ", glass.shaderKeywords)}] meshSize={size.size:F3}");

            Assert.AreEqual(1f, surface, "URP surface type should be Transparent");
            Assert.That(baseColor.a, Is.EqualTo(0.15f).Within(0.01f), "Alpha should match the Blender material");
            Assert.That(glass.renderQueue, Is.GreaterThanOrEqualTo(3000), "Transparent render queue");
        }

        // The URP template assigns the pipeline per quality level (Mobile, PC),
        // not in GraphicsSettings, so every quality level is checked.
        [Test]
        public void Project_UsesUniversalRenderPipelineOnEveryQualityLevel()
        {
            Assert.IsNotNull(GraphicsSettings.currentRenderPipeline, "No active render pipeline");
            for (int i = 0; i < QualitySettings.count; i++)
            {
                RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(i);
                string level = QualitySettings.names[i];
                Assert.IsNotNull(asset, $"Quality level {level} has no render pipeline asset");
                StringAssert.Contains("Universal", asset.GetType().Name, level);
            }
        }
    }
}
