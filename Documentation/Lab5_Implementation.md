# Lab 5 (Hafta 5): Bölüm 2 — Sınır Hattı (scene Rota05)

Date: 2026-11-03. Source: the worksheet text "Laboratuvar Föyü: Bölüm 2 — Sınır Hattı", pasted into the conversation (the only source; no images, no GDD file). Status values: IMPLEMENTED, VERIFIED, PARTIALLY VERIFIED, NOT TESTED, BLOCKED.

## 1. Inputs that were not available
| Item named by the worksheet | State | What was done |
|---|---|---|
| Supplied `Patlama.cs`, `MuhimmatMevzii.cs`, `SeviyeYoneticisi.cs`, `DalisGostergesi.cs` | **Not in the workspace** | Minimal stand-ins written from the call shapes in the worksheet (`Patlama.Yarat(point, radius, damage, layers)`, `SeviyeYoneticisi.UsseVarildi()` code kept verbatim, `yuk.Kalan/Kapasite/Ikmal()`). Replace them with the course files when available; the scene wiring may then need adjusting |
| Hurricane Mk IIB model | **Not in the workspace: BLOCKED** | The NU.D.36 model flies with Hurricane-scale flight values (§4). No Hurricane model was made or imported |
| Week 2 bonus `TopcuAtes.cs`, `Mermi.cs` | Never built in this project (D-018: bonus skipped) | `UcaksavarMevzii` (gun) and `Mermi` (round) written new for this lab |
| GDD §6.4, §9 | Not in the workspace | Only the values quoted in the worksheet are used |
| Unity version | Worksheet says Unity 6.3 LTS; project is 6000.6.4f1 | Unchanged (CLAUDE.md: no version change) |

Discrepancy (reported, not changed): the worksheet's week 4 economy is 3 × 100 + 2 × 25 + 40 + 30 = 420 with the Hurricane threshold at 400. Rota04 uses the earlier INFERRED defaults (threshold 300, D-042). Rota04 is a finished lab scene and was left byte-identical; tell me if it should be aligned.

## 2. Requirements and status
CONFIRMED = stated in the worksheet. INFERRED = implementation choice (rationale in DecisionLog D-055 … D-066).

