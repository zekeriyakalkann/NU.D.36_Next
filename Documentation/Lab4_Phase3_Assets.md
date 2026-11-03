# Lab 4, Phase 3: ten UI assets

**Status (2026-10-20): IMPLEMENTED and measured; PARTIALLY VERIFIED.** The ten files are now original Blender work (section "Original Blender assets" below) and are imported in `NU.D.36_Unity/Assets/UI/`. Everything after that section is history: the sections written on 2026-10-19 (BLOCKED) and the review of the supplied internet pictures (2026-10-20, removed from the workspace) are kept for traceability only.

## Original Blender assets (2026-10-20)
Decision (D-049): the supplied internet pictures are dropped; the ten sprites are modelled and rendered in Blender, so the licence is ours (see `Belgeler/ASSET-LISANS.md`). No Phase 4 text exists, so no HUD integration was started.

### Sources and method
- Scripts: `Tools/Blender/ui/ui_common.py` (camera, lighting, materials, geometry helpers), `build_ui_assets.py` (nine sprites), `build_menu_background.py` (menu image, reads `Exports/NU_D_36.fbx`, which is only read). Typography: Blender's bundled Inter font (SIL OFL).
- Rebuild: `blender --background --factory-startup --python Tools/Blender/ui/build_ui_assets.py -- all Exports/Lab4_UI Blender/UI` and `... build_menu_background.py -- Exports/Lab4_UI/menu-arkaplan.png Blender/UI`. `build_ui_assets.py` takes a comma list instead of `all`.
- Method: every part is real geometry (bevelled prisms, rings, domed screws, text curves, a lacquered sphere), orthographic camera on a transparent film (Eevee, transparent PNG RGBA). The compass uses Cycles (Eevee showed a left-to-right brightness trend on large flat meshes; the actual cause was smooth shading on huge flat quads, fixed by flat-shading un-bevelled meshes; Cycles is kept for the compass).
- Blender sources: `Blender/UI/ui_<name>.blend`, one per asset, written with `bpy.data.libraries.write` (no `save_as_mainfile`; the aircraft `.blend` was not opened or touched). `verify_ui_blends.py` reopens them: all ten open, each with a camera (`Logs/lab4_ui_blend_verify.txt`). The menu `.blend` has the aircraft removed before writing; the FBX remains its source.
- Render copies: `Exports/Lab4_UI/`. Unity copies: `NU.D.36_Unity/Assets/UI/` (import settings applied by `Lab4UiTexturePostprocessor`).
- Style: brass and aged steel, olive-painted steel, dark leather with cream stitching, a wood-grain backing, domed rivets and slotted screws, cream numerals in one typeface.

### Measured result (`Verify-Lab4Assets.ps1`, `Logs/lab4_ui_verify.txt`, plus Unity `Validate UI Assets`, `Logs/lab4_ui_validation.txt`)
| File | Size | Alpha | Unity import | Use |
|---|---|---|---|---|
| hud-cerceve.png | 1920x1080 | real, 69 % transparent | Sprite, Clamp | UI Image, stretch. Bottom band, two corner pods with dark sockets for the dial sprites, centre leather inlay (text "NU.D.36" baked in) |
| gosterge-kadranlari.png | 1024x512 | real | Sprite, Clamp | two dial faces, no needle: left airspeed (HIZ, M/S), right altimeter (IRTIFA, x100 M). Cut into two 512x512 halves, e.g. two RectTransforms with a sprite rect or two Images using `uvRect` on RawImage |
| gosterge-ibre.png | 512x512 | real | Sprite, Clamp | needle pointing up, hub exactly at the centre (256,256); farthest pixel 236.5 px from the centre, so any rotation stays inside the canvas |
| gaz-kolu-plakasi.png | 256x1024 | real | Sprite, Clamp | empty throttle track (centre x 100); 0 % at y 912, 100 % at y 112 (plate pixels), ticks and labels 0/50/100, "GAZ" |
| gaz-kolu-topuz.png | 512x512 | real | Sprite, Clamp | knob (red lacquered ball on a brass carriage), centre = slot position, moved separately |
| pusula-seridi.png | 1536x128 | opaque | Sprite, **Repeat** | ribbon; North at the left edge: heading h is at u = h/360 |
| hedef-isaretleri.png | 1024x1024 | real | Sprite, Clamp | four 512x512 cells: aiming reticle, locked target brackets (red), objective diamond, base marker |
| dugme-seti.png | 768x1024 | real | Sprite, Clamp | four 768x256 rows (top to bottom): normal, hover, pressed, disabled; no label text |
| puan-tablosu.png | 1024x1280 | real | Sprite, Clamp | brass frame, empty header plate, 10 empty rows, column lines at x 172/572/772 (rank + 3 columns) |
| menu-arkaplan.png | 1920x1080 | opaque | Sprite, Clamp | golden-hour apron with the NU.D.36 aircraft in the right two thirds; soft dark vignette on the left third for buttons |

