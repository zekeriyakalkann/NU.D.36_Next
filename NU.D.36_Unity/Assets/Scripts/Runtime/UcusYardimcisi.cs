using UnityEngine;
using UnityEngine.InputSystem;

namespace NU36
{
    /// <summary>
    /// Flight assistant of the night mission (user request 2026-11-10: "fren mekanizması ve daha rahat bir uçuş").
    /// Keys (own InputActions, the shared "Ucus" map is not changed; ignored while paused):
    ///   F or B (hold) — brake: the air brake in flight, the wheel brakes on the ground (<see cref="UcusKontrol.Fren"/>);
    ///   K — easy flight on/off (<see cref="UcusKontrol.KolayUcus"/>).
    /// While easy flight is on it sets the external limits of the law every frame, before UcusKontrol moves:
    ///   • terrain floor: the flight path may not aim lower than what keeps <c>korumaYuksekligi</c> above the terrain (and
    ///     <c>yapiPayi</c> above towers, the bridge and houses) over the next <c>ongoruSuresi</c> seconds. In the airfield box
    ///     (runway ± approach) with the throttle below <c>kalkisGazi</c> the floor is the ground itself over the runway (5 m under
    ///     it) and <c>pistDisiPayi</c> elsewhere, so the wheels touch down only on the runway. Ahead of the aircraft only ground
    ///     that rises above the floor here constrains the path;
    ///   • sink envelope: the descent rate near the ground is limited to enAzDusus + √(2·frenIvmesi·height), so the aircraft
    ///     flares by itself and touches down softly; faster than <c>inisHizi</c> below <c>suzulmeYuksekligi</c> it does not
    ///     descend at all (it floats while it slows, the brake helps);
    ///   • automatic touchdown: over the runway below <c>otomatikInisYuksekligi</c> (landing, throttle below kalkisGazi) the
    ///     path is also held under a ceiling of <c>inisTakipOrani</c> × the envelope's sink, so the aircraft settles onto the
    ///     runway by itself instead of floating along it (the speed protection would keep it flying);
    ///   • full power while the floor asks for a climb (<see cref="UcusKontrol.GazDestegi"/>);
    ///   • map edge: beyond <c>sinirYaricapi</c> from the centre, flying outwards, the aircraft turns back by itself
    ///     (<see cref="UcusKontrol.ZorunluYatis"/>) until it points inwards;
    ///   • bank limit near the ground.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class UcusYardimcisi : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [SerializeField] InisAlgilayici algilayici;
        [SerializeField] InputAction frenTusu = Tus("Fren", "<Keyboard>/f", "<Keyboard>/b");
        [SerializeField] InputAction kolayTusu = Tus("KolayUcus", "<Keyboard>/k");

        [Header("Yer koruması")]
        [Tooltip("Height kept above the terrain outside the airfield box (m).")]
        [SerializeField, Min(0f)] float korumaYuksekligi = 50f;
        [Tooltip("Look-ahead along the flight path (s) ...")]
        [SerializeField, Min(0.5f)] float ongoruSuresi = 6f;
        [Tooltip("... but at least this far (m), so a slow aircraft still sees a hill in time.")]
        [SerializeField, Min(0f)] float enAzOngoru = 400f;
        [Tooltip("The floor climbs back to the protected height over at least this time (s): gentle, not a jump.")]
        [SerializeField, Min(0.5f)] float toparlanmaSuresi = 3f;
        [Tooltip("Steepest climb the floor may ask for (degrees).")]
        [SerializeField, Range(1f, 45f)] float enFazlaKorumaTirmanisi = 25f;
        [Tooltip("Outside the airfield box the floor also clears what the aircraft can crash into (towers, the bridge, houses). " +
                 "Empty = the crash probe's layers (CarpismaAlgilayici).")]
        [SerializeField] LayerMask yapilar;
        [Tooltip("Height kept above those structures (m).")]
        [SerializeField, Min(0f)] float yapiPayi = 25f;

