using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using NU36.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using H = NU36.KartalGecidiHaritasi;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 9 (Hafta 9) Bölüm 5 · Kartal Geçidi in Edit Mode (Documentation/Lab9_Plan.md §7): the new scene and the earlier
    /// ones (TEST-901), the pass as the only way to the bay (TEST-902), the four spaced lanes and where the ships stop and the
    /// tanks land (TEST-903 data), the ship prefab and the absence of people (TEST-911).
    /// Measurements → Logs/lab9/harita_olcumu.txt.
    /// </summary>
    public class Lab9Tests
    {
        Scene scene;
        static readonly StringBuilder Olcum = new StringBuilder();

        [TearDown]
        public void Close()
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        [OneTimeTearDown]
        public void Yaz()
        {
            if (Olcum.Length == 0) return;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab9"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "harita_olcumu.txt"), $"Lab 9 map and scene, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        static string Md5(string path)
        {
            using var md5 = MD5.Create();
            return string.Concat(md5.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2")));
        }

        GameObject[] Open()
        {
            scene = EditorSceneManager.OpenScene(Lab9SceneBuilder.ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            return scene.GetRootGameObjects();
        }

        static T[] Hepsi<T>(GameObject[] roots) where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        static T Tek<T>(GameObject[] roots) where T : Component => Hepsi<T>(roots).Single();

        // ---- TEST-901: a new scene, separate from Bölüm 4 ------------------------------------------------------------

        [Test]
        public void Scene_NewAndInBuildSettings_Bolum4AndEarlierScenesUnchanged()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(Lab9SceneBuilder.ScenePath), "Bölüm 5 scene exists");
            Assert.AreNotEqual(Lab8SceneBuilder.ScenePath, Lab9SceneBuilder.ScenePath);
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.path == Lab9SceneBuilder.ScenePath && s.enabled), "Bölüm 5 in Build Settings");
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.path == Lab8SceneBuilder.ScenePath && s.enabled), "Bölüm 4 still in Build Settings");
            // Hashes taken before Lab 9 (Logs/lab9/sahne_md5_once.txt). AnaMenu changed on purpose (MenuTests checks it).
            Assert.AreEqual("b307549021f4aecb0fe616ed19c7771a", Md5(Lab8SceneBuilder.ScenePath), "Bölüm 4 unchanged");
            Assert.AreEqual("bbc0599bbc5ebca4ba97d5d3ad41b5a9", Md5("Assets/Scenes/Rota07.unity"), "Rota07 unchanged");
            Assert.AreEqual("2864ebcaf382e90c2ce17ec78a3bb590", Md5("Assets/Scenes/Bolum3_GriKutu.unity"), "Bolum3_GriKutu unchanged");
            Assert.AreEqual("4680fa01303df2298a168652cb9cdd73", Md5("Assets/Scenes/Rota05.unity"), "Rota05 unchanged");
        }

        [Test]
        public void Scene_Bolum4WorldRemoved_AircraftControlsAndCoastWorldPresent()
        {
            GameObject[] roots = Open();
            Assert.IsEmpty(Hepsi<Tren>(roots), "no train");
            Assert.IsEmpty(Hepsi<RayHatti>(roots), "no railway");
            Assert.IsEmpty(Hepsi<AtesBolgesi>(roots), "no Bölüm 4 fire zones");
            Assert.IsEmpty(Hepsi<KursunDaglariGorevi>(roots), "no Bölüm 4 mission");
            Assert.IsEmpty(Hepsi<Bolum4Hud>(roots), "no Bölüm 4 HUD");

            var ucus = Tek<UcusKontrol>(roots);
            Assert.IsNotNull(ucus.GetComponent<Makineli>(), "machine gun");
            Assert.IsNotNull(ucus.GetComponent<Bolum4Nisan>(), "targeting assist");
            Assert.IsNotNull(ucus.GetComponent<MotorSesi>(), "engine sound");
            Assert.AreEqual(Lab9SceneBuilder.BombaKapasitesi, ucus.GetComponent<BombaYuku>().Kapasite);
            var so = new SerializedObject(ucus);
            Assert.IsTrue(so.FindProperty("arkad").boolValue, "arcade flight by default (as Bölüm 4)");
            Assert.AreEqual(Taraf.Dost, ucus.GetComponent<Saglik>().Taraf);

            Assert.IsNotNull(Tek<CikarmaFilosu>(roots));
            var koruma = Tek<KorumaUcuslari>(roots);
            Assert.AreEqual(H.KorumaToplam, koruma.Ucaklar.Length);
            var dusmanUcagi = AssetDatabase.LoadAssetAtPath<GameObject>(Lab8Dusmanlar.DusmanUcagiYolu);
            foreach (DusmanUcagi u in koruma.Ucaklar)
            {
                Assert.IsFalse(u.gameObject.activeSelf, "waits inactive");
                Assert.AreSame(dusmanUcagi, PrefabUtility.GetCorrespondingObjectFromSource(u.gameObject), "instance of the enemy aircraft prefab");
                Assert.IsTrue(u.GetComponent<Susturulabilir>().SerbestHedef);
            }
            Assert.IsNotNull(Tek<KartalGecidiGorevi>(roots));
            Assert.IsNotNull(Tek<Bolum5Hud>(roots));
            Assert.IsNotNull(Tek<Bolum5Gps>(roots));
            Assert.IsNotNull(Tek<DuraklatmaMenusu>(roots));
            Assert.IsNotNull(Tek<Terrain>(roots));
            var deniz = roots.Single(r => r.name == "Deniz Yuzeyi");
            Assert.AreEqual(LayerMask.NameToLayer("Arazi"), deniz.layer, "the sea counts as the ground");
            Assert.IsFalse(roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).Any(c => c == null), "missing script");
        }

        // ---- TEST-902: the pass ------------------------------------------------------------------------------------------

        /// <summary>Flood fill over the ridge band (40 m cells) through ground lower than <paramref name="tavan"/>; cells within
        /// <paramref name="gecitHaric"/> m of the pass line are walls when it is &gt; 0. True when the east side is reached.</summary>
        static bool DogudanGecilir(float tavan, float gecitHaric, out int hucre)
        {
            const float a = 40f;
            float x0 = H.SirtBati - 300f, x1 = H.SirtDogu + 300f, z0 = -3200f, z1 = 3200f;
            int nx = Mathf.CeilToInt((x1 - x0) / a), nz = Mathf.CeilToInt((z1 - z0) / a);
            var acik = new bool[nx, nz];
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x = x0 + i * a, z = z0 + j * a;
                    H.GecitUzakligi(x, z, out float d);
                    acik[i, j] = H.Yukseklik(x, z) < tavan && !(gecitHaric > 0f && d < gecitHaric);
                }
            var gorulen = new bool[nx, nz];
            var kuyruk = new Queue<(int, int)>();
            for (int j = 0; j < nz; j++)
                if (acik[0, j]) { gorulen[0, j] = true; kuyruk.Enqueue((0, j)); }
            hucre = 0;
            while (kuyruk.Count > 0)
            {
                var (i, j) = kuyruk.Dequeue();
                hucre++;
                if (i == nx - 1) return true;
                foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int ii = i + di, jj = j + dj;
                    if (ii < 0 || jj < 0 || ii >= nx || jj >= nz || gorulen[ii, jj] || !acik[ii, jj]) continue;
                    gorulen[ii, jj] = true;
                    kuyruk.Enqueue((ii, jj));
                }
            }
            return false;
        }

        [Test]
        public void Map_ThePassIsTheOnlyLowWayThroughTheRidge()
        {
            const float tavan = 600f;
            Assert.IsTrue(DogudanGecilir(tavan, 0f, out int n1), "the pass leads from the valley to the coast below 600 m");
            Assert.IsFalse(DogudanGecilir(tavan, 400f, out int n2), "without the pass the ridge cannot be crossed below 600 m");
            Olcum.AppendLine($"TEST-902 flood fill below {tavan} m: with the pass reached the east ({n1} cells); without it stuck after {n2} cells");

            // Along the pass inside the ridge: a low floor, walls at least 400 m above it on both sides.
            RayEgrisi e = H.Gecit;
            float enYuksekTaban = 0f, enAlcakDuvar = float.MaxValue;
            for (float s = 0f; s <= e.Uzunluk; s += 20f)
            {
                Vector3 p = e.Nokta(s);
                if (p.x < H.SirtBati || p.x > H.SirtDogu) continue;
                enYuksekTaban = Mathf.Max(enYuksekTaban, H.Yukseklik(p.x, p.z));
                Vector3 sag = e.Sag(s);
                foreach (int k in new[] { -1, 1 })
                {
                    Vector3 q = p + sag * (k * 380f);
                    enAlcakDuvar = Mathf.Min(enAlcakDuvar, H.Yukseklik(q.x, q.z) - p.y);
                }
            }
            Olcum.AppendLine($"TEST-902 pass length {e.Uzunluk:F0} m; highest floor in the ridge {enYuksekTaban:F0} m; lowest wall 380 m aside {enAlcakDuvar:F0} m above the floor");
            Assert.Less(enYuksekTaban, 300f, "the pass floor stays low");
            Assert.Greater(enAlcakDuvar, 400f, "walls on both sides");

            // The start is west of the ridge at the pass, the bay east of it; the straight line between them is blocked.
            Vector3 b = H.BaslangicNoktasi;
            Assert.Less(b.x, H.SirtBati, "start west of the ridge");
            Assert.Greater(H.KoyMerkezi.x, H.SirtDogu, "bay east of the ridge");
            Assert.Greater(Vector3.Dot(H.BaslangicYonu, Vector3.right), 0.8f, "facing the pass");
            Assert.Greater(b.y - H.Yukseklik(b.x, b.z), 100f, "in the air");
        }

        [Test]
        public void Scene_TerrainFollowsTheMap()
        {
            GameObject[] roots = Open();
            var terrain = Tek<Terrain>(roots);
            float enCok = 0f;
            foreach (Vector2 p in new[] { new Vector2(-1500f, 0f), new Vector2(0f, 1500f), new Vector2(1200f, 200f), new Vector2(2000f, -300f), new Vector2(H.Gecit.Nokta(1800f).x, H.Gecit.Nokta(1800f).z) })
            {
                float a = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
                enCok = Mathf.Max(enCok, Mathf.Abs(a - H.Yukseklik(p.x, p.y)));
            }
            Assert.Less(enCok, 4f, "terrain = map height function");
            Assert.Less(terrain.SampleHeight(H.KoyMerkezi) + terrain.transform.position.y, -10f, "the bay is under water");
        }

        // ---- TEST-903: four spaced lanes; where ships stop and tanks land --------------------------------------------

        [Test]
        public void Map_FourLanesSpacedAlongTheBeach_ShipsStopAtTheWaterline_TanksLandOnDryGround()
        {
            Assert.AreEqual(4, H.SeritZ.Length, "worksheet: 4 ships at once");
            Assert.AreEqual(4, H.IlkUzakliklar.Length);
            Assert.AreEqual(4, H.IlkUzakliklar.Distinct().Count(), "staggered: they arrive one after the other");
            for (int i = 1; i < H.SeritZ.Length; i++)
                Assert.GreaterOrEqual(H.SeritZ[i] - H.SeritZ[i - 1], 250f, "spaced lanes");
            Assert.AreEqual(12, H.GemiSayisi);
            Assert.AreEqual(3, H.GemiBasinaTank);
            Assert.AreEqual(12, H.KiyiDusmeSiniri);
            for (int i = 0; i < 4; i++)
            {
                Vector3 dur = H.YanasmaNoktasi(i);
                Vector3 pruva = dur + H.KiyiyaDogru * H.PruvaUzakligi;
                Vector3 kara = H.KaraNoktasi(i);
                float hd = H.Yukseklik(dur.x, dur.z), hp = H.Yukseklik(pruva.x, pruva.z), hk = H.Yukseklik(kara.x, kara.z);
                Olcum.AppendLine($"TEST-903 lane {i + 1}: stop ({dur.x:F0}, {dur.z:F0}) depth {-hd:F1} m, bow depth {-hp:F1} m, landing point height {hk:F1} m");
                Assert.Less(hd, -1.6f, "the ship floats at its stop point (1.5 m draft)");
                Assert.That(hp, Is.InRange(-1.5f, 0.4f), "the bow at the waterline");
                Assert.Greater(hk, 0.3f, "tanks land on dry ground");
                foreach (float u in H.IlkUzakliklar.Append(H.YeniGemiUzakligi))
                {
                    Vector3 bas = dur - H.KiyiyaDogru * u;
                    Assert.Less(H.Yukseklik(bas.x, bas.z), -10f, "ships start out at sea");
                }
                for (int n = 0; n < 9; n++)
                {
                    Vector3 p = H.ParkYeri(i, n);
                    Assert.Greater(H.Yukseklik(p.x, p.z), 2f, "parking rows are on land");
                    Assert.Less(H.DenizUzakligi(p.x, p.z), -H.KumsalGenisligi, "parking behind the beach");
                }
            }
        }

        [Test]
        public void Map_BeachedShipsRampRestsOnTheSandAtTheWaterline()
        {
            for (int i = 0; i < 4; i++)
            {
                // The beached pose of CikarmaGemisi: heading to the beach, bow up, lifted a little.
                Quaternion q = Quaternion.Euler(-CikarmaGemisi.OturmaEgimi, Quaternion.LookRotation(H.KiyiyaDogru).eulerAngles.y, 0f);
                Vector3 mentese = H.YanasmaNoktasi(i) + Vector3.up * CikarmaGemisi.OturmaYuksekligi + q * new Vector3(0f, Lab9Varliklar.GuverteY, Lab9Varliklar.PruvaZ);
                float a = CikarmaGemisi.RampaAcisiBul(mentese, q, Lab9Varliklar.RampaBoyu, H.Yukseklik);
                Vector3 uc = mentese + q * (Quaternion.Euler(a, 0f, 0f) * Vector3.forward) * Lab9Varliklar.RampaBoyu;
                float ds = H.DenizUzakligi(uc.x, uc.z);
                Olcum.AppendLine($"ramp lane {i + 1}: {a:F1}° down, tip {uc.y:F2} m high, {ds:F1} m from the waterline (ground {H.Yukseklik(uc.x, uc.z):F2} m)");
                Assert.That(a, Is.InRange(8f, 30f), "a real ramp slope, not flat and not vertical");
                Assert.Greater(uc.y, -0.15f, "the tip rests at the water's edge, not under the sea");
                Assert.That(ds, Is.InRange(-4f, 5f), "at the waterline");
                Assert.Less(Mathf.Abs(uc.y - H.Yukseklik(uc.x, uc.z)), 0.3f, "on the sand");
            }
        }

        // ---- TEST-911: the ship carries vehicles only; no people anywhere ---------------------------------------------

        static readonly string[] InsanAdlari = { "insan", "asker", "pilot", "human", "person", "crew", "soldier", "figure", "character", "people", "mürettebat", "murettebat" };

        static bool InsanAdi(string ad) => InsanAdlari.Any(k => ad.ToLowerInvariant().Contains(k));

        [Test]
        public void Prefab_Ship_ThreeTanksOnDeck_PrimitivesOnly_NoPeople()
        {
            var gemi = AssetDatabase.LoadAssetAtPath<GameObject>(Lab9Varliklar.GemiYolu);
            Assert.IsNotNull(gemi);
            var g = gemi.GetComponent<CikarmaGemisi>();
            Assert.AreEqual(3, g.TankSayisi, "three tanks");
            Assert.AreEqual(3, gemi.GetComponentsInChildren<CikarmaTanki>(true).Length);
            var tankOn = AssetDatabase.LoadAssetAtPath<GameObject>(Lab9Varliklar.TankYolu);
            foreach (CikarmaTanki t in g.Tanklar)
                Assert.AreSame(tankOn, PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject), "tank prefab instance");
            var s = gemi.GetComponent<Saglik>();
            Assert.AreEqual(Lab9Varliklar.GemiCani, s.BaslangicCani);
            Assert.AreEqual(Taraf.Dusman, s.Taraf);
            Assert.IsFalse(s.YalnizPatlama, "rounds and bombs both hurt it");
            var sus = gemi.GetComponent<Susturulabilir>();
            Assert.AreEqual(DusmanTuru.Gemi, sus.Tur);
            Assert.IsTrue(sus.SerbestHedef);
            Assert.AreEqual(LayerMask.NameToLayer("Hedef"), gemi.layer);

            // Every mesh of the ship that is not a tank is a Unity primitive (no imported model, so no figure on it).
            var ilkeller = new HashSet<string> { "Cube", "Cylinder" };
            int parca = 0;
            foreach (MeshFilter mf in gemi.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.GetComponentInParent<CikarmaTanki>(true) != null) continue;
                Assert.IsTrue(ilkeller.Contains(mf.sharedMesh.name), $"{mf.name}: {mf.sharedMesh.name}");
                parca++;
            }
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer r in gemi.GetComponentsInChildren<MeshRenderer>(true))
                if (r.GetComponentInParent<CikarmaTanki>(true) == null) b.Encapsulate(r.bounds);
            Olcum.AppendLine($"TEST-911 ship: {parca} primitive parts, {b.size.z:F1} × {b.size.x:F1} m, 3 tanks");
            Assert.That(b.size.z, Is.InRange(40f, 55f), "ship length");
            foreach (Transform t in gemi.GetComponentsInChildren<Transform>(true))
                Assert.IsFalse(InsanAdi(t.name), t.name);

            Renderer govde = tankOn.GetComponentsInChildren<Renderer>().Single(r => r.name == "Tank_body");
            Assert.That(govde.bounds.size.z, Is.InRange(6.5f, 7.5f), "tank hull ≈ 7 m (as Bölüm 4)");
            foreach (Transform t in tankOn.GetComponentsInChildren<Transform>(true))
                Assert.IsFalse(InsanAdi(t.name), t.name);
        }

        [Test]
        public void Scene_NoPeopleAnywhere()
        {
            GameObject[] roots = Open();
            int n = 0;
            foreach (Transform t in roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
            {
                n++;
                Assert.IsFalse(InsanAdi(t.name), t.name);
                if (t.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
                    Assert.IsFalse(InsanAdi(mf.sharedMesh.name), mf.sharedMesh.name);
            }
            var terrain = Tek<Terrain>(roots);
            foreach (TreePrototype p in terrain.terrainData.treePrototypes)
                Assert.IsFalse(InsanAdi(p.prefab.name), p.prefab.name);
            Olcum.AppendLine($"TEST-911 scene: {n} objects checked, none named after people; tree prototypes: {string.Join(", ", terrain.terrainData.treePrototypes.Select(p => p.prefab.name))}");
        }
    }
}