All ten match `Lab4UiAssets.Specs` (names, size, alpha, import); `Validate UI Assets`: 0 problems.

### Dial and compass mapping (the design fixes these; Phase 4 code must use them)
- Dial sweep: value 0 at -135 degrees, maximum at +135 degrees, clockwise from 12 o'clock. Speed 0 to 50 m/s (major ticks every 10, red zone from 42 m/s, the Lab 3 maximum). Altimeter 0 to 500 m (major every 100, minor every 20).
- Needle: rotate about the sprite centre; Unity z rotation = -(value/max * 270 - 135) degrees.
- Compass: seamless horizontally, 360 degrees across the width, labels every 15 degrees (N NE E SE S SW W NW as letters), no pointer in the texture. Unity moves only `uvRect.x`; a fixed pointer must be a separate UI element. Seam check: tiled preview shows continuous ticks and a whole "N"; wrap-seam colour difference 6.1 against 7.3 between ordinary neighbouring columns.
- Whether the compass has to move is still not confirmed by the handout (Phase 4 text missing); the asset supports both a static and a scrolling use.

### Visual verification (done by looking at the renders)
Looked at: all ten at full size or on a contact sheet over magenta (proves the transparent background), the compass wrap seam, the dials, the HUD frame, the score table, the menu image. Findings: clean edges, true transparency, consistent materials and typography, readable numerals. Weak points: the menu image is plain (a grey apron and a dark coast band, the aircraft model is white and simple); the HUD frame is flat in style; brass wear is subtle. Not looked at: the assets inside a running HUD, because Phase 4 does not exist.

### Review round 2 (2026-10-20): real game-view check and two corrections
- **Method:** the editor-only tool `Assets/Scripts/Editor/Lab4UiPreview.cs` (menu **NU36 > Lab 4 > Preview UI On Rota04**; batch `-executeMethod NU36.Editor.Lab4UiPreview.Run`, needs graphics) opens Rota04 without saving, puts the sprites in front of the Main Camera as SpriteRenderers (rule: fit the 1920x1080 reference, anchored bottom-centre) and renders PNGs to `Logs/lab4_ui_review/` (`hud_*`, `menu_*`, `scoretable_*`). It is a visual check, **not the Phase 4 HUD**: no Canvas, no uGUI, no scene or script changed. Rota04.unity MD5 identical before and after (`unity_preview_report.txt`). Sample values and positions in it are preview guesses.
- **Looked at:** HUD at 1280x720, 1920x1080, 2560x1080, 1024x768; score table over the game view; menu at 1920x1080, 1280x720, 2560x1080; edge crops over white and black (`halo_crops.png`: no dark or white halos).
- **Found and fixed 1:** the needle tail covered the "HIZ" caption in the game view. The needle tail is now shorter (hub to tail 44 px instead of 66) and the dial captions moved down (HIZ/IRTIFA at +96 px, units at +130 px from the dial centre). Re-rendered `gosterge-ibre.png` and `gosterge-kadranlari.png`; checked again in the preview: captions clear for both dials.
- **Found and fixed 2:** the menu image's left third was mid-grey (mean luma 0.53), too bright for button text. The vignette now uses the camera window coordinate with a smooth 0.97 to 0 falloff over the left half, the apron is darker and the exposure lower: left third mean luma 0.36. The right two thirds are unchanged. Buttons are readable on it in the preview (labels were not drawn; the preview has no text).
- **Backups before overwriting:** `Exports/Lab4_UI_backup_2026-10-20_menu/` and `..._dials/` hold the previous PNGs, `.blend` files, the previous scripts (`*.py.bak`) and a `RESTORE.txt` each. Not restored from the Recycle Bin: nothing was.
- **Finding for Phase 4 (aspect ratio):** with the fit rule the frame keeps its proportions, so at 21:9 (2560x1080) there are empty strips left and right (the game view shows there, the frame does not reach the screen edge) and at 4:3 the frame fills the width but is smaller. Stretching the frame would misalign the dials from their sockets, so the dials must be children of the frame. Whether side strips are acceptable is a design decision for you.
- **Menu check:** 1280x720 and 2560x1080 render without cropping problems (the background covers, buttons stay in the left third).

