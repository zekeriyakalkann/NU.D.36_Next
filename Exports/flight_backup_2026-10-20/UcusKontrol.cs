using UnityEngine;
using UnityEngine.InputSystem;

namespace NU36
{
    /// <summary>
    /// Lab 3: Transform-based flight control driven by the "Ucus" Input Action Map
    /// (PlayerInput, Behavior = Send Messages). Keys are mapped in the Input Actions asset only.
    /// Axis conventions (verified by Lab 3 tests):
    ///   Yunuslama (W = +1): nose down; Yatis (D = +1): right wing down; Sapma (E = +1): nose right.
    /// </summary>
    public class UcusKontrol : MonoBehaviour
    {
        [Header("Hız (m/s)")]
        [SerializeField, Min(0f)] float enAzHiz = 8f;
        [SerializeField, Min(0f)] float enCokHiz = 42f;

        [Header("Dönüş hızları (derece/s)")]
        [SerializeField, Min(0f)] float yunuslamaHizi = 55f;
        [SerializeField, Min(0f)] float yatisHizi = 90f;
        [SerializeField, Min(0f)] float sapmaHizi = 30f;

        [Tooltip("How quickly the smoothed control inputs follow the raw inputs (1/s).")]
        [SerializeField, Min(0f)] float yumusatma = 6f;

        [Header("Gaz")]
        [SerializeField, Range(0f, 1f)] float baslangicGazi = 0.5f;
        // The worksheet adds v.Get<float>() * 0.02f inside OnGaz. With Send Messages, OnGaz runs only
        // when the axis value changes (press / release), not every frame, so that form changes the
        // throttle by only 0.02 per key press. Here OnGaz stores the axis and Update integrates it.
        // 1.2 per second equals the worksheet's 0.02 step applied every frame at 60 FPS.
        [Tooltip("Throttle change per second while the Gaz axis is held at ±1.")]
        [SerializeField, Min(0f)] float gazDegisimHizi = 1.2f;

        float yunuslama, yatis, sapma, gazGirdisi;
        Vector3 yumusakGirdi; // x = yunuslama, y = sapma, z = yatis
        float gazOrani;
        bool baslatildi;

        /// <summary>Current forward speed in m/s, between enAzHiz and enCokHiz.</summary>
        public float Hiz { get; private set; }

        /// <summary>Current throttle, 0..1.</summary>
        public float GazOrani { get { Baslat(); return gazOrani; } }

        public float EnAzHiz => enAzHiz;
        public float EnCokHiz => enCokHiz;
        public Vector3 YumusakGirdi => yumusakGirdi;

        public float Yumusatma { get => yumusatma; set => yumusatma = Mathf.Max(0f, value); }

        // Raw axis inputs (-1..1). Set by the Input System callbacks; public so that tests and
        // later labs (e.g. an autopilot) can drive the aircraft without a keyboard.
        public float Yunuslama { get => yunuslama; set => yunuslama = Mathf.Clamp(value, -1f, 1f); }
        public float Yatis { get => yatis; set => yatis = Mathf.Clamp(value, -1f, 1f); }
        public float Sapma { get => sapma; set => sapma = Mathf.Clamp(value, -1f, 1f); }
        public float GazGirdisi { get => gazGirdisi; set => gazGirdisi = Mathf.Clamp(value, -1f, 1f); }

        public void GazAyarla(float oran)
        {
            baslatildi = true;
            gazOrani = Mathf.Clamp01(oran);
            Hiz = HizHesapla(gazOrani);
        }

        public float HizHesapla(float oran) => Mathf.Lerp(enAzHiz, enCokHiz, Mathf.Clamp01(oran));

        void Baslat()
        {
            if (baslatildi)
                return;
            baslatildi = true;
            gazOrani = baslangicGazi;
            Hiz = HizHesapla(gazOrani);
        }

        void Awake() => Baslat();

        // ---- PlayerInput (Send Messages) callbacks: method name = "On" + action name ----
        void OnYunuslama(InputValue v) => Yunuslama = v.Get<float>();
        void OnYatis(InputValue v) => Yatis = v.Get<float>();
        void OnSapma(InputValue v) => Sapma = v.Get<float>();
        void OnGaz(InputValue v) => GazGirdisi = v.Get<float>();

        void Update() => Adim(Time.deltaTime);

        /// <summary>Advances throttle, smoothing, rotation and movement by <paramref name="dt"/> seconds.</summary>
        public void Adim(float dt)
        {
            Baslat();
            if (dt <= 0f)
                return;

            gazOrani = Mathf.Clamp01(gazOrani + gazGirdisi * gazDegisimHizi * dt);
            Hiz = HizHesapla(gazOrani);

            // Exponential smoothing: 1 - e^(-k·dt) ≈ k·dt for small steps, but stays frame-rate
            // independent and never overshoots for large dt.
            float t = 1f - Mathf.Exp(-yumusatma * dt);
            yumusakGirdi = Vector3.Lerp(yumusakGirdi, new Vector3(yunuslama, sapma, yatis), t);

            transform.Rotate(
                yumusakGirdi.x * yunuslamaHizi * dt,
                yumusakGirdi.y * sapmaHizi * dt,
                -yumusakGirdi.z * yatisHizi * dt,
                Space.Self);
            transform.position += transform.forward * (Hiz * dt);
        }
    }
}
