using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NU36.Tests
{
    /// <summary>
    /// Main menu, pause menu and the Rota07 gauges in Play Mode: panels and buttons of AnaMenu (scene loads are captured,
    /// not performed), pausing and resuming Rota07, the persistent pause menu's rule, and the needle, knob and read-outs
    /// following the flight model.
    /// </summary>
    public class MenuPlayTests
    {
        [TearDown]
        public void Bitir()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (DuraklatmaMenusu.Kalici != null)
                Object.Destroy(DuraklatmaMenusu.Kalici.gameObject);
        }

        static IEnumerator Yukle(string ad)
        {
            yield return SceneManager.LoadSceneAsync(ad, LoadSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_Panels_Buttons_SceneLoads_Settings()
        {
            yield return Yukle("AnaMenu");
            var menu = Object.FindAnyObjectByType<AnaMenu>();
            Assert.IsNotNull(menu);
            var yuklenen = new List<string>();
            bool cikti = false;
            menu.SahneYukle = yuklenen.Add;
            menu.Cikis = () => cikti = true;
            yield return null;

            Assert.AreEqual(AnaMenu.Panel.Ana, menu.Acik);
            Assert.IsNotNull(DuraklatmaMenusu.Kalici, "the menu creates the persistent pause menu");
            Assert.IsFalse(DuraklatmaMenusu.Kalici.Gorevde, "idle in the menu scene");
            MenuDugmesi basla = menu.Dugme("Basla");
            Assert.IsTrue(basla.interactable);
            Assert.AreEqual(MenuDugmesi.Uzerinde, basla.GorselSatir, "first button selected for the keyboard (hover row)");
            Assert.AreEqual("GÖREVE BAŞLA", basla.Yazi);

            basla.onClick.Invoke();
            Assert.AreEqual("Bolum5_KartalGecidi", yuklenen.Last(), "Hafta 9: the current mission (D-118, D-136)");

            menu.Dugme("Bolumler").onClick.Invoke();
            Assert.AreEqual(AnaMenu.Panel.Bolumler, menu.Acik);
            Assert.IsTrue(menu.Dugme("Bolum_Rota05").gameObject.activeInHierarchy);
            Assert.IsFalse(menu.Dugme("Basla").gameObject.activeInHierarchy, "main panel hidden");
            foreach (AnaMenu.Bolum b in menu.Bolumler)
                Assert.IsTrue(menu.Dugme("Bolum_" + b.sahne).interactable, b.sahne + " is in the build");
            Assert.AreEqual("HAFTA 5 · SINIR HATTI", menu.Dugme("Bolum_Rota05").Yazi);
            menu.Dugme("Bolum_Rota05").onClick.Invoke();
            Assert.AreEqual("Rota05", yuklenen.Last());
            menu.BolumAc("YokBoyleSahne");
            Assert.AreEqual("Rota05", yuklenen.Last(), "a scene that is not in the build is not loaded");
            menu.Dugme("Geri_Bolumler").onClick.Invoke();
            Assert.AreEqual(AnaMenu.Panel.Ana, menu.Acik);

            // Options: the volume button steps the stored value and its label (restored afterwards).
            bool vardi = PlayerPrefs.HasKey(OyunAyarlari.SesAnahtari);
            float eski = OyunAyarlari.Ses;
            try
            {
                menu.Dugme("Secenekler").onClick.Invoke();
                Assert.AreEqual(AnaMenu.Panel.Secenekler, menu.Acik);
                Assert.AreEqual(OyunAyarlari.SesYazisi, menu.Dugme("Ses").Yazi);
                float beklenen = OyunAyarlari.SonrakiSes(eski);
                menu.Dugme("Ses").onClick.Invoke();
                Assert.AreEqual(beklenen, OyunAyarlari.Ses, 1e-4f);
                Assert.AreEqual(beklenen, AudioListener.volume, 1e-4f);
                Assert.AreEqual($"SES: %{Mathf.RoundToInt(beklenen * 100f)}", menu.Dugme("Ses").Yazi);
                StringAssert.StartsWith("GRAFİK: ", menu.Dugme("Kalite").Yazi);
                StringAssert.StartsWith("EKRAN: ", menu.Dugme("Ekran").Yazi);
            }
            finally
            {
                if (vardi) OyunAyarlari.Ses = eski;
                else { PlayerPrefs.DeleteKey(OyunAyarlari.SesAnahtari); AudioListener.volume = 1f; }
            }
            menu.Dugme("Geri_Secenekler").onClick.Invoke();

            menu.Dugme("Kontroller").onClick.Invoke();
            Assert.AreEqual(AnaMenu.Panel.Kontroller, menu.Acik);
            menu.Dugme("Geri_Kontroller").onClick.Invoke();

            // Disabled state uses the sheet's disabled row.
            basla.interactable = false;
            yield return null;
            Assert.AreEqual(MenuDugmesi.Pasif, basla.GorselSatir);
            Assert.AreEqual(MenuDugmesi.Satir(MenuDugmesi.Pasif), basla.GetComponent<RawImage>().uvRect);
            basla.interactable = true;

            menu.Dugme("Cikis").onClick.Invoke();
            Assert.IsTrue(cikti);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Rota07_PauseMenu_StopsAndResumesTheGame()
        {
            yield return Yukle("Rota07");
            var son = Object.FindAnyObjectByType<GeceSonuEkrani>();
            son.SahneYukle = _ => { };
            DuraklatmaMenusu menu = Object.FindObjectsByType<DuraklatmaMenusu>(FindObjectsSortMode.None).Single(m => !m.KaliciMi);
            var yuklenen = new List<string>();
            menu.SahneYukle = yuklenen.Add;
            DuraklatmaMenusu kalici = DuraklatmaMenusu.KaliciOlustur(null, "AnaMenu");
            Assert.IsTrue(menu.Gorevde);
            Assert.IsFalse(kalici.Gorevde, "the scene's own pause menu has priority");
            var ucus = Object.FindAnyObjectByType<UcusKontrol>();
            ucus.HizAyarla(40f);
            yield return null;

            menu.Ac();
            Assert.IsTrue(menu.Acik);
            Assert.IsTrue(DuraklatmaMenusu.Duraklatildi);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
            Assert.IsTrue(menu.DevamDugmesi.gameObject.activeInHierarchy);
            Assert.IsTrue(menu.AnaMenuDugmesi.interactable, "AnaMenu is in the build");
            Vector3 once = ucus.transform.position;
            for (int i = 0; i < 5; i++)
                yield return null;
            Assert.AreEqual(once, ucus.transform.position, "the aircraft does not move while paused");

            menu.DevamDugmesi.onClick.Invoke();
            Assert.IsFalse(menu.Acik);
            Assert.IsFalse(DuraklatmaMenusu.Duraklatildi);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
            Assert.IsFalse(menu.DevamDugmesi.gameObject.activeInHierarchy);

            menu.Ac();
            menu.YenidenDugmesi.onClick.Invoke();
            Assert.AreEqual("Rota07", yuklenen.Last());
            Assert.AreEqual(1f, Time.timeScale, "a restart never starts frozen");
            menu.Ac();
            menu.AnaMenuDugmesi.onClick.Invoke();
            Assert.AreEqual("AnaMenu", yuklenen.Last());
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator PersistentPauseMenu_WorksInAnOlderLabScene_AndResumesOnLoad()
        {
            DuraklatmaMenusu kalici = DuraklatmaMenusu.KaliciOlustur(null, "AnaMenu");
            yield return Yukle("Rota05");
            Assert.IsTrue(kalici != null, "survives the scene load");
            Assert.IsTrue(kalici.Gorevde, "Rota05 has no pause menu of its own");
            kalici.Ac();
            Assert.AreEqual(0f, Time.timeScale);
            yield return Yukle("Rota05");   // e.g. the level's own R restart while paused
            Assert.IsFalse(kalici.Acik);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Rota07_Gauges_FollowAirspeedAndThrottle_AndHideAtTheEnd()
        {
            yield return Yukle("Rota07");
            Object.FindAnyObjectByType<GeceSonuEkrani>().SahneYukle = _ => { };
            var ucus = Object.FindAnyObjectByType<UcusKontrol>();
            var hiz = Object.FindAnyObjectByType<HudGosterge>();
            var gaz = Object.FindAnyObjectByType<HudGazGostergesi>();
            Text hizMetni = GameObject.Find("HizMetni").GetComponent<Text>();
            Text gazMetni = GameObject.Find("GazMetni").GetComponent<Text>();
            Text ucusMetni = GameObject.Find("Ucus").GetComponent<Text>();

            foreach ((float v, float g) in new[] { (0f, 0f), (49f, 0.5f), (100f, 1f), (180f, 0.25f) })
            {
                Time.timeScale = 0f;   // freeze the flight model so the values stay put between frames
                ucus.HizAyarla(v);
                ucus.GazAyarla(g);
                yield return null;
                yield return null;
                Assert.AreEqual(ucus.Hiz, hiz.Deger, 1e-3f);
                Assert.AreEqual(HudHesap.IbreZ(ucus.Hiz, 500f / 3.6f), hiz.IbreZ, 1e-3f);
                Assert.AreEqual(hiz.IbreZ, Mathf.DeltaAngle(0f, hiz.Ibre.localEulerAngles.z), 1e-2f);
                Assert.AreEqual(HudHesap.KolY(ucus.GazOrani, gaz.AltY, gaz.UstY), gaz.Kol.anchoredPosition.y, 1e-3f);
                Assert.AreEqual(GeceHud.HizYazisi(ucus.Hiz), hizMetni.text);
                Assert.AreEqual(GeceHud.GazYazisi(ucus.GazOrani), gazMetni.text);
            }
            Assert.AreEqual(-135f, hiz.IbreZ, 1e-3f, "650 km/h stays at the end of the scale");
            StringAssert.DoesNotContain("km/sa", ucusMetni.text, "airspeed is on the dial, not in the text block");
            StringAssert.Contains("İRTİFA", ucusMetni.text);
            Time.timeScale = 1f;

            var gorev = Object.FindAnyObjectByType<GeceGorevi>();
            gorev.Bitir(false, GeceGorevi.NedenCakildi);
            yield return null;
            yield return null;
            Assert.IsTrue(gorev.Bitti);
            Assert.IsFalse(hiz.gameObject.activeInHierarchy, "gauges hidden behind the end screen");
        }
    }
}
