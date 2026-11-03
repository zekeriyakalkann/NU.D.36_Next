using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>The ten Lab 4 UI textures: required names, sizes and transparency (Phase 3).</summary>
    public static class Lab4UiAssets
    {
        public const string Folder = "Assets/UI";

        public readonly struct Spec
        {
            public readonly string name;
            public readonly int width, height;
            /// <summary>The handout asks for transparent pixels (otherwise the asset may or may not have alpha).</summary>
            public readonly bool needsAlpha;
            /// <summary>The handout asks for a fully opaque image.</summary>
            public readonly bool opaque;
            public readonly bool repeat;

            public Spec(string name, int width, int height, bool needsAlpha = false, bool opaque = false, bool repeat = false)
            {
                this.name = name; this.width = width; this.height = height;
                this.needsAlpha = needsAlpha; this.opaque = opaque; this.repeat = repeat;
            }
        }

        public static readonly Spec[] Specs =
        {
            new Spec("hud-cerceve.png", 1920, 1080, needsAlpha: true),
            new Spec("gosterge-kadranlari.png", 1024, 512),
            new Spec("gosterge-ibre.png", 512, 512, needsAlpha: true),
            new Spec("gaz-kolu-plakasi.png", 256, 1024),
            new Spec("gaz-kolu-topuz.png", 512, 512, needsAlpha: true),
            new Spec("pusula-seridi.png", 1536, 128, repeat: true),
            new Spec("hedef-isaretleri.png", 1024, 1024, needsAlpha: true),
            new Spec("dugme-seti.png", 768, 1024),
            new Spec("puan-tablosu.png", 1024, 1280, needsAlpha: true),
            new Spec("menu-arkaplan.png", 1920, 1080, opaque: true),
        };

        public static bool TryGetSpec(string assetPath, out Spec spec)
        {
            string normal = assetPath.Replace('\\', '/');
            spec = default;
            if (Path.GetDirectoryName(normal)?.Replace('\\', '/') != Folder)
                return false;
            string file = Path.GetFileName(normal);
            foreach (Spec s in Specs)
                if (s.name == file)
                {
                    spec = s;
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Batch/menu report: for every spec, whether the file exists, its size, alpha range and importer settings.
        /// Writes Logs/lab4_ui_validation.txt and changes nothing.
        /// </summary>
        [MenuItem("NU36/Lab 4/Validate UI Assets")]
        public static void Validate()
        {
            var sb = new StringBuilder("file | exists | size (expected) | alpha min | importer\n");
            int problems = 0;
            foreach (Spec s in Specs)
            {
                string path = $"{Folder}/{s.name}";
                if (!File.Exists(path))
                {
                    sb.AppendLine($"{s.name} | MISSING | - | - | -");
                    problems++;
                    continue;
                }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                bool ok = tex.LoadImage(File.ReadAllBytes(path));
                byte minAlpha = 255;
                if (ok)
                    foreach (Color32 c in tex.GetPixels32())
                        if (c.a < minAlpha) minAlpha = c.a;
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                string importer = imp == null ? "no TextureImporter" :
                    $"{imp.textureType}, compression {imp.textureCompression}, mips {imp.mipmapEnabled}, wrap {imp.wrapMode}";
                bool sizeOk = ok && tex.width == s.width && tex.height == s.height;
                bool alphaOk = !ok || (s.needsAlpha ? minAlpha < 255 : !s.opaque || minAlpha == 255);
                bool importOk = imp != null && imp.textureType == TextureImporterType.Sprite && !imp.mipmapEnabled
                    && imp.textureCompression == TextureImporterCompression.Uncompressed
                    && imp.wrapMode == (s.repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp);
                if (!ok || !sizeOk || !alphaOk || !importOk)
                    problems++;
                sb.AppendLine($"{s.name} | {(ok ? "ok" : "UNREADABLE")} | {(ok ? tex.width + "x" + tex.height : "-")} ({s.width}x{s.height}){(sizeOk ? "" : " MISMATCH")} | {minAlpha}{(alphaOk ? "" : " ALPHA?")} | {importer}{(importOk ? "" : " IMPORT?")}");
                Object.DestroyImmediate(tex);
            }
            sb.AppendLine($"problems: {problems}");
            string log = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab4_ui_validation.txt"));
            File.WriteAllText(log, sb.ToString());
            Debug.Log($"[NU36 Lab4 UI]\n{sb}");
            if (Application.isBatchMode)
                EditorApplication.Exit(problems == 0 ? 0 : 1);
        }
    }

    /// <summary>
    /// Applies the Lab 4 import settings to the ten UI textures in Assets/UI when they are imported:
    /// Sprite (2D and UI), no compression, no mip maps, Clamp (Repeat for the compass ribbon).
    /// Other textures and project-wide settings are untouched.
    /// </summary>
    public class Lab4UiTexturePostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!Lab4UiAssets.TryGetSpec(assetPath, out Lab4UiAssets.Spec spec))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = spec.repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Max(spec.width, spec.height) > 2048 ? 4096 : 2048;
        }
    }
}
