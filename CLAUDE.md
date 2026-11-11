# NU.D.36 Simulation Project

## Project Goal
Develop a professional, modular aircraft simulation around the existing NU.D.36 aircraft model, built as a new Unity project while preserving the original aircraft design.

## Language
Communicate with the user in Turkish. Include English equivalents for important technical terms when useful.

## Current Project State (as of 2026-11-03)
- Initial setup is complete. The Unity project already exists at `NU.D.36_Unity/` (Unity 6000.6.4f1, URP Blank template). Do not create a new project or rebuild the existing one; continue development inside `NU.D.36_Unity/`.
- Blender 5.2.2 LTS exported the aircraft to `Exports/NU_D_36.fbx`, which is imported as `NU.D.36_Unity/Assets/Models/NU_D_36.fbx`.
- Reported setup results: Blender mesh comparison passed, project creation, asset import and C# compilation succeeded, EditMode tests 8/8 and PlayMode tests 1/1 passed, Windows standalone build succeeded. The workspace `.blend` SHA256 and timestamp were unchanged.
- Not yet verified: visual appearance, material presentation, glass transparency, moving-part pivot positions, and running the built player.
- Lab 2 (scene `Rota02`, `HedefeGit`, `Radar`, `KanatYatisi`, Gizmos) is implemented from the task list supplied in the conversation; worksheet screenshots were not available, so several values are documented defaults (`Documentation/Lab2_Implementation_Plan.md`, `DecisionLog.md` D-009 … D-024). Verification status: `Documentation/TestReport.md`.
- Lab 3 (scene `Rota03`, a copy of Rota02; `UcusKontrol`, `PlayerInput` with map `Ucus`, `Saglik`/`CanYazisi`, `TakipKamerasi`) is implemented and tested (`Documentation/Lab3_Implementation.md`, D-025 … D-033). The labs accumulate in this one project: keep earlier lab scenes unchanged and their tests passing. The MDA worksheet is a user task. Lab 2 awaits user review before Lab 3.
- Lab 4 (scenes `Rota04`, `Rota04Hud`: reconnaissance loop, landing/crash, countdown, HUD, flight model) is implemented (`Documentation/Lab4_*.md`, D-039 … D-054).
- Lab 5 / Hafta 5 "Bölüm 2 — Sınır Hattı" (scene `Rota05`, copy of Rota04Hud; line of sight, sound locator, tunnelling/sweep, bomb and release window, level loop) is implemented and tested from the worksheet text pasted on 2026-11-03 (`Documentation/Lab5_Implementation.md`, D-055 … D-066). The course's supplied files (Patlama, MuhimmatMevzii, SeviyeYoneticisi, DalisGostergesi) and the Hurricane model were not available: stand-ins exist, the model is BLOCKED. Project layers 8–12 (Ucak, Arazi, Yapi, Hedef, Mermi) belong to Lab 5.
- Lab 6 / Hafta 6 "Kâğıt Görev" (scene `Bolum3_GriKutu`, copy of Rota05; Bölüm 3 grey box from the map data `Bolum3Harita`, stopwatch; Blender paper map in `Exports/Lab6/`; GDD draft) is implemented (`Documentation/Lab6_Bolum3_Tasarim.md`, `Lab6_GDD_Taslagi.md`, D-067 … D-073). The paper kit, browser game and GDD template were not available; the paper play steps are user tasks.
- Lab 7 / Hafta 7 (scene `Rota07`, copy of Bolum3_GriKutu; night mission of Bölüm 3 from the user's request: dark foggy 8 km map with airfield, river, 3 searchlight towers and 3 bridges to bomb, landing in the marked zone, smoke/fire effects, landing light, aerodynamic flight model as an opt-in mode of `UcusKontrol`) is implemented (`Documentation/Lab7_Implementation.md`, D-074 … D-082). No worksheet text was supplied; requirement source is the user's message.
- 2026-11-10:
  - **Menu (user request):** scene `AnaMenu` with main menu and Esc pause menu; airspeed dial 0–500 km/h and throttle gauges in Rota07 (D-083, D-084).
  - **Worksheet rework:** the Hafta 6–7 worksheet ("Kâğıt Görev", graded as one assignment) arrived as the authority. Rota07 was rebuilt on the Bölüm 3 paper map v0.1 at 1 square = 200 m: one bridge H5–J5 as the target, searchlights F4/K5/H7, guns with tracers, sound locator H3, cloud rule E5–G5, dawn at 480 s, return to base (`Documentation/Lab7_Implementation.md`, D-085 … D-093).
  - **Assists (user request):** easy flight (K), brake (F/B), targeting with guided bombs and automatic release (T/O) and a GPS map (M) in Rota07, all on at the start (`Documentation/Lab7_Implementation.md` §11, D-095 … D-100). With K and O off the worksheet game is unchanged; the older Lab 7 Play Mode tests run with the assists off.
  - **User tasks:** the paper steps 01–04 (printable forms in `Exports/Lab6/kagit/`), the GDD in the course Word template (content in `Lab6_GDD_Taslagi.md`), and the submission zip (`Tools/Teslim/teslim_zip.ps1`).
- 2026-11-11: **Lab 8 / Hafta 8 "Bölüm 4 · Kurşun Dağları"** (scene `Bolum4_KursunDaglari`, copy of Rota07; train escort through the mountains, worksheet text pasted 2026-11-11, due 15.11.2026). Plan `Documentation/Lab8_Plan.md` (5 phases); state in `Lab8_Implementation.md` (D-101 …). Phases 1 (terrain, railway, train, stations, base, start, 5-min schedule), 2 (enemies, machine gun, bomb rules, damage sides, failures) and 3 (HUD warnings/markers, Görev tamam panel, engine smoke, T/O assists, GPS, main menu now starts Bölüm 4) are done and tested; phase 4 (look fixes, full-round test, asset register test, builds Windows_Oyun and Windows_Bolum4) is done; phase 5 (trimmed submission copy via `Tools/Teslim/teslim_h8.ps1`, verified with a TEST run; video guide `Lab8_Video_Yonergesi.md`) is done. After the user played the build, Bölüm 4 got an arcade flight mode (default there; K cycles to the physics modes; the user approved it), synthesised sound and an always-visible train marker (D-122 … D-124). User tasks: record the video, run the script with the student number and the video, upload. Downloads in `Indirilenler/Bolum4/` (not in Assets); asset register `Assets/VARLIKLAR.md`.
- Details and evidence: `Documentation/EnvironmentReport.md`, `Requirements.md`, `DecisionLog.md`, `SetupAndRun.md`. Keep them consistent with the project when things change.

## Workspace Layout
- `Blender/`: workspace copy of the aircraft (`.blend`, `.blend1`).
- `Tools/Blender/`: `inspect_scene.py`, `export_fbx.py`, `verify_fbx.py`.
- `Exports/`: FBX output from Blender.
- `NU.D.36_Unity/`: the Unity project.
- `Builds/`, `Logs/`, `Documentation/`: player builds, tool logs and test results, and project documentation.

## Core Rules
- Keep code modular, readable, and maintainable.
- Prefer the smallest reliable implementation over unnecessary complexity.
- Inspect the existing architecture, configuration, and documentation before making changes.
- Preserve Unity .meta files and asset references when working with existing Unity assets.
- Do not change package versions, render pipeline, or project settings without a confirmed need, and record such changes in `Documentation/DecisionLog.md`.
- Do not commit, push, reset, clean, or rewrite Git history unless the user explicitly authorizes the specific operation. Do not initialize Git without explicit authorization.
- Do not delete or overwrite project files, backups, or submission archives without explicit authorization.
- Do not install software or packages system-wide without authorization.
- Do not change Windows security settings, and do not bypass permission restrictions.
- Do not run Unity batch mode while the Unity Editor is open, and never run competing Unity instances against the same project.

## Protected Locations
- `C:\AI\NU.D.36_Git` (legacy project) is strictly prohibited. Do not read, list, search, copy, execute, modify, or otherwise access it under any circumstances, and do not use it as a reference.
- `C:\AI\_PermTest` is prohibited. Do not access it.
- Subagents must never access either location.
- Do not copy legacy Unity scenes, scripts, prefabs, or settings into the project.

## Aircraft Preservation
- The original NU.D.36 aircraft design is a protected asset.
- The original Blender source `C:\AI\NU.D.36\Blender\NU.D.36_project_FINAL.blend` must never be overwritten, modified, renamed, moved, or deleted. Use only the workspace copy in `Blender/` for project operations.
- Preserve the original geometry. Do not alter it, or any Blender source file, without explicit user approval.
- Blender tools must never call `save_mainfile` or `save_as_mainfile`. Before any operation that could modify an asset, verify that its resolved path is inside the workspace.

## Available Workflows
- Blender automation: run `blender.exe --background --factory-startup --disable-autoexec` on the workspace copy with the scripts in `Tools/Blender/`. Verify exports by re-import (`verify_fbx.py`) and confirm the `.blend` hash is unchanged.
- Unity integration: FBX import settings are enforced by `Assets/Scripts/Editor/AircraftModelPostprocessor.cs`. Batch-mode test and build commands are in `Documentation/SetupAndRun.md` (`NU36.Editor.BuildCommands.BuildWindows` builds to `Builds/Windows/`).
- Known model facts: the model has no UVs, and `*_R` parts sit on the pilot's left (Unity -X; DecisionLog D-005). Map controls by actual position, not by name suffix, until the user confirms the naming.

## Lab Requirements
- User-provided LAB 2–4 materials are the authoritative source for laboratory requirements.
- Lab 2 is the next development milestone, and its requirements document is the next dependency.
- Do not invent requirements or present proposals as required features. Earlier conversation descriptions and agent-definition topics are not confirmed requirements.
- Classify requirements as CONFIRMED (explicitly supported by supplied material), INFERRED (implementation decisions that do not change the educational requirements; document the rationale), or UNKNOWN (state whether each blocks implementation).
- If a requirement cannot be verified from the available material, report it as unresolved rather than guessing. Ask the user only when a missing requirement materially affects implementation; continue independent, safe work meanwhile.
- Labs 3 and 4 must wait until their requirements are known and the preceding lab has been reviewed and its status reported.
- Before delegating requirement-specific analysis, ensure the relevant material is available in the agent's context or at a verified, explicitly supplied file path.

## Autonomy and Approval
- Perform routine, safe, reversible development tasks yourself (creating scripts and tests, fixing defects, running tests and builds, updating documentation) rather than describing them to the user. Do not ask repeatedly for approval of an already approved task.
- Destructive operations, Git actions, purchases, system-wide installs, and changes to Blender source geometry remain restricted and require explicit authorization. Access to the protected locations and changes to the original Blender source are never permitted (see above).
- A proposed file, component, scene, folder, or architecture is not an approved implementation plan. Only the main session consolidates proposals into a plan.
- Implement Lab features only for confirmed requirements. Do not build speculative gameplay features or unrequested scope.
- If a tool call is denied, inspect the reason and choose a safe alternative; if none exists, report the blocker.

## Main-Session Ownership
- The main session owns project planning, task assignment, architecture decisions, final integration, and scene/project configuration.
- project-coordinator is a read-only architecture reviewer. It advises and identifies risks but does not coordinate implementation, assign tasks, or make project decisions.
- Specialist agents (unity-developer, flight-physics-engineer) provide technical analysis and proposals.
- The main session reviews proposed changes before integration. Do not assume a specialist implemented or verified anything because it produced a proposal.
- Do not let multiple agents modify the same scene or file concurrently. Use parallel analysis only for independent tasks.

## Write Permissions
- All specialist agents remain read-only.
- Do not grant write tools merely because an implementation proposal exists.
- Any change in agent permissions requires a user-approved, integrated implementation plan and a separate review of the necessary tool permissions.
- Keep scene and project settings under main-session control.

## Testing and Requirement Validation
- Test code changes wherever possible: compile, run EditMode/PlayMode tests (Unity Test Framework), and build when relevant. Keep test results and logs inside the workspace (`Logs/`).
- Report each item as IMPLEMENTED, VERIFIED, PARTIALLY VERIFIED, NOT TESTED, or BLOCKED. Distinguish theoretical analysis, compilation, build verification, and runtime-tested behavior; a successful compile or export is not a gameplay or visual test.
- Requirement mappings identify the source requirement, feature, implementation approach, dependencies, risks, acceptance criteria, and tests, using REQ-xxx / TEST-xxx identifiers. Acceptance criteria not traceable to a confirmed requirement are labeled engineering recommendations.
- Never claim successful testing, or that a feature works, without supporting evidence.

## Workflow
1. Inspect before changing.
2. Explain the proposed implementation and relevant risks when the change is non-routine.
3. Make focused, reversible changes.
4. Run feasible compilation and functional tests; diagnose and fix failures, then rerun.
5. Report what changed, what was tested, and what remains unverified.
6. Ask for user approval before any Git operation.