### Sprite pivots and movable parts (for Phase 4)
| Sprite | Pivot / centre | Use |
|---|---|---|
| gosterge-ibre | sprite centre (0.5, 0.5) = hub; default Unity pivot Center, no change needed | rotate about the centre; z = -(value/max*270 - 135) degrees |
| gosterge-kadranlari | two 512x512 halves: rect (0,0,512,512) speed, (512,0,512,512) altimeter; centres at (256,256) and (768,256) | static; dial centre = needle centre |
| gaz-kolu-topuz | ball centre (256,256) | moves vertically only, on the plate's slot centre x=100 (plate pixels); y 912 = 0 %, y 112 = 100 %; knob scale = plate scale x 0.29 (preview: plate 0.5, knob 0.145) |
| gaz-kolu-plakasi | centre (128,512) | static |
| pusula-seridi | texture, Repeat | window = a fraction of the ribbon; `uvRect.x = heading/360 - window/2` centres a heading; pointer is a separate element (preview: window 0.5) |
| hedef-isaretleri | four 512x512 cells: (0,512) reticle, (512,512) locked brackets, (0,0) objective diamond, (512,0) base marker (Unity rect origin bottom-left) | each cell centre is the marker centre |
| dugme-seti | four 768x256 rows, rect y = 768 normal, 512 hover, 256 pressed, 0 disabled | sprite swap |
| hud-cerceve | frame centre; dial sockets at (200,830) and (1720,830) in the 1920x1080 reference | dials scaled 0.58 fit the sockets |
Slicing into sub-sprites is done at runtime with `Sprite.Create` on the imported texture (as the preview does); the import rule stays Single.

### Inferred design choices (not from the handout)
Dial ranges and captions, the marker set (four markers), button states (four, from the Specs summary), score table columns (rank + 3), the Turkish captions (HIZ, IRTIFA, GAZ), the frame layout with two dial sockets. All can be changed in the scripts.

### Not done
No HUD scene, prefabs or runtime scripts (Phase 4 text unavailable). No Unity tests were run in this step (textures do not touch code; the Unity batch run that validated the import compiled the project and exited 0). Lab 2, Lab 3 and Rota scenes are untouched.

---
## History
The sections below were written on 2026-10-19 and 2026-10-20 before the decision to make original assets. The pictures they discuss were moved to the Recycle Bin; `Exports/Lab4_Phase3_Working/` and `Tools/Lab4Assets/Prepare-Lab4Assets.ps1` were removed with them.

Date: 2026-10-19. **Status then: BLOCKED for generation; preparation done.** No asset existed yet.

## Why blocked
1. This session has no image-generation tool or model (checked the available tools; none produces images). Hand-drawing the art in code would not be the generated art the handout asks for, so it was not attempted and no placeholder files were created.
2. The handout's ten full prompts were not supplied in this conversation (only the short summaries in the task). The prompts must not be paraphrased, so they could not be used or recorded.

