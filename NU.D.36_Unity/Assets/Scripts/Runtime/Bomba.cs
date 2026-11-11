using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 5, Step 04: a free-falling bomb. At release it takes the aircraft's velocity; after that only gravity acts
    /// (no thrust, no guidance). Integration: semi-implicit (symplectic) Euler, velocity first, then position, with a
    /// ray sweep over each step so a fast bomb cannot pass through the ground. On impact it explodes
    /// (<see cref="Patlama.Yarat"/>). Released below <c>parcaGuvenliIrtifa</c> (120 m), the aircraft is still inside
    /// its own bomb's fragment zone: it takes <c>parcaHasari</c> and the Console says so.
    /// Lab 7 assist (user request 2026-11-10): <see cref="Gudumle"/> turns it into a guided bomb that flies to a target at
    /// <c>gudumHizi</c>, turning at most <c>gudumDonusu</c> degrees per second (straight at it on the last stretch), without
    /// gravity. A guided bomb hurts its own aircraft only if that is within <c>parcaGuvenliIrtifa</c> of the explosion.
    /// When its target falls before it arrives, it falls freely again.
    /// </summary>
    public class Bomba : MonoBehaviour
    {
        [Tooltip("Layers the bomb stops on (terrain, structures, targets).")]
        [SerializeField] LayerMask carpacak;
        [Tooltip("Layers the blast damages.")]
        [SerializeField] LayerMask etkilenen;
        [SerializeField, Min(0f)] float patlamaYaricapi = 25f;
        [SerializeField, Min(0)] int hasar = 100;
        [SerializeField] Patlama patlamaOn;
        [Header("Kendi parça tesiri")]
        [Tooltip("Release height above the ground below which the aircraft is caught by its own fragments (m).")]
        [SerializeField, Min(0f)] float parcaGuvenliIrtifa = 120f;
        [SerializeField, Min(0)] int parcaHasari = 40;
        [SerializeField, Min(0.1f)] float omur = 30f;
        [Header("Güdüm (Lab 7 nişan yardımı)")]
        [Tooltip("Speed the guided bomb accelerates to (m/s).")]
        [SerializeField, Min(1f)] float gudumHizi = 160f;
        [Tooltip("Acceleration towards that speed (m/s²).")]
        [SerializeField, Min(0f)] float gudumIvmesi = 60f;
        [Tooltip("Largest turn rate of the guided bomb (degrees/s).")]
        [SerializeField, Min(1f)] float gudumDonusu = 150f;
        [Tooltip("Top attack: the guided bomb aims this share of the remaining horizontal distance above the target (capped by " +
                 "enFazlaYay), so it stays high and dives onto the target at the end instead of skimming the ground.")]
        [SerializeField, Range(0f, 1f)] float gudumYayi = 0.3f;
        [SerializeField, Min(0f)] float enFazlaYay = 120f;

        Vector3 hiz;
        float yas;
        Saglik sahip;

        public Vector3 Hiz => hiz;
        public float BirakmaIrtifasi { get; private set; }
        public bool Patladi { get; private set; }
        public Vector3 PatlamaNoktasi { get; private set; }
        public float PatlamaYaricapi => patlamaYaricapi;
        public float ParcaGuvenliIrtifa => parcaGuvenliIrtifa;
        /// <summary>The target a guided bomb flies to (null = free fall).</summary>
        public Yikilabilir GudumHedefi { get; private set; }
        public bool Gudumlu => GudumHedefi != null || gudumNoktasi != null;
        /// <summary>Lab 8: guidance to a point (e.g. the middle of a tank pair) instead of a destroyable target.</summary>
        System.Func<Vector3> gudumNoktasi;

        /// <summary>Makes this a guided bomb flying to <paramref name="hedef"/> (null = free fall).</summary>
        public void Gudumle(Yikilabilir hedef) => GudumHedefi = hedef;

        /// <summary>Lab 8: guided flight to a point that may move (read every step).</summary>
        public void Gudumle(System.Func<Vector3> nokta) => gudumNoktasi = nokta;

        /// <summary>Raised once on impact with the explosion point.</summary>
        public event Action<Bomba, Vector3> Patlayinca;

        /// <summary>Starts the fall. <paramref name="baslangicHizi"/> is the aircraft's velocity at the moment of release.</summary>
        public void Birak(Vector3 baslangicHizi, float birakmaIrtifasi, Saglik ucakSagligi)
        {
            hiz = baslangicHizi;
            BirakmaIrtifasi = birakmaIrtifasi;
            sahip = ucakSagligi;
            if (hiz.sqrMagnitude > 1e-6f)
                transform.forward = hiz.normalized;
        }

        void FixedUpdate()
        {
            yas += Time.fixedDeltaTime;
            if (yas > omur)
            {
                Destroy(gameObject);
                return;
            }
            Adim(Time.fixedDeltaTime);
        }

        /// <summary>One physics step. Returns true when the bomb hit something and exploded.</summary>
        public bool Adim(float dt)
        {
            if (Patladi)
                return true;

            // Lab 8 point guidance: proximity fuse — the point may sit on open ground between targets, so explode when
            // this step would reach it (otherwise the bomb could circle a point it never touches).
            if (gudumNoktasi != null)
            {
                Vector3 hedefNokta = gudumNoktasi();
                if ((hedefNokta - transform.position).sqrMagnitude <= Mathf.Max(2f, hiz.magnitude * dt) * Mathf.Max(2f, hiz.magnitude * dt))
                {
                    Patla(hedefNokta);
                    return true;
                }
            }

            if ((GudumHedefi != null && !GudumHedefi.Yikik) || gudumNoktasi != null)
                Yonlen(dt);
            else
                // Yarı örtük (symplectic) Euler: ÖNCE hızı güncelle, SONRA konumu.
                // Açık Euler'de düşen cisim her adımda biraz geride kalır (4. hafta).
                hiz += Physics.gravity * dt;

            Vector3 adim = hiz * dt;

            if (Physics.Raycast(transform.position, adim.normalized, out RaycastHit vurus,
                    adim.magnitude, carpacak, QueryTriggerInteraction.Ignore))
            {
                Patla(vurus.point);
                return true;
            }

            transform.position += adim;
            transform.forward = adim.normalized;        // burnu yola döner
            return false;
        }

        /// <summary>Guided flight: speed up towards gudumHizi and turn the velocity to the target (straight at it when close).</summary>
        void Yonlen(float dt)
        {
            Vector3 fark = (GudumHedefi != null ? GudumHedefi.IsaretNoktasi : gudumNoktasi()) - transform.position;
            float d = fark.magnitude;
            if (d < 1e-3f)
                return;
            float hizBuyuk = Mathf.MoveTowards(hiz.magnitude, gudumHizi, gudumIvmesi * dt);
            // Aim above the target while far (top attack), straight at it on the last stretch.
            float yatay = new Vector2(fark.x, fark.z).magnitude;
            Vector3 nisan = fark + Vector3.up * Mathf.Min(yatay * gudumYayi, enFazlaYay);
            Vector3 istenen = d < hizBuyuk * 0.6f ? fark / d : nisan.normalized;
            Vector3 yon = hiz.sqrMagnitude > 1e-4f ? hiz.normalized : istenen;
            yon = d < hizBuyuk * 0.6f ? istenen : Vector3.RotateTowards(yon, istenen, gudumDonusu * Mathf.Deg2Rad * dt, 0f);
            hiz = yon * Mathf.Max(hizBuyuk, 1f);
        }

        void Patla(Vector3 nokta)
        {
            Patladi = true;
            PatlamaNoktasi = nokta;
            Patlama.Yarat(nokta, patlamaYaricapi, hasar, etkilenen, patlamaOn, sahip != null ? sahip.Taraf : Taraf.Notr);
            if (Gudumlu)
            {
                // A guided bomb explodes far from its aircraft: only an aircraft inside the fragment zone is hurt.
                if (sahip != null && Vector3.Distance(sahip.transform.position, nokta) < parcaGuvenliIrtifa)
                    sahip.Vur(parcaHasari);
            }
            else if (BirakmaIrtifasi < parcaGuvenliIrtifa)
            {
                Debug.LogWarning($"[Bomba] {BirakmaIrtifasi:F0} m'den bırakıldı (< {parcaGuvenliIrtifa:F0} m): uçak kendi bombasının parça tesirine yakalandı, {parcaHasari} hasar.");
                if (sahip != null)
                    sahip.Vur(parcaHasari);
            }
            Patlayinca?.Invoke(this, nokta);
            Destroy(gameObject);
        }

        // ---- formulas (worksheet Step 04) ------------------------------------------------------------

        /// <summary>
        /// Impact point found by running the same integration forward ("run the simulation once more"), without the
        /// physics engine: the ground comes from <paramref name="zemin"/>. Returns false if no impact within
        /// <paramref name="enFazlaSure"/> seconds.
        /// </summary>
        public static bool DususNoktasi(Vector3 konum, Vector3 hiz, Vector3 yercekimi, float dt, Func<float, float, float> zemin,
            out Vector3 nokta, out float sure, float enFazlaSure = 30f)
        {
            sure = 0f;
            Vector3 p = konum, v = hiz;
            float onceki = p.y - zemin(p.x, p.z);
            while (sure < enFazlaSure)
            {
                v += yercekimi * dt;
                Vector3 yeni = p + v * dt;
                float yukseklik = yeni.y - zemin(yeni.x, yeni.z);
                sure += dt;
                if (yukseklik <= 0f)
                {
                    float oran = onceki / Mathf.Max(onceki - yukseklik, 1e-6f); // linear interpolation inside the step
                    nokta = Vector3.Lerp(p, yeni, Mathf.Clamp01(oran));
                    return true;
                }
                p = yeni;
                onceki = yukseklik;
            }
            nokta = p;
            return false;
        }

        /// <summary>Fall time from height h at speed v, dive angle θ below the horizon: t = (−v·sinθ + √(v²sin²θ + 2gh)) / g.</summary>
        public static float DususSuresi(float v, float dalisDerece, float h, float g = 9.81f)
        {
            float vs = v * Mathf.Sin(dalisDerece * Mathf.Deg2Rad);
            return (-vs + Mathf.Sqrt(vs * vs + 2f * g * h)) / g;
        }

        /// <summary>Forward travel of the bomb: x = v·cosθ·t.</summary>
        public static float IleriMenzil(float v, float dalisDerece, float h, float g = 9.81f) =>
            v * Mathf.Cos(dalisDerece * Mathf.Deg2Rad) * DususSuresi(v, dalisDerece, h, g);

        /// <summary>Pull-out radius r = v² / (g·n).</summary>
        public static float ToparlanmaYaricapi(float v, float n, float g = 9.81f) => v * v / (g * n);

        /// <summary>Height lost while pulling out of a θ dive: Δh = r·(1 − cosθ).</summary>
        public static float ToparlanmaKaybi(float v, float n, float dalisDerece, float g = 9.81f) =>
            ToparlanmaYaricapi(v, n, g) * (1f - Mathf.Cos(dalisDerece * Mathf.Deg2Rad));
    }
}
