# Setup and Run

Run all commands from the workspace root in PowerShell. **Close the Unity Editor before any batch-mode command.**

```powershell
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$unity   = 'C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe'
$ws      = 'C:\Users\Ceng Zeki\Desktop\Personel\Game Folder\NU.D.36_Next'
```

## Open the project
Unity Hub → Add → `NU.D.36_Unity` (Editor 6000.6.4f1).

## Re-export the aircraft from Blender
Never saves the `.blend`. Afterwards copy `Exports\NU_D_36.fbx` to `NU.D.36_Unity\Assets\Models\`.

```powershell
& $blender --background --factory-startup --disable-autoexec "Blender\NU.D.36_project_FINAL.blend" --python "Tools\Blender\export_fbx.py" -- "Exports\NU_D_36.fbx"
& $blender --background --factory-startup --python "Tools\Blender\verify_fbx.py" -- "Exports\NU_D_36.fbx" "Logs\fbx_verify.json"
```

## Recreate the Lab 2 scene
Menu **NU36 → Lab 2 → Create Rota02 Scene**, or in batch mode (refuses to overwrite an existing `Rota02`):

```powershell
Start-Process $unity -Wait -NoNewWindow -ArgumentList '-batchmode',
  '-projectPath',"`"$ws\NU.D.36_Unity`"",'-executeMethod','NU36.Editor.Lab2SceneBuilder.CreateRota02',
  '-logFile',"`"$ws\Logs\unity_lab2_scene.log`""
```

To update only the Rota02 camera to the values in `Lab2SceneBuilder` (currently D-023), use the menu **NU36 → Lab 2 → Apply Camera To Rota02** or `-executeMethod NU36.Editor.Lab2SceneBuilder.ApplyCameraToRota02`.

## Lab 3 scene (Rota03)
Menu **NU36 → Lab 3 → Create Rota03 Scene**, or `-executeMethod NU36.Editor.Lab3SceneBuilder.CreateRota03`. This adds the `Ucus` action map if it is missing, copies Rota02 to Rota03 (it refuses to overwrite an existing Rota03), wires the Lab 3 components and appends Rota03 to Build Settings. Rota02 is never modified.

Controls in Play Mode (Game view focused): W/S pitch (W = nose down), D/A roll, E/Q yaw, ↑/↓ throttle.

Lab 3 player build, starting in Rota03 (output `Builds\Windows_Rota03\NU.D.36.exe`):

```powershell
Start-Process $unity -Wait -NoNewWindow -ArgumentList '-batchmode',
  '-projectPath',"`"$ws\NU.D.36_Unity`"",'-executeMethod','NU36.Editor.BuildCommands.BuildWindowsRota03',
  '-logFile',"`"$ws\Logs\unity_build_rota03.log`""
```

The Lab 3 experiments run as part of the PlayMode and EditMode suites (`Lab3Deneyler`, `Lab3UcusKontrolTests`, `Lab3KameraTests`) and write their measurements to `Logs\lab3\`.

## Run tests
In the Editor: Window → General → Test Runner. In batch mode:

```powershell
foreach ($p in 'EditMode','PlayMode') {
  Start-Process $unity -Wait -NoNewWindow -ArgumentList '-batchmode',
    '-projectPath',"`"$ws\NU.D.36_Unity`"",'-runTests','-testPlatform',$p,
    '-testResults',"`"$ws\Logs\TestResults_$p.xml`"",'-logFile',"`"$ws\Logs\unity_$p.log`""
}
```
Exit code 0 = all passed, 2 = test failures.

## Visual verification (Editor GUI, not batch mode)
Opens the Editor, runs Rota02 in Play Mode, writes evidence to `Logs\visual\` and quits. Close any open Editor first. Window screenshots were taken while it runs by polling the `phase*.marker` files in `Logs\visual\` and calling `Save-WindowCapture -ProcessId <pid> -Path <png>` from `Tools\Verification\Capture-Window.ps1`. That polling loop is not saved as a script.

```powershell
Start-Process $unity -ArgumentList '-projectPath',"`"$ws\NU.D.36_Unity`"",
  '-executeMethod','NU36.Editor.Lab2VisualVerification.Run','-logFile',"`"$ws\Logs\unity_visual_editor.log`""
