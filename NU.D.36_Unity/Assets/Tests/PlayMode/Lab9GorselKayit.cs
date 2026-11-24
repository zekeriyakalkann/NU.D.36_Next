using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using H = NU36.KartalGecidiHaritasi;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 9: images of Bölüm 5 in Play Mode for checking the look (HUD included where it matters) →
    /// Logs/lab9/gorsel/. Not a pass/fail test. Run with -testFilter Lab9GorselKayit.
    /// </summary>
    [Explicit("Writes images; run with -testFilter Lab9GorselKayit")]
    public class Lab9GorselKayit
    {
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab9/gorsel"));

        [TearDown]
        public void Bitir() => Time.timeScale = 1f;

        static void Kaydet(Camera cam, string ad, bool hud, int w = 1600, int h = 900)
        {
            var tuvaller = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            foreach (Canvas c in tuvaller)
            {
                if (hud) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.05f; }
                else c.enabled = false;
            }
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
            foreach (Canvas c in tuvaller) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.enabled = true; }
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(Path.Combine(Dir, ad + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            ldr.Release();
        }

        static IEnumerator Kare(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator Goruntuler()
        {
            yield return SceneManager.LoadSceneAsync("Bolum5_KartalGecidi", LoadSceneMode.Single);
            var ucus = Object.FindAnyObjectByType<UcusKontrol>();
            var filo = Object.FindAnyObjectByType<CikarmaFilosu>();
            var gorev = Object.FindAnyObjectByType<KartalGecidiGorevi>();
            gorev.SahneYukle = _ => { };
            if (ucus.TryGetComponent(out InisAlgilayici a)) a.enabled = false;
            ucus.GetComponent<Saglik>().BaslangicAyarla(100000);
            ucus.GetComponent<Bolum4Nisan>().Yardim = false;
            Camera cam = Camera.main;
            yield return Kare(30);
            Kaydet(cam, "01_baslangic_gecit_agzi", true);

            // Inside the pass: the aircraft held on the centre line, the follow camera behind it.
            RayEgrisi e = H.Gecit;
            for (int i = 0; i < 60; i++)
            {
                float s = 1900f + i * 2f;
                ucus.transform.SetPositionAndRotation(e.Nokta(s) + Vector3.up * 120f, Quaternion.LookRotation(e.YatayYon(s)));
                yield return null;
            }
            Kaydet(cam, "02_gecit_ici", true);

            // Out over the bay: the four ships, the beach and the HUD.
            for (int i = 0; i < 90; i++)
            {
                ucus.transform.SetPositionAndRotation(new Vector3(900f + i * 3f, 260f, 40f), Quaternion.LookRotation(new Vector3(1f, -0.18f, 0f)));
                yield return null;
            }
            Kaydet(cam, "03_koy_dort_gemi", true);

            // The landing, step by step. First with the HUD: the aircraft over the bay, the beach camera window open.
            CikarmaGemisi g = filo.SeritGemisi(1);
            g.Isinla(25f);
            Time.timeScale = 3f;
            float bitis = Time.time + 60f;
            while (g.Simdiki != CikarmaGemisi.Durum.Indiriyor && Time.time < bitis)
            {
                ucus.transform.SetPositionAndRotation(new Vector3(2300f, 280f, 300f), Quaternion.LookRotation(new Vector3(-1f, -0.25f, -0.3f)));
                yield return null;
            }
            Time.timeScale = 1f;
            bitis = Time.time + 2.5f;   // the first tank is on the deck heading for the ramp
            while (Time.time < bitis)
            {
                ucus.transform.SetPositionAndRotation(new Vector3(2300f, 280f, 300f), Quaternion.LookRotation(new Vector3(-1f, -0.25f, -0.3f)));
                yield return null;
            }
            Kaydet(cam, "04_hud_kiyi_kamerasi", true);

            // Free camera beside the beached ship.
            ucus.enabled = false;
            ucus.transform.position = new Vector3(-2500f, 800f, 0f);
            if (cam.TryGetComponent(out TakipKamerasi takip)) takip.enabled = false;
            Vector3 gp = g.transform.position;
            Vector3 ileri = H.KiyiyaDogru;
            Vector3 yan = Vector3.Cross(Vector3.up, ileri);
            void Bak(Vector3 konum, Vector3 hedef) => cam.transform.SetPositionAndRotation(konum, Quaternion.LookRotation(hedef - konum));
            Vector3 rampa = gp + ileri * 27f;
            CikarmaTanki ilk = g.Tanklar[0], ikinci = g.Tanklar[1];
            bitis = Time.time + 30f;
            while ((ilk.Simdiki == CikarmaTanki.Durum.Gemide || Vector3.Distance(ilk.transform.position, g.RampaUcu) > 3f) && Time.time < bitis) yield return null;
            Bak(rampa + yan * 26f + Vector3.up * 7f - ileri * 4f, rampa + Vector3.up * 1f);
            Kaydet(cam, "05_tank_rampada", false);
            bitis = Time.time + 10f;
            while (Time.time < bitis && ikinci.Simdiki != CikarmaTanki.Durum.Karada) yield return null;
            yield return Kare(40);
            Bak(rampa + ileri * 40f + yan * 30f + Vector3.up * 14f, rampa + ileri * 8f);
            Kaydet(cam, "06_kumsala_cikis_toz", false);
            Bak(gp + yan * 70f - ileri * 20f + Vector3.up * 25f, gp + ileri * 25f);
            Kaydet(cam, "07_gemi_ve_kumsal", false);

            // A ship stopped at sea: smoke and fire, listing.
            CikarmaGemisi v = filo.SeritGemisi(3);
            v.Saglik.Vur(10000, HasarTuru.Mermi, Taraf.Dost);
            Time.timeScale = 3f;
            bitis = Time.time + 24f;
            while (Time.time < bitis) yield return null;
            Time.timeScale = 1f;
            Vector3 vp = v.transform.position;
            cam.transform.SetPositionAndRotation(vp + new Vector3(90f, 35f, -90f), Quaternion.LookRotation(vp - (vp + new Vector3(90f, 35f, -90f))));
            Kaydet(cam, "08_durdurulan_gemi", false);

            // Tanks parked behind the beach.
            yield return Kare(5);
            Vector3 park = H.ParkYeri(1, 1);
            park.y = ZeminOlcer.Yukseklik(park.x, park.z);
            cam.transform.SetPositionAndRotation(park + new Vector3(60f, 30f, -40f), Quaternion.LookRotation(park - (park + new Vector3(60f, 30f, -40f))));
            Kaydet(cam, "09_karaya_cikan_tanklar", false);

            // The map from above (no fog).
            cam.orthographic = true;
            cam.orthographicSize = 3400f;
            cam.transform.SetPositionAndRotation(new Vector3(300f, 5000f, 0f), Quaternion.Euler(90f, 0f, 0f));
            float uzak = cam.farClipPlane;
            cam.farClipPlane = 8000f;
            bool sis = RenderSettings.fog;
            RenderSettings.fog = false;
            Kaydet(cam, "10_ust_harita", false, 1600, 1600);
            RenderSettings.fog = sis;
            cam.farClipPlane = uzak;
            cam.orthographic = false;

            // The end panel: a coast that falls (the other ships beach at once).
            foreach (CikarmaGemisi s in filo.Gemiler.Where(x => x.Simdiki == CikarmaGemisi.Durum.Yolda)) s.Isinla(30f);
            Time.timeScale = 5f;
            bitis = Time.time + 150f;
            while (!gorev.Bitti && Time.time < bitis)
            {
                foreach (CikarmaGemisi s in filo.Gemiler.Where(x => x.Simdiki == CikarmaGemisi.Durum.Yolda && x.KalanMesafe > 40f)) s.Isinla(30f);
                yield return null;
            }
            Time.timeScale = 1f;
            yield return Kare(3);
            cam.transform.SetPositionAndRotation(new Vector3(900f, 120f, -500f), Quaternion.LookRotation(new Vector3(1f, -0.15f, 0.6f)));
            Kaydet(cam, "11_son_paneli", true);
            Assert.Pass($"images in {Dir}; end: {gorev.Simdiki}, tanks ashore {filo.KaradakiTank}");
        }
    }
}
