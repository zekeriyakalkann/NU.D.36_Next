using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 3: speed-sensitive follow camera. Follows a point given in the target's local space,
    /// smoothed with Vector3.SmoothDamp, looks at the target, and widens the field of view as the
    /// throttle rises. Runs in LateUpdate so it uses the target pose after this frame's Update
    /// (UcusKontrol) has moved it.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class TakipKamerasi : MonoBehaviour
    {
        [SerializeField] Transform hedef;
        [Tooltip("Optional. Taken from the target if empty; drives the field of view.")]
        [SerializeField] UcusKontrol ucus;
        [Tooltip("Camera position in the target's local space.")]
        [SerializeField] Vector3 uzaklik = new Vector3(0f, 3.2f, -11f);
        [Tooltip("SmoothDamp smooth time in seconds.")]
        [SerializeField, Min(0f)] float yumusatma = 0.18f;
        [SerializeField, Range(1f, 179f)] float enAzGorusAcisi = 55f;
        [SerializeField, Range(1f, 179f)] float enCokGorusAcisi = 72f;
        [Tooltip("false = the offset turns with the whole aircraft, including roll and pitch (Lab 3). true = the offset follows only the heading, so the camera stays behind and above while the aircraft banks and pitches in view.")]
        [SerializeField] bool ufukKilitli;
        [Tooltip("Horizon-locked mode: largest swing of the heading frame (degrees/s).")]
        [SerializeField, Min(10f)] float ufukDonusHizi = 180f;

        Camera kamera;
        Vector3 hizRef;
        bool uyariVerildi;
        Quaternion sonYon = Quaternion.identity;   // last valid heading frame (kept while the nose points straight up or down)

        public Transform Hedef { get => hedef; set { hedef = value; ucus = null; } }
        public UcusKontrol Ucus => ucus;
        public Vector3 Uzaklik => uzaklik;
        public float Yumusatma { get => yumusatma; set => yumusatma = Mathf.Max(0f, value); }
        public float EnAzGorusAcisi => enAzGorusAcisi;
        public float EnCokGorusAcisi => enCokGorusAcisi;

        /// <summary>Where the camera wants to be this frame (target local offset in world space).</summary>
        public Vector3 IstenenKonum
        {
            get
            {
                if (hedef == null)
                    return transform.position;
                return ufukKilitli ? hedef.position + sonYon * uzaklik : hedef.TransformPoint(uzaklik);
            }
        }

        /// <summary>
        /// Horizon-locked mode: turns the heading frame of the offset towards the aircraft's heading by at most
        /// <c>ufukDonusHizi</c> degrees per second (infinity = at once). The heading is undefined while the nose points straight up or down,
        /// so the last one is kept; after the top of a loop the heading is reversed and the frame then swings round instead of jumping.
        /// </summary>
        public void YonuGuncelle(float dt)
        {
            if (hedef == null)
                return;
            Vector3 yatay = Vector3.ProjectOnPlane(hedef.forward, Vector3.up);
            if (yatay.sqrMagnitude <= 0.01f)
                return;
            Quaternion yeni = Quaternion.LookRotation(yatay.normalized, Vector3.up);
            sonYon = float.IsPositiveInfinity(dt) ? yeni : Quaternion.RotateTowards(sonYon, yeni, ufukDonusHizi * dt);
        }

        public bool UfukKilitli { get => ufukKilitli; set => ufukKilitli = value; }

        public float GorusAcisiHesapla(float gazOrani) =>
            Mathf.Lerp(enAzGorusAcisi, enCokGorusAcisi, Mathf.Clamp01(gazOrani));

        void Awake()
        {
            kamera = GetComponent<Camera>();
            BaglantiyiTamamla();
        }

        void Start() => Yerles();

        void BaglantiyiTamamla()
        {
            if (ucus == null && hedef != null)
                ucus = hedef.GetComponent<UcusKontrol>();
        }

        /// <summary>Places the camera at the offset immediately (no smoothing), e.g. at scene start.</summary>
        public void Yerles()
        {
            if (hedef == null)
                return;
            YonuGuncelle(float.PositiveInfinity);
            transform.position = IstenenKonum;
            transform.LookAt(hedef);
            hizRef = Vector3.zero;
            GorusAcisiniGuncelle();
        }

        void LateUpdate() => Takip(Time.deltaTime);

        /// <summary>One follow step of <paramref name="dt"/> seconds.</summary>
        public void Takip(float dt)
        {
            if (hedef == null)
            {
                if (!uyariVerildi)
                    Debug.LogWarning($"[TakipKamerasi] {name}: no target assigned.", this);
                uyariVerildi = true;
                return;
            }
            if (dt <= 0f)
                return;

            BaglantiyiTamamla();
            YonuGuncelle(dt);
            transform.position = Vector3.SmoothDamp(transform.position, IstenenKonum, ref hizRef, yumusatma, Mathf.Infinity, dt);
            transform.LookAt(hedef);
            GorusAcisiniGuncelle();
        }

        void GorusAcisiniGuncelle()
        {
            if (kamera == null)
                kamera = GetComponent<Camera>();
            if (ucus != null)
                kamera.fieldOfView = GorusAcisiHesapla(ucus.GazOrani);
        }
    }
}
