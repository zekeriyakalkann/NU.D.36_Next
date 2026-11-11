using System.IO;
using System.Text;
using NUnit.Framework;
using NU36.Editor;
using UnityEditor;
using UnityEngine;

namespace NU36.Tests
{
    /// <summary>
    /// Handling record of the Rota07 aircraft (aerodynamic mode with the Rota07 builder's values), stepped without a scene:
    /// how a key tap and a held key move the nose and the wings at different speeds, peak load factors, the stall, the bank
    /// after the roll key is released and hands-off height keeping. Writes Logs/lab7/ucus_tepki_&lt;etiket&gt;.txt; asserts
    /// nothing (the handling rules are asserted in Lab7UcusFizigiTests). Explicit: run before and after a change to compare.
    /// </summary>
    [Explicit("Handling record; run with -testFilter NU36.Tests.Lab7UcusTepkiOlcumu")]
    public class Lab7UcusTepkiOlcumu
    {
        const float Dt = 1f / 60f;
        GameObject go;
        UcusKontrol u;

        void Kur(float hiz, float gaz, float yatis = 0f, float yukseklik = 1500f)
        {
            if (go != null) Object.DestroyImmediate(go);
            go = new GameObject("Olcum Ucak");
            go.transform.SetPositionAndRotation(new Vector3(0f, yukseklik, 0f), Quaternion.Euler(0f, 0f, -yatis));
            u = go.AddComponent<UcusKontrol>();
            var so = new SerializedObject(u);
            so.FindProperty("aerodinamik").boolValue = true;
            so.FindProperty("yunuslamaHizi").floatValue = Lab7SceneBuilder.YunuslamaHizi;
            so.FindProperty("yatisHizi").floatValue = Lab7SceneBuilder.YatisHizi;
            so.FindProperty("sapmaHizi").floatValue = Lab7SceneBuilder.SapmaHizi;
            so.FindProperty("enFazlaYatis").floatValue = 0f;
            so.FindProperty("yatisDengeleme").floatValue = Lab7SceneBuilder.YatisDengeleme;
            so.FindProperty("gazDegisimHizi").floatValue = Lab7SceneBuilder.GazDegisimHizi;
            so.ApplyModifiedPropertiesWithoutUndo();
            u.HizAyarla(hiz);
            u.GazAyarla(gaz);
            Uc(0.5f);   // settle into trimmed flight
        }

        [TearDown]
        public void Sil()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        void Uc(float saniye, System.Action her = null)
        {
            for (float t = 0f; t < saniye - 1e-4f; t += Dt)
            {
                u.Adim(Dt);
                her?.Invoke();
            }
        }

        float Yunuslama => Mathf.Asin(Mathf.Clamp(go.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        [Test]
        public void Kayit()
        {
            string etiket = System.Environment.GetEnvironmentVariable("NU36_OLCUM_ETIKETI");
            if (string.IsNullOrEmpty(etiket)) etiket = "son";
            var r = new StringBuilder();
            r.AppendLine($"Rota07 aircraft handling record '{etiket}', {System.DateTime.Now:yyyy-MM-dd HH:mm}. Key = full input (±1) from the keyboard.");

            foreach (float v in new[] { 50f, 80f, 110f })
            {
                // Pitch: 0.1 s tap of S, then released.
                Kur(v, 0.6f);
                float p0 = Yunuslama, nEnCok = 0f;
                u.Yunuslama = -1f; Uc(0.1f, () => nEnCok = Mathf.Max(nEnCok, u.YukFaktoru));
                u.Yunuslama = 0f; Uc(1.4f, () => nEnCok = Mathf.Max(nEnCok, u.YukFaktoru));
                r.AppendLine($"v {v:F0} m/s  S tap 0.1 s: nose +{Yunuslama - p0:F1}° after 1.5 s, peak {nEnCok:F2} g");

                // Pitch: S held 1 s.
                Kur(v, 0.6f);
                p0 = Yunuslama; nEnCok = 0f;
                float aEnCok = 0f; bool tutunma = false;
                u.Yunuslama = -1f;
                Uc(1f, () => { nEnCok = Mathf.Max(nEnCok, u.YukFaktoru); aEnCok = Mathf.Max(aEnCok, u.HucumAcisi); tutunma |= u.TutunmaKaybi; });
                r.AppendLine($"v {v:F0} m/s  S held 1 s: nose +{Yunuslama - p0:F1}°, peak {nEnCok:F2} g, peak AoA {aEnCok:F1}°, stalled {tutunma}");

                // Roll: 0.1 s tap of D; D held 1 s.
                Kur(v, 0.6f);
                u.Yatis = 1f; Uc(0.1f); u.Yatis = 0f; Uc(0.9f);
                float dokunus = u.YatisAcisi;
                Kur(v, 0.6f);
                u.Yatis = 1f; Uc(1f);
                float tutma = u.YatisAcisi;
                u.Yatis = 0f; Uc(3f);
                r.AppendLine($"v {v:F0} m/s  D tap 0.1 s: bank {dokunus:F1}° after 1 s; D held 1 s: bank {tutma:F1}°, 3 s after release {u.YatisAcisi:F1}°");
            }

            // Full pull held 3 s at 45 m/s, idle: stall?
            Kur(45f, 0f);
            float h0 = go.transform.position.y, enAzHiz = 99f; bool stall = false; float aMax = 0f;
            u.Yunuslama = -1f;
            Uc(3f, () => { stall |= u.TutunmaKaybi; enAzHiz = Mathf.Min(enAzHiz, u.Hiz); aMax = Mathf.Max(aMax, u.HucumAcisi); });
            u.Yunuslama = 0f; Uc(4f, () => enAzHiz = Mathf.Min(enAzHiz, u.Hiz));
            r.AppendLine($"v 45 m/s idle, S held 3 s then released 4 s: stalled {stall}, peak AoA {aMax:F1}°, lowest speed {enAzHiz:F1} m/s, height change {go.transform.position.y - h0:F0} m");

            // A 30° turn: roll in with D, hold 10 s hands-off.
            Kur(70f, 0.6f);
            u.Yatis = 1f;
            float t30 = 0f;
            while (u.YatisAcisi < 30f && t30 < 5f) { u.Adim(Dt); t30 += Dt; }
            u.Yatis = 0f;
            h0 = go.transform.position.y;
            float yon0 = go.transform.eulerAngles.y;
            Uc(10f);
            r.AppendLine($"v 70 m/s  rolled to 30° in {t30:F2} s with D, then 10 s hands-off: bank {u.YatisAcisi:F1}°, heading change {Mathf.DeltaAngle(yon0, go.transform.eulerAngles.y):F0}°, height change {go.transform.position.y - h0:F0} m");

            // Hands-off 30 s at 70 m/s, throttle 0.45.
            Kur(70f, 0.45f);
            float enAz = float.MaxValue, enCokY = float.MinValue;
            Uc(30f, () => { enAz = Mathf.Min(enAz, go.transform.position.y); enCokY = Mathf.Max(enCokY, go.transform.position.y); });
            r.AppendLine($"v 70 m/s  hands-off 30 s: height band {enCokY - enAz:F1} m, end speed {u.Hiz:F1} m/s");

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/lab7"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"ucus_tepki_{etiket}.txt"), r.ToString(), new UTF8Encoding(false));
            Debug.Log(r.ToString());
        }
    }
}
