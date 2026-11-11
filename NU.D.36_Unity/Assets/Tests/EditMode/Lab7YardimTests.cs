using System.Linq;
using NUnit.Framework;
using NU36.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NU36.Tests
{
    /// <summary>
    /// Assists of the night mission (user request 2026-11-10): the easy flight law of UcusKontrol stepped directly (no scene),
    /// the air brake and the wheel brake, the guided bomb's law, and the wiring in Rota07 (assistant, targeting, GPS).
    /// The Lab 7 stick law stays the default of UcusKontrol (Lab7UcusFizigiTests).
    /// </summary>
    public class Lab7YardimTests
    {
        GameObject go;
        UcusKontrol u;
        Scene scene;

        [SetUp]
        public void Kur()
        {
            go = new GameObject("Test Ucak");
            u = go.AddComponent<UcusKontrol>();
            u.Aerodinamik = true;
            u.KolayUcus = true;
            u.YatisDengeleme = 0f;
            go.transform.SetPositionAndRotation(new Vector3(0f, 1000f, 0f), Quaternion.identity);
        }

        [TearDown]
        public void Sil()
        {
            if (go != null) Object.DestroyImmediate(go);
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        void Uc(float saniye, System.Action her = null, float dt = 1f / 60f)
        {
            for (float t = 0f; t < saniye; t += dt)
            {
                her?.Invoke();
                u.Adim(dt);
            }
        }

        // ---- law -----------------------------------------------------------------------------------------------------------

        [Test]
        public void Law_BankLimit_ShrinksAtLowSpeed_ClimbBecomesDescentBelowTheSafeSpeed()
        {
            var y = new UcusYardimi();
            var a = new UcakAerodinamigi();
            float vs = a.TutunmaHizi();
            Assert.AreEqual(60f, y.YatisSiniri(90f, a), 1e-3f, "full bank when fast");
            Assert.Less(y.YatisSiniri(45f, a), 45f, "the wing limits the bank when slow");
            Assert.AreEqual(10f, y.YatisSiniri(90f, a, 10f), 1e-3f, "external limit");
            Assert.AreEqual(y.enFazlaTirmanma, y.EgimKomutu(1f, 90f, vs), 1e-3f);
            Assert.AreEqual(-y.enFazlaDalis, y.EgimKomutu(-1f, 90f, vs), 1e-3f);
            Assert.AreEqual(0f, y.HizSiniri(y.guvenliHizOrani * vs, vs), 1e-3f, "no climb at the safe speed");
            Assert.Less(y.HizSiniri(vs * 1.05f, vs), -3f, "descend when slower");
            Assert.AreEqual(1f, y.YukKomutu(0f, 0f, 0f, 70f), 1e-4f, "level: 1 g");
            Assert.AreEqual(2f, y.YukKomutu(0f, 0f, 60f, 70f), 1e-3f, "level turn at 60°: 2 g");
        }

        // ---- easy flight ---------------------------------------------------------------------------------------------------

        [Test]
        public void EasyFlight_RollKeyBanksToTheLimit_ReleasedTheWingsLevel_HeightHeld()
        {
            u.HizAyarla(75f);
            u.GazAyarla(0.6f);
            float y0 = go.transform.position.y;
            u.Yatis = 1f;
            Uc(3f);
            float yatis = u.YatisAcisi;
            Assert.That(yatis, Is.InRange(50f, 62f), "bank held at the commanded limit, no roll-over");
            u.Yatis = 0f;
            Uc(3f);
            Assert.Less(Mathf.Abs(u.YatisAcisi), 2f, "wings level after releasing the key");
            Assert.Less(Mathf.Abs(go.transform.position.y - y0), 25f, "height held through the turn");
            Uc(20f);
            Assert.Less(Mathf.Abs(go.transform.position.y - y0), 30f, "hands off: level flight");
            Assert.Less(Mathf.Abs(u.Egim), 1f);
        }

        [Test]
        public void EasyFlight_PitchKeysCommandClimbAndDive_ReleasedLevelFlight()
        {
            u.HizAyarla(85f);
            u.GazAyarla(1f);
            u.Yunuslama = -1f;   // S: nose up
            Uc(4f);
            Assert.That(u.Egim, Is.InRange(12f, 21f), "climb angle commanded by S");
            u.Yunuslama = 0f;
            Uc(5f);
            Assert.Less(Mathf.Abs(u.Egim), 2f, "released: level");
            u.Yunuslama = 1f;    // W: nose down
            Uc(3f);
            Assert.That(u.Egim, Is.InRange(-46f, -30f), "dive commanded by W");
            u.Yunuslama = 0f;
            Uc(6f);
            Assert.Less(Mathf.Abs(u.Egim), 3f, "released: pulls out to level");
            Assert.IsFalse(u.TutunmaKaybi);
        }

        [Test]
        public void EasyFlight_FullBackStick_SlowAndIdle_NeverStalls_TheSpeedRecovers()
        {
            u.HizAyarla(45f);
            u.GazAyarla(0f);
            u.Yunuslama = -1f;
            bool tutunma = false;
            float enAz = float.MaxValue;
            Uc(20f, () => { tutunma |= u.TutunmaKaybi; enAz = Mathf.Min(enAz, u.Hiz); });
            Assert.IsFalse(tutunma, "no stall");
            Assert.Greater(enAz, u.Aero.TutunmaHizi() * 1.1f, $"lowest speed {enAz:F1} m/s");
            Assert.Greater(u.Hiz, u.Aero.TutunmaHizi() * 1.2f, "the speed protection holds the speed (engine help, no climb)");
            Assert.IsTrue(u.HizKorumasi || u.Egim < 0f, "the engine helps or the aircraft glides down");
        }

        [Test]
        public void EasyFlight_UpsideDown_RollsUpright()
        {
            go.transform.rotation = Quaternion.Euler(0f, 0f, 175f);
            u.HizAyarla(80f);
            u.GazAyarla(0.6f);
            Uc(4f);
            Assert.Less(Mathf.Abs(u.YatisAcisi), 5f, $"bank {u.YatisAcisi:F0}°");
        }

        [Test]
        public void EasyFlight_ExternalFloor_LiftsTheCommandedPath()
        {
            u.HizAyarla(80f);
            u.GazAyarla(0.7f);
            u.Yunuslama = 1f;          // dive
            u.EgimAltSiniri = 5f;      // terrain floor: climb 5°
            Uc(5f);
            Assert.AreEqual(5f, u.HedefEgim, 1e-3f);
            Assert.That(u.Egim, Is.InRange(3f, 7f), "the floor wins over the dive key");
        }

        [Test]
        public void EasyFlight_OnTheGround_RotatesByItself_WithTheThrottleOpen()
        {
            go.transform.position = new Vector3(0f, 10f, 0f);
            u.YerTemasi = true;
            u.HizAyarla(0f);
            u.GazAyarla(1f);
            Uc(25f);
            float burun = Mathf.Asin(go.transform.forward.y) * Mathf.Rad2Deg;
            Assert.Greater(burun, 5f, "nose up at take-off speed without a key");
            Assert.Greater(u.DikeyHiz, 0f, "lifting off");
            Assert.Greater(u.Hiz, u.Yardim.KalkisHizi(u.Aero) - 1f);
        }

        // ---- brakes --------------------------------------------------------------------------------------------------------

        [Test]
        public void AirBrake_SlowsTheAircraft()
        {
            u.HizAyarla(100f);
            u.GazAyarla(0f);
            Uc(10f);
            float frensiz = u.Hiz;
            go.transform.SetPositionAndRotation(new Vector3(0f, 1000f, 0f), Quaternion.identity);
            u.HizAyarla(100f);
            u.Fren = true;
            Uc(10f);
            Assert.Less(u.Hiz, frensiz - 12f, $"air brake: {u.Hiz:F1} m/s vs {frensiz:F1} m/s after 10 s");
            Assert.IsFalse(u.TutunmaKaybi);
            Uc(20f);
            float vs = u.Aero.TutunmaHizi();
            Assert.IsFalse(u.HavaFreniAcik, "retracted by itself near the stall speed");
            Assert.Greater(u.Hiz, 1.2f * vs, "brake held for 30 s: the speed stays above the safe speed");
        }

        [Test]
        public void SpeedProtection_EngineHelpsBelowTheSafeSpeed_OnlyInEasyFlight()
        {
            var y = new UcusYardimi();
            float vs = new UcakAerodinamigi().TutunmaHizi();
            Assert.AreEqual(0.2f, y.EtkinGaz(0.2f, 1.5f * vs, vs), 1e-4f, "fast: the lever");
            Assert.AreEqual(1f, y.EtkinGaz(0f, 1.15f * vs, vs), 1e-4f, "slow: full power");
            Assert.IsTrue(y.FrenCalisir(1.5f * vs, vs));
            Assert.IsFalse(y.FrenCalisir(1.3f * vs, vs));
            u.KolayUcus = false;
            u.HizAyarla(40f);
            u.GazAyarla(0f);
            Uc(1f);
            Assert.IsFalse(u.HizKorumasi, "the Lab 7 model has no speed protection");
        }

        [Test]
        public void WheelBrake_WorksWithTheThrottleOpen()
        {
            go.transform.position = new Vector3(0f, 10f, 0f);
            u.YerTemasi = true;
            u.HizAyarla(40f);
            u.GazAyarla(0.2f);
            Vector3 bas = go.transform.position;
            Uc(10f);
            float frensiz = u.Hiz;
            go.transform.SetPositionAndRotation(new Vector3(0f, 10f, 0f), Quaternion.identity);
            u.HizAyarla(40f);
            u.Fren = true;
            bas = go.transform.position;
            Uc(20f);
            Assert.Greater(frensiz, 25f, "20 % throttle keeps rolling");
            Assert.Less(u.Hiz, 0.5f, "stopped with the brake");
            Assert.Less(Vector3.Distance(bas, go.transform.position), 300f);
        }

        // ---- scene ---------------------------------------------------------------------------------------------------------

        [Test]
        public void Rota07_HasTheAssists_EasyFlight_Targeting_Gps()
        {
            Object.DestroyImmediate(go);
            scene = EditorSceneManager.OpenScene(Lab7SceneBuilder.ScenePath, OpenSceneMode.Additive);
            GameObject[] roots = scene.GetRootGameObjects();
            UcusKontrol ucus = roots.SelectMany(r => r.GetComponentsInChildren<UcusKontrol>(true)).Single();
            Assert.IsTrue(ucus.KolayUcus, "easy flight on");
            Assert.Greater(ucus.Aero.havaFreni, 0f, "air brake");
            var yardimci = ucus.GetComponent<UcusYardimcisi>();
            Assert.IsNotNull(yardimci);
            Assert.AreEqual(GeceHaritasi.PistBasi, yardimci.PistBasi);
            Assert.AreEqual(GeceHaritasi.PistSonu, yardimci.PistSonu);
            Assert.IsTrue(yardimci.Havaalaninda(GeceHaritasi.InisBolgesiMerkezi), "landing zone in the runway box");
            Assert.IsTrue(yardimci.Havaalaninda(GeceHaritasi.PistSonu + GeceHaritasi.PistEkseni * 2000f), "approach in the box");
            Assert.IsFalse(yardimci.Havaalaninda(GeceHaritasi.KopruMerkezi), "the bridge is outside");
            var nisan = ucus.GetComponent<NisanSistemi>();
            Assert.IsNotNull(nisan);
            Assert.IsTrue(nisan.OtomatikAtis);
            Assert.IsNotNull(nisan.Gorev);
            GpsHaritasi gps = roots.SelectMany(r => r.GetComponentsInChildren<GpsHaritasi>(true)).Single();
            Assert.AreEqual("Gece HUD", gps.transform.parent.name);
            Assert.AreEqual(GorevSonucu(roots), nisan.Gorev);
        }

        static GeceGorevi GorevSonucu(GameObject[] roots) => roots.SelectMany(r => r.GetComponentsInChildren<GeceGorevi>(true)).Single();

        [Test]
        public void GpsWindow_MapsTheTextureAndTheWorld()
        {
            Rect r = GpsHaritasi.DokuAlani(Vector3.zero, 4000f, 8000f);
            Assert.AreEqual(new Rect(0.25f, 0.25f, 0.5f, 0.5f), r);
            Vector2 k = GpsHaritasi.HaritaKonumu(new Vector3(100f, 50f, -200f), new Vector3(-100f, 0f, 0f), 2000f, 400f);
            Assert.AreEqual(new Vector2(40f, -40f), k, "east = right, north = up, 0.2 px per m");
        }
    }
}
