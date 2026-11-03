# Lab 4, Phase 1: core mission loop (Rota04)

Date: 2026-10-19. Source: the Phase 1 task text supplied in the conversation (the handout itself was not available as a file). Values the task text does not give are marked INFERRED. Phases 2–4 (HUD artwork, timer, crash/failure, generated images) are not started.

## 1. Files
New:
- Runtime (`Assets/Scripts/Runtime/`, namespace `NU36`): `KesifHedefi.cs`, `PuanCuzdani.cs`, `GorevYoneticisi.cs`, `UsGirisi.cs`, `GorevSonuEkrani.cs`
- Editor: `Assets/Scripts/Editor/Lab4SceneBuilder.cs` (menu **NU36 → Lab 4 → Create Rota04 Scene**, batch `-executeMethod NU36.Editor.Lab4SceneBuilder.CreateRota04`; refuses to overwrite an existing Rota04)
- Assets: `Assets/Scenes/Rota04.unity`, `Assets/Lab4/Prefabs/KesifHedefi.prefab`, `Assets/Lab4/Materials/Hedef.mat`, `Us.mat`
- Tests: `Assets/Tests/EditMode/Lab4SceneTests.cs`, `Assets/Tests/PlayMode/Lab4GorevTests.cs`

Modified (necessary): `Assets/Tests/EditMode/Lab3SceneTests.cs`, one assertion: the Build Settings test required exactly three scenes; it now requires the Lab 3 order as the first three entries, because Rota04 is appended. `Lab3TerrainSceneBuilder.cs`: replaced a `??` on a Unity component (fails on re-run) with an explicit null check. `ProjectSettings/EditorBuildSettings.asset` (Rota04 appended after Rota03). Documentation: `DecisionLog.md` (D-039 … D-043).

Not modified: Rota02, Rota03, SampleScene (MD5 identical before and after, asserted by `Lab4SceneTests`), Lab 3 flight, camera, health and input code, Blender files.

## 2. Rota04 construction
`Rota04` is `AssetDatabase.CopyAsset(Rota03)` (Save As), so it keeps the aircraft, follow camera, terrain, sky, fog, Radar and Lab 3 components. The builder then adds:
- **Uçak:** one `Rigidbody` (kinematic, no gravity) and one non-trigger `BoxCollider` fitted to the model's renderer bounds. The aircraft had neither, and it moves by Transform, so a kinematic Rigidbody is what lets triggers fire. Nothing else on it changed.
- **Oyun** (root): `PuanCuzdani`, `GorevYoneticisi`, `GorevSonuEkrani`, all references assigned.
- **Hedefler/Hedef_1…3:** instances of the prefab `KesifHedefi`, all required, 100 points each, placed on the terrain surface (height from `AraziUretici.Yukseklik`): (150, 400), (−350, 250) and (300, −300) in x/z, i.e. about 430, 430 and 424 m from the start in three directions, 500+ m apart.
- **Us:** a trigger box 120 × 160 × 120 m (centre y 70) at the start/base position, with `UsGirisi`, a flat blue disc as marker; **inactive** in the scene.
- Build Settings: Rota02, SampleScene, Rota03, Rota04.

Target prefab: root `BoxCollider` trigger 30 × 180 × 30, centre (0, 90, 0), `KesifHedefi`, child `Gorsel` (orange pole 3 × 180 m and a 14 m sphere marker). The column reaches from the ground to 180 m, so it is collectable at ordinary altitudes (20 m start altitude and up).

## 3. Event flow
1. Aircraft enters a target trigger → `KesifHedefi.OnTriggerEnter` → `Dene(collider)`. Only an object with `UcusKontrol` on it or a parent counts as the player.
2. First time only: the target hides its visual, disables its collider, then raises the static `KesifHedefi.Fotograflandi(int puan, bool zorunlu)` and the instance event `Toplandi(KesifHedefi)`. The target never touches the score.
3. `PuanCuzdani` (subscribed in `OnEnable`, unsubscribed in `OnDisable`, never twice) adds the points and raises `PuanDegisti(total)`. When the total reaches `kilitEsigi` it raises `UcakKilidiAcildi("Hawker Hurricane")` once.
4. `GorevYoneticisi` listens to each target's `Toplandi` (identity avoids double counts; a `HashSet` guards it). A required target lowers `Kalan` and raises `KalanDegisti(n)`; an optional target does nothing to the count (it still scores through the wallet). At `Kalan == 0` it enters the return phase once: sets `DonusAsamasi`, activates `Us`, raises `DonusAsamasiBasladi`.
5. Aircraft enters `Us` → `UsGirisi.Dene` → `GorevYoneticisi.UsseVarildi()`. Ignored before the return phase and after completion. Otherwise it sets `Tamamlandi` and raises `GorevTamamlandi` once.
6. `GorevSonuEkrani` (temporary `OnGUI`) shows "GÖREV TAMAMLANDI", the wallet's score and a **Yeniden Başlat** button that calls `SceneManager.LoadScene("Rota04")`. Before completion it shows a one-line status ("Kalan hedef: n   Puan: x", then "Üsse dönün"). The aircraft is not frozen after completion.

