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
