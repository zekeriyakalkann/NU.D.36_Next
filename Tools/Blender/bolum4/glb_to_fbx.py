# Bölüm 4: converts a downloaded GLB model to FBX for Unity (the project has no glTF importer package).
# Never saves a .blend. Usage:
#   blender --background --factory-startup --disable-autoexec --python glb_to_fbx.py -- <in.glb> <out.fbx>
import sys
import bpy

args = sys.argv[sys.argv.index("--") + 1:]
src, dst = args[0], args[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)

for o in bpy.data.objects:
    print("OBJ", o.type, o.name, tuple(round(v, 3) for v in o.dimensions))
for m in bpy.data.materials:
    col = None
    if m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type == "BSDF_PRINCIPLED":
                col = tuple(round(c, 3) for c in n.inputs["Base Color"].default_value)
    print("MAT", m.name, col)
for img in bpy.data.images:
    print("IMG", img.name, tuple(img.size))

bpy.ops.export_scene.fbx(filepath=dst, use_selection=False, object_types={"MESH", "EMPTY"},
                         apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", bake_space_transform=False,
                         axis_forward="-Z", axis_up="Y", path_mode="COPY", embed_textures=True, mesh_smooth_type="FACE")
print("WROTE", dst)
