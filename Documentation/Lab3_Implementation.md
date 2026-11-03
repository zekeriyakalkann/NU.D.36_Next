# Lab 3: Pilot Koltuğu (Rota03)

Date: 2026-10-19. Source: the Lab 3 task text supplied in the conversation ("GÖREV: NU.D.36 — LAB 3: PİLOT KOLTUĞU"), including the worksheet parameters it quotes. The worksheet itself (föy) was not available as a file. Where the task text only says "as in the worksheet", the choice below is marked as a decision.

Status values: PASS (tested and passed), FAIL, NOT VERIFIED (insufficient evidence), NOT APPLICABLE.

## 1. What was built

| Item | Location | Notes |
|---|---|---|
| Rota03 scene | `Assets/Scenes/Rota03.unity` | Copy of Rota02 (`AssetDatabase.CopyAsset`, the Save As equivalent), made by `NU36.Editor.Lab3SceneBuilder.CreateRota03`. Appended to Build Settings (Rota02, SampleScene, Rota03). |
| Ucus action map | `Assets/InputSystem_Actions.inputactions` | Yunuslama W/S, Yatis D/A, Sapma E/Q, Gaz ↑/↓. Value/Axis actions, 1D Axis composites, `Keyboard&Mouse` group. The Player and UI maps and the 5 control schemes are unchanged. |
| UcusKontrol | `Assets/Scripts/Runtime/UcusKontrol.cs` | Transform-based flight, PlayerInput Send Messages callbacks, `Hiz` and `GazOrani` public. |
| Saglik | `Assets/Scripts/Runtime/Saglik.cs` | `Can`, `CanDegisti(int)`, `Oldu`, `Vur(int)`. Starts at 100, never goes below 0, raises death once. |
| CanYazisi | `Assets/Scripts/Runtime/CanYazisi.cs` | Observer. Subscribes in OnEnable and unsubscribes in OnDisable; uses only Saglik's public event. |
| TakipKamerasi | `Assets/Scripts/Runtime/TakipKamerasi.cs` | Local-space offset, SmoothDamp, LookAt, field of view from throttle, LateUpdate. |
| Lab 3 build | `Builds/Windows_Rota03/NU.D.36.exe` | `NU36.Editor.BuildCommands.BuildWindowsRota03`: same scenes, Rota03 first. Build Settings order is not changed. |

The task text named `Assets/Scripts/UcusKontrol.cs` and `Assets/Scripts/TakipKamerasi.cs`. Neither `TakipKamerasi` nor `UcusKontrol` existed in the project. The new scripts follow the existing architecture: `Assets/Scripts/Runtime/`, namespace `NU36`, assembly `NU36.Runtime` (D-025).

### Rota03 wiring
- **Uçak:**
  - `HedefeGit` and `KanatYatisi`: kept but **disabled**. `KanatYatisi` would add a second, Balon-directed bank on top of player roll.
  - Added: `UcusKontrol`, `PlayerInput` (project Input Actions asset, default map `Ucus`, Behavior `Send Messages`), `Saglik`, `CanYazisi`.
- **Main Camera:** `TakipKamerasi`. Its target is Uçak and its flight reference is that UcusKontrol. The start pose is the follow offset.
- **Unchanged from Rota02:** Uçaksavar's `Radar` (target Uçak), Balon, the positions, and the light.

### Worksheet parameters kept
`enAzHiz` 8, `enCokHiz` 42, `yunuslamaHizi` 55, `yatisHizi` 90, `sapmaHizi` 30, `yumusatma` 6, start throttle 0.5. Camera: offset (0, 3.2, -11), smoothing 0.18, field of view 55–72°.

