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

        [Header("Aerodinamik model (Lab 7)")]
        [Tooltip("false = the kinematic model above (Labs 3–6, unchanged). true = forces: lift, drag, propeller thrust and gravity " +
                 "move a velocity vector; the keys command rotation rates whose authority grows with the airspeed; the nose weathervanes " +
                 "into the airflow; the wing stalls; the wheels roll on the ground. enAzHiz/enCokHiz/hizIvmesi/egimHizKaybi/yatisDonusu are not used.")]
        [SerializeField] bool aerodinamik;
        [SerializeField] UcakAerodinamigi aero = new UcakAerodinamigi();
        [Tooltip("Aerodynamic mode: speed along the nose at the start (m/s). 0 = standing on the ground.")]
        [SerializeField, Min(0f)] float baslangicHizi;

        [Header("Kolay uçuş (2026-11-10, kullanıcı isteği)")]
        [Tooltip("Aerodynamic mode only. On: A/D command a bank angle and W/S a climb/dive angle; released, the aircraft flies " +
                 "straight and level; the speed protection prevents the stall; on the ground the nose rises by itself at take-off " +
                 "speed with the throttle open (see UcusYardimi). Off: the stick law above (Lab 7 model).")]
        [SerializeField] bool kolayUcus;
        [SerializeField] UcusYardimi yardim = new UcusYardimi();

        [Header("Arcade uçuş (2026-11-11, kullanıcı isteği)")]
        [Tooltip("On: the keys move the aircraft directly (ArkadUcus): no lift, no stall, level when released, ground and map-edge " +
                 "protection. Takes precedence over the two models above. Off by default: Labs 3–7 unchanged.")]
        [SerializeField] bool arkad;
        [Tooltip("K cycles arcade → easy (aerodynamic) → realistic (aerodynamic). Off: K only toggles the easy flight (Rota07).")]
        [SerializeField] bool arkadSecenegi;
        [SerializeField] ArkadUcus arkadAyar = new ArkadUcus();

        const float AeroAltAdim = 0.01f;   // s; the aerodynamic step is split so the stability terms stay stable at any frame rate

        float yunuslama, yatis, sapma, gazGirdisi;
        Vector3 yumusakGirdi; // x = yunuslama, y = sapma, z = yatis
        // Aerodynamic mode: the keyboard moves a stick (x = pitch, y = yaw, z = roll), the roll mode has a state, and the
        // flight path direction of the last step gives the path's turn rate (the nose follows the path).
        float yatisHiziDurum;
        Vector3 oncekiYon;
        bool oncekiYonVar;
        float gazOrani;
        bool baslatildi;
        Vector3 hizVektoru;

        /// <summary>Current forward speed in m/s (kinematic mode: between enAzHiz and enCokHiz; aerodynamic mode: airspeed).</summary>
        public float Hiz { get; private set; }

        /// <summary>Velocity in world space (m/s). Kinematic mode: along the nose.</summary>
        public Vector3 HizVektoru => aerodinamik ? hizVektoru : transform.forward * Hiz;

        /// <summary>Aerodynamic mode on/off. Switching it on keeps the current speed along the nose.</summary>
        public bool Aerodinamik
        {
            get => aerodinamik;
            set
            {
                Baslat();
                if (value && !aerodinamik)
                    hizVektoru = transform.forward * Hiz;
                aerodinamik = value;
            }
        }

        public UcakAerodinamigi Aero => aero;
        public ArkadUcus ArkadAyar => arkadAyar;
        public bool ArkadSecenegi { get => arkadSecenegi; set => arkadSecenegi = value; }

        /// <summary>Arcade flight on/off. On: the aerodynamic model is off; off: back to the aerodynamic model with the
        /// current speed along the nose (when the scene offers the arcade choice).</summary>
        public bool Arkad
        {
            get => arkad;
            set
            {
                Baslat();
                if (value == arkad) return;
                if (value)
                {
                    Hiz = Mathf.Max(aerodinamik ? hizVektoru.magnitude : Hiz, arkadAyar.enAzHiz);
                    aerodinamik = false;
                    arkadAyar.Sifirla();
                }
                else
                {
                    arkad = false;
                    Aerodinamik = true;
                    hizVektoru = transform.forward * Hiz;
                    oncekiYonVar = false;
                    yatisHiziDurum = 0f;
                }
                arkad = value;
            }
        }

        /// <summary>Name of the flight mode for the HUD.</summary>
        public string ModAdi => arkad ? "ARCADE" : aerodinamik ? (kolayUcus ? "KOLAY" : "GERÇEKÇİ") : "KİNEMATİK";

        /// <summary>K: with the arcade choice arcade → easy → realistic → arcade; without it the easy flight toggles (Rota07).</summary>
        public void SonrakiMod()
        {
            if (!arkadSecenegi) { kolayUcus = !kolayUcus; return; }
            if (arkad) { Arkad = false; kolayUcus = true; }
            else if (kolayUcus) kolayUcus = false;
            else Arkad = true;
        }
        public UcusYardimi Yardim => yardim;

        /// <summary>Easy flight on/off (aerodynamic mode; see <see cref="UcusYardimi"/>).</summary>
        public bool KolayUcus { get => kolayUcus; set => kolayUcus = value; }
        /// <summary>Brake held: in the air the air brake adds drag, on the ground the wheel brakes work at any throttle (aerodynamic mode).</summary>
        public bool Fren { get; set; }
        /// <summary>Easy flight: lowest flight path angle allowed this frame (degrees; terrain floor, landing sink envelope). −90 = none.</summary>
        public float EgimAltSiniri { get; set; } = -90f;
        /// <summary>Easy flight: largest bank allowed this frame (degrees; near the ground). 180 = none.</summary>
        public float YatisSiniri { get; set; } = 180f;
        /// <summary>Easy flight: highest flight path angle allowed this frame (degrees; the automatic touchdown). The floor wins. 90 = none.</summary>
        public float EgimUstSiniri { get; set; } = 90f;
        /// <summary>Easy flight: least throttle the engine runs at this frame (0..1; full power while the terrain floor climbs).</summary>
        public float GazDestegi { get; set; }
        /// <summary>Easy flight: a bank (degrees) that replaces the roll keys this frame (the map-edge turn). NaN = the keys.</summary>
        public float ZorunluYatis { get; set; } = float.NaN;
        /// <summary>Easy flight: the flight path angle (degrees) the law is steering to (after all limits).</summary>
        public float HedefEgim { get; private set; }
        /// <summary>Easy flight: the pilot's flight path command (degrees) after the speed protection, before the external floor.</summary>
        public float HedefEgimHam { get; private set; }
        /// <summary>Easy flight: the commanded bank (degrees).</summary>
        public float HedefYatis { get; private set; }
        /// <summary>Easy flight: the engine runs above the lever (speed protection, or full power for the terrain floor).</summary>
        public bool HizKorumasi { get; private set; }
        /// <summary>The air brake is out this step (brake held in the air, and in easy flight fast enough).</summary>
        public bool HavaFreniAcik { get; private set; }
        /// <summary>Flight path angle (degrees, positive = climbing).</summary>
        public float Egim => Hiz > 0.5f ? Mathf.Asin(Mathf.Clamp(HizVektoru.y / Mathf.Max(HizVektoru.magnitude, 1e-3f), -1f, 1f)) * Mathf.Rad2Deg : 0f;

        /// <summary>Set by the ground probe (<see cref="InisAlgilayici"/>) while the wheels are on the ground. Aerodynamic mode only.</summary>
        public bool YerTemasi { get; set; }
        /// <summary>Angle of attack in degrees (aerodynamic mode).</summary>
        public float HucumAcisi { get; private set; }
        /// <summary>Sideslip in degrees, positive = airflow from the right (aerodynamic mode).</summary>
        public float YanKayma { get; private set; }
        /// <summary>Load factor n = lift / weight (aerodynamic mode).</summary>
        public float YukFaktoru { get; private set; } = 1f;
        /// <summary>The wing is beyond its stall angle (aerodynamic mode).</summary>
        public bool TutunmaKaybi { get; private set; }
        /// <summary>Within 3 degrees of the stall angle or beyond it (aerodynamic mode): the stall warning.</summary>
        public bool TutunmaUyarisi => aerodinamik && !YerTemasi && Hiz > 5f && HucumAcisi > aero.tutunmaAcisi - 3f;
        /// <summary>Vertical speed (m/s, positive = climbing).</summary>
        public float DikeyHiz => HizVektoru.y;

        /// <summary>Aerodynamic mode: sets the velocity to <paramref name="v"/> m/s along the nose (tests, scene start).</summary>
        public void HizAyarla(float v)
        {
            Baslat();
            hizVektoru = transform.forward * Mathf.Max(0f, v);
            Hiz = Mathf.Max(0f, v);
            oncekiYonVar = false;   // a set velocity is not a turn of the flight path
            yatisHiziDurum = 0f;
        }

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

        /// <summary>Sets the throttle. Kinematic mode: the speed jumps to the throttle speed. Aerodynamic mode: only the throttle changes.</summary>
        public void GazAyarla(float oran)
        {
            Baslat();
            gazOrani = Mathf.Clamp01(oran);
            if (!aerodinamik && !arkad)
                Hiz = HizHesapla(gazOrani);
        }

        public float HizHesapla(float oran) => Mathf.Lerp(enAzHiz, enCokHiz, Mathf.Clamp01(oran));

        void Baslat()
        {
            if (baslatildi)
                return;
            baslatildi = true;
            gazOrani = baslangicGazi;
            if (arkad)
                Hiz = baslangicHizi > 0f ? baslangicHizi : arkadAyar.HedefHiz(gazOrani, false);
            else if (aerodinamik)
            {
                Hiz = baslangicHizi;
                hizVektoru = transform.forward * baslangicHizi;
            }
            else
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
            if (arkad)
            {
                Hiz = arkadAyar.Adim(transform, Hiz, dt, yunuslama, yatis, sapma, yunuslamaTers, gazOrani, Fren, ZeminOlcer.Yukseklik);
                return;
            }
            if (aerodinamik)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(dt / AeroAltAdim - 1e-4f));
                for (int i = 0; i < n; i++)
                    AeroAdim(dt / n);
                return;
            }
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

        // ---- Lab 7: aerodynamic mode --------------------------------------------------------------------------------

        /// <summary>Angle of attack and sideslip (radians) of a world velocity relative to the current attitude.</summary>
        void AkisAcilari(Vector3 v, out float alfa, out float beta)
        {
            Vector3 yerel = transform.InverseTransformDirection(v);
            if (v.sqrMagnitude < 1f)
            {
                alfa = beta = 0f;
                return;
            }
            float ileri = Mathf.Max(yerel.z, 0.1f);
            alfa = Mathf.Atan2(-yerel.y, ileri);
            beta = Mathf.Atan2(yerel.x, ileri);
        }

        /// <summary>
        /// Easy flight pitch (<see cref="UcusYardimi"/>): stick → flight path angle (speed protection, then the external floor
        /// <see cref="EgimAltSiniri"/>) → load factor → angle of attack, inside the stall protection. While upside down the pitch
        /// keys are ignored and the wing is unloaded (n = 0) until the roll law has the wings upright.
        /// </summary>
        float KolayYunuslama(float komut, Vector3 V, float v, float vv, float alt, float ust)
        {
            float gama = v > 1f ? Mathf.Asin(Mathf.Clamp(V.y / v, -1f, 1f)) * Mathf.Rad2Deg : 0f;
            if (transform.up.y <= 0.1f)
            {
                HedefEgim = HedefEgimHam = gama;
                return Mathf.Clamp(aero.GerekenAci(vv, 0f), alt, ust);
            }
            HedefEgimHam = yardim.EgimKomutu(komut, v, aero.TutunmaHizi());
            float hedef = Mathf.Max(Mathf.Min(HedefEgimHam, EgimUstSiniri), EgimAltSiniri);
            HedefEgim = hedef;
            float n = Mathf.Clamp(yardim.YukKomutu(hedef, gama, YatisAcisi, v), aero.enKucukYuk, aero.enBuyukYuk);
            return Mathf.Clamp(aero.GerekenAci(vv, n), alt, ust);
        }

        /// <summary>
        /// One aerodynamic sub-step:
        /// 1. Rotation. The keys move a stick (ramp + expo, <see cref="UcakAerodinamigi.Cubuk"/>), not the aircraft directly.
        ///    Pitch: the stick commands a load factor between the trim and <c>enBuyukYuk</c> / <c>enKucukYuk</c>, i.e. an angle of
        ///    attack, limited below the stall by the protection; the nose follows the flight path's turn rate plus a short-period
        ///    correction towards that angle (stronger with the airspeed). Fast: the g limit binds; slow: the wing does.
        ///    Trim: the load needed for the bank and the climb angle (<c>donusYardimi</c>), so a banked aircraft turns level.
        ///    Roll: the aileron commands a roll rate (pb/2V constant: faster with the airspeed, capped) reached through the roll
        ///    mode's time constant; released, the bank holds, only small banks are levelled by the dihedral.
        ///    Yaw: the fin turns the nose with the flight path and into the airflow; the rudder adds a yaw rate.
        /// 2. Forces on the velocity: lift perpendicular to the flight path in the plane of the wing's up axis, drag against it,
        ///    a side force against sideslip, thrust along the nose, weight. Semi-implicit Euler (velocity, then position).
        /// 3. On the ground the wheels carry the weight, roll with friction (brakes at idle throttle), keep the wings level,
        ///    keep the nose between −1° and +14° and keep the velocity along the heading.
        /// </summary>
        void AeroAdim(float h)
        {
            // Keyboard → stick (aerodynamic mode); yumusakGirdi shows the stick (HUD, tests).
            yumusakGirdi = new Vector3(aero.Cubuk(yumusakGirdi.x, yunuslama, h), aero.Cubuk(yumusakGirdi.y, sapma, h), aero.Cubuk(yumusakGirdi.z, yatis, h));

            Vector3 V = hizVektoru;
            float v = V.magnitude;
            float oran = v / aero.kontrolHizi;
            float otorite = Mathf.Clamp(oran * oran, 0.05f, 1f);
            AkisAcilari(V, out float alfa, out float beta);

            // ---- 1. rotation (degrees per second; nose up, nose right, right wing down positive)
            float komut = aero.Etki(yunuslamaTers ? yumusakGirdi.x : -yumusakGirdi.x);   // +1 = nose up
            float burunYukari, burunSaga, sagaYatis;
            float yunuslamaAcisi = Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 yon0 = v > 0.01f ? V / v : transform.forward;
            if (YerTemasi)
            {
                // Easy flight: with the throttle open the nose rises by itself at take-off speed (one key take-off).
                if (kolayUcus && komut < 0.5f && gazOrani >= 0.5f && v > yardim.KalkisHizi(aero))
                    komut = 0.5f;
                // Elevator works with the airflow (at least 30 % for the rotation); released, the tail settles back to the ground attitude.
                burunYukari = komut * yunuslamaHizi * Mathf.Max(otorite, 0.3f);
                if (Mathf.Abs(komut) < 0.1f)
                    burunYukari -= yunuslamaAcisi * 2f;
                burunSaga = aero.Etki(yumusakGirdi.y) * sapmaHizi * Mathf.Max(otorite, 0.6f);   // tail wheel steering
                sagaYatis = Mathf.Clamp(YatisAcisi * -4f, -60f, 60f);                          // the wheels keep the wings level
                yatisHiziDurum = 0f;
                oncekiYonVar = false;
            }
            else
            {
                // The tail and the fin carry the nose round with the flight path: the aircraft turns by the rotation of its velocity
                // over the last step (all three axes, so a level turn keeps its bank), then the stability terms below only correct
                // the angle of attack and the slip. Capped: a jump of the velocity (a collision, a reset) is not followed in one step.
                if (oncekiYonVar && v > 5f)
                {
                    Quaternion yol = Quaternion.FromToRotation(oncekiYon, yon0);
                    yol.ToAngleAxis(out float yolAcisi, out Vector3 yolEkseni);
                    if (yolAcisi > 180f) yolAcisi -= 360f;
                    if (Mathf.Abs(yolAcisi) > 90f * h)
                        yol = Quaternion.AngleAxis(Mathf.Sign(yolAcisi) * 90f * h, yolEkseni);
                    transform.rotation = yol * transform.rotation;
                    AkisAcilari(V, out alfa, out beta);
                }

                // Trim: the angle of attack for the load the bank and the climb angle need (turn assist), or a fixed trim angle.
                float vv = Mathf.Max(v, 5f);
                float hedefAlfa = aero.trimAcisi * Mathf.Deg2Rad;
                if (transform.up.y > 0.1f)
                {
                    float yatisCos = Mathf.Cos(Mathf.Min(Mathf.Abs(YatisAcisi), 75f) * Mathf.Deg2Rad);
                    float gamaCos = v > 1f ? Mathf.Sqrt(Mathf.Max(0f, 1f - (V.y / v) * (V.y / v))) : 1f;
                    hedefAlfa = Mathf.Lerp(hedefAlfa, aero.GerekenAci(vv, gamaCos / yatisCos), aero.donusYardimi);
                }
                float ust = aero.CekisAcisiSiniri, alt = aero.ItisAcisiSiniri;
                hedefAlfa = Mathf.Clamp(hedefAlfa, alt, ust);
                // Stick → load factor → angle of attack: full back stick asks for enBuyukYuk, limited by the wing.
                float alfaKomut = komut >= 0f
                    ? Mathf.Lerp(hedefAlfa, Mathf.Max(hedefAlfa, Mathf.Min(aero.GerekenAci(vv, aero.enBuyukYuk), ust)), komut)
                    : Mathf.Lerp(hedefAlfa, Mathf.Min(hedefAlfa, Mathf.Max(aero.GerekenAci(vv, aero.enKucukYuk), alt)), -komut);
                if (kolayUcus)
                    alfaKomut = KolayYunuslama(komut, V, v, vv, alt, ust);
                float hizOrani = Mathf.Clamp(oran, 0.4f, 1.8f);
                burunYukari = aero.kisaPeriyot * hizOrani * (alfaKomut - alfa) * Mathf.Rad2Deg;

                // Fin and rudder.
                burunSaga = aero.sapmaKararliligi * Mathf.Clamp(oran, 0.3f, 1.8f) * beta * Mathf.Rad2Deg
                          + aero.Etki(yumusakGirdi.y) * sapmaHizi * Mathf.Clamp(oran, 0.2f, 1.5f);

                // Ailerons: roll rate command (pb/2V), roll mode lag; without aileron the bank holds, small banks level out.
                float yatisKomut = aero.Etki(yumusakGirdi.z);
                float hedefYatisHizi = yatisKomut * aero.YatisHiziSiniri(v);
                if (Mathf.Abs(yatisKomut) < 0.02f && transform.up.y > 0.2f && Mathf.Abs(YatisAcisi) < aero.kanatDuzeltmeBandi)
                    hedefYatisHizi = -YatisAcisi * aero.kanatDuzeltme;
                if (kolayUcus)
                {
                    // Easy flight: the key commands a bank angle; released, the wings level (also from upside down).
                    HedefYatis = float.IsNaN(ZorunluYatis)
                        ? yardim.YatisKomutu(yatisKomut, v, aero, YatisSiniri)
                        : Mathf.Clamp(ZorunluYatis, -yardim.YatisSiniri(v, aero, YatisSiniri), yardim.YatisSiniri(v, aero, YatisSiniri));
                    hedefYatisHizi = yardim.YatisHiziKomutu(HedefYatis, YatisAcisi, aero.YatisHiziSiniri(v));
                }
                yatisHiziDurum = Mathf.Lerp(yatisHiziDurum, hedefYatisHizi, 1f - Mathf.Exp(-h / aero.yatisZamanSabiti));
                sagaYatis = yatisHiziDurum;
                if (enFazlaYatis > 0f && transform.up.y > 0f)
                {
                    float bank = YatisAcisi;
                    float adim = sagaYatis * h;
                    adim = adim > 0f ? Mathf.Min(adim, Mathf.Max(0f, enFazlaYatis - bank)) : Mathf.Max(adim, -Mathf.Max(0f, enFazlaYatis + bank));
                    sagaYatis = adim / h;
                }
            }
            transform.Rotate(-burunYukari * h, burunSaga * h, -sagaYatis * h, Space.Self);
            if (YerTemasi)
            {
                float yeniYunuslama = Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                float sinir = Mathf.Clamp(yeniYunuslama, -1f, 14f);
                if (!Mathf.Approximately(sinir, yeniYunuslama))
                    transform.Rotate(yeniYunuslama - sinir, 0f, 0f, Space.Self);
            }

            // ---- 2. forces
            AkisAcilari(V, out alfa, out beta);
            float cl = aero.KaldirmaKatsayisi(alfa);
            float qs = UcakAerodinamigi.DinamikBasinc(v) * aero.kanatAlani;
            Vector3 yon = v > 0.01f ? V / v : transform.forward;
            Vector3 kaldirmaYonu = Vector3.Cross(yon, transform.right);
            kaldirmaYonu = kaldirmaYonu.sqrMagnitude > 1e-6f ? kaldirmaYonu.normalized : transform.up;
            float kaldirma = qs * cl;
            // Air brake; easy flight: it retracts by itself near the stall speed and the engine helps when the speed runs low.
            float gazEtkin = gazOrani;
            bool havaFreni = Fren && !YerTemasi;
            HizKorumasi = false;
            if (kolayUcus && !YerTemasi)
            {
                float vs = aero.TutunmaHizi();
                havaFreni &= yardim.FrenCalisir(v, vs);
                gazEtkin = Mathf.Max(yardim.EtkinGaz(gazOrani, v, vs), Mathf.Clamp01(GazDestegi));
                HizKorumasi = gazEtkin > gazOrani + 0.01f;
            }
            HavaFreniAcik = havaFreni;
            float cd = aero.SuruklemeKatsayisi(cl, alfa) + (havaFreni ? aero.havaFreni : 0f);
            Vector3 F = kaldirmaYonu * kaldirma
                      - yon * (qs * cd)
                      - transform.right * (qs * aero.yanKuvvet * Mathf.Sin(beta))
                      + transform.forward * aero.Itki(gazEtkin, v)
                      + Vector3.down * aero.Agirlik;

            float normal = 0f;
            if (YerTemasi && F.y < 0f)
            {
                normal = -F.y;   // the wheels carry what the wing does not
                F.y = 0f;
            }
            V += F / aero.kutle * h;

            if (YerTemasi)
            {
                if (V.y < 0f)
                    V.y = 0f;
                Vector3 yatayIleri = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                if (yatayIleri.sqrMagnitude > 1e-4f)
                {
                    yatayIleri.Normalize();
                    var yatay = new Vector3(V.x, 0f, V.z);
                    float ileriHiz = Mathf.Max(0f, Vector3.Dot(yatay, yatayIleri));          // no rolling backwards
                    Vector3 yanal = yatay - yatayIleri * Vector3.Dot(yatay, yatayIleri);
                    float mu = gazOrani < 0.05f || Fren ? aero.frenSurtunmesi : aero.yuvarlanmaSurtunmesi;
                    ileriHiz = Mathf.Max(0f, ileriHiz - mu * normal / aero.kutle * h);       // friction cannot reverse the motion
                    yanal *= Mathf.Exp(-8f * h);                                             // tyres: sideways skid dies out
                    V = new Vector3(0f, V.y, 0f) + yatayIleri * ileriHiz + yanal;
                }
            }

            transform.position += V * h;
            hizVektoru = V;
            if (!YerTemasi && V.sqrMagnitude > 1f)
            {
                oncekiYon = yon0;   // the path direction at the start of this step: next step's turn rate = change over one step
                oncekiYonVar = true;
            }
            Hiz = V.magnitude;
            HucumAcisi = alfa * Mathf.Rad2Deg;
            YanKayma = beta * Mathf.Rad2Deg;
            YukFaktoru = kaldirma / aero.Agirlik;
            TutunmaKaybi = !YerTemasi && v > 5f && Mathf.Abs(alfa) > aero.TutunmaAcisiRad;
        }
    }
}