## What was prepared
- `Assets/Scripts/Editor/Lab4UiAssets.cs`:
  - `Lab4UiAssets.Specs`: the ten required file names, canvas sizes, transparency intent (alpha required for 01, 03, 05, 07, 09; opaque for 10) and Repeat for 06.
  - `Lab4UiTexturePostprocessor`: applies the import settings to those ten files in `Assets/UI/` when they are imported: Sprite (2D and UI), Single, no compression, no mip maps, Clamp (Repeat for `pusula-seridi.png`), Alpha Is Transparency on, no NPOT scaling. Nothing else is touched.
  - **NU36 → Lab 4 → Validate UI Assets** (batch: `-executeMethod NU36.Editor.Lab4UiAssets.Validate`): for each file reports presence, readability, real size against the expected size, minimum alpha and the importer settings, into `Logs/lab4_ui_validation.txt`. It changes nothing.
- `Assets/Tests/EditMode/Lab4UiAssetSpecTests.cs` (3 tests): names and sizes, the Repeat and opaque exceptions, and that the import rule matches only the ten files in `Assets/UI/`.
- `Belgeler/ASSET-LISANS.md`: provenance table with every field present and empty ("üretilmedi"); nothing invented. (The path is the one the task names; the other documents live in `Documentation/`.)
- No existing gameplay script, scene or Rota02/Rota03/SampleScene was modified.

## Asset list and intended use
| # | File | Canvas | Use in Phase 4 |
|---|---|---|---|
| 01 | hud-cerceve.png | 1920×1080, transparent | HUD frame, bottom and lower corners |
| 02 | gosterge-kadranlari.png | 1024×512 | two dial faces (airspeed, altimeter), no needle |
| 03 | gosterge-ibre.png | 512×512, transparent | needle, rotated about the centre pivot |
| 04 | gaz-kolu-plakasi.png | 256×1024 | throttle plate, empty track |
| 05 | gaz-kolu-topuz.png | 512×512, transparent | throttle knob, moved separately |
| 06 | pusula-seridi.png | 1536×128, tileable | compass ribbon, Repeat wrapping |
| 07 | hedef-isaretleri.png | 1024×1024, transparent | target atlas |
| 08 | dugme-seti.png | 768×1024 | button states |
| 09 | puan-tablosu.png | 1024×1280, transparent | score table |
| 10 | menu-arkaplan.png | 1920×1080, opaque | menu background, quiet left third |

## Quality control
Nothing to inspect: all ten files are missing. The visual checks listed in the task (no needle on the dials, centred pivot, empty throttle track, separate knob, seamless ribbon, separated atlas elements, four button states, empty table cells, opaque menu image with a quiet left third) are **NOT TESTED**.

## To finish this phase
1. Supply the ten full prompts and an image-generation route (the tool name and model version are needed for the licence table), or supply the generated PNGs.
2. Put the files, with the exact names, in `NU.D.36_Unity/Assets/UI/`. The import settings apply automatically.
3. Run **Validate UI Assets**, look at every image, and fill `Belgeler/ASSET-LISANS.md`. Image models often miss exact sizes and real alpha, so sizes, alpha and the ribbon seam must be checked on the files.

## Remaining for Phase 4
HUD integration of all ten assets (needle rotation, throttle knob position, compass scrolling, target markers, buttons, score table, menu background). It cannot start until the assets exist.

---

## Review of the supplied pictures (2026-10-20)
Source: `NU_D_36_Pictures/` (10 PNG files). Originals untouched, nothing copied into Unity. Status: **REVIEWED, NOT IMPORTED.** Pixel format and alpha were measured with System.Drawing on every file, and all ten images were viewed.

### Requirements
The only Phase 3 requirements in the workspace are the ten names, sizes, alpha intent and import rules in `Lab4UiAssets.Specs`, taken from the task summary in the conversation. The handout's full prompts are still missing, so the exact content requirements are known only from that short summary. Phase 4 (HUD integration) is not started.

