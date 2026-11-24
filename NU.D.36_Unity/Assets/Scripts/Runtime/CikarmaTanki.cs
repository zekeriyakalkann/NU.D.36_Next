using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5): a tank carried by a landing ship (worksheet: "gemiden yalnız araç iniyor"). It rides on the well deck
    /// until the ship releases it, then drives off:
    /// <list type="number">
    /// <item>along the deck to the bow (slowly) and down the ramp, pitched with it; where the ramp meets the surf the water
    /// splashes;</item>
    /// <item>onto the wet sand — out of the water, it has landed (<see cref="KarayaCikti"/>, the worksheet's count);</item>
    /// <item>up the beach and inland to its parking spot like a vehicle: it speeds up, steers in curves towards each point,
    /// follows the ground (pitch and roll from the slope) and raises dust; it slows down and stops at its spot.</item>
    /// </list>
    /// Unarmed: the worksheet only counts the landing. No crew model.
    /// </summary>
    public class CikarmaTanki : MonoBehaviour
    {
        public enum Durum { Gemide, Iniyor, Karada, Park }

        [Tooltip("Speeds (m/s): along the deck, down the ramp, on land; acceleration (m/s²).")]
        [SerializeField, Min(0.5f)] float guverteHizi = 3.5f;
        [SerializeField, Min(0.5f)] float rampaHizi = 2.2f;
        [SerializeField, Min(0.5f)] float karaHizi = 6f;
        [SerializeField, Min(0.1f)] float ivme = 2f;
        [Tooltip("Steering rate on land (°/s).")]
        [SerializeField, Min(5f)] float donusHizi = 40f;
        [Tooltip("Dust behind the tracks on land (child, world simulation space).")]
        [SerializeField] ParticleSystem toz;
        [Tooltip("Water splash where the tank leaves the ramp into the surf (prefab, removes itself).")]
        [SerializeField] GameObject sicramaOn;

        Vector3[] yol = new Vector3[0];
        int sira, karaSirasi;
        float yon, hiz, egim, yatma;

        public Durum Simdiki { get; private set; } = Durum.Gemide;
        public bool KaradaMi => Simdiki == Durum.Karada || Simdiki == Durum.Park;
        public float Hiz => hiz;
        public float KaraHizi => karaHizi;
        /// <summary>Pitch of the hull (°, nose down positive), smoothed.</summary>
        public float Egim => egim;
        public bool TozCikiyor => toz != null && toz.emission.enabled;
        public event Action<CikarmaTanki> KarayaCikti;

        void Awake()
        {
            if (toz != null)
            {
                var e = toz.emission;
                e.enabled = false;
            }
        }

        /// <summary>
        /// Starts unloading through <paramref name="noktalar"/> (world): points up to <paramref name="kara"/> are followed in a
        /// straight line (deck edge, ramp tip, wet sand); reaching <paramref name="kara"/> is the landing; the rest is driven
        /// over the ground. Point 1 is taken as the ramp tip (splash).
        /// </summary>
        public void IndirmeyeBasla(Vector3[] noktalar, int kara)
        {
            if (Simdiki != Durum.Gemide) return;
            yol = noktalar;
            karaSirasi = Mathf.Clamp(kara, 0, noktalar.Length - 1);
            sira = 0;
            hiz = 0.5f;
            transform.SetParent(null, true);
            // Off the ship: a body of its own so its colliders move as a kinematic body, not as moved static colliders.
            if (!TryGetComponent(out Rigidbody rb))
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            yon = transform.eulerAngles.y;
            egim = 0f;
            Simdiki = Durum.Iniyor;
            if (toz != null) toz.Play(true);
        }

        void Update()
        {
            if (Time.deltaTime > 0f) Adim(Time.deltaTime);
        }

        static float Zemin(Vector3 p) => ZeminOlcer.Yukseklik(p.x, p.z);

        /// <summary>One step of the drive (public for tests).</summary>
        public void Adim(float dt)
        {
            if (Simdiki == Durum.Gemide || Simdiki == Durum.Park || sira >= yol.Length)
                return;
            Vector3 p = transform.position;
            float k = 1f - Mathf.Exp(-6f * dt);

            if (sira <= karaSirasi)
            {
                // Deck, ramp, out of the surf: straight lines, pitched along the way.
                float istenen = sira == 0 ? guverteHizi : rampaHizi;
                hiz = Mathf.MoveTowards(hiz, istenen, ivme * dt);
                Vector3 d = yol[sira] - p;
                float adim = hiz * dt;
                Vector3 yatay = new Vector3(d.x, 0f, d.z);
                if (yatay.sqrMagnitude > 0.01f)
                    yon = Mathf.MoveTowardsAngle(yon, Mathf.Atan2(yatay.x, yatay.z) * Mathf.Rad2Deg, 120f * dt);
                float hedefEgim = d.sqrMagnitude > 0.01f ? -Mathf.Asin(Mathf.Clamp(d.normalized.y, -1f, 1f)) * Mathf.Rad2Deg : 0f;   // nose down on the ramp
                egim = Mathf.Lerp(egim, hedefEgim, k);
                if (d.magnitude <= adim)
                {
                    p = yol[sira];
                    if (sira == 1) Sicrat(p);
                    if (sira == karaSirasi)
                    {
                        Simdiki = Durum.Karada;
                        KarayaCikti?.Invoke(this);
                    }
                    sira++;
                }
                else
                    p += d.normalized * adim;
                yatma = Mathf.Lerp(yatma, 0f, k);
            }
            else
            {
                // On land: a vehicle steering towards the next point, slowing for the last one.
                Vector3 hedef = yol[sira];
                Vector3 d = new Vector3(hedef.x - p.x, 0f, hedef.z - p.z);
                bool son = sira == yol.Length - 1;
                float mesafe = d.magnitude;
                if (d.sqrMagnitude > 0.01f)
                    yon = Mathf.MoveTowardsAngle(yon, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, donusHizi * dt);
                float istenen = son ? Mathf.Min(karaHizi, 0.6f * mesafe) : karaHizi;
                // Slow down in a sharp turn.
                float aci = Mathf.Abs(Mathf.DeltaAngle(yon, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg));
                istenen *= Mathf.Lerp(1f, 0.45f, Mathf.InverseLerp(20f, 90f, aci));
                hiz = Mathf.MoveTowards(hiz, istenen, ivme * dt);
                Vector3 ileri = Quaternion.Euler(0f, yon, 0f) * Vector3.forward;
                p += ileri * (hiz * dt);
                if (mesafe < (son ? 0.6f : 6f))
                {
                    sira++;
                    if (sira >= yol.Length)
                    {
                        Simdiki = Durum.Park;
                        hiz = 0f;
                    }
                }
                // Sit on the ground: height under the centre, pitch and roll from the slope under the tracks.
                Vector3 sag = Quaternion.Euler(0f, yon, 0f) * Vector3.right;
                p.y = Zemin(p);
                float on = Zemin(p + ileri * 3f), arka = Zemin(p - ileri * 3f);
                float sg = Zemin(p + sag * 1.7f), sl = Zemin(p - sag * 1.7f);
                egim = Mathf.Lerp(egim, -Mathf.Atan2(on - arka, 6f) * Mathf.Rad2Deg, k);
                yatma = Mathf.Lerp(yatma, Mathf.Atan2(sg - sl, 3.4f) * Mathf.Rad2Deg, k);
            }

            if (toz != null)
            {
                var e = toz.emission;
                e.enabled = KaradaMi && Simdiki != Durum.Park && hiz > 1f;
            }
            transform.SetPositionAndRotation(p, Quaternion.Euler(egim, yon, yatma));
        }

        void Sicrat(Vector3 p)
        {
            if (sicramaOn != null && p.y < 0.8f)
                Instantiate(sicramaOn, new Vector3(p.x, Mathf.Max(p.y, 0f), p.z), Quaternion.identity);
        }
    }
}
