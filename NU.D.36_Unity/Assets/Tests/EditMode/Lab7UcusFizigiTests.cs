using NUnit.Framework;
using UnityEngine;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 7: the aerodynamic flight model of UcusKontrol, stepped directly (no scene): formulas, trimmed flight, turns, pull-up,
    /// stall, dive, ground roll and brakes. The kinematic model of Labs 3–6 is covered by the earlier tests and stays the default.
    /// </summary>
    public class Lab7UcusFizigiTests
    {
        GameObject go;
        UcusKontrol u;

        [SetUp]
        public void Kur()
        {
            go = new GameObject("Test Ucak");
            u = go.AddComponent<UcusKontrol>();
            u.Aerodinamik = true;
            u.YatisDengeleme = 0f;
            go.transform.SetPositionAndRotation(new Vector3(0f, 1000f, 0f), Quaternion.identity);
        }

        [TearDown]
        public void Sil() => Object.DestroyImmediate(go);

        void Uc(float saniye, float dt = 1f / 60f)
        {
            for (float t = 0f; t < saniye; t += dt)
                u.Adim(dt);
        }

        static UcakAerodinamigi A => new UcakAerodinamigi();

        // ---- formulas -------------------------------------------------------------------------------------------------

        [Test]
        public void Formulas_StallSpeed_Climb_TopSpeed_AreHurricaneClass()
        {
            var a = A;
            Assert.That(a.TutunmaHizi(), Is.InRange(33f, 37f), "stall speed (m/s) at C_Lmax");
            Assert.That(a.TutunmaHizi(2f), Is.EqualTo(a.TutunmaHizi() * Mathf.Sqrt(2f)).Within(0.01f), "stall speed grows with √n");
            Assert.That(a.TirmanmaHizi(70f, 1f), Is.InRange(12f, 17f), "climb rate at full throttle, 70 m/s");
            Assert.That(a.EnYuksekHiz(1f), Is.InRange(105f, 125f), "top level speed at full throttle");
            Assert.Less(a.EnYuksekHiz(0.4f), a.EnYuksekHiz(1f));
            Assert.That(a.Itki(1f, 0f), Is.EqualTo(a.statikItki), "static thrust limit");
            Assert.That(a.Itki(1f, 100f), Is.EqualTo(a.motorGucu / 100f).Within(0.01f), "constant power: T = P / v");
        }

        [Test]
        public void LiftCurve_LinearToTheStall_ThenFalls()
        {
            var a = A;
            float s = a.TutunmaAcisiRad;
            Assert.That(a.KaldirmaKatsayisi(0f), Is.EqualTo(a.sifirKaldirma));
            Assert.That(a.KaldirmaKatsayisi(0.1f) - a.KaldirmaKatsayisi(0f), Is.EqualTo(a.kaldirmaEgimi * 0.1f).Within(1e-4f));
            Assert.That(a.KaldirmaKatsayisi(s), Is.EqualTo(a.EnBuyukKaldirma).Within(1e-4f));
            Assert.Less(a.KaldirmaKatsayisi(s + 0.15f), a.EnBuyukKaldirma * 0.7f, "beyond the stall the lift falls away");
            Assert.Greater(a.SuruklemeKatsayisi(1f, s + 0.2f), a.SuruklemeKatsayisi(1f, s), "and the drag rises");
            Assert.Less(a.KaldirmaKatsayisi(-s), 0f, "negative angle: negative lift");
        }

        [Test]
        public void KinematicModel_IsStillTheDefault_VelocityAlongTheNose()
        {
            var g = new GameObject("Eski");
            var k = g.AddComponent<UcusKontrol>();
            try
            {
                Assert.IsFalse(k.Aerodinamik);
                k.GazAyarla(0.5f);
                Assert.That(k.HizVektoru, Is.EqualTo(g.transform.forward * k.Hiz));
                Assert.That(k.Hiz, Is.EqualTo(k.HizHesapla(0.5f)));
            }
            finally { Object.DestroyImmediate(g); }
        }

        // ---- flight ----------------------------------------------------------------------------------------------------

        [Test]
        public void HandsOff_TrimmedFlight_HoldsHeight()
        {
            u.HizAyarla(70f);
            u.GazAyarla(0.45f);
            float h0 = go.transform.position.y;
            Uc(20f);
            Assert.That(go.transform.position.y - h0, Is.InRange(-20f, 20f), "height change after 20 s hands-off");
            Assert.That(u.Hiz, Is.InRange(55f, 110f));
            Assert.That(u.YukFaktoru, Is.InRange(0.85f, 1.15f), "about 1 g");
            Assert.That(Mathf.Abs(u.YanKayma), Is.LessThan(1f), "no sideslip");
        }

        [Test]
        public void BankedTurn_TurnRateIsGTanBankOverV_WithoutLosingHeight()
        {
            u.HizAyarla(75f);
            u.GazAyarla(0.75f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, -45f);   // right wing down 45°
            Uc(3f);
            float yon0 = go.transform.eulerAngles.y, h0 = go.transform.position.y;
            const float sure = 6f;
            Uc(sure);
            float donus = Mathf.DeltaAngle(yon0, go.transform.eulerAngles.y) / sure;
            float beklenen = 9.81f * Mathf.Tan(45f * Mathf.Deg2Rad) / u.Hiz * Mathf.Rad2Deg;
            Assert.That(u.YatisAcisi, Is.EqualTo(45f).Within(3f), "the bank stays");
            Assert.That(donus, Is.EqualTo(beklenen).Within(beklenen * 0.25f), $"turn rate {donus:F1}°/s vs g·tanφ/v {beklenen:F1}°/s");
            Assert.That(go.transform.position.y - h0, Is.InRange(-40f, 40f), "the turn assist keeps the height");
            Assert.That(u.YukFaktoru, Is.EqualTo(1.414f).Within(0.25f), "load factor 1/cos 45°");
        }

        [Test]
        public void FullPull_Fast_ReachesTheGLimit_Slow_HeldAtTheBuffet_WithoutProtectionStalls()
        {
            // Fast: the structural / stick-force limit binds.
            u.HizAyarla(110f);
            u.GazAyarla(0.8f);
            u.Yunuslama = -1f;   // S: nose up
            float enCok = 0f;
            for (int i = 0; i < 90; i++) { u.Adim(1f / 60f); enCok = Mathf.Max(enCok, u.YukFaktoru); }
            Assert.That(enCok, Is.InRange(u.Aero.enBuyukYuk - 0.5f, u.Aero.enBuyukYuk + 0.3f), $"peak {enCok:F2} g at 110 m/s");

            // Below the corner speed the wing binds: 60 m/s gives the lift of the stall angle minus the margin, not 4.5 g.
            go.transform.rotation = Quaternion.identity;
            u.HizAyarla(60f);
            enCok = 0f;
            float alfa = 0f;
            for (int i = 0; i < 45; i++) { u.Adim(1f / 60f); enCok = Mathf.Max(enCok, u.YukFaktoru); alfa = Mathf.Max(alfa, u.HucumAcisi); }
            Assert.That(enCok, Is.InRange(2.2f, 3.2f), $"peak {enCok:F2} g at 60 m/s (wing-limited)");
            Assert.Less(alfa, u.Aero.tutunmaAcisi, "the protection keeps the wing below the stall");

            // Slow and idle, back stick held: the warning sounds, the wing does not stall, the aircraft sinks.
            go.transform.rotation = Quaternion.identity;
            u.HizAyarla(40f);
            u.GazAyarla(0f);
            bool uyari = false, tutunma = false;
            for (int i = 0; i < 300; i++)
            {
                u.Adim(1f / 60f);
                uyari |= u.TutunmaUyarisi;
                tutunma |= u.TutunmaKaybi;
            }
            Assert.IsTrue(uyari, "stall warning near the stall angle");
            Assert.IsFalse(tutunma, "with the protection the pull stops at the buffet");

            // Protection off: the same pull stalls the wing (the physics is still there).
            u.Aero.tutunmaKorumasi = false;
            go.transform.rotation = Quaternion.identity;
            u.HizAyarla(40f);
            tutunma = false;
            for (int i = 0; i < 300; i++) { u.Adim(1f / 60f); tutunma |= u.TutunmaKaybi; }
            Assert.IsTrue(tutunma, "without the protection full back stick at 40 m/s stalls");
        }

        [Test]
        public void KeyTap_IsASmallInput_KeyHeld_IsAFullOne()
        {
            u.HizAyarla(80f);
            u.GazAyarla(0.6f);
            Uc(0.5f);
            u.Yatis = 1f; Uc(0.1f); u.Yatis = 0f; Uc(0.9f);
            float dokunus = u.YatisAcisi;
            Assert.That(dokunus, Is.InRange(0.3f, 4f), $"a 0.1 s tap of D: {dokunus:F1}° of bank");

            go.transform.rotation = Quaternion.identity;
            u.HizAyarla(80f);
            Uc(0.5f);
            float y0 = Mathf.Asin(go.transform.forward.y) * Mathf.Rad2Deg;
            u.Yunuslama = -1f; Uc(0.1f); u.Yunuslama = 0f; Uc(0.9f);
            float burun = Mathf.Asin(go.transform.forward.y) * Mathf.Rad2Deg - y0;
            Assert.That(burun, Is.InRange(0.2f, 4f), $"a 0.1 s tap of S: nose +{burun:F1}°");

            go.transform.rotation = Quaternion.identity;
            u.HizAyarla(80f);
            Uc(0.5f);
            u.Yatis = 1f; Uc(1f); u.Yatis = 0f;
            Assert.Greater(u.YatisAcisi, 20f, "held 1 s: a real bank");
            Assert.Less(u.YatisAcisi, 70f, "but no snap roll");
        }

        [Test]
        public void RollRate_GrowsWithTheAirspeed_pb2V_AndTheBankHolds_SmallBanksLevel()
        {
            float SabitHiz(float v)
            {
                go.transform.rotation = Quaternion.identity;
                u.HizAyarla(v);
                u.GazAyarla(0.6f);
                u.Yatis = 1f;
                Uc(1.2f);
                float b0 = u.YatisAcisi;
                Uc(0.2f);
                float oran = Mathf.DeltaAngle(b0, u.YatisAcisi) / 0.2f;
                u.Yatis = 0f;
                return oran;
            }
            float p50 = SabitHiz(50f), p100 = SabitHiz(100f);
            Assert.That(p100 / p50, Is.InRange(1.6f, 2.3f), $"roll rate {p50:F0}°/s at 50 m/s, {p100:F0}°/s at 100 m/s (∝ V)");
            Assert.That(p100, Is.EqualTo(u.Aero.YatisHiziSiniri(100f)).Within(12f), "pb/2V law");

            // Roll to 30°, release: the bank holds (spirally neutral), the aircraft keeps turning.
            go.transform.rotation = Quaternion.identity;
            u.HizAyarla(70f);
            u.Yatis = 1f;
            for (int i = 0; i < 600 && u.YatisAcisi < 30f; i++) u.Adim(1f / 60f);
            u.Yatis = 0f;
            Uc(1f);
            float b1 = u.YatisAcisi, yon0 = go.transform.eulerAngles.y;
            Uc(4f);
            Assert.That(u.YatisAcisi, Is.InRange(25f, 48f), "the bank stays where it was left");
            Assert.Less(Mathf.Abs(u.YatisAcisi - b1), 3f, "and does not drift");
            Assert.Greater(Mathf.DeltaAngle(yon0, go.transform.eulerAngles.y), 12f, "a banked aircraft turns");

            // A small bank without aileron levels out (dihedral).
            go.transform.rotation = Quaternion.Euler(0f, 0f, -5f);
            u.HizAyarla(70f);
            Uc(6f);
            Assert.Less(Mathf.Abs(u.YatisAcisi), 1.5f, "5° of bank levels out");
        }

        [Test]
        public void Dive_GainsSpeed_Climb_LosesIt()
        {
            go.transform.rotation = Quaternion.Euler(30f, 0f, 0f);   // nose 30° down
            u.HizAyarla(60f);                                        // velocity along the nose
            u.GazAyarla(0.3f);
            Uc(5f);
            Assert.Greater(u.Hiz, 75f, "diving speeds up");
            Assert.Less(u.DikeyHiz, -20f);

            go.transform.rotation = Quaternion.Euler(-25f, 0f, 0f);
            u.HizAyarla(80f);
            Uc(5f);
            Assert.Less(u.Hiz, 70f, "climbing steeply slows down");
        }

        [Test]
        public void ControlAuthority_FallsWithAirspeed()
        {
            float Donus(float v)
            {
                u.Yatis = 0f;
                Uc(0.2f);   // the stick back in the centre (it remembers the previous run)
                go.transform.rotation = Quaternion.identity;
                u.HizAyarla(v);
                u.Yatis = 1f;
                Uc(0.5f);
                u.Yatis = 0f;
                return Mathf.Abs(u.YatisAcisi);
            }
            // 40 m/s: just above the stall speed (34.8 m/s); below it the aircraft cannot fly and the bank angle is not defined well.
            float hizli = Donus(80f), yavas = Donus(40f);
            Assert.Greater(hizli, 1.6f * yavas, $"roll in 0.5 s: {hizli:F0}° at 80 m/s vs {yavas:F0}° at 40 m/s (pb/2V: ∝ V)");
        }

        // ---- ground ----------------------------------------------------------------------------------------------------

        [Test]
        public void GroundRoll_FullThrottle_Accelerates_WithoutLeavingTheGround()
        {
            go.transform.position = new Vector3(0f, 10f, 0f);
            u.YerTemasi = true;
            u.HizAyarla(0f);
            u.GazAyarla(1f);
            Uc(10f);
            Assert.That(u.Hiz, Is.InRange(25f, 45f), "speed after 10 s on the runway");
            Assert.That(u.DikeyHiz, Is.GreaterThanOrEqualTo(0f));
            Assert.That(go.transform.position.y, Is.EqualTo(10f).Within(1.5f), "stays down without back stick");
            Assert.That(Mathf.Abs(u.YatisAcisi), Is.LessThan(0.5f));
        }

        [Test]
        public void Brakes_AtIdle_StopWithinTheLandingZone()
        {
            go.transform.position = new Vector3(0f, 10f, 0f);
            u.YerTemasi = true;
            u.HizAyarla(40f);
            u.GazAyarla(0f);
            Vector3 bas = go.transform.position;
            Uc(20f);
            float yol = Vector3.Distance(bas, go.transform.position);
            Assert.That(u.Hiz, Is.LessThan(0.5f), "stopped");
            Assert.That(yol, Is.LessThan(260f), $"stopping distance {yol:F0} m from 40 m/s");
        }
    }
}
