using System.Linq;
using NU36.Editor;
using NUnit.Framework;
using UnityEngine;

namespace NU36.Tests
{
    /// <summary>Lab 4 phase 3: the asset specification table and the import rule (no image files needed).</summary>
    public class Lab4UiAssetSpecTests
    {
        [Test]
        public void TenAssets_WithTheRequiredNamesAndCanvasSizes()
        {
            var expected = new (string, int, int)[]
            {
                ("hud-cerceve.png", 1920, 1080), ("gosterge-kadranlari.png", 1024, 512), ("gosterge-ibre.png", 512, 512),
                ("gaz-kolu-plakasi.png", 256, 1024), ("gaz-kolu-topuz.png", 512, 512), ("pusula-seridi.png", 1536, 128),
                ("hedef-isaretleri.png", 1024, 1024), ("dugme-seti.png", 768, 1024), ("puan-tablosu.png", 1024, 1280),
                ("menu-arkaplan.png", 1920, 1080),
            };
            CollectionAssert.AreEqual(expected, Lab4UiAssets.Specs.Select(s => (s.name, s.width, s.height)).ToArray());
        }

        [Test]
        public void OnlyTheCompassRibbonRepeats_AndOnlyTheMenuBackgroundIsOpaque()
        {
            Assert.AreEqual(new[] { "pusula-seridi.png" }, Lab4UiAssets.Specs.Where(s => s.repeat).Select(s => s.name).ToArray());
            Assert.AreEqual(new[] { "menu-arkaplan.png" }, Lab4UiAssets.Specs.Where(s => s.opaque).Select(s => s.name).ToArray());
        }

        [Test]
        public void ImportRule_MatchesOnlyTheTenFilesInAssetsUi()
        {
            Assert.IsTrue(Lab4UiAssets.TryGetSpec("Assets/UI/pusula-seridi.png", out var s));
            Assert.IsTrue(s.repeat);
            Assert.IsFalse(Lab4UiAssets.TryGetSpec("Assets/UI/other.png", out _));
            Assert.IsFalse(Lab4UiAssets.TryGetSpec("Assets/Models/hud-cerceve.png", out _));
            Assert.IsFalse(Lab4UiAssets.TryGetSpec("Assets/UI/Sub/hud-cerceve.png", out _));
        }
    }
}
