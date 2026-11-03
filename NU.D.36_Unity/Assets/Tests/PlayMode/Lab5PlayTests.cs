using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>Lab 5 in the real Rota05 scene: line of sight behind Tepe 3, tunnelling and the sweep, the bomb, resupply and the clock.</summary>
    public class Lab5PlayTests
    {
        UcusKontrol ucus;
        SeviyeYoneticisi seviye;
        BombaYuku yuk;
        Saglik saglik;

        static string LogDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab5"));

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Rota05", LoadSceneMode.Single);
            yield return null;
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            seviye = Object.FindAnyObjectByType<SeviyeYoneticisi>();
            yuk = ucus.GetComponent<BombaYuku>();
            saglik = ucus.GetComponent<Saglik>();
            Object.FindAnyObjectByType<SeviyeSonuEkrani>().SahneYukle = _ => { }; // never reload during a test
            Assert.IsNotNull(seviye);
        }

        /// <summary>Freezes the aircraft at a pose and lets the terrain window and the physics scene catch up.</summary>
        IEnumerator Konumla(Vector3 p, Quaternion r)
        {
            ucus.enabled = false;
            ucus.transform.SetPositionAndRotation(p, r);
            for (int i = 0; i < 70; i++) // terrain builds the nearest chunks first, 3 per frame
                yield return null;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
        }

        static GameObject Bul(string ad) => Object.FindObjectsByType<Transform>().Single(t => t.name == ad).gameObject;

        [UnityTest]
        public IEnumerator Rota05_StartsWithoutErrors_AndTheLevelRuns()
        {
            yield return Yukle();
            float t0 = seviye.KalanSure;
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.Less(seviye.KalanSure, t0, "the clock runs");
            Assert.IsFalse(seviye.Bitti);
            Assert.AreEqual(4, seviye.MevziSayisi);
            Assert.AreEqual(2, yuk.Kalan);
            Assert.IsTrue(Object.FindAnyObjectByType<SonsuzArazi>().GetComponentsInChildren<MeshCollider>().All(c => !c.enabled || c.sharedMesh != null),
                "every active terrain chunk has a cooked Mesh Collider");
            Assert.AreEqual(Object.FindAnyObjectByType<SonsuzArazi>().HazirChunkSayisi,
                Object.FindAnyObjectByType<SonsuzArazi>().GetComponentsInChildren<MeshCollider>().Count(c => c.enabled && c.sharedMesh != null));
            Assert.That(ZeminOlcer.AraziMaskesi, Is.EqualTo(1 << LayerMask.NameToLayer("Arazi")));
        }

        [UnityTest]
        public IEnumerator Radar_BehindTepe3_IsBlockedYellow_InTheOpen_Sees()
        {
            yield return Yukle();
            Radar radar = Bul("Ucaksavar 2").GetComponent<Radar>();
            Tepe tepe = Bul("Tepe 3").GetComponent<Tepe>();
            Vector3 goz = radar.Goz, merkez = tepe.transform.position;
            Vector3 yon = new Vector3(merkez.x - goz.x, 0f, merkez.z - goz.z).normalized;

            // Behind the hill, 450 m past its summit, 60 m above the ground: inside the cone and range, but the hill is in the way.
            Vector3 arka = merkez + yon * 450f;
            arka.y = ZeminOlcer.Yukseklik(arka.x, arka.z) + 60f;
            yield return Konumla(arka, Quaternion.LookRotation(yon));
            radar.Denetle();
            Assert.IsTrue(Radar.GorusAlanindaMi(radar.transform.position, radar.transform.forward, ucus.transform.position, radar.YariAci, radar.Menzil),
                "test pose must be inside the cone");
            Assert.IsTrue(radar.EngelVar, "the hill blocks the ray");
            Assert.IsFalse(radar.Goruyor);
            Assert.AreEqual(Color.yellow, Radar.GizmoRengi(radar.Goruyor, radar.EngelVar));

            // Same distance, straight above the hill: the ray is clear.
            Vector3 ust = new Vector3(merkez.x, ZeminOlcer.Yukseklik(merkez.x, merkez.z) + 250f, merkez.z); // 250 m above the summit
            yield return Konumla(ust, Quaternion.LookRotation(yon));
            radar.Denetle();
            Assert.IsFalse(radar.EngelVar);
            Assert.IsTrue(radar.Goruyor, "no obstacle: green");

            // The aircraft's own collider never counts as an obstacle (it is not in the mask).
            Assert.IsFalse(Radar.GorusEngelliMi(goz, ucus.transform.position, radar.EngelKatmani));
        }

        [UnityTest]
        public IEnumerator Mermi_At260_TunnelsWithoutSweep_HitsWithSweep()
        {
            yield return Yukle();
            Vector3 hedef = new Vector3(-3000f, 600f, -3000f); // empty sky, far from everything
            yield return Konumla(hedef, Quaternion.identity);
            var mermiOn = Bul("Ucaksavar 1").GetComponent<UcaksavarMevzii>();
            int maske = LayerMask.GetMask("Ucak", "Arazi");

            // The body sphere (r 2.4) is centred 13 m ahead; 5.2 m steps land at 10.4 and 15.6 m, both outside it.
            Mermi Yeni(bool supur)
            {
                var go = new GameObject("TestMermi");
                go.transform.position = hedef - Vector3.forward * 13f;
                var m = go.AddComponent<Mermi>();
                m.enabled = false; // stepped by hand at the physics step
                m.Ates(Vector3.forward, 260f, 10, maske, supur);
                return m;
            }

            int can0 = saglik.Can;
            Mermi kirik = Yeni(false);
            bool vurdu = false;
            for (int i = 0; i < 4 && !vurdu; i++)
                vurdu = kirik.Adim(0.02f);
            Assert.IsFalse(vurdu, "end-point check: the round jumped over the aircraft");
            Assert.AreEqual(can0, saglik.Can);
            Object.Destroy(kirik.gameObject);

            Mermi supuren = Yeni(true);
            vurdu = false;
            for (int i = 0; i < 4 && !vurdu; i++)
                vurdu = supuren.Adim(0.02f);
            Assert.IsTrue(vurdu, "the sweep finds the aircraft inside the step");
            Assert.AreEqual(can0 - 10, saglik.Can, "health drops");
        }

        IEnumerator DalisaKoy(MuhimmatMevzii hedef, float aci, float irtifa, float hiz = 85f)
        {
            Vector3 h = hedef.transform.position;
            Vector3 ileri = new Vector3(-h.x, 0f, -h.z).normalized * -1f; // flying away from the base, towards the dump
            float x = Bomba.IleriMenzil(hiz, aci, irtifa);
            Vector3 p = h - ileri * x;
            p.y = ZeminOlcer.Yukseklik(p.x, p.z) + irtifa + 1.6f; // the bomb leaves 1.6 m below the pivot
            yield return Konumla(p, Quaternion.LookRotation(ileri) * Quaternion.Euler(aci, 0f, 0f));
            ucus.enabled = true;
            ucus.GazAyarla(Mathf.InverseLerp(ucus.EnAzHiz, ucus.EnCokHiz, hiz));
        }

        [UnityTest]
        public IEnumerator Bomba_ReleasedInTheWindow_DestroysTheDump()
        {
            yield return Yukle();
            MuhimmatMevzii hedef = Object.FindObjectsByType<MuhimmatMevzii>().OrderBy(m => m.name).First();
            yield return DalisaKoy(hedef, 60f, 200f);
            Assert.That(ucus.Hiz, Is.EqualTo(85f).Within(0.01f));
            Assert.IsTrue(BombaYuku.PencereIcindeMi(BombaYuku.DalisAcisi(ucus.transform.forward), ZeminOlcer.Irtifa(yuk.BirakmaNoktasi)));

            Bomba b = yuk.Birak();
            ucus.enabled = false; // freeze the aircraft; the bomb keeps the release velocity
            Assert.IsNotNull(b);
            Assert.AreEqual(1, yuk.Kalan);
            Vector3 patlama = Vector3.zero;
            bool patladi = false;
            b.Patlayinca += (_, p) => { patladi = true; patlama = p; };
            float t = 0f;
            while (!patladi && t < 10f)
            {
                yield return new WaitForFixedUpdate();
                t += Time.fixedDeltaTime;
            }
            Assert.IsTrue(patladi, "the bomb hit the ground");
            Assert.Less(Vector3.Distance(new Vector3(patlama.x, 0f, patlama.z), new Vector3(hedef.transform.position.x, 0f, hedef.transform.position.z)), 15f,
                "free fall lands where the closed form says");
            yield return null;
            Assert.IsTrue(hedef.Imha, "dump destroyed");
            Assert.AreEqual(1, seviye.ImhaSayisi);
            Assert.AreEqual(100, seviye.Puan);
            Assert.AreEqual(Saglik.VarsayilanCan, saglik.Can, "released above 120 m: no fragment damage");
        }

        [UnityTest]
        public IEnumerator DalisGostergesi_InTheWindow_FrameGreen_RingOnThePredictedImpact()
        {
            yield return Yukle();
            DalisGostergesi hud = Object.FindAnyObjectByType<DalisGostergesi>();
            MuhimmatMevzii hedef = Object.FindObjectsByType<MuhimmatMevzii>().OrderBy(m => m.name).First();
            yield return DalisaKoy(hedef, 60f, 200f);
            yield return null;
            Assert.That(hud.DalisAcisi, Is.EqualTo(60f).Within(1.5f));
            Assert.That(hud.Irtifa, Is.InRange(185f, 215f));
            Assert.IsTrue(hud.PencereIcinde);
            var cerceve = hud.GetComponent<UnityEngine.UI.Image>();
            Assert.Greater(cerceve.color.g, cerceve.color.r, "frame green inside the window");
            Assert.IsTrue(hud.TahminVar);
            Vector3 h = hedef.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(hud.TahminNoktasi.x, hud.TahminNoktasi.z), new Vector2(h.x, h.z)), 20f,
                "the ring is drawn where the bomb would land");
            Transform halka = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Single(t => t.name == "NisanHalkasi");
            Assert.IsTrue(halka.gameObject.activeSelf, "ring visible");

            // Level flight: outside the window, no ring.
            yield return Konumla(ucus.transform.position + Vector3.up * 300f, Quaternion.LookRotation(new Vector3(1f, 0f, 0f)));
            ucus.enabled = true;
            yield return null;
            yield return null;
            Assert.IsFalse(hud.PencereIcinde);
            Assert.IsFalse(hud.TahminVar);
            Assert.IsFalse(halka.gameObject.activeSelf);
            Assert.Greater(cerceve.color.r, cerceve.color.g);
        }

        [UnityTest]
        public IEnumerator Bomba_ReleasedBelow120m_FragmentsHitTheAircraft()
        {
            yield return Yukle();
            MuhimmatMevzii hedef = Object.FindObjectsByType<MuhimmatMevzii>().OrderBy(m => m.name).Last();
            yield return DalisaKoy(hedef, 50f, 90f);
            int can0 = saglik.Can;
            Bomba b = yuk.Birak();
            ucus.enabled = false;
            Assert.Less(b.BirakmaIrtifasi, 120f);
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Bomba\].*parça tesirine"));
            float t = 0f;
            while (b != null && t < 10f)
            {
                yield return new WaitForFixedUpdate();
                t += Time.fixedDeltaTime;
            }
            Assert.Less(saglik.Can, can0, "own fragments damaged the aircraft");
        }

        [UnityTest]
        public IEnumerator Ikmal_OnlyWhenNotFull_AddsBombsAnd90s()
        {
            yield return Yukle();
            float s0 = seviye.KalanSure;
            seviye.UsseVarildi();
            Assert.AreEqual(0, seviye.IkmalSayisi, "full load: no resupply");
            Assert.That(seviye.KalanSure, Is.EqualTo(s0).Within(0.001f));

            ucus.transform.SetPositionAndRotation(new Vector3(1000f, 400f, 0f), Quaternion.identity); // away from the base trigger
            Physics.SyncTransforms();
            yuk.Birak();
            Assert.AreEqual(1, yuk.Kalan);
            float s1 = seviye.KalanSure;
            seviye.UsseVarildi();
            Assert.AreEqual(1, seviye.IkmalSayisi);
            Assert.AreEqual(2, yuk.Kalan);
            Assert.That(seviye.KalanSure, Is.EqualTo(s1 + 90f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Ikmal_ThroughTheBaseTrigger()
        {
            yield return Yukle();
            yield return Konumla(new Vector3(800f, 300f, 0f), Quaternion.identity);
            ucus.enabled = true;
            Assert.IsNotNull(yuk.Birak());
            yield return Konumla(new Vector3(0f, 60f, -100f), Quaternion.identity); // just outside the 120 m base box
            ucus.enabled = true;
            ucus.GazAyarla(0.5f);
            float t = 0f;
            while (seviye.IkmalSayisi == 0 && t < 3f)
            {
                yield return null;
                t += Time.deltaTime;
            }
            Assert.AreEqual(1, seviye.IkmalSayisi, "flying into the base trigger resupplies");
            Assert.AreEqual(2, yuk.Kalan);
        }

        [UnityTest]
        public IEnumerator Time_RunsOut_LevelFailsAndAircraftStops()
        {
            yield return Yukle();
            seviye.Adim(seviye.KalanSure + 1f);
            Assert.IsTrue(seviye.Bitti);
            Assert.IsFalse(seviye.Basarili);
            Assert.AreEqual(SeviyeYoneticisi.NedenSure, seviye.Neden);
            Assert.IsFalse(ucus.enabled);
            Assert.IsNull(yuk.Birak(), "no release after the end");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllDumpsDestroyed_Succeeds_ScoreIncludesTimeBonus()
        {
            yield return Yukle();
            foreach (MuhimmatMevzii m in Object.FindObjectsByType<MuhimmatMevzii>())
                m.Saglik.Vur(1000);
            Assert.IsTrue(seviye.Bitti);
            Assert.IsTrue(seviye.Basarili);
            Assert.AreEqual(400 + Mathf.FloorToInt(seviye.KalanSure), seviye.Puan);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SesDinleyici_InScene_HearsTheEngine_NotWhenThrottleIsCut()
        {
            yield return Yukle();
            SesDinleyici kulak = Object.FindAnyObjectByType<SesDinleyici>();
            Vector3 p = kulak.transform.position + new Vector3(0f, 150f, -600f);
            yield return Konumla(p, Quaternion.Euler(0f, 90f, 0f));
            ucus.enabled = true;
            ucus.GazAyarla(1f);
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.IsTrue(kulak.Duyuyor);
            Assert.Greater(kulak.Sapma, 0f, "heard position lags the true position");
            ucus.GazAyarla(0f);
            yield return null;
            yield return null;
            Assert.IsFalse(kulak.Duyuyor, "gaz kesilince Duyuyor söner");
        }

        /// <summary>
        /// Step 05 support measurement (not the Profiler task itself): cost of the line-of-sight rays with 3 and 8 positions,
        /// and rays per second with a check every frame versus 5 times per second. Written to Logs/lab5/olcum_gorus.txt.
        /// </summary>
        [UnityTest]
        public IEnumerator Olcum_GorusIsini_3ve8Mevzi_HerKareVe5Hz()
        {
            yield return Yukle();
            yield return Konumla(new Vector3(1600f, 600f, 400f), Quaternion.identity); // above Tepe 3: inside all three cones and ranges; aircraft frozen, guns idle
            Radar[] uc = Object.FindObjectsByType<Radar>().Where(r => r.EngelKatmani.value != 0).ToArray();
            Assert.AreEqual(3, uc.Length);
            var sekiz = uc.ToList();
            for (int i = 0; sekiz.Count < 8; i++)
            {
                Radar kopya = Object.Instantiate(uc[i % 3].gameObject, uc[i % 3].transform.position + new Vector3(40f * (i + 1), 0f, 0f), uc[i % 3].transform.rotation).GetComponent<Radar>();
                kopya.GetComponent<UcaksavarMevzii>().enabled = false;
                sekiz.Add(kopya);
            }
            Physics.SyncTransforms();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Lab 5 Step 05 measurement, {System.DateTime.Now:yyyy-MM-dd HH:mm}, batch-mode PlayMode test (Stopwatch, not the Profiler)");
            foreach (var grup in new[] { uc, sekiz.ToArray() })
            {
                const int n = 2000;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (int k = 0; k < n; k++)
                    foreach (Radar r in grup)
                        r.Denetle();
                sw.Stop();
                double msKare = sw.Elapsed.TotalMilliseconds / n;
                sb.AppendLine($"{grup.Length} positions: {msKare:F4} ms per frame for all line-of-sight checks ({msKare * 1000.0 / grup.Length:F2} µs per ray); sees: {grup.Count(r => r.Goruyor)}/{grup.Length}");
            }

            // Rays per second with denetimAraligi 0 (every frame) and 0.2 s (5 Hz), 8 positions, real frames.
            foreach (float aralik in new[] { 0f, 0.2f })
            {
                foreach (Radar r in sekiz) r.DenetimAraligi = aralik;
                int once = sekiz.Sum(r => r.IsinSayisi);
                float t0 = Time.time;
                int kare = 0;
                while (Time.time - t0 < 2f)
                {
                    ucus.transform.position = new Vector3(1600f, 600f, 400f);
                    yield return null;
                    kare++;
                }
                float sure = Time.time - t0;
                int isin = sekiz.Sum(r => r.IsinSayisi) - once;
                sb.AppendLine($"denetimAraligi {aralik:F1} s: {isin / sure:F0} rays/s over {sure:F2} s ({kare / sure:F0} frames/s, 8 positions)");
                if (aralik > 0f)
                    Assert.LessOrEqual(isin, 8 * (Mathf.CeilToInt(sure / aralik) + 1), "5 Hz: at most 5 rays per position per second");
            }
            Directory.CreateDirectory(LogDir);
            File.WriteAllText(Path.Combine(LogDir, "olcum_gorus.txt"), sb.ToString());
            Debug.Log(sb.ToString());
        }

        [UnityTest]
        public IEnumerator Ucaksavar_FiresOnlyWithData_NotWhenSilentAndHidden()
        {
            yield return Yukle();
            UcaksavarMevzii top = Bul("Ucaksavar 2").GetComponent<UcaksavarMevzii>();
            Tepe tepe = Bul("Tepe 3").GetComponent<Tepe>();
            Vector3 merkez = tepe.transform.position, goz = top.Goz.Goz;
            Vector3 yon = new Vector3(merkez.x - goz.x, 0f, merkez.z - goz.z).normalized;
            Vector3 arka = merkez + yon * 450f;
            arka.y = ZeminOlcer.Yukseklik(arka.x, arka.z) + 60f;
            yield return Konumla(arka, Quaternion.LookRotation(yon));
            ucus.GazAyarla(0f);   // silent
            ucus.enabled = true;  // the guns ignore a stopped aircraft
            int atis0 = top.AtisSayisi;
            for (int i = 0; i < 20; i++)
            {
                ucus.transform.position = arka; // hold the pose while the gun updates
                yield return null;
            }
            Assert.IsFalse(top.VeriVar, "behind the hill with the engine cut: neither seen nor heard");
            Assert.AreEqual(atis0, top.AtisSayisi, "no data, no fire");
        }
    }
}
