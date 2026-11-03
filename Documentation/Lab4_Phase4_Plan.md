# Lab 4, Phase 4: HUD integration plan (not implemented)

Update 2026-10-20 (later): a display-only HUD was implemented in `Rota04Hud` on the user's request; see `Lab4_Hud_Implementation.md`. The rows below about the missing handout and the open questions U1 to U10 still apply; the HUD candidates P4-01 to P4-06 were built as inferred choices, P4-07 to P4-09 were not.

Date: 2026-10-20. **Status then: PLAN ONLY.** The Phase 4 handout is not in the workspace (searched `Documentation/`, `Belgeler/`, `README.md`, `CLAUDE.md`, the decision log and all Lab 4 documents: the only mentions are our own notes saying the text is missing). No scene, script or asset was changed in this step. Everything below that is not tagged FOUND is a candidate for you to confirm, not a requirement.

## 1. Asset check (re-run 2026-10-20)
- Ten PNGs in `Exports/Lab4_UI/`, `NU.D.36_Unity/Assets/UI/` (byte-identical, hash compared, all with `.meta`). Sizes and alpha match `Lab4UiAssets.Specs` (`Verify-Lab4Assets.ps1`, `Logs/lab4_ui_verify.txt`); Unity `Validate UI Assets` on 2026-10-20 12:22: 0 problems.
- Ten Blender sources in `Blender/UI/`, scripts in `Tools/Blender/ui/` (all ten `.blend` reopen, `Logs/lab4_ui_blend_verify.txt`).
- The folder `Tools/Lab4ADocumentation/` named in the task does not exist. `Tools/Lab4Assets/` holds the verify script.

## 2. What exists in the Unity project (FOUND in code)
| Area | State |
|---|---|
| Scenes | Rota02, Rota03, Rota04, SampleScene. **No main-menu scene.** Rota04 (Lab 4) is the only mission scene |
| UI | **No Canvas, no EventSystem** in Rota03 or Rota04. The only UI is the temporary `OnGUI` screen `GorevSonuEkrani` (status line, score, time, failure panel, restart button) |
| Packages | `com.unity.ugui` 2.6.0, `com.unity.inputsystem` 1.20.0, test framework 1.8.0 are present: no new package is needed |
| Flight data | `UcusKontrol.Hiz` (m/s, 8 to 42), `GazOrani` (0 to 1), `EnAzHiz`, `EnCokHiz` |
| Altitude | not exposed; aircraft `transform.position.y` (world), terrain height from `AraziUretici.Yukseklik(x, z)` / `YuzeyYuksekligi` |
| Heading | not exposed; aircraft `transform.eulerAngles.y` (0 = +Z) |
| Mission | `GorevYoneticisi`: `Durum`, `Kalan`, `ZorunluSayisi`, `KalanSure`, events `KalanDegisti`, `DonusAsamasiBasladi`, `GorevTamamlandi`, `GorevBasarisiz` |
| Score | `PuanCuzdani.Puan`, event `PuanDegisti`, unlock event `UcakKilidiAcildi`. **No persistence, no ranking list** |
| Targets | `KesifHedefi` (3 required columns at about 430 m), base trigger `Us` |
| Input | Action map `Ucus` (keyboard); the project's `UI` map exists in the actions asset but no UI uses it yet |

## 3. Unknowns (no Phase 4 text): candidates to confirm
| # | Question | Blocks implementation? |
|---|---|---|
| U1 | Is the Phase 4 HUD required to show dials, throttle, compass, target markers, score table at all, and which of them | Yes, scope |
| U2 | Which scene: extend Rota04, or a new scene (copy of Rota04, as earlier labs did) | Yes, but copying Rota04 is the project pattern |
| U3 | Must the compass move with the heading, or is a static compass enough | Yes for the compass |
| U4 | Dial ranges (we drew 0 to 50 m/s and 0 to 500 m); what the altimeter measures (height above sea level or above the ground) | Minor; scripts can change |
| U5 | Throttle control: display only, or clickable/draggable (needs the `UI` action map) | Yes for the throttle |
| U6 | What the score table shows: this run, or a saved high-score list (needs persistence, player names) | Yes for the table |
| U7 | Menu: is a main menu scene part of Phase 4 (the menu background and button set exist) | Yes |
| U8 | Target marker meaning: aiming reticle, "locked", objective diamond, base marker: which should react to what | Minor |
| U9 | The failure/success screens: keep the current `OnGUI` or move to the new UI | No, can wait |
| U10 | Required resolutions or aspect ratios | Minor; plan uses Canvas Scaler 1920x1080 |

## 4. Requirements matrix (candidates, not confirmed)
Status legend: FOUND = the code or asset exists today; CANDIDATE = depends on a confirmed Phase 4 requirement.

