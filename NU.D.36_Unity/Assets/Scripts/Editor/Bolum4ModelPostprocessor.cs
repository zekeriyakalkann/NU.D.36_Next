using UnityEditor;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8 (Bölüm 4): import settings for the downloaded assets under <c>Assets/Bolum4</c>. Models: no cameras, lights or
    /// animation (the tank is turned by code); the rail is readable because <see cref="Lab8Varliklar"/> bends it along the line.
    /// Textures: Poly Haven normal maps (OpenGL convention, "nor_gl") as normal maps, roughness maps linear.
    /// </summary>
    public class Bolum4ModelPostprocessor : AssetPostprocessor
    {
        public const string Kok = "Assets/Bolum4/";
        public const string ModelKoku = Kok + "Modeller/";
        public const string DokuKoku = Kok + "Dokular/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelKoku))
                return;
            var mi = (ModelImporter)assetImporter;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.isReadable = assetPath.Contains("/Ray/");
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }

        /// <summary>Lab 9: Bölüm 5's downloaded textures (Poly Haven sand) get the same settings.</summary>
        public const string Bolum5DokuKoku = "Assets/Bolum5/Dokular/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Kok) && !assetPath.StartsWith(Bolum5DokuKoku))
                return;
            var ti = (TextureImporter)assetImporter;
            if (assetPath.Contains("_nor_gl_"))
                ti.textureType = TextureImporterType.NormalMap;
            else if (assetPath.Contains("_rough_"))
                ti.sRGBTexture = false;
            ti.maxTextureSize = 2048;
        }
    }
}
