# Test Report: Lab 2 (Rota02) and Lab 3 (Rota03)

Last full verification: 2026-10-19 (after Lab 3), Unity 6000.6.4f1, URP. Everything below was run on the current project state. Lab 3 details: §5 and `Documentation/Lab3_Implementation.md`.

Status values: VERIFIED (evidence obtained), PARTIALLY VERIFIED, NOT TESTED, BLOCKED.

## 0. Current test totals (whole project, run in the Editor GUI after Lab 3)

| Suite | Result | Per fixture |
|---|---|---|
| EditMode | **93/93 passed** | AircraftModelImport 9, Lab2Scene 8, Lab2Vector 35, Lab3InputActions 6, Lab3Kamera 11, Lab3Scene 9, Lab3UcusKontrol 15 |
| PlayMode | **24/24 passed** | Lab2PlayMode 3, SceneSmoke 2 (now loads Rota02, SampleScene, Rota03), Lab3Deneyler 3, Lab3Girdi 8, Lab3Saglik 8 |

All Lab 2 tests still pass after Lab 3 (regression). Rota02 and SampleScene SHA256 values are unchanged.

## 1. Summary (Lab 2 verification, before Lab 3)

| Check | Result | Evidence |
|---|---|---|
| C# compilation | VERIFIED: no CS errors or warnings | `Logs/unity_editmode.log`, `Logs/unity_playmode.log` |
| EditMode tests | **52/52 passed** (Lab 2 state; now 93/93, see §0) | `Logs/TestResults_EditMode.xml` |
| PlayMode tests | **5/5 passed** (Lab 2 state; now 24/24, see §0) | `Logs/TestResults_PlayMode.xml` |
| Editor GUI run (Rota02, Play Mode 8 s) | VERIFIED: no console errors or warnings; no missing scripts, meshes, materials or references | `Logs/visual/edit_scene_report.txt`, `console.log`, `play_samples.csv` |
| Windows 64-bit build | VERIFIED: succeeded (0 errors) | `Logs/unity_build.log` |
| Built player | VERIFIED: opens windowed, shows Rota02, aircraft reaches Balon, closes on WM_CLOSE; log has the radar message and no exceptions | `Logs/player_run.log`, `Logs/visual_screens/player_*.png` |

Two Unity warnings are noted. Neither affects Lab 2:
- The build report counts 2 warnings. The only warnings in the log are Unity toolchain hint-path notes.
- The player log reports that URP stripped the Depth of Field and Panini Projection shaders. The scene does not use these effects.

## 2. Lab 2 traceability

Task numbers refer to the task list supplied in the conversation. Worksheet details that were not supplied are listed in `Lab2_Implementation_Plan.md` (W-items).

