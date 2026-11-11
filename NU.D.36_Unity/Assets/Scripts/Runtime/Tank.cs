using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4), worksheet 04: a tank by the line that fires at the train. The turret turns to the nearest vehicle;
    /// within range it fires a tracer shell every few seconds, aimed where the vehicle will be when the shell arrives (the
    /// train's speed along the line), with a small scatter. Only explosions hurt it (its <see cref="Saglik"/> is set to
    /// "explosions only"); <see cref="Susturulabilir"/> switches this component off.
    /// </summary>
    public class Tank : MonoBehaviour
    {
        [SerializeField] Tren tren;
        [Tooltip("Turns about its vertical axis to the target (child pivot over the turret ring).")]
        [SerializeField] Transform kule;
        [Tooltip("Muzzle point (child of the turret).")]
        [SerializeField] Transform namluUcu;
        [SerializeField] Mermi mermiOn;
        [SerializeField] GameObject namluAlevi;
        [SerializeField, Min(1f)] float menzil = 900f;
        [SerializeField, Min(0.1f)] float atisAraligi = 5f;
        [SerializeField, Min(1f)] float mermiHizi = 320f;
        [SerializeField, Min(0)] int hasar = 2;
        [Tooltip("Scatter half-angle (degrees).")]
        [SerializeField, Min(0f)] float sacilma = 0.35f;
        [SerializeField, Min(1f)] float donusHizi = 40f;
        [SerializeField] LayerMask vurulacak;
        [SerializeField] Taraf taraf = Taraf.Dusman;
        [SerializeField] int tohum = 1;

        float sonrakiAtis;
        System.Random rastgele;

        public Tren Tren { get => tren; set => tren = value; }
        public float Menzil => menzil;
        public int AtisSayisi { get; private set; }
        public Vector3 SonNisan { get; private set; }

        void Awake()
        {
            rastgele = new System.Random(tohum);
        }

        void OnEnable()
        {
            // First shot a little after showing up, staggered between the two tanks of a pair.
            sonrakiAtis = Time.time + 1.5f + (float)rastgele.NextDouble() * 2f;
        }

        /// <summary>Middle of the train vehicle nearest to <paramref name="p"/> and the train's velocity there.</summary>
        public static bool HedefBul(Tren t, Vector3 p, out Vector3 hedef, out Vector3 hiz)
        {
            hedef = hiz = Vector3.zero;
            if (t == null || t.Araclar == null || t.Araclar.Length == 0 || t.Hat == null)
                return false;
            float enIyi = float.MaxValue;
            int enIyiI = -1;
            for (int i = 0; i < t.Araclar.Length; i++)
            {
                if (t.Araclar[i] == null) continue;
                float d = (t.Araclar[i].transform.position - p).sqrMagnitude;
                if (d < enIyi) { enIyi = d; enIyiI = i; }
            }
            if (enIyiI < 0) return false;
            float s = t.AracKonumu(enIyiI);
            hedef = t.Araclar[enIyiI].transform.position + Vector3.up * 1.5f;
            hiz = t.Hat.Egri.Yon(s) * t.Hiz;
            return true;
        }

        /// <summary>Aim point for a shell of speed <paramref name="v"/> from <paramref name="cikis"/> at a target moving at
        /// <paramref name="hedefHizi"/> (two fixed-point steps; enough for a slow train).</summary>
        public static Vector3 OncuNokta(Vector3 cikis, Vector3 hedef, Vector3 hedefHizi, float v)
        {
            Vector3 n = hedef;
            for (int i = 0; i < 3; i++)
                n = hedef + hedefHizi * (Vector3.Distance(cikis, n) / v);
            return n;
        }

        void Update()
        {
            if (tren == null || tren.Vuruldu || tren.Vardi)
                return;
            Vector3 cikis = namluUcu != null ? namluUcu.position : transform.position + Vector3.up * 2.5f;
            if (!HedefBul(tren, cikis, out Vector3 hedef, out Vector3 hiz))
                return;
            Vector3 nisan = OncuNokta(cikis, hedef, hiz, mermiHizi);
            SonNisan = nisan;

            if (kule != null)
            {
                Vector3 d = nisan - kule.position;
                d.y = 0f;
                if (d.sqrMagnitude > 1f)
                    kule.rotation = Quaternion.RotateTowards(kule.rotation, Quaternion.LookRotation(d, Vector3.up), donusHizi * Time.deltaTime);
            }

            if ((nisan - cikis).sqrMagnitude > menzil * menzil || Time.time < sonrakiAtis)
                return;
            sonrakiAtis = Time.time + atisAraligi;
            Ates(cikis, nisan);
        }

        void Ates(Vector3 cikis, Vector3 nisan)
        {
            if (mermiOn == null)
                return;
            Vector3 yon = Sac((nisan - cikis).normalized, sacilma, rastgele);
            Mermi m = Instantiate(mermiOn, cikis, Quaternion.LookRotation(yon));
            m.Ates(yon, mermiHizi, hasar, vurulacak, true, taraf);
            m.Omur = menzil * 1.3f / mermiHizi;
            if (namluAlevi != null)
                Instantiate(namluAlevi, cikis, Quaternion.LookRotation(yon));
            AtisSayisi++;
        }

        /// <summary>Random direction inside a cone of half-angle <paramref name="derece"/> around <paramref name="yon"/>.</summary>
        public static Vector3 Sac(Vector3 yon, float derece, System.Random r)
        {
            if (derece <= 0f) return yon;
            float aci = Mathf.Sqrt((float)r.NextDouble()) * derece;
            float donus = (float)r.NextDouble() * 360f;
            Vector3 dik = Vector3.Cross(yon, Mathf.Abs(yon.y) < 0.95f ? Vector3.up : Vector3.right).normalized;
            dik = Quaternion.AngleAxis(donus, yon) * dik;
            return Quaternion.AngleAxis(aci, dik) * yon;
        }
    }
}
