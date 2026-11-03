using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NU36.Tests
{
    /// <summary>Lab 4 phase 2: Rota04 wiring of the detector, restart handler and countdown; terrain surface function.</summary>
    public class Lab4Phase2SceneTests
    {
        Scene scene;

        [TearDown]
        public void Close()
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        [Test]
        public void Rota04_DetectorRestartHandlerAndCountdown_AreWired()
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/Rota04.unity", OpenSceneMode.Additive);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject ucak = roots.Single(g => g.GetComponent<UcusKontrol>() != null);
            GameObject oyun = roots.Single(g => g.name == "Oyun");

            var det = ucak.GetComponent<InisAlgilayici>();
            Assert.IsNotNull(det);
            Assert.AreEqual(1, ucak.GetComponents<InisAlgilayici>().Length);
            Assert.AreEqual(1, ucak.GetComponentsInChildren<Rigidbody>().Length, "still a single Rigidbody");
            var dso = new SerializedObject(det);
            Assert.AreSame(ucak.GetComponent<UcusKontrol>(), dso.FindProperty("ucus").objectReferenceValue);
            Assert.That(dso.FindProperty("tekerOfseti").floatValue, Is.InRange(0.3f, 4f));
            Assert.AreEqual(16f, dso.FindProperty("enFazlaHiz").floatValue);
            Assert.AreEqual(18f, dso.FindProperty("enFazlaPitchHatasi").floatValue);
            Assert.AreEqual(27f, dso.FindProperty("enFazlaYatis").floatValue);

            var yonetici = oyun.GetComponent<GorevYoneticisi>();
            var yso = new SerializedObject(yonetici);
            Assert.AreSame(det, yso.FindProperty("ucakAlgilayici").objectReferenceValue);
            Assert.AreEqual(150f, yso.FindProperty("sure").floatValue);

            var yb = oyun.GetComponent<YenidenBasla>();
            Assert.IsNotNull(yb);
            var bso = new SerializedObject(yb);
            Assert.AreSame(yonetici, bso.FindProperty("yonetici").objectReferenceValue);
            Assert.AreSame(ucak.GetComponent<UcusKontrol>(), bso.FindProperty("ucus").objectReferenceValue);
            Assert.AreEqual(2.5f, bso.FindProperty("bekleme").floatValue);
            Assert.AreEqual("Rota04", bso.FindProperty("sahne").stringValue);
        }

        [Test]
        public void SurfaceHeight_MatchesTheGeneratedMeshTriangles()
        {
            const int hucre = 16;
            const float boyut = 256f;
            int kose = AraziUretici.KoseSayisi(hucre);
            var v = new Vector3[kose]; var n = new Vector3[kose]; var c = new Color32[kose];
            var ornek = new float[(hucre + 1) * (hucre + 1)];
            int kontrol = 0;
            foreach (var (cx, cz) in new[] { (0, 0), (3, -2), (-5, 7), (40, 40) })
            {
                AraziUretici.ChunkUret(cx, cz, boyut, hucre, v, n, c, ornek, out _, out _);
                var rnd = new System.Random(cx * 31 + cz);
                for (int k = 0; k < 60; k++)
                {
                    int tri = rnd.Next(kose / 3) * 3;
                    // random point inside that triangle (barycentric), compared with the surface function
                    float a = (float)rnd.NextDouble(), b = (float)rnd.NextDouble();
                    if (a + b > 1f) { a = 1f - a; b = 1f - b; }
                    Vector3 p = v[tri] + a * (v[tri + 1] - v[tri]) + b * (v[tri + 2] - v[tri]);
                    float x = cx * boyut + p.x, z = cz * boyut + p.z;
                    Assert.AreEqual(p.y, AraziUretici.YuzeyYuksekligi(x, z), 0.02f, $"chunk ({cx},{cz}) triangle {tri / 3}");
                    kontrol++;
                }
            }
            Assert.AreEqual(240, kontrol);
        }
    }
}
