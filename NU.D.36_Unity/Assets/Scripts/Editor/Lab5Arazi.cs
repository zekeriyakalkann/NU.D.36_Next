using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NU36.Editor
{
    /// <summary>Lab 5 diagnostics: writes a coarse height map of the terrain function (used to choose the Rota05 layout).</summary>
    public static class Lab5Arazi
    {
        [MenuItem("NU36/Lab 5/Write Terrain Height Map")]
        public static void HaritaRaporu()
        {
            const int adim = 200, yaricap = 3000;
            var sb = new StringBuilder();
            sb.AppendLine($"AraziUretici.Yukseklik, step {adim} m, x across (-{yaricap}..{yaricap}), z rows from +{yaricap} (top) to -{yaricap}");
            sb.Append("z\\x  ");
            for (int x = -yaricap; x <= yaricap; x += adim) sb.Append($"{x,6}");
            sb.AppendLine();
            for (int z = yaricap; z >= -yaricap; z -= adim)
            {
                sb.Append($"{z,5}");
                for (int x = -yaricap; x <= yaricap; x += adim)
                    sb.Append($"{AraziUretici.Yukseklik(x, z),6:F0}");
                sb.AppendLine();
            }
            string yol = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab5/arazi_haritasi.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(yol));
            File.WriteAllText(yol, sb.ToString());
            Debug.Log("[NU36 Lab5] Height map written to " + yol);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
    }
}
