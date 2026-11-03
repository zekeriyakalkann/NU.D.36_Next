using System.Globalization;
using System.IO;
using UnityEngine;

namespace NU36.Tests
{
    /// <summary>Writes Lab 3 measurement files to Logs/lab3 (outside the Unity project).</summary>
    static class Lab3Kayit
    {
        public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Klasor
        {
            get
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab3"));
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static void Yaz(string dosya, string icerik) => File.WriteAllText(Path.Combine(Klasor, dosya), icerik);

        public static string F(float v, string format = "F4") => v.ToString(format, Inv);
    }
}
