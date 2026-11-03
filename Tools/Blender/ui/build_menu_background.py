"""Menu background for Lab 4 phase 3: a golden-hour apron with the NU.D.36 aircraft (read from Exports/NU_D_36.fbx).

Usage:
  blender.exe --background --factory-startup --python build_menu_background.py -- <output.png> [blend_dir]
The aircraft FBX is only read. Composition: aircraft in the right two thirds, quiet sky/sea and a soft vignette on the left third
so menu buttons stay readable. Opaque 1920x1080 render (Lab4UiAssets.Specs: menu-arkaplan.png).
"""
import math
import os
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ui_common as U  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
FBX = os.path.join(ROOT, "Exports", "NU_D_36.fbx")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = os.path.abspath(argv[0])
    blend_dir = os.path.abspath(argv[1]) if len(argv) > 1 else None
    sc = U.new_scene(1920, 1080, opaque=True)
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.exposure = float(os.environ.get("NU_EXPOSURE", "-1.0"))

    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=FBX)
    plane = [o for o in bpy.data.objects if o not in before]
    meshes = [o for o in plane if o.type == "MESH"]
    lo = Vector((1e9,) * 3)
    hi = Vector((-1e9,) * 3)
    for o in meshes:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            lo = Vector((min(lo[i], w[i]) for i in range(3)))
            hi = Vector((max(hi[i], w[i]) for i in range(3)))
    size = hi - lo
    print("AIRCRAFT_BOUNDS", tuple(round(v, 2) for v in size), tuple(round(v, 2) for v in lo))

    # root empty to place the aircraft: wheels on the ground, centred on the origin
    root = bpy.data.objects.new("AircraftRoot", None)
    sc.collection.objects.link(root)
    for o in plane:
        if o.parent is None:
            o.parent = root
    root.location = (-(lo.x + hi.x) / 2, -(lo.y + hi.y) / 2, -lo.z)
    bpy.context.view_layer.update()
    span = max(size.x, size.y)
    scale = float(os.environ.get("NU_AIRCRAFT_SCALE", "1"))

    # ground: dark worn concrete apron + sea beyond
    ground_mat = U.material("Concrete", (0.22, 0.18, 0.14), 0.0, 0.8, wear=(0.08, 0.07, 0.06), wear_scale=0.6, bump=0.3, bump_scale=6)
    sea_mat = U.material("Sea", (0.10, 0.20, 0.27), 0.0, 0.22, bump=0.15, bump_scale=0.8)
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, -10, -0.001))
    g = bpy.context.active_object
    g.scale = (1600, 120, 1)
    g.data.materials.append(ground_mat)
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 1050, -1.5))
    sea = bpy.context.active_object
    sea.scale = (6000, 2000, 1)
    sea.data.materials.append(sea_mat)
    hill_mat = U.material("Hill", (0.06, 0.05, 0.07), 0.0, 1.0, bump=0.0)
    for (x, h, w) in ((-1400, 160, 700), (-500, 90, 600), (500, 140, 900), (1500, 200, 800)):
        bpy.ops.mesh.primitive_cone_add(vertices=7, radius1=w, depth=h, location=(x, 2300, h / 2 - 1.5))
        bpy.context.active_object.data.materials.append(hill_mat)

    # golden-hour sky (Nishita) and low sun behind the aircraft, to the right
    sun_dir = Vector((0.12, 0.99, 0.105)).normalized()
    w = sc.world
    nt = w.node_tree
    nt.nodes.clear()
    out_n = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    sky = nt.nodes.new("ShaderNodeTexSky")
    for t in ("MULTIPLE_SCATTERING", "SINGLE_SCATTERING", "NISHITA"):
        try:
            sky.sky_type = t
            break
        except TypeError:
            continue
    sky.sun_elevation = math.radians(float(os.environ.get("NU_SUN_ELEV", "6")))
    sky.sun_rotation = math.radians(-7)
    sky.altitude = 10
    sky.air_density = 1.8
    sky.aerosol_density = 3.0
    sky.sun_disc = True
    sky.sun_size = math.radians(1.5)
    sky.sun_intensity = 0.6
    bg.inputs["Strength"].default_value = 0.5
    nt.links.new(sky.outputs["Color"], bg.inputs["Color"])
    nt.links.new(bg.outputs["Background"], out_n.inputs["Surface"])
    for o in list(bpy.data.objects):
        if o.type == "LIGHT":
            bpy.data.objects.remove(o)
    sun = bpy.data.lights.new("Sun", "SUN")
    sun.energy = 4.5
    sun.color = (1.0, 0.68, 0.38)
    sun.angle = math.radians(2)
    so = bpy.data.objects.new("Sun", sun)
    sc.collection.objects.link(so)
    # light travels from the sun towards the scene: sun is at azimuth 150 deg (world), low
    so.rotation_euler = (-sun_dir).to_track_quat("-Z", "Y").to_euler()

    # camera: low three-quarter view, aircraft in the right two thirds
    cam = sc.camera
    cam.data.type = "PERSP"
    cam.data.lens = 38
    cam.data.sensor_width = 36
    cx, cy = span * 0.85, -span * 1.45
    cam.location = (cx, cy, span * 0.32)
    target = Vector((span * 0.02, 0, span * 0.22))
    direction = target - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    # shift the frame so the aircraft sits right of centre
    cam.data.shift_x = -0.18

    # left-third vignette: soft dark gradient plane in front of the camera
    vg = bpy.data.materials.new("Vignette")
    vg.use_nodes = True
    vnt = vg.node_tree
    vnt.nodes.clear()
    o2 = vnt.nodes.new("ShaderNodeOutputMaterial")
    em = vnt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (0.02, 0.015, 0.012, 1)
    tr = vnt.nodes.new("ShaderNodeBsdfTransparent")
    mixn = vnt.nodes.new("ShaderNodeMixShader")
    tc = vnt.nodes.new("ShaderNodeTexCoord")
    sep = vnt.nodes.new("ShaderNodeSeparateXYZ")
    mr = vnt.nodes.new("ShaderNodeMapRange")
    mr.inputs["From Min"].default_value = 0.0
    mr.inputs["From Max"].default_value = 0.52
    mr.inputs["To Min"].default_value = 0.97
    mr.inputs["To Max"].default_value = 0.0
    mr.interpolation_type = "SMOOTHSTEP"
    mr.clamp = True
    vnt.links.new(tc.outputs["Window"], sep.inputs[0])
    vnt.links.new(sep.outputs["X"], mr.inputs["Value"])
    vnt.links.new(mr.outputs["Result"], mixn.inputs["Fac"])
    vnt.links.new(tr.outputs["BSDF"], mixn.inputs[1])
    vnt.links.new(em.outputs["Emission"], mixn.inputs[2])
    vnt.links.new(mixn.outputs["Shader"], o2.inputs["Surface"])
    try:
        vg.surface_render_method = "BLENDED"
    except Exception:
        vg.blend_method = "BLEND"
    bpy.ops.mesh.primitive_plane_add(size=2)
    vp = bpy.context.active_object
    vp.data.materials.append(vg)
    vp.parent = cam
    dist = 1.0
    vp.location = (0, 0, -dist)
    half_h = dist * math.tan(0.5 * 2 * math.atan(24 / (2 * cam.data.lens)))
    vp.scale = (half_h * 16 / 9 * 2.0, half_h * 2.0, 1)
    vp.visible_shadow = False

    U.render(out)
    if blend_dir:
        # keep the aircraft out of the saved asset: only scene datablocks are written, the FBX stays the source
        for o in plane:
            bpy.data.objects.remove(o)
        U.write_blend(os.path.join(blend_dir, "ui_menu-arkaplan.blend"))
    print("BUILT menu-arkaplan")


main()
