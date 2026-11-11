using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 5: a light anti-aircraft position. Aim source: the eye (<see cref="Radar"/>: cone + line of sight) gives the
    /// true position; without it, the shared ear (<see cref="SesDinleyici"/>) gives the delayed, heard position;
    /// with neither it does not fire. It aims at that point with no lead, so a crossing target is missed and a target
    /// diving straight at the gun is easy to track. Scatter by the target's height above the ground (period report:
    /// "exceedingly accurate" below 1,500 ft, poor at 0–50 ft): 15–460 m no scatter, outside the band
    /// <c>bandDisiSacilma</c> (above 460 m × 0.7).
    /// </summary>
    public class UcaksavarMevzii : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucak;
        [SerializeField] Radar goz;
        [SerializeField] SesDinleyici kulak;
        [Header("Gece (Lab 7)")]
        [Tooltip("Night: the eye counts only while one of these searchlights holds the aircraft in its beam, or inside geceGorusu. " +
                 "Empty = daylight (Labs 5–6): the eye alone is enough.")]
        [SerializeField] Isildak[] isildaklar;
        [Tooltip("Night: distance (m) within which the crew sees the aircraft without a searchlight.")]
        [SerializeField, Min(0f)] float geceGorusu = 250f;
        [Header("Atış")]
        [SerializeField] Mermi mermiOn;
        [Tooltip("Muzzle; also turned towards the aim point.")]
        [SerializeField] Transform namlu;
        [Tooltip("Round speed (m/s). Worksheet: 70 → 260.")]
        [SerializeField, Min(1f)] float mermiHizi = 260f;
        [SerializeField, Min(0)] int mermiHasari = 8;
        [Tooltip("Seconds between rounds.")]
        [SerializeField, Min(0.02f)] float atisAraligi = 0.3f;
        [Tooltip("Does not fire at aim points farther than this (m).")]
        [SerializeField, Min(0f)] float menzil = 1400f;
        [Tooltip("Layers the rounds can hit.")]
        [SerializeField] LayerMask vurulacak;
        [Tooltip("false = the rounds use the broken end-point check (Step 03 experiment).")]
        [SerializeField] bool supurme = true;
        [Tooltip("Lab 8: does not fire at an aircraft more than this many metres above the gun; 0 = no ceiling (Labs 5–7).")]
        [SerializeField, Min(0f)] float atesTavani;
        [Tooltip("Lab 8: side of the rounds (Notr = Labs 5–7).")]
        [SerializeField] Taraf taraf = Taraf.Notr;
        [Header("İrtifa bandı (m, yerden)")]
        [SerializeField, Min(0f)] float cokAlcak = 15f;
        [SerializeField, Min(0f)] float etkiliTavan = 460f;
        [Tooltip("Scatter half-angle outside the band (degrees).")]
        [SerializeField, Min(0f)] float bandDisiSacilma = 6f;

        float sonrakiAtis;
        System.Random rastgele;

        public UcusKontrol Ucak { get => ucak; set => ucak = value; }
        public Radar Goz { get => goz; set => goz = value; }
        public SesDinleyici Kulak { get => kulak; set => kulak = value; }
        public float MermiHizi { get => mermiHizi; set => mermiHizi = Mathf.Max(1f, value); }
        public bool Supurme { get => supurme; set => supurme = value; }
        public float AtesTavani { get => atesTavani; set => atesTavani = Mathf.Max(0f, value); }
        public Taraf Taraf { get => taraf; set => taraf = value; }
        public float Menzil { get => menzil; set => menzil = Mathf.Max(0f, value); }
        /// <summary>Lab 8: the aircraft is above the ceiling (the gun holds fire).</summary>
        public bool TavanUstunde => atesTavani > 0f && ucak != null && ucak.transform.position.y - transform.position.y > atesTavani;
        /// <summary>Lab 8: the gun sees the aircraft in range under its ceiling (it is firing or about to).</summary>
        public bool AtesEdiyor => isActiveAndEnabled && VeriVar && !TavanUstunde &&
                                  (Nisan - transform.position).sqrMagnitude <= menzil * menzil;

        public bool GozleGoruyor => goz != null && goz.Goruyor && (isildaklar == null || isildaklar.Length == 0 || Aydinlatiliyor());

        /// <summary>Night rule: a searchlight holds the aircraft, or the aircraft is within <c>geceGorusu</c>.</summary>
        bool Aydinlatiliyor()
        {
            foreach (Isildak i in isildaklar)
                if (i != null && i.Yakaladi)
                    return true;
            return ucak != null && (ucak.transform.position - transform.position).sqrMagnitude <= geceGorusu * geceGorusu;
        }

        public Isildak[] Isildaklar { get => isildaklar; set => isildaklar = value; }
        public bool KulaklaDuyuyor => kulak != null && kulak.Duyuyor;
        public bool VeriVar { get; private set; }
        public Vector3 Nisan { get; private set; }
        /// <summary>Target height above the ground at the last update (m).</summary>
        public float Irtifa { get; private set; }
        public int AtisSayisi { get; private set; }

        /// <summary>Scatter half-angle (degrees) for a target <paramref name="irtifa"/> metres above the ground.</summary>
        public float Sacilma(float irtifa) => Sacilma(irtifa, cokAlcak, etkiliTavan, bandDisiSacilma);

        public static float Sacilma(float irtifa, float cokAlcak, float etkiliTavan, float bandDisiSacilma)
        {
            if (irtifa < cokAlcak) return bandDisiSacilma;          // 15 m altı: kör
            if (irtifa > etkiliTavan) return bandDisiSacilma * 0.7f; // 460 m üstü
            return 0f;                                               // ölüm bandı
        }

        [Tooltip("Seed of the scatter random numbers (repeatable runs).")]
        [SerializeField] int tohum = 1;

        void Awake() => rastgele = new System.Random(tohum);

        void Update()
        {
            if (ucak == null || !ucak.enabled)
            {
                VeriVar = false;
                return;
            }

            // Nişan kaynağı: gözü varsa gerçek konum, yoksa kulağın geç duyduğu konum.
            VeriVar = GozleGoruyor || KulaklaDuyuyor;
            Nisan = GozleGoruyor ? ucak.transform.position     // göz: anlık
                  : KulaklaDuyuyor ? kulak.DuyulanKonum        // kulak: gecikmeli
                  : Vector3.zero;
            if (!VeriVar)
                return;

            Irtifa = ZeminOlcer.Irtifa(ucak.transform.position);
            Vector3 cikis = namlu != null ? namlu.position : transform.position + Vector3.up * 2f;
            Vector3 fark = Nisan - cikis;
            if (namlu != null && fark.sqrMagnitude > 1f)
                namlu.rotation = Quaternion.LookRotation(fark);
            if (fark.magnitude > menzil || TavanUstunde || Time.time < sonrakiAtis)
                return;
            sonrakiAtis = Time.time + atisAraligi;
            Ates(cikis, fark.normalized);
        }

        void Ates(Vector3 cikis, Vector3 yon)
        {
            if (mermiOn == null)
                return;
            float sacilma = Sacilma(Irtifa);
            if (sacilma > 0f)
                yon = Sac(yon, sacilma);
            Mermi m = Instantiate(mermiOn, cikis, Quaternion.LookRotation(yon));
            m.Ates(yon, mermiHizi, mermiHasari, vurulacak, supurme, taraf);
            AtisSayisi++;
        }

        /// <summary>Random direction inside a cone of half-angle <paramref name="derece"/> around <paramref name="yon"/>.</summary>
        Vector3 Sac(Vector3 yon, float derece)
        {
            float aci = (float)rastgele.NextDouble() * derece;
            float donus = (float)rastgele.NextDouble() * 360f;
            Vector3 dik = Vector3.Cross(yon, Mathf.Abs(yon.y) < 0.95f ? Vector3.up : Vector3.right).normalized;
            dik = Quaternion.AngleAxis(donus, yon) * dik;
            return Quaternion.AngleAxis(aci, dik) * yon;
        }
    }
}
