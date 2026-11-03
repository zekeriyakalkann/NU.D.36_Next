using System.Collections.Generic;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 5, Step 02: the period sound locator. Sound travels at 343 m/s, so the engine noise heard now left the
    /// aircraft d / c seconds ago: the listener reports where the aircraft WAS (<see cref="DuyulanKonum"/>), not where it
    /// is. A silent engine (throttle below <c>sessizlikEsigi</c>) or a target beyond <c>duymaMenzili</c> is not heard.
    /// Angular error seen from the listener: θ = atan((v/c)·sin α), independent of the distance.
    /// </summary>
    [DefaultExecutionOrder(50)] // after UcusKontrol has moved the aircraft this frame
    public class SesDinleyici : MonoBehaviour
    {
        public const float VarsayilanSesHizi = 343f;

        [SerializeField] UcusKontrol ucak;
        [Tooltip("Speed of sound (m/s).")]
        [SerializeField, Min(1f)] float sesHizi = VarsayilanSesHizi;
        [Tooltip("Farthest distance at which the engine is heard (m).")]
        [SerializeField, Min(0f)] float duymaMenzili = 3000f;
        [Tooltip("Below this throttle the engine is too quiet to hear (0..1).")]
        [SerializeField, Range(0f, 1f)] float sessizlikEsigi = 0.15f;
        [Tooltip("How many seconds of position history are kept (must exceed the largest delay, range / speed of sound).")]
        [SerializeField, Min(0.5f)] float gecmisSuresi = 12f;

        [Header("Gözlem (Play'de her kare yazılır; elle değiştirmek etkisizdir)")]
        [SerializeField] bool duyuyor;
        [SerializeField] float gecikme;
        [SerializeField] float sapma;

        struct Kayit
        {
            public float t;
            public Vector3 k;
        }

        readonly List<Kayit> gecmis = new List<Kayit>();

        public UcusKontrol Ucak { get => ucak; set { ucak = value; gecmis.Clear(); } }
        public float SesHizi { get => sesHizi; set => sesHizi = Mathf.Max(1f, value); }
        public float DuymaMenzili { get => duymaMenzili; set => duymaMenzili = Mathf.Max(0f, value); }
        public float SessizlikEsigi { get => sessizlikEsigi; set => sessizlikEsigi = Mathf.Clamp01(value); }

        public bool Duyuyor { get => duyuyor; private set => duyuyor = value; }
        /// <summary>Sound travel time from the aircraft to the listener (s).</summary>
        public float Gecikme { get => gecikme; private set => gecikme = value; }
        /// <summary>Where the listener believes the aircraft is: its position <see cref="Gecikme"/> seconds ago.</summary>
        public Vector3 DuyulanKonum { get; private set; }
        /// <summary>Distance between the heard and the true position (m). Shown in the Inspector (worksheet Step 02).</summary>
        public float Sapma { get => sapma; private set => sapma = value; }
        public int GecmisSayisi => gecmis.Count;

        /// <summary>Delay of sound over <paramref name="uzaklik"/> metres.</summary>
        public static float GecikmeHesapla(float uzaklik, float sesHizi = VarsayilanSesHizi) => uzaklik / sesHizi;

        /// <summary>Angular error (degrees) for speed v, speed of sound c and angle α between flight path and line of sight.</summary>
        public static float AcisalHata(float v, float c, float alfaDerece) =>
            Mathf.Atan(v / c * Mathf.Sin(alfaDerece * Mathf.Deg2Rad)) * Mathf.Rad2Deg;

        void LateUpdate() => Adim(Time.time);

        /// <summary>Records the aircraft position at time <paramref name="simdi"/> and updates what is heard.</summary>
        public void Adim(float simdi)
        {
            if (ucak == null)
            {
                Duyuyor = false;
                return;
            }

            // Geçmiş konumları sakla: dinleyici bunlardan okuyacak.
            Vector3 gercek = ucak.transform.position;
            gecmis.Add(new Kayit { t = simdi, k = gercek });
            int eski = 0;
            while (eski < gecmis.Count - 2 && gecmis[eski + 1].t < simdi - gecmisSuresi)
                eski++;
            if (eski > 0)
                gecmis.RemoveRange(0, eski);

            float uzaklik = Vector3.Distance(transform.position, gercek);

            // Susan motor duyulmaz. Gaz kesip süzülürsen mevzi seni hiç fark etmez.
            if (ucak.GazOrani < sessizlikEsigi || uzaklik > duymaMenzili)
            {
                Duyuyor = false;
                return;
            }

            Duyuyor = true;
            Gecikme = uzaklik / sesHizi;
            DuyulanKonum = KonumGecmisten(simdi - Gecikme); // GEÇMİŞTEKİ yer
            Sapma = Vector3.Distance(DuyulanKonum, gercek);
        }

        /// <summary>Aircraft position at time <paramref name="t"/>, interpolated from the history (clamped to the stored span).</summary>
        public Vector3 KonumGecmisten(float t)
        {
            int n = gecmis.Count;
            if (n == 0)
                return ucak != null ? ucak.transform.position : transform.position;
            if (t <= gecmis[0].t)
                return gecmis[0].k;
            if (t >= gecmis[n - 1].t)
                return gecmis[n - 1].k;

            int lo = 0, hi = n - 1; // binary search: gecmis[lo].t <= t < gecmis[hi].t
            while (hi - lo > 1)
            {
                int m = (lo + hi) / 2;
                if (gecmis[m].t <= t) lo = m;
                else hi = m;
            }
            Kayit a = gecmis[lo], b = gecmis[hi];
            float oran = b.t > a.t ? (t - a.t) / (b.t - a.t) : 0f;
            return Vector3.Lerp(a.k, b.k, oran);
        }

        // mavi = dinleyicinin sandığı yer, kırmızı = uçağın gerçek yeri; aradaki boşluk gecikmedir.
        void OnDrawGizmos()
        {
            if (ucak == null)
                return;
            Vector3 p = transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(p, ucak.transform.position);
            if (!Application.isPlaying || !Duyuyor)
                return;
            Gizmos.color = new Color(0.2f, 0.45f, 1f);
            Gizmos.DrawLine(p, DuyulanKonum);
            Gizmos.DrawWireSphere(DuyulanKonum, 6f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(DuyulanKonum, ucak.transform.position);
        }
    }
}
