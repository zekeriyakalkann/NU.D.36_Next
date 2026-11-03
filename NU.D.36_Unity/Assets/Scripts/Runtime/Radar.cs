using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 2, Task 5: reports whether the target is inside a field of view defined by a
    /// half-angle around this transform's forward axis and a maximum range.
    /// </summary>
    public class Radar : MonoBehaviour
    {
        public static readonly Color GorunurRengi = Color.green;
        public static readonly Color GorunmezRengi = Color.red;
        static readonly Color IleriRengi = Color.white;

        [SerializeField] Transform hedef;
        [Tooltip("Half of the field-of-view angle, in degrees, measured from forward.")]
        [SerializeField, Range(0f, 180f)] float yariAci = 45f;
        [Tooltip("Maximum detection range, in metres.")]
        [SerializeField, Min(0f)] float menzil = 100f;

        public Transform Hedef { get => hedef; set => hedef = value; }
        public float YariAci { get => yariAci; set => yariAci = Mathf.Clamp(value, 0f, 180f); }
        public float Menzil { get => menzil; set => menzil = Mathf.Max(0f, value); }

        /// <summary>Result of the most recent Update.</summary>
        public bool HedefGorunur { get; private set; }

        /// <summary>
        /// Dot-product field-of-view test. For unit vectors, Dot(forward, toTarget) is the
        /// cosine of the angle between them, so the target is inside the view when that
        /// cosine is at least cos(half-angle). This avoids computing the angle itself.
        /// </summary>
        public static bool GorusAlanindaMi(Vector3 konum, Vector3 ileri, Vector3 hedefKonum,
            float yariAciDerece, float menzil)
        {
            Vector3 fark = hedefKonum - konum;
            float mesafe = fark.magnitude;
            if (mesafe > menzil)
                return false;
            if (mesafe <= Mathf.Epsilon)
                return true;

            float dot = Vector3.Dot(ileri.normalized, fark / mesafe);
            return dot >= Mathf.Cos(yariAciDerece * Mathf.Deg2Rad);
        }

        public static Color GizmoRengi(bool gorunur) => gorunur ? GorunurRengi : GorunmezRengi;

        /// <summary>Evaluates the field-of-view test for the current transforms.</summary>
        public bool Hesapla()
        {
            return hedef != null &&
                   GorusAlanindaMi(transform.position, transform.forward, hedef.position, yariAci, menzil);
        }

        void Update()
        {
            bool gorunur = Hesapla();
            if (gorunur != HedefGorunur)
                Debug.Log($"[Radar] {name}: hedef {(gorunur ? "görüş alanına girdi" : "görüş alanından çıktı")}");
            HedefGorunur = gorunur;
        }

        // Editor visualization only: evaluates the same test independently of Update,
        // so it is also correct outside Play Mode and never changes runtime state.
        void OnDrawGizmos()
        {
            Vector3 konum = transform.position;
            Gizmos.color = IleriRengi;
            Gizmos.DrawLine(konum, konum + transform.forward * menzil);

            if (hedef == null)
                return;
            Gizmos.color = GizmoRengi(Hesapla());
            Gizmos.DrawLine(konum, hedef.position);
        }
    }
}