| ID | Requirement (worksheet) | Implementation | Test | Status |
|---|---|---|---|---|
| REQ-501 | Rota05 = Save As of the week 4 scene | `Lab5SceneBuilder.Create` copies **Rota04Hud** (the latest Lab 4 state: HUD + flight model) to Rota05, appended to Build Settings; earlier scenes byte-identical | `EarlierScenes_AreByteIdentical` ×5, `Rota05_IsLastInBuildSettings` | VERIFIED |
| REQ-502 | Radar: line of sight (`engelKatmani`, `gozYuksekligi` 3 m, `EngelVar`, raycast after the cone, `QueryTriggerInteraction.Ignore`) | `Radar.cs`; empty mask = old behaviour (Lab 2–4 unchanged) | `Radar_BehindTepe3_IsBlockedYellow_InTheOpen_Sees`, `GorusEngelliMi_EmptyMaskNeverBlocks`, all Lab 2 radar tests | VERIFIED |
| REQ-503 | Gizmo green / yellow / red | `Radar.OnDrawGizmos`, `GizmoRengi(goruyor, engelVar)` | `GizmoRengi_ThreeStates` (colour logic); Scene-view look **not** captured | PARTIALLY VERIFIED |
| REQ-504 | Layers Ucak, Arazi, Yapi, Hedef, Mermi; objects on their layers (children too) | User layers 8–12; aircraft, terrain, hills, guns, locator, base, dumps, prefabs | `Layers_FiveWorksheetLayersExist`, scene tests | VERIFIED |
| REQ-505 | Mesh Collider on the Çakıl Burnu terrain (layer Arazi) | Root renamed "Çakıl Burnu Arazisi"; `SonsuzArazi.carpisici` gives every chunk a Mesh Collider (off in Rota03/04) | `Rota05_StartsWithoutErrors…` (every active chunk cooked) | VERIFIED |
| REQ-506 | Radar mask = Arazi + Yapi, not the aircraft | Set on the three guns | `Enemy_…RadarMaskIsAraziYapiWithoutUcak` | VERIFIED |
| REQ-507 | Collision matrix: Mermi×Mermi, Hedef×Arazi off | `Physics.IgnoreLayerCollision` in `KatmanlariKur` | `CollisionMatrix_…` | VERIFIED |
| REQ-508 | Tepe 3 is the highest, 230 m; flying into a slope low crashes | Hills Tepe 1/2/3 (120/170/230 m above the ground) with Mesh Colliders; `ZeminBaglayici` makes the ground probe see them | scene test, LOS test; crash on a hill slope **not** run as a test | PARTIALLY VERIFIED |
| REQ-509 | Sound locator: history, `sesHizi` 343, silent below `sessizlikEsigi`, out of `duymaMenzili`, `Gecikme`, `DuyulanKonum`, `Sapma`; blue/red gizmo | `SesDinleyici.cs` (+ white gap line) | `SesDinleyici_PassAt600m_SapmaIsAbout149m` (148.7 m), `…ThrottleCut_NotHeard…`, in-scene test, `AcisalHata_500kmh…` (22.0°) | VERIFIED (gizmo look not captured) |
| REQ-510 | Gun aim source: eye → true position, else ear → heard position, else nothing | `UcaksavarMevzii` | `Ucaksavar_FiresOnlyWithData_NotWhenSilentAndHidden` | VERIFIED |
| REQ-511 | `mermiHizi` 70 → 260; step = v·fixedDeltaTime = 5.2 m > 4.8 m (body sphere 2.4 m) | Guns at 260; aircraft `SphereCollider` r 2.4 | `MermiAdimi_At260…`, scene tests | VERIFIED |
| REQ-512 | Tunnelling seen, then fixed by the sweep in FixedUpdate | `Mermi.supurme` (true = worksheet raycast sweep; false = end-point check kept for the experiment; also on each gun) | `Mermi_At260_TunnelsWithoutSweep_HitsWithSweep` | VERIFIED |
| REQ-513 | Altitude band: < 15 m scatter, > 460 m scatter × 0.7, between 0 | `UcaksavarMevzii.Sacilma` | `Sacilma_DeathBand15To460` ×6 | VERIFIED |
| REQ-514 | Bomb: semi-implicit Euler + ray sweep, inherits aircraft velocity, `Patlama.Yarat` on impact | `Bomba.cs` | `Bomba_ReleasedInTheWindow_DestroysTheDump` | VERIFIED |
| REQ-515 | Formulas and table (Δh 25/54/92/121/184 m; range 198/128/77/49/0 m) | `Bomba.ToparlanmaKaybi`, `IleriMenzil`, `DususSuresi` | `ToparlanmaKaybi_…` ×5, `IleriMenzil_…` ×5 (closed form and integrator within 1 m) | VERIFIED |
| REQ-516 | Window 45–70°, 120–260 m; HUD shows only dive angle and AGL; frame green inside | `BombaYuku.PencereIcindeMi`, `DalisGostergesi` (top right) | `BirakmaPenceresi_…` ×6, `DalisGostergesi_InTheWindow_FrameGreen_RingOnThePredictedImpact` | VERIFIED |
| REQ-517 | Aiming ring from `Bomba.DususNoktasi` (integration run forward) | Ring drawn at the predicted point | same test (prediction within 20 m of the dump) | VERIFIED |
| REQ-518 | Release below 120 m: read the Console | Warning + 40 damage from own fragments | `Bomba_ReleasedBelow120m_FragmentsHitTheAircraft` | VERIFIED |
| REQ-519 | `BombaYuku` on the aircraft, `bombaOn` = Bomba prefab; 2 × 250 lb | Space releases; capacity 2 | scene test | VERIFIED (key press itself not tested) |
| REQ-520 | 4 ammunition dumps; a dump can be disabled | `MuhimmatMevzii` (health 100, bomb 100 within 25 m) | bomb test, `AllDumpsDestroyed_Succeeds…` | VERIFIED |
| REQ-521 | 180 s; resupply +2 bombs and +90 s only when not full; `UsseVarildi` as in the worksheet | `SeviyeYoneticisi`, base trigger `IkmalNoktasi` | `Ikmal_OnlyWhenNotFull…`, `Ikmal_ThroughTheBaseTrigger`, `Time_RunsOut…` | VERIFIED |
| REQ-522 | Restart | End screen button and R key reload Rota05 | `Ozet_ListsTheFourNumbers`; reload itself not run | PARTIALLY VERIFIED |
| REQ-523 | Enemy: sound locator + 3 guns | Scene | scene tests | VERIFIED |
| REQ-524 | Step 05: check LOS 5 times a second | `Radar.denetimAraligi` (0 = every frame, 0.2 = 5 Hz) | `Olcum_GorusIsini_3ve8Mevzi_HerKareVe5Hz` | VERIFIED (mechanism); the Profiler reading is a user task |
| REQ-525 | Hurricane Mk IIB model | — | — | BLOCKED (file missing) |
| — | Bonus tasks | Not implemented (optional) | — | — |

