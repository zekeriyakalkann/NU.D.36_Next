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
    /// Lab 8 (Hafta 8) phase 2 in Play Mode, scene Bolum4_KursunDaglari (Documentation/Lab8_Plan.md §9): zones wake up as
    /// the train comes and fall silent behind it (TEST-812), every enemy type fires with a flash and a tracer (TEST-813),
    /// silenced units stay silent and smoke, a hit aircraft withdraws (TEST-815), enemy fire hurts the aircraft and the
    /// train (TEST-810, TEST-816), the ceilings (TEST-817), the failures (TEST-818), the player's weapons and their rules
    /// (TEST-819 … TEST-822). Measurements → Logs/lab8/dusman_olcumu.txt.
    /// </summary>
    public class Lab8DusmanPlayTests
    {
        const string Sahne = "Bolum4_KursunDaglari";
        UcusKontrol ucus;
        Tren tren;
        Makineli mk;
        BombaYuku yuk;
        KursunDaglariGorevi gorev;
        AtesBolgesi[] bolgeler;
        string yuklenen;
        static readonly StringBuilder Olcum = new StringBuilder();

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync(Sahne, LoadSceneMode.Single);
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            tren = Object.FindAnyObjectByType<Tren>();
            mk = ucus.GetComponent<Makineli>();
            yuk = ucus.GetComponent<BombaYuku>();
            gorev = Object.FindAnyObjectByType<KursunDaglariGorevi>();
            bolgeler = Object.FindObjectsByType<AtesBolgesi>(FindObjectsSortMode.None).OrderBy(b => b.Bas).ToArray();
            yuklenen = null;
            gorev.SahneYukle = ad => yuklenen = ad;
            if (ucus.TryGetComponent(out UcusYardimcisi y)) y.enabled = false;
            if (ucus.TryGetComponent(out InisAlgilayici a)) a.enabled = false;
            yield return null;
        }

        [TearDown]
        public void Bitir() => Time.timeScale = 1f;

        [OneTimeTearDown]
        public void Yaz()
        {
            if (Olcum.Length == 0) return;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab8"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "dusman_olcumu.txt"), $"Lab 8 enemies and weapons, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        /// <summary>Holds the aircraft at a point (still "flying": enabled, so guns fire at it and its own guns work).</summary>
        IEnumerator Tut(Vector3 konum, Vector3 bak, float sure, System.Func<bool> dur = null)
        {
            float bitis = Time.time + sure;
            while (Time.time < bitis && (dur == null || !dur()))
            {
                Yerlestir(konum, bak);
                yield return null;
            }
        }

        void Yerlestir(Vector3 konum, Vector3 bak)
        {
            ucus.transform.SetPositionAndRotation(konum, Quaternion.LookRotation(bak - konum));
            ucus.HizAyarla(0f);
            ucus.Yunuslama = ucus.Yatis = ucus.Sapma = 0f;
            Physics.SyncTransforms();
            Lab7PlayTests.CarpismaSifirla(ucus);
        }

        /// <summary>Parks the aircraft far above the start (out of every gun's reach) for tests about the train.</summary>
        void UcagiUzaklastir()
        {
            ucus.enabled = false;
            ucus.transform.position = new Vector3(-2500f, 2200f, -2500f);
        }

        /// <summary>A point <paramref name="uzaklik"/> m from <paramref name="hedef"/> (horizontally) and
        /// <paramref name="yukseklik"/> m above it, clear of the terrain, with a clear line of sight to it.</summary>
        static Vector3 AcikNokta(Vector3 hedef, float uzaklik, float yukseklik)
        {
            int arazi = LayerMask.GetMask("Arazi");
            Vector3 enIyi = hedef + Vector3.up * yukseklik;
            float enIyiPay = float.MinValue;
            for (int i = 0; i < 24; i++)
            {
                Vector3 p = hedef + Quaternion.Euler(0f, i * 15f, 0f) * Vector3.forward * uzaklik + Vector3.up * yukseklik;
                float pay = ZeminOlcer.Irtifa(p);
                if (Physics.Linecast(p, hedef + Vector3.up * 2f, arazi)) continue;
                if (pay > enIyiPay) { enIyiPay = pay; enIyi = p; }
            }
            Assert.Greater(enIyiPay, 20f, $"a clear spot {uzaklik} m from {hedef}");
            return enIyi;
        }

        /// <summary>Jumps the train to just before zone i wakes and lets it wake.</summary>
        IEnumerator BolgeyeGetir(int i)
        {
            float s = bolgeler[i].Bas - bolgeler[i].Onden + 5f;
            float v = tren.SeyirHizi;
            tren.ZamanaAtla((s - H.TrenBaslangici - 0.5f * v * H.KalkisSuresi) / v + H.KalkisSuresi);
            yield return null;
            yield return null;
            Assert.AreEqual(AtesBolgesi.Durum.Etkin, bolgeler[i].Simdiki, $"{bolgeler[i].Ad} awake");
        }

        // ---- TEST-812 ----------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Zones_HiddenUntilTheTrainComes_CalmBetweenThem()
        {
            yield return Yukle();
            UcagiUzaklastir();
            foreach (AtesBolgesi b in bolgeler)
            {
                Assert.AreEqual(AtesBolgesi.Durum.Bekliyor, b.Simdiki);
                Assert.IsTrue(b.YerBirimleri.All(g => !g.activeSelf), $"{b.Ad}: ground units hidden");
                Assert.IsTrue(b.Ucaklar.All(u => !u.gameObject.activeSelf), $"{b.Ad}: aircraft not yet in");
            }
            // The train must live through all three zones here; the damage each zone does is logged for the balance.
            tren.Saglik.BaslangicAyarla(100000);
            var hasar = new int[3];
            int onceCan = tren.Saglik.Can;
            Time.timeScale = 20f;
            var acilis = new float[3];
            var kapanis = new float[3];
            int Atis() => bolgeler.Sum(b => b.YerBirimleri.Sum(g => g.TryGetComponent(out Tank t) ? t.AtisSayisi : g.GetComponent<UcaksavarMevzii>().AtisSayisi)
                                                + b.Ucaklar.Sum(u => u.AtisSayisi));
            int sakinlikteAtis = 0;
            float sinir = Time.realtimeSinceStartup + 120f;
            while (bolgeler[2].Simdiki != AtesBolgesi.Durum.Bitti && !tren.DurduMu && Time.realtimeSinceStartup < sinir)
            {
                bool sakin = bolgeler.All(b => b.Simdiki != AtesBolgesi.Durum.Etkin) && bolgeler[0].Simdiki == AtesBolgesi.Durum.Bitti;
                int once = Atis();
                yield return new WaitForFixedUpdate();
                if (sakin && bolgeler.All(b => b.Simdiki != AtesBolgesi.Durum.Etkin))
                    sakinlikteAtis += Atis() - once;
                for (int i = 0; i < 3; i++)
                {
                    if (acilis[i] == 0f && bolgeler[i].Simdiki != AtesBolgesi.Durum.Bekliyor) acilis[i] = tren.Gecen;
                    if (kapanis[i] == 0f && bolgeler[i].Simdiki == AtesBolgesi.Durum.Bitti)
                    {
                        kapanis[i] = tren.Gecen;
                        hasar[i] = onceCan - tren.Saglik.Can;
                        onceCan = tren.Saglik.Can;
                    }
                }
            }
            Time.timeScale = 1f;
            Olcum.AppendLine($"TEST-812 zones awake/over at (train time): {string.Join(", ", Enumerable.Range(0, 3).Select(i => $"{bolgeler[i].Ad} {Bolum4Hud.SureYazisi(acilis[i])}–{Bolum4Hud.SureYazisi(kapanis[i])}"))}; rounds fired in the calm stretches: {sakinlikteAtis}; damage to the train with no player, per zone: {string.Join(" / ", hasar)} (health 100)");
            for (int i = 0; i < 3; i++)
            {
                Assert.Greater(acilis[i], 0f, $"{bolgeler[i].Ad} woke up");
                Assert.Greater(kapanis[i], acilis[i], $"{bolgeler[i].Ad} ended");
                if (i > 0) Assert.Greater(acilis[i] - kapanis[i - 1], 15f, $"a calm stretch before {bolgeler[i].Ad}");
            }
            Assert.AreEqual(0, sakinlikteAtis, "no fire in the calm stretches");
        }

        // ---- TEST-813, TEST-810, TEST-816 -----------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Tank_FiresAtTheTrain_WithFlashAndTracer_AndHitsHurtTheTrain()
        {
            yield return Yukle();
            UcagiUzaklastir();
            yield return BolgeyeGetir(0);
            Tank tank = bolgeler[0].YerBirimleri.Select(g => g.GetComponent<Tank>()).First(t => t != null);
            int can = tren.Saglik.Can;
            bool iz = false, alev = false;
            float sinir = Time.time + 25f;
            while (Time.time < sinir && tren.Saglik.Can == can)
            {
                yield return null;
                iz |= Object.FindObjectsByType<Mermi>(FindObjectsSortMode.None).Any(m => m.name.StartsWith("TankMermisi") && m.GetComponent<TrailRenderer>() != null);
                alev |= GameObject.Find("TankNamluAlevi(Clone)") != null;
            }
            Olcum.AppendLine($"TEST-813 tank: {tank.AtisSayisi} shells, train health {can} → {tren.Saglik.Can}");
            Assert.Greater(tank.AtisSayisi, 0, "the tank fires");
            Assert.IsTrue(iz, "a tracer shell flies");
            Assert.IsTrue(alev, "muzzle flash");
            Assert.Less(tren.Saglik.Can, can, "TEST-810: a hit on the train lowers its health");
        }

        [UnityTest]
        public IEnumerator Aa_FiresAtALowAircraft_HurtsIt_HoldsFireAboveTheCeiling()
        {
            yield return Yukle();
            yield return BolgeyeGetir(0);
            UcaksavarMevzii aa = bolgeler[0].YerBirimleri.Select(g => g.GetComponent<UcaksavarMevzii>()).First(a => a != null);
            var saglik = ucus.GetComponent<Saglik>();

            // High: 400 m above the gun → no fire (worksheet 05).
            Vector3 yuksek = AcikNokta(aa.transform.position, 150f, 400f);
            yield return Tut(yuksek, yuksek + Vector3.forward * 100f, 3f);
            Assert.IsTrue(aa.TavanUstunde);
            Assert.AreEqual(0, aa.AtisSayisi, "TEST-817: AA silent 400 m above it");

            // Low: 150 m above, 250 m away → fires and hurts.
            Vector3 alcak = AcikNokta(aa.transform.position, 250f, 150f);
            int can = saglik.Can;
            bool iz = false;
            yield return Tut(alcak, alcak + Vector3.forward * 100f, 8f, () =>
            {
                iz |= Object.FindObjectsByType<Mermi>(FindObjectsSortMode.None).Any(m => m.name.StartsWith("DusmanMermisi"));
                return saglik.Can < can - 5;
            });
            Olcum.AppendLine($"TEST-816/817 AA: 0 rounds 400 m above; {aa.AtisSayisi} rounds 150 m above, aircraft health {can} → {saglik.Can}");
            Assert.Greater(aa.AtisSayisi, 0, "TEST-813: AA fires at a low aircraft");
            Assert.IsTrue(iz, "tracer rounds");
            Assert.Less(saglik.Can, can, "TEST-816: enemy fire hurts the aircraft");
        }

        [UnityTest]
        public IEnumerator EnemyAircraft_FlyIn_AttackWithTracers_StayAboveTheFloor()
        {
            yield return Yukle();
            UcagiUzaklastir();
            yield return BolgeyeGetir(1);
            DusmanUcagi[] u = bolgeler[1].Ucaklar;
            Assert.IsTrue(u.All(x => x.gameObject.activeSelf && x.Simdiki == DusmanUcagi.Durum.Saldiri), "the wave flies in");
            float enAz = float.MaxValue;
            int can = tren.Saglik.Can;
            float sinir = Time.time + 40f;
            while (Time.time < sinir && u.Sum(x => x.AtisSayisi) < 6)
            {
                yield return null;
                foreach (var x in u) enAz = Mathf.Min(enAz, ZeminOlcer.Irtifa(x.transform.position));
            }
            Olcum.AppendLine($"TEST-813 enemy aircraft: {u.Sum(x => x.AtisSayisi)} rounds in {40f - (sinir - Time.time):F0} s, lowest {enAz:F0} m above ground, train health {can} → {tren.Saglik.Can}");
            Assert.GreaterOrEqual(u.Sum(x => x.AtisSayisi), 6, "the aircraft attack and fire");
            Assert.Greater(enAz, 75f, "they keep 80 m clear of the ground (plan §5.4)");
        }

        // ---- TEST-815, TEST-820, TEST-821 ------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Bomb_BetweenATankPair_SilencesBoth_RoundsDoNot_SilencedStaySilentAndSmoke()
        {
            yield return Yukle();
            UcagiUzaklastir();
            yield return BolgeyeGetir(0);
            Tank[] cift = bolgeler[0].YerBirimleri.Select(g => g.GetComponent<Tank>()).Where(t => t != null).ToArray();
            Vector3 orta = (cift[0].transform.position + cift[1].transform.position) * 0.5f;

            // Machine-gun rounds glance off (TEST-820, first half).
            var ss = cift[0].GetComponent<Saglik>();
            Mermi on = (Mermi)typeof(Makineli).GetField("mermiOn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(mk);
            for (int i = 0; i < 20; i++)
            {
                Vector3 c = cift[0].transform.position + new Vector3(0f, 30f, -40f);
                Mermi m = Object.Instantiate(on, c, Quaternion.identity);
                m.Ates((cift[0].transform.position + Vector3.up * 1.5f - c).normalized, 700f, mk.Hasar, mk.Vurulacak, true, Taraf.Dost);
            }
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(100, ss.Can, "rounds do not hurt a tank");

            // A free-falling bomb dropped over the middle of the pair from 150 m (TEST-820, second half).
            Bomba bombaOn = yuk.BombaOn;
            Bomba bomba = Object.Instantiate(bombaOn, orta + Vector3.up * 150f, Quaternion.identity);
            bomba.Birak(Vector3.down * 5f, 150f, ucus.GetComponent<Saglik>());
            float sinir = Time.time + 10f;
            while (Time.time < sinir && bomba != null && !bomba.Patladi)
                yield return null;
            yield return null;
            Assert.IsTrue(cift.All(t => t.GetComponent<Susturulabilir>().Sustu), "one good bomb silences both tanks");
            Assert.IsTrue(cift.All(t => !t.enabled), "their guns are off");
            Assert.IsTrue(cift.All(t => t.GetComponent<Susturulabilir>().Duman.gameObject.activeSelf), "they smoke");
            int atis = cift.Sum(t => t.AtisSayisi);
            yield return new WaitForSeconds(8f);
            Assert.AreEqual(atis, cift.Sum(t => t.AtisSayisi), "a silenced tank never fires again");
            Assert.AreEqual(2, gorev.SusturulanTank, "the mission counts both");
            Olcum.AppendLine($"TEST-820 bomb at the pair's middle: both tanks silenced; 20 rounds did no damage");
        }

        [UnityTest]
        public IEnumerator MachineGun_SilencesAnAaGun_AndAnEnemyAircraft_WhichWithdrawsWithoutFalling()
        {
            yield return Yukle();
            yield return BolgeyeGetir(1);
            UcaksavarMevzii aa = bolgeler[1].YerBirimleri.Select(g => g.GetComponent<UcaksavarMevzii>()).First(a => a != null);
            var aaS = aa.GetComponent<Susturulabilir>();
            // Two AA guns and two aircraft fire at the held aircraft here: it must last the whole test.
            ucus.GetComponent<Saglik>().BaslangicAyarla(100000);
            int ucakCani = ucus.GetComponent<Saglik>().Can;

            // From 300 m out and 120 m up, nose at the gun, trigger held.
            Vector3 hedef = aa.transform.position + Vector3.up * 1.5f;
            Vector3 konum = AcikNokta(hedef, 280f, 110f);
            mk.Tetik = true;
            yield return Tut(konum, hedef, 10f, () => aaS.Sustu);
            mk.Tetik = false;
            int aaAtis = aa.AtisSayisi;
            Olcum.AppendLine($"TEST-821 AA silenced after {mk.Kapasite - mk.Kalan} rounds");
            Assert.IsTrue(aaS.Sustu, "the machine gun silences an AA gun");
            Assert.IsTrue(aaS.Duman.gameObject.activeSelf, "it smokes");
            yield return Tut(konum, hedef, 3f);
            Assert.AreEqual(aaAtis, aa.AtisSayisi, "and stays silent");

            // Enemy aircraft: hold it still in front of the guns (as if flying straight at us).
            DusmanUcagi d = bolgeler[1].Ucaklar[0];
            var dS = d.GetComponent<Susturulabilir>();
            Vector3 benim = konum + Vector3.up * 200f;
            int once = mk.Kalan;
            mk.Tetik = true;
            float sinir = Time.time + 8f;
            while (Time.time < sinir && !dS.Sustu)
            {
                Vector3 onde = benim + Vector3.forward * 250f;
                d.transform.SetPositionAndRotation(onde, Quaternion.LookRotation(Vector3.back));
                d.GetComponent<Rigidbody>().position = onde;
                Yerlestir(benim, onde);
                yield return null;
            }
            mk.Tetik = false;
            Assert.IsTrue(dS.Sustu, "the machine gun silences an enemy aircraft");
            Assert.AreEqual(DusmanUcagi.Durum.Cekilme, d.Simdiki, "it withdraws");
            int dAtis = d.AtisSayisi;
            float y0 = d.transform.position.y, enAz = y0;
            float bitis = Time.time + 6f;
            while (Time.time < bitis && d.gameObject.activeSelf)
            {
                Yerlestir(benim, benim + Vector3.forward * 100f);
                yield return null;
                enAz = Mathf.Min(enAz, d.transform.position.y);
            }
            Olcum.AppendLine($"TEST-815/821 enemy aircraft silenced with {once - mk.Kalan} rounds; withdrawing: lowest {enAz - y0:F1} m vs. the hit point, smoke trail {(d.GetComponentInChildren<ParticleSystem>(true) != null)}");
            Assert.AreEqual(dAtis, d.AtisSayisi, "a withdrawing aircraft does not fire");
            Assert.Greater(enAz, y0 - 5f, "it does not fall");
            Assert.Greater(d.transform.position.y, y0, "it climbs away");
            Assert.Greater(ucakCani, 0);
        }

        // ---- TEST-819, TEST-822 ------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Rounds_1200_CountDown_FlashAndTracer_EachShot_NoFireWhenEmpty_HudShowsThem()
        {
            yield return Yukle();
            Assert.AreEqual(1200, mk.Kalan);
            Assert.AreEqual(2, yuk.Kalan);
            var hud = Object.FindAnyObjectByType<Bolum4Hud>();
            StringAssert.Contains("MERMİ 1200  ·  BOMBA 2", hud.DurumYazisi());
            int atis = 0, izli = 0;
            mk.AtesEdildi += m => { atis++; if (m.GetComponent<TrailRenderer>() != null) izli++; };
            Vector3 p = ucus.transform.position;
            bool alev = false;
            mk.Tetik = true;
            yield return Tut(p, p + ucus.transform.forward * 100f, 1f, () =>
            {
                alev |= ucus.GetComponentsInChildren<ParticleSystem>().Any(x => x.name.StartsWith("NamluAlevi"));
                return false;
            });
            mk.Tetik = false;
            Olcum.AppendLine($"TEST-819 one second of fire: {atis} rounds ({1200 - mk.Kalan} counted)");
            Assert.That(atis, Is.InRange(14, 19), "16 rounds a second");
            Assert.AreEqual(1200 - atis, mk.Kalan, "every shot counts down");
            Assert.AreEqual(atis, izli, "TEST-822: every round draws a tracer");
            Assert.IsTrue(alev, "TEST-822: muzzle flash");
            StringAssert.Contains($"MERMİ {mk.Kalan}", hud.DurumYazisi());

            mk.KalanAyarla(0);
            atis = 0;
            mk.Tetik = true;
            yield return Tut(p, p + ucus.transform.forward * 100f, 0.5f);
            mk.Tetik = false;
            Assert.AreEqual(0, atis, "empty guns do not fire");
            StringAssert.Contains("MERMİ YOK", hud.DurumYazisi());
        }

        [UnityTest]
        public IEnumerator Bomb_RefusedAbove300m_ReleasedBelow_CountsDown()
        {
            yield return Yukle();
            Vector3 zemin = tren.Araclar[0].transform.position;
            Vector3 yuksek = zemin + Vector3.up * 480f;
            Yerlestir(yuksek, yuksek + Vector3.forward * 100f);
            yield return null;
            Yerlestir(yuksek, yuksek + Vector3.forward * 100f);
            Assert.IsTrue(yuk.CokYuksek);
            Assert.IsNull(yuk.Birak(), "TEST-817: no release 480 m up");
            Assert.AreEqual(2, yuk.Kalan);
            Vector3 alcak = zemin + Vector3.up * 200f;
            Yerlestir(alcak, alcak + Vector3.forward * 100f);
            yield return null;
            Yerlestir(alcak, alcak + Vector3.forward * 100f);
            Assert.IsFalse(yuk.CokYuksek);
            Assert.IsNotNull(yuk.Birak(), "released 200 m up");
            Assert.AreEqual(1, yuk.Kalan, "the count goes down");
            StringAssert.Contains("BOMBA 1", Object.FindAnyObjectByType<Bolum4Hud>().DurumYazisi());
        }

        // ---- TEST-818 ----------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TrainDestroyed_TrenDurdu_ThenRestart()
        {
            yield return Yukle();
            UcagiUzaklastir();
            yield return new WaitForSeconds(1f);
            tren.Saglik.Vur(tren.Saglik.Can, HasarTuru.Mermi, Taraf.Dusman);
            yield return null;
            Assert.AreEqual(KursunDaglariGorevi.Asama.TrenDurdu, gorev.Simdiki);
            StringAssert.Contains(KursunDaglariGorevi.YaziTrenDurdu, Object.FindAnyObjectByType<Bolum4Hud>().SonBildirim);
            Assert.IsNull(yuklenen);
            yield return new WaitForSeconds(4.3f);
            Assert.AreEqual(Sahne, yuklenen, "the level restarts after 4 s");
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(tren.DurduMu, "the train braked to a stop (6 s)");
        }

        [UnityTest]
        public IEnumerator AircraftDestroyed_Vuruldun_ThenRestart()
        {
            yield return Yukle();
            ucus.GetComponent<Saglik>().Vur(100, HasarTuru.Mermi, Taraf.Dusman);
            yield return null;
            Assert.AreEqual(KursunDaglariGorevi.Asama.Vuruldun, gorev.Simdiki);
            StringAssert.Contains(KursunDaglariGorevi.YaziVuruldun, Object.FindAnyObjectByType<Bolum4Hud>().SonBildirim);
            Assert.IsFalse(ucus.enabled, "the aircraft stops");
            yield return new WaitForSeconds(4.3f);
            Assert.AreEqual(Sahne, yuklenen, "the level restarts");
        }
    }
}
