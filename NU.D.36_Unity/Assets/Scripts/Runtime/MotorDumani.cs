using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4), worksheet 07: thin, half-transparent black smoke from behind the engine; the lower the aircraft's
    /// health, the denser and darker it gets (the worksheet's extra point), and below 30 % it turns thick. The particle
    /// system is a child at the exhaust, simulated in world space so the smoke stays behind as a trail.
    /// </summary>
    public class MotorDumani : MonoBehaviour
    {
        [SerializeField] Saglik saglik;
        [SerializeField] ParticleSystem duman;
        [Header("Tam can → can yok")]
        [SerializeField] Vector2 yayin = new Vector2(10f, 60f);
        [SerializeField] Vector2 opaklik = new Vector2(0.22f, 0.85f);
        [SerializeField] Vector2 boyut = new Vector2(0.6f, 2.6f);
        [Tooltip("Below this share of health the smoke turns thick.")]
        [SerializeField, Range(0f, 1f)] float kalinEsik = 0.3f;

        public ParticleSystem Duman => duman;
        /// <summary>0 (full health) … 1 (no health).</summary>
        public float Hasar { get; private set; }
        public float Yayin { get; private set; }
        public float Opaklik { get; private set; }
        public float Boyut { get; private set; }

        void Awake()
        {
            if (saglik == null) saglik = GetComponentInParent<Saglik>();
        }

        void OnEnable()
        {
            if (saglik != null) saglik.CanDegisti += Guncelle;
            Uygula(0f);
        }

        void OnDisable()
        {
            if (saglik != null) saglik.CanDegisti -= Guncelle;
        }

        void Guncelle(int can) => Uygula(1f - can / (float)Mathf.Max(1, saglik.BaslangicCani));

        /// <summary>Emission rate, opacity and size for a damage share 0…1 (pure; tested).</summary>
        public static (float yayin, float opaklik, float boyut) Degerler(float hasar, Vector2 yayin, Vector2 opaklik, Vector2 boyut, float kalinEsik)
        {
            float h = Mathf.Clamp01(hasar);
            // Thick below the threshold: the curve steepens once health drops under kalinEsik.
            float t = h < 1f - kalinEsik ? h * 0.5f / (1f - kalinEsik) : 0.5f + 0.5f * (h - (1f - kalinEsik)) / Mathf.Max(0.01f, kalinEsik);
            return (Mathf.Lerp(yayin.x, yayin.y, t), Mathf.Lerp(opaklik.x, opaklik.y, t), Mathf.Lerp(boyut.x, boyut.y, t));
        }

        void Uygula(float hasar)
        {
            Hasar = Mathf.Clamp01(hasar);
            (Yayin, Opaklik, Boyut) = Degerler(Hasar, yayin, opaklik, boyut, kalinEsik);
            if (duman == null) return;
            var e = duman.emission;
            e.rateOverTime = Yayin;
            var ana = duman.main;
            ana.startColor = new Color(0.06f, 0.06f, 0.06f, Opaklik);
            ana.startSize = new ParticleSystem.MinMaxCurve(Boyut * 0.8f, Boyut * 1.2f);
            if (!duman.isPlaying) duman.Play();
        }
    }
}
