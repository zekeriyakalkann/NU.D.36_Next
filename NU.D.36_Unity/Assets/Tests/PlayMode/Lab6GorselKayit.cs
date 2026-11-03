using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>Lab 6 visual record (explicit, run on demand): camera renders of the grey box into Logs/lab6/gorsel/*.png.</summary>
    [Explicit("Writes images; run with -testFilter Lab6GorselKayit")]
    public class Lab6GorselKayit
    {
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab6/gorsel"));

        static void Kaydet(Camera cam, string ad, int w = 1600, int h = 900)
        {
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
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
            yield return SceneManager.LoadSceneAsync("Bolum3_GriKutu", LoadSceneMode.Single);
            yield return null;
            var ucus = Object.FindAnyObjectByType<UcusKontrol>();
            ucus.enabled = false;
            Camera cam = Camera.main;
            cam.GetComponent<TakipKamerasi>().enabled = false;
            yield return null;

            // Top-down plan, the paper map's view.
            cam.orthographic = true;
            cam.orthographicSize = 1000f;
            cam.transform.SetPositionAndRotation(Bolum3Harita.Merkez + Vector3.up * 1500f, Quaternion.Euler(90f, 0f, 0f));
            Kaydet(cam, "01_ustten_plan", 1600, 1200);
            cam.orthographic = false;

            Vector3 pist = Bolum3Harita.KareMerkezi("C8"), kopru = Bolum3Harita.KareMerkezi("I5");
            cam.transform.position = pist + new Vector3(-250f, 180f, -250f);
            cam.transform.LookAt(kopru + Vector3.up * 40f);
            Kaydet(cam, "02_pistten_kopruye");

            cam.transform.position = Bolum3Harita.KareMerkezi("F8") + new Vector3(0f, 60f, -150f);
            cam.transform.LookAt(Bolum3Harita.KareMerkezi("G4") + Vector3.up * 30f);
            Kaydet(cam, "03_yol1_tepe_arkasindan_U1e");

            cam.transform.position = kopru + new Vector3(-500f, 260f, -420f);
            cam.transform.LookAt(kopru + Vector3.up * 25f);
            Kaydet(cam, "04_kopru_ve_isiklar");
            Assert.Pass();
        }
    }
}
