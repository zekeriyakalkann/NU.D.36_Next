using UnityEditor;

namespace NU36.Editor
{
    /// <summary>
    /// Applies the NU.D.36 aircraft FBX import settings from code so they are
    /// reproducible and reviewable instead of living only in the .meta file.
    /// Settings changed in the Inspector for this asset are overwritten on reimport.
    /// </summary>
    public class AircraftModelPostprocessor : AssetPostprocessor
    {
        public const string AircraftModelPath = "Assets/Models/NU_D_36.fbx";

        void OnPreprocessModel()
        {
            if (assetPath != AircraftModelPath)
                return;

            var importer = (ModelImporter)assetImporter;

            // Exported from Blender in metres with FBX_SCALE_ALL: 1 file unit = 1 Unity unit.
            importer.globalScale = 1f;
            importer.useFileScale = true;
            // Axis conversion is already baked into the geometry by the Blender exporter.
            importer.bakeAxisConversion = false;

            // The export contains meshes only; keep the importer from adding anything else.
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.addCollider = false;

            importer.importNormals = ModelImporterNormals.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }
    }
}
