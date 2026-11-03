# Lab 4, Phase 2: countdown, crash, landing, failure

Date: 2026-10-19. Builds on `Lab4_Phase1_Implementation.md` (verified by the user). The handout was not available as a file; handout values quoted in the task text (pitch 18°, bank 27°, 2.5 s restart, about 150 s) are used, the rest are INFERRED defaults. Phases 3–4 (HUD artwork) are not started.

## 1. Mission state (one source of truth)
`GorevYoneticisi` now exposes `Durum`: **Aktif → Donus → Tamamlandi**, or **Basarisiz** from Aktif/Donus. Success and failure exclude each other; every transition happens once.

| Event | Effect |
|---|---|
| Last required target | `Donus`, base trigger enabled (Phase 1) |
| Base reached in `Donus` | `Tamamlandi`, countdown stops |
| Base reached earlier, or after failure | ignored |
| Countdown reaches 0 | `GoreviBasarisizYap("SÜRE DOLDU")` |
| Crash (`InisAlgilayici.Cakildi`) | `GoreviBasarisizYap("ÇAKILDIN")` |
| Failure after completion, or a second failure | ignored (returns false) |

On failure every target is closed (`KesifHedefi.Kapat`), so no more points are awarded, and the base trigger is disabled. `GorevBasarisiz(string)` is raised once.

## 2. Countdown
`GorevYoneticisi.sure`, default **150 s** (Inspector). `Update` calls `Adim(Time.deltaTime)`; `KalanSure` is readable; `sure <= 0` disables the countdown (`GeriSayimAktif` false, `KalanSure` 0). It stops after completion or failure. `SureAyarla(s)` changes and restarts it (used by tests).

## 3. Ground contact and crash (`InisAlgilayici`, on Uçak, LateUpdate)
The terrain has no colliders and the aircraft moves by Transform, so contact is a probe: the lowest point (pivot y − `tekerOfseti`, 1.22 m from the aircraft collider) against `AraziUretici.YuzeyYuksekligi(x, z)`, which returns the height of the actual terrain mesh triangles (same checkerboard diagonals as the chunks; tested against generated meshes). No physics, no second Rigidbody, terrain and `UcusKontrol` unchanged. Start position (y 20 over ground −1 m) is 19 m clear, so nothing triggers at the start.

At contact it measures and classifies:
| Check | Limit (Inspector) | Convention |
|---|---|---|
| Speed `UcusKontrol.Hiz` | ≤ 16 m/s (INFERRED; range 8–42) | |
| Pitch error | ≤ 18° | `asin(forward.y)` minus the terrain slope along the heading (nose up positive); a climbing slope matched by the nose is not an error |
| Bank | ≤ 27° | `|atan2(right.y, up.y)|`; upside down = 180° |
| Ground slope | ≤ 20° (INFERRED) | steepest slope under the aircraft |

Any limit exceeded → crash, with reason "çok hızlı", "dik açı", "aşırı yatış" or "dik arazi": `UcusKontrol` is disabled (the aircraft stops) and `Cakildi(reason)` is raised once. Otherwise a **controlled landing**: `InisYapildi` is raised, the aircraft is held on the surface, and only after it climbs 3 m clear can the next contact be classified. Landing raises no score (the wallet's landing points are not wired in this phase).

## 4. Failure feedback and restart
- `GorevSonuEkrani` (temporary OnGUI, Phase 1 screen extended): time `Süre m:ss` under the status line, red and larger at ≤ 10 s; on failure a red panel with the reason (ÇAKILDIN / SÜRE DOLDU) and "yeniden başlıyor". Success screen unchanged. Nothing from a previous run is shown after the reload.
- `YenidenBasla` (on Oyun): on `GorevBasarisiz` it disables `UcusKontrol` (no more flight updates), waits **2.5 s**, then loads `Rota04` once. Rota04 is already in Build Settings (order unchanged).

## 5. Files
New: `Runtime/InisAlgilayici.cs`, `Runtime/YenidenBasla.cs`, `Editor/Lab4Phase2SceneBuilder.cs` (menu **NU36 → Lab 4 → Add Phase 2 To Rota04**, batch `-executeMethod NU36.Editor.Lab4Phase2SceneBuilder.Apply`), `Tests/PlayMode/Lab4Phase2Tests.cs`, `Tests/EditMode/Lab4Phase2SceneTests.cs`.
Modified: `GorevYoneticisi.cs` (countdown, state, failure), `KesifHedefi.cs` (`Kapat`), `GorevSonuEkrani.cs` (time and failure panel), `AraziUretici.cs` (added `YuzeyYuksekligi`; no change to the terrain output), `Rota04.unity` (detector on Uçak, `YenidenBasla` on Oyun, manager references), `DecisionLog.md` (D-044 … D-047).
Not modified: `UcusKontrol`, camera, health, input, terrain scripts' behaviour, Rota02, Rota03, SampleScene (MD5 identical; asserted by `Lab4SceneTests`).

## 6. Tests actually run (batch mode)
| Run | Result |
|---|---|
| EditMode, whole project | 110 passed, 0 failed, 0 skipped |
| PlayMode, whole project | 67 passed, 0 failed, 0 skipped |

New: 2 EditMode (Rota04 wiring, surface height equals the mesh at 240 random mesh points) and 24 PlayMode, covering: countdown decreases by the given step and by real time (Rota04, 150 s); expiry fails once; disabled countdown; completed mission never fails and the timer stops; controlled landing; landing on a slope aligned with the nose; crash by speed, nose up, nose down, bank, inverted and steep ground; configurable limits; repeated detection raises each event once, and a second landing after climbing away; no contact in normal flight; crash and timeout each show the right reason, stop the aircraft, wait 2.5 s and reload "Rota04" once; crash plus timeout give one failure; no points and no completion after failure; base closed on failure; in the real Rota04 a timeout reload restores the initial state (3 targets, no message, no score), a real fast ground contact fails with ÇAKILDIN, and a slow level touchdown is a landing and the mission continues. All earlier Lab 2, 3 and 4 Phase 1 tests still pass.

Not tested: manual flight or landing by a person, the look of the failure panel and warning colours, whether the limits feel right, build (none made).

## 7. Limitations
- A landing keeps the aircraft on the ground, but its attitude is not levelled and the ground contact is a single point under the pivot; wings or a nose can pass through hills. Mountains are only detected at the pivot's wheel point.
- The limit values for speed (16 m/s) and slope (20°) are guesses; at the default throttle (25 m/s) a touchdown is a crash unless the player lowers the throttle.
- Landing earns no points yet.
- Failure freezes the aircraft; the camera keeps following it.

## 8. Manual checks (Rota04)
1. Play: top-left shows targets, score and "Süre 2:30" counting down.
2. Wait or lower the time (Oyun → GorevYoneticisi → Sure, e.g. 15): at 10 s the time turns red and large; at 0 a red "SÜRE DOLDU" panel appears, the aircraft stops, and about 2.5 s later Rota04 restarts at 3 targets and full time.
3. Dive at the ground at full speed: "ÇAKILDIN", same restart.
4. Throttle down (↓ to 8 m/s), level wings, nose level, glide to the ground away from hills: you should roll on the surface with no failure. Then climb away again.
5. Collect all three targets, return over the base before time ends: success screen; wait: no failure follows.
6. Rota02 and Rota03 behave as before.
