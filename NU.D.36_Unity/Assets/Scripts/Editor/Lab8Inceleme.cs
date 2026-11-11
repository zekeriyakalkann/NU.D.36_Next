using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>
    /// Lab 8: writes the imported Bölüm 4 models' hierarchy, local bounds, renderers and materials to
    /// <c>Logs/lab8/model_inceleme.txt</c> (used to choose scales, axes and the tank's turret).
    /// Batch mode: -executeMethod NU36.Editor.Lab8Inceleme.Dok
    /// </summary>
    public static class Lab8Inceleme
    {
        [MenuItem("NU36/Lab 8/Dump Bolum4 Models")]
        public static void Dok()
        {
            AssetDatabase.Refresh();
            var sb = new StringBuilder();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Bolum4/Modeller" }))
            {
                string yol = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(yol);
                sb.AppendLine($"== {yol}");
                Bounds b = Sinirlar(go);
                sb.AppendLine($"   bounds centre {b.center:F2} size {b.size:F2}");
                Yaz(go.transform, 1, sb);
            }
            string dosya = Path.Combine(Path.GetDirectoryName(Application.dataPath), "..", "Logs", "lab8", "model_inceleme.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(dosya));
            File.WriteAllText(dosya, sb.ToString());
            Debug.Log("[NU36 Lab8] " + dosya);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        public static Bounds Sinirlar(GameObject go)
        {
            bool ilk = true;
            var b = new Bounds();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) Ekle(ref b, ref ilk, go.transform, mf.transform, mf.sharedMesh.bounds);
            foreach (var sm in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (sm.sharedMesh != null) Ekle(ref b, ref ilk, go.transform, sm.transform, sm.sharedMesh.bounds);
            return b;
        }

        static void Ekle(ref Bounds b, ref bool ilk, Transform kok, Transform t, Bounds yerel)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 k = yerel.center + Vector3.Scale(yerel.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = kok.InverseTransformPoint(t.TransformPoint(k));
                if (ilk) { b = new Bounds(p, Vector3.zero); ilk = false; }
                else b.Encapsulate(p);
            }
        }

        static void Yaz(Transform t, int d, StringBuilder sb)
        {
            string ek = "";
            var mf = t.GetComponent<MeshFilter>();
            var mr = t.GetComponent<Renderer>();
            if (mf != null && mf.sharedMesh != null)
                ek += $" mesh {mf.sharedMesh.name} v{mf.sharedMesh.vertexCount} sub{mf.sharedMesh.subMeshCount} b{mf.sharedMesh.bounds.size:F2}";
            if (t.TryGetComponent(out SkinnedMeshRenderer s) && s.sharedMesh != null)
                ek += $" skinned {s.sharedMesh.name} v{s.sharedMesh.vertexCount} bones{s.bones.Length}";
            if (mr != null)
                ek += " mats[" + string.Join(", ", mr.sharedMaterials.Select(m => m == null ? "null" : $"{m.name}:{(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString("F2") : "-")}{(m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? "+tex" : "")}")) + "]";
            sb.AppendLine($"{new string(' ', d * 2)}{t.name} pos{t.localPosition:F2} rot{t.localEulerAngles:F0} scl{t.localScale:F2}{ek}");
            foreach (Transform c in t)
                Yaz(c, d + 1, sb);
        }
    }
}
