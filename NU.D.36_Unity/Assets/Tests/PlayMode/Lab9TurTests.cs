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
    /// Lab 9 (Hafta 9) TEST-912: one whole round of Bölüm 5 at 4× speed with the game's assists (user preference: easy flying,
    /// easy hitting). A scripted pilot puts the aircraft every frame 450 m behind the ship nearest to the beach, 120 m over the
    /// sea, nose on it; the targeting assist of the game does the shooting (machine gun in its cone, guided bombs within range).
    /// Like Lab 8's round test (D-121) this measures the mission loop and the assisted balance, not flying. The aircraft's
    /// health is raised so a lucky fighter cannot end the measurement; the damage taken is reported.
    /// Measurements → Logs/lab9/tur_olcumu.txt.
    /// </summary>
    public class Lab9TurTests
    {
        [TearDown]
        public void Bitir() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator FullRound_WithTheAssists_AllTwelveShipsCome_TheCoastHolds()
        {
            yield return SceneManager.LoadSceneAsync("Bolum5_KartalGecidi", LoadSceneMode.Single);
            var ucus = Object.FindAnyObjectByType<UcusKontrol>();
            var filo = Object.FindAnyObjectByType<CikarmaFilosu>();
            var koruma = Object.FindAnyObjectByType<KorumaUcuslari>();
            var gorev = Object.FindAnyObjectByType<KartalGecidiGorevi>();
            var nisan = ucus.GetComponent<Bolum4Nisan>();
            var mk = ucus.GetComponent<Makineli>();
            var yuk = ucus.GetComponent<BombaYuku>();
            var can = ucus.GetComponent<Saglik>();
            gorev.SahneYukle = _ => { };
            if (ucus.TryGetComponent(out InisAlgilayici a)) a.enabled = false;
            can.BaslangicAyarla(100000);
            Assert.IsTrue(nisan.Yardim, "assists on at the start");
            yield return null;

            Time.timeScale = 4f;
            float bitis = Time.time + 900f;
            float enCokTehdit = 0f;
            while (!gorev.Bitti && Time.time < bitis)
            {
                CikarmaGemisi hedef = filo.Gemiler.Where(g => g != null && g.Tehdit).OrderBy(g => g.KalanMesafe).FirstOrDefault();
                enCokTehdit = Mathf.Max(enCokTehdit, filo.TehditSayisi);
                Vector3 bak = hedef != null ? hedef.transform.position + Vector3.up * 2.5f : H.KoyMerkezi;
                Vector3 konum = hedef != null ? hedef.transform.position - H.KiyiyaDogru * 450f + Vector3.up * 120f : H.KoyMerkezi + new Vector3(600f, 200f, 0f);
                ucus.transform.SetPositionAndRotation(konum, Quaternion.LookRotation(bak - konum));
                ucus.HizAyarla(0f);
                ucus.Yunuslama = ucus.Yatis = ucus.Sapma = 0f;
                Physics.SyncTransforms();
                yield return null;
            }
            Time.timeScale = 1f;

            string satir = $"TEST-912 full round (assists, scripted pilot, 4×): {gorev.Simdiki} at {Bolum4Hud.SureYazisi(gorev.Sure)}; " +
                           $"ships {filo.Gelen}/12 came, {filo.Durdurulan} stopped, {filo.Bosaltan} unloaded; tanks ashore {filo.KaradakiTank}/12; " +
                           $"rounds used {mk.Kapasite - mk.Kalan}, bombs used {yuk.Kapasite - yuk.Kalan} ({nisan.OtomatikBomba} released by the assist); " +
                           $"guard fighters shot down {koruma.Dusurulen}, sent {koruma.Gonderilen}/{koruma.Toplam}; damage taken {100000 - can.Can}; " +
                           $"at most {enCokTehdit:F0} ships heading for the beach at once";
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab9"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "tur_olcumu.txt"), $"Lab 9 full round, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n{satir}\n");
            Debug.Log(satir);

            Assert.AreEqual(KartalGecidiGorevi.Asama.Tamam, gorev.Simdiki, satir);
            Assert.AreEqual(12, filo.Gelen);
            Assert.Less(filo.KaradakiTank, 12);
        }
    }
}
