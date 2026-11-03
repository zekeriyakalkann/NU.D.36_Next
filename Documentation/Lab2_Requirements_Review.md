# Lab 2 Requirements Review

Date: 2026-10-19. Status: **SUPERSEDED.** The Lab 2 task list was supplied later the same day; see `Lab2_Implementation_Plan.md`. Original status: BLOCKED (no Lab 2 specification available).

**Current state (end of 2026-10-19):**
- Tasks 1 (scene, README, .gitignore) and 3–7 are implemented and verified (`TestReport.md`).
- Still open:
  - U-8: submission format.
  - The MDA analysis (user task).
  - Git authorization.
  - Worksheet values W-1, W-3, W-6, W-8 … W-10 and W-12. No worksheet screenshots were available, so these remain UNKNOWN and use documented defaults.

The tables below are the original pre-specification record.

Sources inspected: `CLAUDE.md`, `Documentation/Requirements.md`, `EnvironmentReport.md`, `DecisionLog.md`, `SetupAndRun.md`, `.claude/agents/*.md`, the Unity project (`NU.D.36_Unity/`), and the instructions given in this conversation. No Lab 2 document, task sheet, or grading rubric exists in the workspace or in the conversation.

## 1. Confirmed

Only facts with an explicit source. None of them describes Lab 2 functionality.

| ID | Statement | Source |
|---|---|---|
| C-1 | Lab 2 is the next development milestone, and work proceeds Lab 2 → 3 → 4 | User master prompt §13, §17; `CLAUDE.md` (Lab Requirements) |
| C-2 | The Lab 2, 3 and 4 documents "have not yet been created" | User master prompt §4 |
| C-3 | Earlier conversation descriptions "do not constitute a complete formal specification" | User master prompt §4 |
| C-4 | The project uses the existing NU.D.36 aircraft with its original geometry preserved | `CLAUDE.md` (Aircraft Preservation); REQ-001 |
| C-5 | Unity 6000.6.4f1; the project lives in `NU.D.36_Unity/` | User master prompt §1, §7; `CLAUDE.md` |

**No Lab 2 feature requirement is confirmed.**

## 2. Inferred

These are technical interpretations, not requirements.

| ID | Interpretation | Rationale |
|---|---|---|
| I-1 | Lab 2 work will need a dedicated scene, rather than the template `SampleScene` | `SampleScene` contains only a camera, light and volume; the aircraft is not placed in any scene |
| I-2 | If Lab 2 needs input, the project already uses the Input System only (`activeInputHandler: 1`) | `ProjectSettings.asset` |
| I-3 | New runtime code will need its own assembly definition and namespace (`NU36`), plus EditMode/PlayMode tests | Existing project conventions (`NU36.Editor`, `NU36.Tests.*`) |
| I-4 | Any feature that moves control surfaces must map them by position, not by the `_R`/`_L` name suffix | DecisionLog D-005 |

## 3. Unknown

| ID | Unknown | Blocks implementation? |
|---|---|---|
| U-1 | Lab 2 task list and evaluation criteria | **Yes: all Lab 2 work** |
| U-2 | Whether any of the topics named in earlier configuration-phase descriptions belong to Lab 2: targeting, radar field of view, Gizmos, projectiles. These are recorded only as unconfirmed candidate areas in `.claude/agents/unity-developer.md` | Yes |
| U-3 | Whether Lab 2 includes aircraft controls, movement or flight physics, or whether these belong to a later lab | Yes |
| U-4 | Whether moving surfaces (ailerons, elevators, rudder, propeller) must be animated in Lab 2 | Yes, if included |
| U-5 | Required scene content: terrain or ground, targets, camera behavior, lighting | Yes, for scene setup |
| U-6 | Required input devices and bindings | Yes, if Lab 2 has input |
| U-7 | Required UI or on-screen output | Yes, if included |
| U-8 | Submission format (project, build, video, report) and deadline | No for implementation; yes for delivery |
| U-9 | Intended meaning of the `_R`/`_L` part names | Only if control surfaces are animated |
| U-10 | Whether template sample content (`TutorialInfo/`, `Readme.asset`, `SampleScene`) may be removed | No |

## 4. Existing implementation relevant to Lab 2

| Item | Location | Status |
|---|---|---|
| Aircraft model (29 meshes, metres, nose +Z) | `Assets/Models/NU_D_36.fbx` | VERIFIED by EditMode tests |
| Reproducible import settings | `Assets/Scripts/Editor/AircraftModelPostprocessor.cs` | VERIFIED (import tests pass) |
| Batch build entry point | `Assets/Scripts/Editor/BuildCommands.cs` | VERIFIED (Windows build succeeded) |
| EditMode tests (8) | `Assets/Tests/EditMode/` | Passed on 2026-10-19 (not rerun in this task) |
| PlayMode smoke test (1) | `Assets/Tests/PlayMode/` | Passed on 2026-10-19 (not rerun in this task) |
| Scene | `Assets/Scenes/SampleScene.unity` (only scene in Build Settings) | Template content only; no aircraft |
| Input actions | `Assets/InputSystem_Actions.inputactions` (template maps `Player`, `UI`) | Template default, not aircraft-specific |
| Packages | URP 17.6.0, Input System 1.20.0, Test Framework 1.8.0, uGUI 2.6.0, Timeline, AI Navigation, Visual Scripting, IDE integrations | From template, unchanged |
| Runtime (gameplay) scripts | None | Not started |

## 5. Potential dependencies

- The Lab 2 specification (U-1), which every item below depends on.
- If Lab 2 has interaction: a scene containing the aircraft, and possibly a prefab built from the FBX.
- If Lab 2 has movement or controls: a decision on the Lab 2/Lab 3 boundary (U-3), and on input bindings (U-6).
- If Lab 2 animates control surfaces: verifying the pivots and resolving the naming question (U-4, U-9). Pivot positions are NOT TESTED.
- If Lab 2 needs textures: the model has no UVs, so Blender changes would need user approval.

## 6. Questions that block implementation

1. **Please provide the Lab 2 assignment text and its evaluation criteria** (as a file, file path, or pasted text). This is the only question that blocks starting.
2. Only if the material does not say so: which features belong to Lab 2 and which to Lab 3 (especially controls and movement)?

All other unknowns (U-5 … U-10) are expected to be answered by the Lab 2 material. They will be asked about only if it leaves them open.
