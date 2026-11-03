"""Shared helpers for the NU.D.36 steampunk UI sprites (Lab 4 phase 3).

Design coordinates are screen pixels of the output image (origin top-left, y down). One pixel is 0.01 Blender units.
The camera is orthographic, looks down -Z and renders with a transparent film, so every sprite gets a real alpha channel.
Never saves the aircraft file; assets are written with bpy.data.libraries.write (no save_as_mainfile).
"""
import math
import os

import bmesh
import bpy
from mathutils import Vector

PX = 0.01
_ctx = {"W": 1024, "H": 1024}


def P(x, y, z=0.0):
    """Screen pixel -> world position."""
    return Vector(((x - _ctx["W"] / 2) * PX, (_ctx["H"] / 2 - y) * PX, z))


# ---------------------------------------------------------------- scene
def new_scene(W, H, opaque=False, sun=2.2, fill=0.55, shadows=True, engine="EEVEE"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _ctx["W"], _ctx["H"] = W, H
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = W, H
    sc.render.resolution_percentage = int(os.environ.get("UI_PERCENT", "100"))
    sc.render.film_transparent = not opaque
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB" if opaque else "RGBA"
    sc.render.image_settings.color_depth = "8"
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    if engine == "CYCLES":
        sc.render.engine = "CYCLES"
        sc.cycles.device = "CPU"
        sc.cycles.samples = int(os.environ.get("UI_SAMPLES", "96"))
        sc.cycles.use_denoising = True
    else:
        try:
            sc.render.engine = "BLENDER_EEVEE"
        except TypeError:
            sc.render.engine = "BLENDER_EEVEE_NEXT"
        try:
            sc.eevee.taa_render_samples = 64
        except Exception:
            pass
    cam = bpy.data.cameras.new("Cam")
    cam.type = "ORTHO"
    cam.ortho_scale = max(W, H) * PX
    cam.clip_start, cam.clip_end = 0.1, 50
    co = bpy.data.objects.new("Cam", cam)
    sc.collection.objects.link(co)
    co.location = (0, 0, 10)
    sc.camera = co
    # key light from the upper left, soft
    sl = bpy.data.lights.new("Key", "SUN")
    sl.energy = sun
    sl.angle = math.radians(18)
    sl.use_shadow = shadows
    so = bpy.data.objects.new("Key", sl)
    sc.collection.objects.link(so)
    so.rotation_euler = (math.radians(38), math.radians(-32), math.radians(15))
    # world: warm top, dark bottom gradient for metal reflections (invisible: film is transparent)
    w = bpy.data.worlds.new("W")
    w.use_nodes = True
    nt = w.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Strength"].default_value = fill
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.02, 0.016, 0.014, 1)
    ramp.color_ramp.elements[1].color = (1.0, 0.86, 0.62, 1)
    tc = nt.nodes.new("ShaderNodeTexCoord")
    dot = nt.nodes.new("ShaderNodeVectorMath")
    dot.operation = "DOT_PRODUCT"
    dot.inputs[1].default_value = (-0.55, 0.65, 0.5)
    mp = nt.nodes.new("ShaderNodeMapRange")
    mp.inputs["From Min"].default_value = -0.6
    mp.inputs["From Max"].default_value = 0.9
    nt.links.new(tc.outputs["Generated"], dot.inputs[0])
    nt.links.new(dot.outputs["Value"], mp.inputs["Value"])
    nt.links.new(mp.outputs["Result"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bg.inputs["Color"])
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])
    sc.world = w
    return sc


def render(path):
    sc = bpy.context.scene
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


