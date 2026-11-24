using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using H = NU36.KartalGecidiHaritasi;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 9 (Hafta 9) Bölüm 5 · Kartal Geçidi in Play Mode (Documentation/Lab9_Plan.md §7): four ships in the bay at the
    /// start (TEST-903), a beached ship lands three tanks and the HUD counts them (TEST-904, TEST-908), a ship hit while
    /// unloading stops with its other tanks aboard (TEST-905), a stopped ship is replaced and the new one lands tanks
    /// (TEST-906), the guard fighters patrol and attack the player (TEST-907), 12 tanks ashore fail the level (TEST-909),
    /// 12 ships with fewer than 12 tanks ashore complete it (TEST-910). Measurements → Logs/lab9/oyun_olcumu.txt.
    /// </summary>
    public class Lab9PlayTests
    {
        const string Sahne = "Bolum5_KartalGecidi";
        UcusKontrol ucus;
        CikarmaFilosu filo;
        KorumaUcuslari koruma;
        KartalGecidiGorevi gorev;
        Bolum5Hud hud;
        Bolum4Nisan nisan;
        string yuklenen;
        static readonly StringBuilder Olcum = new StringBuilder();

        /// <summary>Loads the level; the aircraft is parked (frozen, assists off) unless <paramref name="ucakSerbest"/>.</summary>
        IEnumerator Yukle(bool ucakSerbest = false)
        {
            yield return SceneManager.LoadSceneAsync(Sahne, LoadSceneMode.Single);
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            filo = Object.FindAnyObjectByType<CikarmaFilosu>();
            koruma = Object.FindAnyObjectByType<KorumaUcuslari>();
            gorev = Object.FindAnyObjectByType<KartalGecidiGorevi>();
            hud = Object.FindAnyObjectByType<Bolum5Hud>();
            nisan = ucus.GetComponent<Bolum4Nisan>();
            yuklenen = null;
            gorev.SahneYukle = ad => yuklenen = ad;
            if (ucus.TryGetComponent(out InisAlgilayici a)) a.enabled = false;
            if (!ucakSerbest)
            {
                nisan.Yardim = false;
                ucus.enabled = false;
            }
            yield return null;
        }

        [TearDown]
        public void Bitir() => Time.timeScale = 1f;

        [OneTimeTearDown]
        public void Yaz()
        {
            if (Olcum.Length == 0) return;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab9"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "oyun_olcumu.txt"), $"Lab 9 play mode, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        static IEnumerator Bekle(System.Func<bool> kosul, float oyunSuresi)
        {
            float bitis = Time.time + oyunSuresi;
            while (!kosul() && Time.time < bitis)
                yield return null;
        }

        static void Vur(CikarmaGemisi g) => g.Saglik.Vur(10000, HasarTuru.Mermi, Taraf.Dost);

        // ---- TEST-903 ---------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Start_FourShipsInTheBay_Spaced_GuardsUp_HudCountsZero()
        {
            yield return Yukle();
            Assert.AreEqual(4, filo.Gelen, "four ships at once");
            var gemiler = filo.Gemiler.ToArray();
            Assert.AreEqual(4, gemiler.Select(g => g.Serit).Distinct().Count(), "one per lane");
            foreach (var g in gemiler)
            {
                Assert.AreEqual(CikarmaGemisi.Durum.Yolda, g.Simdiki);
                Assert.IsTrue(H.Denizde(g.transform.position.x, g.transform.position.z), "in the bay");
            }
            float enAz = float.MaxValue;
            for (int i = 0; i < gemiler.Length; i++)
                for (int j = i + 1; j < gemiler.Length; j++)
                    enAz = Mathf.Min(enAz, Vector3.Distance(gemiler[i].transform.position, gemiler[j].transform.position));
            Assert.Greater(enAz, 250f, "spaced apart");
            Assert.AreEqual(H.KorumaAyniAnda, koruma.Aktif, "guard fighters in the air");
            Assert.AreEqual(4, Susturulabilir.Serbestler.Count(s => s.Tur == DusmanTuru.Gemi), "ships are targets");
            yield return null;
            StringAssert.Contains("KARADA TANK  0 / 12", hud.SonDurum);
            Assert.AreEqual(KartalGecidiGorevi.Asama.Yaklasma, gorev.Simdiki);
            Olcum.AppendLine($"TEST-903 start: 4 ships, nearest pair {enAz:F0} m apart; distances to the beach {string.Join(", ", gemiler.Select(g => g.KalanMesafe.ToString("F0")))} m; {koruma.Aktif} guard fighters");
        }

        // ---- TEST-904, TEST-908 -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Ship_BeachesAndLandsThreeTanks_HudShowsTheCount()
        {
            yield return Yukle();
            CikarmaGemisi g = filo.SeritGemisi(0);
            var kiyi = Object.FindAnyObjectByType<KiyiKamerasi>();
            Assert.IsFalse(kiyi.Gorunuyor, "the beach camera is closed while no ship is on the beach");
            g.Isinla(40f);
            Time.timeScale = 4f;
            float t0 = Time.time;
            yield return Bekle(() => g.Simdiki == CikarmaGemisi.Durum.Indiriyor, 60f);
            yield return null;
            Assert.AreEqual(CikarmaGemisi.Durum.Indiriyor, g.Simdiki);
            Assert.That(g.RampaHedefi, Is.InRange(8f, 30f), "the ramp slopes down to the sand");
            Assert.Less(Mathf.Abs(g.RampaAcisi - g.RampaHedefi), 0.5f, "fully down");
            Assert.Less(Mathf.Abs(g.RampaUcu.y - ZeminOlcer.Yukseklik(g.RampaUcu.x, g.RampaUcu.z)), 0.4f, "its tip on the ground");
            Assert.IsTrue(kiyi.Gorunuyor, "the beach camera shows the landing");
            Assert.AreSame(g, kiyi.Izlenen);
            float rampa = g.RampaAcisi;
            CikarmaTanki ilk = g.Tanklar[0];
            float enCokEgim = 0f;
            bool toz = false;
            float bitis = Time.time + 40f;
            while (!ilk.KaradaMi && Time.time < bitis) { enCokEgim = Mathf.Max(enCokEgim, ilk.Egim); yield return null; }
            bitis = Time.time + 6f;
            while (!toz && Time.time < bitis) { toz = ilk.TozCikiyor; yield return null; }
            Assert.Greater(enCokEgim, rampa * 0.6f, "the tank pitches down the ramp");
            Assert.IsTrue(toz, "dust behind the tank on the sand");
            yield return Bekle(() => g.Simdiki == CikarmaGemisi.Durum.Ayriliyor, 80f);
            float sure = Time.time - t0;
            Time.timeScale = 1f;
            Assert.AreEqual(CikarmaGemisi.Durum.Ayriliyor, g.Simdiki, "unloaded and leaving");
            Assert.AreEqual(3, g.KarayaCikan, "three tanks landed");
            Assert.AreEqual(0, g.GemideKalan);
            Assert.AreEqual(3, filo.KaradakiTank);
            foreach (CikarmaTanki t in g.Tanklar)
            {
                Assert.IsTrue(t.KaradaMi);
                Assert.IsNull(t.transform.parent, "off the ship");
                Assert.Less(H.DenizUzakligi(t.transform.position.x, t.transform.position.z), 0f, "on land");
            }
            yield return Bekle(() => g.Tanklar.All(t => t.Simdiki == CikarmaTanki.Durum.Park), 60f);
            foreach (CikarmaTanki t in g.Tanklar)
            {
                Vector3 p = t.transform.position;
                Assert.AreEqual(CikarmaTanki.Durum.Park, t.Simdiki);
                Assert.Less(Mathf.Abs(p.y - ZeminOlcer.Yukseklik(p.x, p.z)), 0.6f, "standing on the ground");
            }
            yield return null;
            StringAssert.Contains("KARADA TANK  3 / 12", hud.SonDurum, "worksheet: the count is on screen");
            Assert.IsFalse(Susturulabilir.Serbestler.Contains(g.GetComponent<Susturulabilir>()), "an empty ship is no longer a target");
            Olcum.AppendLine($"TEST-904 ramp {rampa:F1}° down to the sand; the first tank pitched {enCokEgim:F1}° on it and raised dust ashore; the beach camera showed lane {g.Serit + 1}");
            Olcum.AppendLine($"TEST-904 a ship 40 m out: beached, ramp down, three tanks off and the ship leaving after {sure:F1} s game time; " +
                             $"tanks parked at {string.Join(", ", g.Tanklar.Select(t => $"({t.transform.position.x:F0}, {t.transform.position.z:F0})"))}");
            Olcum.AppendLine($"TEST-908 HUD: \"{hud.SonDurum.Split('\n')[1]}\"");
        }

        // ---- TEST-905 -------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Ship_HitWhileUnloading_Stops_TheOtherTanksStayAboard()
        {
            yield return Yukle();
            CikarmaGemisi g = filo.SeritGemisi(1);
            g.Isinla(20f);
            Time.timeScale = 4f;
            yield return Bekle(() => g.Indirilen >= 1, 40f);
            Assert.AreEqual(1, g.Indirilen, "the first tank is off");
            Vur(g);
            Assert.AreEqual(CikarmaGemisi.Durum.Durdu, g.Simdiki);
            yield return Bekle(() => false, 20f);
            Time.timeScale = 1f;
            Assert.AreEqual(1, g.Indirilen, "no more tanks left the deck");
            Assert.AreEqual(2, g.GemideKalan);
            Assert.AreEqual(1, g.KarayaCikan, "only the tank that had already left landed");
            Assert.AreEqual(1, filo.KaradakiTank);
            Assert.AreEqual(CikarmaTanki.Durum.Gemide, g.Tanklar[1].Simdiki);
            Assert.AreEqual(CikarmaTanki.Durum.Gemide, g.Tanklar[2].Simdiki);
            Assert.AreSame(g.transform, g.Tanklar[2].transform.parent, "still aboard");
            Assert.Less(g.HizVektoru.magnitude, 0.01f, "stopped");
            Assert.IsTrue(g.GetComponent<Susturulabilir>().Sustu);
            Assert.AreEqual(1, filo.Durdurulan);
            Olcum.AppendLine($"TEST-905 hit while unloading: {g.Indirilen} tank off, {g.GemideKalan} aboard, sunk to y {g.transform.position.y:F2} m, listing {Mathf.DeltaAngle(0f, g.transform.eulerAngles.z):F1}°");
        }

        // ---- TEST-906 -------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator StoppedShip_IsReplacedInItsLane_AndTheNewOneLandsTanks()
        {
            yield return Yukle();
            CikarmaGemisi eski = filo.SeritGemisi(2);
            Vector3 once = eski.transform.position;
            Vur(eski);
            Time.timeScale = 4f;
            yield return Bekle(() => filo.SeritGemisi(2) != eski, 30f);
            CikarmaGemisi yeni = filo.SeritGemisi(2);
            Assert.AreNotSame(eski, yeni, "a new ship in the stopped one's lane");
            Assert.AreEqual(5, filo.Gelen);
            Assert.AreEqual(CikarmaGemisi.Durum.Yolda, yeni.Simdiki);
            Assert.AreEqual(H.YeniGemiUzakligi, yeni.KalanMesafe, 30f, "comes from out at sea");
            Assert.Less(Vector3.Distance(once, eski.transform.position), 40f, "the stopped one lost way where it was hit");
            yeni.Isinla(40f);
            yield return Bekle(() => yeni.KarayaCikan == 3, 80f);
            Time.timeScale = 1f;
            Assert.AreEqual(3, yeni.KarayaCikan, "the replacement lands its tanks too");
            Assert.AreEqual(0, eski.KarayaCikan);
            Olcum.AppendLine($"TEST-906 lane 3: ship stopped {once.x - H.YanasmaNoktasi(2).x:F0} m out; replacement after {H.YenilemeGecikmesi:F0} s from {H.YeniGemiUzakligi:F0} m; it landed 3 tanks");
        }

        // ---- TEST-907 -------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator GuardFighters_PatrolOverTheBay_ThenAttackThePlayer()
        {
            yield return Yukle(ucakSerbest: true);
            nisan.Yardim = false;
            Saglik can = ucus.GetComponent<Saglik>();
            // The player far away in the start valley: the fighters patrol over the landing.
            Vector3 uzak = new Vector3(-2600f, 700f, 0f);
            Time.timeScale = 3f;
            float bitis = Time.time + 25f;
            while (Time.time < bitis)
            {
                ucus.transform.SetPositionAndRotation(uzak, Quaternion.LookRotation(Vector3.right));
                yield return null;
            }
            var aktif = koruma.Ucaklar.Where(u => u.gameObject.activeInHierarchy).ToArray();
            Assert.AreEqual(2, aktif.Length);
            foreach (DusmanUcagi u in aktif)
            {
                Assert.AreEqual(DusmanUcagi.Durum.Devriye, u.Simdiki, "patrolling");
                float d = Vector2.Distance(new Vector2(u.transform.position.x, u.transform.position.z), new Vector2(H.KoyMerkezi.x, H.KoyMerkezi.z));
                Assert.Less(d, 2000f, "over the bay");
            }
            Assert.AreEqual(100, can.Can);
            string devriye = string.Join(", ", aktif.Select(u => $"{Vector2.Distance(new Vector2(u.transform.position.x, u.transform.position.z), new Vector2(H.KoyMerkezi.x, H.KoyMerkezi.z)):F0} m"));

            // The player over the bay: they attack it, fire and hit.
            Vector3 koy = new Vector3(1700f, 250f, 200f);
            bitis = Time.time + 40f;
            while (Time.time < bitis && can.Can > 90)
            {
                ucus.transform.SetPositionAndRotation(koy, Quaternion.LookRotation(Vector3.right));
                ucus.HizAyarla(0f);
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.IsTrue(aktif.Any(u => u.SonHedef == ucus.transform), "the player is their target");
            Assert.Greater(aktif.Sum(u => u.AtisSayisi), 0, "they fire");
            Assert.Less(can.Can, 100, "the player is hit");
            Olcum.AppendLine($"TEST-907 player away: 2 fighters patrolling {devriye} from the bay centre; player over the bay: " +
                             $"{aktif.Sum(u => u.AtisSayisi)} shots, health {can.Can}");
        }

        // ---- TEST-909 -------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TwelveTanksAshore_TheCoastFalls_LevelRestarts()
        {
            yield return Yukle();
            foreach (var g in filo.Gemiler) g.Isinla(30f + 10f * g.Serit);
            Time.timeScale = 5f;
            yield return Bekle(() => gorev.Bitti, 120f);
            Assert.AreEqual(KartalGecidiGorevi.Asama.KiyiDustu, gorev.Simdiki, "worksheet: 12 tanks ashore → failure");
            Assert.AreEqual(12, filo.KaradakiTank);
            yield return null;
            Assert.IsTrue(hud.SonPaneliGorunuyor);
            StringAssert.StartsWith(KartalGecidiGorevi.YaziKiyiDustu, hud.SonPaneliYazisi);
            StringAssert.Contains("Karaya çıkan tank:  12 / 12", hud.SonPaneliYazisi);
            yield return Bekle(() => yuklenen != null, 6f);
            Time.timeScale = 1f;
            Assert.AreEqual(Sahne, yuklenen, "the level restarts");
            Olcum.AppendLine($"TEST-909 four ships beached: coast fell at {gorev.Sure:F0} s with {filo.KaradakiTank} tanks ashore; restart requested");
        }

        // ---- TEST-910 -------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator AllTwelveShipsCame_FewerThanTwelveAshore_LevelComplete()
        {
            yield return Yukle();
            filo.YenilemeGecikmesi = 0.5f;
            CikarmaGemisi birakilan = filo.SeritGemisi(0);   // this one lands its three tanks
            birakilan.Isinla(30f);
            Time.timeScale = 5f;
            float bitis = Time.time + 200f;
            while (!gorev.Bitti && Time.time < bitis)
            {
                foreach (CikarmaGemisi g in filo.Gemiler)
                    if (g != birakilan && g.Simdiki == CikarmaGemisi.Durum.Yolda) Vur(g);
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.AreEqual(KartalGecidiGorevi.Asama.Tamam, gorev.Simdiki, "worksheet: all 12 ships came, the coast held");
            Assert.AreEqual(12, filo.Gelen);
            Assert.AreEqual(11, filo.Durdurulan);
            Assert.AreEqual(3, filo.KaradakiTank);
            yield return null;
            Assert.IsTrue(hud.SonPaneliGorunuyor);
            StringAssert.StartsWith(KartalGecidiGorevi.YaziTamam, hud.SonPaneliYazisi);
            StringAssert.Contains("Durdurulan gemi:  11 / 12", hud.SonPaneliYazisi);
            Assert.IsNull(yuklenen, "no restart after a success");
            Olcum.AppendLine($"TEST-910 one ship let through, 11 stopped: \"BÖLÜM TAMAM\" at {gorev.Sure:F0} s, {filo.KaradakiTank} tanks ashore");
        }

        [UnityTest]
        public IEnumerator AircraftShotDown_Vuruldun_Restarts()
        {
            yield return Yukle(ucakSerbest: true);
            ucus.GetComponent<Saglik>().Vur(1000, HasarTuru.Mermi, Taraf.Dusman);
            Assert.AreEqual(KartalGecidiGorevi.Asama.Vuruldun, gorev.Simdiki);
            Time.timeScale = 3f;
            yield return Bekle(() => yuklenen != null, 15f);
            Time.timeScale = 1f;
            Assert.AreEqual(Sahne, yuklenen);
            Assert.IsFalse(hud.SonPaneliGorunuyor, "the end panel is for the coast, not for being shot down");
        }
    }
}
