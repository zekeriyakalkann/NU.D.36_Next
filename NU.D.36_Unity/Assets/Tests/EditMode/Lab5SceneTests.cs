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
    /// <summary>Lab 5: project layers and physics matrix, the Rota05 wiring, and proof that the earlier scenes are untouched.</summary>
    public class Lab5SceneTests
    {
        const string Path5 = Lab5SceneBuilder.ScenePath;

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
            scene = EditorSceneManager.OpenScene(Path5, OpenSceneMode.Additive);
            return scene.GetRootGameObjects();
        }

        T Tek<T>(GameObject[] roots) where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        T[] Hepsi<T>(GameObject[] roots) where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        static int Mask(params string[] ad) => LayerMask.GetMask(ad);

        // MD5 of the earlier scene files before Lab 5 (recorded 2026-11-03).
        [TestCase("Assets/Scenes/Rota02.unity", "fee5968598add072519e4abffc067647")]
        [TestCase("Assets/Scenes/Rota03.unity", "8d210ec5e8591cda4b0f48473db09e96")]
        [TestCase("Assets/Scenes/Rota04.unity", "af087d445491ebe41ba67b8854e84d5b")]
        [TestCase("Assets/Scenes/Rota04Hud.unity", "664d5637761682ac6303a6fb31f02c12")]
        [TestCase("Assets/Scenes/SampleScene.unity", "d6526b592015a080816b5f6ef43545b9")]
        public void EarlierScenes_AreByteIdentical(string path, string md5)
        {
            Assert.AreEqual(md5, Md5(path));
        }

        [Test]
        public void Rota05_IsInBuildSettings_AfterRota04Hud()
        {
            // Later labs append their scenes after Rota05 (Lab 6: Bolum3_GriKutu), so only the order is checked.
            EditorBuildSettingsScene[] s = EditorBuildSettings.scenes;
            int i = System.Array.FindIndex(s, x => x.path == Path5);
            Assert.GreaterOrEqual(i, 0);
            Assert.IsTrue(s[i].enabled);
            Assert.Greater(i, System.Array.FindIndex(s, x => x.path == "Assets/Scenes/Rota04Hud.unity"));
            CollectionAssert.IsSubsetOf(new[] { "Assets/Scenes/Rota02.unity", "Assets/Scenes/Rota03.unity", "Assets/Scenes/Rota04.unity",
                "Assets/Scenes/Rota04Hud.unity" }, s.Select(x => x.path).ToArray());
        }

        [Test]
        public void Layers_FiveWorksheetLayersExist()
        {
            for (int i = 0; i < Lab5SceneBuilder.Katmanlar.Length; i++)
                Assert.AreEqual(Lab5SceneBuilder.IlkKatman + i, LayerMask.NameToLayer(Lab5SceneBuilder.Katmanlar[i]), Lab5SceneBuilder.Katmanlar[i]);
        }

        [Test]
        public void CollisionMatrix_MermiMermiAndHedefAraziOff_OthersOn()
        {
            int mermi = LayerMask.NameToLayer("Mermi"), hedef = LayerMask.NameToLayer("Hedef"), arazi = LayerMask.NameToLayer("Arazi");
            int ucak = LayerMask.NameToLayer("Ucak"), yapi = LayerMask.NameToLayer("Yapi");
            Assert.IsTrue(Physics.GetIgnoreLayerCollision(mermi, mermi));
            Assert.IsTrue(Physics.GetIgnoreLayerCollision(hedef, arazi));
            Assert.IsFalse(Physics.GetIgnoreLayerCollision(ucak, arazi));
            Assert.IsFalse(Physics.GetIgnoreLayerCollision(ucak, yapi));
            Assert.IsFalse(Physics.GetIgnoreLayerCollision(0, 0));
        }

        [Test]
        public void Aircraft_LayerUcak_BodySphere24_BombLoadOf2_GroundProbeUsesColliders()
        {
            GameObject[] roots = Open();
            GameObject ucak = roots.Single(g => g.GetComponent<UcusKontrol>() != null);
            Assert.IsTrue(ucak.GetComponentsInChildren<Transform>(true).All(t => t.gameObject.layer == LayerMask.NameToLayer("Ucak")),
                "root and all children on Ucak");
            var kure = ucak.GetComponent<SphereCollider>();
            Assert.IsNotNull(kure);
            Assert.AreEqual(2.4f, kure.radius, 1e-5f);
            Assert.IsNull(ucak.GetComponent<BoxCollider>());
            Assert.IsTrue(ucak.GetComponent<Rigidbody>().isKinematic);
            var yuk = ucak.GetComponent<BombaYuku>();
            Assert.IsNotNull(yuk.BombaOn, "bombaOn = Bomba prefab");
            Assert.AreEqual(2, yuk.Kapasite);
            Assert.IsNotNull(ucak.GetComponent<ZeminBaglayici>());
            Assert.IsNotNull(ucak.GetComponent<Saglik>());
            Assert.IsNull(ucak.GetComponent<HedefeGit>());
        }

        [Test]
        public void Terrain_NamedCakilBurnu_LayerArazi_WithColliders_AndThreeHills()
        {
            GameObject[] roots = Open();
            GameObject arazi = roots.Single(g => g.GetComponent<SonsuzArazi>() != null);
            Assert.AreEqual("Çakıl Burnu Arazisi", arazi.name);
            Assert.AreEqual(LayerMask.NameToLayer("Arazi"), arazi.layer);
            Assert.IsTrue(arazi.GetComponent<SonsuzArazi>().Carpisici);

            Tepe[] tepeler = Hepsi<Tepe>(roots);
            CollectionAssert.AreEquivalent(new[] { "Tepe 1", "Tepe 2", "Tepe 3" }, tepeler.Select(t => t.name).ToArray());
            Tepe enYuksek = tepeler.OrderByDescending(t => t.Yukseklik).First();
            Assert.AreEqual("Tepe 3", enYuksek.name);
            Assert.AreEqual(230f, enYuksek.Yukseklik, 1e-4f);
            Assert.IsTrue(tepeler.All(t => t.gameObject.layer == LayerMask.NameToLayer("Arazi")));
        }

        [Test]
        public void Enemy_SoundLocatorAndThreeGuns_RadarMaskIsAraziYapiWithoutUcak()
        {
            GameObject[] roots = Open();
            UcusKontrol ucus = Tek<UcusKontrol>(roots);
            SesDinleyici kulak = Tek<SesDinleyici>(roots);
            Assert.AreSame(ucus, kulak.Ucak);
            Assert.AreEqual(343f, kulak.SesHizi, 1e-4f);

            UcaksavarMevzii[] toplar = Hepsi<UcaksavarMevzii>(roots);
            Assert.AreEqual(3, toplar.Length);
            foreach (UcaksavarMevzii t in toplar)
            {
                Assert.AreSame(kulak, t.Kulak);
                Assert.AreSame(ucus, t.Ucak);
                Assert.AreEqual(260f, t.MermiHizi, 1e-4f, "worksheet: mermiHizi 70 → 260");
                Assert.IsTrue(t.Supurme);
                Radar r = t.Goz;
                Assert.AreSame(t.GetComponent<Radar>(), r);
                Assert.AreSame(ucus.transform, r.Hedef);
                Assert.AreEqual(Mask("Arazi", "Yapi"), r.EngelKatmani.value, "engelKatmani = Arazi + Yapi, not the aircraft");
                Assert.AreEqual(3f, r.GozYuksekligi, 1e-4f);
                // the eye must be above the gun's own collider, or the ray would start inside it
                foreach (Collider c in t.GetComponentsInChildren<Collider>())
                    Assert.Less(c.bounds.max.y, r.Goz.y, c.name);
            }
        }

        [Test]
        public void Targets_FourDumpsOnLayerHedef_WiredToTheLevel()
        {
            GameObject[] roots = Open();
            MuhimmatMevzii[] mevziler = Hepsi<MuhimmatMevzii>(roots);
            Assert.AreEqual(4, mevziler.Length);
            Assert.IsTrue(mevziler.All(m => m.gameObject.layer == LayerMask.NameToLayer("Hedef") && m.GetComponent<Collider>() != null));

            SeviyeYoneticisi s = Tek<SeviyeYoneticisi>(roots);
            var so = new SerializedObject(s);
            SerializedProperty arr = so.FindProperty("mevziler");
            Assert.AreEqual(4, arr.arraySize);
            CollectionAssert.AreEquivalent(mevziler, Enumerable.Range(0, 4).Select(i => arr.GetArrayElementAtIndex(i).objectReferenceValue).ToArray());
            Assert.AreEqual(180f, s.BaslangicSuresi, 1e-4f);
            Assert.AreEqual(90f, s.IkmalEkSuresi, 1e-4f);
            Assert.AreEqual(Bolum.MuhimmatAvi, s.Bolum);
            Assert.IsNotNull(so.FindProperty("yuk").objectReferenceValue);
            Assert.IsNotNull(so.FindProperty("ucakSagligi").objectReferenceValue);
            Assert.IsNotNull(so.FindProperty("algilayici").objectReferenceValue);

            IkmalNoktasi us = Tek<IkmalNoktasi>(roots);
            Assert.AreSame(s, us.Yonetici);
            Assert.IsTrue(us.gameObject.activeSelf);
            Assert.IsTrue(us.GetComponent<Collider>().isTrigger);
            Assert.AreEqual("Rota05", Tek<SeviyeSonuEkrani>(roots).Sahne);
        }

        [Test]
        public void Lab4MissionObjects_AreRemoved_HudShowsTheLevel()
        {
            GameObject[] roots = Open();
            Assert.IsEmpty(Hepsi<GorevYoneticisi>(roots));
            Assert.IsEmpty(Hepsi<KesifHedefi>(roots));
            Assert.IsEmpty(Hepsi<UsGirisi>(roots));
            Assert.IsEmpty(Hepsi<HudMetin>(roots));
            SeviyeHud hud = Tek<SeviyeHud>(roots);
            Assert.IsNotNull(hud.DurumMetni);
            Assert.IsNotNull(hud.SureMetni);
            Assert.IsNotNull(Tek<DalisGostergesi>(roots).Yuk);
            Assert.AreEqual(50f, Hepsi<HudGosterge>(roots).Single(g => g.Kaynak == HudKaynak.Hiz).Azami, 1e-4f, "dial face is printed 0-50");
        }

        [Test]
        public void Prefabs_MasksMatchTheWorksheet()
        {
            var bomba = AssetDatabase.LoadAssetAtPath<Bomba>(Lab5SceneBuilder.BombaPrefabPath);
            var mermi = AssetDatabase.LoadAssetAtPath<Mermi>(Lab5SceneBuilder.MermiPrefabPath);
            Assert.IsNotNull(bomba);
            Assert.IsNotNull(mermi);
            Assert.IsNull(bomba.GetComponentInChildren<Collider>());
            Assert.IsNull(mermi.GetComponentInChildren<Collider>());
            var so = new SerializedObject(bomba);
            Assert.AreEqual(Mask("Arazi", "Yapi", "Hedef"), so.FindProperty("carpacak").intValue);
            Assert.AreEqual(120f, bomba.ParcaGuvenliIrtifa, 1e-4f);
            Assert.IsNotNull(so.FindProperty("patlamaOn").objectReferenceValue);
            Assert.AreEqual(LayerMask.NameToLayer("Mermi"), mermi.gameObject.layer);
        }

        [Test]
        public void Scene_HasNoMissingScripts()
        {
            GameObject[] roots = Open();
            int eksik = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            Assert.AreEqual(0, eksik);
        }
    }
}
