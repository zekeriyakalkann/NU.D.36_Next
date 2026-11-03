# Lab 3 Terrain: endless low-poly environment in Rota03

Date: 2026-10-19. This is an environment addition requested after Lab 3, not a worksheet requirement. Decisions: DecisionLog D-034 … D-038.

## 1. Architecture
Chunked procedural mesh terrain in a fixed pool, chosen over Unity Terrain tiles because it is deterministic from a pure function, needs no assets, allows flat shading with vertex colours and has no collider cost.

- `AraziUretici` (static): `Yukseklik(x, z)` is the height as a function of world position, and `ChunkUret` builds one chunk's flat-shaded mesh data. Nothing is stored; the same chunk always looks the same, whichever way the aircraft arrived.
- `SonsuzArazi` (MonoBehaviour on the scene object `Arazi`): keeps a pool of (2·6+1)² = 169 chunk objects in a square window centred on the aircraft's chunk. When the aircraft enters another chunk, chunks that left the window are re-assigned to the newly visible coordinates and rebuilt, at most 3 per frame, nearest first. The first window is built synchronously in `Start`, so terrain exists on the first frame. The window is updated in `LateUpdate` (execution order 100, after the flight controller).
- Seams: all chunks sample the same function at the same integer-grid world positions, so shared edges are identical (tested with exact equality). The colour of a face depends only on its global cell index, so colours are continuous across chunks.
- No colliders. The terrain is visual only and does not touch the aircraft, radar or health.

| Setting | Value |
|---|---|
| Chunk size | 256 m, 16×16 cells (16 m), 512 triangles, 1 536 vertices |
| Window | 13×13 chunks (±6), at least 1 280 m to the window edge in every direction |
| Pool | 169 chunk objects and meshes, created once; no creation or destruction while flying |
| Mesh rebuild | reused arrays and meshes; none of the arrays is reallocated while flying |
| Fog | linear, 600–1 400 m (full fog before the window edge, so the edge never shows) |
| Camera far plane | 2 200 m (Rota03 camera only) |

## 2. Visual design
- **Height:** rolling hills (about −9 to +25 m) from 4-octave gradient noise plus detail. More than about 650 m from the start, ridged mountain ranges (up to about 350 m) appear in some regions. Around the start the ground is flat and level at −1 m, so the Uçaksavar capsule stands on it, and Balon and the aircraft's start altitude (20 m) stay clear. The mountain-region offset (`DagBolgeX/Z`) was chosen from a 24-sector sampling so that ranges are ahead (+Z) of the start (`Logs/lab3/arazi_daglar_sektor.csv`).
- **Colour:** per face from height, steepness and a 130 m patch noise: two meadow greens with darker forest and drier patches, olive uplands, grey rock on steep or high faces, snow above about 190 m, ±7 % brightness jitter per face.
- **Shader `NU36/LowPolyTerrain`:** vertex colour × (hemisphere ambient + main light N·L), no textures, no shadows. Fog is computed from `unity_FogParams`, so it does not depend on fog shader variants being kept in the build.
- **Sky `NU36/GradientSky`:** zenith blue to a horizon colour that equals the fog colour, with a soft sun glow. Ambient light is Trilight in sky blue / grey-green / earth.
- The Directional Light is unchanged.

## 3. Files
New:
- `Assets/Scripts/Runtime/AraziUretici.cs`, `SonsuzArazi.cs`
- `Assets/Environment/Shaders/LowPolyTerrain.shader`, `GradientSky.shader`
- `Assets/Environment/Materials/Rota03_Arazi.mat`, `Rota03_Gokyuzu.mat` (created by the builder)
- `Assets/Scripts/Editor/Lab3TerrainSceneBuilder.cs` (menu **NU36 → Lab 3 → Add Terrain To Rota03**; batch: `-executeMethod NU36.Editor.Lab3TerrainSceneBuilder.Apply`; it opens and saves only Rota03 and can be re-run)
- `Assets/Tests/EditMode/Lab3TerrainTests.cs`, `Assets/Tests/PlayMode/Lab3TerrainPlayTests.cs`
- `Documentation/Lab3_Terrain_Implementation.md`

Modified: `Assets/Scenes/Rota03.unity` (new root object `Arazi`; skybox, fog, ambient; Main Camera far clip plane 1 000 → 2 200), `Documentation/DecisionLog.md`, `Documentation/SetupAndRun.md`. `Assets/Scenes/Build Settings` unchanged.

