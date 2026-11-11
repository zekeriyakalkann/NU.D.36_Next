using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8 (Hafta 8) submission (plan §10, REQ-830): the worksheet asks for Assets, ProjectSettings, Packages and the
    /// .meta files, with the unused assets deleted. The project is cumulative (Labs 2–7 live in it too), so nothing is
    /// deleted here: <see cref="Listele"/> writes the list of what Bölüm 4 needs — the dependencies of AnaMenu and
    /// Bolum4_KursunDaglari, all scripts, the render-pipeline settings, the input actions and VARLIKLAR.md — and
    /// Tools/Teslim/teslim_h8.ps1 copies only those into a separate folder. <see cref="Dogrula"/> runs inside that copy:
    /// it sets the build list to the two scenes, looks for missing scripts and broken references, and (optionally)
    /// builds the player.
    /// Batch mode: -executeMethod NU36.Editor.TeslimKopyasi.Listele -teslimListe &lt;file&gt;
    ///             -executeMethod NU36.Editor.TeslimKopyasi.Dogrula [-teslimBuild &lt;exe&gt;]
    /// </summary>
    public static class TeslimKopyasi
    {
        public static readonly string[] Sahneler = { "Assets/Scenes/AnaMenu.unity", "Assets/Scenes/Bolum4_KursunDaglari.unity" };
        static readonly string[] HepsiKlasorler = { "Assets/Scripts", "Assets/Settings" };
        static readonly string[] Tekil = { "Assets/InputSystem_Actions.inputactions", "Assets/VARLIKLAR.md" };

        static string Arguman(string ad)
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, ad);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }

        /// <summary>Asset paths (no .meta) the submission copy needs, sorted.</summary>
        public static List<string> Dosyalar()
        {
            var s = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string d in AssetDatabase.GetDependencies(Sahneler, true))
                if (d.StartsWith("Assets/")) s.Add(d);
            string proje = Path.GetDirectoryName(Application.dataPath);
            foreach (string k in HepsiKlasorler)
                foreach (string f in Directory.GetFiles(Path.Combine(proje, k), "*", SearchOption.AllDirectories))
                    if (!f.EndsWith(".meta")) s.Add(f.Substring(proje.Length + 1).Replace('\\', '/'));
            foreach (string t in Tekil)
                if (File.Exists(Path.Combine(proje, t))) s.Add(t);
            return s.ToList();
        }

        [MenuItem("NU36/Lab 8/Write Submission File List")]
        public static void Listele()
        {
            int kod = 0;
            try
            {
                string hedef = Arguman("-teslimListe") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab8/teslim_listesi.txt"));
                List<string> l = Dosyalar();
                Directory.CreateDirectory(Path.GetDirectoryName(hedef));
                File.WriteAllLines(hedef, l);
                // Baseline: GUIDs already unresolved in this project (template leftovers, e.g. the URP sample settings);
                // the copy is only blamed for what resolves here and not there.
                string proje = Path.GetDirectoryName(Application.dataPath);
                var taban = Cozulmeyenler(l.Select(d => Path.Combine(proje, d)));
                string tabanYolu = Arguman("-teslimTaban") ?? Path.Combine(Path.GetDirectoryName(hedef), "teslim_taban_guid.txt");
                File.WriteAllLines(tabanYolu, taban.Select(x => x.guid).Distinct());
                Debug.Log($"[NU36 Teslim] {l.Count} files listed → {hedef}; {taban.Select(x => x.guid).Distinct().Count()} GUIDs already unresolved in the project → {tabanYolu}");
            }
            catch (Exception e) { Debug.LogError($"[NU36 Teslim] {e}"); kod = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(kod);
        }

        /// <summary>Missing scripts and broken object references in the open scenes (count, with a short report).</summary>
        public static int KopukSay(List<string> rapor)
        {
            int n = 0;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var sahne = EditorSceneManager.GetSceneAt(i);
                foreach (GameObject kok in sahne.GetRootGameObjects())
                    foreach (Transform t in kok.GetComponentsInChildren<Transform>(true))
                    {
                        int eksik = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                        if (eksik > 0) { n += eksik; rapor.Add($"{sahne.name}: {t.name}: {eksik} missing script(s)"); }
                    }
            }
            return n;
        }

        static readonly System.Text.RegularExpressions.Regex GuidDeseni = new System.Text.RegularExpressions.Regex("guid: ([0-9a-f]{32})");

        /// <summary>
        /// Every asset GUID written in the copy's scenes, prefabs, materials and assets must resolve to a file in the copy
        /// (the risk of a trimmed copy: something referenced was not copied). Unity built-ins (GUIDs starting with 16 zeros)
        /// and package assets are skipped.
        /// </summary>
        public static int CozulmeyenGuidSay(List<string> rapor)
        {
            string proje = Path.GetDirectoryName(Application.dataPath);
            string tabanYolu = Arguman("-teslimTaban");
            var taban = tabanYolu != null && File.Exists(tabanYolu) ? new HashSet<string>(File.ReadAllLines(tabanYolu)) : new HashSet<string>();
            var hepsi = Cozulmeyenler(Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories));
            int eski = hepsi.Count(x => taban.Contains(x.guid));
            var yeni = hepsi.Where(x => !taban.Contains(x.guid)).ToList();
            rapor.Add($"unresolved GUIDs already unresolved in the full project (ignored): {eski}");
            foreach (var x in yeni.Take(20))
                rapor.Add($"unresolved GUID {x.guid} in {x.dosya.Substring(proje.Length + 1)}");
            return yeni.Count;
        }

        /// <summary>(GUID, file) pairs whose GUID does not resolve to a file in this project.</summary>
        static List<(string guid, string dosya)> Cozulmeyenler(IEnumerable<string> dosyalar)
        {
            string proje = Path.GetDirectoryName(Application.dataPath);
            var sonuc = new List<(string, string)>();
            var gorulen = new HashSet<string>();
            foreach (string f in dosyalar)
            {
                string uz = Path.GetExtension(f).ToLowerInvariant();
                if (uz != ".unity" && uz != ".prefab" && uz != ".mat" && uz != ".asset" && uz != ".terrainlayer" && uz != ".inputactions") continue;
                string metin = File.ReadAllText(f);
                foreach (System.Text.RegularExpressions.Match m in GuidDeseni.Matches(metin))
                {
                    string g = m.Groups[1].Value;
                    if (g.StartsWith("0000000000000000") || !gorulen.Add(g)) continue;
                    string yol = AssetDatabase.GUIDToAssetPath(g);
                    if (string.IsNullOrEmpty(yol) || (!yol.StartsWith("Packages/") && !File.Exists(Path.Combine(proje, yol)) && !Directory.Exists(Path.Combine(proje, yol))))
                        sonuc.Add((g, f));
                }
            }
            return sonuc;
        }

        /// <summary>Runs inside the submission copy: build list, reference check, optional player build. Exit 0 = good.</summary>
        public static void Dogrula()
        {
            int kod = 0;
            var rapor = new List<string>();
            try
            {
                string[] var = Sahneler.Where(p => File.Exists(Path.Combine(Path.GetDirectoryName(Application.dataPath), p))).ToArray();
                if (var.Length != Sahneler.Length)
                    throw new Exception("Scene missing in the copy: " + string.Join(", ", Sahneler.Except(var)));
                EditorBuildSettings.scenes = var.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
                AssetDatabase.SaveAssets();

                int kopuk = 0;
                foreach (string p in var)
                {
                    EditorSceneManager.OpenScene(p, OpenSceneMode.Single);
                    kopuk += KopukSay(rapor);
                }
                int guid = CozulmeyenGuidSay(rapor);
                kopuk += guid;
                rapor.Insert(0, $"build list: {string.Join(", ", var)}; missing scripts and unresolved asset references: {kopuk} (GUIDs {guid})");
                if (kopuk > 0) kod = 1;

                string exe = Arguman("-teslimBuild");
                if (kod == 0 && !string.IsNullOrEmpty(exe))
                {
                    var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = var,
                        locationPathName = exe,
                        target = BuildTarget.StandaloneWindows64,
                        options = BuildOptions.None,
                    });
                    rapor.Add($"build: {r.summary.result}, errors {r.summary.totalErrors}, warnings {r.summary.totalWarnings}, {r.summary.totalSize / 1048576} MB → {exe}");
                    if (r.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) kod = 1;
                }
            }
            catch (Exception e) { rapor.Add(e.ToString()); kod = 1; }
            foreach (string s in rapor) Debug.Log($"[NU36 Teslim] {s}");
            File.WriteAllLines(Path.Combine(Path.GetDirectoryName(Application.dataPath), "teslim_dogrulama.txt"), rapor);
            if (Application.isBatchMode) EditorApplication.Exit(kod);
        }
    }
}
