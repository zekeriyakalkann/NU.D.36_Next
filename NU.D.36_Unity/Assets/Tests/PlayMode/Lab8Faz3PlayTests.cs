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
    /// Lab 8 (Hafta 8) phase 3 in Play Mode (Documentation/Lab8_Plan.md §9): engine smoke (TEST-823), the five HUD items in
    /// every phase (TEST-824), "Üsse dön" (TEST-825), "Görev tamam" with its values (TEST-826), the assists (TEST-829),
    /// the HUD warnings and the GPS. Measurements → Logs/lab8/faz3_olcumu.txt.
    /// </summary>
    public class Lab8Faz3PlayTests
    {
        const string Sahne = "Bolum4_KursunDaglari";
        UcusKontrol ucus;
        Tren tren;
        Makineli mk;
        BombaYuku yuk;
        Bolum4Nisan nisan;
        Bolum4Hud hud;
        KursunDaglariGorevi gorev;
        AtesBolgesi[] bolgeler;
        static readonly StringBuilder Olcum = new StringBuilder();

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync(Sahne, LoadSceneMode.Single);
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            tren = Object.FindAnyObjectByType<Tren>();
            mk = ucus.GetComponent<Makineli>();
            yuk = ucus.GetComponent<BombaYuku>();
            nisan = ucus.GetComponent<Bolum4Nisan>();
            hud = Object.FindAnyObjectByType<Bolum4Hud>();
            gorev = Object.FindAnyObjectByType<KursunDaglariGorevi>();
            bolgeler = Object.FindObjectsByType<AtesBolgesi>(FindObjectsSortMode.None).OrderBy(b => b.Bas).ToArray();
            gorev.SahneYukle = _ => { };
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
            File.WriteAllText(Path.Combine(dir, "faz3_olcumu.txt"), $"Lab 8 phase 3, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        void Yerlestir(Vector3 konum, Vector3 bak)
        {
            ucus.transform.SetPositionAndRotation(konum, Quaternion.LookRotation(bak - konum));
            ucus.HizAyarla(0f);
            ucus.Yunuslama = ucus.Yatis = ucus.Sapma = 0f;
            Physics.SyncTransforms();
            Lab7PlayTests.CarpismaSifirla(ucus);
        }

        IEnumerator Tut(System.Func<(Vector3 konum, Vector3 bak)> yer, float sure, System.Func<bool> dur = null)
        {
            float bitis = Time.time + sure;
            while (Time.time < bitis && (dur == null || !dur()))
            {
                var (k, b) = yer();
                Yerlestir(k, b);
                yield return null;
            }
        }

        IEnumerator BolgeyeGetir(int i)
        {
            float s = bolgeler[i].Bas - bolgeler[i].Onden + 5f;
            float v = tren.SeyirHizi;
            tren.ZamanaAtla((s - H.TrenBaslangici - 0.5f * v * H.KalkisSuresi) / v + H.KalkisSuresi);
            yield return null;
            yield return null;
        }

        static Vector3 AcikNokta(Vector3 hedef, float uzaklik, float yukseklik)
        {
            int arazi = LayerMask.GetMask("Arazi");
            Vector3 enIyi = hedef + Vector3.up * yukseklik;
            float enIyiPay = float.MinValue;
            for (int i = 0; i < 24; i++)
            {
                Vector3 p = hedef + Quaternion.Euler(0f, i * 15f, 0f) * Vector3.forward * uzaklik + Vector3.up * yukseklik;
                if (Physics.Linecast(p, hedef + Vector3.up * 2f, arazi)) continue;
                float pay = ZeminOlcer.Irtifa(p);
                if (pay > enIyiPay) { enIyiPay = pay; enIyi = p; }
            }
            return enIyi;
        }

        static void SatirlarDolu(string yazi, string asama)
        {
            string[] s = yazi.Split('\n');
            Assert.AreEqual(5, s.Length, $"{asama}: five lines");
            string[] etiket = { "UÇAK", "TREN", "MERMİ", "GÜVENLİ BÖLGE", "GÖREV" };
            for (int i = 0; i < 5; i++)
            {
                StringAssert.StartsWith(etiket[i], s[i], asama);
                Assert.Greater(s[i].Substring(etiket[i].Length).Trim().Length, 0, $"{asama}: {etiket[i]} has a value");
            }
        }

        // ---- TEST-823 ----------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EngineSmoke_ThinAtTheStart_DenserAndDarkerWhenHit()
        {
            yield return Yukle();
            var md = ucus.GetComponent<MotorDumani>();
            Assert.IsNotNull(md);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(md.Duman.isPlaying, "smoke from the start");
            Assert.Greater(md.Duman.particleCount, 0);
            float y0 = md.Yayin, o0 = md.Opaklik, b0 = md.Boyut;
            Assert.Less(o0, 0.35f, "thin and half-transparent at full health");
            ucus.GetComponent<Saglik>().Vur(70, HasarTuru.Mermi, Taraf.Dusman);   // 30 % left
            yield return new WaitForSeconds(1f);
            Olcum.AppendLine($"TEST-823 engine smoke: full health rate {y0:F0}/s, opacity {o0:F2}, size {b0:F1} m → 30 % health rate {md.Yayin:F0}/s, opacity {md.Opaklik:F2}, size {md.Boyut:F1} m");
            Assert.Greater(md.Yayin, y0 * 2f, "denser");
            Assert.Greater(md.Opaklik, o0 + 0.25f, "darker");
            Assert.Greater(md.Boyut, b0, "thicker");
            Assert.AreEqual(md.Yayin, md.Duman.emission.rateOverTime.constant, 0.01f, "applied to the particle system");
        }

        // ---- TEST-824, TEST-825, TEST-826 ------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Hud_FiveItemsInEveryPhase_UsseDon_ThenGorevTamamWithItsValues()
        {
            yield return Yukle();
            yield return null;
            SatirlarDolu(hud.DurumYazisi(), "escort");
            StringAssert.Contains("Treni", hud.Gorev);

            // Train inside the safe zone → "Üsse dön".
            tren.ZamanaAtla(H.VarisSuresi + 0.1f);
            yield return null;
            yield return null;
            Assert.AreEqual(KursunDaglariGorevi.Asama.UsseDon, gorev.Simdiki, "TEST-825");
            StringAssert.StartsWith("Üsse dön", hud.Gorev);
            SatirlarDolu(hud.DurumYazisi(), "return");
            Assert.IsFalse(hud.TamamGorunuyor);

            // Some silenced, some ammunition used, then low over the base → "Görev tamam".
            bolgeler[1].Etkinlestir();
            bolgeler[1].YerBirimleri[0].GetComponent<Saglik>().Vur(1000, HasarTuru.Patlama, Taraf.Dost);
            mk.KalanAyarla(1000);
            tren.Saglik.Vur(23, HasarTuru.Mermi, Taraf.Dusman);
            Vector3 us = gorev.UsMerkezi;
            Vector3 ust = new Vector3(us.x + 100f, ZeminOlcer.Yukseklik(us.x + 100f, us.z) + 120f, us.z);
            Yerlestir(ust, ust + Vector3.right * 100f);
            yield return null;
            Yerlestir(ust, ust + Vector3.right * 100f);
            yield return null;
            Assert.AreEqual(KursunDaglariGorevi.Asama.Tamam, gorev.Simdiki, "TEST-826");
            yield return null;
            Assert.IsTrue(hud.TamamGorunuyor, "the Görev tamam panel shows");
            string t = hud.TamamYazisiSon;
            Olcum.AppendLine("TEST-826 panel:\n" + t);
            StringAssert.Contains("GÖREV TAMAM", t);
            StringAssert.Contains("Trenin kalan canı:  %77", t);
            StringAssert.Contains("1 / 12", t);
            StringAssert.Contains("1000 mermi · 2 bomba", t);
            SatirlarDolu(hud.DurumYazisi(), "done");
            StringAssert.StartsWith("Görev tamam", hud.Gorev);
        }

        // ---- warnings, GPS ------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Warnings_AaFire_TrainHit_TooHigh_AndGpsShowsTheEnemies()
        {
            yield return Yukle();
            var gps = Object.FindAnyObjectByType<Bolum4Gps>();
            Assert.IsNotNull(gps.Doku, "the GPS map is drawn");
            Assert.IsTrue(gps.Acik);
            yield return BolgeyeGetir(0);
            UcaksavarMevzii aa = bolgeler[0].YerBirimleri.Select(g => g.GetComponent<UcaksavarMevzii>()).First(a => a != null);
            ucus.GetComponent<Saglik>().BaslangicAyarla(100000);
            Vector3 alcak = AcikNokta(aa.transform.position, 250f, 150f);
            bool aaUyarisi = false;
            yield return Tut(() => (alcak, alcak + Vector3.forward * 100f), 6f, () => (aaUyarisi |= hud.SonUyari.Contains("UÇAKSAVAR ATEŞİ")) && false);
            Assert.IsTrue(aaUyarisi, "AA fire → climb warning");
            Assert.Greater(gps.GorunenDusman, 0, "the GPS shows the awake zone's enemies");

            tren.Saglik.Vur(1, HasarTuru.Mermi, Taraf.Dusman);
            yield return null;
            yield return null;
            StringAssert.Contains("TREN VURULUYOR", hud.SonUyari);

            Vector3 yuksek = tren.Araclar[0].transform.position + Vector3.up * 480f;
            Yerlestir(yuksek, yuksek + Vector3.forward * 100f);
            yield return null;
            Yerlestir(yuksek, yuksek + Vector3.forward * 100f);
            yuk.Birak();
            yield return null;
            StringAssert.Contains("ÇOK YÜKSEK", hud.SonUyari);

            gps.Acik = false;
            yield return null;
            Assert.IsFalse(gps.Acik);
            Assert.IsTrue(gps.isActiveAndEnabled, "hidden GPS still listens for M");
            Olcum.AppendLine($"warnings seen: AA fire, TREN VURULUYOR, ÇOK YÜKSEK; GPS markers {gps.GorunenDusman}");
        }

        // ---- TEST-829 ----------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Assist_MachineGun_AimsAndFiresByItself_AtAnAaGunSlightlyOffTheNose()
        {
            yield return Yukle();
            yield return BolgeyeGetir(0);
            ucus.GetComponent<Saglik>().BaslangicAyarla(100000);
            UcaksavarMevzii aa = bolgeler[0].YerBirimleri.Select(g => g.GetComponent<UcaksavarMevzii>()).First(a => a != null);
            var s = aa.GetComponent<Susturulabilir>();
            Vector3 hedef = aa.transform.position + Vector3.up * 1.5f;
            Vector3 konum = AcikNokta(hedef, 350f, 120f);
            // Nose 4° to the side of the gun: inside the 6° cone, outside the gun's 4 m box at 370 m without the assist.
            Vector3 yanBak = konum + Quaternion.AngleAxis(4f, Vector3.up) * (hedef - konum);
            Assert.IsTrue(nisan.Yardim, "assist on at the start");
            yield return Tut(() => (konum, yanBak), 8f, () => s.Sustu);
            Olcum.AppendLine($"TEST-829 machine-gun assist: AA 4° off the nose silenced = {s.Sustu} with {mk.AtisSayisi} rounds, no trigger pressed");
            Assert.IsTrue(s.Sustu, "the assist aims at the gun and fires by itself");
            Assert.Less(mk.AtisSayisi, 120, "short bursts, rounds not wasted");
        }

        [UnityTest]
        public IEnumerator Assist_Off_NoAutomaticFire_NoAutomaticBomb()
        {
            yield return Yukle();
            yield return BolgeyeGetir(0);
            ucus.GetComponent<Saglik>().BaslangicAyarla(100000);
            nisan.Yardim = false;
            UcaksavarMevzii aa = bolgeler[0].YerBirimleri.Select(g => g.GetComponent<UcaksavarMevzii>()).First(a => a != null);
            Vector3 hedef = aa.transform.position + Vector3.up * 1.5f;
            Vector3 konum = AcikNokta(hedef, 350f, 120f);
            yield return Tut(() => (konum, hedef), 2f);
            Assert.AreEqual(0, mk.AtisSayisi, "no fire without the key when the assist is off");
            Tank t = bolgeler[0].YerBirimleri.Select(g => g.GetComponent<Tank>()).First(x => x != null);
            Vector3 tk = AcikNokta(t.transform.position, 600f, 200f);
            yield return Tut(() => (tk, t.transform.position), 2f);
            Assert.AreEqual(2, yuk.Kalan, "no automatic release when the assist is off");
            Assert.IsFalse(aa.GetComponent<Susturulabilir>().Sustu);
        }

        [UnityTest]
        public IEnumerator Assist_GuidedBomb_ReleasedByItself_SilencesATankPair()
        {
            yield return Yukle();
            yield return BolgeyeGetir(0);
            ucus.GetComponent<Saglik>().BaslangicAyarla(100000);
            Tank[] cift = bolgeler[0].YerBirimleri.Select(g => g.GetComponent<Tank>()).Where(x => x != null).ToArray();
            Vector3 orta = (cift[0].transform.position + cift[1].transform.position) * 0.5f;
            Vector3 konum = AcikNokta(orta, 700f, 220f);
            if (ZeminOlcer.Irtifa(konum) > H.BombaTavani - 20f) konum.y -= ZeminOlcer.Irtifa(konum) - (H.BombaTavani - 40f);   // under the ceiling
            Bomba bomba = null;
            yuk.BombaBirakildi += b => bomba = b;
            yield return Tut(() => (konum, orta), 2f, () => yuk.Kalan < 2);
            float izSonraki = Time.time;
            var iz = new StringBuilder();
            System.Action izle = () =>
            {
                if (Time.time < izSonraki) return;
                izSonraki = Time.time + 0.5f;
                iz.Append(bomba == null ? " [gone]" : $" [{bomba.transform.position:F0} v{bomba.Hiz.magnitude:F0} guided {bomba.Gudumlu} d{Vector3.Distance(bomba.transform.position, orta):F0}]");
            };
            Assert.AreEqual(1, yuk.Kalan, "the assist released one bomb by itself (T/O)");
            Assert.IsTrue(nisan.Secili != null && nisan.Secili.Bomba, "a tank pair is the selected target");
            float sinir = Time.time + 15f;
            while (Time.time < sinir && !cift.All(c => c.GetComponent<Susturulabilir>().Sustu))
            {
                Yerlestir(konum, orta);
                izle();
                yield return null;
            }
            Olcum.AppendLine($"TEST-829 guided bomb track:{iz}");
            Olcum.AppendLine($"TEST-829 guided bomb from {Vector3.Distance(konum, orta):F0} m, {ZeminOlcer.Irtifa(konum):F0} m up: pair silenced = {cift.All(c => c.GetComponent<Susturulabilir>().Sustu)}; automatic releases {nisan.OtomatikBomba}");
            Assert.IsTrue(cift.All(c => c.GetComponent<Susturulabilir>().Sustu), "the guided bomb silences both tanks");
            Assert.AreEqual(1, yuk.Kalan, "one bomb for one pair");
        }
    }
}
