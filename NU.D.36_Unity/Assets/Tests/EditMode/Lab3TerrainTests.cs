using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NU36.Tests
{
    /// <summary>Lab 3 terrain: generator maths, chunk seams, Rota03 wiring, and untouched Rota02 / SampleScene.</summary>
    public class Lab3TerrainTests
    {
        const string Rota03Path = "Assets/Scenes/Rota03.unity";
        // MD5 of the scene files before the terrain work (recorded 2026-10-19).
        const string Rota02Md5 = "fee5968598add072519e4abffc067647";
        const string SampleSceneMd5 = "d6526b592015a080816b5f6ef43545b9";

        Scene scene;

        [TearDown]
        public void Close()
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        static string Md5(string assetPath)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(File.ReadAllBytes(assetPath));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        [Test]
        public void Rota02AndSampleScene_AreByteIdentical()
        {
            Assert.AreEqual(Rota02Md5, Md5("Assets/Scenes/Rota02.unity"));
            Assert.AreEqual(SampleSceneMd5, Md5("Assets/Scenes/SampleScene.unity"));
        }

        [Test]
        public void Height_IsDeterministicAndBounded()
        {
            for (int i = 0; i < 200; i++)
            {
                float x = (i * 137.31f) - 9000f, z = (i * 71.77f) - 4000f;
                float h = AraziUretici.Yukseklik(x, z);
                Assert.AreEqual(h, AraziUretici.Yukseklik(x, z));
                Assert.That(h, Is.InRange(-40f, 450f), $"({x},{z})");
            }
        }

        [Test]
        public void Height_IsLevelAtOrigin_SoUcaksavarStandsOnGround()
        {
            Assert.AreEqual(AraziUretici.BaslangicYuksekligi, AraziUretici.Yukseklik(0f, 0f), 1e-4f);
            // Balon (40, 30, 60) and the aircraft start (0, 20, 0) stay well above the ground.
            Assert.That(AraziUretici.Yukseklik(40f, 60f), Is.LessThan(10f));
            Assert.That(AraziUretici.Yukseklik(0f, 0f), Is.LessThan(0f));
        }

        [Test]
        public void Height_StaysBelowFlightAltitudeNearStart()
        {
            for (float r = 0f; r <= 600f; r += 25f)
                for (int a = 0; a < 24; a++)
                {
                    float ang = a * Mathf.PI / 12f;
                    Assert.That(AraziUretici.Yukseklik(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r), Is.LessThan(25f), $"r={r} a={a}");
                }
        }

        [Test]
        public void Mountains_ExistFarFromStart_AndAreLoggedBySector()
        {
            var sb = new StringBuilder("angle_deg,max_height_at_r800_1400\n");
            int withMountains = 0;
            for (int a = 0; a < 24; a++)
            {
                float ang = a * Mathf.PI / 12f, best = float.MinValue;
                for (float r = 800f; r <= 1400f; r += 50f)
                    best = Mathf.Max(best, AraziUretici.Yukseklik(Mathf.Sin(ang) * r, Mathf.Cos(ang) * r)); // angle 0 = +Z
                if (best > 80f) withMountains++;
                sb.AppendLine($"{a * 15},{best:F1}");
            }
            Lab3Kayit.Yaz("arazi_daglar_sektor.csv", sb.ToString());
            Assert.That(withMountains, Is.InRange(4, 20), "Mountains in some, but not all, directions around the start");
            float forward = Enumerable.Range(-3, 7).Max(k =>
                Enumerable.Range(0, 13).Max(s => AraziUretici.Yukseklik(Mathf.Sin(k * 0.1f) * (800f + s * 50f), Mathf.Cos(k * 0.1f) * (800f + s * 50f))));
            Assert.That(forward, Is.GreaterThan(80f), "A mountain range should be visible ahead (+Z) of the start");
        }

        [Test]
        public void NeighbouringChunks_ShareIdenticalEdgeHeights()
        {
            const int hucre = 16;
            const float boyut = 256f;
            int kose = AraziUretici.KoseSayisi(hucre);
            var (v, n, c) = (new Vector3[kose], new Vector3[kose], new Color32[kose]);
            var ornekA = new float[(hucre + 1) * (hucre + 1)];
            var ornekB = new float[(hucre + 1) * (hucre + 1)];
            var ornekC = new float[(hucre + 1) * (hucre + 1)];
            AraziUretici.ChunkUret(-3, 4, boyut, hucre, v, n, c, ornekA, out _, out _);
            AraziUretici.ChunkUret(-2, 4, boyut, hucre, v, n, c, ornekB, out _, out _);
            AraziUretici.ChunkUret(-3, 5, boyut, hucre, v, n, c, ornekC, out _, out _);
            int w = hucre + 1;
            for (int j = 0; j < w; j++)
                Assert.AreEqual(ornekA[j * w + hucre], ornekB[j * w], 0f, $"east/west edge row {j}");
            for (int i = 0; i < w; i++)
                Assert.AreEqual(ornekA[hucre * w + i], ornekC[i], 0f, $"north/south edge column {i}");
        }

        [Test]
        public void ChunkMesh_IsValid_FacesUp_AndColoursAreOpaque()
        {
            const int hucre = 16;
            int kose = AraziUretici.KoseSayisi(hucre);
            var v = new Vector3[kose]; var n = new Vector3[kose]; var c = new Color32[kose];
            var ornek = new float[(hucre + 1) * (hucre + 1)];
            foreach (var (cx, cz) in new[] { (0, 0), (5, -7), (-40, 22) })
            {
                AraziUretici.ChunkUret(cx, cz, 256f, hucre, v, n, c, ornek, out float lo, out float hi);
                Assert.That(lo, Is.LessThanOrEqualTo(hi));
                for (int i = 0; i < kose; i += 3)
                {
                    Vector3 cross = Vector3.Cross(v[i + 1] - v[i], v[i + 2] - v[i]);
                    Assert.That(cross.y, Is.GreaterThan(0f), "Triangle must wind so that it faces up");
                    Assert.That((cross.normalized - n[i]).magnitude, Is.LessThan(1e-4f));
                    Assert.That(n[i].y, Is.GreaterThan(0f));
                    Assert.AreEqual(255, c[i].a);
                    Assert.That(float.IsNaN(v[i].y) || float.IsInfinity(v[i].y), Is.False);
                    Assert.That(v[i].y, Is.InRange(lo - 1e-3f, hi + 1e-3f));
                }
            }
        }

        [Test]
        public void Rota03_HasTerrainSkyFogAndFarClip_Wired()
        {
            scene = EditorSceneManager.OpenScene(Rota03Path, OpenSceneMode.Additive);
            GameObject arazi = scene.GetRootGameObjects().Single(g => g.name == "Arazi");
            var sonsuz = arazi.GetComponent<SonsuzArazi>();
            GameObject ucak = scene.GetRootGameObjects().Single(g => g.name == "Uçak");
            Assert.IsNotNull(sonsuz);
            Assert.AreSame(ucak.transform, sonsuz.Hedef);
            Assert.AreEqual("NU36/LowPolyTerrain", sonsuz.Materyal.shader.name);
            Assert.AreEqual(0, arazi.transform.childCount, "Chunks are created at runtime, not saved in the scene");
            Assert.IsNull(arazi.GetComponent<Collider>());
            Camera cam = scene.GetRootGameObjects().Single(g => g.name == "Main Camera").GetComponent<Camera>();
            Assert.AreEqual(2200f, cam.farClipPlane);
        }

        [Test]
        public void Rota02_HasNoTerrainFogOrSkyChange()
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/Rota02.unity", OpenSceneMode.Additive);
            Assert.IsFalse(scene.GetRootGameObjects().Any(g => g.name == "Arazi" || g.GetComponentInChildren<SonsuzArazi>() != null));
        }
    }
}
