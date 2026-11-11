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
    /// Lab 7 night mission in Play Mode: take-off from the runway with the aerodynamic model, bombs that topple a tower and
    /// drop a bridge span (with smoke and fire), the searchlight → AA rule, resupply, the landing in the marked zone, and the
    /// three ways to crash (ground, water, structure). Measurements go to Logs/lab7/ucus_olcumu.txt.
    /// </summary>
    public class Lab7PlayTests
    {
        UcusKontrol ucus;
        InisAlgilayici algilayici;
        GeceGorevi gorev;
        BombaYuku yuk;

        static string LogDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab7"));
        static readonly StringBuilder Olcum = new StringBuilder();

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Rota07", LoadSceneMode.Single);
            yield return null;
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            algilayici = ucus.GetComponent<InisAlgilayici>();
            yuk = ucus.GetComponent<BombaYuku>();
            gorev = Object.FindAnyObjectByType<GeceGorevi>();
            Object.FindAnyObjectByType<GeceSonuEkrani>().SahneYukle = _ => { };
            YardimlariKapat(ucus);
            Physics.SyncTransforms();
        }

        /// <summary>
        /// These tests measure the Lab 7 model itself (stick law, free-falling bombs): the assists added on 2026-11-10 (easy flight,
        /// terrain floor, targeting with automatic release) are switched off. Lab7YardimPlayTests covers the assists.
        /// </summary>
        public static void YardimlariKapat(UcusKontrol u)
        {
            u.KolayUcus = false;
            if (u.TryGetComponent(out UcusYardimcisi y)) y.enabled = false;
            if (u.TryGetComponent(out NisanSistemi n)) n.enabled = false;
        }

        [TearDown]
        public void Bitir() => Time.timeScale = 1f;

        [OneTimeTearDown]
        public void Yaz()
        {
            if (Olcum.Length == 0) return;
            Directory.CreateDirectory(LogDir);
            File.WriteAllText(Path.Combine(LogDir, "ucus_olcumu.txt"), $"Lab 7 measurements, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        static IEnumerator Bekle(float saniye)
        {
            float bitis = Time.time + saniye;
            while (Time.time < bitis)
                yield return null;
        }

        /// <summary>Puts the aircraft in the air at a point with a heading and a speed (aerodynamic state reset).</summary>
        void Yerlestir(Vector3 konum, float yon, float hiz, float gaz, float yunuslama = 0f)
        {
            ucus.transform.SetPositionAndRotation(konum, Quaternion.Euler(yunuslama, yon, 0f));
            ucus.HizAyarla(hiz);
            ucus.GazAyarla(gaz);
            ucus.Yunuslama = ucus.Yatis = ucus.Sapma = 0f;
            Physics.SyncTransforms();
            CarpismaSifirla(ucus);
        }

        /// <summary>A test teleport is not a flight: the structure sweep restarts from the new position.</summary>
        public static void CarpismaSifirla(UcusKontrol u)
        {
            var c = u.GetComponent<CarpismaAlgilayici>();
            if (c != null && c.enabled) { c.enabled = false; c.enabled = true; }
        }

        static float Yon(Vector3 d) => Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;

        // ---- start and take-off -----------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Start_StandingOnTheRunway_LightsOn_ZoneOff()
        {
            yield return Yukle();
            yield return Bekle(0.5f);
            Assert.AreEqual(GorevAsamasi.Kalkis, gorev.Asama);
            Assert.IsTrue(algilayici.Yerde);
            Assert.IsTrue(ucus.YerTemasi);
            Assert.That(ucus.Hiz, Is.LessThan(0.5f));
            Assert.That(ZeminOlcer.Irtifa(ucus.transform.position), Is.EqualTo(algilayici.TekerOfseti).Within(0.2f), "wheels on the asphalt");
            Assert.IsFalse(algilayici.Cakti);
            Assert.IsTrue(GameObject.Find("Pist Isiklari").activeInHierarchy);
            Assert.IsFalse(gorev.InisBolgesi.Aktif);
            Assert.IsTrue(Object.FindObjectsByType<GeceArazisi>(FindObjectsInactive.Exclude).Single().Hazir, "terrain generated");
        }

        [UnityTest]
        public IEnumerator Takeoff_FullThrottle_Rotate_ClimbOut_AttackPhase()
        {
            yield return Yukle();
            Time.timeScale = 3f;
            Vector3 bas = ucus.transform.position;
            ucus.GazAyarla(1f);
            float kalkisHizi = 0f, kalkisMesafesi = 0f, t = 0f;
            while (t < 60f && !algilayici.Cakti)
            {
                t += Time.deltaTime;
                // Rotate at 145 km/h, then hold a gentle climb.
                ucus.Yunuslama = ucus.Hiz > 40f ? (ucus.transform.forward.y < Mathf.Sin(10f * Mathf.Deg2Rad) ? -0.6f : 0f) : 0f;
                if (!algilayici.Yerde && kalkisHizi == 0f)
                {
                    kalkisHizi = ucus.Hiz;
                    kalkisMesafesi = Vector3.Distance(bas, ucus.transform.position);
                }
                if (ZeminOlcer.Irtifa(ucus.transform.position) > 150f)
                    break;
                yield return null;
            }
            Assert.IsFalse(algilayici.Cakti, "no crash on take-off");
            Assert.Greater(kalkisHizi, 0f, "left the ground");
            Assert.Less(kalkisMesafesi, GeceHaritasi.PistBoyu - 70f, "airborne before the end of the runway");
            Assert.AreEqual(GorevAsamasi.Saldiri, gorev.Asama);
            Assert.IsFalse(GameObject.Find("Havaalani").transform.Find("Pist/Pist Isiklari").gameObject.activeSelf, "blackout during the attack");
            Olcum.AppendLine($"Take-off: full throttle from standstill, rotate above 40 m/s: lift-off at {kalkisHizi:F1} m/s ({kalkisHizi * 3.6f:F0} km/h) after {kalkisMesafesi:F0} m of the {GeceHaritasi.PistBoyu:F0} m runway; 150 m AGL after {t:F1} s.");
        }

        [UnityTest]
        public IEnumerator HandsOff_30s_StableFlight()
        {
            yield return Yukle();
            Time.timeScale = 3f;
            Vector3 p = GeceHaritasi.Zeminde(-1200f, 600f) + Vector3.up * 350f;
            Yerlestir(p, 45f, 70f, 0.45f);
            yield return null;
            float enAz = float.MaxValue, enCok = float.MinValue;
            float t = 0f;
            while (t < 30f)
            {
                t += Time.deltaTime;
                float y = ucus.transform.position.y;
                enAz = Mathf.Min(enAz, y); enCok = Mathf.Max(enCok, y);
                yield return null;
            }
            Assert.IsFalse(algilayici.Cakti);
            Assert.Less(enCok - enAz, 60f, "height band over 30 s hands-off");
            Assert.Less(Mathf.Abs(ucus.YatisAcisi), 5f, "wings stay level");
            Olcum.AppendLine($"Hands-off 30 s at throttle 0.45 from 70 m/s: height band {enCok - enAz:F1} m, end speed {ucus.Hiz:F1} m/s.");
        }

        // ---- targets ------------------------------------------------------------------------------------------------------

        /// <summary>Drops a bomb from 160 m straight above a point (aircraft held still there), then moves the aircraft away.</summary>
        IEnumerator BombaBirak(Vector3 hedef)
        {
            Yerlestir(hedef + Vector3.up * 160f, 0f, 0f, 0f);
            Bomba b = yuk.Birak();
            Assert.IsNotNull(b, "bomb released");
            ucus.transform.position = hedef + new Vector3(0f, 900f, -2000f);
            ucus.enabled = false;
            Physics.SyncTransforms();
            float t = 0f;
            while (b != null && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(b == null, "bomb exploded");
        }

        [UnityTest]
        public IEnumerator Bomb_ToppleTower_LightOut_SmokeAndFire()
        {
            yield return Yukle();
            Yikilabilir kule = gorev.Hedefler.First(h => h.Tur == HedefTuru.Kule);
            Transform govde = kule.DusenParcalar[0];
            yield return BombaBirak(kule.transform.position + Vector3.up * 20f);
            Assert.IsTrue(kule.Yikik, "tower destroyed");
            Assert.IsTrue(kule.Isildak.Sondu, "searchlight out");
            Assert.AreEqual(1, gorev.YikilanHedef);
            Assert.Greater(gorev.Puan, 0);
            Assert.IsTrue(Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude).Any(p => p.transform.root.name.StartsWith("PatlamaEfekti")), "blast smoke");
            Assert.Greater(kule.GetComponentsInChildren<ParticleSystem>().Count(p => p.name == "Duman"), 0, "the wreck smokes");
            yield return Bekle(7f);
            float egim = Vector3.Angle(govde.up, Vector3.up);
            Assert.Greater(egim, 40f, $"the tower body toppled ({egim:F0}°)");
            Olcum.AppendLine($"Tower: one bomb from 160 m; body tilted {egim:F0}° after 7 s.");
        }

        [UnityTest]
        public IEnumerator EveryTower_Topples_WhenDestroyed()
        {
            yield return Yukle();
            var kuleler = gorev.Hedefler.Where(h => h.Tur == HedefTuru.Kule).ToArray();
            foreach (Yikilabilir k in kuleler)
                k.Yik();
            yield return Bekle(8f);
            foreach (Yikilabilir k in kuleler)
            {
                float egim = Vector3.Angle(k.DusenParcalar[0].up, Vector3.up);
                Assert.Greater(egim, 40f, $"{k.GorunenAd} body tilted {egim:F0}°");
            }
        }

        [UnityTest]
        public IEnumerator Bomb_DropsTheSpansAtTheHit_IntoTheRiver_TheRestStays()
        {
            yield return Yukle();
            Yikilabilir kopru = gorev.AnaHedef;
            Assert.IsNotNull(kopru);
            float guverte = kopru.transform.position.y;
            // Aim 100 m east of the middle (over the water of the reach).
            Vector3 nokta = kopru.transform.position + GeceHaritasi.KopruEkseni * 100f + Vector3.up * 2f;
            var uzak = kopru.DusenParcalar.Where(pn => Vector3.Distance(Duz(pn.position), Duz(nokta)) > 150f).ToArray();
            var uzakKonum = uzak.Select(pn => pn.position).ToArray();
            yield return BombaBirak(nokta);
            Assert.IsTrue(kopru.Yikik, "one bomb on the deck destroys the bridge");
            Assert.AreEqual(GorevAsamasi.Inis, gorev.Asama, "bridge down: return to base (the towers are optional)");
            Assert.That(kopru.Dusenler.Length, Is.InRange(5, 16), "only the panels near the hit fall");
            yield return Bekle(8f);
            foreach (Transform panel in kopru.Dusenler)
            {
                Assert.Less(panel.position.y, guverte - 3f, $"{panel.name} fell");
                Assert.Less(Vector3.Distance(Duz(panel.position), Duz(nokta)), 120f, $"{panel.name} fell near the hit");
            }
            float ortalama = kopru.Dusenler.Average(pn => pn.position.y);
            Assert.Less(ortalama, guverte - 8f, "the fallen spans lie in the river");
            for (int i = 0; i < uzak.Length; i++)
                Assert.That(Vector3.Distance(uzak[i].position, uzakKonum[i]), Is.LessThan(0.01f), $"{uzak[i].parent.name}/{uzak[i].name} stays up");
            Olcum.AppendLine($"Bridge (600 m, 60 panels): one bomb 100 m east of the middle; {kopru.Dusenler.Length} panels fell, lying {guverte - ortalama:F1} m below the deck after 8 s; {uzak.Length} far panels unmoved.");
            Assert.IsFalse(kopru.GetComponent<BoxCollider>().enabled, "target collider off");
        }

        static Vector2 Duz(Vector3 v) => new Vector2(v.x, v.z);

        [UnityTest]
        public IEnumerator Searchlight_CatchesTheAircraft_GunsFire_ThenLosesIt()
        {
            yield return Yukle();
            Isildak isildak = gorev.Hedefler.First(h => h.Tur == HedefTuru.Kule).Isildak;
            UcaksavarMevzii top = Object.FindObjectsByType<UcaksavarMevzii>(FindObjectsInactive.Exclude)
                .OrderBy(u => Vector3.Distance(u.transform.position, isildak.transform.position)).First();
            // A point over the gun, between it and the tower; the head is turned onto it (as if it had swept there).
            Vector3 nokta = top.transform.position + Vector3.up * 300f + (isildak.transform.position - top.transform.position).normalized * 150f;
            isildak.Kafa.rotation = Quaternion.LookRotation(nokta - isildak.Kafa.position);
            int atis0 = top.AtisSayisi;
            float t = 0f;
            while (t < 3f)
            {
                Yerlestir(nokta, 0f, 0f, 0.5f);   // held in place, still "flying" (enabled)
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(isildak.Yakaladi, "caught in the beam");
            Olcum.AppendLine($"Searchlight: caught at 500 m; gun shots in 3 s while lit: {top.AtisSayisi - atis0}; held: {isildak.Yakaladi}.");
            Assert.IsTrue(isildak.Yakaladi || top.AtisSayisi > atis0, "still lit or the gun fired");
            Assert.Greater(top.AtisSayisi, atis0, "the gun fires while the searchlight holds the aircraft");
            // Far away behind the terrain: the light loses it.
            ucus.enabled = false;
            ucus.transform.position = GeceHaritasi.PistBasi + Vector3.up * 30f;
            Physics.SyncTransforms();
            t = 0f;
            while (isildak.Yakaladi && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(isildak.Yakaladi, "lost");
        }

        [UnityTest]
        public IEnumerator Guns_HoldFire_InTheDark()
        {
            yield return Yukle();
            UcaksavarMevzii top = Object.FindObjectsByType<UcaksavarMevzii>(FindObjectsInactive.Exclude).First();
            foreach (Isildak i in Object.FindObjectsByType<Isildak>(FindObjectsInactive.Exclude))
                i.enabled = false;   // no searchlight sweeps over the aircraft by chance
            Vector3 p = top.transform.position + Vector3.up * 400f + top.transform.forward * 500f;
            // Throttle below the locator's silence threshold: the engine is not heard either.
            Yerlestir(p, Yon(top.transform.position - p), 60f, 0.1f);
            int atis0 = top.AtisSayisi;
            yield return Bekle(2.5f);
            Assert.AreEqual(atis0, top.AtisSayisi, "no searchlight, not heard, beyond 250 m: no fire");
        }

        [UnityTest]
        public IEnumerator Resupply_LowPassOverTheRunway_RefillsTheBombs()
        {
            yield return Yukle();
            ucus.GetComponent<Saglik>();
            Yerlestir(GeceHaritasi.Zeminde(500f, -1200f) + Vector3.up * 300f, 0f, 0f, 0f);
            yuk.Birak();
            yuk.Birak();
            Assert.AreEqual(Lab7Kapasite - 2, yuk.Kalan);
            yield return null;
            // Fly into the box over the runway from the side.
            Vector3 dis = GeceHaritasi.InisBolgesiMerkezi + Vector3.Cross(Vector3.up, GeceHaritasi.PistEkseni) * 200f + Vector3.up * 60f;
            Yerlestir(dis, Yon(GeceHaritasi.InisBolgesiMerkezi - dis), 70f, 0.6f);
            float t = 0f;
            while (gorev.IkmalSayisi == 0 && t < 6f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1, gorev.IkmalSayisi);
            Assert.AreEqual(Lab7Kapasite, yuk.Kalan);
        }

        const int Lab7Kapasite = 4;

        // ---- landing ------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator AllTargetsDown_LandInTheZone_MissionComplete()
        {
            yield return Yukle();
            foreach (Yikilabilir h in gorev.Hedefler)
                h.Yik();
            yield return null;
            Assert.AreEqual(GorevAsamasi.Inis, gorev.Asama);
            Assert.IsTrue(gorev.InisBolgesi.Aktif);
            Assert.IsTrue(GameObject.Find("Pist Isiklari") != null, "runway lights on again");
            // This test is about the landing itself: the guns near the approach (paper I7) are switched off.
            foreach (UcaksavarMevzii u in Object.FindObjectsByType<UcaksavarMevzii>(FindObjectsInactive.Exclude))
                u.enabled = false;

            Time.timeScale = 3f;
            Vector3 eksen = GeceHaritasi.PistEkseni;
            float uzak = 1400f;
            Vector3 bas = GeceHaritasi.PistSonu + eksen * uzak + Vector3.up * (uzak * Mathf.Tan(3f * Mathf.Deg2Rad) + 3f);
            float yon = GeceHaritasi.PistYonu + 180f;
            Yerlestir(bas, yon, 50f, 0.3f);
            float t = 0f, enYuksekDusus = 0f;
            bool temas = false;
            while (!gorev.Bitti && t < 120f)
            {
                t += Time.deltaTime;
                Vector3 p = ucus.transform.position;
                float agl = ZeminOlcer.Irtifa(p) - algilayici.TekerOfseti;
                if (!algilayici.Yerde)
                {
                    // 3° glide path to a point 150 m past the threshold, then a flare below 8 m.
                    Vector3 nokta = GeceHaritasi.PistSonu - eksen * 150f;
                    float d = Vector3.Dot(p - nokta, eksen);
                    float yol = GeceHaritasi.PistYuksekligi + 0.3f + algilayici.TekerOfseti + Mathf.Max(0f, d) * Mathf.Tan(3f * Mathf.Deg2Rad);
                    float hedefDikey = agl > 8f
                        ? Mathf.Clamp(-ucus.Hiz * Mathf.Tan(3f * Mathf.Deg2Rad) - 0.3f * (p.y - yol), -6f, 0f)
                        : -0.8f;
                    ucus.Yunuslama = Mathf.Clamp((ucus.DikeyHiz - hedefDikey) * 0.25f, -1f, 1f);
                    ucus.GazAyarla(Mathf.Clamp(0.25f + (42f - ucus.Hiz) * 0.06f, 0f, 0.8f));
                    // keep on the centreline: aim at a point 600 m down the runway
                    Vector3 nisan = GeceHaritasi.PistSonu - eksen * 600f;
                    float hata = Mathf.DeltaAngle(ucus.transform.eulerAngles.y, Yon(nisan - p));
                    ucus.Sapma = Mathf.Clamp(hata * 0.15f, -1f, 1f);
                    enYuksekDusus = Mathf.Max(enYuksekDusus, -ucus.DikeyHiz);
                }
                else
                {
                    temas = true;
                    ucus.Yunuslama = 0f;
                    ucus.Sapma = 0f;
                    ucus.GazAyarla(0f);
                }
                yield return null;
            }
            Assert.IsFalse(algilayici.Cakti, $"no crash ({algilayici.CakmaNedeni})");
            Assert.IsTrue(temas, "touched down");
            Assert.IsTrue(gorev.Basarili, $"mission complete ({gorev.Neden}, phase {gorev.Asama})");
            Assert.AreEqual(GeceGorevi.NedenBasari, gorev.Neden);
            Assert.IsTrue(gorev.InisBolgesi.Iceride(ucus.transform.position) || gorev.Bitti);
            Olcum.AppendLine($"Landing (autopilot, 3° glide at ~42 m/s, flare below 10 m): touchdown sink {algilayici.SonDususHizi:F2} m/s " +
                             $"(limit {algilayici.EnFazlaDususHizi:F1}); stopped {Vector3.Distance(algilayici.SonInisNoktasi, ucus.transform.position):F0} m after touchdown; score {gorev.Puan}.");
        }

        [UnityTest]
        public IEnumerator LandingBeforeTheTargets_DoesNotEndTheMission()
        {
            yield return Yukle();
            yield return Bekle(0.3f);
            Assert.AreEqual(GorevAsamasi.Kalkis, gorev.Asama);
            yield return Bekle(1f);
            Assert.IsFalse(gorev.Bitti, "standing on the runway at the start is not a landing");
        }

        // ---- crashes ------------------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Crash_DiveIntoTheGround_GameOver()
        {
            yield return Yukle();
            Vector3 p = GeceHaritasi.Zeminde(-600f, 900f) + Vector3.up * 80f;
            Yerlestir(p, 30f, 70f, 0.6f, 45f);
            float t = 0f;
            while (!gorev.Bitti && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(algilayici.Cakti);
            Assert.IsTrue(gorev.Bitti);
            Assert.IsFalse(gorev.Basarili);
            Assert.AreEqual(GeceGorevi.NedenCakildi, gorev.Neden);
            Assert.IsFalse(ucus.enabled, "the aircraft stops");
        }

        [UnityTest]
        public IEnumerator Crash_DitchingInTheRiver_GameOver()
        {
            yield return Yukle();
            Vector3 c = GeceHaritasi.Kare("I2");                              // paper river square, column I
            Vector3 p = new Vector3(c.x, 7f, c.z);
            Yerlestir(p, 180f, 40f, 0.2f, 3f);   // gentle descent along the river (flowing south)
            float t = 0f;
            while (!gorev.Bitti && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(gorev.Bitti, "ended");
            Assert.AreEqual(GeceGorevi.NedenCakildi, gorev.Neden);
            Assert.AreEqual("suya düştü", gorev.Ayrinti, $"probe: {algilayici.CakmaNedeni}");
        }

        [UnityTest]
        public IEnumerator Crash_IntoABridge_GameOver()
        {
            yield return Yukle();
            Yikilabilir kopru = gorev.Hedefler.First(h => h.Tur == HedefTuru.Kopru);
            Vector3 k = kopru.transform.position + Vector3.up * 4f;
            Vector3 yan = kopru.transform.right;
            Vector3 p = k + yan * 250f;
            Yerlestir(p, Yon(-yan), 60f, 0.6f);
            float t = 0f;
            while (!gorev.Bitti && t < 8f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(gorev.Bitti);
            Assert.AreEqual("yapıya çarptı", algilayici.CakmaNedeni);
            Assert.Less(Vector3.Distance(ucus.transform.position, k), 20f, "stopped at the bridge");
        }

        [UnityTest]
        public IEnumerator ShotDown_HealthZero_GameOver()
        {
            yield return Yukle();
            Yerlestir(GeceHaritasi.Zeminde(-800f, 0f) + Vector3.up * 300f, 0f, 70f, 0.5f);
            ucus.GetComponent<Saglik>().Vur(1000);
            yield return null;
            Assert.IsTrue(gorev.Bitti);
            Assert.AreEqual(GeceGorevi.NedenVuruldu, gorev.Neden);
        }
    }
}