| ID | Candidate requirement | Source | Current state | Change needed | Asset | Acceptance (engineering recommendation) | Test |
|---|---|---|---|---|---|---|---|
| P4-01 | A HUD canvas in the mission scene | none (candidate) | none | Canvas (Screen Space - Overlay), Canvas Scaler 1920x1080 match 0.5, EventSystem only if input is needed | hud-cerceve | Frame anchored to the bottom, stretched; no missing references | EditMode scene test, screenshots at 1280x720, 1920x1080, 2560x1080 |
| P4-02 | Airspeed dial | none | `UcusKontrol.Hiz` FOUND | `HudKadran` (reads Hiz, rotates the needle) | gosterge-kadranlari (left half), gosterge-ibre | needle z = -(Hiz/50*270 - 135) deg; Hiz 8 and 42 give the right angles; Hiz above 50 clamps | EditMode math test, PlayMode in Rota04 |
| P4-03 | Altimeter | none | altitude source missing | add a read-only property or a HUD-side computation (`position.y`) | right half, needle | needle = -(h/500*270 - 135); decision U4 | EditMode, PlayMode |
| P4-04 | Throttle indicator | none | `GazOrani` FOUND | knob `anchoredPosition.y` from 912 (0) to 112 (100) in plate pixels, scaled | gaz-kolu-plakasi, gaz-kolu-topuz | knob at 0.5 sits at the middle of the track | EditMode, PlayMode with throttle keys |
| P4-05 | Compass | none (U3) | heading source missing | static: show only. Moving: `RawImage.uvRect.x = heading/360` with a fixed pointer Image over it | pusula-seridi (Repeat) | N at heading 0, E at 90; no seam at wrap | EditMode math, visual check |
| P4-06 | Score and mission readout in the new UI | Lab 4 phase 1 and 2 (FOUND) | OnGUI | text from `PuanCuzdani.PuanDegisti`, `GorevYoneticisi.KalanDegisti`, `KalanSure` | puan-tablosu or plain text | values equal the wallet and manager values | PlayMode |
| P4-07 | Target markers | none (U8) | `KesifHedefi` positions FOUND | world-to-screen markers for the three targets and the base | hedef-isaretleri (cut into four sprites) | marker follows target, hidden when collected | PlayMode |
| P4-08 | Score table / results screen | none (U6) | no persistence | layout only, or saved list | puan-tablosu | empty cells filled from data | EditMode, PlayMode |
| P4-09 | Main menu | none (U7) | no scene | new scene with `menu-arkaplan` and button states | menu-arkaplan, dugme-seti | buttons load the mission scene; Build Settings order unchanged | PlayMode, build |
| P4-10 | Keep Lab 2 and Lab 3 untouched | CLAUDE.md (FOUND) | passing | work in a copy of Rota04 or additive components | none | Rota02/03/04 files byte-identical (as the earlier scene tests assert) | existing 113 EditMode and 67 PlayMode tests |

## 5. Proposed integration (if you confirm U1 to U3)
- **Scene.** Copy `Rota04` to a new scene (same pattern as D-039) by an editor builder, append it to Build Settings. Rota04 stays byte-identical, so its tests keep passing. If you prefer to extend Rota04 itself, the Rota04 scene tests must be updated deliberately.
- **Hierarchy.** `HUD` (Canvas, Overlay) > `Cerceve` (hud-cerceve, stretch) > `HizKadrani` (left half sprite, 296 px) with `Ibre` child (pivot 0.5/0.5, rotation about the centre) > `IrtifaKadrani` (same) > `GazPlakasi` with `GazTopuzu` > `Pusula` (RawImage plus fixed pointer) > `Hedefler` (marker pool) > `Puan` text. Sprites: the dial sheet is one 1024x512 Single sprite, so the two halves are separate `Image`s with a rect crop (or a small editor step slices it; the import rule uses Single today).
- **Code (small, event-driven where events exist).** One read-only component per gauge in `NU36.Runtime`, polling `UcusKontrol` in `LateUpdate`; no change to `UcusKontrol`, `GorevYoneticisi`, `PuanCuzdani`. Altitude and heading come from the aircraft transform. Math lives in static functions so EditMode tests need no scene.
- **Input.** Display-only gauges need no input. A clickable throttle would use the `UI` map with an `InputSystemUIInputModule` (U5).
- **Resolution.** Canvas Scaler Scale With Screen Size 1920x1080, match 0.5; frame anchored to the bottom stretch; dials anchored to the bottom corners.
- **Tests.** EditMode: needle and compass math at known values, missing references and scripts in the new scene, Build Settings, existing scenes byte-identical. PlayMode: values follow `UcusKontrol` in the scene, screenshots at three resolutions (examined by eye).
- **Minimum useful subset** (my recommendation, still needs U1): frame, two dials, throttle indicator, score/time text. Compass, markers, score table and menu only if the handout asks for them.

## 6. What I need from you (single list)
1. The Phase 4 handout text (or the list of required HUD elements): U1, U3, U5, U6, U7.
2. Scene decision (U2): new copy of Rota04 (recommended) or extend Rota04.
3. Dial ranges and altimeter meaning (U4), if you want something other than 0 to 50 m/s and 0 to 500 m above sea level.
4. Required resolutions (U10), if any.
5. Still open from before: a real-key flight check in the Rota03 exe, and the W-key pitch sign (D-029).
