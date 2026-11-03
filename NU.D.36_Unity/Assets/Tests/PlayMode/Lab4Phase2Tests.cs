using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>Lab 4 phase 2: countdown, landing / crash classification, unified failure and delayed restart.</summary>
    public class Lab4Phase2Tests
    {
        readonly List<GameObject> olusan = new List<GameObject>();

        [TearDown]
        public void Temizle()
        {
            foreach (GameObject g in olusan)
                if (g != null)
                    Object.DestroyImmediate(g);
            olusan.Clear();
        }

        // ---- builders ----
        class Sistem
        {
            public GorevYoneticisi yonetici;
            public PuanCuzdani cuzdan;
            public GorevSonuEkrani ekran;
            public YenidenBasla yenidenBasla;
            public InisAlgilayici algilayici;
            public UcusKontrol ucus;
            public KesifHedefi[] hedefler;
            public UsGirisi us;
            public Collider ucakCollider;
            public List<string> yuklenenler = new List<string>();
            public List<string> basarisizlar = new List<string>();
            public int inisSayisi, bitisSayisi;
        }

        /// <summary>Mission, wallet, screen, restart handler and a flat ground at y = 0 with a level aircraft.</summary>
        Sistem Kur(int hedefSayisi = 2, float sure = 150f)
        {
            var s = new Sistem();
            var ucakGo = new GameObject("TestUcak");
            olusan.Add(ucakGo);
            s.ucakCollider = ucakGo.AddComponent<BoxCollider>();
            ucakGo.SetActive(false);
            s.ucus = ucakGo.AddComponent<UcusKontrol>();
            s.algilayici = ucakGo.AddComponent<InisAlgilayici>();
            s.algilayici.Ucus = s.ucus;
            s.algilayici.TekerOfseti = 1f;
            s.algilayici.ZeminYuksekligi = (x, z) => 0f;
            ucakGo.SetActive(true);
            s.ucus.enabled = false; // the test moves the aircraft itself
            s.ucus.GazAyarla(0f);   // 8 m/s
            ucakGo.transform.position = new Vector3(0f, 50f, 0f);

            s.hedefler = new KesifHedefi[hedefSayisi];
            for (int i = 0; i < hedefSayisi; i++)
            {
                var h = new GameObject("TestHedef" + i);
                olusan.Add(h);
                h.AddComponent<BoxCollider>().isTrigger = true;
                s.hedefler[i] = h.AddComponent<KesifHedefi>();
            }
            var usGo = new GameObject("TestUs");
            olusan.Add(usGo);
            usGo.AddComponent<BoxCollider>().isTrigger = true;
            s.us = usGo.AddComponent<UsGirisi>();

            var oyun = new GameObject("TestOyun");
            olusan.Add(oyun);
            oyun.SetActive(false);
            s.cuzdan = oyun.AddComponent<PuanCuzdani>();
            s.yonetici = oyun.AddComponent<GorevYoneticisi>();
            s.ekran = oyun.AddComponent<GorevSonuEkrani>();
            s.yenidenBasla = oyun.AddComponent<YenidenBasla>();
            s.yonetici.Kur(s.hedefler, s.us);
            s.yonetici.SureAyarla(sure);
            s.yonetici.Algilayici = s.algilayici;
            s.us.Yonetici = s.yonetici;
            s.ekran.Yonetici = s.yonetici;
            s.ekran.Cuzdan = s.cuzdan;
            s.yenidenBasla.Yonetici = s.yonetici;
            s.yenidenBasla.Ucus = s.ucus;
            s.yenidenBasla.SahneYukle = s.yuklenenler.Add;
            oyun.SetActive(true);

            s.yonetici.GorevBasarisiz += n => s.basarisizlar.Add(n);
            s.yonetici.GorevTamamlandi += () => s.bitisSayisi++;
            s.algilayici.InisYapildi += () => s.inisSayisi++;
            return s;
        }

        static void Yerlestir(Sistem s, float hiz01, float pitchYukari, float yatis, float yukseklik = 0.5f)
        {
            s.ucus.GazAyarla(hiz01);
            Transform t = s.ucus.transform;
            t.rotation = Quaternion.Euler(-pitchYukari, 0f, yatis); // negative X Euler = nose up
            t.position = new Vector3(0f, yukseklik, 0f);            // wheels at y - 1 < 0: touching
        }

        static void HepsiniTopla(Sistem s)
        {
            foreach (KesifHedefi h in s.hedefler)
                h.Dene(s.ucakCollider);
        }

        // ---- countdown ----
        [Test]
        public void Countdown_DefaultIs150Seconds_AndDecreasesWithTheStepGiven()
        {
            Sistem s = Kur();
            Assert.AreEqual(150f, s.yonetici.KalanSure);
            s.yonetici.Adim(1.5f);
            s.yonetici.Adim(0.25f);
            Assert.AreEqual(148.25f, s.yonetici.KalanSure, 1e-4f);
            Assert.AreEqual(GorevDurumu.Aktif, s.yonetici.Durum);
        }

        [Test]
        public void Countdown_ExpiryFailsTheMissionExactlyOnce_WithTheTimeoutReason()
        {
            Sistem s = Kur(sure: 1f);
            s.yonetici.Adim(0.6f);
            Assert.IsFalse(s.yonetici.Basarisiz);
            s.yonetici.Adim(0.6f);
            s.yonetici.Adim(5f);
            s.yonetici.Adim(5f);
            Assert.AreEqual(new[] { "SÜRE DOLDU" }, s.basarisizlar.ToArray());
            Assert.AreEqual(GorevDurumu.Basarisiz, s.yonetici.Durum);
            Assert.AreEqual(0f, s.yonetici.KalanSure);
        }

        [Test]
        public void Countdown_CanBeDisabledWithZeroOrNegativeDuration()
        {
            Sistem s = Kur(sure: 0f);
            Assert.IsFalse(s.yonetici.GeriSayimAktif);
            s.yonetici.Adim(1000f);
            Assert.IsFalse(s.yonetici.Basarisiz);
            s.yonetici.SureAyarla(-5f);
            s.yonetici.Adim(1000f);
            Assert.IsFalse(s.yonetici.Basarisiz);
        }

        [Test]
        public void CompletedMission_NeverFails_ByTimerOrByCrash_AndTheTimerStops()
        {
            Sistem s = Kur(sure: 10f);
            s.yonetici.Adim(3f);
            HepsiniTopla(s);
            Assert.IsTrue(s.us.Dene(s.ucakCollider));
            Assert.AreEqual(GorevDurumu.Tamamlandi, s.yonetici.Durum);
            float kalan = s.yonetici.KalanSure;
            s.yonetici.Adim(100f);
            Assert.AreEqual(kalan, s.yonetici.KalanSure, "countdown stopped");
            Yerlestir(s, 1f, 0f, 0f); // would be a crash
            s.algilayici.Adim();
            Assert.IsTrue(s.algilayici.Cakti, "the detector still detects");
            Assert.IsFalse(s.yonetici.GoreviBasarisizYap("x"));
            Assert.IsFalse(s.yonetici.Basarisiz);
            Assert.AreEqual(0, s.basarisizlar.Count);
            Assert.AreEqual(1, s.bitisSayisi);
            Assert.IsFalse(s.ekran.BasarisizGorunur);
        }

        // ---- landing / crash classification ----
        [Test]
        public void ControlledLanding_IsNotACrash_AndHoldsTheAircraftOnTheGround()
        {
            Sistem s = Kur();
            Yerlestir(s, 0f, 5f, 10f); // 8 m/s, 5 deg nose up, 10 deg bank
            s.algilayici.Adim();
            Assert.IsFalse(s.algilayici.Cakti);
            Assert.IsTrue(s.algilayici.Yerde);
            Assert.AreEqual(1, s.inisSayisi);
            Assert.AreEqual(1f, s.ucus.transform.position.y, 1e-4f, "wheels rest on the ground");
            Assert.AreEqual(0, s.basarisizlar.Count);
        }

        [Test]
        public void LandingOnAGentleSlope_WithTheNoseAlongTheSlope_IsControlled()
        {
            Sistem s = Kur();
            s.algilayici.ZeminYuksekligi = (x, z) => z * Mathf.Tan(10f * Mathf.Deg2Rad); // rises ahead (+Z) by 10 degrees
            Yerlestir(s, 0f, 10f, 0f);
            s.algilayici.Adim();
            Assert.IsFalse(s.algilayici.Cakti, "pitch error relative to the slope is 0");
            Assert.IsTrue(s.algilayici.Yerde);
        }

        [TestCase(1f, 0f, 0f, "çok hızlı", Description = "42 m/s")]
        [TestCase(0f, 30f, 0f, "dik açı", Description = "30 degrees nose up")]
        [TestCase(0f, -30f, 0f, "dik açı", Description = "30 degrees nose down")]
        [TestCase(0f, 0f, 40f, "aşırı yatış", Description = "40 degrees bank")]
        [TestCase(0f, 0f, 180f, "aşırı yatış", Description = "upside down")]
        public void HarshContact_IsACrash_WithItsReason(float gaz, float pitch, float yatis, string beklenenNeden)
        {
            Sistem s = Kur();
            Yerlestir(s, gaz, pitch, yatis);
            s.algilayici.Adim();
            Assert.IsTrue(s.algilayici.Cakti);
            Assert.AreEqual(beklenenNeden, s.algilayici.CakmaNedeni);
            Assert.AreEqual(0, s.inisSayisi);
        }

        [Test]
        public void ThresholdsAreInclusive_AndConfigurable()
        {
            Sistem s = Kur();
            s.algilayici.EnFazlaYatis = 45f;
            Yerlestir(s, 0f, 0f, 40f);
            s.algilayici.Adim();
            Assert.IsFalse(s.algilayici.Cakti, "40 degrees is fine with a 45 degree limit");
            Assert.IsNull(s.algilayici.Siniflandir(16f, 18f, 27f, 20f), "exactly on every limit is a landing");
            Assert.IsNotNull(s.algilayici.Siniflandir(16.1f, 0f, 0f, 0f));
        }

        [Test]
        public void SteepGroundIsACrash_EvenWhenAligned()
        {
            Sistem s = Kur();
            s.algilayici.ZeminYuksekligi = (x, z) => z * Mathf.Tan(35f * Mathf.Deg2Rad);
            Yerlestir(s, 0f, 35f, 0f);
            s.algilayici.Adim();
            Assert.IsTrue(s.algilayici.Cakti);
            Assert.AreEqual("dik arazi", s.algilayici.CakmaNedeni);
        }

        [Test]
        public void RepeatedDetection_RaisesEachEventOnce()
        {
            Sistem s = Kur();
            int cakma = 0;
            s.algilayici.Cakildi += _ => cakma++;
            Yerlestir(s, 1f, 0f, 0f);
            for (int i = 0; i < 10; i++)
                s.algilayici.Adim();
            Assert.AreEqual(1, cakma);
            Assert.AreEqual(new[] { "ÇAKILDIN" }, s.basarisizlar.ToArray());

            Sistem l = Kur();
            Yerlestir(l, 0f, 0f, 0f);
            for (int i = 0; i < 10; i++)
                l.algilayici.Adim();
            Assert.AreEqual(1, l.inisSayisi, "staying on the ground is not a new landing");
            // climb away, then touch down again: a second landing is allowed
            l.ucus.transform.position = new Vector3(0f, 10f, 0f);
            l.algilayici.Adim();
            Assert.IsFalse(l.algilayici.Yerde);
            l.ucus.transform.position = new Vector3(0f, 0.5f, 0f);
            l.algilayici.Adim();
            Assert.AreEqual(2, l.inisSayisi);
        }

        [Test]
        public void NormalFlight_AndTheStartPosition_NeverCountAsContact()
        {
            Sistem s = Kur();
            s.ucus.transform.position = new Vector3(0f, 20f, 0f);
            for (int i = 0; i < 5; i++)
                s.algilayici.Adim();
            Assert.IsFalse(s.algilayici.Cakti);
            Assert.IsFalse(s.algilayici.Yerde);
            Assert.AreEqual(0, s.inisSayisi);
            Assert.That(AraziUretici.YuzeyYuksekligi(0f, 0f) + 1.22f, Is.LessThan(20f));
        }

        // ---- failure, HUD, restart ----
        [Test]
        public void Crash_FailsTheMission_ShowsTheReason_AndRestartsRota04AfterTheDelay()
        {
            Sistem s = Kur();
            Assert.AreEqual(2.5f, YenidenBasla.VarsayilanBekleme);
            Assert.AreEqual(2.5f, s.yenidenBasla.Bekleme);
            s.ucus.enabled = true;
            Yerlestir(s, 1f, 0f, 0f);
            s.ucus.enabled = true;
            s.algilayici.Adim();

            Assert.AreEqual(GorevDurumu.Basarisiz, s.yonetici.Durum);
            Assert.AreEqual("ÇAKILDIN", s.yonetici.BasarisizNedeni);
            Assert.AreEqual("ÇAKILDIN", s.ekran.BasarisizNedeni);
            Assert.IsTrue(s.ekran.BasarisizGorunur);
            Assert.IsFalse(s.ekran.Gorunur, "failure screen, not the success screen");
            Assert.IsTrue(s.yenidenBasla.Bekliyor);
            Assert.IsFalse(s.ucus.enabled, "aircraft stopped");

            s.yenidenBasla.Adim(2.4f);
            Assert.AreEqual(0, s.yuklenenler.Count, "still waiting");
            s.yenidenBasla.Adim(0.2f);
            s.yenidenBasla.Adim(5f);
            Assert.AreEqual(new[] { "Rota04" }, s.yuklenenler.ToArray(), "reloaded once, Rota04");
        }

        [Test]
        public void Timeout_FailsTheMission_ShowsTheReason_AndRestarts()
        {
            Sistem s = Kur(sure: 2f);
            s.yonetici.Adim(2.1f);
            Assert.AreEqual("SÜRE DOLDU", s.ekran.BasarisizNedeni);
            Assert.IsTrue(s.yenidenBasla.Bekliyor);
            s.yenidenBasla.Adim(2.5f);
            Assert.AreEqual(new[] { "Rota04" }, s.yuklenenler.ToArray());
        }

        [Test]
        public void CrashAndTimeout_TogetherProduceOneFailure()
        {
            Sistem s = Kur(sure: 1f);
            Yerlestir(s, 1f, 0f, 0f);
            s.algilayici.Adim();
            s.yonetici.Adim(5f);
            Assert.AreEqual(new[] { "ÇAKILDIN" }, s.basarisizlar.ToArray());
            Assert.AreEqual("ÇAKILDIN", s.yenidenBasla.Neden);
        }

        [Test]
        public void FailedMission_AwardsNoMorePoints_AndCannotBeCompleted()
        {
            Sistem s = Kur(hedefSayisi: 3, sure: 1f);
            s.hedefler[0].Dene(s.ucakCollider);
            Assert.AreEqual(100, s.cuzdan.Puan);
            s.yonetici.Adim(2f);
            Assert.IsFalse(s.hedefler[1].Dene(s.ucakCollider));
            Assert.IsFalse(s.hedefler[2].Dene(s.ucakCollider));
            Assert.AreEqual(100, s.cuzdan.Puan, "no points after the failure");
            Assert.AreEqual(2, s.yonetici.Kalan);
            Assert.IsFalse(s.yonetici.DonusAsamasi);
            s.us.Dene(s.ucakCollider);
            s.yonetici.UsseVarildi();
            Assert.IsFalse(s.yonetici.Tamamlandi);
            Assert.AreEqual(0, s.bitisSayisi);
            Assert.IsFalse(s.ekran.Gorunur);
        }

        [Test]
        public void FailureAfterTheReturnPhaseOpened_ClosesTheBase()
        {
            Sistem s = Kur(sure: 5f);
            HepsiniTopla(s);
            Assert.IsTrue(s.us.gameObject.activeSelf);
            s.yonetici.Adim(6f);
            Assert.IsFalse(s.us.gameObject.activeSelf);
            Assert.IsFalse(s.yonetici.Tamamlandi);
        }

        // ---- real Rota04 ----
        IEnumerator Rota04Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Rota04", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Rota04_Countdown_RunsOnDeltaTime_AndStartsAt150()
        {
            yield return Rota04Yukle();
            var y = Object.FindFirstObjectByType<GorevYoneticisi>();
            Assert.AreEqual(150f, y.Sure);
            float t0 = Time.time, k0 = y.KalanSure;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(Time.time - t0, k0 - y.KalanSure, 0.05f);
            Assert.AreEqual(GorevDurumu.Aktif, y.Durum);
        }

        [UnityTest]
        public IEnumerator Rota04_Timeout_ShowsFailureAndReloadsWithFreshState()
        {
            yield return Rota04Yukle();
            var y = Object.FindFirstObjectByType<GorevYoneticisi>();
            var ekran = Object.FindFirstObjectByType<GorevSonuEkrani>();
            var ucus = Object.FindFirstObjectByType<UcusKontrol>();
            y.SureAyarla(0.5f);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual("SÜRE DOLDU", y.BasarisizNedeni);
            Assert.IsTrue(ekran.BasarisizGorunur);
            Assert.IsFalse(ucus.enabled, "flight stopped after failure");
            Vector3 p = ucus.transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(p, ucus.transform.position, "aircraft no longer moves");

            yield return new WaitForSeconds(2.5f); // 2.5 s delay, then reload
            Assert.AreEqual("Rota04", SceneManager.GetActiveScene().name);
            y = Object.FindFirstObjectByType<GorevYoneticisi>();
            Assert.IsFalse(y.Basarisiz);
            Assert.Greater(y.KalanSure, 140f);
            Assert.AreEqual(3, y.Kalan);
            Assert.IsFalse(Object.FindFirstObjectByType<GorevSonuEkrani>().BasarisizGorunur, "no stale failure message");
            Assert.IsTrue(Object.FindFirstObjectByType<UcusKontrol>().enabled);
            Assert.AreEqual(0, Object.FindFirstObjectByType<PuanCuzdani>().Puan);
        }

        [UnityTest]
        public IEnumerator Rota04_CrashIntoTheGround_FailsTheMissionWithTheRealDetector()
        {
            yield return Rota04Yukle();
            var y = Object.FindFirstObjectByType<GorevYoneticisi>();
            var ucus = Object.FindFirstObjectByType<UcusKontrol>();
            Assert.IsNotNull(y.Algilayici);
            ucus.transform.position = new Vector3(0f, 0.2f, 0f); // wheels (1.22 m below) under the flat start ground at -1 m
            ucus.transform.rotation = Quaternion.identity;       // default throttle: 25 m/s, above the 16 m/s limit
            yield return null;
            yield return null;
            Assert.IsTrue(y.Basarisiz);
            Assert.AreEqual("ÇAKILDIN", y.BasarisizNedeni);
            Assert.IsFalse(ucus.enabled);
        }

        [UnityTest]
        public IEnumerator Rota04_SlowLevelTouchdown_IsALanding_AndTheMissionContinues()
        {
            yield return Rota04Yukle();
            var y = Object.FindFirstObjectByType<GorevYoneticisi>();
            var ucus = Object.FindFirstObjectByType<UcusKontrol>();
            var det = y.Algilayici;
            int inis = 0;
            det.InisYapildi += () => inis++;
            ucus.GazAyarla(0f);
            ucus.transform.position = new Vector3(0f, 0.2f, 0f);
            ucus.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(det.Cakti);
            Assert.IsTrue(det.Yerde);
            Assert.AreEqual(1, inis);
            Assert.IsFalse(y.Basarisiz);
            Assert.AreEqual(AraziUretici.YuzeyYuksekligi(ucus.transform.position.x, ucus.transform.position.z) + det.TekerOfseti,
                ucus.transform.position.y, 0.05f, "rolling on the surface");
        }
    }
}