### Technical state
| Picture | Size | Format | Real alpha | Checkerboard in pixels | Likely slot | Fit |
|---|---|---|---|---|---|---|
| Cockpit HUD Frame | 1672x941 | RGBA | yes (78 % transparent) | no | hud-cerceve | Content fits (bottom frame, two empty windows). Size differs from 1920x1080; scale. |
| Aircraft Dials | 1774x887 | RGBA | yes (33 % transparent) | no | gosterge-kadranlari | Fits: two dials, no needle, 2:1 like the spec. Scale to 1024x512. |
| Gauge Needle | 1254x1254 | RGB | **no** | **yes** | gosterge-ibre | Shape right; background not transparent. Hub is near the bottom, not the centre. |
| Control Panel | 1024x1536 | RGBA | yes (vignette, 41 % partial) | no | gaz-kolu-plakasi | Empty track plus a second coloured scale. Aspect 2:3, spec 1:4: needs a crop. Mapping is a guess. |
| Throttle Lever | 1536x1024 | RGB | **no** | **yes** | gaz-kolu-topuz | Single red knob, diagonal. Needs background removal, crop to 512x512. |
| Compass Strip | 2172x724 | RGB | **no** | **yes** (plus faint ghost shapes) | pusula-seridi | **Not usable as a scrolling ribbon** (see below). |
| Icon Set | 1536x1024 | RGB | **no** | **yes** | hedef-isaretleri | Reticles, arrow, camera, warning, signal, chevrons and a MISSION list panel. Usable as a source atlas after background removal. |
| UI Control Sprite Sheet | 1536x1024 | RGB | **no** | **yes** | dugme-seti | Text buttons in three states, icon buttons in four, lamps, switches. Spec is portrait 768x1024 with four states. |
| Scoreboard Panel | 1536x1024 | RGB | **no** | **yes** | puan-tablosu | Content fits (empty cells). Spec expects transparency and portrait 1024x1280; this is opaque landscape. |
| Seaside Airfield | 1672x941 | RGB | none (opaque, as wanted) | no | menu-arkaplan | Opaque 16:9. The spec asks for a quiet left third; the left third here is busy (hangar). Usable by scaling or cropping. |

Checkerboard test: files without an alpha channel have a corner patch of 12-32 colours, mean brightness 232-241 and chroma 3 or less (grey and white). The files with real alpha have a single transparent colour there. Viewing confirmed the squares in Needle, Compass, Throttle, Icon Set, Scoreboard and Sprite Sheet.

### Compass strip problems
- 3:1 (2172x724), not the 12:1 ribbon (1536x128) of the spec.
- Not seamless: it has end caps with screws and a fixed triangular pointer in the middle.
- The labels are inconsistent: W appears twice, the step after 240 is 255 and then W, and NW sits between 300 and 330 (it should be 315). Ticks and numbers do not match.
- It can be used only as a static decorative frame, unless it is regenerated.

### Mapping status
Probable, not confirmed. Every picture's content matches one slot, but none matches the sizes in `Specs` and the full prompts are missing. Strongest: Dials, HUD Frame, Scoreboard, Seaside Airfield. Weakest: Compass and Control Panel.

### Name check
The task wording used `gosterge-ibresi.png`; code and `Specs` use `gosterge-ibre.png`. The code name stays until the user says otherwise.

### Proposed Unity integration (not executed)
Common: `Assets/UI/`, Sprite (2D and UI), Single, Uncompressed, no mip maps, Clamp, Alpha Is Transparency on, NPOT None. `Lab4UiTexturePostprocessor` applies this to the ten target names. Work on copies; originals stay in `NU_D_36_Pictures/`.

