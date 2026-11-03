# Lab 4: display-only HUD in Rota04Hud

Date: 2026-10-20. **Status: IMPLEMENTED and tested; the official Phase 4 text is still missing**, so everything marked INFERRED or "open" below is a decision you can reverse. Plan and open questions: `Lab4_Phase4_Plan.md`. Assets: `Lab4_Phase3_Assets.md`.

## 1. Scene decision
`Assets/Scenes/Rota04Hud.unity` is a copy of Rota04 (`AssetDatabase.CopyAsset`, the Save As equivalent, D-039 pattern) plus a `HUD` object. Reason: no handout text says which scene must carry the HUD, so the safe choice is a copy; Rota04 and all earlier scenes stay byte-identical (MD5 asserted by tests). Appended as the fifth Build Settings entry. In the copy only these existing fields changed: `GorevSonuEkrani.yenidenBaslatSahnesi` and `YenidenBasla.sahne` = `Rota04Hud` (a restart must not drop the HUD) and `GorevSonuEkrani.durumSatiriGoster` = false (the HUD shows the mission line; the OnGUI success and failure screens stay). Builder: menu **NU36 > Lab 4 > Create HUD Scene (Rota04Hud)** or `-executeMethod NU36.Editor.Lab4HudSceneBuilder.Create` (refuses to overwrite). If you prefer the HUD inside Rota04, run the same builder logic on Rota04 and update the MD5 guard in `Lab4HudTests`.

## 2. Hierarchy (Canvas, Screen Space - Overlay)
`HUD` (Canvas, CanvasScaler: Scale With Screen Size, 1920x1080, match mode **Expand**, `HudMetin`)
- `YanPanelSol`, `YanPanelSag`: solid-colour side panels (colours sampled from the frame PNG, brass edge line, no new art). Zero width at 16:9 and narrower; they continue the frame's pod body on wider screens.
- `Kok` (1920x1080, anchored bottom centre, never stretched): `Cerceve` (hud-cerceve), `HizKadrani` and `IrtifaKadrani` (dial halves as RawImage uvRect, `HudGosterge`, child `Ibre` = gosterge-ibre), `GazPlakasi` (gaz-kolu-plakasi, `HudGazGostergesi`, child `GazKolu` = gaz-kolu-topuz), `Pusula` (RawImage of the Repeat ribbon, `HudPusula`, child `Isaretci` = separate red pointer, a solid-colour Image).
- `GorevMetni` and `SureMetni`: legacy `Text` (built-in LegacyRuntime font, Turkish glyphs checked in the screenshots) with a dark `Outline`, anchored to the canvas top-left corner.
Expand scaling = min(width/1920, height/1080), so the 16:9 frame keeps its proportions and the dials stay in their sockets.

## 3. Components and data sources (display only, all `LateUpdate`, namespace NU36)
| Component | Reads | Shows |
|---|---|---|
| `HudGosterge` (Hiz) | `UcusKontrol.Hiz` (m/s) | needle z = -(-135 + v/50*270), clamped; 0 to 50 m/s |
| `HudGosterge` (Irtifa) | aircraft `transform.position.y` (m) | needle on 0 to 500 m. It is the **world y of the aircraft pivot**, not a height above the terrain (the start ground is at y = -1); the dial says IRTIFA as the Blender art does, the meaning is an open decision |
| `HudGazGostergesi` | `UcusKontrol.GazOrani` | knob y between -200 (0 %) and +200 (100 %) in plate units; not clickable |
| `HudPusula` | aircraft yaw (`eulerAngles.y`, 0 = world +Z) | `uvRect.x = h/360 - window/2`, window 0.5. **Assumptions:** that the ribbon should move (`kaydir`, default on) and that world +Z is "N" (`sifirOfseti` 0): neither is confirmed; one checkbox / field each |
| `HudMetin` | `GorevYoneticisi` (Durum, Kalan, DonusAsamasi, KalanSure) and `PuanCuzdani.Puan` | the same strings as the OnGUI line ("Kalan hedef: n   Puan: x", "Üsse dönün", "Süre: m:ss", red at 10 s or less); hidden when the mission is completed or failed |
`HudHesap` holds the pure math. No flight value is computed twice: the HUD only reads what `UcusKontrol`, `GorevYoneticisi` and `PuanCuzdani` already expose. No existing runtime script was changed.

