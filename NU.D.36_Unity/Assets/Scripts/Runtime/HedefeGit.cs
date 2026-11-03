using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 2, Task 4: turns toward a target with a bounded turn rate and flies forward
    /// at constant speed, stopping while within the arrival distance.
    /// Rotates only this transform (heading). Visual wing roll is applied to a child
    /// by <see cref="KanatYatisi"/>, so the two rotations do not overwrite each other.
    /// </summary>
    public class HedefeGit : MonoBehaviour
    {
        static readonly Color IleriRengi = Color.cyan;
        static readonly Color HedefYonuRengi = Color.yellow;

        [SerializeField] Transform hedef;
        [Tooltip("Forward speed in m/s.")]
        [SerializeField, Min(0f)] float hiz = 20f;
        [Tooltip("Maximum turn rate in degrees per second.")]
        [SerializeField, Min(0f)] float donusHizi = 90f;
        // 10 m keeps the aircraft outside Balon when it stops: Balon radius 3 m (scale 6) plus the
        // nose about 3.9 m ahead of the root, with about 3 m clearance. 5 m put the nose inside it.
        [Tooltip("Movement stops while the target is closer than this, in metres (measured from this transform).")]
        [SerializeField, Min(0f)] float varisMesafesi = 10f;
        [Tooltip("Length of the forward Gizmo line, in metres.")]
        [SerializeField, Min(0f)] float gizmoUzunlugu = 10f;

        public Transform Hedef { get => hedef; set => hedef = value; }
        public float Hiz { get => hiz; set => hiz = Mathf.Max(0f, value); }
        public float DonusHizi { get => donusHizi; set => donusHizi = Mathf.Max(0f, value); }
        public float VarisMesafesi { get => varisMesafesi; set => varisMesafesi = Mathf.Max(0f, value); }

        /// <summary>True while the target is within <see cref="VarisMesafesi"/>.</summary>
        public bool HedefeVardi { get; private set; }

        /// <summary>
        /// Unit vector from <paramref name="konum"/> toward <paramref name="hedefKonum"/>.
        /// Normalizing removes the distance from the vector, so the movement speed does not
        /// depend on how far away the target is. Returns zero when both points coincide.
        /// </summary>
        public static Vector3 HedefYonu(Vector3 konum, Vector3 hedefKonum)
        {
            return (hedefKonum - konum).normalized;
        }

        void Update()
        {
            Adim(Time.deltaTime);
        }

        /// <summary>Advances the movement by <paramref name="dt"/> seconds.</summary>
        public void Adim(float dt)
        {
            if (hedef == null)
                return;

            float mesafe = Vector3.Distance(transform.position, hedef.position);
            HedefeVardi = mesafe <= varisMesafesi;
            if (HedefeVardi)
                return;

            Vector3 yon = HedefYonu(transform.position, hedef.position);
            if (yon == Vector3.zero)
                return;

            Quaternion hedefRotasyonu = Quaternion.LookRotation(yon);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, hedefRotasyonu, donusHizi * dt);
            transform.position += transform.forward * (hiz * dt);
        }

        void OnDrawGizmos()
        {
            Vector3 konum = transform.position;
            Gizmos.color = IleriRengi;
            Gizmos.DrawLine(konum, konum + transform.forward * gizmoUzunlugu);

            if (hedef == null)
                return;
            Gizmos.color = HedefYonuRengi;
            Gizmos.DrawLine(konum, hedef.position);
        }
    }
}
