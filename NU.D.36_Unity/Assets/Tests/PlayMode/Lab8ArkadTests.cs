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
    /// Arcade flight in Bölüm 4 (2026-11-11, user: "uçağın kontrolleri gerçekten zor … daha esnek ve rahat"), driven like a
    /// keyboard: an axis held at ±1 for a time, then released. Measures the response the player feels: level when hands off,
    /// turn rate, stop of the turn and level wings after release, climb and return to level, the ground and canyon-wall
    /// protection, the map edge, the K mode cycle; plus the engine sound, the battle sounds and the train marker.
    /// Measurements → Logs/lab8/arkad_olcumu.txt.
    /// </summary>
    public class Lab8ArkadTests
    {
        UcusKontrol ucus;
        InisAlgilayici algilayici;
        static readonly StringBuilder Olcum = new StringBuilder();

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Bolum4_KursunDaglari", LoadSceneMode.Single);
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            algilayici = ucus.GetComponent<InisAlgilayici>();
            foreach (AtesBolgesi b in Object.FindObjectsByType<AtesBolgesi>(FindObjectsSortMode.None)) b.enabled = false;
            Object.FindAnyObjectByType<KursunDaglariGorevi>().SahneYukle = _ => { };
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
            File.WriteAllText(Path.Combine(dir, "arkad_olcumu.txt"), $"Arcade flight, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        void Koy(Vector3 p, float yon)
        {
            ucus.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yon, 0f));
            ucus.HizAyarla(70f);
            ucus.Yunuslama = ucus.Yatis = ucus.Sapma = 0f;
            Physics.SyncTransforms();
            Lab7PlayTests.CarpismaSifirla(ucus);
        }

        static float Yon(Transform t) => Mathf.Atan2(t.forward.x, t.forward.z) * Mathf.Rad2Deg;
        static float Egim(Transform t) => Mathf.Asin(Mathf.Clamp(t.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        IEnumerator Bekle(float s) { float b = Time.time + s; while (Time.time < b) yield return null; }

        /// <summary>Over the southern plain, east of the base, 400 m up, heading east (open air, flat ground).</summary>
        Vector3 Ova => new Vector3(-900f, ZeminOlcer.Yukseklik(-900f, -1900f) + 400f, -1900f);

        [UnityTest]
        public IEnumerator Arcade_IsTheDefault_HandsOffItFliesLevel()
        {
            yield return Yukle();
            Assert.IsTrue(ucus.Arkad, "arcade flight at the start");
            Assert.AreEqual("ARCADE", ucus.ModAdi);
            Koy(Ova, 90f);
            float y0 = ucus.transform.position.y, enCok = 0f;
            float bitis = Time.time + 8f;
            while (Time.time < bitis)
            {
                yield return null;
                enCok = Mathf.Max(enCok, Mathf.Abs(ucus.transform.position.y - y0));
            }
            Olcum.AppendLine($"hands off 8 s: height change at most {enCok:F1} m, speed {ucus.Hiz * 3.6f:F0} km/h, bank {ucus.YatisAcisi:F1}°");
            Assert.Less(enCok, 5f, "level");
            Assert.Less(Mathf.Abs(ucus.YatisAcisi), 2f, "wings level");
            Assert.IsFalse(algilayici.Cakti);
        }

        [UnityTest]
        public IEnumerator Turn_DHeld_TurnsAtOnce_ReleaseStopsTheTurn_AndLevelsTheWings()
        {
            yield return Yukle();
            Koy(Ova, 90f);
            yield return Bekle(0.5f);
            float y0 = Yon(ucus.transform);
            ucus.Yatis = 1f;
            yield return Bekle(0.3f);
            float ilk = Mathf.DeltaAngle(y0, Yon(ucus.transform));
            yield return Bekle(1.2f);
            float donus = Mathf.DeltaAngle(y0, Yon(ucus.transform));
            float yatis = ucus.YatisAcisi;
            ucus.Yatis = 0f;
            float y1 = Yon(ucus.transform);
            yield return Bekle(0.6f);
            float y2 = Yon(ucus.transform);
            yield return Bekle(0.4f);
            float sonrakiHiz = Mathf.Abs(Mathf.DeltaAngle(y2, Yon(ucus.transform))) / 0.4f;
            yield return Bekle(1.0f);
            Olcum.AppendLine($"D held 1.5 s: turned {donus:F0}° (first 0.3 s: {ilk:F0}°), bank {yatis:F0}°; released: overrun {Mathf.DeltaAngle(y1, y2):F0}° in 0.6 s, then {sonrakiHiz:F1}°/s; bank 2 s after release {ucus.YatisAcisi:F1}°");
            Assert.Greater(ilk, 3f, "the turn starts at once");
            Assert.That(donus, Is.InRange(60f, 100f), "about 60°/s");
            Assert.That(yatis, Is.InRange(20f, 66f), "banks into the turn by itself (right wing down)");
            Assert.Less(sonrakiHiz, 3f, "released: the turn stops");
            Assert.Less(Mathf.Abs(ucus.YatisAcisi), 4f, "released: wings level");
        }

        [UnityTest]
        public IEnumerator Climb_SHeld_ClimbsAtOnce_ReleaseReturnsToLevel()
        {
            yield return Yukle();
            Koy(Ova, 90f);
            yield return Bekle(0.3f);
            float y0 = ucus.transform.position.y;
            ucus.Yunuslama = -1f;   // S (W = nose down, as in all labs)
            yield return Bekle(1f);
            float egim = Egim(ucus.transform);
            ucus.Yunuslama = 0f;
            yield return Bekle(2.5f);
            float sonra = Egim(ucus.transform);
            Olcum.AppendLine($"S held 1 s: climb angle {egim:F0}°; 2.5 s after release {sonra:F1}°; climbed {ucus.transform.position.y - y0:F0} m");
            Assert.That(egim, Is.InRange(20f, 41f), "climbs at once (40°/s)");
            Assert.Less(Mathf.Abs(sonra), 5f, "levels by itself");
            Assert.Greater(ucus.transform.position.y - y0, 30f);
        }

        [UnityTest]
        public IEnumerator Protection_WHeldIntoTheGround_AndIntoACanyonWall_NoCrash()
        {
            yield return Yukle();
            Koy(Ova, 90f);
            ucus.Yunuslama = 1f;   // W held: dive
            float enAz = float.MaxValue;
            float bitis = Time.time + 8f;
            while (Time.time < bitis)
            {
                yield return null;
                enAz = Mathf.Min(enAz, ZeminOlcer.Irtifa(ucus.transform.position));
            }
            ucus.Yunuslama = 0f;
            Assert.IsFalse(algilayici.Cakti, "diving at the ground does not crash");
            Assert.Greater(enAz, 14f, "the floor holds");

            // Dar Vadi: 60 m over the bed, heading straight into the canyon wall, hands off.
            RayEgrisi e = H.Egri;
            float s = (H.Bolgeler[0].Bas + H.Bolgeler[0].Son) * 0.5f * e.Uzunluk;
            Vector3 p = e.Nokta(s) + Vector3.up * 60f;
            Vector3 sag = e.Sag(s);
            Koy(p, Mathf.Atan2(sag.x, sag.z) * Mathf.Rad2Deg);
            float enAzDuvar = float.MaxValue, yukseldi = 0f;
            bitis = Time.time + 8f;
            while (Time.time < bitis)
            {
                yield return null;
                enAzDuvar = Mathf.Min(enAzDuvar, ZeminOlcer.Irtifa(ucus.transform.position));
                yukseldi = Mathf.Max(yukseldi, ucus.transform.position.y - p.y);
            }
            Olcum.AppendLine($"W held 8 s: lowest {enAz:F0} m above ground; into the Dar Vadi wall: lowest {enAzDuvar:F0} m, climbed {yukseldi:F0} m over it");
            Assert.IsFalse(algilayici.Cakti, "flying at the canyon wall does not crash");
            Assert.Greater(enAzDuvar, 10f);
            Assert.Greater(yukseldi, 200f, "it climbed over the 330 m wall by itself");
        }

        [UnityTest]
        public IEnumerator MapEdge_TurnsBack_AndKCyclesTheModes()
        {
            yield return Yukle();
            Vector3 p = new Vector3(0f, 0f, -2850f);
            p.y = ZeminOlcer.Yukseklik(p.x, p.z) + 300f;
            Koy(p, 180f);   // heading out (south)
            float bitis = Time.time + 8f;
            while (Time.time < bitis && Vector3.Angle(ucus.transform.forward, -new Vector3(ucus.transform.position.x, 0f, ucus.transform.position.z)) > 80f)
                yield return null;
            Assert.Less(Vector3.Angle(new Vector3(ucus.transform.forward.x, 0f, ucus.transform.forward.z), -new Vector3(ucus.transform.position.x, 0f, ucus.transform.position.z)), 80f, "turned back to the map");

            Koy(Ova, 90f);
            ucus.SonrakiMod();
            Assert.AreEqual("KOLAY", ucus.ModAdi);
            Assert.IsTrue(ucus.Aerodinamik && ucus.KolayUcus);
            yield return Bekle(2f);
            Assert.Greater(ucus.Hiz, 30f, "keeps flying after the switch");
            ucus.SonrakiMod();
            Assert.AreEqual("GERÇEKÇİ", ucus.ModAdi);
            ucus.SonrakiMod();
            Assert.AreEqual("ARCADE", ucus.ModAdi);
            Assert.IsFalse(ucus.Aerodinamik);
            yield return Bekle(1f);
            Assert.Greater(ucus.Hiz, 35f);
        }

        [UnityTest]
        public IEnumerator Sound_EngineFollowsTheThrottle_ShotsAreHeard_TrainMarkedEvenBehind()
        {
            yield return Yukle();
            var motor = ucus.GetComponent<MotorSesi>();
            Assert.IsNotNull(motor, "engine sound");
            Assert.IsTrue(motor.Kaynak.isPlaying, "the engine source plays");
            ucus.GazAyarla(0f);
            yield return null;
            float alcak = motor.Frekans;
            ucus.GazAyarla(1f);
            yield return Bekle(0.5f);
            Assert.Greater(motor.Frekans, alcak + 10f, "the pitch rises with the throttle");
            Assert.Greater(motor.Ses, 0f);

            var ses = Object.FindAnyObjectByType<SavasSesleri>();
            int once = ses.CalinanSes;
            var mk = ucus.GetComponent<Makineli>();
            mk.Tetik = true;
            yield return Bekle(0.3f);
            mk.Tetik = false;
            Assert.Greater(ses.CalinanSes, once, "machine-gun shots are heard");

            // The train behind the aircraft: still marked at the screen edge.
            var tren = Object.FindAnyObjectByType<Tren>();
            Transform lok = tren.Araclar[0].transform;
            Koy(lok.position + Vector3.up * 200f + lok.forward * 800f, Mathf.Atan2(lok.forward.x, lok.forward.z) * Mathf.Rad2Deg);
            yield return null;
            yield return null;
            var hud = Object.FindAnyObjectByType<Bolum4Hud>();
            Olcum.AppendLine($"engine {alcak:F0} → {motor.Frekans:F0} Hz; shots heard {ses.CalinanSes - once}; train marker behind: '{hud.TrenIsaretiYazisi.Replace("\n", " ")}'");
            Assert.IsTrue(hud.TrenIsaretiGorunuyor, "the train is always marked");
            StringAssert.Contains("TREN", hud.TrenIsaretiYazisi);
            string y = hud.TrenIsaretiYazisi;
            Assert.IsTrue(y.Contains("vv") || y.Contains("<<") || y.Contains(">>") || y.Contains("^^"), "behind: an arrow at the screen edge");
        }
    }
}
