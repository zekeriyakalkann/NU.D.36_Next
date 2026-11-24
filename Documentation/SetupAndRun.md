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

## Lab 7 night mission (Rota07)
Menu **NU36 → Lab 7 → Create Rota07 Scene** (`-executeMethod NU36.Editor.Lab7SceneBuilder.Create`): copies Bolum3_GriKutu (refuses to overwrite), deletes the grey box, the old Bölüm 2 roots and the old HUD from the copy, creates the Lab 7 textures, materials, meshes and prefabs in `Assets/Lab7/` (kept if they exist; the 2048² colour map takes about a minute) and builds the level from `GeceHaritasi`. Development helper: **NU36 → Lab 7 → Rebuild Rota07 (delete and create)** (`…Lab7SceneBuilder.YenidenKur`) deletes only Rota07 and the generated Lab 7 materials/prefabs/meshes/volume profile and builds again; `YenidenKurDokularla` also regenerates the textures (needed after a change to the map functions).

Player: `-executeMethod NU36.Editor.BuildCommands.BuildWindowsRota07` → `Builds/Windows_Rota07/NU.D.36.exe`. Keys: W/S nose down/up, A/D roll, Q/E rudder (steering on the ground), ↑/↓ throttle (idle = brakes on the ground), F or B brake (held), K easy flight on/off, Space bomb (guided while a target is locked), T next target, O automatic release on/off, M GPS scale, L landing light, Esc pause menu, R restart after the end.

Tests: `-testFilter NU36.Tests.Lab7` (EditMode: `Lab7Tests`, `Lab7UcusFizigiTests`), `-testFilter NU36.Tests.Lab7PlayTests` (PlayMode; measurements → `Logs/lab7/ucus_olcumu.txt`). Images (explicit): `-testFilter NU36.Tests.Lab7GorselKayit` → `Logs/lab7/gorsel/`. Details: `Lab7_Implementation.md`.

## Main menu, gauges and the Bölüm 3 night level (2026-11-10)
- **Main menu:**
  - Build it with **NU36 → Menu → Create AnaMenu Scene** (`-executeMethod NU36.Editor.MenuSceneBuilder.Create`). It refuses to overwrite an existing scene and appends AnaMenu to Build Settings.
  - Game player with the menu first: `-executeMethod NU36.Editor.BuildCommands.BuildWindowsOyun` → `Builds/Windows_Oyun/NU.D.36.exe`.
  - Bölüm 4 first (Lab 8 video): `-executeMethod NU36.Editor.BuildCommands.BuildWindowsBolum4` → `Builds/Windows_Bolum4/NU.D.36.exe`. A first build takes over 10 minutes.
  - In the Editor, open `Assets/Scenes/AnaMenu.unity` and press Play.
- **Gauges and pause menu in Rota07:** **NU36 → Lab 7 → Add Gauges And Pause Menu To Rota07** (`…Lab7Arayuz.Guncelle`). It is idempotent; a rebuild of Rota07 adds them too.
- **Assists in Rota07 (easy flight, brake, targeting, GPS):** **NU36 → Lab 7 → Add Flight Assist, Targeting And GPS To Rota07** (`-executeMethod NU36.Editor.Lab7Yardim.Guncelle`). Idempotent: it re-adds `UcusYardimcisi` and `NisanSistemi` with the code defaults and rebuilds the GPS panel; a rebuild of Rota07 adds them too. Tests: `-testFilter NU36.Tests.Lab7YardimTests` (EditMode) and `-testFilter NU36.Tests.Lab7YardimPlayTests` (PlayMode, measurements → `Logs/lab7/yardim_olcumu.txt`). Details: `Lab7_Implementation.md` §11.
- **Airspeed dial face:**

  ```
  blender --background --factory-startup --disable-autoexec --python Tools/Blender/ui/build_ui_assets.py -- night_speed Exports/Lab7_UI Blender/UI
  ```

  This writes `Exports/Lab7_UI/gece-hiz-kadrani.png`. The Rota07 builder imports it into `Assets/Lab7/UI/` when missing.
- **Night level on the paper map:**
  - After a change to `Bolum3Harita` (e.g. the v0.2 change) or to `GeceHaritasi`, rebuild with `-executeMethod NU36.Editor.Lab7SceneBuilder.YenidenKurDokularla`. This regenerates the colour map and takes about a minute.
  - Run **NU36 → Lab 6 → Create Bolum3_GriKutu Scene** for the grey box after deleting the old one.
- **Tests:**
  - EditMode: `-testFilter "NU36.Tests.MenuTests;NU36.Tests.Lab7Tests"`.
  - PlayMode: `-testFilter "NU36.Tests.MenuPlayTests;NU36.Tests.Lab7PlayTests;NU36.Tests.Lab7Bolum3PlayTests"`.
  - Images (explicit tests): `-testFilter "NU36.Tests.Lab7GorselKayit;NU36.Tests.MenuGorselKayit"`.
- **Paper forms:** `Exports/Lab6/kagit/bolum3_kagit_formlari.html`. Open it in a browser and print it to PDF.
- **Submission zip:** `powershell -ExecutionPolicy Bypass -File Tools/Teslim/teslim_zip.ps1 -OgrenciNo <no> -GddPdf <pdf>` → `Teslim/BMU1421_<no>_H6-7.zip`. Close Unity first.


