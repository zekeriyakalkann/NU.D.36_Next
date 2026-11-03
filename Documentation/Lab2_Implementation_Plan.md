# Lab 2 Implementation Plan

Date: 2026-10-19. Status: **APPROVED and IMPLEMENTED for Tasks 1 (scene, README, .gitignore), 3–7. Verified by automated tests, an Editor Play Mode run, image evidence and a Windows build (`TestReport.md`). Task 2 (MDA) is a user task. Git is not initialized.** Decisions: `DecisionLog.md` D-009 … D-024.

Changes after the first implementation:
- **D-022:** the arrival distance changed from 5 to 10 m, so the aircraft no longer stops inside Balon.
- **D-023:** the Main Camera changed to a side view (presentation only).
- **D-024:** visual verification tooling was added.

Lab coordinates, the radar logic and the roll logic were not changed.

## Resolution of worksheet ambiguities (2026-10-19)

No worksheet screenshots were available in the conversation at implementation time, so none of these was resolved from the worksheet.

| Item | Resolution |
|---|---|
| W-1 Repository details | **OPEN.** README and .gitignore created at the workspace root (D-020); Git init, commit and push need authorization |
| W-2 / W-11 Uçaksavar forward | Resolved from the task list ("forward toward +Z"): identity rotation (D-013) |
| W-3 Parent-child | Technical default (D-011). **OPEN:** the worksheet's specific parenting exercise, if any |
| W-4 Aircraft position | Root and model both at world (0, 20, 0); no ambiguity remains |
| W-5 Colliders | Default kept (D-014) |
| W-6 Speed, turn rate, threshold | Defaults: 20 m/s and 90°/s (D-015); arrival distance 10 m (D-022). **UNKNOWN: may need worksheet values** |
| W-7 Normalization test | Implemented as unit-length and distance-independent step tests, with a written explanation (TestReport) |
| W-8 – W-10 Radar target, values, output | Defaults (D-016). **May need worksheet values** |
| W-12 Roll formula | Default (D-017). **OPEN:** the worksheet's exact formula |
| W-13 Sign convention | Resolved empirically (+1 = right, banks right wing down) |
| W-14 Gizmo details | Default (D-018) |

## 0. Source of requirements

- **Primary source (CONFIRMED):** the Lab 2 task list in the user's message of 2026-10-19 ("NU.D.36 — Lab 2 Implementation Planning"), referred to below as *Task list §n*.
- **Worksheet screenshots:** the message refers to screenshots as the authoritative source, but **no screenshots are present in the conversation**. Every detail the task list delegates to the worksheet ("as specified", "follow the worksheet") is therefore marked **UNKNOWN (W-x)** until the screenshots or the worksheet text are supplied.
- Supersedes the BLOCKED status in `Lab2_Requirements_Review.md` for the items listed here.

## 1. Current project state relevant to Lab 2

| Item | State |
|---|---|
| Unity project | `NU.D.36_Unity/`, Unity 6000.6.4f1, URP, Input System only (`activeInputHandler: 1`) |
| Aircraft | `Assets/Models/NU_D_36.fbx`: 29 meshes, metres, nose +Z, pivot at the model origin (combined bounds centre ≈ (0, 0, -0.035)); not placed in any scene |
| Scenes | `Assets/Scenes/SampleScene.unity` only: camera, light, volume |
| Runtime scripts | None |
| Tests | 8 EditMode tests (model import) and 1 PlayMode test (first build scene loads); all passed on 2026-10-19 |
| Git | Not initialized |

No Lab 2 worksheet task is satisfied by the current project. The imported model is a prerequisite for tasks 3–7, not a completed task.

## 2. Architecture (INFERRED; does not change requirements)

- **Runtime assembly:** `Assets/Scripts/Runtime/NU36.Runtime.asmdef` (namespace `NU36`). The PlayMode test assembly references it. Keeping it separate keeps scripts testable and leaves room for Labs 3–4.
- **Testable vector maths:** each MonoBehaviour follows the worksheet's code. Its vector computation (direction, dot-product view test, cross-product side sign) is exposed as a small `public static` method on the same class. EditMode tests can then check the maths without a scene. No extra framework.
- **Reproducible scene:** `Rota02` is created by an editor command (`NU36.Editor.Lab2SceneBuilder.CreateRota02`) that runs in batch mode. The command refuses to overwrite an existing scene. After that, the scene can be edited normally in the Editor.
- **Aircraft hierarchy (pending W-3):** proposed `Ucak` root (moved and rotated by `HedefeGit`) → `NU_D_36` model instance as child (receives visual roll). This keeps movement heading and visual bank from overwriting each other. Final structure follows the worksheet's parent-child instructions.

## 3. Tasks

