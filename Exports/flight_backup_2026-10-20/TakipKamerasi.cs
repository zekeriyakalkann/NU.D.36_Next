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

        Camera kamera;
        Vector3 hizRef;
        bool uyariVerildi;

        public Transform Hedef { get => hedef; set { hedef = value; ucus = null; } }
        public UcusKontrol Ucus => ucus;
        public Vector3 Uzaklik => uzaklik;
        public float Yumusatma { get => yumusatma; set => yumusatma = Mathf.Max(0f, value); }
        public float EnAzGorusAcisi => enAzGorusAcisi;
        public float EnCokGorusAcisi => enCokGorusAcisi;

        /// <summary>Where the camera wants to be this frame (target local offset in world space).</summary>
        public Vector3 IstenenKonum => hedef != null ? hedef.TransformPoint(uzaklik) : transform.position;

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
