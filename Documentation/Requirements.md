# Lab 2 Requirements Analysis

Status (updated 2026-10-19): Lab 2 tasks were supplied as a task list in the conversation and are implemented. See `Lab2_Implementation_Plan.md`, `TestReport.md` and `DecisionLog.md`. The analysis below is the original pre-specification record.

Original status: **BLOCKED — no Lab 2 specification available.** Date: 2026-10-19.

Lab 2, 3 and 4 documents have not been provided. Nothing below is an official course requirement unless marked CONFIRMED with a source.

## 1. Confirmed requirements (project level)

| ID | Requirement | Source | Status |
|---|---|---|---|
| REQ-001 | Use the existing NU.D.36 aircraft model and preserve its original geometry | `CLAUDE.md` (Aircraft Preservation) | VERIFIED for import (TEST-001…006) |
| REQ-002 | New, modular Unity project built from scratch; do not reuse the legacy project | `CLAUDE.md`, user master prompt §1–2 | IMPLEMENTED (project skeleton) |
| REQ-003 | Proceed sequentially Lab 2 → Lab 3 → Lab 4; do not implement later labs early | User master prompt §13 | In force |
| REQ-004 | Unity Editor version 6000.6.4f1 | User master prompt §1, §7 | VERIFIED (`ProjectVersion.txt`) |

## 2. Inferred implementation decisions
See `DecisionLog.md` (D-001 … D-008). None of them adds gameplay behaviour.

## 3. Unknown requirements

| ID | Unknown | Blocks implementation? |
|---|---|---|
| UNK-001 | The Lab 2 task list and evaluation criteria | **Yes — all Lab 2 features** |
| UNK-002 | Whether Lab 2 covers the topics named in the configuration-phase agent definition for `unity-developer` (targeting, radar field of view, Gizmos, projectiles). These came from earlier descriptions and are **not** confirmed requirements | Yes |
| UNK-003 | Required scene content (terrain, targets, camera, lighting) | Yes, for scene setup |
| UNK-004 | Required input method (keyboard/mouse/gamepad, Input System vs legacy) | Yes, if Lab 2 has input |
| UNK-005 | Whether Lab 2 expects aircraft movement, or movement belongs to Lab 3 | Yes, affects architecture boundary |
| UNK-006 | Submission format (project folder, build, video, report) | No for implementation; yes for delivery |
| UNK-007 | Intended meaning of `_R`/`_L` in Blender part names (see DecisionLog D-005) | No for Lab 2 unless control surfaces are animated |

## 4. Dependencies
- Lab 2 specification (UNK-001) is required before any feature work.
- Verified asset pipeline (Blender → FBX → Unity) is in place.

## 5. Technical risks
- R-1: Control-surface naming vs. actual side (D-005) could invert controls later.
- R-2: No UVs on the model; any texture-based visual requirement would need Blender changes (approval needed).
- R-3: Visual correctness of materials not yet checked by a human.

## 6–9. Architecture, steps, acceptance criteria, tests for Lab 2
Not written. They will be derived from the Lab 2 document once supplied, using REQ-1xx / TEST-1xx identifiers.

## Traceability (existing tests)

| Test | Verifies | Requirement | Result |
|---|---|---|---|
| TEST-001 `ModelAsset_IsImported` | FBX imports | REQ-001 | Passed |
| TEST-002 `Model_ContainsAllAircraftMeshesAndNothingElse` | 29 meshes, no extra objects | REQ-001 | Passed |
| TEST-003 `Model_TransformsAreUnrotatedAndUnscaled` | No hidden rotation/scale | REQ-001 | Passed |
| TEST-004 `Model_OverallSizeMatchesBlenderSourceInMetres` | Dimensions equal source | REQ-001 | Passed |
| TEST-005 `Model_NosePointsAlongPositiveZAndWingsStackAlongY` | Orientation | REQ-001 | Passed |
| TEST-006 `Model_LeftAndRightPartsAreMirroredAcrossX` | Symmetry | REQ-001 | Passed |
| TEST-007 `Model_EveryRendererHasOneMaterialPerSubmesh` | Material slots | Engineering check | Passed |
| TEST-008 `Project_UsesUniversalRenderPipelineOnEveryQualityLevel` | Render pipeline | Engineering check (D-002) | Passed |
| TEST-009 `FirstBuildScene_LoadsAndRunsWithoutErrors` (PlayMode) | Scene runs | Engineering check | Passed |
| (script) `verify_fbx.py` | Per-object vertex/polygon/material equality with source | REQ-001 | Passed |