The static event is cleared in a `SubsystemRegistration` hook, so nothing leaks when domain reload is off; scene reload destroys all subscribers.

Configuration errors are logged, not thrown: missing target list, missing base trigger, `None` entries, missing wallet or manager on the screen. With zero required targets the return phase opens immediately and a warning is logged.

## 4. Economy (all Inspector fields on `PuanCuzdani`)
Target points are set per target (100). `inisPuani` 250, `temizInisPuani` 150 (`InisOdulu(bool temiz)` adds 250 or 400; no landing exists yet, so this is unused until a later phase), `kilitliUcakAdi` "Hawker Hurricane", `kilitEsigi` 300. The numbers are INFERRED defaults; the handout values were not available. With three targets of 100, the unlock fires when the last target is photographed.

## 5. Tests actually run (batch mode, 2026-10-19)
| Run | Result |
|---|---|
| EditMode, whole project | 108 passed, 0 failed, 0 skipped (6 new Lab 4 tests) |
| PlayMode, whole project | 43 passed, 0 failed, 0 skipped (15 new Lab 4 tests) |

Task test list → coverage: 1 `Target_IgnoresObjects…`; 2, 3 `Target_IsCollectedOnce…`; 4, 5 `Wallet_AddsTheEventPointsOnce…`; 6 `Manager_OptionalTargets…`; 7 `Manager_RequiredTargetsReduce…`; 8, 9 `Manager_ReturnPhaseStaysClosed…`; 10 `Base_CannotComplete…`; 11 `Base_CompletesTheMissionOnce…`; 12 and 13 `Screen_ReceivesTheFinalScore_AndRestartsRota04`; 14 `Wallet_UnlockEventFiresOnlyOnce…`. Also: wallet unsubscribe, hidden visual, missing references logged, and in the real Rota04: `Rota04_FlyingThroughTargets_ThenHome…` (first target collected by moving the aircraft with `UcusKontrol.Adim` into the column so real physics triggers fire, the other two at 20 m and 150 m altitude, return over the base completes, restart reloads Rota04 with score 0, 3 targets left, return phase closed) and `Rota04_AircraftOutsideTheColumns…`. EditMode: Rota04 has no missing scripts or references, Lab 3 components and terrain are kept, exactly one kinematic Rigidbody and one collider on the aircraft, three required target columns of the right size, base inactive and wired, Build Settings order, earlier scenes byte-identical.

Not tested: keyboard flight to the targets by a person, the look of the `OnGUI` screen and button click (the button's method is tested, the click is not), frame rate, a Rota04 standalone build (no build made; optional in this phase).

## 6. Limitations
- Targets are collected the moment the aircraft collider touches the column. There is no check that the aircraft is looking at the target.
- The score and the status line are shown by a temporary `OnGUI`; layout is not final. The restart does not reset the mouse cursor or time scale because neither is changed.
- Terrain has no colliders (Lab 3 terrain), so crashes do not exist in this phase.
- Target and economy numbers are defaults (INFERRED).

## 7. Manual checks for you
1. Open `Rota04` and press Play. Top-left shows "Kalan hedef: 3   Puan: 0".
2. Fly to each orange pole (about 430 m away: one ahead right, one ahead left, one behind right). Passing through the column at any height collects it; the pole disappears, the count drops, the score rises by 100.
3. After the third, the text changes to "Üsse dönün" and a blue disc is at the start position. Fly over it: the completion box with "Puan: 300" appears.
4. Click **Yeniden Başlat**: Rota04 restarts with 3 targets.
5. Check the Console for the log lines and for the Hawker Hurricane unlock message at 300.
6. Check Rota02 and Rota03 still play as before.
