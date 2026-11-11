using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NU36.Tests
{
    /// <summary>
    /// Bölüm 3 rules of the Hafta 6–7 worksheet in the night level (Rota07): dawn ends the night (and the mission when the
    /// aircraft has not landed), the single rule "Bulut" hides the aircraft from the searchlights at Orta in E5–G5, the sound
    /// locator (H3) turns the searchlights to the heard point and lets the guns fire at it, the guns fire tracer rounds, and
    /// the bridge alone is the mission target. Measurements → Logs/lab7/bolum3_olcumu.txt.
    /// </summary>
    public class Lab7Bolum3PlayTests
    {
        UcusKontrol ucus;
        GeceGorevi gorev;
        SesDinleyici kulak;
        static readonly StringBuilder Olcum = new StringBuilder();

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Rota07", LoadSceneMode.Single);
            yield return null;
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            gorev = Object.FindAnyObjectByType<GeceGorevi>();
            kulak = Object.FindAnyObjectByType<SesDinleyici>();
            Object.FindAnyObjectByType<GeceSonuEkrani>().SahneYukle = _ => { };
            Lab7PlayTests.YardimlariKapat(ucus);   // the worksheet's rules, without the 2026-11-10 assists
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Bitir() => Time.timeScale = 1f;

        [OneTimeTearDown]
        public void Yaz()
        {
            if (Olcum.Length == 0) return;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab7"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "bolum3_olcumu.txt"), $"Bölüm 3 rules in Rota07, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        static IEnumerator Bekle(float saniye)
        {
            float bitis = Time.time + saniye;
            while (Time.time < bitis)
                yield return null;
        }

        /// <summary>Holds the aircraft at a point, still "flying" (enabled), with a throttle.</summary>
        void Tut(Vector3 konum, float yon, float gaz)
        {
            ucus.transform.SetPositionAndRotation(konum, Quaternion.Euler(0f, yon, 0f));
            ucus.HizAyarla(0f);
            ucus.GazAyarla(gaz);
            ucus.Yunuslama = ucus.Yatis = ucus.Sapma = 0f;
            Physics.SyncTransforms();
            Lab7PlayTests.CarpismaSifirla(ucus);
        }

        static Isildak IsildakKaresi(string kare) =>
            Object.FindObjectsByType<Isildak>(FindObjectsInactive.Exclude).Single(i => i.GetComponentInParent<Yikilabilir>().name.EndsWith(kare));

        static Text HudYazisi(string ad) => Object.FindAnyObjectByType<GeceHud>().transform.Find(ad).GetComponent<Text>();

        static Vector3 KareUstu(string kare, float yukseklik)
        {
            Vector3 c = GeceHaritasi.Kare(kare);
            return new Vector3(c.x, yukseklik, c.z);
        }

        // ---- dawn ---------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Dawn_BrightensTheSky_AndEndsTheMission_WhenNotLanded()
        {
            yield return Yukle();
            Assert.AreEqual(480f, gorev.SafakSuresi, "8 minutes of night");
            var safak = Object.FindAnyObjectByType<Safak>();
            Light ay = RenderSettings.sun;
            Color geceIsigi = ay.color;
            Color geceSisi = RenderSettings.fogColor;
            Assert.AreEqual(0f, safak.Aydinlik);
            Text hedefMetni = HudYazisi("Hedefler");
            yield return null;
            StringAssert.Contains("ŞAFAK    8:00", hedefMetni.text);

            gorev.SafakSuresi = 6f;   // a short night for the test
            float t = 0f;
            float yari = -1f;
            while (!gorev.Bitti && t < 10f)
            {
                t += Time.deltaTime;
                if (yari < 0f && gorev.SafakOrani > 0.86f) yari = safak.Aydinlik;
                yield return null;
            }
            yield return null;
            Assert.IsTrue(gorev.Bitti);
            Assert.IsFalse(gorev.Basarili);
            Assert.AreEqual(GeceGorevi.NedenSafak, gorev.Neden);
            Assert.AreEqual("köprü hâlâ ayakta", gorev.Ayrinti);
            Assert.That(yari, Is.InRange(0.05f, 0.95f), "the sky brightens gradually");
            Assert.AreEqual(1f, safak.Aydinlik, 1e-3f);
            Assert.AreEqual(1f, RenderSettings.skybox.GetFloat("_Safak"), 1e-3f, "dawn glow on the sky");
            Assert.Greater(ay.color.r - ay.color.b, geceIsigi.r - geceIsigi.b + 0.2f, "the light warms (sunrise)");
            Assert.Greater(RenderSettings.fogColor.grayscale, geceSisi.grayscale + 0.2f, "the fog brightens");
            Assert.Greater(Vector3.Dot(-ay.transform.forward, Vector3.right), 0.9f, "the sun stands in the east");
            Olcum.AppendLine($"Dawn: mission over at {gorev.GecenSure:F1} s (night shortened to 6 s); blend {yari:F2} at 86 % of the night, 1.00 at dawn.");
        }

        [UnityTest]
        public IEnumerator BridgeDown_ThenLanded_BeforeDawn_IsTheWin_TowersOptional()
        {
            yield return Yukle();
            gorev.AnaHedef.Yik();
            yield return null;
            Assert.AreEqual(GorevAsamasi.Inis, gorev.Asama, "the bridge alone opens the return to base");
            Assert.AreEqual(0, gorev.Sayi(HedefTuru.Kule, true), "no tower needed");
            Assert.IsTrue(gorev.InisBolgesi.Aktif);
            yield return null;
            StringAssert.StartsWith("ÜSSE DÖN", HudYazisi("Gorev").text);
            // Stopping in the zone ends the mission (the full approach is in Lab7PlayTests).
            Vector3 p = GeceHaritasi.InisBolgesiMerkezi;
            ucus.transform.SetPositionAndRotation(new Vector3(p.x, p.y + 0.3f + ucus.GetComponent<InisAlgilayici>().TekerOfseti, p.z), Quaternion.Euler(0f, 270f, 0f));
            ucus.HizAyarla(0f);
            Physics.SyncTransforms();
            Lab7PlayTests.CarpismaSifirla(ucus);
            float t = 0f;
            while (!gorev.Bitti && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(gorev.Basarili, $"{gorev.Neden} {gorev.Ayrinti}");
            Assert.Greater(gorev.KalanSure, 0f, "before dawn");
        }

        // ---- the single rule: clouds ------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Cloud_AtOrta_HidesTheAircraft_FromTheSearchlight()
        {
            yield return Yukle();
            kulak.enabled = false;
            Isildak isildak = IsildakKaresi("F4");
            foreach (UcaksavarMevzii u in Object.FindObjectsByType<UcaksavarMevzii>(FindObjectsInactive.Exclude))
                u.enabled = false;

            // In the F5 cloud at Orta (200 m), the head turned straight onto it: no catch.
            Vector3 bulutta = KareUstu("F5", Bolum3Harita.Orta);
            Assert.IsTrue(Bulut.Gizler(bulutta));
            float t = 0f;
            while (t < 2f)
            {
                Tut(bulutta, 90f, 0.5f);
                isildak.Kafa.rotation = Quaternion.LookRotation(bulutta - isildak.Kafa.position);
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(isildak.Yakaladi, "a searchlight cannot catch an aircraft in the cloud");
            StringAssert.Contains("BULUT   F5", HudYazisi("Ucus").text);

            // Same square under the cloud (Alçak) and over it: caught.
            foreach (float y in new[] { 70f, 330f })
            {
                Vector3 disari = KareUstu("F5", y);
                Assert.IsFalse(Bulut.Gizler(disari));
                t = 0f;
                while (!isildak.Yakaladi && t < 2f)
                {
                    Tut(disari, 90f, 0.5f);
                    isildak.Kafa.rotation = Quaternion.LookRotation(disari - isildak.Kafa.position);
                    t += Time.deltaTime;
                    yield return null;
                }
                Assert.IsTrue(isildak.Yakaladi, $"caught at {y} m outside the cloud band");
                // Into the cloud while held: lost.
                t = 0f;
                while (isildak.Yakaladi && t < 4f)
                {
                    Tut(bulutta, 90f, 0.5f);
                    t += Time.deltaTime;
                    yield return null;
                }
                Assert.IsFalse(isildak.Yakaladi, "lost in the cloud");
                Olcum.AppendLine($"Cloud: caught at {y} m over F5, lost {t:F1} s after entering the cloud at 200 m.");
            }
        }

        // ---- sound locator ------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator SoundLocator_Hears_SearchlightsTurn_GunsFireAtTheHeardPoint_WithTracers()
        {
            yield return Yukle();
            Vector3 ses = kulak.transform.position;
            // 600 m from H3 at 300 m (G2), engine loud.
            Vector3 p = KareUstu("G2", 320f);
            Assert.Less(Vector3.Distance(new Vector3(ses.x, 0f, ses.z), new Vector3(p.x, 0f, p.z)), GeceHaritasi.SesYaricapi);
            Isildak f4 = IsildakKaresi("F4");
            float aci0 = Vector3.Angle(f4.Kafa.forward, p - f4.Kafa.position);
            float t = 0f;
            while (t < 3f)
            {
                Tut(p, 90f, 0.7f);
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(kulak.Duyuyor, "heard within the 1000 m ring");
            Assert.IsTrue(f4.SeseDonuk || f4.Yakaladi, "F4 turned to the heard point (or already holds the aircraft)");
            float aci1 = Vector3.Angle(f4.Kafa.forward, p - f4.Kafa.position);
            Assert.Less(aci1, Mathf.Max(aci0 - 10f, 15f), $"the head turned towards the aircraft ({aci0:F0}° → {aci1:F0}°)");
            GameObject isaret = Object.FindAnyObjectByType<GeceHud>().transform.Find("Isaretler").GetComponentsInChildren<Text>().FirstOrDefault(x => x.text.StartsWith("DUYULDU"))?.gameObject;
            Assert.IsNotNull(isaret, "the lead mark of the heard square on the HUD");

            // Searchlights out (as if destroyed): the guns still fire at the heard point, every round a tracer.
            foreach (Isildak i in Object.FindObjectsByType<Isildak>(FindObjectsInactive.Exclude))
                i.Sondur();
            var toplar = Object.FindObjectsByType<UcaksavarMevzii>(FindObjectsInactive.Exclude);
            int atis0 = toplar.Sum(u => u.AtisSayisi);
            bool izGoruldu = false;
            t = 0f;
            while (t < 3f)
            {
                Tut(p, 90f, 0.7f);
                izGoruldu |= Object.FindObjectsByType<Mermi>(FindObjectsInactive.Exclude).Any(m => m.GetComponent<TrailRenderer>() != null);
                t += Time.deltaTime;
                yield return null;
            }
            int heard = toplar.Sum(u => u.AtisSayisi) - atis0;
            Assert.Greater(heard, 0, "guns fire at the heard point");
            Assert.IsTrue(toplar.Any(u => u.KulaklaDuyuyor && !u.GozleGoruyor), "aim from the ear, not the eye");
            Assert.IsTrue(izGoruldu, "tracer rounds in the air");

            // Throttle cut below the silence threshold: not heard, the guns fall silent.
            t = 0f;
            while (t < 1f) { Tut(p, 90f, 0.1f); t += Time.deltaTime; yield return null; }
            int atis1 = toplar.Sum(u => u.AtisSayisi);
            t = 0f;
            while (t < 2f) { Tut(p, 90f, 0.1f); t += Time.deltaTime; yield return null; }
            Assert.IsFalse(kulak.Duyuyor);
            Assert.AreEqual(atis1, toplar.Sum(u => u.AtisSayisi), "silent engine: no fire");
            Olcum.AppendLine($"Sound locator: heard at {Vector3.Distance(ses, p):F0} m; F4 head {aci0:F0}° → {aci1:F0}° off the aircraft in 3 s; {heard} rounds at the heard point in 3 s; gliding (throttle 0.1): silent.");
        }

        // ---- range of the guns ----------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Guns_DoNotFire_OutOfRange_AndUnheard()
        {
            yield return Yukle();
            foreach (Isildak i in Object.FindObjectsByType<Isildak>(FindObjectsInactive.Exclude))
                i.Sondur();
            // North-east of the paper map: outside the locator's ring and beyond every gun's paper range.
            Vector3 p = new Vector3(1600f, 300f, 900f);
            Assert.Greater(Vector3.Distance(kulak.transform.position, p), GeceHaritasi.SesYaricapi);
            var toplar = Object.FindObjectsByType<UcaksavarMevzii>(FindObjectsInactive.Exclude);
            foreach (UcaksavarMevzii u in toplar)
                Assert.Greater(Vector3.Distance(u.transform.position, p), GeceHaritasi.UcaksavarYaricapi);
            int atis0 = toplar.Sum(u => u.AtisSayisi);
            float t = 0f;
            while (t < 2f) { Tut(p, 0f, 0.8f); t += Time.deltaTime; yield return null; }
            Assert.IsFalse(kulak.Duyuyor, "outside the locator's ring");
            Assert.AreEqual(atis0, toplar.Sum(u => u.AtisSayisi));
        }
    }

}
