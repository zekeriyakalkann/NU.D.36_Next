using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NU36.Tests
{
    /// <summary>
    /// Lab 8 (Hafta 8) worksheet step 01: the downloaded assets are inside Assets/Bolum4 (TEST-801) and every one has a row
    /// in Assets/VARLIKLAR.md with its source link, licence and whether it was changed, the author for CC BY (TEST-803).
    /// Sizes (TEST-802) are checked in Lab8Tests (locomotive, wagons) and Lab8DusmanTests (tank, enemy aircraft).
    /// </summary>
    public class Lab8VarlikTests
    {
        const string Kok = "Assets/Bolum4";
        static string VarliklarYolu => Path.Combine(Application.dataPath, "VARLIKLAR.md");

        static string[] Dosyalar(string klasor, string filtre) =>
            AssetDatabase.FindAssets(filtre, new[] { klasor }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

        [Test]
        public void Assets_FiveKindsInsideBolum4()
        {
            string[] tren = Dosyalar($"{Kok}/Modeller/Tren", "t:GameObject");
            Assert.IsTrue(tren.Any(p => p.Contains("Locomotive_Front")), "locomotive");
            Assert.GreaterOrEqual(tren.Count(p => !p.Contains("Locomotive_Front") && !p.Contains("CoalTender")), 2, "wagon models (4 wagons from 3 models)");
            Assert.IsNotEmpty(Dosyalar($"{Kok}/Modeller/Ray", "t:GameObject"), "rail");
            Assert.IsNotEmpty(Dosyalar($"{Kok}/Dokular", "t:Texture2D"), "terrain surface textures");
            Assert.IsNotEmpty(Dosyalar($"{Kok}/Modeller/Tank", "t:GameObject"), "tank");
            Assert.IsNotEmpty(Dosyalar($"{Kok}/Modeller/DusmanUcagi", "t:GameObject"), "enemy aircraft");
            // Nothing from the download folder outside Assets/Bolum4.
            foreach (string ad in new[] { "Locomotive_Front", "DusmanUcagi_Doku", "RailwayTrack_Straight", "cliff_side_diff_2k", "PineTree_1" })
                Assert.IsTrue(AssetDatabase.FindAssets(ad).Select(AssetDatabase.GUIDToAssetPath).Where(p => !p.EndsWith(".cs") && !p.EndsWith(".prefab") && !p.EndsWith(".mat"))
                    .All(p => p.StartsWith(Kok + "/")), $"{ad} only inside {Kok}");
        }

        [Test]
        public void Register_OneRowPerAssetFolder_SourceLicenceChanged_AuthorForCcBy()
        {
            Assert.IsTrue(File.Exists(VarliklarYolu), "Assets/VARLIKLAR.md exists");
            string metin = File.ReadAllText(VarliklarYolu);
            string[] satirlar = metin.Split('\n').Where(s => s.StartsWith("|") && !s.StartsWith("|---") && !s.StartsWith("| Varlık")).ToArray();
            Assert.GreaterOrEqual(satirlar.Length, 6);

            foreach (string klasor in AssetDatabase.GetSubFolders($"{Kok}/Modeller").Append($"{Kok}/Dokular"))
            {
                string kisa = klasor.Substring("Assets/".Length);
                Assert.IsTrue(satirlar.Any(s => s.Contains(kisa)), $"a row for {kisa}");
            }
            foreach (string s in satirlar)
            {
                string[] sutun = s.Split('|').Select(x => x.Trim()).ToArray();   // "", varlık, yer, kaynak, lisans, değişti, ""
                Assert.GreaterOrEqual(sutun.Length, 6, s);
                StringAssert.Contains("http", sutun[3], $"source link: {sutun[1]}");
                Assert.IsNotEmpty(sutun[4], $"licence: {sutun[1]}");
                Assert.IsTrue(sutun[5].StartsWith("Evet") || sutun[5].StartsWith("Hayır"), $"changed yes/no: {sutun[1]}");
                if (sutun[4].Contains("CC BY"))
                    StringAssert.Contains("Yazar", sutun[4], $"author named for CC BY: {sutun[1]}");
            }
        }
    }
}
