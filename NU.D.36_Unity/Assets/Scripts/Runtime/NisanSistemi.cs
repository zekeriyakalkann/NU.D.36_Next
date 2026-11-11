using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NU36
{
    /// <summary>
    /// Targeting assist of the night mission (user request 2026-11-10: "menzile girdiğimde kuleyi otomatik vuracak şekilde").
    ///   • Target: the standing target (searchlight tower or bridge) that is nearest, weighted towards the one ahead; T picks
    ///     the next one by distance and keeps it until it falls.
    ///   • Lock: the target is within <c>kilitMenzili</c> and within ±<c>kilitAcisi</c> of the flight direction (horizontal),
    ///     the terrain does not hide it (line of sight, the Hafta 5 rule), the aircraft is flying.
    ///   • Release: a bomb dropped while locked (Space) is guided (<see cref="Bomba.Gudumle"/>). With the automatic release on
    ///     (O toggles) the system drops one by itself as soon as the locked target is within <c>atisMenzili</c>, one bomb per
    ///     target in flight, at most one every <c>atisAraligi</c> seconds.
    /// The bomb count, the resupply and the bomb itself stay the Lab 5/7 ones (<see cref="BombaYuku"/>).
    /// </summary>
    public class NisanSistemi : MonoBehaviour
    {
        [SerializeField] GeceGorevi gorev;
        [SerializeField] BombaYuku yuk;
        [SerializeField] UcusKontrol ucus;
        [Tooltip("Distance at which a target ahead is locked (m).")]
        [SerializeField, Min(50f)] float kilitMenzili = 1600f;
        [Tooltip("Distance at which the automatic release drops the guided bomb (m).")]
        [SerializeField, Min(50f)] float atisMenzili = 1200f;
        [Tooltip("Half-angle off the flight direction (horizontal, degrees) inside which a target can be locked.")]
        [SerializeField, Range(5f, 180f)] float kilitAcisi = 60f;
        [SerializeField] bool otomatikAtis = true;
        [Tooltip("Least time between two automatic releases (s).")]
        [SerializeField, Min(0.1f)] float atisAraligi = 2f;
        [SerializeField] InputAction hedefTusu = new InputAction("HedefDegistir", InputActionType.Button, "<Keyboard>/t");
        [SerializeField] InputAction otomatikTusu = new InputAction("OtomatikAtis", InputActionType.Button, "<Keyboard>/o");

        readonly Dictionary<Yikilabilir, Bomba> ucustaki = new Dictionary<Yikilabilir, Bomba>();
        readonly List<Yikilabilir> adaylar = new List<Yikilabilir>();
        Yikilabilir elleSecilen;
        float sonAtis = -100f;

        /// <summary>Raised when a guided bomb leaves the aircraft (bomb, target).</summary>
        public event Action<Bomba, Yikilabilir> GudumluAtildi;

        /// <summary>The selected target (null when every target is down).</summary>
        public Yikilabilir Hedef { get; private set; }
        /// <summary>The selected target is locked: within the lock range and ahead.</summary>
        public bool Kilitli { get; private set; }
        /// <summary>Locked and within the release range.</summary>
        public bool Menzilde { get; private set; }
        /// <summary>Distance to the selected target (m).</summary>
        public float Mesafe { get; private set; }
        /// <summary>Bearing of the selected target relative to the flight direction (degrees, positive = right).</summary>
        public float GoreceliYon { get; private set; }
        /// <summary>Compass bearing to the selected target (degrees).</summary>
        public float Kerteriz { get; private set; }
        /// <summary>No terrain between the aircraft and the selected target.</summary>
        public bool GorusVar { get; private set; }

        /// <summary>Short state of the selected target for the HUD and the GPS.</summary>
        public string Durum
        {
            get
            {
                if (Hedef == null) return "";
                if (UcustaBombaVar(Hedef)) return "BOMBA YOLDA";
                if (Menzilde) return otomatikAtis ? "MENZİLDE — ATEŞ" : "MENZİLDE — Space";
                if (Kilitli) return "KİLİTLİ";
                if (Mesafe <= kilitMenzili && !GorusVar) return "GÖRÜŞ YOK (arazi)";
                if (Mesafe <= kilitMenzili && Mathf.Abs(GoreceliYon) > kilitAcisi) return "BURNU HEDEFE ÇEVİR";
                return "HEDEF (T)";
            }
        }
        /// <summary>True when the target was picked with T (kept until it falls).</summary>
        public bool ElleSecildi => elleSecilen != null;
        public bool OtomatikAtis { get => otomatikAtis; set => otomatikAtis = value; }
        public float KilitMenzili => kilitMenzili;
        public float AtisMenzili => atisMenzili;
        public int GudumluSayisi { get; private set; }
        public GeceGorevi Gorev { get => gorev; set => gorev = value; }

        void Awake()
        {
            if (ucus == null) ucus = GetComponent<UcusKontrol>();
            if (yuk == null) yuk = GetComponent<BombaYuku>();
        }

        void OnEnable()
        {
            hedefTusu.Enable();
            otomatikTusu.Enable();
            if (yuk != null)
            {
                yuk.GudumHedefi = GudumHedefiVer;
                yuk.BombaBirakildi += Birakildi;
            }
        }

        void OnDisable()
        {
            hedefTusu.Disable();
            otomatikTusu.Disable();
            if (yuk != null)
            {
                if (yuk.GudumHedefi == (Func<Yikilabilir>)GudumHedefiVer)
                    yuk.GudumHedefi = null;
                yuk.BombaBirakildi -= Birakildi;
            }
            Kilitli = Menzilde = false;
        }

        Yikilabilir GudumHedefiVer() => Kilitli ? Hedef : null;

        void Update()
        {
            bool oyunda = Time.timeScale > 0f;
            if (oyunda && hedefTusu.WasPressedThisFrame())
                SonrakiHedef();
            if (oyunda && otomatikTusu.WasPressedThisFrame())
            {
                otomatikAtis = !otomatikAtis;
                if (gorev != null)
                    gorev.Bildir($"Otomatik atış {(otomatikAtis ? "AÇIK" : "KAPALI")}");
            }
            Guncelle();
            if (oyunda && OtomatikBirakmali())
                yuk.Birak();
        }

        Vector3 UcakKonumu => ucus != null ? ucus.transform.position : transform.position;

        /// <summary>Flight direction on the horizontal plane (the nose when nearly stopped).</summary>
        Vector3 UcusYonu()
        {
            Vector3 v = ucus != null ? ucus.HizVektoru : Vector3.zero;
            v.y = 0f;
            if (v.sqrMagnitude < 4f)
                v = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }

        void AdaylariTopla()
        {
            adaylar.Clear();
            if (gorev != null && gorev.Hedefler != null)
                foreach (Yikilabilir h in gorev.Hedefler)
                    if (h != null && !h.Yikik)
                        adaylar.Add(h);
        }

        /// <summary>Score of a target for the automatic choice: distance, doubled for a target behind.</summary>
        float Puan(Yikilabilir h, Vector3 p, Vector3 ileri)
        {
            Vector3 d = h.IsaretNoktasi - p;
            float aci = Vector3.Angle(ileri, new Vector3(d.x, 0f, d.z));
            return d.magnitude * (1f + aci / 180f);
        }

        /// <summary>Picks the next standing target by distance (T). It is kept until it falls.</summary>
        public void SonrakiHedef()
        {
            AdaylariTopla();
            if (adaylar.Count == 0)
                return;
            Vector3 p = UcakKonumu;
            adaylar.Sort((a, b) => (a.IsaretNoktasi - p).sqrMagnitude.CompareTo((b.IsaretNoktasi - p).sqrMagnitude));
            int i = Hedef != null ? adaylar.IndexOf(Hedef) : -1;
            elleSecilen = adaylar[(i + 1) % adaylar.Count];
            Guncelle();
            if (gorev != null)
                gorev.Bildir($"Hedef: {elleSecilen.GorunenAd} ({Mesafe / 1000f:F1} km)");
        }

        /// <summary>Updates the selection, the lock and the read-outs (also called by tests).</summary>
        public void Guncelle()
        {
            AdaylariTopla();
            if (elleSecilen != null && elleSecilen.Yikik)
                elleSecilen = null;
            Vector3 p = UcakKonumu;
            Vector3 ileri = UcusYonu();
            Yikilabilir secilen = elleSecilen;
            if (secilen == null)
            {
                // Automatic: the best score; the current target is kept unless another is clearly better (no flicker).
                float enIyi = float.MaxValue;
                foreach (Yikilabilir h in adaylar)
                {
                    float s = Puan(h, p, ileri);
                    if (s < enIyi) { enIyi = s; secilen = h; }
                }
                if (Hedef != null && !Hedef.Yikik && secilen != Hedef && Puan(Hedef, p, ileri) < enIyi * 1.25f)
                    secilen = Hedef;
            }
            Hedef = secilen;
            if (Hedef == null)
            {
                Kilitli = Menzilde = false;
                Mesafe = 0f;
                return;
            }
            Vector3 d = Hedef.IsaretNoktasi - p;
            Mesafe = d.magnitude;
            var yatay = new Vector3(d.x, 0f, d.z);
            GoreceliYon = yatay.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(ileri, yatay, Vector3.up) : 0f;
            Kerteriz = Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f);
            bool ucuyor = ucus == null || (ucus.enabled && !ucus.YerTemasi);
            int arazi = ZeminOlcer.AraziMaskesi;
            GorusVar = arazi == 0 || !Physics.Linecast(p, Hedef.IsaretNoktasi, arazi, QueryTriggerInteraction.Ignore);
            Kilitli = ucuyor && GorusVar && Mesafe <= kilitMenzili && Mathf.Abs(GoreceliYon) <= kilitAcisi && (gorev == null || !gorev.Bitti);
            Menzilde = Kilitli && Mesafe <= atisMenzili;
        }

        /// <summary>A guided bomb is still flying to <paramref name="h"/>.</summary>
        public bool UcustaBombaVar(Yikilabilir h) => h != null && ucustaki.TryGetValue(h, out Bomba b) && b != null && !b.Patladi;

        bool OtomatikBirakmali() =>
            otomatikAtis && Menzilde && yuk != null && yuk.Kalan > 0 && Time.time >= sonAtis + atisAraligi && !UcustaBombaVar(Hedef);

        void Birakildi(Bomba b)
        {
            if (b == null || !b.Gudumlu)
                return;
            ucustaki[b.GudumHedefi] = b;
            sonAtis = Time.time;
            GudumluSayisi++;
            if (gorev != null)
                gorev.Bildir($"Güdümlü bomba atıldı → {b.GudumHedefi.GorunenAd} ({Mesafe / 1000f:F1} km)");
            GudumluAtildi?.Invoke(b, b.GudumHedefi);
        }
    }
}
