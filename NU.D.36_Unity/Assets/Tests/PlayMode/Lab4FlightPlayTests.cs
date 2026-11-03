using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// The flight model in the real Rota04Hud scene, driven through the public input properties of UcusKontrol (the same values the
    /// Input System callbacks set), frame by frame in Play Mode. Checks that the HUD reads the real movement. Not a manual flight test.
    /// </summary>
    public class Lab4FlightPlayTests
    {
        UcusKontrol ucus;
        HudGosterge hiz, irtifa;
        HudGazGostergesi gaz;
        HudPusula pusula;
        TakipKamerasi kamera;

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Rota04Hud", LoadSceneMode.Single);
            yield return null;
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            HudGosterge[] dials = Object.FindObjectsByType<HudGosterge>(FindObjectsSortMode.None);
            hiz = dials.Single(d => d.Kaynak == HudKaynak.Hiz);
            irtifa = dials.Single(d => d.Kaynak == HudKaynak.Irtifa);
            gaz = Object.FindAnyObjectByType<HudGazGostergesi>();
            pusula = Object.FindAnyObjectByType<HudPusula>();
            kamera = Object.FindAnyObjectByType<TakipKamerasi>();
            Object.FindAnyObjectByType<YenidenBasla>().SahneYukle = _ => { };
            Assert.IsNotNull(ucus);
        }

        static float Z(RectTransform r) => Mathf.DeltaAngle(0f, r.localEulerAngles.z);

        [UnityTest]
        public IEnumerator ThrottleUp_RaisesTheSpeedGradually_AndTheHudReadsTheRealMovement()
        {
            yield return Yukle();
            ucus.GazAyarla(0.5f);
            yield return null;
            Assert.AreEqual(25f, ucus.Hiz, 0.5f, "starts at the half-throttle speed");
            ucus.GazGirdisi = 1f;
            Vector3 p = ucus.transform.position;
            float previous = ucus.Hiz, elapsed = 0f, firstSecond = 0f;
            int frames = 0;
            while (elapsed < 1.5f)
            {
                yield return null;
                float dt = Time.deltaTime;
                elapsed += dt;
                Assert.GreaterOrEqual(ucus.Hiz, previous - 1e-4f, "speed never dips while accelerating");
                Assert.LessOrEqual(ucus.Hiz - previous, 6f * Mathf.Min(dt, 0.1f) + 1e-3f, "never faster than 6 m/s²");
                if (dt <= 0.1f)
                {
                    float moved = (ucus.transform.position - p).magnitude;
                    Assert.AreEqual(ucus.Hiz * dt, moved, ucus.Hiz * dt * 0.01f + 1e-3f, "the distance this frame equals speed x time");
                }
                // the HUD updates in LateUpdate, after this coroutine step: update it by hand and compare with this frame's flight data
                hiz.Guncelle(); irtifa.Guncelle(); gaz.Guncelle();
                Assert.AreEqual(ucus.Hiz, hiz.Deger, 1e-3f);
                Assert.AreEqual(HudHesap.IbreZ(ucus.Hiz, 50f), Z(hiz.Ibre), 0.05f);
                Assert.AreEqual(ucus.transform.position.y, irtifa.Deger, 1e-3f);
                Assert.AreEqual(ucus.GazOrani, Mathf.InverseLerp(gaz.AltY, gaz.UstY, gaz.Kol.anchoredPosition.y), 1e-3f);
                if (elapsed <= 1f)
                    firstSecond = ucus.Hiz;
                previous = ucus.Hiz;
                p = ucus.transform.position;
                frames++;
            }
            Assert.Less(firstSecond, 34f, "after about one second the speed is still far from the top speed (25 + 6)");
            Assert.Greater(firstSecond, 28f);
            Assert.Greater(frames, 5);
        }

        [UnityTest]
        public IEnumerator BankingTurnsTheAircraft_HoldsTheAltitude_AndLevelsOutOnRelease()
        {
            yield return Yukle();
            ucus.GazAyarla(0.5f);
            yield return null;
            float y0 = ucus.transform.position.y;
            float h0 = ucus.transform.eulerAngles.y;
            ucus.Yatis = 1f;   // D held
            float t = 0f;
            while (t < 1.2f) { yield return null; t += Time.deltaTime; }
            Assert.Greater(ucus.YatisAcisi, 40f, "right wing down");
            Assert.LessOrEqual(ucus.YatisAcisi, 65.5f, "bank limit");
            Assert.Greater(Mathf.DeltaAngle(h0, ucus.transform.eulerAngles.y), 8f, "the bank turns the aircraft to the right");
            pusula.Guncelle();
            Assert.AreEqual(HudHesap.Yon(ucus.transform.eulerAngles.y), pusula.Yon, 0.01f, "compass = aircraft yaw");
            ucus.Yatis = 0f;
            t = 0f;
            while (t < 3f) { yield return null; t += Time.deltaTime; }
            Assert.Less(Mathf.Abs(ucus.YatisAcisi), 5f, "level three seconds after the release");
            Assert.AreEqual(y0, ucus.transform.position.y, 2f, "the turn did not climb or sink");
            irtifa.Guncelle();
            Assert.AreEqual(ucus.transform.position.y, irtifa.Deger, 1e-3f);
        }

        [UnityTest]
        public IEnumerator PressingAndReleasingKeys_NeverCausesAJump()
        {
            yield return Yukle();
            ucus.GazAyarla(1f);
            yield return null;
            Quaternion q = ucus.transform.rotation;
            Vector3 p = ucus.transform.position;
            float t = 0f, worstRate = 0f, worstStep = 0f;
            bool released = false;
            while (t < 3f)
            {
                if (t > 0.3f && t < 1.3f) { ucus.Yunuslama = -1f; ucus.Yatis = 1f; }
                else if (!released && t >= 1.3f) { ucus.Yunuslama = 0f; ucus.Yatis = 0f; released = true; }
                yield return null;
                float dt = Time.deltaTime;
                t += dt;
                if (dt > 0.1f)
                {
                    q = ucus.transform.rotation;
                    p = ucus.transform.position;
                    continue;   // a hitch (scene load): not part of the measurement
                }
                worstRate = Mathf.Max(worstRate, Quaternion.Angle(q, ucus.transform.rotation) / dt);
                worstStep = Mathf.Max(worstStep, (ucus.transform.position - p).magnitude / dt);
                q = ucus.transform.rotation;
                p = ucus.transform.position;
            }
            Assert.Less(worstRate, 170f, "angular rate (degrees/s) stays within pitch + roll + turn");
            Assert.LessOrEqual(worstStep, ucus.EnCokHiz * 1.01f, "never faster than the top speed along the path");
        }

        [UnityTest]
        public IEnumerator ChaseCamera_IsHorizonLocked_InTheHudScene()
        {
            yield return Yukle();
            Assert.IsTrue(kamera.UfukKilitli);
            ucus.transform.rotation = Quaternion.Euler(0f, 70f, 0f);
            kamera.YonuGuncelle(float.PositiveInfinity);
            Vector3 level = kamera.IstenenKonum - ucus.transform.position;
            ucus.transform.rotation = Quaternion.Euler(0f, 70f, -75f);
            kamera.YonuGuncelle(float.PositiveInfinity);
            Assert.AreEqual(0f, (kamera.IstenenKonum - ucus.transform.position - level).magnitude, 1e-3f, "rolling does not swing the camera");
        }
    }
}
