using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NU36.Tests
{
    /// <summary>Lab 6 grey box in Play Mode: the ground and the 120 m hill work with the Lab 5 rays, and a flight runway → bridge → runway is timed.</summary>
    public class Lab6PlayTests
    {
        UcusKontrol ucus;
        Bolum3Kronometre krono;

        static string LogDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab6"));

        IEnumerator Yukle()
        {
            yield return SceneManager.LoadSceneAsync("Bolum3_GriKutu", LoadSceneMode.Single);
            yield return null;
            ucus = Object.FindAnyObjectByType<UcusKontrol>();
            krono = Object.FindAnyObjectByType<Bolum3Kronometre>();
            krono.SahneYukle = _ => { };
            Assert.IsNotNull(ucus);
            Assert.IsNotNull(krono);
        }

        [UnityTest]
        public IEnumerator GriKutu_Starts_OverTheRunwayAt30m_StopwatchRuns()
        {
            yield return Yukle();
            for (int i = 0; i < 10; i++)
                yield return null;
            Assert.That(ZeminOlcer.Irtifa(ucus.transform.position), Is.InRange(25f, 35f), "height above the grey ground plane");
            Assert.Greater(krono.Gecen, 0f);
            Assert.IsTrue(float.IsNaN(krono.KopruSuresi));
            Assert.IsNull(Object.FindAnyObjectByType<SeviyeYoneticisi>(), "the Bölüm 2 level is switched off");
        }

        [UnityTest]
        public IEnumerator Hill120m_CutsAlcak30m_NotOrta200m()
        {
            yield return Yukle();
            Physics.SyncTransforms();
            int maske = LayerMask.GetMask("Arazi");
            Vector3 t = Bolum3Harita.KareMerkezi("E4");
            foreach (var (h, engel) in new[] { (Bolum3Harita.Alcak, true), (Bolum3Harita.Orta, false) })
            {
                Vector3 a = t + new Vector3(-300f, h, 0f), b = t + new Vector3(300f, h, 0f);
                Assert.AreEqual(engel, Radar.GorusEngelliMi(a, b, maske), $"line of sight across the hill at {h} m");
            }
            Assert.That(ZeminOlcer.Yukseklik(t.x, t.z), Is.EqualTo(120f).Within(0.01f), "ground probe sees the hill top");
            Vector3 k = Bolum3Harita.KareMerkezi("I5");
            Assert.That(ZeminOlcer.Yukseklik(k.x, k.z), Is.EqualTo(35f).Within(0.01f), "and the bridge deck (25 m centre + 10 m)");
        }

        /// <summary>
        /// Flies a route with UcusKontrol (throttle 0.6 → 66 m/s) while the test only turns the nose towards the next
        /// waypoint at most <paramref name="donus"/> degrees per second and holds the altitude, out and back.
        /// Returns (bridge time, return time) from the stopwatch.
        /// </summary>
        IEnumerator Uc(string[] yol, float irtifa, float donus, float[] sonuc)
        {
            yield return Yukle();
            Time.timeScale = 4f; // game time is what is measured; this only shortens the test
            try
            {
                var noktalar = yol.Concat(yol.Reverse().Skip(1)).Select(Bolum3Harita.KareMerkezi).ToList();
                ucus.transform.position = noktalar[0] + Vector3.up * irtifa;
                ucus.GazAyarla(0.6f);
                int i = 1;
                float gecen = 0f;
                while (!krono.Bitti && gecen < 400f)
                {
                    Vector3 p = ucus.transform.position;
                    Vector3 hedef = noktalar[Mathf.Min(i, noktalar.Count - 1)];
                    Vector3 fark = new Vector3(hedef.x - p.x, 0f, hedef.z - p.z);
                    if (fark.magnitude < 70f && i < noktalar.Count - 1)
                        i++;
                    float istenen = Mathf.Atan2(fark.x, fark.z) * Mathf.Rad2Deg;
                    float yaw = Mathf.MoveTowardsAngle(ucus.transform.eulerAngles.y, istenen, donus * Time.deltaTime);
                    ucus.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    ucus.transform.position = new Vector3(p.x, irtifa, p.z);
                    gecen += Time.deltaTime;
                    yield return null;
                }
            }
            finally
            {
                Time.timeScale = 1f;
            }
            Assert.IsTrue(ucus.enabled, "no crash on the way");
            Assert.IsTrue(krono.Bitti, "back over the runway");
            sonuc[0] = krono.KopruSuresi;
            sonuc[1] = krono.DonusSuresi;
        }

        [UnityTest]
        public IEnumerator Sure_PistKopruPist_IkiYol_KagitTuruylaKarsilastir()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Lab 6 Step 05: runway → bridge → runway in Bolum3_GriKutu, {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine("Flight: UcusKontrol at throttle 0.6 (≈66 m/s), the test turns the nose ≤ 30°/s (≈3.6 g at 66 m/s) towards each route square in turn (within 70 m = reached) and holds the altitude.");
            sb.AppendLine("Paper: 1 square per turn assumed (the kit's movement rules were not available), 1 turn = 15 s.");
            foreach (var (ad, yol, irtifa) in new[] { ("Yol 1 tepe arkası", Bolum3Harita.Yol1TepeArkasi, 60f), ("Yol 2 açık", Bolum3Harita.Yol2Acik, Bolum3Harita.Orta) })
            {
                var s = new float[2];
                yield return Uc(yol, irtifa, 30f, s);
                int hamle = (yol.Length - 1) * 2;
                float kagit = hamle * Bolum3Harita.TurSuresi;
                sb.AppendLine($"{ad} ({string.Join("-", yol)}), {irtifa:F0} m: bridge {s[0]:F1} s, back {s[1]:F1} s = {Bolum3Kronometre.TurKarsiligi(s[1]):F1} turns; " +
                              $"route {Bolum3Harita.YolUzunlugu(yol) * 2f:F0} m out and back; paper {hamle} moves = {hamle} turns = {kagit:F0} s; screen/paper = {s[1] / kagit:F2}");
                Assert.Greater(s[1], s[0]);
            }
            Directory.CreateDirectory(LogDir);
            File.WriteAllText(Path.Combine(LogDir, "sure_olcumu.txt"), sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
