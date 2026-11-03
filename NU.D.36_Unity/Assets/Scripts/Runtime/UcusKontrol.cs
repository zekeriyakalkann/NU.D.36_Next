using UnityEngine;
using UnityEngine.InputSystem;

namespace NU36
{
    /// <summary>
    /// Lab 3: Transform-based flight control driven by the "Ucus" Input Action Map
    /// (PlayerInput, Behavior = Send Messages). Keys are mapped in the Input Actions asset only.
    /// Axis conventions (verified by Lab 3 tests):
    ///   Yunuslama (W = +1): nose down; Yatis (D = +1): right wing down; Sapma (E = +1): nose right.
    /// Flight model (opt-in, all zero by default so Lab 3 and Rota04 behave exactly as before; Rota04Hud switches them on):
    ///   speed follows the throttle with a limited acceleration, climbing costs and diving gains speed, a banked aircraft
    ///   turns (coordinated-turn rate g*tan(bank)/speed), the bank levels out when the roll key is released, and the bank
    ///   is limited. Still one script, one Transform: nothing else moves the aircraft in the flying scenes.
    /// </summary>
    public class UcusKontrol : MonoBehaviour
    {
        const float YerCekimi = 9.81f;

        [Header("Hız (m/s)")]
        [SerializeField, Min(0f)] float enAzHiz = 8f;
        [SerializeField, Min(0f)] float enCokHiz = 42f;
        [Tooltip("Speed change per second towards the throttle speed (m/s²). 0 = the speed jumps to the throttle speed (Lab 3).")]
        [SerializeField, Min(0f)] float hizIvmesi;
        [Tooltip("Target speed change per unit of forward.y (m/s): climbing slows the aircraft, diving speeds it up. 0 = off.")]
        [SerializeField, Min(0f)] float egimHizKaybi;

        [Header("Dönüş hızları (derece/s)")]
        [SerializeField, Min(0f)] float yunuslamaHizi = 55f;
        [SerializeField, Min(0f)] float yatisHizi = 90f;
        [SerializeField, Min(0f)] float sapmaHizi = 30f;
        [Tooltip("Reverses the pitch key: false = W nose down (Lab 3, D-029), true = W nose up.")]
        [SerializeField] bool yunuslamaTers;

        [Tooltip("How quickly the smoothed control inputs follow the raw inputs (1/s).")]
        [SerializeField, Min(0f)] float yumusatma = 6f;

        [Header("Yatış (derece)")]
        [Tooltip("Largest bank angle reachable with the roll key (0 = unlimited). Not applied while inverted (loops).")]
        [SerializeField, Range(0f, 90f)] float enFazlaYatis;
        [Tooltip("Return-to-level rate (1/s: degrees per second per degree of bank, capped at 60 % of the roll rate) when the roll key is released. 0 = it stays banked (Lab 3).")]
        [SerializeField, Min(0f)] float yatisDengeleme;
        [Tooltip("Scale of the coordinated-turn rate g*tan(bank)/speed applied about the world up axis (0 = a banked aircraft does not turn, Lab 3; 1 = physical).")]
        [SerializeField, Range(0f, 1f)] float yatisDonusu;
        [Tooltip("Largest turn rate caused by the bank (degrees/s).")]
        [SerializeField, Min(1f)] float enFazlaDonusHizi = 45f;

        [Header("Gaz")]
        [SerializeField, Range(0f, 1f)] float baslangicGazi = 0.5f;
        // The worksheet adds v.Get<float>() * 0.02f inside OnGaz. With Send Messages, OnGaz runs only
        // when the axis value changes (press / release), not every frame, so that form changes the
        // throttle by only 0.02 per key press. Here OnGaz stores the axis and Update integrates it.
        // 1.2 per second equals the worksheet's 0.02 step applied every frame at 60 FPS.
        [Tooltip("Throttle change per second while the Gaz axis is held at ±1.")]
        [SerializeField, Min(0f)] float gazDegisimHizi = 1.2f;

        [Header("Zaman adımı")]
        [Tooltip("Largest time step Update passes on (s). A long frame (a hitch) then moves the aircraft less instead of jumping.")]
        [SerializeField, Min(0.005f)] float enFazlaAdim = 0.1f;

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
        public float HizIvmesi { get => hizIvmesi; set => hizIvmesi = Mathf.Max(0f, value); }
        public float EgimHizKaybi { get => egimHizKaybi; set => egimHizKaybi = Mathf.Max(0f, value); }
        public float EnFazlaYatis { get => enFazlaYatis; set => enFazlaYatis = Mathf.Clamp(value, 0f, 90f); }
        public float YatisDengeleme { get => yatisDengeleme; set => yatisDengeleme = Mathf.Max(0f, value); }
        public float YatisDonusu { get => yatisDonusu; set => yatisDonusu = Mathf.Clamp01(value); }
        public float EnFazlaDonusHizi { get => enFazlaDonusHizi; set => enFazlaDonusHizi = Mathf.Max(1f, value); }
        public bool YunuslamaTers { get => yunuslamaTers; set => yunuslamaTers = value; }
        public float EnFazlaAdim { get => enFazlaAdim; set => enFazlaAdim = Mathf.Max(0.005f, value); }

