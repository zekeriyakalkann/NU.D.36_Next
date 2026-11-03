"""Export the NU.D.36 aircraft meshes to FBX for Unity.

Usage:
  blender.exe --background --factory-startup --disable-autoexec <file.blend> --python export_fbx.py -- <output.fbx>

Exports only MESH objects from the "NU.D.36" collection (reference empties,
cameras and lights are excluded). Never saves the .blend file: selection
changes exist only in memory and are discarded when Blender quits.
"""
import bpy
import os
import sys

AIRCRAFT_COLLECTION = "NU.D.36"


def main():
    argv = sys.argv
    if "--" not in argv:
        raise SystemExit("Output path required after '--'")
    out_path = os.path.abspath(argv[argv.index("--") + 1])
    if os.path.abspath(out_path) == os.path.abspath(bpy.data.filepath):
        raise SystemExit("Refusing to write over the source file")

    coll = bpy.data.collections.get(AIRCRAFT_COLLECTION)
    if coll is None:
        raise SystemExit(f"Collection '{AIRCRAFT_COLLECTION}' not found")

    meshes = [o for o in coll.all_objects if o.type == "MESH"]
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    result = bpy.ops.export_scene.fbx(
        filepath=out_path,
        use_selection=True,
        object_types={"MESH"},
        # Blender is Z-up, -Y forward for this model; Unity is Y-up, +Z forward.
        # -Z forward / Y up maps the nose (Blender -Y) to Unity +Z after Unity's
        # X-flip. Baking the conversion keeps object rotations at identity.
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        # Scene is metric with scale_length 1.0: 1 Blender unit = 1 m = 1 Unity unit.
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_tspace=False,  # meshes have no UV layers; tangents cannot be computed
        use_custom_props=False,
        add_leaf_bones=False,
        bake_anim=False,  # the file contains no actions
        path_mode="AUTO",
        embed_textures=False,
    )
    print(f"EXPORT_RESULT {result} objects={len(meshes)} path={out_path}")
    if result != {"FINISHED"}:
        raise SystemExit(1)


main()
