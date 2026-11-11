using System.Collections.Generic;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4): the centre line of the railway. A centripetal Catmull-Rom curve through the control points
    /// (no cusps, no overshoot in tight bends), resampled at a fixed arc-length step, so a distance along the line
    /// (<c>s</c>, metres) maps directly to a point. Pure C#: used by <see cref="RayHatti"/>, the train, the terrain and tests.
    /// </summary>
    public sealed class RayEgrisi
    {
        public const float VarsayilanAdim = 1f;
        const int AltOrnek = 64;   // dense samples per control segment before resampling

        readonly Vector3[] ornekler;
        readonly float adim;

        public float Uzunluk { get; }
        public float Adim => adim;
        public int OrnekSayisi => ornekler.Length;
        public Vector3 Ornek(int i) => ornekler[Mathf.Clamp(i, 0, ornekler.Length - 1)];

        public RayEgrisi(IReadOnlyList<Vector3> kontrol, float adim = VarsayilanAdim)
        {
            if (kontrol == null || kontrol.Count < 2)
                throw new System.ArgumentException("At least two control points are needed.");
            this.adim = Mathf.Max(0.05f, adim);

            // Dense polyline along the spline (end tangents from mirrored phantom points).
            int n = kontrol.Count;
            var yogun = new List<Vector3>((n - 1) * AltOrnek + 1);
            for (int k = 0; k < n - 1; k++)
            {
                Vector3 p0 = k > 0 ? kontrol[k - 1] : 2f * kontrol[0] - kontrol[1];
                Vector3 p1 = kontrol[k], p2 = kontrol[k + 1];
                Vector3 p3 = k + 2 < n ? kontrol[k + 2] : 2f * kontrol[n - 1] - kontrol[n - 2];
                for (int j = 0; j < AltOrnek; j++)
                    yogun.Add(CatmullRom(p0, p1, p2, p3, j / (float)AltOrnek));
            }
            yogun.Add(kontrol[n - 1]);

            var birikim = new float[yogun.Count];
            for (int i = 1; i < yogun.Count; i++)
                birikim[i] = birikim[i - 1] + Vector3.Distance(yogun[i - 1], yogun[i]);
            Uzunluk = birikim[birikim.Length - 1];

            // Uniform resampling: sample i sits at s = i·step (the last one at the end of the line).
            int sayi = Mathf.CeilToInt(Uzunluk / this.adim) + 1;
            ornekler = new Vector3[sayi];
            int seg = 0;
            for (int i = 0; i < sayi; i++)
            {
                float s = Mathf.Min(i * this.adim, Uzunluk);
                while (seg < birikim.Length - 2 && birikim[seg + 1] < s)
                    seg++;
                float bas = birikim[seg], son = birikim[seg + 1];
                float t = son > bas ? (s - bas) / (son - bas) : 0f;
                ornekler[i] = Vector3.Lerp(yogun[seg], yogun[seg + 1], t);
            }
        }

        /// <summary>Centripetal Catmull-Rom (Barry–Goldman), alpha 0.5, knots from the 3D chord lengths.</summary>
        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t0 = 0f;
            float t1 = t0 + Mathf.Sqrt(Mathf.Max(1e-4f, Vector3.Distance(p0, p1)));
            float t2 = t1 + Mathf.Sqrt(Mathf.Max(1e-4f, Vector3.Distance(p1, p2)));
            float t3 = t2 + Mathf.Sqrt(Mathf.Max(1e-4f, Vector3.Distance(p2, p3)));
            float u = Mathf.Lerp(t1, t2, t);
            Vector3 a1 = (t1 - u) / (t1 - t0) * p0 + (u - t0) / (t1 - t0) * p1;
            Vector3 a2 = (t2 - u) / (t2 - t1) * p1 + (u - t1) / (t2 - t1) * p2;
            Vector3 a3 = (t3 - u) / (t3 - t2) * p2 + (u - t2) / (t3 - t2) * p3;
            Vector3 b1 = (t2 - u) / (t2 - t0) * a1 + (u - t0) / (t2 - t0) * a2;
            Vector3 b2 = (t3 - u) / (t3 - t1) * a2 + (u - t1) / (t3 - t1) * a3;
            return (t2 - u) / (t2 - t1) * b1 + (u - t1) / (t2 - t1) * b2;
        }

        /// <summary>Point at distance <paramref name="s"/> along the line (clamped to the ends).</summary>
        public Vector3 Nokta(float s)
        {
            float f = Mathf.Clamp(s, 0f, Uzunluk) / adim;
            int i = Mathf.Min((int)f, ornekler.Length - 2);
            return Vector3.LerpUnclamped(ornekler[i], ornekler[i + 1], f - i);
        }

        /// <summary>Unit tangent (3D, direction of increasing s).</summary>
        public Vector3 Yon(float s)
        {
            float h = Mathf.Max(adim, 1f);
            Vector3 d = Nokta(Mathf.Min(s + h, Uzunluk)) - Nokta(Mathf.Max(s - h, 0f));
            return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward;
        }

        /// <summary>Unit tangent projected on the ground plane.</summary>
        public Vector3 YatayYon(float s)
        {
            Vector3 d = Yon(s);
            d.y = 0f;
            return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward;
        }

        /// <summary>Horizontal unit vector to the right of the direction of travel.</summary>
        public Vector3 Sag(float s) => Vector3.Cross(Vector3.up, YatayYon(s)).normalized;

        /// <summary>Distance along the line of the point nearest to <paramref name="p"/> in plan view (brute force over the
        /// samples, then projected on the two neighbouring segments; fine for occasional queries), with the plan-view
        /// distance to it.</summary>
        public float EnYakin(Vector3 p, out float yatayUzaklik)
        {
            float enIyi = float.MaxValue;
            int enIyiI = 0;
            for (int i = 0; i < ornekler.Length; i++)
            {
                float dx = ornekler[i].x - p.x, dz = ornekler[i].z - p.z;
                float d = dx * dx + dz * dz;
                if (d < enIyi) { enIyi = d; enIyiI = i; }
            }
            float s = enIyiI * adim;
            for (int i = Mathf.Max(0, enIyiI - 1); i <= Mathf.Min(enIyiI, ornekler.Length - 2); i++)
            {
                Vector2 a = new Vector2(ornekler[i].x, ornekler[i].z), b = new Vector2(ornekler[i + 1].x, ornekler[i + 1].z);
                Vector2 ab = b - a, q = new Vector2(p.x, p.z);
                float u = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
                float d = (a + ab * u - q).sqrMagnitude;
                if (d < enIyi) { enIyi = d; s = (i + u) * adim; }
            }
            yatayUzaklik = Mathf.Sqrt(enIyi);
            return Mathf.Min(s, Uzunluk);
        }

        /// <summary>Plan-view radius of curvature at s, from the heading change over ±<paramref name="yarim"/> metres.</summary>
        public float EgrilikYaricapi(float s, float yarim = 25f)
        {
            Vector3 a = YatayYon(Mathf.Max(0f, s - yarim)), b = YatayYon(Mathf.Min(Uzunluk, s + yarim));
            float aci = Vector3.Angle(a, b) * Mathf.Deg2Rad;
            return aci > 1e-5f ? 2f * yarim / aci : float.PositiveInfinity;
        }
    }
}
