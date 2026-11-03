using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NU36.Tests
{
    /// <summary>Lab 4 phase 1: Rota04 configuration and proof that earlier scenes are untouched.</summary>
    public class Lab4SceneTests
    {
        const string Rota04Path = "Assets/Scenes/Rota04.unity";
        // MD5 recorded before Lab 4 phase 1 (2026-10-19).
        const string Rota03Md5 = "8d210ec5e8591cda4b0f48473db09e96";

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
            scene = EditorSceneManager.OpenScene(Rota04Path, OpenSceneMode.Additive);
            return scene.GetRootGameObjects();
        }

        static GameObject[] All(GameObject[] roots) =>
            roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();

        [Test]
        public void EarlierScenes_AreByteIdentical()
        {
            Assert.AreEqual("fee5968598add072519e4abffc067647", Md5("Assets/Scenes/Rota02.unity"));
            Assert.AreEqual(Rota03Md5, Md5("Assets/Scenes/Rota03.unity"));
            Assert.AreEqual("d6526b592015a080816b5f6ef43545b9", Md5("Assets/Scenes/SampleScene.unity"));
        }

        [Test]
        public void BuildSettings_KeepEarlierOrder_AndAppendRota04()
        {
            // Later work appends scenes (Rota04Hud); the Lab 4 phase 1 order must stay a prefix.
            string[] paths = EditorBuildSettings.scenes.Select(s => s.path).Take(4).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "Assets/Scenes/Rota02.unity", "Assets/Scenes/SampleScene.unity", "Assets/Scenes/Rota03.unity", Rota04Path
            }, paths);
        }

        [Test]
        public void Rota04_HasNoMissingScriptsOrReferences()
        {
            GameObject[] all = All(Open());
            Assert.AreEqual(0, all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount), "Missing scripts");
            foreach (Component c in all.SelectMany(g => g.GetComponents<Component>()))
            {
                var p = new SerializedObject(c).GetIterator();
                while (p.NextVisible(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null)
                        Assert.That(System.Collections.Generic.EqualityComparer<UnityEngine.EntityId>.Default.Equals(p.objectReferenceEntityIdValue, default), $"Missing reference {c.GetType().Name}.{p.propertyPath} on {c.name}");
            }
        }

        [Test]
        public void Rota04_KeepsLab3AndTerrain_AndAircraftHasOneKinematicRigidbodyAndACollider()
        {
            GameObject[] roots = Open();
            GameObject ucak = roots.Single(g => g.GetComponent<UcusKontrol>() != null);
            Assert.IsNotNull(ucak.GetComponent<Saglik>());
            Assert.IsNotNull(ucak.GetComponent<CanYazisi>());
            Assert.IsNotNull(roots.Single(g => g.name == "Main Camera").GetComponent<TakipKamerasi>());
            Assert.IsNotNull(roots.Single(g => g.name == "Arazi").GetComponent<SonsuzArazi>());
            Rigidbody[] rbs = ucak.GetComponentsInChildren<Rigidbody>();
            Assert.AreEqual(1, rbs.Length);
            Assert.IsTrue(rbs[0].isKinematic);
            Assert.IsFalse(rbs[0].useGravity);
            Collider[] cols = ucak.GetComponentsInChildren<Collider>();
            Assert.AreEqual(1, cols.Length);
            Assert.IsFalse(cols[0].isTrigger);
            Assert.That(cols[0].bounds.size.magnitude, Is.GreaterThan(3f));
        }

        [Test]
        public void Rota04_HasThreeRequiredTargetColumns_AtDifferentPlaces_WiredToTheManager()
        {
            GameObject[] roots = Open();
            GameObject oyun = roots.Single(g => g.name == "Oyun");
            var yonetici = oyun.GetComponent<GorevYoneticisi>();
            var so = new SerializedObject(yonetici);
            var arr = so.FindProperty("hedefler");
            Assert.AreEqual(3, arr.arraySize);
            var targets = Enumerable.Range(0, 3).Select(i => (KesifHedefi)arr.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            Assert.That(targets.All(t => t != null && t.Zorunlu && t.Puan == 100));
            Assert.AreEqual(3, targets.Distinct().Count());
            foreach (KesifHedefi t in targets)
            {
                var box = t.GetComponent<BoxCollider>();
                Assert.IsTrue(box.isTrigger);
                Assert.AreEqual(new Vector3(30f, 180f, 30f), box.size);
                Assert.AreEqual(new Vector3(0f, 90f, 0f), box.center);
                Assert.IsNotNull(t.Gorsel);
                Assert.IsNotNull(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject), "instance of the shared prefab");
            }
            for (int i = 0; i < 3; i++)
                for (int j = i + 1; j < 3; j++)
                    Assert.That(Vector3.Distance(targets[i].transform.position, targets[j].transform.position), Is.GreaterThan(300f));
            Assert.That(targets.All(t => t.transform.position.magnitude < 600f), "within ordinary flight range of the start");
            Assert.IsNotNull(oyun.GetComponent<PuanCuzdani>());
            Assert.IsNotNull(so.FindProperty("ussu").objectReferenceValue);
        }

        [Test]
        public void Rota04_BaseTrigger_IsWiredAndStartsDisabled()
        {
            GameObject[] roots = Open();
            GameObject us = roots.Single(g => g.name == "Us");
            Assert.IsFalse(us.activeSelf);
            Assert.IsTrue(us.GetComponent<BoxCollider>().isTrigger);
            var yonetici = roots.Single(g => g.name == "Oyun").GetComponent<GorevYoneticisi>();
            Assert.AreSame(yonetici, new SerializedObject(us.GetComponent<UsGirisi>()).FindProperty("yonetici").objectReferenceValue);
            var ekran = roots.Single(g => g.name == "Oyun").GetComponent<GorevSonuEkrani>();
            var so = new SerializedObject(ekran);
            Assert.AreSame(yonetici, so.FindProperty("yonetici").objectReferenceValue);
            Assert.IsNotNull(so.FindProperty("cuzdan").objectReferenceValue);
            Assert.AreEqual("Rota04", so.FindProperty("yenidenBaslatSahnesi").stringValue);
        }
    }
}
