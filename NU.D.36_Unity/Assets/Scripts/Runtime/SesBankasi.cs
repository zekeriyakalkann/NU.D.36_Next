using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (2026-11-11, user: "uçağın sesi de yok"): the project had no audio at all. Sounds are synthesised in code at
    /// first use (no downloaded files, no licences): a rifle-calibre shot, a cannon shot and an explosion. Deterministic
    /// noise (fixed seed). <see cref="Cal"/> plays a clip at a point (3D, with distance roll-off).
    /// </summary>
    public static class SesBankasi
    {
        const int Hz = 44100;
        static AudioClip mermi, top, patlama;

        public static AudioClip Mermi => mermi ??= Uret("Mermi", 0.12f, 1, (t, r) =>
            (r() * 2f - 1f) * Mathf.Exp(-t / 0.018f) * 0.9f + Mathf.Sin(2f * Mathf.PI * 150f * t) * Mathf.Exp(-t / 0.03f) * 0.6f);

        public static AudioClip Top => top ??= Uret("Top", 0.7f, 2, (t, r) =>
            (r() * 2f - 1f) * Mathf.Exp(-t / 0.06f) * 0.8f + Mathf.Sin(2f * Mathf.PI * 62f * t) * Mathf.Exp(-t / 0.16f) * 0.9f);

        public static AudioClip Patlama => patlama ??= Uret("Patlama", 2.2f, 3, (t, r) =>
            (r() * 2f - 1f) * Mathf.Exp(-t / 0.45f) * (t < 0.02f ? t / 0.02f : 1f) + Mathf.Sin(2f * Mathf.PI * 38f * t) * Mathf.Exp(-t / 0.35f) * 0.8f);

        /// <summary>Builds a mono clip: <paramref name="f"/>(t, random) per sample, then a one-pole low-pass for weight.</summary>
        static AudioClip Uret(string ad, float sure, int tohum, System.Func<float, System.Func<float>, float> f)
        {
            int n = Mathf.CeilToInt(sure * Hz);
            var veri = new float[n];
            var rnd = new System.Random(tohum);
            System.Func<float> r = () => (float)rnd.NextDouble();
            float onceki = 0f, enCok = 1e-4f;
            for (int i = 0; i < n; i++)
            {
                float x = f(i / (float)Hz, r);
                onceki += (x - onceki) * (ad == "Mermi" ? 0.55f : 0.22f);
                veri[i] = onceki;
                enCok = Mathf.Max(enCok, Mathf.Abs(onceki));
            }
            for (int i = 0; i < n; i++) veri[i] = veri[i] / enCok * 0.9f;
            var c = AudioClip.Create(ad, n, 1, Hz, false);
            c.SetData(veri, 0);
            return c;
        }

        /// <summary>Plays <paramref name="klip"/> at <paramref name="nokta"/> (3D). Returns the temporary source.</summary>
        public static AudioSource Cal(AudioClip klip, Vector3 nokta, float ses = 1f, float enAz = 15f, float enCok = 900f, float perde = 1f)
        {
            var go = new GameObject("Ses " + klip.name);
            go.transform.position = nokta;
            var a = go.AddComponent<AudioSource>();
            a.clip = klip;
            a.volume = ses;
            a.pitch = perde;
            a.spatialBlend = 1f;
            a.rolloffMode = AudioRolloffMode.Logarithmic;
            a.minDistance = enAz;
            a.maxDistance = enCok;
            a.dopplerLevel = 0f;
            a.Play();
            Object.Destroy(go, klip.length / Mathf.Max(0.1f, perde) + 0.1f);
            return a;
        }
    }
}
