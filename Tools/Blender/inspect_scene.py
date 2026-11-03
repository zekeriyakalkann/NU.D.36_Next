"""Read-only inspection of the NU.D.36 .blend scene.

Usage:
  blender.exe --background --factory-startup --disable-autoexec <file.blend> --python inspect_scene.py -- <report.json>

Never saves the .blend file. Writes only the JSON report given after "--".
"""
import bpy
import json
import sys


def vec(v):
    return [round(x, 6) for x in v]


def main():
    argv = sys.argv
    out_path = argv[argv.index("--") + 1] if "--" in argv else None

    scene = bpy.context.scene
    us = scene.unit_settings
    report = {
        "blend_file": bpy.data.filepath,
        "blender_version": bpy.app.version_string,
        "scene": scene.name,
        "scenes": [s.name for s in bpy.data.scenes],
        "units": {
            "system": us.system,
            "scale_length": us.scale_length,
            "length_unit": us.length_unit,
        },
        "frame_range": [scene.frame_start, scene.frame_end],
        "objects": [],
        "materials": [],
        "images": [],
        "actions": [a.name for a in bpy.data.actions],
        "collections": [c.name for c in bpy.data.collections],
        "libraries": [lib.filepath for lib in bpy.data.libraries],
    }

    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in scene.objects:
        entry = {
            "name": obj.name,
            "type": obj.type,
            "parent": obj.parent.name if obj.parent else None,
            "collections": [c.name for c in obj.users_collection],
            "location": vec(obj.location),
            "rotation_mode": obj.rotation_mode,
            "rotation_euler": vec(obj.rotation_euler),
            "scale": vec(obj.scale),
            "dimensions": vec(obj.dimensions),
            "hide_render": obj.hide_render,
            "hide_viewport": obj.hide_viewport,
            "visible": obj.visible_get(),
            "modifiers": [{"name": m.name, "type": m.type} for m in obj.modifiers],
            "has_animation": bool(obj.animation_data and obj.animation_data.action),
            "constraints": [c.type for c in obj.constraints],
        }
        if obj.type == "MESH":
            me = obj.data
            entry["mesh"] = {
                "name": me.name,
                "vertices": len(me.vertices),
                "polygons": len(me.polygons),
                "uv_layers": [uv.name for uv in me.uv_layers],
                "materials": [m.name if m else None for m in me.materials],
            }
            ev = obj.evaluated_get(depsgraph)
            em = ev.to_mesh()
            entry["mesh"]["evaluated_vertices"] = len(em.vertices)
            entry["mesh"]["evaluated_polygons"] = len(em.polygons)
            ev.to_mesh_clear()
        report["objects"].append(entry)

    for mat in bpy.data.materials:
        m = {"name": mat.name, "users": mat.users, "use_nodes": mat.use_nodes, "nodes": []}
        if mat.use_nodes and mat.node_tree:
            for n in mat.node_tree.nodes:
                nd = {"type": n.type, "name": n.name}
                if n.type == "BSDF_PRINCIPLED":
                    bc = n.inputs.get("Base Color")
                    if bc is not None:
                        nd["base_color"] = vec(bc.default_value)
                        nd["base_color_linked"] = bc.is_linked
                    for key in ("Metallic", "Roughness", "Alpha"):
                        inp = n.inputs.get(key)
                        if inp is not None:
                            nd[key.lower()] = round(inp.default_value, 4)
                if n.type == "TEX_IMAGE" and n.image:
                    nd["image"] = n.image.name
                m["nodes"].append(nd)
        else:
            m["diffuse_color"] = vec(mat.diffuse_color)
        report["materials"].append(m)

    for img in bpy.data.images:
        report["images"].append({
            "name": img.name,
            "filepath": img.filepath,
            "packed": img.packed_file is not None,
            "source": img.source,
            "size": list(img.size),
        })

    text = json.dumps(report, indent=2)
    if out_path:
        with open(out_path, "w", encoding="utf-8") as f:
            f.write(text)
    print("INSPECT_REPORT_BEGIN")
    print(text)
    print("INSPECT_REPORT_END")


main()