        [Header("İniş yardımı")]
        [Tooltip("Sink rate allowed at the ground (m/s).")]
        [SerializeField, Min(0.1f)] float enAzDusus = 1f;
        [Tooltip("Deceleration of the descent the envelope allows (m/s²): sink ≤ enAzDusus + √(2·a·height).")]
        [SerializeField, Min(0.1f)] float frenIvmesi = 2f;
        [Tooltip("Below this height (m above the wheels) the aircraft does not descend while faster than inisHizi.")]
        [SerializeField, Min(0f)] float suzulmeYuksekligi = 15f;
        [Tooltip("Highest touchdown speed the assist lets the aircraft land with (m/s): a little under the landing limit.")]
        [SerializeField, Min(1f)] float inisHizi = 57f;
        [Tooltip("Largest bank below yerYatisYuksekligi (degrees).")]
        [SerializeField, Range(1f, 90f)] float yerYatisSiniri = 10f;
        [SerializeField, Min(0f)] float yerYatisYuksekligi = 20f;
        [Tooltip("Over the runway below this height (m above the wheels) the aircraft settles onto it by itself.")]
        [SerializeField, Min(0f)] float otomatikInisYuksekligi = 15f;
        [Tooltip("Share of the envelope's sink rate the automatic touchdown descends at, at least.")]
        [SerializeField, Range(0.1f, 1f)] float inisTakipOrani = 0.7f;

        [Header("Harita sınırı")]
        [Tooltip("Centre of the play area (the paper map).")]
        [SerializeField] Vector3 haritaMerkezi;
        [Tooltip("Beyond this distance from the centre (m), an aircraft flying outwards is turned back by itself (the mountains " +
                 "closing the map start at 2800 m). 0 = off.")]
        [SerializeField, Min(0f)] float sinirYaricapi = 2300f;
        [Tooltip("Bank of that turn (degrees; less when the wing cannot carry it).")]
        [SerializeField, Range(5f, 80f)] float sinirYatisi = 50f;

        [Header("Havaalanı kutusu (iniş serbest)")]
        [SerializeField] Vector3 pistBasi;
        [SerializeField] Vector3 pistSonu;
        [Tooltip("Half-width of the box around the runway centreline (m).")]
        [SerializeField, Min(0f)] float alanYariGenisligi = 150f;
        [Tooltip("Length of the box beyond each runway end (approach and climb-out, m).")]
        [SerializeField, Min(0f)] float yaklasmaBoyu = 2500f;
        [Tooltip("In the box, a throttle at or above this means take-off or go-around: the terrain floor stays on.")]
        [SerializeField, Range(0f, 1f)] float kalkisGazi = 0.7f;
        [Tooltip("Runway width (m).")]
        [SerializeField, Min(1f)] float pistEni = 40f;
        [Tooltip("The wheels may touch down only over the runway: up to this far beside its edge (m) ...")]
        [SerializeField, Min(0f)] float pistYanPayi = 25f;
        [Tooltip("... and from this far before either threshold (m).")]
        [SerializeField, Min(0f)] float esikPayi = 150f;
        [Tooltip("Elsewhere in the box the aircraft is held this high above the ground (m) until it is lined up over the runway.")]
        [SerializeField, Min(0f)] float pistDisiPayi = 10f;

        static readonly Vector3[] Ornek = new Vector3[8];
        /// <summary>Floor over the runway while landing (m, below the ground: the wheels reach it in a few seconds).</summary>
        const float PistPayi = -5f;

        public UcusKontrol Ucus { get => ucus; set => ucus = value; }
        /// <summary>Brake held by code (tests, an autopilot), in addition to the key.</summary>
        public bool FrenTutuluyor { get; set; }
        /// <summary>Height of the wheels above the ground (m), this frame.</summary>
        public float Agl { get; private set; }
        /// <summary>True while the terrain floor lifts the commanded path (the aircraft is being pulled up).</summary>
        public bool YerKorumasi { get; private set; }
        /// <summary>True while the sink envelope or the float rule limits the descent near the ground.</summary>
        public bool InisYardimi { get; private set; }
        /// <summary>The aircraft is inside the airfield box.</summary>
        public bool HavaalanindaMi { get; private set; }
        /// <summary>Landing in the box but not over the runway: held <c>pistDisiPayi</c> above the ground until lined up.</summary>
        public bool PisteHizalan { get; private set; }
        /// <summary>Settling onto the runway by itself (automatic touchdown).</summary>
        public bool OtomatikInis { get; private set; }
        /// <summary>Turning back at the map edge by itself.</summary>
        public bool SinirDonusu { get; private set; }
        public Vector3 HaritaMerkezi { get => haritaMerkezi; set => haritaMerkezi = value; }
        public float SinirYaricapi => sinirYaricapi;
        public Vector3 PistBasi { get => pistBasi; set => pistBasi = value; }
        public Vector3 PistSonu { get => pistSonu; set => pistSonu = value; }

