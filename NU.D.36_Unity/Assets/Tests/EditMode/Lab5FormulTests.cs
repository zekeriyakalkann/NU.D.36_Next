using NUnit.Framework;
using UnityEngine;

namespace NU36.Tests
{
    /// <summary>Lab 5: the worksheet's numbers, checked against the formulas and integrators used in the game.</summary>
    public class Lab5FormulTests
    {
        // ---- Step 01: line of sight -------------------------------------------------------------

        [Test]
        public void GorusEngelliMi_EmptyMaskNeverBlocks()
        {
            Assert.IsFalse(Radar.GorusEngelliMi(Vector3.zero, new Vector3(0f, 0f, 100f), 0));
        }

        [Test]
        public void GizmoRengi_ThreeStates()
        {
            Assert.AreEqual(Color.green, Radar.GizmoRengi(true, false), "sees");
            Assert.AreEqual(Color.yellow, Radar.GizmoRengi(false, true), "inside the cone but blocked");
            Assert.AreEqual(Color.red, Radar.GizmoRengi(false, false), "outside the cone");
        }

        // ---- Step 02: sound delay -----------------------------------------------------------------

        [Test]
        public void SesGecikmesi_600mIs175s()
        {
            Assert.That(SesDinleyici.GecikmeHesapla(600f), Is.EqualTo(1.749f).Within(0.001f));
        }

        [Test]
        public void AcisalHata_500kmhAbeamIs22Degrees_AndIndependentOfDistance()
        {
            Assert.That(SesDinleyici.AcisalHata(500f / 3.6f, 343f, 90f), Is.EqualTo(22.0f).Within(0.05f));
            Assert.That(SesDinleyici.AcisalHata(85f, 343f, 0f), Is.EqualTo(0f).Within(1e-4f), "head-on: no angular error");
        }

        static (SesDinleyici kulak, UcusKontrol ucak) Dinleyici()
        {
            var kulak = new GameObject("Kulak").AddComponent<SesDinleyici>();
            var ucak = new GameObject("Ucak").AddComponent<UcusKontrol>();
            kulak.Ucak = ucak;
            return (kulak, ucak);
        }

        [Test]
        public void SesDinleyici_PassAt600m_SapmaIsAbout149m()
        {
            var (kulak, ucak) = Dinleyici();
            try
            {
                ucak.GazAyarla(1f);
                const float v = 85f, dt = 0.02f;
                // Straight line 600 m abeam of the listener; the aircraft is exactly abeam at t = 6 s.
                for (int i = 0; i <= 300; i++)
                {
                    float t = i * dt;
                    ucak.transform.position = new Vector3(v * (t - 6f), 0f, 600f);
                    kulak.Adim(t);
                }
                Assert.IsTrue(kulak.Duyuyor);
                Assert.That(kulak.Gecikme, Is.EqualTo(600f / 343f).Within(0.01f));
                Assert.That(kulak.Sapma, Is.EqualTo(85f * 600f / 343f).Within(1.5f), "worksheet: 85 × 600 / 343 ≈ 149 m");
                Assert.That(kulak.DuyulanKonum.x, Is.LessThan(ucak.transform.position.x), "the heard position lies behind the aircraft");
            }
            finally
            {
                Object.DestroyImmediate(kulak.gameObject);
                Object.DestroyImmediate(ucak.gameObject);
            }
        }

        [Test]
        public void SesDinleyici_ThrottleCut_NotHeard_AndOutOfRange_NotHeard()
        {
            var (kulak, ucak) = Dinleyici();
            try
            {
                ucak.transform.position = new Vector3(0f, 0f, 500f);
                ucak.GazAyarla(0.5f);
                kulak.Adim(0f);
                Assert.IsTrue(kulak.Duyuyor);
                ucak.GazAyarla(0f);
                kulak.Adim(0.02f);
                Assert.IsFalse(kulak.Duyuyor, "gaz kesilince Duyuyor söner");
                ucak.GazAyarla(1f);
                ucak.transform.position = new Vector3(0f, 0f, kulak.DuymaMenzili + 10f);
                kulak.Adim(0.04f);
                Assert.IsFalse(kulak.Duyuyor);
            }
            finally
            {
                Object.DestroyImmediate(kulak.gameObject);
                Object.DestroyImmediate(ucak.gameObject);
            }
        }

        // ---- Step 03: tunnelling and the altitude band ------------------------------------------