### Decisions (DecisionLog D-025 … D-033)
- **Throttle (D-028).**
  - The worksheet's `OnGaz: gaz += v.Get<float>() * 0.02f` only changes the throttle when a callback arrives. With Send Messages a callback arrives only when the value changes. This was verified in the PlayerInput source ("we only care about `performed` and, for value actions, `canceled`") and measured: one `OnGaz(1)` on press, one `OnGaz(0)` on release, none while held.
  - With the worksheet form, each key press would add only 0.02.
  - Implemented instead: `OnGaz` stores the axis value, and `Update` adds `axis × 1.2/s × deltaTime`, clamped to 0..1. 1.2/s is the worksheet's 0.02 per frame at 60 FPS. Holding ramps smoothly, release holds the value, and repeated taps add proportionally to their duration.
- **Axis signs (D-029).**
  - Yunuslama W = +1 pitches the **nose down**, as a stick pushed forward. Yatis D = +1 rolls right. Sapma E = +1 yaws right.
  - The worksheet table only defines the keys. If it expects W to pitch the nose up, one sign flips.
- **Smoothing (D-030).** `Lerp(smoothed, raw, 1 − e^(−yumusatma·dt))`. This equals the usual `yumusatma·dt` for small steps but is frame-rate independent and cannot overshoot.

## 2. Requirement status

| Requirement | Evidence | Status |
|---|---|---|
| Rota02 preserved | SHA256 identical before and after; `Rota02_IsUnchangedByLab3`; all Lab 2 tests pass | PASS |
| Rota03 exists, opens, no missing scripts or references | `Rota03_HasNoMissingScriptsOrReferences`; PlayMode smoke test loads all build scenes | PASS |
| Build Settings: Rota03 appended, earlier order kept | `BuildSettings_AppendRota03AndKeepEarlierOrder` | PASS |
| HedefeGit kept but disabled; no conflict | `Rota03_Lab2AutomationIsKeptButDisabled`; PlayMode: heading stays +Z with no input | PASS |
| Ucus map: 4 Value/Axis actions, 1D Axis, correct keys | `Lab3InputActionsTests` (6) | PASS |
| Existing maps and schemes preserved | Same test; JSON diff: bindings identical, only serializer defaults added (`version`, `priority`) | PASS |
| Positive and negative keys give +1 and −1 | PlayMode, simulated keyboard: all 8 keys (`girdi_yonleri.csv`) | PASS |
| PlayerInput: asset, Ucus map, Send Messages, callbacks | `Rota03_PlayerInput…`, `PlayerInput_IsOnUcusMap…`, callback test | PASS |
| No direct key polling in code | `UcusKontrol` reads only `InputValue` from callbacks | PASS (code review) |
| Rotation follows input | EditMode: 6 direction tests and 3 rate tests (55, 90, 30 °/s); PlayMode: D, E, W held | PASS |
| Throttle sets speed between 8 and 42 m/s | EditMode and PlayMode (8, 25, 42 m/s) | PASS |
| Movement uses deltaTime | Same distance at 30 and 144 FPS; PlayMode distance = Hiz × elapsed time | PASS |
| Saglik: 100 → Vur(10) → 90; floor 0; death once; events | `Lab3SaglikTests` (8) | PASS |
| Listener subscribes and unsubscribes; null safety | Same tests: disable, re-enable, destroy, missing Saglik | PASS |
| Camera: target wired, LateUpdate, follows, field of view 55–72 | `Lab3KameraTests` (11), PlayMode follow and field-of-view test | PASS |
| Lab 2 camera behaviour unaffected | Rota02 camera unchanged; Lab 2 framing tests pass | PASS |
| Keyboard control in the built exe | Exe starts in Rota03 and the camera follows (screenshot). Real key presses were not possible in this session | NOT VERIFIED |

## 3. Experiments

Files are in `Logs/lab3/`. `*_batch.csv` files come from the batch-mode run; the files without that suffix come from the Editor GUI run.