        static InputAction Tus(string ad, params string[] yollar)
        {
            var a = new InputAction(ad, InputActionType.Button, yollar[0]);
            for (int i = 1; i < yollar.Length; i++)
                a.AddBinding(yollar[i]);
            return a;
        }

        void Awake()
        {
            if (ucus == null) ucus = GetComponent<UcusKontrol>();
            if (algilayici == null) algilayici = GetComponent<InisAlgilayici>();
            if (yapilar.value == 0 && TryGetComponent(out CarpismaAlgilayici c))
                yapilar = c.Engeller;
        }

        /// <summary>
        /// Height (m) the floor must keep above at <paramref name="q"/>: the terrain + <paramref name="pay"/>, and with
        /// <paramref name="yapiDahil"/> the top of any structure within <paramref name="yaricap"/> m + <c>yapiPayi</c>.
        /// </summary>
        float Engel(Vector3 q, float pay, bool yapiDahil, float yaricap)
        {
            float h = ZeminOlcer.Yukseklik(q.x, q.z) + pay;
            if (yapiDahil && yapilar.value != 0 &&
                Physics.SphereCast(new Vector3(q.x, 3000f, q.z), yaricap, Vector3.down, out RaycastHit v, 6000f, yapilar, QueryTriggerInteraction.Ignore))
                h = Mathf.Max(h, v.point.y + yapiPayi);
            return h;
        }

        void OnEnable()
        {
            frenTusu.Enable();
            kolayTusu.Enable();
        }

        void OnDisable()
        {
            frenTusu.Disable();
            kolayTusu.Disable();
            if (ucus != null)
            {
                ucus.Fren = false;
                ucus.EgimAltSiniri = -90f;
                ucus.EgimUstSiniri = 90f;
                ucus.YatisSiniri = 180f;
                ucus.GazDestegi = 0f;
                ucus.ZorunluYatis = float.NaN;
            }
            YerKorumasi = InisYardimi = PisteHizalan = OtomatikInis = SinirDonusu = false;
        }

        void Update()
        {
            if (ucus == null)
                return;
            bool oyunda = Time.timeScale > 0f;
            if (oyunda && kolayTusu.WasPressedThisFrame())
                ucus.SonrakiMod();   // Rota07: easy flight on/off; with the arcade choice (Bölüm 4): arcade → easy → realistic
            ucus.Fren = FrenTutuluyor || (oyunda && frenTusu.IsPressed());
            Hesapla();
        }

        /// <summary>True inside the box along the runway (both ends extended by <c>yaklasmaBoyu</c>, ± <c>alanYariGenisligi</c>).</summary>
        public bool Havaalaninda(Vector3 p) => PisteGore(p, alanYariGenisligi, yaklasmaBoyu);

        /// <summary>True over the runway, where the wheels may touch down (± its half-width + <c>pistYanPayi</c>, from <c>esikPayi</c> before a threshold).</summary>
        public bool PistUstunde(Vector3 p) => PisteGore(p, pistEni / 2f + pistYanPayi, esikPayi);

        bool PisteGore(Vector3 p, float yanSinir, float uzanti)
        {
            Vector3 eksen = pistSonu - pistBasi;
            eksen.y = 0f;
            float boy = eksen.magnitude;
            if (boy < 1f)
                return false;
            eksen /= boy;
            Vector3 d = p - pistBasi;
            d.y = 0f;
            float boyuna = Vector3.Dot(d, eksen);
            float yana = Mathf.Abs(Vector3.Dot(d, new Vector3(-eksen.z, 0f, eksen.x)));
            return yana <= yanSinir && boyuna >= -uzanti && boyuna <= boy + uzanti;
        }

        /// <summary>Sink rate (m/s) the envelope allows at <paramref name="yukseklik"/> m above the ground.</summary>
        public float IzinliDusus(float yukseklik) => enAzDusus + Mathf.Sqrt(2f * frenIvmesi * Mathf.Max(0f, yukseklik));

