# Flight model and control feel (Rota04Hud)

Date: 2026-10-20. Status: IMPLEMENTED and tested automatically; **not flown by a person after the change** (see section 7). Backups before the change: `Exports/flight_backup_2026-10-20/` (UcusKontrol.cs, TakipKamerasi.cs, Rota04Hud.unity, RESTORE.txt).

## 1. How the aircraft is moved (diagnosis, answers to the ten questions)
| # | Question | Answer (code and scene read, then measured) |
|---|---|---|
| 1 | Rigidbody or Transform? | **Transform.** `UcusKontrol.Adim` rotates and moves `transform` every `Update`. The aircraft also has a kinematic Rigidbody and a box collider (Lab 4 phase 1) only so that trigger volumes fire; physics does not move it |
| 2 | Local axes | Forward = +Z, up = +Y, right = +X of the root; pitch about local X, yaw about local Y, roll about local Z (`Space.Self`) |
| 3 | Keys | Actions in the `Ucus` map: W/S pitch (W = +1 = nose down, D-029), D/A roll (D = right wing down), E/Q yaw, Up/Down throttle |
| 4 | Does the throttle change the real speed? | Yes, but **instantly**: speed = Lerp(8, 42, throttle). With the throttle ramp of 1.2/s the speed went 25 to 42 m/s in 0.42 s (40.8 m/s², about 4 g) and 42 to 8 in 0.83 s |
| 5 | Does it bank in a turn? | **No.** Roll and yaw were independent. A banked aircraft kept flying straight (45 degree bank, heading change 0 degrees in 3 s) and stayed banked after the key was released; only the yaw key turned it (55 degrees in 2 s, flat, bank 0) |
| 6 | Orientation vs direction of motion | Consistent: it always moves along `transform.forward`, no sideslip |
| 7 | Input lag or conflict | Inputs are smoothed (1 - e^(-6 dt)); the aircraft keeps rotating about 9 degrees after a pitch key is released (0.17 s tail). No second script moves it in the flying scenes: `HedefeGit` and `KanatYatisi` are disabled in Rota03, Rota04 and Rota04Hud (test); `InisAlgilayici` only clamps the height after a landing |
| 8 | HUD vs real movement | Consistent: the dials read `UcusKontrol.Hiz` and the transform y, the compass the yaw (PlayMode test, frame by frame) |
| 9 | Gravity, drag, speed limit | None, except the speed limits 8 and 42 m/s. Climbing and diving did not change the speed |
| 10 | Ground | Rota03: no terrain collision, the aircraft can pass through the ground (D-038). Rota04 and Rota04Hud: `InisAlgilayici` classifies contact as landing or crash (phase 2) |
Also found: the chase camera rotated its offset with the whole aircraft, so rolling 90 degrees swung the camera 4.5 m sideways; a single long frame (a hitch) moved the aircraft 14 m at once; the loop (pitch held 7 s) worked without roll drift and frame-rate dependence was already small (0.25 m over an 85 m path between 30 and 60 FPS). These two were not problems and were left alone.
Measurements before and after: `Logs/flight/probe_before_changes.txt`, `probe_lab3_defaults.txt` (identical to before: the Lab 3 behaviour is preserved), `probe_rota04hud_model.txt`.

## 2. What was changed
All new parameters of `UcusKontrol` and `TakipKamerasi` are **opt-in and zero/false by default**, so Rota02, Rota03 and Rota04 fly exactly as before (tests assert it). Rota04Hud switches them on (`NU36 > Lab 4 > Apply Flight Model To Rota04Hud`, batch `-executeMethod NU36.Editor.Lab4FlightSceneBuilder.Apply`, re-runnable).
| Parameter | Rota04Hud | Reason (from the diagnosis) |
|---|---|---|
| `hizIvmesi` | 6 m/s² | speed no longer jumps: 8 to 42 m/s in 5.7 s, 25 to 42 in 2.8 s (was 0.4 s). 6 m/s² is a light-aircraft order of magnitude and makes the speed dial move visibly |
| `egimHizKaybi` | 12 m/s per unit of forward.y | a 45 degree climb at full throttle settles at 33.5 m/s, a 45 degree dive at half throttle at 33.5 m/s; always inside 8 to 42 so the HUD scale and the landing limits keep their meaning |
| `yatisDonusu` | 1 | a banked aircraft turns at the coordinated-turn rate g·tan(bank)/speed about the world up axis: 30 degrees at 25 m/s turns 13 degrees/s, and it turns faster when slow. A level turn keeps its altitude |
| `enFazlaDonusHizi` | 45 degrees/s | cap for steep bank at low speed |
| `yatisDengeleme` | 1.5 /s | the bank returns to level when D/A is released (it fades in as the smoothed input dies, rate capped at 60 % of the roll rate, no step); skipped when inverted or with the nose near vertical so a loop is not rolled upright (test) |
| `enFazlaYatis` | 65 degrees | the roll key stops at 65 degrees (not applied inverted): controllable, no barrel-roll tumbling by accident; loops with pitch still work |
| `enFazlaAdim` (all scenes) | 0.1 s | `Update` cuts a long frame so a hitch moves the aircraft at most 4.2 m instead of 14 m; no effect at 10 FPS or more |
| `yunuslamaTers` | false | switch for the W key direction; not changed because the user has not decided D-029 |
| Camera `ufukKilitli` | true | the camera offset follows only the heading, so the aircraft's roll and pitch are visible in the view and the camera does not swing; the heading frame swings at most 180 degrees/s (it would otherwise jump 22 m when the nose passes vertical in a loop) |
The pitch, roll and yaw **rates** (55, 90, 30 degrees/s), the smoothing (6/s) and the throttle ramp (1.2/s) were not changed: nothing measured argued against them.

