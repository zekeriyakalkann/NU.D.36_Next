using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using NU36.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NU36.Tests
{
    /// <summary>Lab 6 (Hafta 6): the Bölüm 3 map data follows the worksheet's design rules, and the grey box follows its sizes.</summary>
    public class Lab6Tests
    {
        Scene scene;

        [TearDown]
        public void Close()
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        static string Md5(string path)
        {
            using var md5 = MD5.Create();
            return string.Concat(md5.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2")));
        }

        GameObject[] Open()
        {
            scene = EditorSceneManager.OpenScene(Lab6SceneBuilder.ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene); // RenderSettings belong to the active scene
            return scene.GetRootGameObjects();
        }

        static Transform Bul(GameObject[] roots, string ad) =>
            roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == ad);

        static bool Komsu(string a, string b)
        {
            var (s1, r1) = Bolum3Harita.Coz(a);
            var (s2, r2) = Bolum3Harita.Coz(b);
            return Mathf.Max(Mathf.Abs(s1 - s2), Mathf.Abs(r1 - r2)) == 1;
        }

        /// <summary>True if the straight line between two square centres crosses a hill square (sampled every 10 m).</summary>
        static bool TepeArkasinda(string a, string b)
        {
            Vector3 p = Bolum3Harita.KareMerkezi(a), q = Bolum3Harita.KareMerkezi(b);
            int n = Mathf.CeilToInt(Vector3.Distance(p, q) / 10f);
            for (int i = 1; i < n; i++)
            {
                Vector3 x = Vector3.Lerp(p, q, (float)i / n);
                foreach (string t in Bolum3Harita.Tepeler)
                {
                    Vector3 c = Bolum3Harita.KareMerkezi(t);
                    if (Mathf.Abs(x.x - c.x) < 100f && Mathf.Abs(x.z - c.z) < 100f)
                        return true;
                }
            }
            return false;
        }

        // ---------------------------------------------------------------- map data (worksheet Step 02 rules)

        [Test]
        public void Grid_12x9_200m_CornersAroundGriKutu()
        {
            Assert.AreEqual(new Vector3(1900f, 0f, 800f), Bolum3Harita.KareMerkezi("A1"));
            Assert.AreEqual(new Vector3(4100f, 0f, -800f), Bolum3Harita.KareMerkezi("L9"));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Bolum3Harita.KareMerkezi("M1"));
        }

        [Test]
        public void Bridge_3Squares_AllOverTheRiver_Runway_3Squares()
        {
            Assert.AreEqual(3, Bolum3Harita.Kopru.Length);
            CollectionAssert.IsSubsetOf(Bolum3Harita.Kopru, Bolum3Harita.Nehir);
            Assert.IsTrue(Komsu(Bolum3Harita.Kopru[0], Bolum3Harita.Kopru[1]) && Komsu(Bolum3Harita.Kopru[1], Bolum3Harita.Kopru[2]));
            Assert.AreEqual(3, Bolum3Harita.Pist.Length);
            CollectionAssert.Contains(Bolum3Harita.Pist, Bolum3Harita.Baslangic);
        }

        [Test]
        public void Threats_3Searchlights_2To3Guns_1Locator_NothingOnTheRiver()
        {
            Assert.AreEqual(3, Bolum3Harita.Isildaklar.Length);
            Assert.That(Bolum3Harita.Ucaksavarlar.Length, Is.InRange(2, 3));
            Assert.IsNotEmpty(Bolum3Harita.SesDinleyici);
            string[] yerler = Bolum3Harita.Isildaklar.Select(i => i.kare).Concat(Bolum3Harita.Ucaksavarlar)
                .Append(Bolum3Harita.SesDinleyici).Concat(Bolum3Harita.Tepeler).ToArray();
            Assert.AreEqual(yerler.Length, yerler.Distinct().Count(), "one thing per square");
            CollectionAssert.IsEmpty(yerler.Intersect(Bolum3Harita.Nehir));
        }

        [Test]
        public void TwoRoutes_FromStartToTheBridge_OneBehindAHill_OneOpen()
        {
            foreach (string[] yol in new[] { Bolum3Harita.Yol1TepeArkasi, Bolum3Harita.Yol2Acik })
            {
                Assert.AreEqual(Bolum3Harita.Baslangic, yol.First());
                CollectionAssert.Contains(Bolum3Harita.Kopru, yol.Last());
                for (int i = 1; i < yol.Length; i++)
                    Assert.IsTrue(Komsu(yol[i - 1], yol[i]), $"{yol[i - 1]} → {yol[i]} is one move");
                CollectionAssert.IsEmpty(yol.Intersect(Bolum3Harita.Tepeler), "no route through a hill");
            }
            // Route 1: its low leg (row 8, E8–G8) is hidden from gun U1 (G4) by the F7–G7 ridge.
            foreach (string k in new[] { "E8", "F8", "G8" })
                Assert.IsTrue(TepeArkasinda(Bolum3Harita.Ucaksavarlar[0], k), k);
            // Route 2: every square is in the open as seen from U1.
            foreach (string k in Bolum3Harita.Yol2Acik)
                Assert.IsFalse(TepeArkasinda(Bolum3Harita.Ucaksavarlar[0], k), k);
        }

        [Test]
        public void SingleRule_OneSentence_WithANumber()
        {
            string kural = Bolum3Harita.TekKural;
            Assert.AreEqual(1, kural.Count(c => c == '.'), "one sentence");
            Assert.IsTrue(kural.Any(char.IsDigit), "with a number");
            Assert.AreEqual(3, Bolum3Harita.Bulutlar.Length);
        }

        // ---------------------------------------------------------------- grey box scene (Step 05)

        [TestCase("Assets/Scenes/Rota02.unity", "fee5968598add072519e4abffc067647")]
        [TestCase("Assets/Scenes/Rota03.unity", "8d210ec5e8591cda4b0f48473db09e96")]
        [TestCase("Assets/Scenes/Rota04.unity", "af087d445491ebe41ba67b8854e84d5b")]
        [TestCase("Assets/Scenes/Rota04Hud.unity", "664d5637761682ac6303a6fb31f02c12")]
        [TestCase("Assets/Scenes/Rota05.unity", "4680fa01303df2298a168652cb9cdd73")]
        public void EarlierScenes_AreByteIdentical(string path, string md5) => Assert.AreEqual(md5, Md5(path));

        [Test]
        public void GriKutu_IsLastInBuildSettings()
        {
            Assert.AreEqual(Lab6SceneBuilder.ScenePath, EditorBuildSettings.scenes.Last().path);
        }

        [Test]
        public void Ground_At3000_2400x1800_LayerArazi()
        {
            GameObject[] roots = Open();
            GameObject gri = roots.Single(g => g.name == "GriKutu");
            Assert.AreEqual(new Vector3(3000f, 0f, 0f), gri.transform.position);
            Transform zemin = Bul(roots, "Zemin");
            Assert.AreEqual(new Vector3(240f, 1f, 180f), zemin.localScale);
            Bounds b = zemin.GetComponent<Renderer>().bounds;
            Assert.AreEqual(2400f, b.size.x, 0.01f);
            Assert.AreEqual(1800f, b.size.z, 0.01f);
            Assert.AreEqual(LayerMask.NameToLayer("Arazi"), zemin.gameObject.layer);
        }

        [Test]
        public void RiverBridgeHills_SizesHeightsAndLayers()
        {
            GameObject[] roots = Open();
            int arazi = LayerMask.NameToLayer("Arazi");
            foreach (string k in Bolum3Harita.Nehir)
            {
                Transform n = Bul(roots, $"Nehir {k}");
                Assert.AreEqual(new Vector3(200f, 2f, 200f), n.localScale);
                Assert.AreEqual(Bolum3Harita.KareMerkezi(k).x, n.position.x, 0.01f);
            }
            foreach (string k in Bolum3Harita.Kopru)
            {
                Transform t = Bul(roots, $"Kopru {k}");
                Assert.AreEqual(new Vector3(200f, 20f, 60f), t.localScale);
                Assert.AreEqual(25f, t.position.y, 0.01f, "25 m above the river");
                Assert.AreEqual(arazi, t.gameObject.layer);
            }
            foreach (string k in Bolum3Harita.Tepeler)
            {
                Transform t = Bul(roots, $"Tepe {k}");
                Assert.AreEqual(new Vector3(200f, 120f, 200f), t.localScale);
                Assert.AreEqual(0f, t.GetComponent<Renderer>().bounds.min.y, 0.01f, "stands on the ground");
                Assert.AreEqual(arazi, t.gameObject.layer);
            }
        }

        [Test]
        public void Searchlights_Beam8x8x1200_StartsAtTheLight_AlongTheArrow()
        {
            GameObject[] roots = Open();
            for (int i = 0; i < Bolum3Harita.Isildaklar.Length; i++)
            {
                var (k, yon) = Bolum3Harita.Isildaklar[i];
                Transform g = Bul(roots, $"Isildak {i + 1} {k}");
                Transform isin = g.Find("Isin");
                Assert.AreEqual(new Vector3(8f, 8f, 1200f), isin.localScale);
                Vector3 d = Bolum3Harita.Yon(yon);
                Assert.Greater(Vector3.Dot(isin.forward, d), 0.9999f, "beam along the arrow");
                Vector3 uc = isin.position - isin.forward * 600f;
                Assert.Less(Vector2.Distance(new Vector2(uc.x, uc.z), new Vector2(g.position.x, g.position.z)), 0.01f, "one end at the searchlight");
                Assert.IsNull(isin.GetComponent<Collider>(), "a beam does not block rays");
                Assert.IsTrue(isin.GetComponent<Renderer>().sharedMaterial.renderQueue >= 3000, "semi-transparent");
            }
        }

        [Test]
        public void Guns_RangeDisc1600_Locator_Ring2000()
        {
            GameObject[] roots = Open();
            for (int i = 0; i < Bolum3Harita.Ucaksavarlar.Length; i++)
            {
                Transform g = Bul(roots, $"Ucaksavar {i + 1} {Bolum3Harita.Ucaksavarlar[i]}");
                Assert.AreEqual(new Vector3(1600f, 0.1f, 1600f), g.Find("Menzil").localScale);
            }
            Transform s = Bul(roots, "SesDinleyici " + Bolum3Harita.SesDinleyici);
            Assert.AreEqual(2000f, s.Find("Menzil").GetComponent<Renderer>().bounds.size.x, 1f, "ring Ø 2000 m");
        }

        [Test]
        public void Aircraft_OverTheRunwayAtAlcak_Level2Off_StopwatchWired()
        {
            GameObject[] roots = Open();
            GameObject ucak = roots.Single(g => g.GetComponent<UcusKontrol>() != null);
            Vector3 c = Bolum3Harita.KareMerkezi(Bolum3Harita.Baslangic);
            Assert.AreEqual(c.x, ucak.transform.position.x, 0.01f);
            Assert.AreEqual(c.z, ucak.transform.position.z, 0.01f);
            Assert.AreEqual(Bolum3Harita.Alcak, ucak.transform.position.y, 0.01f);
            foreach (string ad in Lab6SceneBuilder.KapatilanKokler)
                Assert.IsFalse(roots.Single(g => g.name == ad).activeSelf, ad);
            var k = roots.SelectMany(r => r.GetComponentsInChildren<Bolum3Kronometre>()).Single();
            var so = new SerializedObject(k);
            Assert.AreSame(ucak.transform, so.FindProperty("ucak").objectReferenceValue);
            Assert.AreEqual(3, so.FindProperty("kopru").arraySize);
            Assert.AreEqual(1, so.FindProperty("pist").arraySize);
            Assert.AreEqual(4000f, RenderSettings.fogEndDistance, 0.01f);
        }

        [Test]
        public void TurKarsiligi_15sPerTurn()
        {
            Assert.AreEqual(4f, Bolum3Kronometre.TurKarsiligi(60f), 1e-5f);
        }
    }
}
