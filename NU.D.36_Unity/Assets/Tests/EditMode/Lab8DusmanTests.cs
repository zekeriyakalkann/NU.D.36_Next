using System.Linq;
using NUnit.Framework;
using NU36.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using H = NU36.KursunDaglariHaritasi;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 8 (Hafta 8) phase 2: the enemies, weapons and damage rules in the scene and as pure functions
    /// (Documentation/Lab8_Plan.md §9: TEST-811, TEST-814, the static parts of TEST-817, TEST-819, TEST-820, TEST-802 for the
    /// tank and the enemy aircraft).
    /// </summary>
    public class Lab8DusmanTests
    {
        Scene scene;
        GameObject gecici;

        [TearDown]
        public void Close()
        {
            if (gecici != null) Object.DestroyImmediate(gecici);
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        GameObject[] Open()
        {
            scene = EditorSceneManager.OpenScene(Lab8SceneBuilder.ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            return scene.GetRootGameObjects();
        }

        static T[] Hepsi<T>(GameObject[] roots) where T : Component =>
            roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        static T Tek<T>(GameObject[] roots) where T : Component => Hepsi<T>(roots).Single();

        static int Katman(string ad) => LayerMask.NameToLayer(ad);

        // ---- damage rules (pure) -------------------------------------------------------------------------------------

        [Test]
        public void Health_SameSideIsNotHurt_ExplosionOnlyIgnoresRounds_NeutralHurtsAll()
        {
            gecici = new GameObject("saglik");
            var s = gecici.AddComponent<Saglik>();
            s.BaslangicAyarla(100);
            s.Taraf = Taraf.Dost;
            Assert.IsFalse(s.Vur(10, HasarTuru.Mermi, Taraf.Dost), "own side's round");
            Assert.IsFalse(s.Vur(10, HasarTuru.Patlama, Taraf.Dost), "own side's bomb");
            Assert.AreEqual(100, s.Can);
            Assert.IsTrue(s.Vur(10, HasarTuru.Mermi, Taraf.Dusman));
            Assert.IsTrue(s.Vur(10, HasarTuru.Mermi, Taraf.Notr), "no side = Labs 5–7");
            Assert.AreEqual(80, s.Can);

            s.YalnizPatlama = true;
            HasarTuru? sekme = null;
            s.Sekti += t => sekme = t;
            Assert.IsFalse(s.Vur(10, HasarTuru.Mermi, Taraf.Dusman), "a round glances off armour");
            Assert.AreEqual(HasarTuru.Mermi, sekme);
            Assert.IsTrue(s.Vur(30, HasarTuru.Patlama, Taraf.Dusman), "an explosion hurts");
            Assert.AreEqual(50, s.Can);

            // The old call (Labs 3–7) is unchanged.
            s.Vur(10);
            Assert.AreEqual(40, s.Can);
        }

        [Test]
        public void TankLead_ShellMeetsAMovingTarget()
        {
            Vector3 cikis = new Vector3(0f, 3f, 0f), hedef = new Vector3(600f, 1.5f, 200f), hiz = new Vector3(0f, 0f, 16.6f);
            const float v = 320f;
            Vector3 n = Tank.OncuNokta(cikis, hedef, hiz, v);
            float t = Vector3.Distance(cikis, n) / v;
            Assert.That(Vector3.Distance(hedef + hiz * t, n), Is.LessThan(0.05f), "the shell and the train meet");
            var r = new System.Random(3);
            for (int i = 0; i < 200; i++)
                Assert.LessOrEqual(Vector3.Angle(Vector3.forward, Tank.Sac(Vector3.forward, 2f, r)), 2.001f, "scatter inside its cone");
        }

        [Test]
        public void EnemyAircraft_FiresOnlyInsideItsConeAndRange()
        {
            Vector3 p = Vector3.zero, ileri = Vector3.forward;
            Assert.IsTrue(DusmanUcagi.AtisMenzilinde(p, ileri, new Vector3(0f, 0f, 400f), 8f, 500f));
            Assert.IsTrue(DusmanUcagi.AtisMenzilinde(p, ileri, Quaternion.Euler(0f, 7f, 0f) * Vector3.forward * 450f, 8f, 500f));
            Assert.IsFalse(DusmanUcagi.AtisMenzilinde(p, ileri, Quaternion.Euler(0f, 9f, 0f) * Vector3.forward * 300f, 8f, 500f), "outside ±8°");
            Assert.IsFalse(DusmanUcagi.AtisMenzilinde(p, ileri, new Vector3(0f, 0f, 520f), 8f, 500f), "beyond 500 m");
        }

        // ---- scene: zones and prefabs (TEST-811, TEST-814) ---------------------------------------------------------------

        [Test]
        public void Scene_ThreeZones_WorksheetCounts_AllHiddenUntilTheTrainComes()
        {
            GameObject[] roots = Open();
            AtesBolgesi[] b = Hepsi<AtesBolgesi>(roots).OrderBy(x => x.Bas).ToArray();
            Assert.AreEqual(3, b.Length);
            int T(AtesBolgesi z) => z.YerBirimleri.Count(g => g.GetComponent<Tank>() != null);
            int U(AtesBolgesi z) => z.YerBirimleri.Count(g => g.GetComponent<UcaksavarMevzii>() != null);
            CollectionAssert.AreEqual(new[] { 2, 0, 2 }, b.Select(T), "tanks per zone");
            CollectionAssert.AreEqual(new[] { 1, 2, 1 }, b.Select(U), "AA guns per zone");
            CollectionAssert.AreEqual(new[] { 0, 2, 2 }, b.Select(z => z.Ucaklar.Length), "aircraft per zone (two waves)");
            Assert.AreEqual(12, b.Sum(z => z.Toplam), "12 fire sources");
            var tren = Tek<Tren>(roots);
            foreach (AtesBolgesi z in b)
            {
                Assert.AreSame(tren, z.Tren);
                Assert.AreEqual(H.BolgeOnden, z.Onden);
                Assert.Greater(z.Bas - z.Onden, H.TrenBaslangici, $"{z.Ad}: nothing shows up at the start");
            }
            for (int i = 1; i < b.Length; i++)
                Assert.Greater(b[i].Bas - b[i].Onden, b[i - 1].Son + 200f, $"calm stretch of over 200 m before {b[i].Ad}");
            var gorev = Tek<KursunDaglariGorevi>(roots);
            CollectionAssert.AreEquivalent(b, gorev.Bolgeler);
        }

        [Test]
        public void Scene_EveryEnemyIsAnInstanceOfItsTypesPrefab()
        {
            GameObject[] roots = Open();
            var tankOn = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.TankYolu);
            var aaOn = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.UcaksavarYolu);
            var ucakOn = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.DusmanUcagiYolu);
            void Kontrol<T>(GameObject on, int beklenen) where T : Component
            {
                T[] hepsi = Hepsi<T>(roots);
                Assert.AreEqual(beklenen, hepsi.Length, typeof(T).Name);
                foreach (T c in hepsi)
                {
                    Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(c.gameObject), $"{c.name} is a prefab instance");
                    Assert.AreSame(on, PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject), $"{c.name} comes from {on.name}.prefab");
                    Assert.IsEmpty(PrefabUtility.GetAddedComponents(c.gameObject), $"{c.name}: no components added in the scene");
                }
            }
            Kontrol<Tank>(tankOn, 4);
            Kontrol<UcaksavarMevzii>(aaOn, 4);
            Kontrol<DusmanUcagi>(ucakOn, 4);
        }

        [Test]
        public void Prefabs_Rules_TankOnlyByExplosion_AaCeiling_Sides_Layers()
        {
            var tank = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.TankYolu);
            var aa = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.UcaksavarYolu);
            var ucak = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.DusmanUcagiYolu);
            int hedef = Katman("Hedef");
            foreach (GameObject g in new[] { tank, aa, ucak })
            {
                Assert.AreEqual(hedef, g.layer, $"{g.name} on Hedef (bombs and rounds reach it)");
                Assert.AreEqual(Taraf.Dusman, g.GetComponent<Saglik>().Taraf, g.name);
                Assert.IsNotNull(g.GetComponent<Collider>(), $"{g.name} can be hit");
                Assert.IsNotNull(g.GetComponent<Susturulabilir>(), $"{g.name} can be silenced");
            }
            Assert.IsTrue(tank.GetComponent<Saglik>().YalnizPatlama, "worksheet 06: a tank only by a bomb");
            Assert.IsFalse(aa.GetComponent<Saglik>().YalnizPatlama, "AA by the machine gun");
            Assert.IsFalse(ucak.GetComponent<Saglik>().YalnizPatlama, "aircraft by the machine gun");
            Assert.AreEqual(DusmanTuru.Tank, tank.GetComponent<Susturulabilir>().Tur);
            Assert.AreEqual(DusmanTuru.Ucaksavar, aa.GetComponent<Susturulabilir>().Tur);
            Assert.AreEqual(DusmanTuru.Ucak, ucak.GetComponent<Susturulabilir>().Tur);
            Assert.AreEqual(H.UcaksavarTavani, aa.GetComponent<UcaksavarMevzii>().AtesTavani, "worksheet 05: AA holds fire high up");
            Assert.AreEqual(Taraf.Dusman, aa.GetComponent<UcaksavarMevzii>().Taraf);
            Assert.IsNotNull(tank.GetComponent<Susturulabilir>().Duman, "silenced tank smokes");
            Assert.IsNotNull(aa.GetComponent<Susturulabilir>().Duman, "silenced AA smokes");
            Assert.IsTrue(tank.transform.Find("Kule/Namlu Ucu") != null, "tank turret pivot with the muzzle");
        }

        [Test]
        public void Prefabs_RealisticSizes_TankAndEnemyAircraft()
        {
            Bounds Sinir(GameObject on)
            {
                var g = (GameObject)PrefabUtility.InstantiatePrefab(on);
                gecici = g;
                Renderer[] rs = g.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer) && !(r is TrailRenderer)).ToArray();
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs) b.Encapsulate(r.bounds);
                return b;
            }
            Bounds t = Sinir(AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.TankYolu));
            Object.DestroyImmediate(gecici);
            Bounds u = Sinir(AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.DusmanUcagiYolu));
            Assert.That(t.size.z, Is.InRange(6.5f, 10f), "tank: 7 m hull, gun forward");
            Assert.That(t.size.y, Is.InRange(2f, 4.5f), "tank height");
            Assert.That(u.size.x, Is.EqualTo(Lab8Dusmanlar.DusmanKanatAcikligi).Within(0.5f), "enemy aircraft span 11 m");
            Assert.That(u.size.z, Is.InRange(7f, 11f), "enemy aircraft length");
        }

        [Test]
        public void Scene_PlayerWeapons_1200Rounds_2Bombs_Ceiling_SidesOfAircraftAndTrain()
        {
            GameObject[] roots = Open();
            var ucus = Tek<UcusKontrol>(roots);
            var mk = ucus.GetComponent<Makineli>();
            Assert.IsNotNull(mk, "the aircraft has machine guns");
            Assert.AreEqual(1200, mk.Kapasite, "worksheet 06: 1 200 rounds");
            Assert.AreEqual(2, ucus.GetComponent<BombaYuku>().Kapasite, "worksheet 06: 2 bombs");
            Assert.AreEqual(H.BombaTavani, ucus.GetComponent<BombaYuku>().BirakmaTavani, "no release above 300 m");
            Assert.AreEqual(10, mk.Hasar);
            Assert.AreEqual(16f, mk.AtisHizi);
            Assert.AreEqual(1000f, mk.Menzil);
            int maske = mk.Vurulacak;
            Assert.AreEqual(0, maske & (1 << Katman("Ucak")), "own aircraft not in the mask");
            Assert.AreNotEqual(0, maske & (1 << Katman("Hedef")), "enemies in the mask");
            Assert.AreEqual(Taraf.Dost, ucus.GetComponent<Saglik>().Taraf);
            Assert.AreEqual(Taraf.Dost, Tek<Tren>(roots).Saglik.Taraf, "the player cannot hurt the train");
            var so = new SerializedObject(mk);
            var mermi = (Mermi)so.FindProperty("mermiOn").objectReferenceValue;
            Assert.IsNotNull(mermi, "tracer round");
            Assert.IsNotNull(mermi.GetComponent<TrailRenderer>(), "the round draws a tracer line (worksheet 07)");
            Assert.IsNotNull(so.FindProperty("namluAlevi").objectReferenceValue, "muzzle flash (worksheet 07)");
        }

        [Test]
        public void Scene_TankPairs16mApart_BesideTheLine_OnTheGround()
        {
            GameObject[] roots = Open();
            RayEgrisi e = Tek<RayHatti>(roots).Egri;
            Terrain arazi = Tek<Terrain>(roots);
            foreach (AtesBolgesi z in Hepsi<AtesBolgesi>(roots))
            {
                var tanklar = z.YerBirimleri.Where(g => g.GetComponent<Tank>() != null).ToArray();
                for (int i = 0; i + 1 < tanklar.Length; i += 2)
                {
                    float d = Vector3.Distance(tanklar[i].transform.position, tanklar[i + 1].transform.position);
                    Assert.That(d, Is.EqualTo(H.TankCiftiAraligi).Within(0.5f), $"{z.Ad}: pair spacing");
                    Assert.Less(d * 0.5f, 25f, "a bomb between them reaches both (25 m blast)");
                }
                foreach (GameObject g in z.YerBirimleri)
                {
                    Vector3 p = g.transform.position;
                    e.EnYakin(p, out float yan);
                    Assert.That(yan, Is.InRange(20f, 120f), $"{g.name} beside the line");
                    float zemin = arazi.SampleHeight(p) + arazi.transform.position.y;
                    Assert.That(p.y - zemin, Is.InRange(-0.5f, 0.5f), $"{g.name} stands on the ground");
                }
            }
        }
    }
}
