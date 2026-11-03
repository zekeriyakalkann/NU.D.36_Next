# Environment and Technical Validation Report

Date: 2026-10-19. All evidence files referenced below are in `Logs/`.

## 1. Workspace audit (Phase A)

| Item | Result |
|---|---|
| Workspace | `C:\Users\Ceng Zeki\Desktop\Personel\Game Folder\NU.D.36_Next` |
| `CLAUDE.md`, `.claude/settings.local.json`, 3 agent definitions | Present, unchanged |
| Git | Not initialized (by design) |
| Pre-existing Unity project | None |
| `Blender/NU.D.36_project_FINAL.blend` | 255 985 bytes, modified 2026-10-06 11:52:26, SHA256 `A09923471FDA1D5F857296E6F268EC9968C36CEE1E533EA3D9DDEDF84A32277E` |
| `Blender/NU.D.36_project_FINAL.blend1` | 255 365 bytes, modified 2026-10-05 21:02:18, SHA256 `9758FD439B387A5200534890F2310DFFED1D655BAF127C189C617029A1F54B44` |

Protected locations (`C:\AI\NU.D.36_Git`, `C:\AI\_PermTest`, `C:\AI\NU.D.36\...`) were not accessed.

## 2. Tools

| Tool | Version | Path |
|---|---|---|
| Blender | 5.2.2 LTS (bundled Python 3.13) | `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe` |
| Unity Editor | 6000.6.4f1 (12bfff696524) | `C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe` |
| Unity Hub | 3.22.2 | `C:\Program Files\Unity Hub` |
| Unity build modules | Windows Standalone, WebGL | |
| System Python | 3.15.0 / 3.13.16 | `py` launcher (not used for Blender) |

## 3. Blender source inspection (Phase B)

Script: `Tools/Blender/inspect_scene.py` → `Logs/blender_inspect.json`.
Blender was run with `--background --factory-startup --disable-autoexec`; the file was never saved.

- Units: metric, scale 1.0, metres. Single scene, no actions/animation, no linked libraries, no image textures.
- Collections: `NU.D.36` (29 meshes), `NU36_References` (11 empties), `Studio` (2 cameras, 2 lights).
- All 29 meshes: unparented, no modifiers, identity rotation/scale, **no UV layers**. 6 922 vertices / 6 308 polygons total.
- Movable parts (ailerons, elevators, rudder, propeller, wheels) have object origins away from the world origin (likely hinge/axle points; not verified visually).
- 14 materials, all plain Principled BSDF colours (no textures). `NU36_MAT_Glass_Transparent` has alpha 0.15.
- Orientation: nose at Blender **-Y** (`REF_Nose` y = -3.65, propeller y = -3.75), up = +Z.
- **Naming note:** `*_R` parts are on Blender +X. With the nose at -Y this is the *pilot's left*. See DecisionLog D-005.

## 4. FBX export

Script: `Tools/Blender/export_fbx.py` → `Exports/NU_D_36.fbx` (276 588 bytes). Exit code 0, result `FINISHED`.

| Setting | Value | Reason |
|---|---|---|
| Objects | MESH objects of collection `NU.D.36` only | Excludes reference empties, cameras, lights |
| Axis | forward `-Z`, up `Y`, `bake_space_transform` on | Nose → Unity +Z, no -90° root rotation |
| Scale | `global_scale 1`, `FBX_SCALE_ALL`, apply unit scale | 1 Blender m = 1 Unity unit |
| Smoothing | `FACE` | Standard for Unity import |
| Tangent space | off | No UVs, tangents cannot be computed |
| Animation | off | No actions in file |
| Textures | none embedded | No textures exist |

Verification:
- `Tools/Blender/verify_fbx.py` re-imported the FBX into an empty scene: 29 meshes, **vertex, polygon counts and material slots identical** to the source for every object (`Logs/fbx_verify.json`).
- Source `.blend` SHA256 and timestamp identical before and after all Blender runs.

## 5. Unity project (Phases C–D)

- Created `NU.D.36_Unity/` with `-batchmode -createProject -cloneFromTemplate com.unity.template.urp-blank-17.2.1.tgz` (exit 0, `Logs/unity_create.log`).
- Packages from template: URP 17.6.0, Input System 1.20.0, Test Framework 1.8.0, uGUI, Timeline, AI Navigation, Visual Scripting, IDE integrations. Nothing added.
- URP is assigned per quality level (Mobile, PC); `GraphicsSettings.defaultRenderPipeline` is empty by template design.
- Licensing: entitlement resolved in batch mode. The log contains `Code 10 while verifying Licensing Client signature` and `Access token is unavailable`; neither blocked any operation.
- FBX copied (hash-verified) to `Assets/Models/NU_D_36.fbx`. Import settings are applied by `Assets/Scripts/Editor/AircraftModelPostprocessor.cs`.

### Verification results

| Check | Status | Evidence |
|---|---|---|
| Project creation | VERIFIED | `Logs/unity_create.log` |
| C# compilation (template + project scripts) | VERIFIED, no CS errors/warnings | `Logs/unity_editmode.log` |
| FBX import: 29 meshes, no cameras/lights/animator | VERIFIED | EditMode tests |
| Size 9.740 × 2.440 × 7.730 m, nose +Z, upper wing above lower | VERIFIED | EditMode tests |
| Identity rotation and scale on all transforms | VERIFIED | EditMode tests |
| L/R parts mirrored; `_R` parts at Unity -X | VERIFIED | EditMode test output |
| Materials: URP/Lit, glass in transparent queue (3000) | VERIFIED (data only) | EditMode test output |
| URP on every quality level | VERIFIED | EditMode tests |
| SampleScene loads in Play Mode and runs 10 frames with no errors | VERIFIED | `Logs/TestResults_PlayMode.xml` |
| Windows 64-bit player build | VERIFIED: build succeeded | `Logs/unity_build.log`, `Builds/Windows/NU.D.36.exe` |
| Built player runs | NOT TESTED | |
| Visual appearance of the aircraft (shading, colours, transparency) | NOT TESTED: needs a human look in the Editor | |
| Pivot positions of control surfaces as hinge points | NOT TESTED | |

Results: EditMode 8/8 passed, PlayMode 1/1 passed (`Logs/TestResults_*.xml`).

## 6. Missing or limiting items

- No C# IDE tooling (no .NET SDK on PATH, no C# extension in VS Code). Not needed for batch compilation; affects editing comfort only.
- Batch mode cannot run while the Editor has the project open; close the Editor before running the scripts in `SetupAndRun.md`.
- The model has no UVs, so texture-based materials would need UV unwrapping in Blender (geometry change → requires approval).
