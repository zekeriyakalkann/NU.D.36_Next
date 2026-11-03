using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace NU36.Tests
{
    /// <summary>
    /// Measurement probe for the flight model (no judgement): steps UcusKontrol with fixed dt and writes the numbers to
    /// Logs/flight/probe_<name>.txt (lab3_defaults = parameters at zero, rota04hud_model = the values of Rota04Hud). It only uses the public API that exists since Lab 3, so the same probe can be run before and
    /// after a change to see what changed. The Lab 3 defaults must keep producing the numbers recorded in baseline_before.
    /// </summary>
    public class FlightProbeTests
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        GameObject go;

        [TearDown]
        public void Destroy()
        {
            if (go != null)
                Object.DestroyImmediate(go);
        }

        bool model;

        UcusKontrol Create()
        {
            go = new GameObject("Ucak");
            var u = go.AddComponent<UcusKontrol>();
            if (model)   // the flight model as set in Rota04Hud
            {
                u.HizIvmesi = 6f; u.EgimHizKaybi = 12f; u.YatisDengeleme = 1.5f; u.EnFazlaYatis = 65f; u.YatisDonusu = 1f; u.EnFazlaDonusHizi = 45f;
            }
            return u;
        }

        static float Bank(Transform t) => Mathf.Atan2(-t.right.y, t.up.y) * Mathf.Rad2Deg;   // + = right wing down
        static float Pitch(Transform t) => Mathf.Asin(Mathf.Clamp(t.forward.y, -1f, 1f)) * Mathf.Rad2Deg;   // + = nose up
        static float Heading(Transform t) => t.eulerAngles.y;

        static void Run(UcusKontrol u, float seconds, int fps = 60)
        {
            int n = Mathf.RoundToInt(seconds * fps);
            for (int i = 0; i < n; i++)
                u.Adim(1f / fps);
        }

        [TestCase(false, "lab3_defaults")]
        [TestCase(true, "rota04hud_model")]
        public void WriteFlightProbe(bool withModel, string name)
        {
            model = withModel;
            var sb = new StringBuilder();
            void L(string s) => sb.AppendLine(s);
            string F(float v) => v.ToString("F2", Inv);

            // 1. speed response to the throttle
            {
                UcusKontrol u = Create();
                u.Adim(0.001f);
                u.GazGirdisi = 1f;
                float t = 0f;
                while (u.Hiz < u.EnCokHiz - 0.01f && t < 20f) { u.Adim(1f / 60f); t += 1f / 60f; }
                L($"throttle 0.5 -> 1 held: speed 25 -> {F(u.Hiz)} m/s in {F(t)} s (mean acceleration {F((u.Hiz - 25f) / Mathf.Max(t, 1e-3f))} m/s2)");
                u.GazGirdisi = -1f;
                t = 0f;
                float v0 = u.Hiz;
                while (u.Hiz > u.EnAzHiz + 0.01f && t < 20f) { u.Adim(1f / 60f); t += 1f / 60f; }
                L($"throttle 1 -> 0 held: speed {F(v0)} -> {F(u.Hiz)} m/s in {F(t)} s (mean deceleration {F((v0 - u.Hiz) / Mathf.Max(t, 1e-3f))} m/s2)");
                Object.DestroyImmediate(go);
            }
            // 2. bank 0.5 s, release, 3 s: does the bank stay, does the heading change
            {
                UcusKontrol u = Create();
                u.Yatis = 1f;
                Run(u, 0.5f);
                u.Yatis = 0f;
                Run(u, 1f);
                float bank1 = Bank(u.transform), h1 = Heading(u.transform);
                Run(u, 3f);
                L($"roll right 0.5 s then released: bank {F(bank1)} deg after 1 s, {F(Bank(u.transform))} deg after 4 s (leveling: {(Mathf.Abs(Bank(u.transform)) < Mathf.Abs(bank1) - 1f ? "yes" : "no")}); heading change while banked 3 s: {F(Mathf.DeltaAngle(h1, Heading(u.transform)))} deg");
                Object.DestroyImmediate(go);
            }
            // 3. rudder (yaw key)
            {
                UcusKontrol u = Create();
                u.Sapma = 1f;
                Run(u, 2f);
                L($"yaw key 2 s: heading change {F(Mathf.DeltaAngle(0f, Heading(u.transform)))} deg, bank {F(Bank(u.transform))} deg");
                Object.DestroyImmediate(go);
            }
            // 4. pitch 1 s and the tail after release
            {
                UcusKontrol u = Create();
                u.Yunuslama = 1f;
                Run(u, 1f);
                float p1 = Pitch(u.transform);
                u.Yunuslama = 0f;
                Run(u, 1f);
                L($"pitch key +1 (W) 1 s: pitch {F(p1)} deg (negative = nose down); then released 1 s: pitch {F(Pitch(u.transform))} deg (continued {F(Pitch(u.transform) - p1)} deg)");
                Object.DestroyImmediate(go);
            }
            // 5. loop: pitch up held 7 s; does the roll stay level
            {
                UcusKontrol u = Create();
                u.Yunuslama = -1f;
                float maxRoll = 0f, minUp = 1f;
                for (int i = 0; i < 7 * 60; i++)
                {
                    u.Adim(1f / 60f);
                    maxRoll = Mathf.Max(maxRoll, Mathf.Abs(u.transform.right.y));
                    minUp = Mathf.Min(minUp, u.transform.up.y);
                }
                L($"pitch up (S) held 7 s: max |right.y| {F(maxRoll)}, min up.y {F(minUp)} (negative = went over the top)");
                Object.DestroyImmediate(go);
            }
            // 6. frame-rate dependence of a scripted manoeuvre
            {
                Vector3[] pos = new Vector3[3];
                Vector3[] fwd = new Vector3[3];
                int[] fps = { 30, 60, 144 };
                for (int k = 0; k < 3; k++)
                {
                    UcusKontrol u = Create();
                    u.Yunuslama = -1f; Run(u, 1f, fps[k]);
                    u.Yunuslama = 0f; u.Yatis = 1f; Run(u, 1f, fps[k]);
                    u.Yatis = 0f; u.Sapma = 1f; u.GazGirdisi = 1f; Run(u, 1f, fps[k]);
                    pos[k] = u.transform.position;
                    fwd[k] = u.transform.forward;
                    Object.DestroyImmediate(go);
                }
                L($"scripted 3 s manoeuvre: 30 vs 60 FPS position difference {F((pos[0] - pos[1]).magnitude)} m, heading difference {F(Vector3.Angle(fwd[0], fwd[1]))} deg; 144 vs 60 FPS {F((pos[2] - pos[1]).magnitude)} m, {F(Vector3.Angle(fwd[2], fwd[1]))} deg (path length about {F(pos[1].magnitude)} m)");
            }
            // 7. one long frame
            {
                UcusKontrol u = Create();
                u.GazAyarla(1f);
                Vector3 p0 = u.transform.position;
                u.Adim(0.333f);
                L($"one 0.333 s frame at 42 m/s: jump {F((u.transform.position - p0).magnitude)} m");
                Object.DestroyImmediate(go);
            }
            // 8. chase camera offset under roll
            {
                UcusKontrol u = Create();
                var camGo = new GameObject("Cam", typeof(Camera));
                var cam = camGo.AddComponent<TakipKamerasi>();
                cam.Hedef = u.transform;
                cam.UfukKilitli = withModel;
                cam.YonuGuncelle(float.PositiveInfinity);
                Vector3 level = cam.IstenenKonum;
                u.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
                cam.YonuGuncelle(float.PositiveInfinity);
                Vector3 rolled = cam.IstenenKonum;
                L($"chase camera wanted position, aircraft rolled 90 degrees vs level: moves {F((rolled - level).magnitude)} m (offset 3.2 up / 11 back)");
                Object.DestroyImmediate(camGo);
            }

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/flight"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"probe_{name}.txt"), sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[FlightProbe]\n" + sb);
            Assert.Pass();
        }
    }
}
