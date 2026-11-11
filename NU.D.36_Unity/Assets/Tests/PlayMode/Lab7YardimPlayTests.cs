using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// The 2026-11-10 assists in Rota07 (user request: brake, easier flight, automatic hits, a GPS), in Play Mode:
    /// take-off with the throttle only, the terrain floor with the dive key held at a hill, a keyboard-style landing with the
    /// brake, the targeting system destroying a tower and the bridge by itself, the Space key's guided / free-fall rule, and the
    /// GPS map. The AA guns are switched off (these tests are about the assists, not about being shot at).
    /// Measurements → Logs/lab7/yardim_olcumu.txt.
    /// </summary>
    public class Lab7YardimPlayTests
    {
        UcusKontrol ucus;
        InisAlgilayici algilayici;
        GeceGorevi gorev;
        BombaYuku yuk;
        NisanSistemi nisan;
        UcusYardimcisi yardimci;
        GpsHaritasi gps;
        static readonly StringBuilder Olcum = new StringBuilder();
        const int Kapasite = 4;

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Rota07", LoadSceneMode.Single);
            yield return null;
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            algilayici = ucus.GetComponent<InisAlgilayici>();
            yuk = ucus.GetComponent<BombaYuku>();
            nisan = ucus.GetComponent<NisanSistemi>();
            yardimci = ucus.GetComponent<UcusYardimcisi>();
            gorev = Object.FindAnyObjectByType<GeceGorevi>();
            gps = Object.FindAnyObjectByType<GpsHaritasi>();
            Object.FindAnyObjectByType<GeceSonuEkrani>().SahneYukle = _ => { };
            foreach (UcaksavarMevzii u in Object.FindObjectsByType<UcaksavarMevzii>(FindObjectsInactive.Exclude))
                u.enabled = false;
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
            File.WriteAllText(Path.Combine(dir, "yardim_olcumu.txt"), $"Rota07 assists, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        void Yerlestir(Vector3 konum, float yon, float hiz, float gaz)
        {
            ucus.transform.SetPositionAndRotation(konum, Quaternion.Euler(0f, yon, 0f));
            ucus.HizAyarla(hiz);
            ucus.GazAyarla(gaz);
            ucus.Yunuslama = ucus.Yatis = ucus.Sapma = 0f;
            Physics.SyncTransforms();
            Lab7PlayTests.CarpismaSifirla(ucus);
        }

        /// <summary>A point <paramref name="agl"/> m above the ground at (x, z).</summary>
        static Vector3 Ustunde(float x, float z, float agl) => new Vector3(x, ZeminOlcer.Yukseklik(x, z) + agl, z);

        static float Yon(Vector3 d) => Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;

        Yikilabilir Kule(string kare) => gorev.Hedefler.Single(h => h.Tur == HedefTuru.Kule && h.name.EndsWith(kare));

        void Sec(Yikilabilir h)
        {
            for (int i = 0; i < gorev.Hedefler.Length && nisan.Hedef != h; i++)
                nisan.SonrakiHedef();
            Assert.AreEqual(h, nisan.Hedef, "selected with T");
        }

        // ---- flight ----------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TakeOff_ThrottleOnly_LiftsOffByItself_AndClimbsToTheProtectedHeight()
        {
            yield return Yukle();
            Assert.IsTrue(ucus.KolayUcus);
            Time.timeScale = 3f;
            ucus.GazAyarla(1f);
            float t = 0f, kalkis = -1f, kalkisHizi = 0f;
            while (t < 120f && !algilayici.Cakti)
            {
                t += Time.deltaTime;
                if (kalkis < 0f && !algilayici.Yerde) { kalkis = t; kalkisHizi = ucus.Hiz; }
                if (kalkis > 0f && t > kalkis + 25f) break;
                yield return null;
            }
            Assert.IsFalse(algilayici.Cakti, $"no crash ({algilayici.CakmaNedeni})");
            Assert.Greater(kalkis, 0f, "left the ground without a pitch key");
            Assert.AreEqual(GorevAsamasi.Saldiri, gorev.Asama);
            Assert.Greater(yardimci.Agl, 35f, "climbed out to the protected height");
            Assert.Less(Mathf.Abs(ucus.YatisAcisi), 3f, "wings level");
            Olcum.AppendLine($"Take-off, throttle only (easy flight): lift-off at {kalkisHizi * 3.6f:F0} km/h after {kalkis:F1} s; " +
                             $"25 s later {yardimci.Agl:F0} m above the ground at {ucus.Hiz * 3.6f:F0} km/h.");
        }

        [UnityTest]
        public IEnumerator TerrainFloor_DiveKeyHeldAtTheHill_NoCrash_OverTheTop()
        {
            yield return Yukle();
            Vector3 tepe = GeceHaritasi.Kare("E4");
            Yerlestir(Ustunde(tepe.x - 1000f, tepe.z + 60f, 90f), 90f, 75f, 0.6f);
            Time.timeScale = 2f;
            float t = 0f, enAz = float.MaxValue;
            bool koruma = false;
            while (t < 25f && !gorev.Bitti)
            {
                ucus.Yunuslama = 1f;   // W held: a player diving at the hill
                t += Time.deltaTime;
                if (t > 1f) enAz = Mathf.Min(enAz, yardimci.Agl);
                koruma |= yardimci.YerKorumasi;
                yield return null;
            }
            Assert.IsFalse(algilayici.Cakti, $"no crash ({algilayici.CakmaNedeni})");
            Assert.IsTrue(koruma, "the floor took over");
            Assert.Greater(enAz, 15f, "lowest height above the ground");
            Assert.Greater(ucus.transform.position.x, tepe.x + 200f, "flew on over the hill");
            Olcum.AppendLine($"Terrain floor: W held for 25 s at 270 km/h towards the 120 m hill E4: no crash, lowest {enAz:F0} m above the ground.");
        }

        [UnityTest]
        public IEnumerator Landing_KeyboardStyle_WithTheBrake_SoftTouchdown_StopsInTheZone()
        {
            yield return Yukle();
            gorev.AnaHedef.Yik();
            yield return null;
            Assert.AreEqual(GorevAsamasi.Inis, gorev.Asama);
            Time.timeScale = 3f;
            Vector3 eksen = GeceHaritasi.PistEkseni, esik = GeceHaritasi.PistSonu;
            Vector3 bas = esik + eksen * 2600f;
            Yerlestir(Ustunde(bas.x, bas.z, 140f), GeceHaritasi.PistYonu + 180f, 62f, 0.15f);
            Vector3 nokta = esik - eksen * 200f, nisanNoktasi = esik - eksen * 3000f;   // steer at a point beyond the far end
            float t = 0f;
            bool temas = false;
            algilayici.InisYapildi += () => temas = true;   // not "Yerde": right after the teleport the probe still says "on the ground"
            while (!gorev.Bitti && t < 150f)
            {
                t += Time.deltaTime;
                Vector3 p = ucus.transform.position;
                if (!temas)
                {
                    // A keyboard player: W held while above a 4° path to the aim point, released on or below it; F held when
                    // fast or low; throttle to idle in the flare; A/D towards a point down the runway.
                    float d = Vector3.Dot(p - nokta, eksen);
                    float yol = Mathf.Max(0f, d) * Mathf.Tan(4f * Mathf.Deg2Rad);
                    ucus.Yunuslama = yardimci.Agl > yol + 4f ? 1f : 0f;
                    yardimci.FrenTutuluyor = ucus.Hiz > 55f || yardimci.Agl < 30f;
                    if (yardimci.Agl < 10f) ucus.GazAyarla(0f);
                    float hata = Mathf.DeltaAngle(ucus.transform.eulerAngles.y, Yon(nisanNoktasi - p));
                    ucus.Yatis = Mathf.Clamp(hata * 0.1f, -1f, 1f);
                }
                else
                {
                    temas = true;
                    ucus.Yunuslama = ucus.Yatis = 0f;
                    ucus.GazAyarla(0f);
                    yardimci.FrenTutuluyor = true;
                }
                yield return null;
            }
            Assert.IsFalse(algilayici.Cakti, $"no crash ({algilayici.CakmaNedeni})");
            Assert.IsTrue(temas, "touched down");
            Assert.Less(algilayici.SonDususHizi, algilayici.EnFazlaDususHizi, "soft touchdown");
            Assert.IsTrue(gorev.Basarili, $"stopped in the zone ({gorev.Neden}, {gorev.Bildirim})");
            float esiktenUzaklik = Vector3.Dot(esik - algilayici.SonInisNoktasi, eksen);
            Olcum.AppendLine($"Landing, keyboard style (W on/off, F, easy flight): touchdown sink {algilayici.SonDususHizi:F2} m/s " +
                             $"(limit {algilayici.EnFazlaDususHizi:F1}), {esiktenUzaklik:F0} m past the threshold, stopped " +
                             $"{Vector3.Distance(algilayici.SonInisNoktasi, ucus.transform.position):F0} m later; score {gorev.Puan}.");
        }

        [UnityTest]
        public IEnumerator MapEdge_HandsOffFlyingOutwards_TurnsBackByItself_BeforeTheMountains()
        {
            yield return Yukle();
            Yerlestir(Ustunde(-2150f, 300f, 120f), 280f, 60f, 0.3f);   // west of the map, flying outwards, no keys
            Time.timeScale = 3f;
            float t = 0f, enUzak = 0f;
            bool dondu = false;
            while (t < 45f && !gorev.Bitti)
            {
                t += Time.deltaTime;
                Vector3 p = ucus.transform.position;
                enUzak = Mathf.Max(enUzak, new Vector2(p.x - yardimci.HaritaMerkezi.x, p.z - yardimci.HaritaMerkezi.z).magnitude);
                dondu |= yardimci.SinirDonusu;
                yield return null;
            }
            Assert.IsFalse(algilayici.Cakti, $"no crash ({algilayici.CakmaNedeni})");
            Assert.IsTrue(dondu, "the map-edge turn acted");
            Assert.Less(enUzak, 2800f, "turned back before the mountains (2800 m)");
            Olcum.AppendLine($"Map edge: hands off at 216 km/h flying west from 2.2 km: turned back by itself, farthest {enUzak:F0} m from the centre (mountains from 2800 m).");
        }

        // ---- targeting -------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator AutoRelease_FlyAtATower_LocksDropsAGuidedBomb_TheTowerFalls()
        {
            yield return Yukle();
            Yikilabilir kule = Kule("F4");
            Vector3 k = kule.IsaretNoktasi;
            Yerlestir(Ustunde(k.x, k.z + 2300f, 220f), 180f, 75f, 0.6f);   // from the north, away from the hill E4
            yield return null;
            Sec(kule);
            int can0 = ucus.GetComponent<Saglik>().Can;
            float kilitMesafesi = -1f, atisMesafesi = -1f;
            int kuleyeAtis = 0;
            nisan.GudumluAtildi += (b, h) =>
            {
                if (h != kule) return;   // after the tower falls the system goes on to the next target in range
                kuleyeAtis++;
                if (atisMesafesi < 0f) atisMesafesi = nisan.Mesafe;
            };
            Time.timeScale = 2f;
            float t = 0f;
            while (!kule.Yikik && t < 45f)
            {
                t += Time.deltaTime;
                if (kilitMesafesi < 0f && nisan.Kilitli) kilitMesafesi = nisan.Mesafe;
                yield return null;
            }
            Assert.IsTrue(kule.Yikik, $"tower down (lock at {kilitMesafesi:F0} m, release at {atisMesafesi:F0} m)");
            Assert.IsTrue(kule.Isildak.Sondu, "its searchlight is out");
            Assert.That(kilitMesafesi, Is.InRange(nisan.KilitMenzili - 150f, nisan.KilitMenzili), "locked on entering the lock range");
            Assert.That(atisMesafesi, Is.InRange(nisan.AtisMenzili - 150f, nisan.AtisMenzili), "released by itself in range");
            Assert.AreEqual(1, kuleyeAtis, "one bomb for the tower");
            Assert.AreEqual(can0, ucus.GetComponent<Saglik>().Can, "the guided bomb exploded far from the aircraft");
            Assert.IsFalse(algilayici.Cakti);
            Olcum.AppendLine($"Automatic release: F4 tower locked at {kilitMesafesi:F0} m, guided bomb released at {atisMesafesi:F0} m, tower down after {t:F1} s with 1 bomb" +
                             $"{(nisan.GudumluSayisi > 1 ? $"; then at once the next target in range ({nisan.Hedef?.GorunenAd})" : "")}.");
        }

        [UnityTest]
        public IEnumerator AutoRelease_TheBridge_OneGuidedBomb_DropsIt_LandingPhase()
        {
            yield return Yukle();
            Yikilabilir kopru = gorev.AnaHedef;
            Vector3 k = kopru.IsaretNoktasi;
            Yerlestir(Ustunde(k.x, k.z + 2200f, 200f), 180f, 75f, 0.6f);   // along the river from the north
            yield return null;
            Sec(kopru);
            int kopruyeAtis = 0;
            nisan.GudumluAtildi += (b, h) => { if (h == kopru) kopruyeAtis++; };
            Time.timeScale = 2f;
            float t = 0f;
            while (!kopru.Yikik && t < 45f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(kopru.Yikik, "bridge down");
            Assert.AreEqual(GorevAsamasi.Inis, gorev.Asama, "return to base");
            Assert.AreEqual(1, kopruyeAtis, "one bomb for the bridge");
            Assert.Greater(kopru.Dusenler.Length, 0);
            Olcum.AppendLine($"Automatic release: the bridge fell after {t:F1} s with one guided bomb; {kopru.Dusenler.Length} panels fell.");
        }

        [UnityTest]
        public IEnumerator SpaceKey_LockedIsGuided_NotLockedIsAFreeFall_NoAutoReleaseWhenOff()
        {
            yield return Yukle();
            nisan.OtomatikAtis = false;
            Yikilabilir kule = Kule("K5");
            Vector3 k = kule.IsaretNoktasi;
            Vector3 p = Ustunde(k.x, k.z + 1400f, 250f);
            Yerlestir(p, 0f, 70f, 0.5f);   // flying away from it: behind
            yield return null;
            yield return null;
            Sec(kule);
            Assert.IsFalse(nisan.Kilitli, "behind: no lock");
            Bomba serbest = yuk.Birak();
            Assert.IsNotNull(serbest);
            Assert.IsFalse(serbest.Gudumlu, "not locked: Space drops a free-falling bomb");

            Yerlestir(p, 180f, 70f, 0.5f);  // turned towards it, 1.4 km: locked, not yet in release range
            yield return null;
            yield return null;
            nisan.Guncelle();
            Assert.IsTrue(nisan.Kilitli, nisan.Durum);
            Assert.IsFalse(nisan.Menzilde);
            Bomba gudumlu = yuk.Birak();
            Assert.IsTrue(gudumlu.Gudumlu, "locked: Space drops a guided bomb");
            Assert.AreEqual(kule, gudumlu.GudumHedefi);
            float t = 0f;
            while (!kule.Yikik && t < 20f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(kule.Yikik, "the guided bomb found the tower");
            Assert.AreEqual(Kapasite - 2, yuk.Kalan, "no automatic release while it is off");
        }

        // ---- GPS ---------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Gps_ShowsEveryTarget_TheSelection_TheAircraft()
        {
            yield return Yukle();
            yield return null;
            yield return null;
            Assert.IsNotNull(gps);
            Assert.AreEqual(gorev.Hedefler.Length, gps.IsaretSayisi, "a marker per target");
            Vector3 merkez = ucus.transform.position;
            float pencere = gps.Pencere, boy = gps.GorusBoyu;
            Assert.Greater(boy, 300f);
            Rect uv = GpsHaritasi.DokuAlani(merkez, pencere);
            Assert.AreEqual(uv.x, gps.Zemin.uvRect.x, 1e-4f, "the map is centred on the aircraft");
            Assert.AreEqual(uv.y, gps.Zemin.uvRect.y, 1e-4f);
            Assert.AreEqual(uv.width, gps.Zemin.uvRect.width, 1e-4f);
            float yari = boy / 2f - 10f;
            for (int i = 0; i < gps.IsaretSayisi; i++)
            {
                Vector2 beklenen = GpsHaritasi.HaritaKonumu(gps.IsaretHedefi(i).IsaretNoktasi, merkez, pencere, boy);
                if (Mathf.Abs(beklenen.x) > yari || Mathf.Abs(beklenen.y) > yari)
                    beklenen *= yari / Mathf.Max(Mathf.Abs(beklenen.x), Mathf.Abs(beklenen.y));
                Assert.Less(Vector2.Distance(beklenen, gps.Isaret(i).anchoredPosition), 0.5f, gps.IsaretHedefi(i).name);
            }
            Assert.AreEqual(Mathf.Repeat(-ucus.transform.eulerAngles.y, 360f), Mathf.Repeat(gps.UcakIsareti.localEulerAngles.z, 360f), 0.5f, "arrow turned to the heading");
            int sec = System.Array.IndexOf(gorev.Hedefler, nisan.Hedef);
            Assert.GreaterOrEqual(sec, 0, "a target is selected");
            Assert.Less(Vector2.Distance(gps.SecimHalkasi.anchoredPosition, gps.Isaret(sec).anchoredPosition), 0.5f, "ring on the selected target");
            StringAssert.Contains("KARE " + GeceHaritasi.KareAdi(merkez.x, merkez.z), gps.Baslik);
            StringAssert.Contains(nisan.Hedef.GorunenAd, gps.AltYazi);

            Yikilabilir kule = Kule("H7");
            kule.Yik();
            yield return null;
            yield return null;
            int i7 = System.Array.IndexOf(gorev.Hedefler, kule);
            Color c = gps.IsaretRengi(i7);
            Assert.Less(Mathf.Abs(c.r - c.g) + Mathf.Abs(c.g - c.b), 0.05f, "a fallen target turns grey");
            Assert.AreNotEqual(kule, nisan.Hedef, "the selection moves to a standing target");
        }
    }
}
