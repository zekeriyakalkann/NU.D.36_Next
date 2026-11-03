using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 4 phase 1. The first group builds the mission from plain GameObjects (logic of target,
    /// wallet, manager, base, screen); the second group plays the real Rota04 scene.
    /// </summary>
    public class Lab4GorevTests
    {
        readonly List<GameObject> olusan = new List<GameObject>();
        readonly List<(int puan, bool zorunlu)> fotograflar = new List<(int, bool)>();

        [SetUp]
        public void Abone() => KesifHedefi.Fotograflandi += Kaydet;

        [TearDown]
        public void Temizle()
        {
            KesifHedefi.Fotograflandi -= Kaydet;
            fotograflar.Clear();
            foreach (GameObject g in olusan)
                if (g != null)
                    Object.DestroyImmediate(g);
            olusan.Clear();
        }

        void Kaydet(int puan, bool zorunlu) => fotograflar.Add((puan, zorunlu));

        // ---- builders ----
        Collider Ucak()
        {
            var g = new GameObject("TestUcak");
            olusan.Add(g);
            var col = g.AddComponent<BoxCollider>();
            g.AddComponent<UcusKontrol>().enabled = false;
            return col;
        }

        Collider Baskasi()
        {
            var g = new GameObject("Balon");
            olusan.Add(g);
            return g.AddComponent<SphereCollider>();
        }

        KesifHedefi Hedef(int puan = 100, bool zorunlu = true)
        {
            var g = new GameObject("TestHedef");
            olusan.Add(g);
            g.AddComponent<BoxCollider>().isTrigger = true;
            var h = g.AddComponent<KesifHedefi>();
            h.Puan = puan;
            h.Zorunlu = zorunlu;
            return h;
        }

        T Bilesen<T>(GameObject g, bool etkin = false) where T : Component
        {
            g.SetActive(etkin);
            return g.AddComponent<T>();
        }

        class Gorev
        {
            public GorevYoneticisi yonetici;
            public PuanCuzdani cuzdan;
            public GorevSonuEkrani ekran;
            public UsGirisi us;
            public KesifHedefi[] hedefler;
        }

        Gorev GorevKur(params KesifHedefi[] hedefler)
        {
            var oyun = new GameObject("TestOyun");
            olusan.Add(oyun);
            oyun.SetActive(false); // components get Awake only after everything is wired
            var us = new GameObject("TestUs");
            olusan.Add(us);
            us.AddComponent<BoxCollider>().isTrigger = true;
            var g = new Gorev
            {
                hedefler = hedefler,
                us = us.AddComponent<UsGirisi>(),
                cuzdan = oyun.AddComponent<PuanCuzdani>(),
                yonetici = oyun.AddComponent<GorevYoneticisi>(),
                ekran = oyun.AddComponent<GorevSonuEkrani>(),
            };
            g.yonetici.Kur(hedefler, g.us);
            g.us.Yonetici = g.yonetici;
            g.ekran.Yonetici = g.yonetici;
            g.ekran.Cuzdan = g.cuzdan;
            oyun.SetActive(true);
            return g;
        }

        // ---- target ----
        [Test]
        public void Target_IgnoresObjectsThatAreNotThePlayerAircraft()
        {
            KesifHedefi h = Hedef();
            Assert.IsFalse(h.Dene(Baskasi()));
            Assert.IsFalse(h.Dene(null));
            Assert.IsFalse(h.Fotografli);
            Assert.AreEqual(0, fotograflar.Count);
        }

        [Test]
        public void Target_IsCollectedOnce_AndRaisesTheEventWithItsValues()
        {
            KesifHedefi h = Hedef(puan: 150, zorunlu: false);
            Collider ucak = Ucak();
            Assert.IsTrue(h.Dene(ucak));
            Assert.IsFalse(h.Dene(ucak), "second pass must not collect again");
            Assert.IsFalse(h.Dene(ucak));
            Assert.AreEqual(new[] { (150, false) }, fotograflar.ToArray());
            Assert.IsTrue(h.Fotografli);
            Assert.IsFalse(h.GetComponent<Collider>().enabled, "trigger is switched off after collection");
        }

        [Test]
        public void Target_HidesItsVisualWhenCollected()
        {
            KesifHedefi h = Hedef();
            var gorsel = new GameObject("Gorsel");
            gorsel.transform.SetParent(h.transform);
            h.Gorsel = gorsel;
            h.Dene(Ucak());
            Assert.IsFalse(gorsel.activeSelf);
        }

        // ---- wallet ----
        [Test]
        public void Wallet_AddsTheEventPointsOnce_AndReportsEachTotal()
        {
            Gorev g = GorevKur(Hedef(), Hedef());
            var toplamlar = new List<int>();
            g.cuzdan.PuanDegisti += toplamlar.Add;
            Collider ucak = Ucak();
            g.hedefler[0].Dene(ucak);
            g.hedefler[0].Dene(ucak);
            Assert.AreEqual(100, g.cuzdan.Puan);
            g.hedefler[1].Dene(ucak);
            Assert.AreEqual(200, g.cuzdan.Puan);
            Assert.AreEqual(new[] { 100, 200 }, toplamlar.ToArray());
        }

        [Test]
        public void Wallet_StopsListeningWhenDisabled_AndNeverSubscribesTwice()
        {
            Gorev g = GorevKur(Hedef(), Hedef(), Hedef());
            g.cuzdan.enabled = false;
            g.cuzdan.enabled = true;
            g.cuzdan.enabled = true;
            g.hedefler[0].Dene(Ucak());
            Assert.AreEqual(100, g.cuzdan.Puan);
            g.cuzdan.enabled = false;
            g.hedefler[1].Dene(Ucak());
            Assert.AreEqual(100, g.cuzdan.Puan, "a disabled wallet is not listening");
        }

        [Test]
        public void Wallet_UnlockEventFiresOnlyOnce_WhenTheThresholdIsReached()
        {
            Gorev g = GorevKur(Hedef(), Hedef(), Hedef(), Hedef());
            g.cuzdan.KilitEsigi = 300;
            var acilanlar = new List<string>();
            g.cuzdan.UcakKilidiAcildi += acilanlar.Add;
            Collider ucak = Ucak();
            g.hedefler[0].Dene(ucak);
            g.hedefler[1].Dene(ucak);
            Assert.AreEqual(0, acilanlar.Count, "200 < 300");
            g.hedefler[2].Dene(ucak);
            Assert.AreEqual(new[] { "Hawker Hurricane" }, acilanlar.ToArray());
            g.hedefler[3].Dene(ucak);
            g.cuzdan.InisOdulu(true);
            Assert.AreEqual(1, acilanlar.Count, "no repeated unlock above the threshold");
            Assert.AreEqual(400 + g.cuzdan.InisPuani + g.cuzdan.TemizInisPuani, g.cuzdan.Puan);
            Assert.IsTrue(g.cuzdan.KilitAcildiMi);
        }

        // ---- mission manager ----
        [Test]
        public void Manager_OptionalTargetsDoNotChangeTheRequiredCount()
        {
            Gorev g = GorevKur(Hedef(), Hedef(), Hedef(zorunlu: false));
            Assert.AreEqual(2, g.yonetici.ZorunluSayisi);
            Assert.AreEqual(2, g.yonetici.Kalan);
            g.hedefler[2].Dene(Ucak());
            Assert.AreEqual(2, g.yonetici.Kalan, "optional target does not reduce the count");
            Assert.IsFalse(g.yonetici.DonusAsamasi);
            Assert.AreEqual(100, g.cuzdan.Puan, "but it still scores");
        }

        [Test]
        public void Manager_RequiredTargetsReduceTheRemainingCountExactlyOnce()
        {
            Gorev g = GorevKur(Hedef(), Hedef(), Hedef());
            var kalanlar = new List<int>();
            g.yonetici.KalanDegisti += kalanlar.Add;
            Collider ucak = Ucak();
            g.hedefler[0].Dene(ucak);
            g.hedefler[0].Dene(ucak);
            Assert.AreEqual(2, g.yonetici.Kalan);
            g.hedefler[1].Dene(ucak);
            Assert.AreEqual(1, g.yonetici.Kalan);
            Assert.AreEqual(new[] { 2, 1 }, kalanlar.ToArray());
        }

        [Test]
        public void Manager_ReturnPhaseStaysClosedUntilAllRequiredTargetsAreCollected_ThenOpensOnce()
        {
            Gorev g = GorevKur(Hedef(), Hedef(), Hedef(zorunlu: false));
            int acilma = 0;
            g.yonetici.DonusAsamasiBasladi += () => acilma++;
            Collider ucak = Ucak();
            Assert.IsFalse(g.us.gameObject.activeSelf, "base trigger starts disabled");
            g.hedefler[0].Dene(ucak);
            Assert.IsFalse(g.yonetici.DonusAsamasi);
            Assert.IsFalse(g.us.gameObject.activeSelf);
            g.hedefler[1].Dene(ucak);
            Assert.IsTrue(g.yonetici.DonusAsamasi);
            Assert.IsTrue(g.us.gameObject.activeSelf);
            g.hedefler[2].Dene(ucak); // optional, after the phase opened
            Assert.AreEqual(1, acilma);
        }

        [Test]
        public void Base_CannotCompleteTheMissionBeforeTheReturnPhase()
        {
            Gorev g = GorevKur(Hedef(), Hedef());
            int bitis = 0;
            g.yonetici.GorevTamamlandi += () => bitis++;
            g.us.Dene(Ucak());
            g.yonetici.UsseVarildi();
            Assert.IsFalse(g.yonetici.Tamamlandi);
            Assert.AreEqual(0, bitis);
            g.hedefler[0].Dene(Ucak());
            g.us.Dene(Ucak());
            Assert.AreEqual(0, bitis, "one target still missing");
        }

        [Test]
        public void Base_CompletesTheMissionOnce_AndOnlyForThePlayer()
        {
            Gorev g = GorevKur(Hedef(), Hedef());
            int bitis = 0;
            g.yonetici.GorevTamamlandi += () => bitis++;
            Collider ucak = Ucak();
            foreach (KesifHedefi h in g.hedefler)
                h.Dene(ucak);
            Assert.IsFalse(g.us.Dene(Baskasi()));
            Assert.AreEqual(0, bitis);
            Assert.IsTrue(g.us.Dene(ucak));
            g.us.Dene(ucak);
            g.yonetici.UsseVarildi();
            Assert.IsTrue(g.yonetici.Tamamlandi);
            Assert.AreEqual(1, bitis);
        }

        [Test]
        public void Manager_WithoutReferences_ReportsErrorsInsteadOfThrowing()
        {
            var g = new GameObject("BosYonetici");
            olusan.Add(g);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Hedef listesi atanmamış"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Üs tetikleyicisi"));
            var y = g.AddComponent<GorevYoneticisi>();
            Assert.DoesNotThrow(() => y.UsseVarildi());
            Assert.IsFalse(y.Tamamlandi);
        }

        // ---- completion screen ----
        [Test]
        public void Screen_ReceivesTheFinalScore_AndRestartsRota04()
        {
            Gorev g = GorevKur(Hedef(puan: 120), Hedef(puan: 80));
            Collider ucak = Ucak();
            Assert.IsFalse(g.ekran.Gorunur);
            foreach (KesifHedefi h in g.hedefler)
                h.Dene(ucak);
            Assert.IsFalse(g.ekran.Gorunur, "not before the base is reached");
            g.us.Dene(ucak);
            Assert.IsTrue(g.ekran.Gorunur);
            Assert.AreEqual(200, g.ekran.SonPuan);
            Assert.AreEqual("Rota04", g.ekran.YenidenBaslatSahnesi);
            Assert.AreEqual("Rota04", GorevSonuEkrani.VarsayilanSahne);
        }

        // ---- real Rota04 ----
        Scene yukle;

        IEnumerator Rota04Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Rota04", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Rota04_FlyingThroughTargets_ThenHome_CompletesTheMissionAndRestartResets()
        {
            yield return Rota04Yukle();
            var yonetici = Object.FindFirstObjectByType<GorevYoneticisi>();
            var cuzdan = Object.FindFirstObjectByType<PuanCuzdani>();
            var ekran = Object.FindFirstObjectByType<GorevSonuEkrani>();
            var ucus = Object.FindFirstObjectByType<UcusKontrol>();
            KesifHedefi[] hedefler = Object.FindObjectsByType<KesifHedefi>(FindObjectsSortMode.InstanceID);
            UsGirisi us = Object.FindFirstObjectByType<UsGirisi>(FindObjectsInactive.Include);
            Assert.AreEqual(3, hedefler.Length);
            Assert.AreEqual(3, yonetici.Kalan);
            Assert.IsFalse(us.gameObject.activeSelf);

            Transform t = ucus.transform;
            // Fly the first target for real: the aircraft is moved by UcusKontrol.Adim (the same
            // Transform movement as in play) and the physics trigger has to detect it.
            Vector3 hedef0 = hedefler.OrderBy(h => h.transform.position.z).Last().transform.position;
            t.SetPositionAndRotation(new Vector3(hedef0.x, 20f, hedef0.z - 80f), Quaternion.identity);
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < 60 && yonetici.Kalan == 3; i++)
            {
                ucus.Adim(0.1f);
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(2, yonetici.Kalan, "flying through the first column collects it");
            Assert.AreEqual(100, cuzdan.Puan);

            // The other two: placed inside their columns at different altitudes.
            float[] irtifa = { 20f, 150f };
            int k = 0;
            foreach (KesifHedefi h in hedefler.Where(h => !h.Fotografli))
            {
                t.position = new Vector3(h.transform.position.x, h.transform.position.y + irtifa[k++], h.transform.position.z);
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(0, yonetici.Kalan);
            Assert.AreEqual(300, cuzdan.Puan);
            Assert.IsTrue(cuzdan.KilitAcildiMi, "300 points reach the Hawker Hurricane threshold");
            Assert.IsTrue(yonetici.DonusAsamasi);
            Assert.IsTrue(us.gameObject.activeSelf);
            Assert.IsFalse(yonetici.Tamamlandi);

            t.position = new Vector3(0f, 20f, 0f); // back over the base
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(yonetici.Tamamlandi);
            Assert.IsTrue(ekran.Gorunur);
            Assert.AreEqual(300, ekran.SonPuan);

            ekran.YenidenBaslat();
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.AreEqual("Rota04", SceneManager.GetActiveScene().name);
            yonetici = Object.FindFirstObjectByType<GorevYoneticisi>();
            Assert.AreEqual(3, yonetici.Kalan);
            Assert.IsFalse(yonetici.DonusAsamasi);
            Assert.IsFalse(yonetici.Tamamlandi);
            Assert.AreEqual(0, Object.FindFirstObjectByType<PuanCuzdani>().Puan);
            Assert.IsFalse(Object.FindFirstObjectByType<GorevSonuEkrani>().Gorunur);
            Assert.AreEqual(3, Object.FindObjectsByType<KesifHedefi>(FindObjectsSortMode.None).Count(h => !h.Fotografli));
        }

        [UnityTest]
        public IEnumerator Rota04_AircraftOutsideTheColumnsDoesNotCollect_AndLab3SystemsStillRun()
        {
            yield return Rota04Yukle();
            var yonetici = Object.FindFirstObjectByType<GorevYoneticisi>();
            var ucus = Object.FindFirstObjectByType<UcusKontrol>();
            Vector3 p0 = ucus.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(3, yonetici.Kalan, "start position is away from all targets");
            Assert.That((ucus.transform.position - p0).z, Is.GreaterThan(ucus.Hiz * 0.5f), "existing flight control still moves the aircraft");
            Assert.IsNotNull(ucus.GetComponent<Saglik>());
            Assert.IsNotNull(Object.FindFirstObjectByType<SonsuzArazi>());
            Assert.IsFalse(yonetici.Tamamlandi);
        }
    }
}
