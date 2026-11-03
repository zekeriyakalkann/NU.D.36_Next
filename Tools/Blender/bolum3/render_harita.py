"""Printable Bölüm 3 paper map (Lab 6 / Hafta 6), drawn in Blender from the map JSON written by Unity.

Usage:
  blender.exe --background --factory-startup --disable-autoexec --python render_harita.py -- <map.json> <output.png> [bos]
"bos" draws only the empty 12 x 9 grid (for redesigning by hand).

Nothing is loaded from or saved to a .blend file: the scene is built in memory and only the PNG is written.
Coordinates: one square = 1 Blender unit; column A..L -> x 0..12, row 1..9 -> y 0..-9 (north up).
"""
import json
import math
import os
import sys

import bpy

ARGV = sys.argv[sys.argv.index("--") + 1:]
JSON_PATH, OUT = os.path.abspath(ARGV[0]), os.path.abspath(ARGV[1])
BOS = len(ARGV) > 2 and ARGV[2] == "bos"
M = json.load(open(JSON_PATH, encoding="utf-8"))
COLS, ROWS = M["sutun"], M["satir"]

WHITE = (1, 1, 1, 1)
BLACK = (0.05, 0.05, 0.05, 1)
GRID = (0.35, 0.35, 0.35, 1)
RIVER = (0.55, 0.75, 0.97, 1)
RUNWAY = (0.18, 0.28, 0.6, 1)
BRIDGE = (1.0, 0.82, 0.1, 1)
HILL = (0.62, 0.62, 0.62, 1)
CLOUD = (0.86, 0.9, 0.96, 1)
BEAM = (1.0, 0.95, 0.6, 1)
GUN = (0.88, 0.12, 0.1, 1)
LOC = (0.6, 0.2, 0.85, 1)
ROUTE1 = (0.1, 0.6, 0.2, 1)
ROUTE2 = (0.95, 0.5, 0.05, 1)
GUN_RANGE = (0.96, 0.62, 0.6, 1)
LOC_RANGE = (0.82, 0.68, 0.95, 1)
LEGEND_W = 11.0   # the gun / locator ranges reach x = 14.5; the legend starts after them


def cell(name):
    c = ord(name[0].upper()) - ord("A")
    r = int(name[1:]) - 1
    return c, r


def centre(name):
    c, r = cell(name)
    return c + 0.5, -(r + 0.5)


def compass(deg):
    a = math.radians(deg)
    return math.sin(a), math.cos(a)  # (east, north) = (+x, +y)


def obj(name, verts, faces, color, z):
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x, y, z) for x, y in verts], [], faces)
    o = bpy.data.objects.new(name, me)
    o.color = color
    bpy.context.scene.collection.objects.link(o)
    return o


def rect(name, x0, y0, x1, y1, color, z):
    return obj(name, [(x0, y0), (x1, y0), (x1, y1), (x0, y1)], [(0, 1, 2, 3)], color, z)


def segment(name, p, q, width, color, z):
    dx, dy = q[0] - p[0], q[1] - p[1]
    length = math.hypot(dx, dy) or 1e-6
    nx, ny = -dy / length * width / 2, dx / length * width / 2
    v = [(p[0] + nx, p[1] + ny), (q[0] + nx, q[1] + ny), (q[0] - nx, q[1] - ny), (p[0] - nx, p[1] - ny)]
    return obj(name, v, [(0, 1, 2, 3)], color, z)


def disc(name, cx, cy, r, color, z, n=48):
    v = [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]
    return obj(name, v, [tuple(range(n))], color, z)


def ring(name, cx, cy, r, w, color, z, n=128):
    v, f = [], []
    for i in range(n):
        a = 2 * math.pi * i / n
        v += [(cx + (r - w / 2) * math.cos(a), cy + (r - w / 2) * math.sin(a)),
              (cx + (r + w / 2) * math.cos(a), cy + (r + w / 2) * math.sin(a))]
    for i in range(n):
        j = (i + 1) % n
        f.append((2 * i, 2 * j, 2 * j + 1, 2 * i + 1))
    return obj(name, v, f, color, z)


def dashed(name, pts, width, color, z, dash=0.22, gap=0.14):
    k = 0
    for a, b in zip(pts, pts[1:]):
        length = math.hypot(b[0] - a[0], b[1] - a[1])
        t = 0.0
        while t < length:
            t1 = min(t + dash, length)
            p = (a[0] + (b[0] - a[0]) * t / length, a[1] + (b[1] - a[1]) * t / length)
            q = (a[0] + (b[0] - a[0]) * t1 / length, a[1] + (b[1] - a[1]) * t1 / length)
            segment(f"{name}_{k}", p, q, width, color, z)
            k += 1
            t += dash + gap