```

Windscreen transparency close-up (batch mode; renders in a new unsaved scene and writes `Logs\visual\windscreen_*`):

```powershell
Start-Process $unity -Wait -NoNewWindow -ArgumentList '-batchmode',
  '-projectPath',"`"$ws\NU.D.36_Unity`"",'-executeMethod','NU36.Editor.Lab2VisualVerification.CaptureWindscreenCloseUp',
  '-logFile',"`"$ws\Logs\unity_windscreen.log`""
```

Note: `Run` clears `Logs\visual\` first, so run the windscreen close-up after it.

## Build (Windows 64-bit)
```powershell
Start-Process $unity -Wait -NoNewWindow -ArgumentList '-batchmode',
  '-projectPath',"`"$ws\NU.D.36_Unity`"",'-executeMethod','NU36.Editor.BuildCommands.BuildWindows',
  '-logFile',"`"$ws\Logs\unity_build.log`""
```
Output: `Builds\Windows\NU.D.36.exe`.

## Layout
```
Blender/          protected source copy (.blend, .blend1) – never saved by tools
Tools/Blender/    inspect_scene.py, export_fbx.py, verify_fbx.py
Exports/          FBX produced from Blender
NU.D.36_Unity/    Unity project (Assets/Models, Scripts/Editor, Tests/EditMode, Tests/PlayMode)
Builds/           player builds
Logs/             tool logs and test results
Documentation/    this documentation
```

## Rota03 terrain
Menu **NU36 → Lab 3 → Add Terrain To Rota03** (or `-executeMethod NU36.Editor.Lab3TerrainSceneBuilder.Apply` in batch mode; re-runnable, touches only Rota03). The terrain is generated at runtime when Rota03 starts, no button is needed. Details: `Lab3_Terrain_Implementation.md`.

## Lab 5 scene (Rota05)
Menu **NU36 → Lab 5 → Create Rota05 Scene**, or `-executeMethod NU36.Editor.Lab5SceneBuilder.Create`. It sets up the five layers and the collision matrix (**NU36 → Lab 5 → Set Up Layers And Physics** does only that), creates the prefabs in `Assets/Lab5/` (kept if they exist), copies Rota04Hud to Rota05 (refuses to overwrite an existing Rota05) and appends it to Build Settings. Details: `Lab5_Implementation.md`.

Lab 5 player (Rota05 first): `-executeMethod NU36.Editor.BuildCommands.BuildWindowsRota05` → `Builds/Windows_Rota05/NU.D.36.exe`. Keys: W/S pitch, A/D roll, Q/E yaw, ↑/↓ throttle, Space bomb, R restart after the end.

Viewpoint images of Rota05 (explicit test, not part of the normal run): `-runTests -testPlatform PlayMode -testFilter Lab5GorselKayit` → `Logs/lab5/gorsel/`. Terrain height map used for the layout: **NU36 → Lab 5 → Write Terrain Height Map** → `Logs/lab5/arazi_haritasi.txt`.

## Lab 6 grey box (Bolum3_GriKutu)
Menu **NU36 → Lab 6 → Create Bolum3_GriKutu Scene** (`-executeMethod NU36.Editor.Lab6SceneBuilder.Create`): copies Rota05 (refuses to overwrite), builds the grey box from `Bolum3Harita`, appends it to Build Settings and writes `Exports/Lab6/bolum3_harita.json`. Player: `-executeMethod NU36.Editor.BuildCommands.BuildWindowsGriKutu` → `Builds/Windows_Bolum3_GriKutu/`.

Printable paper map (after the JSON exists):
```powershell
& $blender --background --factory-startup --disable-autoexec --python Tools\Blender\bolum3\render_harita.py -- Exports\Lab6\bolum3_harita.json Exports\Lab6\bolum3_harita_v0.1.png
# empty grid: add  bos  after the output path
```
Grey box images: `-runTests -testPlatform PlayMode -testFilter Lab6GorselKayit` → `Logs/lab6/gorsel/`. Design and results: `Lab6_Bolum3_Tasarim.md`; GDD draft: `Lab6_GDD_Taslagi.md`.
