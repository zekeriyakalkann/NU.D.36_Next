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
using H = NU36.KursunDaglariHaritasi;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 8 (Hafta 8) phase 1: Bölüm 4 · Kurşun Dağları map data, the railway curve, the train schedule and the scene
    /// structure (Documentation/Lab8_Plan.md §9: TEST-804, TEST-805, TEST-807, the static part of TEST-806 and TEST-809,
    /// the map data of TEST-811). Measurements → Logs/lab8/harita_olcumu.txt.
    /// </summary>
    public class Lab8Tests
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
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab8"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "harita_olcumu.txt"), $"Lab 8 map and scene, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        static string Md5(string path)
        {
            using var md5 = MD5.Create();
            return string.Concat(md5.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2")));
        }

        GameObject[] Open()
        {
            scene = EditorSceneManager.OpenScene(Lab8SceneBuilder.ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            return scene.GetRootGameObjects();
        }

        static GameObject Kok(GameObject[] roots, string ad) => roots.Single(r => r.name == ad);

        static T Tek<T>(GameObject[] roots) where T : Component =>
            roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();

        static Bounds Sinir(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static float KapiKonumu(float L) => L - H.GuvenliBolgeBoyu;
        static float DurmaKonumu(float L) => L - H.TrenBitisPayi;

        // ---- railway curve (TEST-807) ----------------------------------------------------------------------------------

        [Test]
        public void Curve_ArcLengthSamples_AreEvenlySpaced()
        {
            RayEgrisi e = H.Egri;
            const float adim = 10f;
            float enAz = float.MaxValue, enCok = 0f;
            for (float s = 0f; s + adim <= e.Uzunluk; s += adim)
            {
                float d = Vector3.Distance(e.Nokta(s), e.Nokta(s + adim));
                enAz = Mathf.Min(enAz, d);
                enCok = Mathf.Max(enCok, d);
                Assert.That(d, Is.EqualTo(adim).Within(adim * 0.01f), $"chord {s:F0}–{s + adim:F0} m");
            }
            Olcum.AppendLine($"TEST-807 line {e.Uzunluk:F1} m; 10 m steps measure {enAz:F3} … {enCok:F3} m");
        }

        [Test]
        public void Curve_LengthFitsTheFiveMinuteRun_NoKinks_GentleGrades()
        {
            RayEgrisi e = H.Egri;
            Assert.That(e.Uzunluk, Is.InRange(4800f, 5600f), "plan §4: about 5 km of line");
            float enAzR = float.PositiveInfinity, enAzS = 0f, enCokEgim = 0f, enCokDonus = 0f;
            for (float s = 25f; s <= e.Uzunluk - 25f; s += 5f)
            {
                float r = e.EgrilikYaricapi(s);
                if (r < enAzR) { enAzR = r; enAzS = s; }
                enCokDonus = Mathf.Max(enCokDonus, Vector3.Angle(e.YatayYon(s - 2f), e.YatayYon(s + 2f)));
                Vector3 a = e.Nokta(s - 25f), b = e.Nokta(s + 25f);
                float yatay = new Vector2(b.x - a.x, b.z - a.z).magnitude;
                enCokEgim = Mathf.Max(enCokEgim, Mathf.Abs(b.y - a.y) / Mathf.Max(1f, yatay));
            }
            Olcum.AppendLine($"curve: tightest radius {enAzR:F0} m at s={enAzS:F0} m; max heading change per 4 m {enCokDonus:F2}°; steepest grade {enCokEgim * 100f:F1} %");
            Assert.Greater(enAzR, 150f, "no bend tighter than 150 m (plan: Son Viraj R ≈ 300 m)");
            Assert.Less(enCokDonus, 2f, "the curve has no kinks");
            Assert.Less(enCokEgim, 0.05f, "grades stay under 5 %");
        }

        [Test]
        public void Curve_NearestPoint_FindsTheDistanceAlongTheLine()
        {
            RayEgrisi e = H.Egri;
            foreach (float s in new[] { 0f, 500f, 1234.5f, 2600f, 4000f, e.Uzunluk })
            {
                Vector3 p = e.Nokta(s) + e.Sag(s) * 6f;
                float bulunan = e.EnYakin(p, out float yan);
                Assert.That(bulunan, Is.EqualTo(s).Within(0.1f), $"s={s}");
                Assert.That(yan, Is.EqualTo(6f).Within(0.05f), $"sideways distance at s={s}");
            }
        }

        // ---- train schedule (static part of TEST-809) ------------------------------------------------------------------

        [Test]
        public void Schedule_PassesTheSafeZoneGateAtFiveMinutes_ThenStopsInsideTheZone()
        {
            float L = H.Egri.Uzunluk;
            float yol = KapiKonumu(L) - H.TrenBaslangici;
            float v = Tren.SeyirHiziHesapla(yol, H.VarisSuresi, H.KalkisSuresi);
            float fren = 2f * (DurmaKonumu(L) - KapiKonumu(L)) / v;
            float T = H.VarisSuresi, k = H.KalkisSuresi;

            Assert.AreEqual(300f, T, "worksheet: 5 minutes");
            Assert.That(Tren.Yol(T, yol, T, k, fren), Is.EqualTo(yol).Within(0.01f), "at the gate at 300 s");
            Assert.That(Tren.Yol(T + fren, yol, T, k, fren), Is.EqualTo(DurmaKonumu(L) - H.TrenBaslangici).Within(0.05f), "stops at the stopping point");
            Assert.That(Tren.Yol(T + fren + 60f, yol, T, k, fren), Is.EqualTo(DurmaKonumu(L) - H.TrenBaslangici).Within(0.05f), "and stays there");
            Assert.AreEqual(0f, Tren.HizAt(T + fren, yol, T, k, fren), 1e-4f);
            // Continuous speed: no jump at the end of the start or at the gate.
            Assert.That(Tren.HizAt(k - 0.001f, yol, T, k, fren), Is.EqualTo(v).Within(0.01f));
            Assert.That(Tren.HizAt(T + 0.001f, yol, T, k, fren), Is.EqualTo(v).Within(0.01f));
            // Monotonic: the train never rolls back.
            float onceki = 0f;
            for (float t = 0f; t <= T + fren + 5f; t += 0.5f)
            {
                float x = Tren.Yol(t, yol, T, k, fren);
                Assert.GreaterOrEqual(x, onceki - 1e-3f, $"t={t}");
                onceki = x;
            }
            Assert.That(v * 3.6f, Is.InRange(50f, 80f), "cruise speed of a supply train");
            Olcum.AppendLine($"TEST-809 schedule: gate at s={KapiKonumu(L):F0} m, cruise {v:F2} m/s ({v * 3.6f:F1} km/h), braking {fren:F1} s, stop at s={DurmaKonumu(L):F0} m");
        }

        // ---- zones (map data of TEST-811) ------------------------------------------------------------------------------

        [Test]
        public void Zones_ThreeInOrder_WithCalmGaps_WorksheetCounts()
        {
            float L = H.Egri.Uzunluk;
            Assert.AreEqual(3, H.Bolgeler.Length);
            float yol = KapiKonumu(L) - H.TrenBaslangici;
            float v = Tren.SeyirHiziHesapla(yol, H.VarisSuresi, H.KalkisSuresi);
            for (int i = 0; i < 3; i++)
            {
                var b = H.Bolgeler[i];
                Assert.Less(b.Bas, b.Son, b.Ad);
                if (i > 0) Assert.Less(H.Bolgeler[i - 1].Son, b.Bas, $"calm gap before {b.Ad}");
                // Zone times along the schedule (cruise part; the start takes the first 20 s).
                float t0 = (b.Bas * L - H.TrenBaslangici - 0.5f * v * H.KalkisSuresi) / v + H.KalkisSuresi;
                float t1 = (b.Son * L - H.TrenBaslangici - 0.5f * v * H.KalkisSuresi) / v + H.KalkisSuresi;
                Olcum.AppendLine($"zone {b.Ad}: s {b.Bas * L:F0}–{b.Son * L:F0} m, locomotive there {Bolum4Hud.SureYazisi(t0)} – {Bolum4Hud.SureYazisi(t1)}");
            }
            Assert.Less(H.Bolgeler[2].Son * L, KapiKonumu(L), "the last zone ends before the safe zone");
            CollectionAssert.AreEqual(new[] { 2, 0, 2 }, H.Bolgeler.Select(b => b.Tank));
            CollectionAssert.AreEqual(new[] { 1, 2, 1 }, H.Bolgeler.Select(b => b.Ucaksavar));
            CollectionAssert.AreEqual(new[] { 0, 2, 2 }, H.Bolgeler.Select(b => b.Ucak));
            Assert.AreEqual(4, H.Bolgeler.Sum(b => b.Tank), "4 tanks");
            Assert.AreEqual(4, H.Bolgeler.Sum(b => b.Ucaksavar), "4 AA guns");
            Assert.AreEqual(4, H.Bolgeler.Sum(b => b.Ucak), "4 enemy aircraft in two waves");

            // Positions: one entry per tank pair, one per AA gun, inside their zone.
            for (int i = 0; i < 3; i++)
            {
                var b = H.Bolgeler[i];
                var burada = H.Mevziler.Where(m => m.Bolge == i).ToArray();
                Assert.AreEqual(b.Tank / 2, burada.Count(m => m.Tur == H.MevziTuru.Tank), $"tank pairs in {b.Ad}");
                Assert.AreEqual(b.Ucaksavar, burada.Count(m => m.Tur == H.MevziTuru.Ucaksavar), $"AA guns in {b.Ad}");
                foreach (var m in burada)
                    Assert.That(m.S, Is.InRange(b.Bas, b.Son), $"{m.Tur} inside {b.Ad}");
            }
        }

        // ---- scene (TEST-804, TEST-805, TEST-806) ----------------------------------------------------------------------

        [Test]
        public void Scene_InBuildSettings_EarlierScenesUnchanged()
        {
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.path == Lab8SceneBuilder.ScenePath && s.enabled), "Bölüm 4 in Build Settings");
            // Lab 7 Tests check Rota02 … Bolum3_GriKutu; the scenes added since then:
            Assert.AreEqual("bbc0599bbc5ebca4ba97d5d3ad41b5a9", Md5("Assets/Scenes/Rota07.unity"), "Rota07 unchanged");
            // AnaMenu changed on purpose in phase 3 (GÖREVE BAŞLA → Bölüm 4); MenuTests checks it.
        }

        [Test]
        public void Scene_StationAtTheStart_SafeZoneAtTheEnd_BaseNearTheStation()
        {
            GameObject[] roots = Open();
            var hat = Tek<RayHatti>(roots);
            CollectionAssert.AreEqual(H.RayNoktalari(), hat.KontrolNoktalari, "the scene's line is the map's line");
            RayEgrisi e = hat.Egri;
            float L = e.Uzunluk;

            float sIstasyon = e.EnYakin(Sinir(Kok(roots, "Kalkis Istasyonu")).center, out float yIstasyon);
            float sGuvenli = e.EnYakin(Sinir(Kok(roots, "Guvenli Bolge")).center, out float yGuvenli);
            Vector3 pist = Kok(roots, "Us").transform.Find("Pist").position;
            float usUzakligi = Vector3.Distance(new Vector3(pist.x, 0f, pist.z), new Vector3(e.Nokta(0f).x, 0f, e.Nokta(0f).z));
            Olcum.AppendLine($"TEST-804 station at s={sIstasyon:F0} m ({yIstasyon:F0} m aside), safe zone at s={sGuvenli:F0} m of {L:F0}, base {usUzakligi:F0} m from the line start");

            Assert.Less(sIstasyon, 300f, "departure station at one end");
            Assert.Greater(sGuvenli, L - H.GuvenliBolgeBoyu - 50f, "safe zone at the other end");
            Assert.Less(yIstasyon, 60f);
            Assert.Less(yGuvenli, 60f);
            Assert.Less(usUzakligi, 1500f, "the base is near the departure station");
            Assert.Greater(Vector3.Distance(e.Nokta(0f), e.Nokta(L)), 1500f, "the two ends are far apart");
        }

        [Test]
        public void Scene_TrainOnTheLine_LocomotiveAndAtLeastThreeWagons()
        {
            GameObject[] roots = Open();
            var tren = Tek<Tren>(roots);
            Assert.IsNotNull(tren.Hat);
            RayEgrisi egri = tren.Hat.Egri;
            Assert.IsNotNull(tren.Saglik, "the train has health");
            Assert.AreSame(tren.gameObject, tren.Saglik.gameObject);
            Assert.GreaterOrEqual(tren.Araclar.Length, 4, "locomotive + at least 3 wagons");
            Assert.That(tren.Araclar[0].Uzunluk, Is.EqualTo(15f).Within(2f), "locomotive ≈ 15 m (worksheet 01)");
            foreach (TrenAraci a in tren.Araclar.Skip(1))
                Assert.That(a.Uzunluk, Is.InRange(8f, 16f), $"{a.name} length");
            foreach (TrenAraci a in tren.Araclar)
            {
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(a.gameObject), $"{a.name} comes from a prefab");
                Assert.IsTrue(a.TryGetComponent(out Collider _), $"{a.name} can be hit");
                Assert.IsTrue(a.GetComponent<Rigidbody>().isKinematic, $"{a.name} is moved by the schedule, not by physics");
                egri.EnYakin(a.transform.position, out float yan);
                Assert.Less(yan, 0.3f, $"{a.name} stands on the line");
            }
            Olcum.AppendLine($"train: {string.Join(", ", tren.Araclar.Select(a => $"{a.name} {a.Uzunluk:F1} m"))}");
        }

        [Test]
        public void Scene_RailIsUnbroken_FromStationToSafeZone()
        {
            GameObject[] roots = Open();
            RayEgrisi e = Tek<RayHatti>(roots).Egri;
            Transform ray = Kok(roots, "Ray Hatti").transform.Find("Ray");
            Assert.IsNotNull(ray, "rail chunks under Ray Hatti/Ray");

            // Plan-view grid of the curve samples, to find the distance along the line of each rail vertex quickly.
            const float hucre = 10f;
            var izgara = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i < e.OrnekSayisi; i++)
            {
                Vector3 o = e.Ornek(i);
                var c = new Vector2Int(Mathf.FloorToInt(o.x / hucre), Mathf.FloorToInt(o.z / hucre));
                if (!izgara.TryGetValue(c, out var l)) izgara[c] = l = new List<int>();
                l.Add(i);
            }
            float SBul(Vector3 p, out float yan)
            {
                var c = new Vector2Int(Mathf.FloorToInt(p.x / hucre), Mathf.FloorToInt(p.z / hucre));
                int enIyi = -1; float enIyiD = float.MaxValue;
                for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                    if (izgara.TryGetValue(new Vector2Int(c.x + dx, c.y + dz), out var l))
                        foreach (int i in l)
                        {
                            Vector3 o = e.Ornek(i);
                            float d = (o.x - p.x) * (o.x - p.x) + (o.z - p.z) * (o.z - p.z);
                            if (d < enIyiD) { enIyiD = d; enIyi = i; }
                        }
                if (enIyi < 0) { yan = float.PositiveInfinity; return -1f; }
                // Refine on the neighbouring segment.
                float s = enIyi * e.Adim;
                Vector3 t = e.YatayYon(s);
                Vector3 r = p - e.Ornek(enIyi); r.y = 0f;
                float u = Mathf.Clamp(Vector3.Dot(r, t), -e.Adim, e.Adim);
                yan = (r - t * u).magnitude;
                return Mathf.Clamp(s + u, 0f, e.Uzunluk);
            }

            // Interval along the line covered by each steel (sub-mesh 0) triangle; their union must cover the line.
            var araliklar = new List<(float a, float b)>();
            float enCokYan = 0f;
            int parca = 0;
            foreach (MeshFilter mf in ray.GetComponentsInChildren<MeshFilter>())
            {
                parca++;
                Mesh m = mf.sharedMesh;
                Matrix4x4 w = mf.transform.localToWorldMatrix;
                Vector3[] v = m.vertices;
                var sV = new float[v.Length];
                var hesap = new bool[v.Length];
                int[] tri = m.GetTriangles(0);
                for (int k = 0; k < tri.Length; k += 3)
                {
                    float a = float.MaxValue, b = float.MinValue;
                    for (int j = 0; j < 3; j++)
                    {
                        int ix = tri[k + j];
                        if (!hesap[ix])
                        {
                            sV[ix] = SBul(w.MultiplyPoint3x4(v[ix]), out float yan);
                            enCokYan = Mathf.Max(enCokYan, yan);
                            hesap[ix] = true;
                        }
                        a = Mathf.Min(a, sV[ix]);
                        b = Mathf.Max(b, sV[ix]);
                    }
                    araliklar.Add((a, b));
                }
            }
            araliklar.Sort((x, y) => x.a.CompareTo(y.a));
            float kapsanan = araliklar[0].a, enBuyukBosluk = araliklar[0].a;
            foreach (var (a, b) in araliklar)
            {
                enBuyukBosluk = Mathf.Max(enBuyukBosluk, a - kapsanan);
                kapsanan = Mathf.Max(kapsanan, b);
            }
            enBuyukBosluk = Mathf.Max(enBuyukBosluk, e.Uzunluk - kapsanan);
            Olcum.AppendLine($"TEST-805 rail: {parca} chunk meshes, {araliklar.Count} steel triangles, largest gap {enBuyukBosluk:F3} m, steel at most {enCokYan:F2} m from the centre line");
            Assert.Greater(parca, 1);
            Assert.Less(enBuyukBosluk, 0.5f, "no gap over 0.5 m along the whole line");
            Assert.Less(enCokYan, 1.2f, "the rails follow the line");
        }

        [Test]
        public void Scene_GroundUnderTheLine_IsTheRailBed_ExceptOnTheViaduct()
        {
            GameObject[] roots = Open();
            RayEgrisi e = Tek<RayHatti>(roots).Egri;
            Terrain terrain = Tek<Terrain>(roots);
            var (kBas, kSon) = Lab8Yapilar.KopruAraligi(e, (x, z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y);
            Assert.Greater(kBas, 0f, "the line crosses the gorge");
            Assert.Greater(kSon - kBas, 100f, "on a viaduct over 100 m long");
            Assert.That(Kok(roots, "Viyaduk").transform.childCount, Is.GreaterThan(0));

            float enCokUst = float.MinValue, enCokAlt = float.MinValue, ustS = 0f, altS = 0f;
            for (float s = 0f; s <= e.Uzunluk; s += 2f)
            {
                if (s > kBas - 30f && s < kSon + 30f) continue;
                foreach (float yan in new[] { -1.5f, 0f, 1.5f })
                {
                    Vector3 p = e.Nokta(s) + e.Sag(s) * yan;
                    float zemin = terrain.SampleHeight(p) + terrain.transform.position.y;
                    float fark = zemin - p.y;   // + : ground above the line centre
                    if (fark > enCokUst) { enCokUst = fark; ustS = s; }
                    if (-fark > enCokAlt) { enCokAlt = -fark; altS = s; }
                }
            }
            Olcum.AppendLine($"rail bed: ground at most {enCokUst:F2} m above the line (s={ustS:F0}) and {enCokAlt:F2} m below it (s={altS:F0}); viaduct s {kBas:F0}–{kSon:F0} m");
            Assert.Less(enCokUst, H.RayUstu - 0.02f, $"the ground never covers the rails (worst at s={ustS:F0} m)");
            Assert.Less(enCokAlt, 1.5f, $"the rails never float above the ground off the viaduct (worst at s={altS:F0} m)");
        }

        [Test]
        public void Scene_AircraftStartsInTheAir_AboveTheTrain_AlongTheLine()
        {
            GameObject[] roots = Open();
            var ucus = Tek<UcusKontrol>(roots);
            var tren = Tek<Tren>(roots);
            RayEgrisi e = tren.Hat.Egri;
            Transform lok = tren.Araclar[0].transform;
            Vector3 fark = ucus.transform.position - lok.position;
            Olcum.AppendLine($"TEST-806 start: {fark.y:F0} m above the locomotive, {new Vector2(fark.x, fark.z).magnitude:F0} m behind it");
            Assert.That(fark.y, Is.EqualTo(150f).Within(20f), "150 m above the locomotive");
            Assert.Less(new Vector2(fark.x, fark.z).magnitude, 200f, "over the train");
            Assert.Less(Vector3.Angle(ucus.transform.forward, e.YatayYon(H.TrenBaslangici)), 5f, "heading along the line");

            var so = new SerializedObject(ucus);
            Assert.That(so.FindProperty("baslangicHizi").floatValue * 3.6f, Is.EqualTo(220f).Within(1f), "220 km/h");
            Assert.IsFalse(new SerializedObject(ucus.GetComponent<InisAlgilayici>()).FindProperty("yerdeBasla").boolValue, "starts flying");
            Assert.AreEqual(2, new SerializedObject(ucus.GetComponent<BombaYuku>()).FindProperty("kapasite").intValue, "2 bombs (worksheet 06)");
            Assert.IsTrue(ucus.GetComponent<ZeminBaglayici>() != null, "ground height from the terrain collider");
            Assert.IsTrue(ucus.GetComponent<UcusYardimcisi>() != null, "Bölüm 3 assists came along");

            var yardimci = new SerializedObject(ucus.GetComponent<UcusYardimcisi>());
            Assert.AreEqual(H.SinirYaricapi, yardimci.FindProperty("sinirYaricapi").floatValue, 0.01f, "map boundary of the new map");
            Assert.AreEqual(H.PistBasi, yardimci.FindProperty("pistBasi").vector3Value, "runway of the new base");
        }

        [Test]
        public void Scene_NightWorldRemoved_DaylightAndHud()
        {
            GameObject[] roots = Open();
            // "Nehir Suyu" is removed with the night world and built again for the gorge river of the new map.
            foreach (string ad in Lab8SceneBuilder.SilinenKokler.Where(a => a != "Nehir Suyu"))
                Assert.IsFalse(roots.Any(r => r.name == ad), $"{ad} removed");
            Assert.AreEqual(1, roots.Count(r => r.name == "Nehir Suyu"), "one river: the gorge under the viaduct");
            Assert.IsFalse(roots.SelectMany(r => r.GetComponentsInChildren<GeceGorevi>(true)).Any(), "no night mission");
            Assert.IsFalse(roots.SelectMany(r => r.GetComponentsInChildren<Isildak>(true)).Any(), "no searchlights");
            Assert.IsNotNull(Tek<Terrain>(roots).terrainData);
            Assert.AreEqual(ZeminOlcer.AraziKatmani, LayerMask.LayerToName(Tek<Terrain>(roots).gameObject.layer));
            Assert.IsTrue(RenderSettings.fog);
            Assert.Greater(RenderSettings.sun.intensity, 1f, "daylight");

            var hud = Tek<Bolum4Hud>(roots);
            var so = new SerializedObject(hud);
            foreach (string alan in new[] { "ucus", "ucakSagligi", "tren", "yuk", "durumMetni", "gorevMetni" })
                Assert.IsNotNull(so.FindProperty(alan).objectReferenceValue, $"HUD {alan} wired");
        }
    }
}