### Task 1: Project and repository preparation
- **Source:** Task list §1.
- **Existing:** none.
- **Files:** `Assets/Scenes/Rota02.unity` (+ .meta). Add `Rota02` to `ProjectSettings/EditorBuildSettings.asset` (a project-settings change, needed for PlayMode loading and builds; SampleScene stays). Optional on approval: `.gitignore` (standard Unity ignores: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, `*.csproj`, `*.slnx`) and `README.md` at the workspace root.
- **Dependencies:** none.
- **Acceptance:** `Rota02` exists, opens without errors, and is listed and enabled in Build Settings.
- **Tests:** EditMode `Rota02_ExistsAndIsInBuildSettings`.
- **Manual / authorization:** `git init`, the first commit, the remote URL and the push all require explicit user authorization. The worksheet's exact repository requirements (W-1) are needed: repository location (workspace root or `NU.D.36_Unity/`), required README content, and how it is submitted.

### Task 2: MDA worksheet
- **Source:** Task list §2.
- **Existing:** none.
- **Files:** optional `Documentation/Lab2_MDA.md` template with the required fields (game name and link, mechanics, dynamics, aesthetics, one mechanic change that would alter the aesthetics).
- **Manual:** **the user must play and analyze the game.** I will not write gameplay observations. I can format the user's notes.
- **Acceptance / tests:** none automated.

### Task 3: Scene setup and transformations
- **Source:** Task list §3.
- **Existing:** aircraft FBX only.
- **Files:** `Assets/Scripts/Editor/Lab2SceneBuilder.cs` (editor-only); `Rota02.unity`.
- **Planned content:**
  - Aircraft at (0, 20, 0).
  - `Balon`: Sphere primitive, scale (6, 6, 6), position (40, 30, 60).
  - `Uçaksavar`: Cylinder primitive, position (0, 0, 0), forward toward +Z.
- **Ambiguities:**
  - W-2: what "forward toward +Z" means for the Cylinder. With identity rotation `transform.forward` is already +Z, but the cylinder's long axis is +Y. Does the worksheet want a rotation such as (90, 0, 0), so the barrel points along +Z?
  - W-3: the exact parent-child relationships, and which transformation behaviour must be demonstrated.
  - W-4: whether the aircraft root's position or the model's position is meant by (0, 20, 0).
  - W-5: whether colliders on the primitives must be kept or removed.
- **Acceptance:** each object exists with the exact names (including `ç` in `Uçaksavar`) and with the specified world position, scale and forward vector (tolerance 1e-4).
- **Tests:** EditMode `Rota02_ObjectsHaveSpecifiedTransforms`, which opens the scene with `EditorSceneManager` and checks the values. The parent-child behaviour test is defined after W-3.

### Task 4: `HedefeGit.cs`: move toward a target
- **Source:** Task list §4.
- **Files:** `Assets/Scripts/Runtime/HedefeGit.cs`.
- **Behaviour:**
  - `direction = (target.position - transform.position).normalized`.
  - The rotation turns toward `Quaternion.LookRotation(direction)` with `Quaternion.RotateTowards`, limited to a configurable degrees-per-second rate.
  - The object moves along `transform.forward` at the configured speed.
  - It stops (or switches behaviour, per W-6) when the distance is below the configured threshold.
  - `[SerializeField]` speed, turn rate, threshold and target; no magic numbers.
- **Ambiguities:**
  - W-6: default speed, turn rate and threshold values, and the behaviour on arrival.
  - W-7: the exact "normalization test" the worksheet asks for.
- **Acceptance:**
  - The direction vector has magnitude 1 ± 1e-5 for any non-zero offset.
  - From (0, 20, 0), the aircraft reaches within the threshold of `Balon` in finite time and does not oscillate around it.
  - Speed along the path is independent of the distance to the target (this is what normalization ensures).
- **Tests:**
  - EditMode: direction magnitude for several offsets; zero-offset handling.
  - PlayMode:
    - The aircraft reaches the target within the expected time bound.
    - The per-frame displacement equals speed × deltaTime.
    - The rotation never exceeds turn rate × deltaTime per frame.
- **Explanation deliverable:** short written explanation of why normalization makes speed constant (in the test report).

### Task 5: `Radar.cs`: field of view with the dot product
- **Source:** Task list §5.
- **Files:** `Assets/Scripts/Runtime/Radar.cs`, on `Uçaksavar`.
- **Behaviour:**
  - The target is visible when `distance <= maxRange` and `Vector3.Dot(forward, toTarget.normalized) >= cos(halfAngle)`.
  - `[SerializeField]` half-angle (degrees), max range and target.
- **Ambiguities:**
  - W-8: which object is the target (the aircraft or `Balon`).
  - W-9: default half-angle and range.
  - W-10: the worksheet's exact code and output (log, flag, colour, event).
  - W-11: whether "in front" uses the cylinder's `forward` (+Z) or its long axis (`up`); this is linked to W-2.
- **Acceptance:**
  - True for a target straight ahead within range.
  - False for a target behind, false beyond range.
  - At exactly ±halfAngle the boundary behaviour matches the chosen comparison (`>=`).
  - True just inside and false just outside the angle.