        /// <summary>Bank angle in degrees, positive = right wing down (the direction of Yatis = +1).</summary>
        public float YatisAcisi => Mathf.Atan2(-transform.right.y, transform.up.y) * Mathf.Rad2Deg;

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

        void Update() => Adim(AdimSuresi(Time.deltaTime));

        /// <summary>The time step Update uses for a frame of <paramref name="dt"/> seconds (long frames are cut).</summary>
        public float AdimSuresi(float dt) => Mathf.Min(dt, enFazlaAdim);

        /// <summary>Advances throttle, smoothing, rotation and movement by <paramref name="dt"/> seconds.</summary>
        public void Adim(float dt)
        {
            Baslat();
            if (dt <= 0f)
                return;

            gazOrani = Mathf.Clamp01(gazOrani + gazGirdisi * gazDegisimHizi * dt);
            HizGuncelle(dt);

            // Exponential smoothing: 1 - e^(-k·dt) ≈ k·dt for small steps, but stays frame-rate
            // independent and never overshoots for large dt.
            float t = 1f - Mathf.Exp(-yumusatma * dt);
            yumusakGirdi = Vector3.Lerp(yumusakGirdi, new Vector3(yunuslama, sapma, yatis), t);

            float yatisAdimi = yumusakGirdi.z * yatisHizi * dt;   // degrees, positive = bank grows to the right
            if (enFazlaYatis > 0f && transform.up.y > 0f)
            {
                float bank = YatisAcisi;
                yatisAdimi = yatisAdimi > 0f
                    ? Mathf.Min(yatisAdimi, Mathf.Max(0f, enFazlaYatis - bank))
                    : Mathf.Max(yatisAdimi, -Mathf.Max(0f, enFazlaYatis + bank));
            }
            transform.Rotate(
                (yunuslamaTers ? -yumusakGirdi.x : yumusakGirdi.x) * yunuslamaHizi * dt,
                yumusakGirdi.y * sapmaHizi * dt,
                -yatisAdimi,
                Space.Self);

            Dengele(dt);
            BankDonusu(dt);
            transform.position += transform.forward * (Hiz * dt);
        }

        /// <summary>Speed follows the throttle speed (shifted by the climb angle) with a limited acceleration.</summary>
        void HizGuncelle(float dt)
        {
            float hedef = HizHesapla(gazOrani);
            if (egimHizKaybi > 0f)
                hedef = Mathf.Clamp(hedef - egimHizKaybi * transform.forward.y, enAzHiz, enCokHiz);
            Hiz = hizIvmesi > 0f ? Mathf.MoveTowards(Hiz, hedef, hizIvmesi * dt) : hedef;
        }

        // Pitch factor: both effects below fade out when the nose points up or down, where "bank" is not defined.
        float YatayOran => Mathf.Sqrt(Mathf.Max(0f, 1f - transform.forward.y * transform.forward.y));

        /// <summary>
        /// Returns the bank towards level when the roll key is not held. The assist fades in as the smoothed roll input dies away (no step
        /// at the moment of release) and is capped at 60 % of the roll rate. Skipped inverted, so a loop is not rolled upright.
        /// </summary>
        void Dengele(float dt)
        {
            if (yatisDengeleme <= 0f || transform.up.y <= 0.05f)
                return;
            float agirlik = 1f - Mathf.Clamp01(Mathf.Abs(yumusakGirdi.z) / 0.3f);
            if (agirlik <= 0f)
                return;
            float hiz = Mathf.Clamp(YatisAcisi * yatisDengeleme, -0.6f * yatisHizi, 0.6f * yatisHizi);   // degrees/s towards level
            transform.Rotate(0f, 0f, hiz * dt * YatayOran * agirlik, Space.Self);
        }

        /// <summary>A banked aircraft turns: rate = g·tan(bank)/speed about the world up axis (bank clamped to 70 degrees, rate capped).</summary>
        void BankDonusu(float dt)
        {
            if (yatisDonusu <= 0f || transform.up.y <= 0f)
                return;
            float bank = Mathf.Clamp(YatisAcisi, -70f, 70f) * Mathf.Deg2Rad;
            float oran = YerCekimi * Mathf.Tan(bank) / Mathf.Max(Hiz, 1f) * Mathf.Rad2Deg * yatisDonusu;
            oran = Mathf.Clamp(oran, -enFazlaDonusHizi, enFazlaDonusHizi) * YatayOran;
            transform.Rotate(0f, oran * dt, 0f, Space.World);
        }
    }
}
