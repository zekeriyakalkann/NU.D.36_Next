"""Re-import an exported FBX into an empty scene and report mesh statistics.

Usage:
  blender.exe --background --factory-startup --python verify_fbx.py -- <input.fbx> <report.json>

Runs without opening any .blend file and never saves.
"""
import bpy
import json
import sys
from mathutils import Vector


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    fbx_path, out_path = argv[0], argv[1]

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx_path)

    objects = []
    lo = Vector((float("inf"),) * 3)
    hi = Vector((float("-inf"),) * 3)
    for o in bpy.context.scene.objects:
        entry = {"name": o.name, "type": o.type}
        if o.type == "MESH":
            entry["vertices"] = len(o.data.vertices)
            entry["polygons"] = len(o.data.polygons)
            entry["materials"] = [m.name if m else None for m in o.data.materials]
            for corner in o.bound_box:
                w = o.matrix_world @ Vector(corner)
                lo = Vector(map(min, lo, w))
                hi = Vector(map(max, hi, w))
        objects.append(entry)

    report = {
        "objects": objects,
        "mesh_count": sum(1 for o in objects if o["type"] == "MESH"),
        "bounds_min": list(lo),
        "bounds_max": list(hi),
    }
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    print(f"VERIFY_DONE meshes={report['mesh_count']}")


main()
