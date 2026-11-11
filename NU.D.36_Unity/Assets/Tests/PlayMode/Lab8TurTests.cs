using System.Collections;
using System.Collections.Generic;
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
    /// Lab 8 (Hafta 8) TEST-827: one whole round of Bölüm 4 at 5× speed — start in the air over the train, the three fire
    /// zones, the train into the safe zone, back to the base, "Görev tamam". The aircraft is placed by a scripted pilot
    /// (teleported each frame: to an open spot near the selected target, or over the locomotive); the game's own firing
    /// assist (T/O, machine-gun aim, automatic guided bomb) does the shooting. So this measures the mission loop and the
    /// balance with the assists, not the flying. Images with the HUD → Logs/lab8/gorsel/tur_*.png; numbers →
    /// Logs/lab8/tur_olcumu.txt.
    /// </summary>
    public class Lab8TurTests
    {
        static readonly StringBuilder Olcum = new StringBuilder();
        static string GorselDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab8/gorsel"));

        [TearDown]
        public void Bitir() => Time.timeScale = 1f;

        [OneTimeTearDown]
        public void Yaz()
        {
            if (Olcum.Length == 0) return;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab8"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "tur_olcumu.txt"), $"Lab 8 full round, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n" + Olcum);
        }

        /// <summary>Camera render including the overlay HUD (canvases switched to the camera for the shot).</summary>
        static void Kaydet(Camera cam, string ad, int w = 1600, int h = 900)
        {
            var tuvaller = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            foreach (Canvas c in tuvaller) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.05f; }
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR);
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var ldr = new RenderTexture(w, h, 0);
            Graphics.Blit(rt, ldr);
            RenderTexture.active = ldr;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            foreach (Canvas c in tuvaller) c.renderMode = RenderMode.ScreenSpaceOverlay;
            Directory.CreateDirectory(GorselDir);
            File.WriteAllBytes(Path.Combine(GorselDir, ad + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            ldr.Release();
        }

        static Vector3 AcikNokta(Vector3 hedef, float uzaklik, float yukseklik, Vector3 tercih)
        {
            int arazi = LayerMask.GetMask("Arazi");
            Vector3 enIyi = hedef + Vector3.up * yukseklik;
            float enIyiPuan = float.MinValue;
            for (int i = 0; i < 24; i++)
            {
                Vector3 yon = Quaternion.Euler(0f, i * 15f, 0f) * Vector3.forward;
                Vector3 p = hedef + yon * uzaklik + Vector3.up * yukseklik;
                if (Physics.Linecast(p, hedef + Vector3.up * 2f, arazi)) continue;
                float pay = ZeminOlcer.Irtifa(p);
                if (pay < 40f) continue;
                float puan = Vector3.Dot(yon, tercih) * 50f - Mathf.Abs(pay - yukseklik) * 0.2f;
                if (puan > enIyiPuan) { enIyiPuan = puan; enIyi = p; }
            }
            return enIyi;
        }

        [UnityTest]
        public IEnumerator FullRound_ThreeZones_SafeZone_Base_GorevTamam()
        {
            yield return SceneManager.LoadSceneAsync("Bolum4_KursunDaglari", LoadSceneMode.Single);
            var ucus = Object.FindAnyObjectByType<UcusKontrol>();
            var tren = Object.FindAnyObjectByType<Tren>();
            var gorev = Object.FindAnyObjectByType<KursunDaglariGorevi>();
            var nisan = ucus.GetComponent<Bolum4Nisan>();
            var mk = ucus.GetComponent<Makineli>();
            var yuk = ucus.GetComponent<BombaYuku>();
            var hud = Object.FindAnyObjectByType<Bolum4Hud>();
            var bolgeler = Object.FindObjectsByType<AtesBolgesi>(FindObjectsSortMode.None).OrderBy(b => b.Bas).ToArray();
            string yuklenen = null;
            gorev.SahneYukle = ad => yuklenen = ad;
            if (ucus.TryGetComponent(out UcusYardimcisi y)) y.enabled = false;
            if (ucus.TryGetComponent(out InisAlgilayici a)) a.enabled = false;
            var ucakSagligi = ucus.GetComponent<Saglik>();
            ucakSagligi.BaslangicAyarla(100000);   // a hovering scripted pilot is an easy target; its damage is logged
            Camera cam = Camera.main;
            yield return null;
            Kaydet(cam, "tur_01_baslangic_hud");

            var cekimler = new HashSet<string>();
            var hedefNokta = new Dictionary<object, Vector3>();
            int ucakHasari = 0;
            ucakSagligi.CanDegisti += _ => ucakHasari = 100000 - ucakSagligi.Can;
            var trenCanlari = new List<string>();
            Time.timeScale = 5f;
            float sinir = Time.realtimeSinceStartup + 240f;
            while (gorev.Simdiki == KursunDaglariGorevi.Asama.Eskort && Time.realtimeSinceStartup < sinir)
            {
                Transform lok = tren.Araclar[0].transform;
                Bolum4Nisan.Hedef h = nisan.Secili;
                Vector3 konum, bak;
                if (h != null && h.Canli && (h.Bomba ? yuk.Kalan > 0 : !mk.Bos))
                {
                    object anahtar = h.Kaynak;
                    if (!hedefNokta.TryGetValue(anahtar, out konum) || h.Kaynak.Tur == DusmanTuru.Ucak)
                    {
                        Vector3 tercih = (lok.position - h.Nokta); tercih.y = 0f; tercih.Normalize();
                        konum = h.Bomba ? AcikNokta(h.Nokta, 650f, 240f, tercih) : AcikNokta(h.Nokta, h.Kaynak.Tur == DusmanTuru.Ucak ? 260f : 320f, 100f, tercih);
                        if (h.Bomba && ZeminOlcer.Irtifa(konum) > H.BombaTavani - 30f) konum.y -= ZeminOlcer.Irtifa(konum) - (H.BombaTavani - 50f);
                        hedefNokta[anahtar] = konum;
                    }
                    bak = h.Bomba ? h.Nokta : (nisan.OncuNokta ?? h.Nokta);
                }
                else
                {
                    konum = lok.position + Vector3.up * 150f - lok.forward * 120f;
                    bak = lok.position + lok.forward * 200f;
                }
                ucus.transform.SetPositionAndRotation(konum, Quaternion.LookRotation(bak - konum));
                ucus.HizAyarla(0f);
                Physics.SyncTransforms();
                Lab7PlayTests.CarpismaSifirla(ucus);

                foreach (AtesBolgesi b in bolgeler)
                    if (b.Simdiki == AtesBolgesi.Durum.Etkin && cekimler.Add("bolge_" + b.Bas))
                    {
                        yield return null;
                        Time.timeScale = 1f;
                        Kaydet(cam, $"tur_0{cekimler.Count + 1}_bolge{cekimler.Count}");
                        Time.timeScale = 5f;
                        trenCanlari.Add($"{b.Ad} başı: tren %{tren.Saglik.Can}");
                    }
                yield return null;
            }
            Time.timeScale = 1f;
            Olcum.AppendLine($"after the escort: phase {gorev.Simdiki} at {gorev.Sure:F0} s game time; train health {tren.Saglik.Can}; silenced {gorev.Susturulan}/{gorev.ToplamKaynak} (tank {gorev.SusturulanTank}, AA {gorev.SusturulanUcaksavar}, aircraft {gorev.SusturulanUcak}); rounds left {mk.Kalan}, bombs left {yuk.Kalan}; damage taken by the aircraft {ucakHasari}; {string.Join("; ", trenCanlari)}");
            Assert.AreEqual(KursunDaglariGorevi.Asama.UsseDon, gorev.Simdiki, "the train reached the safe zone");
            Assert.IsTrue(bolgeler.All(b => b.Simdiki == AtesBolgesi.Durum.Bitti), "all three zones passed");
            Assert.That(gorev.Sure, Is.EqualTo(H.VarisSuresi).Within(15f), "5 minutes");
            StringAssert.StartsWith("Üsse dön", hud.Gorev);

            // Back to the base, 150 m above the ground at 70 m/s, then down into the base zone.
            Vector3 us = gorev.UsMerkezi;
            float baslangicSure = gorev.Sure;
            sinir = Time.realtimeSinceStartup + 60f;
            Time.timeScale = 3f;
            while (gorev.Simdiki == KursunDaglariGorevi.Asama.UsseDon && Time.realtimeSinceStartup < sinir)
            {
                Vector3 p = ucus.transform.position;
                Vector3 d = new Vector3(us.x - p.x, 0f, us.z - p.z);
                Vector3 adim = d.magnitude > 1f ? d.normalized * Mathf.Min(d.magnitude, 70f * Time.deltaTime) : Vector3.zero;
                Vector3 yeni = p + adim;
                float hedefY = ZeminOlcer.Yukseklik(yeni.x, yeni.z) + (d.magnitude < 600f ? 120f : 160f);
                yeni.y = Mathf.MoveTowards(p.y, Mathf.Max(hedefY, ZeminOlcer.Yukseklik(yeni.x, yeni.z) + 60f), 40f * Time.deltaTime);
                ucus.transform.SetPositionAndRotation(yeni, Quaternion.LookRotation(d.sqrMagnitude > 1f ? d : ucus.transform.forward));
                ucus.HizAyarla(0f);
                Physics.SyncTransforms();
                Lab7PlayTests.CarpismaSifirla(ucus);
                yield return null;
            }
            Time.timeScale = 1f;
            yield return null;
            yield return null;
            Kaydet(cam, "tur_06_gorev_tamam");
            Olcum.AppendLine($"return to base: {gorev.Sure - baslangicSure:F0} s; total {gorev.Sure:F0} s ({Bolum4Hud.SureYazisi(gorev.Sure)})");
            Olcum.AppendLine("Görev tamam panel:\n" + hud.TamamYazisiSon);
            Assert.AreEqual(KursunDaglariGorevi.Asama.Tamam, gorev.Simdiki, "Görev tamam at the base");
            Assert.IsTrue(hud.TamamGorunuyor);
            Assert.Greater(tren.Saglik.Can, 0, "the train survived");
            Assert.Less(gorev.Sure, 7f * 60f, "a round fits in a 7-minute video");
            Assert.IsNull(yuklenen, "no restart");
        }
    }
}