def write_blend(path):
    """Writes the scene as a .blend library without save_as_mainfile."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.data.libraries.write(path, {bpy.context.scene}, fake_user=True)


# ---------------------------------------------------------------- materials
def _noise(nt, scale, detail=6.0):
    n = nt.nodes.new("ShaderNodeTexNoise")
    n.inputs["Scale"].default_value = scale
    n.inputs["Detail"].default_value = detail
    tc = nt.nodes.new("ShaderNodeTexCoord")
    nt.links.new(tc.outputs["Object"], n.inputs["Vector"])
    return n


def material(name, color, metallic=0.0, rough=0.5, wear=None, wear_scale=45.0, bump=0.15, bump_scale=300.0, emit=None, mix=0.4):
    """Principled material with optional worn/dirty colour variation (wear = darker colour) and micro bump."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = rough
    if wear is not None:
        n = _noise(nt, wear_scale)
        ramp = nt.nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.35
        ramp.color_ramp.elements[1].position = 0.65
        wc = tuple(color[i] * (1 - mix) + wear[i] * mix for i in range(3))
        ramp.color_ramp.elements[0].color = (*wc, 1)
        ramp.color_ramp.elements[1].color = (*color, 1)
        nt.links.new(n.outputs["Fac"], ramp.inputs["Fac"])
        nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
        rmp = nt.nodes.new("ShaderNodeMapRange")
        rmp.inputs["To Min"].default_value = min(1.0, rough + 0.25)
        rmp.inputs["To Max"].default_value = max(0.05, rough - 0.12)
        nt.links.new(n.outputs["Fac"], rmp.inputs["Value"])
        nt.links.new(rmp.outputs["Result"], bsdf.inputs["Roughness"])
    else:
        bsdf.inputs["Base Color"].default_value = (*color, 1)
    if bump > 0:
        n2 = _noise(nt, bump_scale, 8.0)
        b = nt.nodes.new("ShaderNodeBump")
        b.inputs["Strength"].default_value = bump
        b.inputs["Distance"].default_value = 0.002
        nt.links.new(n2.outputs["Fac"], b.inputs["Height"])
        nt.links.new(b.outputs["Normal"], bsdf.inputs["Normal"])
    if emit is not None:
        bsdf.inputs["Emission Color"].default_value = (*emit[:3], 1)
        bsdf.inputs["Emission Strength"].default_value = emit[3]
    return m