## 3. Layout (Rota05, metres; flat plain east of the base, `Logs/lab5/arazi_haritasi.txt`)
Base (resupply trigger 120 × 160 × 120 m) at the origin; aircraft starts at (0, 150, 0), heading 57°. Dumps: (1000, 650), (1800, 1000), (2200, −100), (1400, −600). Guns: (1650, 1150) by dump 2, (2050, 50) by dump 3, (1150, 800) by dump 1; dump 4 has no gun of its own. Sound locator (1700, −150). Hills: Tepe 1 (2550, 650) 120 m, Tepe 2 (850, −450) 170 m, Tepe 3 (1600, 350) 230 m, between dump 1 and gun 2, so a low approach from the west is masked from gun 2.

## 4. Values (INFERRED unless the worksheet gives them)
| Value | Used | Source |
|---|---|---|
| Speed of sound, round speed, body radius, LOS eye height | 343 m/s, 260 m/s, 2.4 m, 3 m | worksheet |
| Band 15 / 460 m, window 45–70° / 120–260 m, 180 s, +90 s, 2 bombs, 4 dumps, 3 guns | as given | worksheet |
| Aircraft speed range / pitch rate | 30–90 m/s (start 66), 30°/s (≈ 4.5 g at 85 m/s; worksheet table uses 4 g) | INFERRED (worksheet: dive ≈ 85 m/s) |
| Gun: cone half-angle, sight range, fire range, rate, damage, out-of-band scatter | 120°, 1500 m, 1000 m, 0.3 s, 8, 6° | INFERRED |
| Listener: hearing range, silence threshold | 3000 m, throttle 0.15 | INFERRED |
| Bomb: blast radius, damage; fragment damage below 120 m | 25 m, 100; 40 | INFERRED |
| Score | 100 per dump + remaining whole seconds on success | INFERRED (worksheet asks for a total, no formula) |

## 5. Your tasks from the worksheet (cannot be done for you)
1. **Step 01–02 look:** Play Rota05, select `Ucaksavar 2` / `SesDinleyici` and watch the Gizmo lines (Gizmos on in the Game/Scene view). Fly behind Tepe 3 → yellow.
2. **Step 02:** pass about 600 m from `SesDinleyici` and read `Sapma` in its Inspector (section "Gözlem"). Expected ≈ v × 600 / 343 for your speed v (85 m/s → 149 m; the test measures 148.7 m). Cut the throttle: `Duyuyor` turns off.
3. **Step 03:** set `Supurme` off on the three guns (or on the `Mermi` prefab) to watch rounds pass through, then on again. Write `adım = 260 × 0,02 = 5,2 m` in your notebook.
4. **Step 04:** dive at a dump at 30°, 50°, 80° and note where the bombs land; release below 120 m and read the Console.
5. **Step 05:** play a full level and write the four numbers shown on the end screen; set `SeviyeYoneticisi.baslangicSuresi` 180 → 150; Profiler (Window → Analysis → Profiler, Physics) with 3 and 8 guns (duplicate `Ucaksavar 1` in Play Mode), then `Radar.denetimAraligi` = 0.2. Reference numbers from the automated measurement: `Logs/lab5/olcum_gorus.txt` (8 guns ≈ 0.012 ms per frame for all LOS rays; 5 Hz = 40 rays/s for 8 guns versus 8 per frame).

Controls: W/S pitch, A/D roll, Q/E yaw, ↑/↓ throttle (Lab 3), **Space bomb**, **R restart** after the end.

## 6. Known limitations
- The airspeed dial face is printed 0–50 m/s (Lab 4 art); the Hurricane flies 30–90 m/s, so the needle stops at 50. A new dial face is a UI asset decision.
- No HUD markers for the dumps (not requested); red 40 m flag poles mark them. Fog (600–1400 m) limits how far they can be seen.
- The guns aim without lead (lead is a bonus task).
- End screen is OnGUI, like Lab 4.
- Not tested: a hand-flown full level, balance (whether 180 s and the gun values feel right), the Space/R keys with a real keyboard, the Gizmo look in the Editor, hill-slope crash.