## 4. Other changes
- `NU36.Runtime`, `NU36.Editor`, `NU36.Tests.EditMode`, `NU36.Tests.PlayMode` asmdefs got a reference to `UnityEngine.UI` (package `com.unity.ugui` was already installed; no package change).
- `Lab4SceneTests.BuildSettings_KeepEarlierOrder_AndAppendRota04`: now asserts the first four scenes only (it required exactly four; same kind of change as in phase 1 for Lab 3).
- Not built: target markers (meaning not confirmed), score table in the game (see below), menu scene, clickable throttle, stored scores.

## 5. Tests actually run (2026-10-20, batch mode, Unity 6000.6.4f1)
| Run | Result |
|---|---|
| EditMode, whole project | 123 passed, 0 failed (113 earlier + 10 new in `Lab4HudTests`) |
| PlayMode, whole project (graphics on) | 72 passed, 0 failed, 0 skipped (67 earlier + 5 new) |
New PlayMode tests (`Lab4HudPlayTests`, `Lab4MenuPlayTests`): needles, knob and compass follow speed, throttle, altitude and heading in the real Rota04Hud; end stops (altitude 900 m stays at the end of the scale, zero throttle gives the lowest knob position); text follows score, mission and countdown and hides on failure; screenshots at four resolutions with pixel checks; menu and score table with real text at three resolutions, with the contrast measured on the rendered text. New EditMode tests: dial, throttle, heading and text math; Rota04 and earlier scenes byte-identical; Rota04Hud has no missing scripts, same root objects as Rota04 plus `HUD`, restart fields, Expand scaler, every component wired.
Screenshots: `Logs/lab4_hud/hud_*.png` (real Rota04Hud, Play Mode), `Logs/lab4_ui_review/menu_text_*.png`, `scoretable_text_*.png`, contrast table `menu_text_contrast.txt`. All were looked at.

## 6. Resolutions (looked at in the screenshots)
| Resolution | Result |
|---|---|
| 1280x720, 1920x1080 | frame fills the width, dials in their sockets, texts and compass readable |
| 2560x1080 (21:9) | frame keeps 16:9 in the middle; the side panels continue the pod body to the screen edges (checked by pixel test and by eye); the game view stays visible above them |
| 1024x768 (4:3) | frame fills the width and is smaller; all components readable, the throttle plate stays at the right edge, texts are legible |
The screenshot test renders the canvas into a texture with the scale factor set by hand to the Expand value (min of the two ratios); the real CanvasScaler configuration is asserted in the EditMode test, but "the scaler computes the same factor at those sizes" was not observed in a running Game view.

## 7. Menu and score table with real text
Menu (cream bold text on the Blender button states): drawn contrast 7.5 (BAŞLA), 4.9 to 5.0 (SEÇENEKLER), 10.5 (ÇIKIŞ); the disabled GERİ is 2.8 and is exempt as a disabled control (a brighter colour would also make it look enabled). Score table: header 5.7 to 5.8, rows 8.2 to 10.2. Measured on the rendered pixels (1st and 99th percentile luminance inside each label), at 1920x1080, 1280x720 and 2560x1080 (menu). SEÇENEKLER is the weakest (4.9 against the 4.5 target); a darker hover face or a text outline would raise it. The table shows **only the current score and time**, no list. These are throw-away test scenes: the menu and table are not part of the game, the menu text strings are placeholders.

## 8. Known gaps and open decisions
- Phase 4 handout text still missing; U1 to U10 in `Lab4_Phase4_Plan.md` stand. New in this step: compass movement and "N = +Z" (both switchable), altitude meaning, whether 21:9 side panels are wanted (disable the two objects to remove them).
- The HUD was checked in screenshots and PlayMode tests with **simulated** flight (set throttle, position, heading), not by a person flying: NOT VERIFIED with real key flight, nor in a built player (no build was made; Rota04Hud is in Build Settings).
- The compass pointer is a plain red line on top of the red "N": at heading 0 the "N" is partly hidden. A thinner pointer or a different pointer colour is a polish item.
- Target markers, score table and menu are not in the game scene.
- No Game-view test of real resolution changes (window resizing); the Expand scaler is Unity's standard component.