def arrow(name, cx, cy, deg, size, color, z):
    ex, ny = compass(deg)
    px, py = -ny, ex
    tip = (cx + ex * size, cy + ny * size)
    l = (cx - ex * size * 0.5 + px * size * 0.55, cy - ny * size * 0.5 + py * size * 0.55)
    r = (cx - ex * size * 0.5 - px * size * 0.55, cy - ny * size * 0.5 - py * size * 0.55)
    return obj(name, [tip, l, r], [(0, 1, 2)], color, z)


def text(name, s, x, y, size, color=BLACK, z=0.09, align="CENTER"):
    cu = bpy.data.curves.new(name, "FONT")
    cu.body = s
    cu.size = size
    cu.align_x = align
    cu.align_y = "CENTER"
    o = bpy.data.objects.new(name, cu)
    o.location = (x, y, z)
    o.color = color
    bpy.context.scene.collection.objects.link(o)
    return o


def build():
    sc = bpy.context.scene
    for o in list(bpy.data.objects):  # factory scene: cube, light, camera live in a child collection
        bpy.data.objects.remove(o)
    margin, legend_w = 0.9, LEGEND_W
    rect("Kagit", -margin, -ROWS - margin, COLS + legend_w, margin, WHITE, -0.01)

    # Column / row labels.
    for c in range(COLS):
        text(f"Sutun{c}", chr(ord("A") + c), c + 0.5, 0.45, 0.42)
        text(f"SutunAlt{c}", chr(ord("A") + c), c + 0.5, -ROWS - 0.45, 0.42)
    for r in range(ROWS):
        text(f"Satir{r}", str(r + 1), -0.45, -(r + 0.5), 0.42)
        text(f"SatirSag{r}", str(r + 1), COLS + 0.45, -(r + 0.5), 0.42)

    # Grid.
    for c in range(COLS + 1):
        segment(f"GridV{c}", (c, 0), (c, -ROWS), 0.025, GRID, 0.06)
    for r in range(ROWS + 1):
        segment(f"GridH{r}", (0, -r), (COLS, -r), 0.025, GRID, 0.06)

    lx = COLS + 3.4
    text("Baslik", "NU.D.36 — Bölüm 3", lx, 0.25, 0.5, align="LEFT")
    if BOS:
        text("Alt", "Boş harita · 1 kare = 200 m", lx, -0.5, 0.32, align="LEFT")
        return

    def square(name, color, z=0.0):
        c, r = cell(name)
        rect(f"Kare_{name}_{z}", c, -r, c + 1, -(r + 1), color, z)

    for k in M["nehir"]:
        square(k, RIVER)
    for k in M["bulutlar"]:
        square(k, CLOUD)
        cx, cy = centre(k)
        text(f"BulutYazi{k}", "BULUT", cx, cy + 0.3, 0.2, (0.3, 0.35, 0.45, 1))
    for k in M["tepeler"]:
        square(k, HILL)
        cx, cy = centre(k)
        text(f"TepeYazi{k}", "TEPE\n120 m", cx, cy, 0.24)

    # Runway and bridge.
    a, b = M["pist"][0], M["pist"][-1]
    (ca, ra), (cb, rb) = cell(a), cell(b)
    rect("Pist", min(ca, cb) + 0.03, -(ra + 0.5) + 0.2, max(ca, cb) + 0.97, -(ra + 0.5) - 0.2, RUNWAY, 0.02)
    text("PistYazi", "PİST", (min(ca, cb) + max(ca, cb) + 1) / 2, -(ra + 0.5), 0.26, WHITE, 0.1)
    a, b = M["kopru"][0], M["kopru"][-1]
    (ca, ra), (cb, rb) = cell(a), cell(b)
    rect("Kopru", min(ca, cb), -(ra + 0.5) + 0.15, max(ca, cb) + 1, -(ra + 0.5) - 0.15, BRIDGE, 0.03)
    text("KopruYazi", "KÖPRÜ", (min(ca, cb) + max(ca, cb) + 1) / 2, -(ra + 0.5) - 0.33, 0.24, BLACK, 0.1)

    # Ranges (under the symbols).
    for i, k in enumerate(M["ucaksavarlar"]):
        cx, cy = centre(k)
        ring(f"UcaksavarMenzil{i}", cx, cy, M["ucaksavar_yaricap_kare"], 0.03, GUN_RANGE, 0.04)
    cx, cy = centre(M["ses_dinleyici"])
    ring("SesMenzil", cx, cy, M["ses_yaricap_kare"], 0.035, LOC_RANGE, 0.04)

    # Searchlight beams (6 squares) and symbols.
    for i, s in enumerate(M["isildaklar"]):
        cx, cy = centre(s["kare"])
        ex, ny = compass(s["yon"])
        L = M["isin_kare"]
        segment(f"Isin{i}", (cx, cy), (cx + ex * L, cy + ny * L), 0.22, BEAM, 0.035)
        disc(f"IsildakKenar{i}", cx, cy, 0.3, BLACK, 0.07)
        disc(f"Isildak{i}", cx, cy, 0.25, WHITE, 0.075)
        arrow(f"IsildakOk{i}", cx + ex * 0.42, cy + ny * 0.42, s["yon"], 0.22, BLACK, 0.08)
        text(f"IsildakYazi{i}", f"I{i + 1}", cx, cy, 0.2, BLACK, 0.09)
    for i, k in enumerate(M["ucaksavarlar"]):
        cx, cy = centre(k)
        disc(f"Ucaksavar{i}", cx, cy, 0.27, GUN, 0.07)
        text(f"UcaksavarYazi{i}", f"U{i + 1}", cx, cy, 0.2, WHITE, 0.09)
    cx, cy = centre(M["ses_dinleyici"])
    disc("Ses", cx, cy, 0.27, LOC, 0.07)
    text("SesYazi", "S", cx, cy, 0.26, WHITE, 0.09)

    # Routes and the start token.
    for key, col, off, name in (("yol1", ROUTE1, -0.12, "Yol1"), ("yol2", ROUTE2, 0.12, "Yol2")):
        pts = [(centre(k)[0] + off, centre(k)[1] + off) for k in M[key]]
        dashed(name, pts, 0.07, col, 0.05)
    cx, cy = centre(M["baslangic"])
    arrow("Ucak", cx, cy, M["baslangic_yonu"], 0.34, BLACK, 0.085)

    # Legend.
    y = -0.5
    text("Alt", f"Harita v{M['surum']} · 1 kare = {int(M['kare_m'])} m · 1 tur = 15 s", lx, y, 0.3, align="LEFT")
    items = [
        (RIVER, "Nehir"), (BRIDGE, "Köprü (3 kare, 25 m)"), (RUNWAY, "Pist (3 kare)"), (HILL, "Tepe 120 m (Alçak'ı keser)"),
        (CLOUD, "Bulut (tek kural)"), (BEAM, "Işıldak ışını, 6 kare"), (GUN, "Uçaksavar, menzil 4 kare"),
        (LOC, "S = ses dinleyici, 5 kare"), (ROUTE1, "Yol 1: tepe arkası (alçak)"), (ROUTE2, "Yol 2: açık, bulut içinden"),
    ]
    y = -1.2
    for color, label in items:
        rect(f"Lejant{label}", lx, y + 0.18, lx + 0.5, y - 0.18, color, 0.02)
        text(f"LejantYazi{label}", label, lx + 0.7, y, 0.27, align="LEFT")
        y -= 0.55
    y -= 0.15
    text("KuralBaslik", "Tek kural:", lx, y, 0.3, align="LEFT")
    words, line, lines = M["tek_kural"].split(), "", []
    for w in words:
        if len(line) + len(w) > 34:
            lines.append(line)
            line = w
        else:
            line = (line + " " + w).strip()
    lines.append(line)
    for ln in lines:
        y -= 0.42
        text(f"Kural{y}", ln, lx, y, 0.27, align="LEFT")
    y -= 0.6
    text("BaslangicYazi", f"Başlangıç: {M['baslangic']}, Alçak, burun KD (▲)", lx, y, 0.27, align="LEFT")


def render():
    sc = bpy.context.scene
    margin, legend_w = 0.9, LEGEND_W
    w, h = COLS + legend_w + margin, ROWS + 2 * margin
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "FLAT"
    sc.display.shading.color_type = "OBJECT"
    sc.display.shading.show_object_outline = False
    sc.view_settings.view_transform = "Standard"
    sc.render.resolution_x = 2800
    sc.render.resolution_y = int(2800 * h / w)
    sc.render.film_transparent = False
    sc.world = bpy.data.worlds.new("Beyaz") if sc.world is None else sc.world
    cam_data = bpy.data.cameras.new("Kamera")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = w
    cam = bpy.data.objects.new("Kamera", cam_data)
    cam.location = (-margin + w / 2, -ROWS / 2, 10)
    sc.collection.objects.link(cam)
    sc.camera = cam
    sc.render.image_settings.file_format = "PNG"
    sc.render.filepath = OUT
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    bpy.ops.render.render(write_still=True)
    print("Wrote", OUT)


build()
render()