| Target | Source | Preparation | UI component | Acceptance |
|---|---|---|---|---|
| hud-cerceve.png | HUD Frame | scale to 1920x1080 | Image, stretch anchors | alpha min 0, centre transparent |
| gosterge-kadranlari.png | Dials | scale to 1024x512 | 2 Images | no needle |
| gosterge-ibre.png | Needle | remove checkerboard, set RectTransform pivot to the hub | rotated Image | alpha min 0, turns about the hub |
| gaz-kolu-plakasi.png | Control Panel | crop to the track | Image | knob stays in the track |
| gaz-kolu-topuz.png | Throttle Lever | remove checkerboard, crop 512x512 | Image moved by `UcusKontrol.GazOrani` | alpha min 0 |
| pusula-seridi.png | none usable | regenerate: seamless, 12:1, consistent labels | RawImage with scrolling uvRect, Repeat | seamless seam, N at 0, E at 90 |
| hedef-isaretleri.png | Icon Set | remove checkerboard, slice | Images | markers separate |
| dugme-seti.png | Sprite Sheet | remove checkerboard, slice | Button sprite swap | four states per button |
| puan-tablosu.png | Scoreboard | remove checkerboard, portrait vs landscape decision | Image and Text | empty cells, readable text |
| menu-arkaplan.png | Seaside Airfield | scale to 1920x1080 | Image behind the menu | opaque, buttons readable |

Background removal by colour key or flood fill would damage highlights and soft edges, so regeneration with real alpha is cleaner. No picture was modified.

### Licence
`Belgeler/ASSET-LISANS.md` still has no source, tool, model, date or prompt for any file. The file names look like exported generator titles, but that is not documented, so no tool or licence is attributed. The images are **not cleared for distribution or submission** until the source and terms are recorded.

### Open decisions for the user
1. Confirm that these ten pictures are the Phase 3 assets and the mapping above.
2. Source, tool, model version and terms for each picture.
3. Regenerate with real transparency and correct sizes (and a seamless compass), or allow scripted background removal and cropping on working copies.
4. Whether the compass must scroll (the Phase 4 text is not available) or may be static.
5. Send the handout's ten prompts if the exact acceptance wording is needed.

---

## Working copies (2026-10-20)
Tools: `Tools/Lab4Assets/Prepare-Lab4Assets.ps1` (builds the copies) and `Verify-Lab4Assets.ps1` (checks them against `Lab4UiAssets.Specs`; log `Logs/lab4_phase3_working_verify.txt`). Output: `Exports/Lab4_Phase3_Working/`, with magenta-backed previews in `_onizleme/`. The originals were not touched. **Nothing is in `Assets/UI/`**, because source and licence are UNKNOWN (D-048).