        [Test]
        public void MermiAdimi_At260_IsLargerThanTheBodyDiameter()
        {
            float adim = Mermi.AdimUzunlugu(260f, 0.02f);
            Assert.That(adim, Is.EqualTo(5.2f).Within(1e-4f));
            Assert.That(adim, Is.GreaterThan(2f * 2.4f), "step 5.2 m > diameter 4.8 m: the round can jump over the aircraft");
            Assert.That(Mermi.AdimUzunlugu(70f, 0.02f), Is.LessThan(4.8f));
        }

        [TestCase(5f, 6f)]
        [TestCase(14.9f, 6f)]
        [TestCase(15f, 0f)]
        [TestCase(200f, 0f)]
        [TestCase(460f, 0f)]
        [TestCase(461f, 4.2f)]
        public void Sacilma_DeathBand15To460(float irtifa, float beklenen)
        {
            Assert.That(UcaksavarMevzii.Sacilma(irtifa, 15f, 460f, 6f), Is.EqualTo(beklenen).Within(1e-4f));
        }

        // ---- Step 04: bomb ------------------------------------------------------------------------

        [TestCase(30f, 25f)]
        [TestCase(45f, 54f)]
        [TestCase(60f, 92f)]
        [TestCase(70f, 121f)]
        [TestCase(90f, 184f)]
        public void ToparlanmaKaybi_MatchesWorksheetTable(float aci, float beklenen)
        {
            Assert.That(Bomba.ToparlanmaYaricapi(85f, 4f), Is.EqualTo(184f).Within(0.5f));
            Assert.That(Bomba.ToparlanmaKaybi(85f, 4f, aci), Is.EqualTo(beklenen).Within(0.6f));
        }

        [TestCase(30f, 198f)]
        [TestCase(45f, 128f)]
        [TestCase(60f, 77f)]
        [TestCase(70f, 49f)]
        [TestCase(90f, 0f)]
        public void IleriMenzil_MatchesWorksheetTable_AndTheIntegratorAgrees(float aci, float beklenen)
        {
            Assert.That(Bomba.IleriMenzil(85f, aci, 150f), Is.EqualTo(beklenen).Within(0.6f));

            // The aiming ring uses the bomb's own semi-implicit Euler at the physics step: within 1 m of the closed form.
            Vector3 hiz = Quaternion.Euler(aci, 0f, 0f) * Vector3.forward * 85f;
            Assert.IsTrue(Bomba.DususNoktasi(new Vector3(0f, 150f, 0f), hiz, new Vector3(0f, -9.81f, 0f), 0.02f, (x, z) => 0f,
                out Vector3 nokta, out float sure));
            Assert.That(nokta.z, Is.EqualTo(Bomba.IleriMenzil(85f, aci, 150f)).Within(1f));
            Assert.That(nokta.y, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(sure, Is.EqualTo(Bomba.DususSuresi(85f, aci, 150f)).Within(0.03f));
        }

        [TestCase(44.9f, 200f, false)]
        [TestCase(45f, 120f, true)]
        [TestCase(70f, 260f, true)]
        [TestCase(70.1f, 200f, false)]
        [TestCase(60f, 119f, false)]
        [TestCase(60f, 261f, false)]
        public void BirakmaPenceresi_45To70Degrees_120To260m(float aci, float irtifa, bool beklenen)
        {
            Assert.AreEqual(beklenen, BombaYuku.PencereIcindeMi(aci, irtifa));
        }

        [Test]
        public void DalisAcisi_PositiveNoseDown()
        {
            Assert.That(BombaYuku.DalisAcisi(Quaternion.Euler(60f, 0f, 0f) * Vector3.forward), Is.EqualTo(60f).Within(0.01f));
            Assert.That(BombaYuku.DalisAcisi(Vector3.forward), Is.EqualTo(0f).Within(0.01f));
        }

        // ---- Step 05: HUD texts -------------------------------------------------------------------

        [Test]
        public void Ozet_ListsTheFourNumbers()
        {
            string s = SeviyeSonuEkrani.Ozet(131.26f, 1, 76, 529);
            StringAssert.Contains(131.26f.ToString("F1"), s); // culture-dependent decimal separator
            StringAssert.Contains("İkmal: 1", s);
            StringAssert.Contains("Kalan can: 76", s);
            StringAssert.Contains("Toplam puan: 529", s);
            Assert.AreEqual("Mevzi 1/4   Bomba 1/2   Can 90   Puan 100", SeviyeHud.Metin(1, 4, 1, 2, 90, 100));
        }
    }
}