Not touched: `UcusKontrol`, `Saglik`, `CanYazisi`, `TakipKamerasi`, the Input Actions asset, Rota02, SampleScene, Blender files. MD5 of Rota02 and SampleScene is identical to the value before this work (and is asserted by a test).

## 4. Tests actually run (batch mode, 2026-10-19)
| Run | Result |
|---|---|
| EditMode, whole project | 102 passed, 0 failed, 0 skipped (includes 9 new terrain tests and all Lab 2 / Lab 3 tests) |
| PlayMode, whole project | 28 passed, 0 failed, 0 skipped (includes 4 new terrain tests and all Lab 2 / Lab 3 tests) |
| Windows build `BuildWindowsRota03` | succeeded, 0 errors, 2 warnings → `Builds/Windows_Rota03/NU.D.36.exe` (overwrites the previous Rota03 build, as that command always does) |
| Built exe started and captured | renders the terrain, sky and mountains (`Logs/lab3/arazi_player_rota03.png`); the 4 "shader stripped" lines for depth-of-field and Panini post-processing also appear in the log from before the terrain work |

What the new tests show:
- Height function: deterministic, bounded, level at the origin, below 25 m within 600 m of the start, mountains present in some but not all directions and ahead of the start.
- Seams: neighbouring chunks have identical edge heights (east/west and north/south).
- Mesh: every triangle faces up, normals match, colours opaque, no NaN.
- Rota03: `Arazi` wired to the aircraft and material; Rota02 has no terrain.
- Runtime, initial: all 169 chunks built before the first frame; terrain under the aircraft; no colliders; fog and sky set.
- Runtime, flight (aircraft moved with `UcusKontrol.Adim`, 16.8 m per frame): 271 frames, about 3.6 km, straight, a 60° right turn, a climb and a left roll. At every frame the window was centred on the aircraft and the 5×5 chunks around it were built (0 holes). Pool, child count and mesh count stayed at 169; at most 19 chunks were pending; one gen-0 GC in the whole run.
- Teleport 70 km away: the whole window was rebuilt in 57 frames (0.21 s).
- Existing flight, camera, health and PlayerInput components still present and working (the existing Lab 3 tests, unchanged, pass).

Images examined: `arazi_01_baslangic.png` (start), `arazi_02_gerade.png`, `arazi_03_donus_sonrasi.png`, `arazi_04_tirmanis.png`, `arazi_05_yatis.png`, `arazi_player_rota03.png` in `Logs/lab3/`. They show green low-poly hills, haze-faded mountains with snow caps, a smooth horizon fade into the sky and no visible chunk seams.

## 5. Performance
Not measured. Per frame the work is draw calls for the visible chunks (at most 169, frustum culling removes the rest), up to 3 chunk rebuilds only while the window moves, and no per-frame allocation in the update path except the sort when the window moves. The full rebuild measured above (169 chunks in 0.21 s, including frame time) suggests one chunk costs well under a millisecond, but FPS in the Editor or the exe was not measured.

## 6. Known limitations
- **No collision, by request.** The aircraft can fly through hills and mountains, and the follow camera can end up inside terrain (seen when an early test flew 20 m high into a range). The start area is clear (ground ≤ 25 m within 600 m).
- Mountains are fairly common (rings of ranges in most directions beyond about 900 m). Frequency can be tuned in `Yukseklik`.
- Terrain does not receive or cast shadows and has no water, rivers, roads, forest or settlements (omitted on purpose).
- Straight-line flight reaches the same noise function forever, but float precision degrades far from the origin (visible jitter is expected beyond tens of kilometres, as for every Unity scene). No floating origin was added.
- A fast change of the window (diagonal at 42 m/s) queues up to about 25 chunks, built 3 per frame; at the measured step sizes the 5×5 neighbourhood was always ready.
- Visual quality, framerate and flight feel were judged from screenshots and numbers only.

## 7. Manual checks for you (Unity Editor, Rota03)
1. Open `Rota03`, press Play: green terrain, mountains ahead, Balon and Uçaksavar visible.
2. Fly with W/S, A/D, Q/E and ↑/↓: terrain follows without gaps; the world keeps scrolling when turning and climbing.
3. Fly 3–5 km in any direction; watch for popping at the horizon (it should stay hidden in the haze).
4. Check the Stats window for frame time and the Hierarchy for a constant `Arazi` child count (169).
5. Open `Rota02` and Play: it must look and behave as before.
6. Decide whether you want collision or lower mountain frequency.