## Lab 8: Bölüm 4 · Kurşun Dağları (Bolum4_KursunDaglari)
Plan: `Lab8_Plan.md`. State: `Lab8_Implementation.md`.
- **Scene:** menu **NU36 → Lab 8 → Create Bolum4 Scene** or `-executeMethod NU36.Editor.Lab8SceneBuilder.Create`. It refuses to overwrite.
  - After a change to `KursunDaglariHaritasi`, `Lab8Arazi`, `Lab8Yapilar` or `Lab8Varliklar`, rebuild with `-executeMethod NU36.Editor.Lab8SceneBuilder.YenidenKur` (about 1 min).
  - The rebuild deletes only the builder's outputs: the scene, `Assets/Bolum4/{Malzemeler,Prefablar,Meshler,Arazi}` and `GunduzVolume.asset`.
- **Downloads:** `Indirilenler/Bolum4/` (sources in `KAYNAKLAR.txt`). Not imported as a whole; the models in use are copied to `Assets/Bolum4/Modeller` and `Dokular`. Import settings: `Bolum4ModelPostprocessor`.
  - GLB → FBX: `& $blender --background --factory-startup --disable-autoexec --python Tools\Blender\bolum4\glb_to_fbx.py -- <in.glb> <out.fbx>`.
- **Images:** menu **NU36 → Lab 8 → Render Scene Views** (`Logs/lab8/gorsel/sahne_*.png`) and **Render Model Axes** (`yon_*.png`).
- **Tests:**
  - EditMode: `-testFilter "NU36.Tests.Lab8Tests;NU36.Tests.Lab8DusmanTests;NU36.Tests.MenuTests"`.
  - PlayMode: `-testFilter "NU36.Tests.Lab8PlayTests;NU36.Tests.Lab8DusmanPlayTests;NU36.Tests.Lab8Faz3PlayTests;NU36.Tests.MenuPlayTests"` (about 2.5 min).
  - Measurements: `Logs/lab8/harita_olcumu.txt`, `tren_olcumu.txt`, `dusman_olcumu.txt`, `faz3_olcumu.txt`.
  - The whole PlayMode suite takes about 10 min. On a machine short of memory run it in two foreground parts: the earlier labs by class name, then the Lab 8 and menu classes above.
- **Main menu:** `-executeMethod NU36.Editor.Lab8Menu.Guncelle` points AnaMenu to Bölüm 4 (the Bölüm 4 builder also does it).
- **Keys in Bölüm 4:** Ctrl or the left mouse button = machine gun; Space = bomb; T = next target; O = firing assist on/off; M = GPS; K = easy flight; F/B = brake; R = restart after the end; Esc = menu.
- **Asset register:** `NU.D.36_Unity/Assets/VARLIKLAR.md`.

## Lab 8 submission (H8)
- Close Unity, then from the workspace root: `powershell -ExecutionPolicy Bypass -File Tools\Teslim\teslim_h8.ps1 -OgrenciNo <no> -Video <bolum4.mp4>` → `Teslim\BMU1421_<no>_H8.zip` (15–25 min; `-BuildYok` skips the check build).
- The working copy lives in `%TEMP%\nu36_<no>` and is deleted at the end (`-KopyayiSakla` keeps it). Logs: `Logs/lab8/teslim_*.log`, `teslim_dogrulama.txt`.
- Recording the video: `Documentation/Lab8_Video_Yonergesi.md`.

## Lab 9: Bölüm 5 · Kartal Geçidi (Bolum5_KartalGecidi)
Plan: `Lab9_Plan.md`. State: `Lab9_Implementation.md`.
- **Scene:** menu **NU36 → Lab 9 → Create Bolum5 Scene** or `-executeMethod NU36.Editor.Lab9SceneBuilder.Create` (copies Bölüm 4, so Bölüm 4 must exist; refuses to overwrite).
  - After a change to `KartalGecidiHaritasi`, `Lab9Arazi` or `Lab9Varliklar`, rebuild with `-executeMethod NU36.Editor.Lab9SceneBuilder.YenidenKur` (about 1 min). It deletes only the scene and `Assets/Bolum5/{Malzemeler,Prefablar,Uretilen,Arazi}`; `Assets/Bolum5/Dokular` (the downloaded sand) is kept.
  - Bölüm 5 uses Bölüm 4's tank model, enemy aircraft prefab, trees, rocks and terrain layers: after `Lab8SceneBuilder.YenidenKur` (new GUIDs) rebuild Bölüm 5 too.
- **Downloads:** `Indirilenler/Bolum5/` (`KAYNAKLAR.txt`): Poly Haven `coast_sand_01`, copied to `Assets/Bolum5/Dokular`.
- **Tests:**
  - EditMode: `-testFilter "NU36.Tests.Lab9Tests;NU36.Tests.MenuTests"`.
  - PlayMode: `-testFilter "NU36.Tests.Lab9PlayTests;NU36.Tests.Lab9TurTests"` (about 2 min).
  - Images (explicit): `-testFilter NU36.Tests.Lab9GorselKayit` → `Logs/lab9/gorsel/`.
  - Measurements: `Logs/lab9/harita_olcumu.txt`, `oyun_olcumu.txt`, `tur_olcumu.txt`.
- **Player:** `-executeMethod NU36.Editor.BuildCommands.BuildWindowsBolum5` → `Builds/Windows_Bolum5/NU.D.36.exe` (starts in Bölüm 5); `BuildWindowsOyun` → `Builds/Windows_Oyun/` (menu first; GÖREVE BAŞLA opens Bölüm 5).
- **Main menu:** `-executeMethod NU36.Editor.Lab9Menu.Guncelle` (the Bölüm 5 builder also does it).
- **Keys in Bölüm 5:** as Bölüm 4 — W/S nose, A/D turn, ↑/↓ throttle, Ctrl or left mouse = machine gun, Space = bomb, T = next target, O = firing assist, M = GPS, K = flight mode, F/B = brake, R = restart after the end, Esc = menu.