## 3. Files
Changed: `Scripts/Runtime/UcusKontrol.cs`, `Scripts/Runtime/TakipKamerasi.cs`, `Scenes/Rota04Hud.unity` (only the values above). New: `Scripts/Editor/Lab4FlightSceneBuilder.cs`, `Tests/EditMode/Lab4FlightModelTests.cs`, `Tests/EditMode/FlightProbeTests.cs`, `Tests/PlayMode/Lab4FlightPlayTests.cs`. HUD code, HUD sprites, Rota02, Rota03, Rota04 and SampleScene were not touched (MD5 identical).

## 4. Tests actually run (2026-10-20, batch mode)
| Run | Result |
|---|---|
| EditMode, whole project | 147 passed, 0 failed (123 earlier, 22 new flight-model tests, 2 probe cases) |
| PlayMode, whole project (graphics on) | 76 passed, 0 failed (72 earlier, 4 new) |
Flight-model EditMode tests (fixed time step, no scene): Lab 3 defaults unchanged; the speed rises and falls at no more than 6 m/s², stops at 8 and 42 m/s; climb and dive shift the speed as designed and the top speed is not exceeded; the distance flown equals speed x time; pitch, roll and yaw keys move in the expected directions (and the reversed pitch switch works); a banked aircraft turns at g·tan(bank)/speed to the right or left, holds its altitude, the turn is capped, faster at low speed; the bank returns to level on release without changing the pitch; it is not levelled while the key is held and stops at 65 degrees both sides; a full loop stays in its plane with wings level; pressing and releasing keys never changes the rotation rate by more than 0.45 degrees per frame and no position step exceeds the top speed; a scripted manoeuvre differs by under 3 % of the path and 3 degrees between 30, 60 and 144 FPS (measured 0.39 m of 52 m, 0.4 degrees); long frames are cut; the chase camera (default unchanged, horizon-locked ignores roll and pitch, follows heading, no jump over a full loop); Rota04Hud values; Rota03 and Rota04 keep zeros; `HedefeGit` and `KanatYatisi` disabled; one kinematic Rigidbody.
PlayMode tests in the real Rota04Hud scene (inputs set through the same public properties the Input System callbacks use): throttle up raises the speed gradually (about 31 m/s after 1 s), the distance per frame equals speed x frame time within 1 %, the HUD dial, needle, throttle knob and altitude equal the real values; banking turns the aircraft right, the bank stays at or below 65 degrees, levels out within 3 s of the release, the altitude changes under 2 m, compass = yaw; pressing and releasing pitch and roll keys never exceeds 170 degrees/s or the top speed; the camera is horizon-locked.
Lab 2 and Lab 3 tests: part of both full runs, all passed (Lab 3 flight tests, camera tests, terrain and input tests unchanged).

## 5. HUD compatibility
No HUD code or asset changed. The HUD tests passed again on the new flight model. Speed dial = real speed (it now lags the throttle on purpose); throttle knob = real throttle; altitude = aircraft world y (not height above the terrain, as documented in `Lab4_Hud_Implementation.md`); compass = yaw. No HUD bug found.

## 6. Limitations (not changed)
- No stall, drag or lift model; the aircraft cannot stall at low speed and the speed limits are hard clamps.
- W = nose down is unchanged (D-029); `yunuslamaTers` flips it.
- In Rota03 the aircraft still passes through the ground (no terrain collision, D-038).
- The pitch key keeps the nose attitude after release (no pitch leveling), as before.
- The turn rate formula is the coordinated-turn rate; the aircraft has no sideslip and no adverse yaw.

## 7. Needs a manual flight (not done)
Everything above was checked with simulated inputs and frame-stepped tests. **No person flew it with a real keyboard after the change**, so the following are NOT VERIFIED and are the values to tune by feel: `hizIvmesi` 6, `yatisDengeleme` 1.5, `enFazlaYatis` 65, `enFazlaDonusHizi` 45, the camera swing 180 degrees/s, and whether the speed bleed (12) in climbs is noticeable. All are Inspector fields on the aircraft and the camera in Rota04Hud.
