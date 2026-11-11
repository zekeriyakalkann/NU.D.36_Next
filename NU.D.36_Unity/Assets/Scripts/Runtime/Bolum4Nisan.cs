using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4), user decision 4 ("rahat uçuş ve hedefi kolay vurma"): targeting and firing assists for the escort.
    ///   • Target: the nearest live fire source of an awake zone, weighted towards the one ahead; T picks the next one by
    ///     distance and keeps it until it is silenced. A tank pair is one bomb target (aim point = its middle); AA guns and
    ///     enemy aircraft are machine-gun targets.
    ///   • Firing assist (O toggles, on at the start):
    ///       – machine gun: when a machine-gun target is within ±<see cref="koni"/>° of the nose and <see cref="mgMenzil"/>
    ///         m, the rounds go to its lead point and the guns fire by themselves (no rounds wasted on empty sky);
    ///       – bomb: a bomb released while a tank pair is locked flies to the pair's middle; locked within
    ///         <see cref="bombaMenzil"/> m and under the release ceiling, the system releases one by itself.
    /// The worksheet rules stay: 1 200 rounds, 2 bombs, no release above 300 m, tanks only by bombs.
    /// </summary>
    public class Bolum4Nisan : MonoBehaviour
    {
        public sealed class Hedef
        {
            public Susturulabilir Kaynak;          // for a tank pair: the first live tank
            public readonly List<Susturulabilir> Cift = new List<Susturulabilir>();
            public bool Bomba => Kaynak != null && Kaynak.Tur == DusmanTuru.Tank;
            public bool Canli => Kaynak != null && !Kaynak.Sustu && Kaynak.gameObject.activeInHierarchy;
            public Vector3 Nokta
            {
                get
                {
                    if (!Bomba) return Kaynak.transform.position + Vector3.up * (Kaynak.Tur == DusmanTuru.Ucaksavar ? 1.5f : 0f);
                    Vector3 t = Vector3.zero; int n = 0;
                    foreach (var s in Cift) if (s != null && !s.Sustu) { t += s.transform.position; n++; }
                    return n > 0 ? t / n : Kaynak.transform.position;   // on the ground between the two tanks
                }
            }
            public Vector3 Hiz => Kaynak != null && Kaynak.TryGetComponent(out DusmanUcagi u) && u.Aktif ? u.HizVektoru : Vector3.zero;
            public string Ad => Bomba ? "TANK ÇİFTİ" : Kaynak.Tur == DusmanTuru.Ucaksavar ? "UÇAKSAVAR" : "DÜŞMAN UÇAĞI";
        }

        [SerializeField] UcusKontrol ucus;
        [SerializeField] Makineli makineli;
        [SerializeField] BombaYuku yuk;
        [SerializeField] bool yardim = true;
        [Tooltip("Machine-gun assist cone (half-angle, degrees) and range (m).")]
        [SerializeField, Range(1f, 30f)] float koni = 6f;
        [SerializeField, Min(50f)] float mgMenzil = 900f;
        [Tooltip("Bomb lock: range (m) and half-angle off the flight direction (degrees); automatic release range (m).")]
        [SerializeField, Min(50f)] float kilitMenzili = 1600f;
        [SerializeField, Range(5f, 180f)] float kilitAcisi = 60f;
        [SerializeField, Min(50f)] float bombaMenzil = 900f;
        [SerializeField, Min(0.1f)] float bombaAraligi = 2f;
        [SerializeField] InputAction hedefTusu = new InputAction("HedefDegistir", InputActionType.Button, "<Keyboard>/t");
        [SerializeField] InputAction yardimTusu = new InputAction("AtisYardimi", InputActionType.Button, "<Keyboard>/o");

        readonly List<Hedef> adaylar = new List<Hedef>();
        Hedef elleSecilen;
        Bomba yoldaki;
        float sonBomba = -100f;

        public bool Yardim { get => yardim; set => yardim = value; }
        public Hedef Secili { get; private set; }
        public float Mesafe { get; private set; }
        public bool Kilitli { get; private set; }
        public bool Menzilde { get; private set; }
        /// <summary>Lead point of the selected machine-gun target (where to aim), or null.</summary>
        public Vector3? OncuNokta { get; private set; }
        /// <summary>A machine-gun target inside the assist cone right now.</summary>
        public bool KonideHedef { get; private set; }
        public IReadOnlyList<Hedef> Adaylar => adaylar;
        public int OtomatikBomba { get; private set; }

        public string Durum
        {
            get
            {
                if (Secili == null) return "";
                if (Secili.Bomba)
                {
                    if (yoldaki != null && !yoldaki.Patladi) return "BOMBA YOLDA";
                    if (yuk != null && yuk.Kalan == 0) return "BOMBA YOK";
                    if (Kilitli && yuk != null && yuk.CokYuksek) return "KİLİTLİ — ALÇAL (300 m)";
                    if (Menzilde) return yardim ? "MENZİLDE — ATEŞ" : "MENZİLDE — Space";
                    if (Kilitli) return "KİLİTLİ";
                    return "BURNU HEDEFE ÇEVİR";
                }
                if (makineli != null && makineli.Bos) return "MERMİ YOK";
                if (KonideHedef) return yardim ? "NİŞANDA — ATEŞ" : "NİŞANDA — Ctrl";
                return Mesafe <= mgMenzil ? "NİŞAN HALKASINI HEDEFE GETİR" : "YAKLAŞ";
            }
        }

        void Awake()
        {
            if (ucus == null) ucus = GetComponent<UcusKontrol>();
            if (makineli == null) makineli = GetComponent<Makineli>();
            if (yuk == null) yuk = GetComponent<BombaYuku>();
        }

        void OnEnable()
        {
            hedefTusu.Enable();
            yardimTusu.Enable();
            if (makineli != null)
            {
                makineli.NisanYonu = NisanYonu;
                makineli.OtomatikTetik = () => yardim && KonideHedef;
            }
            if (yuk != null) yuk.BombaBirakildi += Birakildi;
        }

        void OnDisable()
        {
            hedefTusu.Disable();
            yardimTusu.Disable();
            if (makineli != null) { makineli.NisanYonu = null; makineli.OtomatikTetik = null; }
            if (yuk != null) yuk.BombaBirakildi -= Birakildi;
        }

        /// <summary>Live targets of the awake zones: one per tank pair, one per AA gun and enemy aircraft.</summary>
        void AdaylariTopla()
        {
            adaylar.Clear();
            foreach (AtesBolgesi b in AtesBolgesi.Hepsi)
            {
                if (b.Simdiki == AtesBolgesi.Durum.Bekliyor) continue;
                Hedef cift = null;
                foreach (Susturulabilir s in b.Kaynaklar())
                {
                    if (s.Sustu || !s.gameObject.activeInHierarchy) continue;
                    if (s.Tur == DusmanTuru.Tank)
                    {
                        if (cift == null) { cift = new Hedef { Kaynak = s }; adaylar.Add(cift); }
                        cift.Cift.Add(s);
                    }
                    else if (s.Tur != DusmanTuru.Ucak || s.GetComponent<DusmanUcagi>().Aktif)
                        adaylar.Add(new Hedef { Kaynak = s });
                }
            }
        }

        static Hedef Ayni(List<Hedef> l, Hedef h)
        {
            if (h == null) return null;
            foreach (var a in l) if (a.Kaynak == h.Kaynak || (a.Bomba && h.Bomba && a.Cift.Contains(h.Kaynak))) return a;
            return null;
        }

        void Update()
        {
            if (Time.timeScale > 0f && yardimTusu.WasPressedThisFrame()) yardim = !yardim;
            AdaylariTopla();
            Vector3 p = transform.position;
            Vector3 ileri = ucus != null && ucus.Hiz > 1f ? ucus.HizVektoru.normalized : transform.forward;

            elleSecilen = Ayni(adaylar, elleSecilen);
            if (Time.timeScale > 0f && hedefTusu.WasPressedThisFrame() && adaylar.Count > 0)
            {
                adaylar.Sort((a, b) => (a.Nokta - p).sqrMagnitude.CompareTo((b.Nokta - p).sqrMagnitude));
                int i = Secili != null ? adaylar.IndexOf(Ayni(adaylar, Secili)) : -1;
                elleSecilen = adaylar[(i + 1) % adaylar.Count];
            }
            Secili = elleSecilen ?? EnIyi(p, ileri);

            OncuNokta = null;
            KonideHedef = false;
            Kilitli = Menzilde = false;
            if (Secili == null) { Mesafe = 0f; return; }
            Vector3 n = Secili.Nokta;
            Mesafe = Vector3.Distance(p, n);

            // Machine gun: the selected target, or any other machine-gun target that sits in the cone.
            Hedef mg = !Secili.Bomba ? Secili : null;
            Hedef konide = KonideBul(p, transform.forward);
            if (konide != null) mg = konide;
            if (mg != null)
            {
                OncuNokta = Tank.OncuNokta(p, mg.Nokta, mg.Hiz, MermiHizi());
                KonideHedef = konide != null;
            }

            if (Secili.Bomba)
            {
                Vector3 d = n - p; d.y = 0f;
                Vector3 yatay = new Vector3(ileri.x, 0f, ileri.z);
                Kilitli = Mesafe <= kilitMenzili && Vector3.Angle(yatay, d) <= kilitAcisi;
                Menzilde = Kilitli && d.magnitude <= bombaMenzil;
                bool yoldaVar = yoldaki != null && !yoldaki.Patladi;
                if (yardim && Menzilde && !yoldaVar && yuk != null && yuk.Kalan > 0 && !yuk.CokYuksek &&
                    Time.time - sonBomba >= bombaAraligi && (ucus == null || ucus.enabled))
                {
                    if (yuk.Birak() != null) OtomatikBomba++;
                }
            }
        }

        float MermiHizi() => (makineli != null ? makineli.MermiHizi : 600f) + (ucus != null ? ucus.Hiz : 0f);

        Hedef KonideBul(Vector3 p, Vector3 burun)
        {
            Hedef enIyi = null;
            float enIyiAci = koni;
            foreach (Hedef h in adaylar)
            {
                if (h.Bomba) continue;
                Vector3 oncu = Tank.OncuNokta(p, h.Nokta, h.Hiz, MermiHizi());
                Vector3 d = oncu - p;
                if (d.sqrMagnitude > mgMenzil * mgMenzil) continue;
                float aci = Vector3.Angle(burun, d);
                if (aci <= enIyiAci && !Physics.Linecast(p, h.Nokta, ZeminOlcer.AraziMaskesi)) { enIyiAci = aci; enIyi = h; }
            }
            return enIyi;
        }

        Hedef EnIyi(Vector3 p, Vector3 ileri)
        {
            Hedef enIyi = null;
            float enIyiPuan = float.MaxValue;
            foreach (Hedef h in adaylar)
            {
                Vector3 d = h.Nokta - p;
                float puan = d.magnitude * (1.6f - 0.6f * Vector3.Dot(ileri, d.normalized));   // ahead counts as nearer
                if (h.Bomba && yuk != null && yuk.Kalan == 0) puan *= 4f;
                if (!h.Bomba && makineli != null && makineli.Bos) puan *= 4f;
                if (puan < enIyiPuan) { enIyiPuan = puan; enIyi = h; }
            }
            return enIyi;
        }

        /// <summary>Machine-gun assist: the direction to the lead point of the target in the cone (null = along the nose).</summary>
        Vector3? NisanYonu(Vector3 namlu)
        {
            if (!yardim || !KonideHedef || OncuNokta == null) return null;
            return (OncuNokta.Value - namlu).normalized;
        }

        void Birakildi(Bomba b)
        {
            sonBomba = Time.time;
            if (Secili != null && Secili.Bomba && Kilitli)
            {
                Hedef h = Secili;
                b.Gudumle(() => h.Nokta);
                yoldaki = b;
            }
        }
    }
}
