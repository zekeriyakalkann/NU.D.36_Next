using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NU36.Editor
{
    /// <summary>
    /// Interactive (non-batch) verification run for Rota02. Opens the scene without saving
    /// it, renders the main camera to PNG, frames the Scene view with Gizmos on, runs Play
    /// Mode while sampling the aircraft, radar and roll state, captures the console, then
    /// quits the Editor. Writes everything to ../Logs/visual/.
    /// GUI mode: Unity.exe -projectPath NU.D.36_Unity -executeMethod NU36.Editor.Lab2VisualVerification.Run
    /// State survives the domain reload on entering/leaving Play Mode through SessionState.
    /// </summary>
    [InitializeOnLoad]
    public static class Lab2VisualVerification
    {
        const string ActiveKey = "NU36.Lab2Visual.Active";
        const string PhaseKey = "NU36.Lab2Visual.Phase";
        const string PhaseStartKey = "NU36.Lab2Visual.PhaseStart";

        const float SettleSeconds = 8f;       // let the Editor finish loading layouts
        const float EditModeHoldSeconds = 5f; // time for an external Scene view screenshot
        const float PlaySeconds = 8f;
        const float SampleInterval = 0.2f;
        static readonly float[] CaptureTimes = { 0.3f, 1.5f, 3f, 6f };

        static readonly Vector3 ViewPivot = new Vector3(20f, 20f, 30f);
        static readonly Vector3 ViewDirection = new Vector3(80f, -40f, 80f);
        const float ViewSize = 45f;
        const int CaptureWidth = 1280;
        const int CaptureHeight = 720;

        // Logs must use '.' decimals regardless of the system locale (Turkish uses ',').
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static string OutputDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/visual"));

        static readonly List<string> samples = new List<string>();
        static int nextCapture;
        static float nextSample;

        static Lab2VisualVerification()
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        public static void Run()
        {
            Directory.CreateDirectory(OutputDir);
            foreach (string f in Directory.GetFiles(OutputDir))
                File.Delete(f);
            SessionState.SetBool(ActiveKey, true);
            SetPhase(0);
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        static int Phase => SessionState.GetInt(PhaseKey, 0);
        static float PhaseElapsed => (float)EditorApplication.timeSinceStartup - SessionState.GetFloat(PhaseStartKey, 0f);

        static void SetPhase(int phase)
        {
            SessionState.SetInt(PhaseKey, phase);
            SessionState.SetFloat(PhaseStartKey, (float)EditorApplication.timeSinceStartup);
            Marker($"phase{phase}");
        }

        static void Marker(string name) => File.WriteAllText(Path.Combine(OutputDir, name + ".marker"), System.DateTime.Now.ToString("o"));

        static void OnLog(string message, string stack, LogType type)
        {
            File.AppendAllText(Path.Combine(OutputDir, "console.log"),
                $"[{System.DateTime.Now:HH:mm:ss.fff}] [{type}] [{(Application.isPlaying ? "Play" : "Edit")}] {message}\n");
        }

        static void Tick()
        {
            switch (Phase)
            {
                case 0:
                    if (PhaseElapsed >= SettleSeconds)
                    {
                        PrepareEditMode();
                        SetPhase(1);
                    }
                    break;
                case 1:
                    if (PhaseElapsed >= EditModeHoldSeconds)
                    {
                        SetPhase(2);
                        EditorApplication.EnterPlaymode();
                    }
                    break;
                case 2:
                    if (EditorApplication.isPlaying)
                    {
                        FrameSceneView(focus: true);
                        samples.Clear();
                        chaseSamples.Clear();
                        nextCapture = 0;
                        nextChase = 0;
                        nextSample = 0f;
                        SetPhase(3);
                    }
                    break;
                case 3:
                    SamplePlayMode();
                    break;
                case 4:
                    if (!EditorApplication.isPlaying && PhaseElapsed >= 3f)
                    {
                        Marker("done");
                        SessionState.SetBool(ActiveKey, false);
                        EditorApplication.update -= Tick;
                        Application.logMessageReceived -= OnLog;
                        EditorApplication.Exit(0);
                    }
                    break;
            }
        }

        static void PrepareEditMode()
        {
            var scene = EditorSceneManager.OpenScene(Lab2SceneBuilder.ScenePath, OpenSceneMode.Single);
            var report = new StringBuilder();
            report.AppendLine($"scene={scene.path} loaded={scene.isLoaded} dirty={scene.isDirty}");

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform t = root.transform;
                report.AppendLine($"root '{root.name}' pos={t.position:F3} euler={t.eulerAngles:F3} scale={t.localScale:F3} forward={t.forward:F3}");
            }

            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            int missingScripts = all.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
            int nullMeshes = all.Select(g => g.GetComponent<MeshFilter>()).Count(f => f != null && f.sharedMesh == null);
            int nullMaterials = all.Select(g => g.GetComponent<Renderer>()).Where(r => r != null).Sum(r => r.sharedMaterials.Count(m => m == null));
            int errorShaders = all.Select(g => g.GetComponent<Renderer>()).Where(r => r != null)
                .SelectMany(r => r.sharedMaterials).Count(m => m != null && (m.shader == null || !m.shader.isSupported || m.shader.name.Contains("InternalErrorShader")));
            int missingRefs = 0;
            foreach (Component c in all.SelectMany(g => g.GetComponents<Component>()).Where(c => c != null))
            {
                var so = new SerializedObject(c);
                SerializedProperty p = so.GetIterator();
                while (p.NextVisible(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && !IsDefault(p.objectReferenceEntityIdValue))
                        missingRefs++;
            }
            report.AppendLine($"missingScripts={missingScripts} nullMeshes={nullMeshes} nullMaterials={nullMaterials} errorShaders={errorShaders} missingObjectRefs={missingRefs}");
            File.WriteAllText(Path.Combine(OutputDir, "edit_scene_report.txt"), report.ToString());

            CaptureMainCamera("camera_edit.png");
            FrameSceneView(focus: true);
        }

        // A reference that points at a non-zero id but resolves to null is a missing reference.
        static bool IsDefault<T>(T value) => EqualityComparer<T>.Default.Equals(value, default);

        static void FrameSceneView(bool focus)
        {
            SceneView view = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
            view.in2DMode = false;
            view.drawGizmos = true;
            view.LookAt(ViewPivot, Quaternion.LookRotation(ViewDirection), ViewSize);
            if (focus)
                view.Focus();
            view.Repaint();
        }

        static void SamplePlayMode()
        {
            float t = PhaseElapsed;
            var git = Object.FindAnyObjectByType<NU36.HedefeGit>();
            var yatis = Object.FindAnyObjectByType<NU36.KanatYatisi>();
            var radar = Object.FindAnyObjectByType<NU36.Radar>();

            if (t >= nextSample && git != null && yatis != null && radar != null)
            {
                Transform g = yatis.Gorsel;
                float rightY = g.TransformPoint(new Vector3(4.87f, 0f, 0f)).y;
                float leftY = g.TransformPoint(new Vector3(-4.87f, 0f, 0f)).y;
                float distance = Vector3.Distance(git.transform.position, git.Hedef.position);
                Vector3 toAircraft = git.transform.position - radar.transform.position;
                float radarAngle = Vector3.Angle(radar.transform.forward, toAircraft);
                samples.Add(string.Join(",",
                    t.ToString("F2", Inv), git.transform.position.x.ToString("F3", Inv), git.transform.position.y.ToString("F3", Inv),
                    git.transform.position.z.ToString("F3", Inv), git.transform.eulerAngles.y.ToString("F2", Inv),
                    git.transform.eulerAngles.x.ToString("F2", Inv), yatis.YatisAcisi.ToString("F2", Inv),
                    (rightY - leftY).ToString("F3", Inv), distance.ToString("F3", Inv), git.HedefeVardi ? "1" : "0",
                    radarAngle.ToString("F2", Inv), toAircraft.magnitude.ToString("F2", Inv), radar.HedefGorunur ? "1" : "0"));
                nextSample += SampleInterval;
            }

            if (nextCapture < CaptureTimes.Length && t >= CaptureTimes[nextCapture])
            {
                string label = CaptureTimes[nextCapture].ToString("F1", Inv);
                CaptureMainCamera($"camera_play_{label}s.png");
                Marker($"play_{label}s");
                nextCapture++;
            }

            if (nextChase < ChaseTimes.Length && t >= ChaseTimes[nextChase] && yatis != null)
            {
                CaptureChase(t, yatis);
                nextChase++;
            }

            if (t >= PlaySeconds)
            {
                File.WriteAllText(Path.Combine(OutputDir, "play_samples.csv"),
                    "t,x,y,z,yaw,pitch,bank,rightMinusLeftWingY,distToBalon,arrived,radarAngle,radarDist,radarVisible\n" +
                    string.Join("\n", samples) + "\n");
                // Image coordinates: x to the right, y up (Unity viewport), 1280×720.
                File.WriteAllText(Path.Combine(OutputDir, "chase_samples.csv"),
                    "t,bank,rightTipX,rightTipY,leftTipX,leftTipY\n" + string.Join("\n", chaseSamples) + "\n");
                SetPhase(4);
                EditorApplication.ExitPlaymode();
            }
        }

        static void CaptureMainCamera(string fileName)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                File.WriteAllText(Path.Combine(OutputDir, fileName + ".error.txt"), "No main camera");
                return;
            }
            SavePng(cam, fileName);
        }

        static void SavePng(Camera cam, string fileName)
        {
            Texture2D tex = Render(cam);
            File.WriteAllBytes(Path.Combine(OutputDir, fileName), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // ---------- Wing roll: temporary chase camera (Play Mode only, never saved) ----------

        static readonly float[] ChaseTimes = { 0.05f, 0.15f, 0.25f, 0.35f, 0.5f, 0.8f, 1.5f };
        const float ChaseDistance = 12f;
        const float ChaseHeight = 1.5f;
        const float WingtipOffset = 4.87f; // half wingspan, model local X
        static int nextChase;
        static Camera chaseCamera;
        static readonly List<string> chaseSamples = new List<string>();

        static void CaptureChase(float t, NU36.KanatYatisi yatis)
        {
            if (chaseCamera == null)
            {
                // Created inside Play Mode, so it disappears when Play Mode ends.
                chaseCamera = new GameObject("NU36_VerificationChaseCamera").AddComponent<Camera>();
                chaseCamera.enabled = false; // rendered only on request; does not affect the Game view
                chaseCamera.fieldOfView = 40f;
            }

            Transform root = yatis.transform;
            Vector3 position = root.position - root.forward * ChaseDistance + Vector3.up * ChaseHeight;
            chaseCamera.transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(root.position + root.forward * 6f - position, Vector3.up));

            Vector3 right = chaseCamera.WorldToViewportPoint(yatis.Gorsel.TransformPoint(new Vector3(WingtipOffset, 0f, 0f)));
            Vector3 left = chaseCamera.WorldToViewportPoint(yatis.Gorsel.TransformPoint(new Vector3(-WingtipOffset, 0f, 0f)));
            chaseSamples.Add(string.Join(",", t.ToString("F2", Inv), yatis.YatisAcisi.ToString("F2", Inv),
                (right.x * CaptureWidth).ToString("F1", Inv), (right.y * CaptureHeight).ToString("F1", Inv),
                (left.x * CaptureWidth).ToString("F1", Inv), (left.y * CaptureHeight).ToString("F1", Inv)));

            string label = t.ToString("F2", Inv);
            SavePng(chaseCamera, $"chase_{label}s_bank{yatis.YatisAcisi.ToString("F1", Inv)}.png");
        }

        // ---------- Windscreen close-up: temporary unsaved scene ----------

        static readonly string[] Windscreens = { "NU36_Cockpit_Forward_Windscreen", "NU36_Cockpit_Rear_Windscreen" };
        static readonly Vector3 CloseUpOffset = new Vector3(0.9f, 0.45f, 0.9f); // front-right, above
        const float MaskThreshold = 0.08f;

        /// <summary>
        /// Renders each windscreen close-up three times in a new, never-saved scene: as imported
        /// (A), with the windscreen hidden (B) and with an opaque magenta stand-in material (C).
        /// Pixels where B and C differ are the windscreen. If the glass is transparent, A stays
        /// close to B (what is behind shows through) and far from C (an opaque surface).
        /// Batch mode: -executeMethod NU36.Editor.Lab2VisualVerification.CaptureWindscreenCloseUp
        /// </summary>
        public static void CaptureWindscreenCloseUp()
        {
            Directory.CreateDirectory(OutputDir);
            var report = new StringBuilder();
            bool ok = true;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AircraftModelPostprocessor.AircraftModelPath);
            GameObject aircraft = Object.Instantiate(prefab);
            var light = new GameObject("CloseUpLight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var cam = new GameObject("CloseUpCamera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.55f, 0.85f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.05f;
            var standIn = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.magenta };

            // The first render of a new scene comes out before ambient lighting is ready (darker
            // than later frames), which would bias the A/B comparison; render once and discard.
            cam.transform.SetPositionAndRotation(Vector3.forward * 5f, Quaternion.LookRotation(Vector3.back));
            Object.DestroyImmediate(Render(cam));

            foreach (string name in Windscreens)
            {
                var glass = aircraft.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == name);
                Material[] imported = glass.sharedMaterials;
                Bounds b = glass.bounds;
                cam.transform.position = b.center + CloseUpOffset;
                cam.transform.LookAt(b.center);

                Texture2D a = Render(cam);
                glass.enabled = false;
                Texture2D hidden = Render(cam);
                glass.enabled = true;
                glass.sharedMaterials = imported.Select(_ => standIn).ToArray(); // this instance only
                Texture2D opaque = Render(cam);
                glass.sharedMaterials = imported;

                Color[] pa = a.GetPixels(), pb = hidden.GetPixels(), pc = opaque.GetPixels();
                int mask = 0;
                double dAB = 0, dAC = 0, dBC = 0;
                for (int i = 0; i < pa.Length; i++)
                {
                    float bc = Diff(pb[i], pc[i]);
                    if (bc < MaskThreshold)
                        continue;
                    mask++;
                    dAB += Diff(pa[i], pb[i]);
                    dAC += Diff(pa[i], pc[i]);
                    dBC += bc;
                }
                if (mask == 0)
                {
                    ok = false;
                    report.AppendLine($"{name}: windscreen not visible in the close-up (mask empty)");
                }
                else
                {
                    report.AppendLine(string.Format(Inv,
                        "{0}: maskPixels={1} ({2:F1} % of frame) meanDiff(asImported, hidden)={3:F4} " +
                        "meanDiff(asImported, opaque)={4:F4} meanDiff(hidden, opaque)={5:F4} camera={6:F3} target={7:F3}",
                        name, mask, 100.0 * mask / pa.Length, dAB / mask, dAC / mask, dBC / mask,
                        cam.transform.position, b.center));
                }

                string stem = name.Replace("NU36_Cockpit_", "").ToLowerInvariant();
                File.WriteAllBytes(Path.Combine(OutputDir, $"windscreen_{stem}_A_imported.png"), a.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(OutputDir, $"windscreen_{stem}_B_hidden.png"), hidden.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(OutputDir, $"windscreen_{stem}_C_opaque.png"), opaque.EncodeToPNG());
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(hidden);
                Object.DestroyImmediate(opaque);
            }

            Object.DestroyImmediate(standIn);
            File.WriteAllText(Path.Combine(OutputDir, "windscreen_report.txt"), report.ToString());
            Debug.Log($"[NU36 Windscreen] {report}");
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static float Diff(Color x, Color y) => (Mathf.Abs(x.r - y.r) + Mathf.Abs(x.g - y.g) + Mathf.Abs(x.b - y.b)) / 3f;

        static Texture2D Render(Camera cam)
        {
            var rt = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
            {
                RenderPipeline.SubmitRenderRequest(cam, request);
            }
            else
            {
                RenderTexture previous = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = previous;
            }

            RenderTexture active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = active;

            rt.Release();
            Object.DestroyImmediate(rt);
            return tex;
        }
    }
}
