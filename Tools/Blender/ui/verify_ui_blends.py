"""Opens each ui_*.blend in Blender (read only) and reports scene, object and material counts. Usage: blender --background --python verify_ui_blends.py -- <blend_dir>"""
import glob
import os
import sys

import bpy

d = sys.argv[sys.argv.index("--") + 1]
bad = 0
for f in sorted(glob.glob(os.path.join(d, "ui_*.blend"))):
    bpy.ops.wm.open_mainfile(filepath=f)
    n_obj, n_mat = len(bpy.data.objects), len(bpy.data.materials)
    ok = n_obj > 0 and bpy.context.scene.camera is not None
    bad += 0 if ok else 1
    print("BLEND", os.path.basename(f), "scenes", len(bpy.data.scenes), "objects", n_obj, "materials", n_mat, "camera", bpy.context.scene.camera is not None, "OK" if ok else "PROBLEM")
print("BLEND_PROBLEMS", bad)