def wood(name="Wood", base=(0.17, 0.085, 0.04)):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.45
    tc = nt.nodes.new("ShaderNodeTexCoord")
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (6, 90, 6)
    wv = nt.nodes.new("ShaderNodeTexWave")
    wv.inputs["Scale"].default_value = 3.0
    wv.inputs["Distortion"].default_value = 6.0
    wv.inputs["Detail"].default_value = 3.0
    nt.links.new(tc.outputs["Object"], mp.inputs["Vector"])
    nt.links.new(mp.outputs["Vector"], wv.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (base[0] * 0.45, base[1] * 0.45, base[2] * 0.45, 1)
    ramp.color_ramp.elements[1].color = (*base, 1)
    nt.links.new(wv.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    return m


def standard_materials():
    return {
        "brass": material("Brass", (0.74, 0.51, 0.20), 1.0, 0.34, wear=(0.20, 0.11, 0.04)),
        "brass_bright": material("BrassBright", (0.95, 0.72, 0.30), 1.0, 0.25, wear=(0.45, 0.28, 0.10), bump=0.05),
        "steel": material("Steel", (0.15, 0.16, 0.15), 0.85, 0.48, wear=(0.06, 0.06, 0.055)),
        "olive": material("OliveSteel", (0.16, 0.19, 0.12), 0.55, 0.55, wear=(0.07, 0.08, 0.05)),
        "leather": material("Leather", (0.075, 0.042, 0.03), 0.0, 0.5, wear=(0.03, 0.017, 0.012), wear_scale=18, bump=0.5, bump_scale=140),
        "wood": wood(),
        "paper": material("Paper", (0.62, 0.50, 0.31), 0.0, 0.85, wear=(0.40, 0.31, 0.18), wear_scale=9, bump=0.05),
        "ink": material("Ink", (0.035, 0.028, 0.022), 0.0, 0.85, bump=0.0),
        "cream": material("Cream", (0.78, 0.68, 0.45), 0.0, 0.7, bump=0.05),
        "red": material("RedLacquer", (0.60, 0.045, 0.03), 0.0, 0.22, wear=(0.30, 0.03, 0.02), wear_scale=60, bump=0.05),
        "dark": material("DarkRecess", (0.012, 0.011, 0.010), 0.3, 0.7, bump=0.0),
        "grey": material("DisabledGrey", (0.22, 0.22, 0.21), 0.5, 0.6, wear=(0.12, 0.12, 0.12)),
    }


# ---------------------------------------------------------------- geometry
def _link(bm, name, mat, bevel=0.0, segments=2, smooth=True):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    if mat is not None:
        me.materials.append(mat)
    if smooth and bevel > 0:
        for p in me.polygons:
            p.use_smooth = True
    if bevel > 0:
        md = ob.modifiers.new("Bevel", "BEVEL")
        md.width = bevel
        md.segments = segments
        md.limit_method = "ANGLE"
        md.angle_limit = math.radians(35)
        try:
            md.harden_normals = True
        except Exception:
            pass
    return ob


def prism(points, depth, mat, z0=0.0, bevel=0.0015, name="Prism"):
    """Extruded polygon (screen px points, counter-clockwise or clockwise)."""
    bm = bmesh.new()
    vs = [bm.verts.new(P(x, y, z0)) for x, y in points]
    f = bm.faces.new(vs)
    if f.normal.z < 0:
        f.normal_flip()
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    vv = [e for e in r["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, 0, depth), verts=vv)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _link(bm, name, mat, bevel)


def rrect_points(cx, cy, w, h, r, seg=8):
    r = min(r, w / 2, h / 2)
    pts = []
    for (ox, oy, a0) in ((cx + w / 2 - r, cy - h / 2 + r, -90), (cx + w / 2 - r, cy + h / 2 - r, 0),
                         (cx - w / 2 + r, cy + h / 2 - r, 90), (cx - w / 2 + r, cy - h / 2 + r, 180)):
        for i in range(seg + 1):
            a = math.radians(a0 + 90 * i / seg)
            pts.append((ox + r * math.cos(a), oy + r * math.sin(a)))
    return pts


def circle_points(cx, cy, r, n=96):
    return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]


def rrect(cx, cy, w, h, r, depth, mat, z0=0.0, bevel=0.002, name="RRect"):
    return prism(rrect_points(cx, cy, w, h, r), depth, mat, z0, bevel, name)


def disc(cx, cy, r, depth, mat, z0=0.0, bevel=0.002, name="Disc"):
    return prism(circle_points(cx, cy, r), depth, mat, z0, bevel, name)


def pol(cx, cy, r, t):
    """Dial convention: angle t in degrees clockwise from 12 o'clock, screen coordinates."""
    a = math.radians(t)
    return (cx + r * math.sin(a), cy - r * math.cos(a))


def ring(cx, cy, ro, ri, depth, mat, z0=0.0, t0=None, t1=None, n=96, bevel=0.0015, name="Ring"):
    """Annulus, or an arc band between dial angles t0..t1 (degrees clockwise from 12 o'clock)."""
    full = t0 is None
    if full:
        t0, t1 = 0.0, 360.0
    steps = n if full else max(4, int(abs(t1 - t0) / 3))
    bm = bmesh.new()
    outer = [bm.verts.new(P(*pol(cx, cy, ro, t0 + (t1 - t0) * i / steps), z0)) for i in range(steps + (0 if full else 1))]
    inner = [bm.verts.new(P(*pol(cx, cy, ri, t0 + (t1 - t0) * i / steps), z0)) for i in range(steps + (0 if full else 1))]
    count = len(outer)
    faces = []
    for i in range(count if full else count - 1):
        j = (i + 1) % count
        faces.append(bm.faces.new((outer[i], outer[j], inner[j], inner[i])))
    for f in faces:
        if f.normal.z < 0:
            f.normal_flip()
    r = bmesh.ops.extrude_face_region(bm, geom=faces)
    vv = [e for e in r["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, 0, depth), verts=vv)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _link(bm, name, mat, bevel)


def bar(cx, cy, length, width, angle_deg, depth, mat, z0=0.0, bevel=0.0008, name="Bar"):
    """Centered rectangle rotated by angle (degrees, screen, clockwise)."""
    a = math.radians(angle_deg)
    ux, uy = math.cos(a), math.sin(a)
    vx, vy = -uy, ux
    hl, hw = length / 2, width / 2
    pts = [(cx - ux * hl - vx * hw, cy - uy * hl - vy * hw), (cx + ux * hl - vx * hw, cy + uy * hl - vy * hw),
           (cx + ux * hl + vx * hw, cy + uy * hl + vy * hw), (cx - ux * hl + vx * hw, cy - uy * hl + vy * hw)]
    return prism(pts, depth, mat, z0, bevel, name)


def radial_bar(cx, cy, t, r0, r1, width, depth, mat, z0=0.0, name="Tick"):
    mid = (r0 + r1) / 2
    x, y = pol(cx, cy, mid, t)
    return bar(x, y, r1 - r0, width, t - 90, depth, mat, z0, bevel=0.0005, name=name)


def strip(points, width, depth, mat, z0=0.0, closed=False, bevel=0.0012, name="Strip"):
    """Constant-width ribbon along a polyline (screen px)."""
    n = len(points)
    left, right = [], []
    for i in range(n):
        p = points[i]
        a = points[i - 1] if (i > 0 or closed) else None
        b = points[(i + 1) % n] if (i < n - 1 or closed) else None
        d1 = (p[0] - a[0], p[1] - a[1]) if a else None
        d2 = (b[0] - p[0], b[1] - p[1]) if b else None
        def nrm(d):
            l = math.hypot(*d)
            return (-d[1] / l, d[0] / l)
        n1 = nrm(d1) if d1 else nrm(d2)
        n2 = nrm(d2) if d2 else nrm(d1)
        mx, my = n1[0] + n2[0], n1[1] + n2[1]
        ml = math.hypot(mx, my) or 1
        mx, my = mx / ml, my / ml
        k = (width / 2) / max(0.4, mx * n1[0] + my * n1[1])
        left.append((p[0] + mx * k, p[1] + my * k))
        right.append((p[0] - mx * k, p[1] - my * k))
    bm = bmesh.new()
    lv = [bm.verts.new(P(x, y, z0)) for x, y in left]
    rv = [bm.verts.new(P(x, y, z0)) for x, y in right]
    faces = []
    for i in range(n if closed else n - 1):
        j = (i + 1) % n
        faces.append(bm.faces.new((lv[i], lv[j], rv[j], rv[i])))
    for f in faces:
        if f.normal.z < 0:
            f.normal_flip()
    r = bmesh.ops.extrude_face_region(bm, geom=faces)
    vv = [e for e in r["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, 0, depth), verts=vv)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _link(bm, name, mat, bevel)


def rivet(x, y, r, mat, z0=0.0, name="Rivet"):
    """Domed rivet or screw head with a slot (when r >= 7)."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=r * PX, location=P(x, y, z0))
    ob = bpy.context.active_object
    ob.name = name
    ob.scale.z = 0.45
    ob.data.materials.append(mat)
    bpy.ops.object.shade_smooth()
    return ob


def screw(x, y, r, mat, slot_mat, angle=35, z0=0.0):
    rivet(x, y, r, mat, z0, "Screw")
    bar(x, y, r * 1.6, max(1.5, r * 0.28), angle, 0.006, slot_mat, z0 + r * PX * 0.42, 0.0003, "Slot")


def text(s, x, y, size_px, mat, z0=0.0, depth=0.003, align="CENTER", name="Text"):
    cu = bpy.data.curves.new(name, "FONT")
    cu.body = s
    cu.size = size_px * PX
    cu.align_x = align
    cu.align_y = "CENTER"
    cu.extrude = depth / 2
    cu.bevel_depth = 0.0
    ob = bpy.data.objects.new(name, cu)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = P(x, y, z0)
    cu.materials.append(mat)
    return ob
