# NU.D.36 Simulation

Unity simulation around the NU.D.36 biplane model, developed lab by lab (Lab 2 → Lab 3 → Lab 4).

- Unity **6000.6.4f1** (URP) project: `NU.D.36_Unity/`
- Aircraft source: `Blender/NU.D.36_project_FINAL.blend` (Blender 5.2.2 LTS), exported to FBX with `Tools/Blender/`
- Documentation: `Documentation/`

## Open and run
1. Unity Hub → Add → `NU.D.36_Unity`, open with 6000.6.4f1.
2. Open `Assets/Scenes/Rota02.unity` (Lab 2) and press Play.

## Lab 2 (Rota02)
| Object | Setup | Script |
|---|---|---|
| `Uçak` | position (0, 20, 0); aircraft model as child | `HedefeGit` (moves toward `Balon`), `KanatYatisi` (cross-product wing roll) |
| `Balon` | Sphere, position (40, 30, 60), scale (6, 6, 6) | — |
| `Uçaksavar` | Cylinder, position (0, 0, 0), forward +Z | `Radar` (dot-product field of view, target `Uçak`) |

On Play, the aircraft turns toward Balon, banking right during the turn. It flies at a constant speed and stops 10 m from Balon's centre, clear of the sphere, after about 3.2 s. The radar reports the aircraft once it is within 45° of +Z and 100 m.

Speed (20 m/s), turn rate (90°/s), arrival distance (10 m), radar half-angle (45°) and range (100 m), the roll values and the camera position are **technical defaults**, not confirmed worksheet values. See `Documentation/DecisionLog.md`.

Gizmos (Scene view): cyan = aircraft forward, yellow = direction to `Balon`, white = radar forward, green/red = radar target visible/not visible.

## Lab 3 (Rota03): Pilot Koltuğu
A copy of Rota02 in which the player flies the aircraft. Lab 2's `HedefeGit` and `KanatYatisi` are kept but disabled there; `Radar` still works.

| Component | On | Purpose |
|---|---|---|
| `UcusKontrol` + `PlayerInput` (map `Ucus`, Send Messages) | `Uçak` | W/S pitch, D/A roll, E/Q yaw, ↑/↓ throttle; speed 8–42 m/s; Transform-based, deltaTime |
| `Saglik` / `CanYazisi` | `Uçak` | Health 100 with `CanDegisti` / `Oldu` events; listener logs changes (Observer) |
| `TakipKamerasi` | `Main Camera` | Follow offset (0, 3.2, −11), SmoothDamp 0.18, field of view 55–72° with throttle, LateUpdate |

Lab 3 build: `Builds/Windows_Rota03/NU.D.36.exe`. Details, decisions and experiment results: `Documentation/Lab3_Implementation.md`.

## Tests and build
See `Documentation/SetupAndRun.md`. Current results (EditMode 93/93, PlayMode 24/24, Windows builds OK): `Documentation/TestReport.md`.

## Open items
- MDA worksheet (`Documentation/Lab2_MDA.md`): requires the student to play and analyse a browser game.
- Git repository, first commit and push: not done; needs explicit authorization and a remote URL.
- Worksheet values not yet confirmed: see `Documentation/Lab2_Implementation_Plan.md`.
- Lab 3: keyboard flight still needs one manual check by a person; the W pitch sign needs confirming against the worksheet (D-029).
