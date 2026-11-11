using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>Lab 7 visual record (explicit, run on demand): camera renders of the Bölüm 3 night level into Logs/lab7/gorsel/*.png.</summary>
    [Explicit("Writes images; run with -testFilter Lab7GorselKayit")]
    public class Lab7GorselKayit
    {
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab7/gorsel"));

        static void Kaydet(Camera cam, string ad, int w = 1600, int h = 900)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR);
            cam.targetTexture = rt;
            cam.Render();
            var ldr = new RenderTexture(w, h, 0);
            Graphics.Blit(rt, ldr);
            RenderTexture.active = ldr;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(Path.Combine(Dir, ad + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            ldr.Release();
        }

        static IEnumerator Bekle(float saniye)
        {
            float bitis = Time.time + saniye;
            while (Time.time < bitis)
                yield return null;
        }

        [UnityTest]
        public IEnumerator Gorunumler()
        {
            yield return SceneManager.LoadSceneAsync("Rota07", LoadSceneMode.Single);
            yield return null;
            yield return null;
            var ucus = Object.FindAnyObjectByType<UcusKontrol>();
            var gorev = Object.FindAnyObjectByType<GeceGorevi>();
            Object.FindAnyObjectByType<GeceSonuEkrani>().SahneYukle = _ => { };
            Camera cam = Camera.main;
            var takip = cam.GetComponent<TakipKamerasi>();
            yield return Bekle(0.5f);

            // 1. The player's view on the runway at the start (row 8, nose east).
            Kaydet(cam, "01_baslangic_pistte");

            takip.enabled = false;
            Vector3 kopru = GeceHaritasi.KopruMerkezi;

            // 2. From over the airfield's east end towards the bridge, the searchlights and the clouds.
            Vector3 pist = GeceHaritasi.PistSonu;
            cam.transform.position = pist + new Vector3(-150f, 230f, -250f);
            cam.transform.LookAt(kopru);
            Kaydet(cam, "02_havaalani_kopruye_bakis");

            // 3. The 600 m bridge over the wide reach (H5–J5), from the south-west.
            cam.transform.position = kopru + new Vector3(-420f, 90f, -330f);
            cam.transform.LookAt(kopru + Vector3.right * 60f);
            Kaydet(cam, "03_kopru_H5_J5");

            // 4. The K5 searchlight tower at the bridge's east end.
            Vector3 kule = GeceHaritasi.KuleKonumu(1);
            cam.transform.position = kule + new Vector3(70f, 45f, -80f);
            cam.transform.LookAt(kule + Vector3.up * 22f);
            Kaydet(cam, "04_isildak_K5");

            // 5. The sound locator H3.
            Vector3 ses = GeceHaritasi.SesDinleyiciKonumu;
            cam.transform.position = ses + new Vector3(18f, 9f, -16f);
            cam.transform.LookAt(ses + Vector3.up * 3.5f);
            Kaydet(cam, "05_ses_dinleyici_H3");

            // 6. Beams and the clouds of the single rule (E5–G5 at Orta), seen from the west at 200 m.
            cam.transform.position = GeceHaritasi.Kare("C5") + Vector3.up * 230f;
            cam.transform.LookAt(GeceHaritasi.Kare("G5") + Vector3.up * 180f);
            Kaydet(cam, "06_bulutlar_ve_huzmeler");

            // 7. Tracers: the aircraft held where F4 catches it near the G4 gun.
            Isildak f4 = Object.FindObjectsByType<Isildak>(FindObjectsInactive.Exclude).Single(i => i.GetComponentInParent<Yikilabilir>().name.EndsWith("F4"));
            Vector3 hedef = GeceHaritasi.Kare("G3") + Vector3.up * 320f;
            Vector3 g4 = GeceHaritasi.UcaksavarKonumu(0);
            // The rounds must not end the mission here: the aircraft's colliders leave the Ucak layer for the picture.
            var katmanlar = ucus.GetComponentsInChildren<Transform>(true).Select(x => (x, x.gameObject.layer)).ToArray();
            foreach (var (x, _) in katmanlar) x.gameObject.layer = 0;
            float t = 0f;
            while (t < 2.5f)
            {
                ucus.transform.SetPositionAndRotation(hedef, Quaternion.Euler(0f, 90f, 0f));
                ucus.HizAyarla(0f);
                ucus.GazAyarla(0.7f);
                f4.Kafa.rotation = Quaternion.LookRotation(hedef - f4.Kafa.position);
                Physics.SyncTransforms();
                t += Time.deltaTime;
                yield return null;
            }
            cam.transform.position = g4 + new Vector3(-160f, 60f, -170f);
            cam.transform.LookAt(Vector3.Lerp(g4, hedef, 0.45f));
            Kaydet(cam, "07_iz_mermileri");
            foreach (var (x, k) in katmanlar) x.gameObject.layer = k;
            ucus.transform.position = GeceHaritasi.PistBasi + Vector3.up * 0.3f + Vector3.up * ucus.GetComponent<InisAlgilayici>().TekerOfseti;
            ucus.enabled = false;
            Physics.SyncTransforms();

            // 8. A bomb on the bridge 100 m east of the middle: the spans at the hit fall, the rest stays; fire at the break.
            Patlama gorsel = Resources.FindObjectsOfTypeAll<Patlama>().FirstOrDefault(p => p.KaliciEfekt != null);
            Assert.IsNotNull(gorsel, "Lab 7 explosion visual");
            Vector3 vurus = kopru + Vector3.right * 100f + Vector3.up * 1f;
            cam.transform.position = kopru + new Vector3(-200f, 70f, -260f);
            cam.transform.LookAt(vurus + Vector3.down * 8f);
            yield return null;
            Patlama.Yarat(vurus, 25f, 1000, LayerMask.GetMask("Hedef"), gorsel);
            yield return Bekle(0.25f);
            Kaydet(cam, "08_kopru_vuruldu");
            yield return Bekle(2f);
            Kaydet(cam, "09_kopru_cokuyor");
            yield return Bekle(7f);
            Kaydet(cam, "10_kopru_yikik_duman");
            Assert.IsTrue(gorev.AnaHedef.Yikik);

            // 9. A tower destroyed.
            Yikilabilir k5 = gorev.Hedefler.First(h => h.Tur == HedefTuru.Kule && h.name.EndsWith("K5"));
            k5.Saglik.Vur(1000);
            yield return Bekle(6f);
            cam.transform.position = kule + new Vector3(80f, 40f, -90f);
            cam.transform.LookAt(kule + Vector3.up * 5f);
            Kaydet(cam, "11_isildak_yikik");

            // 10. Return to base: the landing zone from the east (the approach over the J8 river and past the F7–G7 ridge).
            Vector3 esik = GeceHaritasi.PistSonu;
            cam.transform.position = esik + GeceHaritasi.PistEkseni * 1500f + Vector3.up * 110f;
            cam.transform.LookAt(esik - GeceHaritasi.PistEkseni * 300f);
            Kaydet(cam, "12_usse_donus_yaklasma");

            // 11. Dawn: the last minutes of the night (mission clock moved on).
            gorev.Adim(gorev.SafakSuresi * 0.9f - gorev.GecenSure);
            yield return Bekle(0.6f);
            cam.transform.position = GeceHaritasi.Kare("D6") + Vector3.up * 160f;
            cam.transform.LookAt(GeceHaritasi.Kare("L5") + Vector3.up * 60f);
            Kaydet(cam, "13_safak_yaklasiyor");
            gorev.Adim(gorev.SafakSuresi * 0.995f - gorev.GecenSure);
            yield return Bekle(0.6f);
            Kaydet(cam, "14_safak");

            // 12. The paper map area from above with the fog off (compare with Exports/Lab6/bolum3_harita_v0.1.png).
            RenderSettings.fog = false;
            cam.orthographic = true;
            cam.orthographicSize = 1000f;
            cam.farClipPlane = 6000f;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 3000f, 0f), Quaternion.Euler(90f, 0f, 0f));
            RenderSettings.ambientSkyColor = Color.white * 0.9f;
            RenderSettings.ambientEquatorColor = Color.white * 0.8f;
            RenderSettings.ambientGroundColor = Color.white * 0.6f;
            Kaydet(cam, "15_kagit_alani_ustten", 1600, 1200);
            cam.orthographicSize = 3200f;
            Kaydet(cam, "16_harita_ustten", 1400, 1400);
        }
    }
}