| Task | Automated evidence | Visual evidence | Status |
|---|---|---|---|
| 1. Rota02 scene | `Rota02_IsEnabledInBuildSettings`; PlayMode `SceneSmokeTests` | Editor and player load Rota02 | VERIFIED |
| 1. Git / README / .gitignore | README and .gitignore exist | n/a | PARTIAL: Git needs user authorization |
| 2. MDA | n/a | n/a | BLOCKED: user must play and analyse a game (`Lab2_MDA.md`) |
| 3. Transforms | `Ucak_IsAtSpecifiedPositionWithModelChild`, `Balon_IsSphereWithSpecifiedScaleAndPosition`, `Ucaksavar_IsCylinderAtOriginFacingPositiveZ`, `Components_AreWiredToTheIntendedTargets`, `ParentChild_ChildFollowsParentTransform` | `edit_scene_report.txt`: Uçak (0,20,0), Balon (40,30,60) scale 6, Uçaksavar (0,0,0) forward +Z | VERIFIED (the worksheet's own parenting exercise is unknown, W-3) |
| 4. Normalized direction, constant speed | `HedefYonu_IsUnitLength` ×4, `HedefYonu_IsZeroForCoincidentPoints`, `Adim_MovesSpeedTimesDtRegardlessOfDistance` ×2 | n/a | VERIFIED |
| 4. Bounded turning | `Adim_TurnIsBoundedByTurnRate` | Yaw 3.5° → 35.3° within about 0.45 s (`play_samples.csv`) | VERIFIED |
| 4. Arrival and stop | `Adim_ReachesRota02TargetAndStopsWithinArrivalDistance`, PlayMode `Aircraft_ReachesBalonAndStops` | Stops at about 3.2 s, 9.9 m from Balon's centre; position unchanged afterwards | VERIFIED |
| 4. Stops clear of Balon | `Ucak_StopsOutsideBalon`: closest aircraft part ≥ 2.83 m from Balon's surface | Side view: aircraft beside Balon (`camera_play_6.0s.png`) | VERIFIED |
| 5. Radar dot product, half-angle, range | `GorusAlanindaMi_RespectsHalfAngleAndRange` ×11, `GorusAlanindaMi_UsesVerticalAngleToo`, `Hesapla_FollowsObserverRotation`, PlayMode `Radar_SeesAircraftOnlyAfterItEntersTheFieldOfView` | Not visible from 87.7° down to 45.6° off-axis; visible from 44.5° (≤ 45°) at 47 m | VERIFIED |
| 6. Cross-product side and roll direction | `YanIsareti_MatchesUnityRightVector` ×5, `Adim_BanksTowardTargetSide` ×2, `Adim_LevelsOutWhenTargetIsAhead`, `Roll_IsNotErasedByHeadingControl`, PlayMode `Aircraft_BanksRightDuringInitialTurn` | Chase-camera frames: right wing visibly down during the right turn, level again by 0.8 s (§3.2) | VERIFIED (numeric and visual) |
| 7. Gizmos | `Component_ImplementsOnDrawGizmos` ×2, `GizmoRengi_DistinguishesVisibility`, `Hesapla_UsedByGizmos_DoesNotChangeRuntimeState` | Scene-view captures: yellow aircraft→Balon, cyan forward, white radar forward; radar line red at start, green after entry (`editor_edit.png`, `editor_play_5.png`) | VERIFIED |
| Model import | 9 `AircraftModelImportTests` (29 meshes, metres, nose +Z, materials, URP) | n/a | VERIFIED |

**Normalization (Task 4).** `target − position` has the distance as its length; using it directly would make the aircraft faster when far away. `.normalized` keeps only the direction, so each step is `speed × Δt`. A 10 m and a 1000 m target both give a 0.4 m step at 20 m/s and Δt = 0.02 s.

## 3. Visual verification

Method: `NU36.Editor.Lab2VisualVerification`.
- **GUI run.** Opens Rota02 without saving it, renders the Main Camera, frames the Scene view with Gizmos on, then runs 8 s of Play Mode with 0.2 s samples and quits.
- **Chase camera.** A temporary chase camera exists only during Play Mode.
- **Windscreen close-up.** A separate batch method renders in a new, never-saved scene.
- **Window captures.** `Tools/Verification/Capture-Window.ps1` (PrintWindow) captured only the Unity or player window.

All images listed in this section were opened and examined.

### 3.1 Main camera (presentation, D-023)
- **Placement.** Camera at (60, 35, 0) looking at (20, 15, 30), Euler (21.80°, 306.87°, 0°): a side view of the flight path with the horizon in frame.
- **Framing: VERIFIED** by `MainCamera_FramesAircraftPathBalonAndUcaksavar`.
  - All objects, including the aircraft's wingtip, nose and tail extents at the start and at the stop, are at least 10 % inside the frame.
  - The aircraft is about 121 px wide at the start and about 120 px at the stop (720p). Before this change: 170 and 81 px.
  - The stopped aircraft is drawn beside Balon with about a 25 px gap.
- **Images:** `camera_edit.png` (start), `camera_play_0.3s.png`, `camera_play_1.5s.png` (moving), `camera_play_3.0s.png`, `camera_play_6.0s.png` (stopped).
- **Remaining:** Balon, near the right edge, shows mild perspective stretching.

### 3.2 Wing roll
- **Numeric** (`play_samples.csv`, `chase_samples.csv`):
  - Bank starts at 2.4° (0.06 s) and peaks at 17.6° (0.39 s), with the right wingtip 243 px below the left in the chase image.
  - It is back to 0° at 0.8 s.
  - The peak varies with frame rate: 13.9°–17.6° across runs.
- **Visual:**
  - `chase_0.39s_bank17.6.png` shows the right wing clearly lower while the aircraft turns right toward Balon.
  - `chase_0.80s_bank0.0.png` shows wings level with Balon straight ahead.
  - The chase camera follows the moving root from behind, so image right is the pilot's right; the projected wingtips confirm this (right tip x ≈ 930 px, left ≈ 340 px).
- **Configured maximum** is 30°. It is not reached in Rota02 because the 35° heading change finishes in about 0.45 s.

### 3.3 Windscreen transparency
- **Close-up of each windscreen in a temporary scene:**
  - A: as imported.
  - B: windscreen hidden.
  - C: windscreen replaced, on that instance only, by an opaque magenta stand-in.
- **Mask.** Pixels where B and C differ are the windscreen (about 49 000 px, 5.3 % of the frame).

| Windscreen | mean diff A–B (vs. seeing through) | mean diff A–C (vs. opaque) | B–C |
|---|---|---|---|
| Forward | 0.056 | 0.420 | 0.435 |
| Rear | 0.048 | 0.412 | 0.424 |

- **Result: VERIFIED visually and numerically.** The imported glass looks almost the same as no glass, and very different from an opaque panel. In `windscreen_forward_windscreen_A_imported.png` the cockpit rim and a strut are clearly visible through the glass, which shows only a faint edge and highlight.
- **Imported settings:** URP/Lit, Surface = Transparent, alpha 0.15, render queue 3000 (`GlassMaterial_IsImportedAsTransparentOnWindscreens`).
- **Methodology note.** A first measurement was biased by the new scene's first render (darker ambient lighting). A discarded warm-up render was added before measuring.
- **Not changed:** no material or alpha value.

## 4. Remaining limitations
- Worksheet values (speed, turn rate, arrival distance, radar angle and range, roll formula, hierarchy exercise) are documented defaults, not confirmed course values (W-items).
- Visual judgements above are mine, from the captured images. Your own look in the Editor is still advisable before submission.
- Earlier camera evidence (D-019, D-021) is kept in `Logs/camera_review/` for history only.

## 5. Lab 3 (Rota03)

Full requirement and experiment table: `Documentation/Lab3_Implementation.md`. Evidence: `Logs/lab3/`.

| Check | Result | Evidence |
|---|---|---|
| Rota02 preserved | VERIFIED: SHA256 unchanged; `Rota02_IsUnchangedByLab3`; Lab 2 suites pass | `Logs/TestResults_*.xml` |
| Rota03 scene, wiring, no missing references | VERIFIED | `Lab3SceneTests` (9) |
| Ucus input map and keys (+/−) | VERIFIED: 8/8 keys through PlayerInput Send Messages | `Lab3InputActionsTests`, `girdi_yonleri.csv` |
| Flight control (rotation, throttle, speed, deltaTime) | VERIFIED | `Lab3UcusKontrolTests`, `Lab3GirdiTests`, `gaz_callback_deneyi.csv` |
| Saglik / CanYazisi (Observer) | VERIFIED | `Lab3SaglikTests` (8) |
| Follow camera (LateUpdate, follow, field of view 55–72°) | VERIFIED | `Lab3KameraTests`, `rota03_gaz_0.png`, `rota03_gaz_1.png` |
| deltaTime experiments (30 / 60 / 144 FPS, real and controlled) | VERIFIED: targets applied (30.0 / 60.0 / 143.4 FPS) | `deltatime_deneyi_*.csv` |
| Smoothing and camera experiments | VERIFIED numerically; subjective feel NOT VERIFIED | `yumusatma_deneyi*.csv`, `kamera_*_deneyi*.csv` |
| Lab 3 Windows build (`Builds/Windows_Rota03`) | VERIFIED: build succeeded; exe starts in Rota03 with the follow camera; no errors in the log | `Logs/unity_build_rota03.log`, `Logs/player_run_rota03.log`, `Logs/lab3/player_rota03_*.png` |
| Keyboard flight in the exe or Editor by a person | NOT VERIFIED (no real key presses possible in this session) | — |


## 6. Lab 5 (Rota05), 2026-11-03

Batch mode, Unity 6000.6.4f1. Logs and results in `Logs/lab5/`.

| Suite | Result |
|---|---|
| EditMode (whole project) | **193/193 passed** (147 before Lab 5 + 46 new: `Lab5FormulTests` 31, `Lab5SceneTests` 15) |
| PlayMode (whole project) | **89 passed, 1 skipped** (76 before + 13 `Lab5PlayTests`; skipped = the explicit image test `Lab5GorselKayit`) |
| C# compilation | no errors; no new warnings from Lab 5 code (older test files keep their obsolete-API warnings) |
| Windows player Rota05 | build succeeded (0 errors, 2 warnings as before); ran windowed 25 s: HUD, countdown, dive panel, terrain, hills, flag poles visible; no exceptions in `player_rota05.log` (`Logs/lab5/player/*.png`) |
| Earlier scenes | Rota02, Rota03, Rota04, Rota04Hud, SampleScene MD5 unchanged (test) |

What the tests prove and what they do not: requirement mapping in `Lab5_Implementation.md` §2. Visual record (camera renders, examined): hills after the terrain-following fix, dumps, gun, sound locator, the view from gun 2 blocked by Tepe 3, a 60° dive (`Logs/lab5/gorsel/`). Measurement for Step 05: `Logs/lab5/olcum_gorus.txt` (Stopwatch in batch mode, not the Profiler).

NOT TESTED: a hand-flown full level and its balance, real keyboard presses of Space/R, the Scene-view Gizmo look, a crash on a hill slope, the Profiler task (user).

## 7. Lab 6 (Bolum3_GriKutu), 2026-11-03

Batch mode, Unity 6000.6.4f1. Logs and results in `Logs/lab6/`.

| Suite | Result |
|---|---|
| EditMode (whole project) | **210/210 passed** (193 + 17 `Lab6Tests`) |
| PlayMode (whole project) | **92 passed, 2 skipped** (89 + 3 `Lab6PlayTests`; skipped = the two explicit image tests) |
| Earlier scenes | Rota02 … Rota05 MD5 unchanged (test) |
| Windows player Bolum3_GriKutu | build succeeded; ran windowed 8 s: stopwatch, HUD, grey boxes visible; no exceptions (`Logs/lab6/player/`) |
| Paper maps (Blender) | rendered and examined: `Exports/Lab6/bolum3_harita_v0.1.png`, `bolum3_bos_harita.png` (first render had the factory cube over A1; fixed) |
| Flight timing | Route 1 (60 m) back at 49.5 s = 3.3 turns, Route 2 (200 m) back at 41.0 s = 2.7 turns (`Logs/lab6/sure_olcumu.txt`); first attempt crashed into the F7–G7 ridge when the test autopilot cut a corner at 20°/s, rerun at 30°/s |

NOT TESTED / USER: paper play (Steps 01, 03, 04), a hand-flown grey box run, the GDD in the Word template.

## 8. Lab 7 (Rota07), 2026-11-03

Batch mode, Unity 6000.6.4f1. Logs and results in `Logs/lab7/`.

| Suite | Result |
|---|---|
| EditMode (whole project) | **232/232 passed** (210 + 12 `Lab7Tests` + 10 `Lab7UcusFizigiTests`) |
| PlayMode (whole project) | **107 passed, 3 skipped** (92 + 15 `Lab7PlayTests`; skipped = the three explicit image tests) |
| Earlier scenes | Rota02 … Rota05 and Bolum3_GriKutu MD5 unchanged (test) |
| Windows player Rota07 | build succeeded (0 errors, 2 warnings as before); ran windowed 25 s: HUD, runway, lights, landing light visible; terrain + forest + colliders generated in 0.96 s; no exceptions (`Logs/lab7/player.log`, `Logs/lab7/gorsel/20_player_hud_baslangic.png`) |
| Measurements (`Logs/lab7/ucus_olcumu.txt`) | lift-off 48.9 m/s after 356 m; hands-off 30 s height band 5.1 m; autopilot landing touchdown sink 0.77 m/s, stop in the zone; tower body 87° after 7 s; bridge panels 14.5 m below the deck; searchlight-held gun 8 rounds in 3 s |
| Visual record (camera renders, examined) | `Logs/lab7/gorsel/01…12` |

Defects found by the tests and renders and fixed: black first renders (no ambient probe, D-080); unlit bulbs (`_EMISSION` dropped, D-079); null bomb/round prefab references in the built scene (D-082); ridge across the approach path (D-077); wheel friction applied at 6 % per step (brakes did not stop the aircraft); bridge panels hanging on the piers and a tower that did not always topple (D-078); 14 s load before the terrain grid was computed in parallel.

The Lab 6 test "GriKutu is last in Build Settings" relaxed to "after Rota05" (Rota07 is appended after it).

NOT TESTED / USER: a hand-flown full mission (feel of the controls, balance of the AA fire and the bomb count), real key presses, sound (none).

## 9. Menu, gauges and the Bölüm 3 night level (Rota07 on the paper map), 2026-11-10

Batch mode, Unity 6000.6.4f1. Logs and results in `Logs/lab7_menu/` and `Logs/lab7_bolum3/`.

| Suite | Result |
|---|---|
| EditMode (whole project) | **243/243 passed**; includes the new `MenuTests` (7) and the rewritten `Lab7Tests`. Lab7Tests checks: every paper element on its square, river squares hold water, hills 120 m, runway on row 8, clouds at Orta, partial-collapse rule, dawn blend |
| PlayMode (whole project) | **116 passed, 4 skipped** (skipped = the explicit image tests, run separately and passed). New suites: `MenuPlayTests` (4), `Lab7Bolum3PlayTests` (5); `Lab7PlayTests` updated (one bridge, partial collapse, silent engine) |
| Image tests | `MenuGorselKayit`: menu panels and night HUD with gauges and pause menu at 1920×1080, 1280×720 and 2560×1080 (`Logs/lab7_menu/gorsel/`). `Lab7GorselKayit`: 16 views of the new level, incl. the paper area from above (`Logs/lab7/gorsel/`). All examined |
| Earlier scenes | Rota02 … Rota05 and Bolum3_GriKutu MD5 unchanged (test) |
| Players | `BuildWindowsOyun` (menu first) and `BuildWindowsRota07`: succeeded, 0 errors, 2 warnings as before. `Windows_Oyun` ran windowed for 15 s: no exceptions (`Logs/lab7_bolum3/player_oyun.log`) |
| Measurements | `Logs/lab7/ucus_olcumu.txt`, `Logs/lab7/bolum3_olcumu.txt` (see `Lab7_Implementation.md` §7) |

Defects found and fixed:
- **Menu labels:** `ToUpperInvariant` gave "SıNıR HATTı" and "GRI"; now tr-TR upper case, with a test. Long labels wrapped onto two lines; the label box is now one line high, so the text shrinks to fit.
- **Dawn:** the last step to full dawn was skipped by the small-change filter (sky 0.9987).
- **Searchlight lookup in the tests:** the towers sit under the "Hedefler" group, so `transform.root` gave the wrong object.
- **Teleports in the tests:** a test teleport from the runway swept through the airfield hut and counted as a crash. The tests now restart the structure sweep after a teleport (`CarpismaSifirla`); real flight is unaffected.

NOT TESTED / USER:
- A hand-flown night mission: bombing the narrow bridge, flying through the clouds, landing before dawn.
- Real key presses in the menus (the tests invoke the buttons).
- The paper steps 01–04 of the worksheet.
- The GDD in the course template.

## 10. Flight controls of the aerodynamic mode, 2026-11-10
- **Before/after handling record:** `Logs/lab7/ucus_tepki_once.txt` and `ucus_tepki_sonra.txt` (explicit EditMode test `Lab7UcusTepkiOlcumu`; table in `Lab7_Implementation.md` §3).
- **`Lab7UcusFizigiTests` 13/13**, with three new tests:
  - g limit at 110 m/s and wing limit at 60 m/s; protection on: no stall, off: stall;
  - a key tap gives a small input, a held key a full one;
  - the roll rate grows with V (pb/2V), the bank holds, small banks level out.
- **Whole project:** EditMode 245 passed + 1 explicit skipped; PlayMode 116 passed + 4 explicit skipped. These include the Rota07 take-off, the hands-off flight and the autopilot landing in the scene.
- **Players:** `Windows_Oyun` and `Windows_Rota07` rebuilt, 0 errors.
- **NOT TESTED / USER:** the feel with a real keyboard.

## 11. Assists in Rota07: easy flight, brake, targeting, GPS, 2026-11-10
Batch mode, Unity 6000.6.4f1. Logs and results in `Logs/lab7_yardim/`; measurements in `Logs/lab7/yardim_olcumu.txt`. Design: `Lab7_Implementation.md` §11, D-095 … D-100.

| Suite | Result |
|---|---|
| EditMode (whole project) | **257 passed**, 1 explicit skipped (`editmode.xml`). New: `Lab7YardimTests` 12/12 |
| PlayMode (whole project) | **123 passed, 1 failed, 4 explicit skipped** (`playmode.xml`, 418 s). New: `Lab7YardimPlayTests` 8/8 |
| The one failure | `Lab3TerrainPlayTests.Flying_StraightTurningAndClimbing…` (Rota03, kinematic model; window centre one chunk behind at frame 222). Rerun alone twice: 4/4 and 4/4 passed (`playmode_lab3terrain_1/2.xml`). Frame-timing dependent; the code it exercises was not changed. Recorded as flaky, not fixed |
| After the last HUD text change | `Lab7YardimPlayTests` + `MenuPlayTests` 12/12 (`playmode_son.xml`) |
| Players | `BuildWindowsOyun` and `BuildWindowsRota07`: succeeded, 0 errors, 2 warnings as before |

Defects found and fixed while developing (each seen in a Play Mode trace):
- **Near-stall hard landing (6.6 m/s):** the player braked down to 128 km/h. Fix: the engine helps below 1.3·V_s and the air brake retracts below 1.4·V_s in easy flight.
- **Floating along the runway:** the floor took the shallowest descent to far flat samples. Fix: ahead, only ground rising above the floor here counts; over the runway the floor is 5 m below the ground, and below 15 m the aircraft settles by itself.
- **Flying into the mountain ring:** a hands-off aircraft left the map at 160 km/h. Fix: map-edge turn at 2300 m and full power while the floor climbs.
- **Stale serialized values** (look-ahead 5 s, climb 15°) overrode changed code defaults. Fix: `Lab7Yardim` re-adds the components.
- **Test bugs:** a teleport frame counted as a touchdown; counting all guided bombs instead of those for one target (the system goes on to the next target in range at once, as designed).

NOT TESTED / USER:
- The feel with a real keyboard: easy flight, brake, landing.
- Reading the GPS and the lock markers on the night screen (no image test was run for them).


## 12. Lab 8 (Bölüm 4 · Kurşun Dağları), phases 1–2, 2026-11-11
Batch mode, Unity 6000.6.4f1. Results and logs in `Logs/lab8/`; measurements in `harita_olcumu.txt`, `tren_olcumu.txt`, `dusman_olcumu.txt`. Design and test mapping: `Lab8_Implementation.md` §3 and §6.6.

| Suite | Result |
|---|---|
| Lab 8 EditMode (`Lab8Tests`, `Lab8DusmanTests`) | **21/21 passed** (`TestResults_EditMode_lab8.xml`) |
| Lab 8 PlayMode (`Lab8PlayTests`, `Lab8DusmanPlayTests`) | **13/13 passed** (`TestResults_PlayMode_lab8.xml`) |
| EditMode, whole project, after phase 2 | **278 passed**, 1 explicit skipped, 0 failed (`TestResults_EditMode_tum.xml`) |
| PlayMode, whole project, after phase 2 | **141/141 passed**, 0 failed, 543 s (`TestResults_PlayMode_eski.xml`). The name filter used here also ran the 4 explicit image tests (`Lab5/6/7GorselKayit`, `MenuGorselKayit`); they passed and rewrote their images under `Logs/` |
| PlayMode, whole project, after phase 1 | 127 passed, 4 explicit skipped, 0 failed (`TestResults_PlayMode_tum.xml`) |
| Earlier scenes | MD5 unchanged (Rota02 … Rota07, AnaMenu) |

The first whole-project run after phase 2 was stopped by Claude Code because the machine ran low on memory (not a test failure); it was repeated in the foreground with the user's approval.

Defects found by the new tests and fixed: rails buried under the Dar Vadi tank pad (1.39 m, D-104); `RayEgrisi.EnYakin` off by up to 0.5 m (D-105); enemy aircraft down to 40 m above the ground (now 80 m); tank pair damage 75 per zone (now 46, D-113).

NOT TESTED / USER:
- Playing phase 2 with a keyboard (enemies, machine gun, bombs).
- Visual check of the enemy prefabs, smoke and tracers on screen (no image test yet).

### 12.1 Phase 3 (HUD, Görev tamam, engine smoke, assists, GPS, main menu), 2026-11-11
| Suite | Result |
|---|---|
| Lab 8 + menu EditMode (`Lab8Tests`, `Lab8DusmanTests`, `MenuTests`) | 28/28 passed |
| Lab 8 + menu PlayMode (`Lab8PlayTests`, `Lab8DusmanPlayTests`, `Lab8Faz3PlayTests`, `MenuPlayTests`) | **23/23 passed** (`TestResults_PlayMode_lab8.xml`, 135 s) |
| EditMode, whole project | **278 passed**, 1 explicit skipped, 0 failed (`TestResults_EditMode_tum.xml`) |
| PlayMode, earlier labs (16 classes by name, image tests not run) | **120/120 passed**, 393 s (`TestResults_PlayMode_eski.xml`) |

The PlayMode suite was run in two foreground parts (earlier labs; Lab 8 + menu) instead of one 10-minute background run, after the memory stop of the first attempt. Together they cover every PlayMode test except the 4 explicit image tests.

Defect found and fixed: the guided bomb circled its aim point 1 m above the ground (trace in `faz3_olcumu.txt`); now the aim point is on the ground and point guidance has a proximity fuse (D-116).

Intended test changes: `MenuTests` and `MenuPlayTests` now expect GÖREVE BAŞLA → Bölüm 4 and 7 levels (D-118).

NOT TESTED / USER:
- The HUD, markers, GPS, smoke and tracers have not been looked at on screen.
- Playing with a keyboard: enemies, machine gun, bombs, assists.

### 12.2 Phase 4 (look, full round, asset register, build), 2026-11-11
| Suite / step | Result |
|---|---|
| Lab 8 + menu EditMode (+ `Lab8VarlikTests`) | **30/30 passed** |
| Lab 8 + menu PlayMode (+ `Lab8TurTests`) | **24/24 passed**, 207 s |
| TEST-827 full round (scripted pilot, game's firing assist, 5×) | Train at the safe zone after 300 s with 98 % health; 12/12 fire sources silenced with 120 rounds and 2 bombs; base reached 30 s later; Görev tamam; total 5:31 (`tur_olcumu.txt`, images `gorsel/tur_*.png`) |
| `BuildWindowsOyun`, `BuildWindowsBolum4` | **Succeeded, 0 errors**, 17 warnings each (`unity_BuildWindows*.log`) |
| Player smoke run (`Windows_Bolum4`, `-batchmode -nographics`, 25 s) | Scene loaded, no exception or error in `Logs/lab8/player_bolum4.log`. Warnings: the tree and rock prototypes do not use the Nature/Soft Occlusion shader (distant billboards may look wrong) |

Not run after phase 4: the earlier labs' PlayMode tests. Phase 4 changed only Lab 8 code and the Lab 8 scene (shared scripts untouched), and the phase 3 run covered them.

NOT TESTED / USER:
- Playing the built game with a keyboard: flying, the enemies, the machine gun, the bombs, the assists (K, O, T), the GPS (M).
- On-screen look in a graphics run: the images above are camera renders; the tree billboards at a distance have not been seen.

### 12.3 After the user's first play: arcade flight, sound, train marker, 2026-11-11
| Suite | Result |
|---|---|
| `Lab8ArkadTests` (key-style inputs) | **6/6 passed**: hands off 0.0 m in 8 s; D 1.5 s → 81°, release → 0.3°/s, wings level; S 1 s → 34°, level 2.5 s after release; no crash diving or into the Dar Vadi wall; map edge; K cycle; engine 55 → 85 Hz, shots heard; train marked behind (`arkad_olcumu.txt`) |
| EditMode, whole project | 280 passed, 1 explicit skipped |
| PlayMode, Lab 8 + menu | 24/24 passed |
| PlayMode, earlier labs (`UcusKontrol`, `UcusYardimcisi` changed) | 120/120 passed |

NOT TESTED / USER: flying the arcade mode with a real keyboard; hearing the synthesised sounds.

### 12.4 Phase 5: submission copy (`teslim_h8.ps1 -OgrenciNo TEST`), 2026-11-11
| Step | Result |
|---|---|
| File list | 261 files (`Logs/lab8/teslim_listesi.txt`); 11 GUIDs already unresolved in the full project (URP template settings, `teslim_taban_guid.txt`) |
| Copy opened in Unity (`TeslimKopyasi.Dogrula`) | Build list = AnaMenu, Bolum4_KursunDaglari; **0** missing scripts and unresolved references (`teslim_dogrulama.txt`) |
| Player built from the copy | Succeeded, 0 errors, 17 warnings, 177 MB; smoke run 20 s (`-batchmode -nographics`): no error (`player_teslim.log`) |
| Zip | `Teslim/BMU1421_TEST_H8.zip`, 39 MB, root: Assets, Packages, ProjectSettings (no video in the test) |

Two failed attempts before: a compile error (obsolete `objectReferenceInstanceIDValue` in Unity 6.6) and package files failing to unpack under a long working path (Windows path limit); both fixed (D-125).

NOT TESTED / USER: the zip with the real video and student number; the upload.
