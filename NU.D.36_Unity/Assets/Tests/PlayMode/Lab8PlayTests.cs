using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using H = NU36.KursunDaglariHaritasi;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 8 (Hafta 8) phase 1 in Play Mode, scene Bolum4_KursunDaglari (Documentation/Lab8_Plan.md §9): the aircraft
    /// starts in the air above the train and keeps flying (TEST-806), the train stays on the rails in the bends with a
    /// constant spacing (TEST-808) and reaches the safe zone in 5 minutes ±15 s with no enemies (TEST-809).
    /// Measurements → Logs/lab8/tren_olcumu.txt.
    /// </summary>
    public class Lab8PlayTests
    {
        const string Sahne = "Bolum4_KursunDaglari";
        UcusKontrol ucus;
        Tren tren;
        static readonly StringBuilder Olcum = new StringBuilder();

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync(Sahne, LoadSceneMode.Single);
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            tren = Object.FindAnyObjectByType<Tren>();
        }

        [TearDown]
        public void Bitir() => Time.timeScale = 1f;

        [OneTimeTearDown]
        public void Yaz()
        {
            if (Olcum.Length == 0) return;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab8"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "tren_olcumu.txt"), $"Lab 8 train and start, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        static IEnumerator Bekle(float saniye)
        {
            float bitis = Time.time + saniye;
            while (Time.time < bitis)
                yield return null;
        }

        /// <summary>The train runs on its own; the aircraft is parked in the air so a long accelerated run cannot crash it.</summary>
        void UcagiDondur()
        {
            ucus.enabled = false;
            if (ucus.TryGetComponent(out InisAlgilayici a)) a.enabled = false;
            if (ucus.TryGetComponent(out UcusYardimcisi y)) y.enabled = false;
        }

        /// <summary>Worksheet 03 measures the run "düşmanlar kapalıyken": the fire zones never wake.</summary>
        static void DusmanlariKapat()
        {
            foreach (AtesBolgesi b in Object.FindObjectsByType<AtesBolgesi>(FindObjectsSortMode.None))
                b.enabled = false;
        }

        // ---- TEST-806 ----------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Start_InTheAirAboveTheTrain_KeepsFlyingWithNoInput_AssistsOn()
        {
            yield return Yukle();
            yield return null;
            Transform lok = tren.Araclar[0].transform;
            float ilkFark = ucus.transform.position.y - lok.position.y;
            float ilkY = ucus.transform.position.y;
            Assert.That(ilkFark, Is.EqualTo(150f).Within(20f), "150 m above the locomotive");
            Assert.IsTrue(ucus.KolayUcus, "easy flight on at the start, as in Rota07");
            Assert.IsTrue(ucus.GetComponent<UcusYardimcisi>().enabled, "assists on at the start, as in Rota07");
            Assert.That(ucus.Hiz * 3.6f, Is.EqualTo(220f).Within(10f), "starts at 220 km/h");

            float enAzYer = float.MaxValue, enCokSapma = 0f;
            float bitis = Time.time + 5f;
            while (Time.time < bitis)
            {
                yield return null;
                enAzYer = Mathf.Min(enAzYer, ZeminOlcer.Irtifa(ucus.transform.position));
                enCokSapma = Mathf.Max(enCokSapma, Mathf.Abs(ucus.transform.position.y - ilkY));
            }
            var a = ucus.GetComponent<InisAlgilayici>();
            Olcum.AppendLine($"TEST-806 start {ilkFark:F0} m above the locomotive; first 5 s: lowest {enAzYer:F0} m above ground, height change at most {enCokSapma:F1} m, speed {ucus.Hiz * 3.6f:F0} km/h");
            Assert.IsFalse(a.Cakti, "no crash");
            Assert.IsFalse(a.Yerde, "still flying");
            Assert.Greater(enAzYer, 60f, "well clear of the ground");
            Assert.Less(enCokSapma, 30f, "height held with no input");
            Assert.Greater(ucus.Hiz * 3.6f, 150f, "still flying fast");
        }

        // ---- TEST-808, TEST-809 ------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Train_StaysOnTheRails_KeepsItsSpacing_ArrivesInFiveMinutes()
        {
            yield return Yukle();
            UcagiDondur();
            DusmanlariKapat();
            RayEgrisi e = tren.Hat.Egri;
            TrenAraci[] araclar = tren.Araclar;
            Rigidbody[] govdeler = araclar.Select(x => x.GetComponent<Rigidbody>()).ToArray();
            float baslangic = Time.fixedTime - tren.Gecen;
            float varis = -1f;
            tren.GuvenliBolgeyeVardi += () => varis = Time.fixedTime - baslangic;
            float hedefBosluk = -1f;
            float enCokBojiSapmasi = 0f, enAzBosluk = float.MaxValue, enCokBosluk = 0f, enAzR = float.PositiveInfinity;
            var bolgeZamani = new float[H.Bolgeler.Length];
            for (int i = 0; i < bolgeZamani.Length; i++) bolgeZamani[i] = -1f;

            Time.timeScale = 20f;
            float sinir = Time.realtimeSinceStartup + 300f;
            int adim = 0;
            while (!tren.DurduMu && Time.realtimeSinceStartup < sinir)
            {
                yield return new WaitForFixedUpdate();
                float t = Time.fixedTime - baslangic;
                for (int i = 0; i < bolgeZamani.Length; i++)
                    if (bolgeZamani[i] < 0f && tren.Konum >= H.Bolgeler[i].Bas * e.Uzunluk)
                        bolgeZamani[i] = t;
                if (adim++ % 5 != 0)
                    continue;   // the geometry checks every 5th step (0.1 s of game time, ≈ 1.8 m of travel)
                enAzR = Mathf.Min(enAzR, e.EgrilikYaricapi(tren.Konum));

                // Bogies on the rails (read back from the bodies, rail top removed).
                Vector3 onceArka = Vector3.zero;
                for (int i = 0; i < araclar.Length; i++)
                {
                    Vector3 p = govdeler[i].position - Vector3.up * H.RayUstu;
                    Vector3 ileri = govdeler[i].rotation * Vector3.forward;
                    foreach (int yon in new[] { 1, -1 })
                    {
                        Vector3 boji = p + ileri * (yon * araclar[i].BojiAraligi);
                        e.EnYakin(boji, out float yan);
                        enCokBojiSapmasi = Mathf.Max(enCokBojiSapmasi, yan);
                    }
                    Vector3 on = p + ileri * (araclar[i].Uzunluk * 0.5f);
                    if (i > 0)
                    {
                        float bosluk = Vector3.Distance(onceArka, on);
                        if (hedefBosluk < 0f) hedefBosluk = bosluk;
                        enAzBosluk = Mathf.Min(enAzBosluk, bosluk);
                        enCokBosluk = Mathf.Max(enCokBosluk, bosluk);
                    }
                    onceArka = p - ileri * (araclar[i].Uzunluk * 0.5f);
                }
            }
            Time.timeScale = 1f;

            float onUcu = tren.Konum + araclar[0].Uzunluk * 0.5f;
            Olcum.AppendLine($"TEST-809 arrival at the safe zone gate after {varis:F1} s (target 300 ±15), stopped at s={tren.Konum:F0} m, nose {e.Uzunluk - onUcu:F1} m before the buffer");
            Olcum.AppendLine($"zones reached: {string.Join(", ", H.Bolgeler.Select((b, i) => $"{b.Ad} {Bolum4Hud.SureYazisi(bolgeZamani[i])}"))}");
            Olcum.AppendLine($"TEST-808 bogies at most {enCokBojiSapmasi:F3} m off the line; coupling gaps {enAzBosluk:F2} … {enCokBosluk:F2} m; tightest radius passed {enAzR:F0} m");

            Assert.IsTrue(tren.DurduMu, "the train stopped at the end (within the time limit)");
            Assert.That(varis, Is.EqualTo(300f).Within(15f), "worksheet 03: 5 min ±15 s with no enemies");
            Assert.GreaterOrEqual(tren.Konum, tren.KapiKonumu, "stopped inside the safe zone");
            Assert.Less(onUcu, e.Uzunluk - 2f, "stopped before the buffer stop");
            Assert.Less(enCokBojiSapmasi, 0.2f, "every bogie stays on the rails");
            Assert.Less(enCokBosluk - enAzBosluk, 0.3f, "constant spacing between the vehicles");
            Assert.That(bolgeZamani, Is.All.GreaterThan(0f).And.All.LessThan(300f), "all three zones before the safe zone");
            Assert.Less(enAzR, 400f, "the run includes a real bend");
        }

        [UnityTest]
        public IEnumerator Train_TimeLeftCountsDown_AndShowsOnTheHud()
        {
            yield return Yukle();
            UcagiDondur();
            yield return Bekle(1f);
            float kalan = tren.KalanSure;
            Assert.That(kalan, Is.InRange(298f, 299.5f));
            var hud = Object.FindAnyObjectByType<Bolum4Hud>();
            string yazi = hud.DurumYazisi();
            StringAssert.Contains("GÜVENLİ BÖLGE  4:5", yazi);
            StringAssert.Contains("TREN  %100", yazi);
            StringAssert.Contains("BOMBA 2", yazi);
            Assert.AreEqual(5, yazi.Split('\n').Length, "five lines (worksheet 08)");
        }
    }
}