        /// <summary>Computes this frame's limits and hands them to the flight model (also called by tests).</summary>
        public void Hesapla()
        {
            Vector3 p = ucus.transform.position;
            float teker = algilayici != null ? algilayici.TekerOfseti : 0f;
            Agl = p.y - teker - ZeminOlcer.Yukseklik(p.x, p.z);
            HavaalanindaMi = Havaalaninda(p);
            float alt = -90f, ust = 90f, yatisSiniri = 180f, destek = 0f, zorunlu = float.NaN;
            bool koruma = false, inis = false, hizalan = false, otomatik = false, sinir = false;

            if (ucus.KolayUcus && ucus.Aerodinamik && ucus.enabled && !ucus.YerTemasi)
            {
                Vector3 V = ucus.HizVektoru;
                float v = V.magnitude;
                var yatay = new Vector3(V.x, 0f, V.z);
                float vh = yatay.magnitude;
                if (v > 5f)
                {
                    // Floor over the look-ahead: the steepest angle needed to stay `pay` metres above the ground (and the
                    // structures) ahead. Landing in the airfield box: the ground itself over the runway, pistDisiPayi elsewhere.
                    bool alanda = HavaalanindaMi && ucus.GazOrani < kalkisGazi;
                    Vector3 yon = vh > 1f ? yatay / vh : Vector3.ProjectOnPlane(ucus.transform.forward, Vector3.up).normalized;
                    // Here: back to (or down to) the floor over toparlanmaSuresi. Ahead: only what rises above the floor here
                    // constrains the path (flat or falling ground ahead must not hold the aircraft up).
                    float enAz = -90f, burada = 0f;
                    float aralik = Mathf.Max(vh * ongoruSuresi, enAzOngoru) / (Ornek.Length - 1);
                    for (int i = 0; i < Ornek.Length; i++)
                    {
                        float d = aralik * i;
                        Vector3 q = p + yon * d;
                        bool pistte = alanda && PistUstunde(q);
                        float pay = !alanda ? korumaYuksekligi : pistte ? PistPayi : pistDisiPayi;
                        float zemin = Engel(q, pay, !pistte, aralik * 0.5f + 10f) + teker;
                        if (i == 0)
                            burada = zemin;
                        else if (zemin <= burada + 1f)
                            continue;
                        float mesafe = Mathf.Max(d, vh * toparlanmaSuresi, 1f);
                        enAz = Mathf.Max(enAz, Mathf.Atan2(zemin - p.y, mesafe) * Mathf.Rad2Deg);
                    }
                    alt = Mathf.Min(enAz, enFazlaKorumaTirmanisi);
                    koruma = alt > ucus.HedefEgimHam + 0.5f && !alanda;
                    hizalan = alanda && !PistUstunde(p) && Agl < pistDisiPayi + 15f;
                    if (koruma && alt > 2f)
                        destek = 1f;   // the floor climbs: full power

                    // Sink envelope near the ground (flare) and the float rule.
                    float izin = Agl < suzulmeYuksekligi && v > inisHizi ? 0f : IzinliDusus(Agl);
                    float zarf = -Mathf.Asin(Mathf.Clamp01(izin / v)) * Mathf.Rad2Deg;
                    if (zarf > alt)
                    {
                        inis = zarf > ucus.HedefEgimHam + 0.5f;
                        alt = zarf;
                    }

                    // Automatic touchdown over the runway.
                    if (alanda && PistUstunde(p) && Agl < otomatikInisYuksekligi)
                    {
                        ust = -Mathf.Asin(Mathf.Clamp01(inisTakipOrani * IzinliDusus(Agl) / v)) * Mathf.Rad2Deg;
                        otomatik = ust >= alt;
                    }
                }
                if (Agl < yerYatisYuksekligi)
                    yatisSiniri = yerYatisSiniri;

                // Map edge: turn back towards the centre, the short way round, until within 15° of it.
                Vector3 r = p - haritaMerkezi;
                r.y = 0f;
                if (sinirYaricapi > 0f && vh > 1f && r.magnitude > sinirYaricapi)
                {
                    float hata = Vector3.SignedAngle(yatay, -r, Vector3.up);   // + = the centre is to the right
                    if (Mathf.Abs(hata) > 90f || (SinirDonusu && Mathf.Abs(hata) > 15f))
                    {
                        zorunlu = Mathf.Sign(hata) * sinirYatisi;
                        sinir = true;
                    }
                }
            }
            ucus.EgimAltSiniri = alt;
            ucus.EgimUstSiniri = ust;
            ucus.YatisSiniri = yatisSiniri;
            ucus.GazDestegi = destek;
            ucus.ZorunluYatis = zorunlu;
            YerKorumasi = koruma;
            InisYardimi = inis && !koruma;
            PisteHizalan = hizalan;
            OtomatikInis = otomatik;
            SinirDonusu = sinir;
        }
    }
}