| Working copy | From | What was done | Specs check |
|---|---|---|---|
| hud-cerceve.png | Cockpit HUD Frame | bicubic scale 1672x941 to 1920x1080 (aspect error 0.05 %) | 1920x1080, alpha min 0: MATCHES |
| gosterge-kadranlari.png | Aircraft Dials | scale 1774x887 to 1024x512 (exact 2:1) | MATCHES |
| menu-arkaplan.png | Seaside Airfield | scale 1672x941 to 1920x1080, saved opaque | size and opaque MATCH. The "quiet left third" is **not** met (hangar on the left) |
| gosterge-ibre.png | Gauge Needle | checkerboard removed; transparent margin added so the hub centre (625, 1028 in the source) sits at (256, 256); scaled 0.265 | 512x512, alpha min 0: MATCHES. The needle points up (0 at 12 o'clock) |
| gaz-kolu-topuz.png | Throttle Lever | checkerboard removed, cropped to the content (1297x922), centred with a 16 px margin, not rotated | 512x512, alpha min 0: MATCHES |
| hedef-isaretleri_kaynak-1536x1024.png | Icon Set | checkerboard removed, native size | atlas for slicing; **not** the 1024x1024 of Specs |
| dugme-seti_kaynak-1536x1024.png | UI Sprite Sheet | checkerboard removed, native size | **not** the 768x1024 of Specs; layout differs |
| puan-tablosu_kaynak-1536x1024.png | Scoreboard | checkerboard removed, native size | **not** the 1024x1280 portrait of Specs |

Checkerboard removal: neutral light pixels (chroma up to 8, brightness 200 or more) in connected regions that touch the border or hold 400+ pixels (this includes holes inside rings), then a 1 px defringe and a half-alpha edge ring. Dark artwork outlines stop it. I looked at the magenta previews of all five: edges are clean, reticle and button interiors are transparent, no square pattern is visible. A few faint specks remain near the OPTIONS/EXIT row of the sprite sheet. The residual counts in the log (up to 3 497 on the sprite sheet) are neutral artwork (silver buttons, highlights), not pattern. Edge quality was judged by eye on previews, not measured.

**Not produced:** `pusula-seridi.png` (no usable source) and `gaz-kolu-plakasi.png` (the Control Panel needs a crop and the choice of track and scale is a design decision; needed size is 256x1024 and the panel body is about 1:3.3).

### Compass: static or scrolling
No text in the workspace says the compass must scroll. The Specs say only "compass ribbon, Repeat wrapping". Repeat wrapping suggests scrolling, but the handout does not confirm it.
- **Static decorative compass:** any picture works, but its labels must still be right. The supplied strip has wrong labels, so it is not suitable even for display.
- **Scrolling ribbon needs:** 1536x128 (12:1); seamless horizontally (the right edge continues into the left); no end caps and no pointer inside the image (the pointer is a separate UI element); 360 degrees across the width in order N 0, NE 45, E 90, SE 135, S 180, SW 225, W 270, NW 315, back to N; equal tick spacing; Wrap Mode Repeat (the postprocessor sets it).
- **Acceptance:** shifting the UV by 1.0 gives an identical image; no visible seam; labels consistent.
Regeneration is cleaner than editing, because the text would have to be redrawn.

### Production instructions for missing or substitute images
- `pusula-seridi.png`: as above.
- `gaz-kolu-plakasi.png`: 256x1024, empty vertical track without a knob. The supplied Control Panel can be cropped to its left slot and frame if the user accepts the second coloured scale; otherwise regenerate.
- `hedef-isaretleri.png`: 1024x1024 transparent atlas with clear space between the markers. Packing the cleaned Icon Set into the square is a slicing step I did not do, because the atlas layout is not specified.
- `dugme-seti.png`: 768x1024, four states per button. The supplied sheet has three states for the text buttons.
- `puan-tablosu.png`: 1024x1280 portrait with transparent corners and empty cells. The supplied scoreboard is landscape.

### Unity import readiness
| Target | Ready? |
|---|---|
| hud-cerceve, gosterge-kadranlari, gosterge-ibre, gaz-kolu-topuz, menu-arkaplan | Technically ready: size, alpha and import rule match `Specs`. **Blocked** by unknown licence and by your confirmation of the mapping. menu-arkaplan also misses the quiet-left-third wish |
| hedef-isaretleri, dugme-seti, puan-tablosu | Not ready: size and layout differ |
| gaz-kolu-plakasi, pusula-seridi | No usable working copy |

When cleared: copy the ready files to `NU.D.36_Unity/Assets/UI/` and run **NU36 > Lab 4 > Validate UI Assets**. The postprocessor sets Sprite (2D and UI), Single, Uncompressed, no mip maps, Clamp (Repeat for the compass), Alpha Is Transparency and NPOT None.

### Lab 3 regression
No tests were run in this step and no code changed. Recorded results (2026-10-19, after Lab 4 phase 2): EditMode 113/113 (`Logs/TestResults_EditMode.xml`), PlayMode 67/67 (`Logs/Final_Phase2_PlayMode.xml`). Real-key flight is still NOT VERIFIED. `Builds/Windows_Rota03/NU.D.36.exe` exists (2026-10-19 16:21). Manual procedure: start the exe (it opens Rota03); hold W/S (pitch), A/D (roll), Q/E (yaw) and Up/Down (throttle; speed 8 to 42 m/s, field of view 55 to 72). Check that the camera follows and the aircraft answers every key. Report whether W pitches the nose up or down (D-029).
