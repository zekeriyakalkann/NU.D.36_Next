using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (2026-11-11): the aircraft's engine sound, synthesised on the audio thread (no sound file). A radial engine
    /// driving a two-blade propeller: a buzzing fundamental (propeller beat) with harmonics, the firing pulses on top and a
    /// little air noise; the pitch rises with the throttle and the speed, the volume with the throttle. Silent when the
    /// aircraft stops (crash, shot down) or the game is paused. A 2D sound: the camera follows the aircraft.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MotorSesi : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [SerializeField, Range(0f, 1f)] float ses = 0.35f;
        [Tooltip("Fundamental at idle and at full power (Hz).")]
        [SerializeField] Vector2 frekans = new Vector2(48f, 92f);

        AudioSource kaynak;
        volatile float hedefFrekans = 60f, hedefSes;
        float faz, ateslemeFaz, gurultu, frekansAnlik = 60f, sesAnlik;
        int ornekleme = 48000;
        uint rnd = 2463534242u;

        /// <summary>The fundamental the engine runs at now (Hz).</summary>
        public float Frekans => hedefFrekans;
        public float Ses => hedefSes;
        public AudioSource Kaynak => kaynak;

        void Awake()
        {
            if (ucus == null) ucus = GetComponent<UcusKontrol>();
            kaynak = GetComponent<AudioSource>();
            ornekleme = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            // A silent looping clip keeps the source playing; the sound itself is written in OnAudioFilterRead.
            var bos = AudioClip.Create("MotorTasiyici", ornekleme, 1, ornekleme, false);
            kaynak.clip = bos;
            kaynak.loop = true;
            kaynak.spatialBlend = 0f;
            kaynak.playOnAwake = true;
            kaynak.volume = 1f;
            kaynak.Play();
        }

        /// <summary>Fundamental (Hz) and volume for a throttle 0…1 and a speed share 0…1 (pure; tested).</summary>
        public static (float frekans, float ses) Degerler(float gaz, float hizOrani, Vector2 frekans, float azami)
        {
            float f = Mathf.Lerp(frekans.x, frekans.y, Mathf.Clamp01(0.65f * gaz + 0.35f * hizOrani));
            return (f, azami * (0.45f + 0.55f * Mathf.Clamp01(gaz)));
        }

        void Update()
        {
            bool calisiyor = ucus != null && ucus.enabled && Time.timeScale > 0f;
            float hizOrani = ucus != null ? Mathf.InverseLerp(30f, 100f, ucus.Hiz) : 0f;
            var (f, s) = Degerler(ucus != null ? ucus.GazOrani : 0f, hizOrani, frekans, ses);
            hedefFrekans = f;
            hedefSes = calisiyor ? s : 0f;
        }

        float Rastgele()
        {
            rnd ^= rnd << 13; rnd ^= rnd >> 17; rnd ^= rnd << 5;
            return (rnd & 0xFFFFFF) / (float)0xFFFFFF * 2f - 1f;
        }

        void OnAudioFilterRead(float[] veri, int kanal)
        {
            float dt = 1f / ornekleme;
            for (int i = 0; i < veri.Length; i += kanal)
            {
                frekansAnlik += (hedefFrekans - frekansAnlik) * 0.0005f;
                sesAnlik += (hedefSes - sesAnlik) * 0.0008f;
                faz += frekansAnlik * dt;
                if (faz > 1f) faz -= 1f;
                ateslemeFaz += frekansAnlik * 4.5f * dt;   // nine cylinders, two blades: firing pulses 4.5× the beat
                if (ateslemeFaz > 1f) ateslemeFaz -= 1f;
                float w = 2f * Mathf.PI * faz;
                float ton = Mathf.Sin(w) * 0.55f + Mathf.Sin(2f * w) * 0.3f + Mathf.Sin(3f * w) * 0.18f + Mathf.Sin(5f * w) * 0.08f;
                float darbe = Mathf.Exp(-ateslemeFaz * 9f) * 0.45f;
                gurultu += (Rastgele() - gurultu) * 0.08f;
                float x = (ton * (0.75f + darbe) + gurultu * 0.35f) * sesAnlik;
                for (int c = 0; c < kanal; c++)
                    veri[i + c] = x;
            }
        }
    }
}