| # | Experiment | Result | Status |
|---|---|---|---|
| 1 | Movement without deltaTime (0.2 per frame) | Distance is exactly 0.2 × frames: 6.00, 12.01 and 28.69 m/s at 30, 60 and 143 FPS | PASS |
| 2 | 30 vs 144 FPS | `Application.targetFrameRate` with `vSyncCount` 0 (it was already 0; restored after). Measured 30.0 / 60.0 / 143.4 FPS in the GUI and 30.0 / 60.0 / 143.9 in batch mode, so the targets were applied. Controlled run with `Time.captureDeltaTime`: A = 12 / 24 / 57.6 m in 2 s | PASS |
| 3 | Movement with deltaTime (12 m/s) | 12.00 m/s at every frame rate; 24.0 m in 2 s in the controlled run | PASS |
| 4 | yumusatma 1 vs 20 | Time to 90 % of a step input: 2.317 s (k = 1), 0.400 s (k = 6), 0.117 s (k = 20); theory ln 10 / k. How it *feels* to fly is not measured | PASS (numeric); feel NOT VERIFIED |
| 5 | Camera in Update vs LateUpdate | Update variant ordered before the flight. Constant 1/60 s frames: no difference. Uneven frames (1/30 and 1/144): LateUpdate jump 0.000, Update 0.0071 viewport units per frame (aircraft shakes about 5 px at 720p). Measured, not watched by eye | PASS (measured); visual flicker NOT VERIFIED |
| 6 | Camera smoothing 0.02 vs 0.6 | Follow lag at 42 m/s: 0.53 m (0.02), 7.2 m (0.18), 24.9 m (0.6). For 0.18 and 0.6 this matches the continuous theory v × T within 5 %. At 0.02, about one frame, SmoothDamp's discrete step gives less lag than the formula | PASS (measured); feel NOT VERIFIED |
| 7 | Field of view changes with throttle | 55° (gas 0), 63.5° (0.5), 72° (1). Screenshots `rota03_gaz_0.png` and `rota03_gaz_1.png` | PASS |
| 8 | Damage event reaches the listener | `Vur(10)` → `[CanYazisi] Uçak: can = 90`, in the test scene and in Rota03 | PASS |

Additional measurement: in the throttle callback experiment (`gaz_callback_deneyi.csv`), a 0.121 s hold raised the throttle 0.500 → 0.645 (expected +0.145), the throttle held after release, 3 short taps added 0.039, and holding ↓ / ↑ for 1.5 s reached 0 (8 m/s) / 1 (42 m/s).

## 4. Visual evidence (examined)
- `Logs/lab3/rota03_baslangic_gaz_0.5.png`: chase view from behind and above; Balon visible.
- `Logs/lab3/rota03_yatis_D.png`: after holding D, the right wing is clearly lower.
- `Logs/lab3/rota03_gaz_1.png` and `rota03_gaz_0.png`: at full throttle the aircraft is smaller (wider field of view, longer lag); at zero throttle it is larger.
- `Logs/lab3/player_rota03_ucus.png`: the built player in Rota03 with the follow camera.

## 5. Test infrastructure notes
- Keyboard input is simulated with a virtual Keyboard and Mouse added to the Input System. PlayerInput is re-paired to them and they are removed afterwards. `InputTestFixture` was tried first: its reset breaks the enabled project-wide actions asset (NullReferenceException in `InputActionState`), so it is not used (D-033).
- The Editor only forwards keyboard input to Play Mode while the Game view is focused, and batch mode has none. The input tests switch `InputSystem.settings` to "all input goes to the game" and back. The project has no Input settings asset, so nothing is written to disk.
- Experiment helpers (`KareBasinaHareket`, `ZamanaBagliHareket`, `UpdateIleTakip`, `KareSonuOrnekleyici`, `GazCagriSayaci`) live in the PlayMode test assembly only.

## 6. Open items
- **Keyboard flight in the built exe or the Editor:** a person needs to press W/S, A/D, Q/E and ↑/↓ once (NOT VERIFIED here).
- **Subjective feel** (smoothing 1 vs 20, camera 0.02 vs 0.6, Update jitter by eye): only measured numerically.
- **W pitch sign:** confirm against the worksheet (D-029).
- **Git:** not initialized; needs authorization.
- **Unity 6.3 LTS:** not attempted. The project stays on 6000.6.4f1, where all Lab 3 APIs used compile and run.