- **Tests:**
  - EditMode on the static method: 0°, halfAngle − 1°, halfAngle + 1°, 90°, 180°, range ± ε.
  - PlayMode: place the target at those angles in a test scene and check the component output.

### Task 6: Wing roll direction with the cross product
- **Source:** Task list §6.
- **Files:** extend `HedefeGit.cs` or add `Assets/Scripts/Runtime/KanatYatisi.cs` (per W-12).
- **Behaviour:**
  - `side = Mathf.Sign(Vector3.Cross(transform.forward, toTarget).y)`.
  - The roll angle is applied to the visual child per the worksheet's roll behaviour.
- **Ambiguities:**
  - W-12: the worksheet's roll formula, maximum bank angle, smoothing, and whether it is a separate script.
  - W-13: the expected sign convention ("right = positive" or otherwise).
- **Signs (theory, to be verified empirically as required):** Unity is left-handed and Y-up. With forward = +Z and a target on the right (+X), `Cross(+Z, +X) = +Y`, so the sign is positive. Banking right means the right wing (+X) goes down, which is a negative rotation about local +Z. Both statements are **theoretical** until tested.
- **Acceptance:**
  - A target on the right gives sign +1 and the aircraft banks right (right wing lower than the left wing in world Y).
  - A target on the left gives the mirror result.
  - A target straight ahead gives 0 and the aircraft returns to level.
- **Tests:**
  - EditMode on the sign function for left, right and ahead targets.
  - PlayMode: measure the world Y of a right-side and a left-side reference point during a turn.
  - Note: the model's `NU36_*_R` meshes are on the pilot's **left** (D-005), so the test uses world positions, not mesh names.

### Task 7: Gizmos
- **Source:** Task list §7.
- **Files:** `OnDrawGizmos` (or `OnDrawGizmosSelected`, per W-14) in `HedefeGit.cs` and `Radar.cs`.
- **Behaviour:**
  - Draw the forward direction and the target direction.
  - The colour shows visibility, for example green when visible and red when not (W-14).
  - Drawing only; no gameplay logic in Gizmo methods.
- **Ambiguities:** W-14, the colours, lengths and which object draws what.
- **Acceptance:** lines are visible in the Scene view and the colour changes when the target enters or leaves the radar view.
- **Tests:** Gizmo rendering cannot be asserted automatically. An EditMode test only checks that the visibility state Gizmos read comes from the same method the radar uses. **Visual check is manual (user, Scene view).**

### Bonus tasks
Satellite behaviour, mouse-aimed targeting, projectile movement, the radar cone, and Three.js are **not planned**. They wait until Tasks 1–7 are reviewed, per the task list.

## 4. Implementation sequence

1. Runtime assembly and test assembly reference (`NU36.Runtime.asmdef`; `NU36.Tests.PlayMode.asmdef` and `NU36.Tests.EditMode.asmdef` gain a reference). This modifies existing test asmdef files.
2. Task 3 scene builder, then generate `Rota02` and add it to Build Settings (Task 1 scene part). Run the EditMode scene tests.
3. Task 4 `HedefeGit` with EditMode and PlayMode tests.
4. Task 5 `Radar` with its tests.
5. Task 6 roll with an empirical sign test.
6. Task 7 Gizmos; manual visual check.
7. Run the full EditMode and PlayMode suites and a Windows build. Update `TestReport` and `DecisionLog`.
8. Optional on authorization: `.gitignore` and `README.md`. Git operations only with explicit authorization.

Each step: compile → test → fix → rerun. Unity batch mode runs only while the Editor is closed.

## 5. Technical risks

- **Missing worksheet detail (W-1 … W-14):** the main risk. Defaults chosen without it may not match grading.
- **Heading vs roll conflict:** `HedefeGit` sets the full rotation every frame and would erase a bank applied to the same transform. This is mitigated by the root/visual-child split (pending W-3).
- **Turning circle:** with a low turn rate and a small threshold, the aircraft can orbit the target forever. PlayMode tests will detect this, and the parameters are tuned or documented.
- **`ç` in `Uçaksavar`:** non-ASCII names are fine in Unity. If a script looks the object up by name, the source file must be UTF-8. Prefer serialized references to name lookups.
- **The PlayMode smoke test loads build index 0:** keep SampleScene at index 0, or update the test to load `Rota02` by name. This will be decided in step 2.
- **Primitive colliders:** spheres and cylinders get colliders by default. They are harmless for Lab 2 unless the worksheet says otherwise (W-5).

## 6. Tasks requiring manual user input

| Item | Why |
|---|---|
| Worksheet screenshots or text | Needed for W-1 … W-14 |
| MDA analysis | Requires actually playing a browser game |
| Git init / commit / remote / push | Requires explicit authorization and a remote URL |
| Gizmo and visual checks in the Scene view | Cannot be verified automatically |
| Opening the Editor | Close it before I run batch-mode steps |
