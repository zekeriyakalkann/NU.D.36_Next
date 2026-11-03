using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 5 visual record (explicit, run on demand): renders the Main Camera of Rota05 from fixed viewpoints into
    /// Logs/lab5/gorsel/*.png (hills, dumps, guns, sound locator, a dive). Overlay HUD is not part of a camera render.
    /// </summary>
    [Explicit("Writes images; run with -testFilter Lab5GorselKayit")]
    public class Lab5GorselKayit
    {
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab5/gorsel"));

        static Transform Bul(string ad) => Object.FindObjectsByType<Transform>().Single(t => t.name == ad);

        static void Kaydet(Camera cam, string ad)
        {
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(Path.Combine(Dir, ad + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
        }

        [UnityTest]
        public IEnumerator Gorunumler()
        {
            yield return SceneManager.LoadSceneAsync("Rota05", LoadSceneMode.Single);
            yield return null;
            var ucus = Object.FindAnyObjectByType<UcusKontrol>();
            Camera cam = Camera.main;
            cam.GetComponent<TakipKamerasi>().enabled = false;
            Object.FindAnyObjectByType<SeviyeSonuEkrani>().SahneYukle = _ => { };

            IEnumerator Bak(Vector3 ucak, Vector3 kamera, Vector3 hedef, string ad)
            {
                ucus.enabled = false;
                ucus.transform.position = ucak;
                for (int i = 0; i < 70; i++)
                    yield return null;
                cam.transform.position = kamera;
                cam.transform.LookAt(hedef);
                Kaydet(cam, ad);
            }

            Vector3 Y(float x, float z, float h) => new Vector3(x, ZeminOlcer.Yukseklik(x, z) + h, z);

            Vector3 t3 = Bul("Tepe 3").position, t2 = Bul("Tepe 2").position, t1 = Bul("Tepe 1").position;
            yield return Bak(Y(1200f, 0f, 300f), Y(900f, -700f, 120f), t3 + Vector3.up * 80f, "01_tepe3_guneybati");
            yield return Bak(Y(400f, 0f, 300f), Y(200f, 100f, 90f), t2 + Vector3.up * 60f, "02_tepe2_ustten");
            yield return Bak(Y(2200f, 400f, 300f), Y(2000f, 300f, 60f), t1 + Vector3.up * 50f, "03_tepe1");
            Vector3 m1 = Bul("Muhimmat Mevzii 1").position;
            yield return Bak(m1 + new Vector3(0f, 300f, 0f), m1 + new Vector3(-40f, 25f, -50f), m1 + Vector3.up * 5f, "04_mevzi1_yakin");
            Vector3 u2 = Bul("Ucaksavar 2").position;
            yield return Bak(u2 + new Vector3(0f, 300f, 0f), u2 + new Vector3(-14f, 7f, -14f), u2 + Vector3.up * 2f, "05_ucaksavar2_yakin");
            Vector3 s = Bul("SesDinleyici").position;
            yield return Bak(s + new Vector3(0f, 300f, 0f), s + new Vector3(8f, 5f, 10f), s + Vector3.up * 4f, "06_sesdinleyici_yakin");
            // From Uçaksavar 2's eye towards Tepe 3: what the gun can (not) see.
            yield return Bak(Y(1100f, 650f, 60f), u2 + Vector3.up * 3f, t3 + Vector3.up * 60f, "07_ucaksavar2_gozu_tepe3");
            // Dive on dump 2 from behind the aircraft, 60 degrees, 200 m.
            Vector3 m2 = Bul("Muhimmat Mevzii 2").position;
            Vector3 ileri = new Vector3(m2.x, 0f, m2.z).normalized;
            Vector3 p = m2 - ileri * Bomba.IleriMenzil(85f, 60f, 200f) + Vector3.up * 200f;
            ucus.transform.rotation = Quaternion.LookRotation(ileri) * Quaternion.Euler(60f, 0f, 0f);
            yield return Bak(p, p - ucus.transform.forward * 14f + ucus.transform.up * 4f, m2, "08_dalis_60_200m");
            // Overview from the base towards the field.
            yield return Bak(Y(0f, 0f, 80f), Y(-150f, -120f, 160f), Y(1600f, 300f, 0f), "09_ussten_saha");
            Assert.Pass();
        }
    }
}
