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

        [Header("Hafta 5: görüş hattı")]
        [Tooltip("Görüşü KESEN katmanlar: Arazi ve Yapi. Hedefin katmanını KOYMA. Nothing = görüş hattı testi kapalı (Lab 2-4 davranışı).")]
        [SerializeField] LayerMask engelKatmani;
        [Tooltip("Işının başladığı göz yüksekliği (m), mevzinin kendi çarpıştırıcısının üstünde olmalı.")]
        [SerializeField, Min(0f)] float gozYuksekligi = 3f;
        [Tooltip("Görüş denetimi aralığı (s). 0 = her kare; 0.2 = saniyede 5 kez (Adım 05 ölçümü).")]
        [SerializeField, Min(0f)] float denetimAraligi;

        float sonrakiDenetim;

        public Transform Hedef { get => hedef; set => hedef = value; }
        public float YariAci { get => yariAci; set => yariAci = Mathf.Clamp(value, 0f, 180f); }
        public float Menzil { get => menzil; set => menzil = Mathf.Max(0f, value); }
        public LayerMask EngelKatmani { get => engelKatmani; set => engelKatmani = value; }
        public float GozYuksekligi { get => gozYuksekligi; set => gozYuksekligi = Mathf.Max(0f, value); }
        public float DenetimAraligi { get => denetimAraligi; set => denetimAraligi = Mathf.Max(0f, value); }

        /// <summary>Result of the most recent check: cone AND (if a mask is set) clear line of sight.</summary>
        public bool HedefGorunur { get; private set; }

        /// <summary>Worksheet name for the final result (same value as <see cref="HedefGorunur"/>).</summary>
        public bool Goruyor => HedefGorunur;

        /// <summary>True when the target is inside the cone but terrain or a structure blocks the ray.</summary>
        public bool EngelVar { get; private set; }

        /// <summary>Number of line-of-sight rays cast since start (for the Step 05 measurement).</summary>
        public int IsinSayisi { get; private set; }

        /// <summary>Eye point of the observer: the ray starts here, above the emplacement's own collider.</summary>
        public Vector3 Goz => transform.position + Vector3.up * gozYuksekligi;

        /// <summary>
        /// Line-of-sight test. The ray only reports colliders on <paramref name="engel"/> (terrain, structures), so the
        /// target's own collider can never count as an obstacle. An empty mask never blocks.
        /// </summary>
        public static bool GorusEngelliMi(Vector3 goz, Vector3 hedefKonum, LayerMask engel)
        {
            if (engel.value == 0)
                return false;
            Vector3 fark = hedefKonum - goz;
            float uzaklik = fark.magnitude;
            if (uzaklik <= Mathf.Epsilon)
                return false;
            return Physics.Raycast(goz, fark / uzaklik, uzaklik, engel, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Gizmo colour: green = sees, yellow = inside the cone but blocked, red = outside the cone.</summary>
        public static Color GizmoRengi(bool goruyor, bool engelVar) =>
            goruyor ? GorunurRengi : engelVar ? Color.yellow : GorunmezRengi;

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
            if (denetimAraligi > 0f)
            {
                if (Time.time < sonrakiDenetim)
                    return; // keep the previous result between checks
                sonrakiDenetim = Time.time + denetimAraligi;
            }
            Denetle();
        }

        /// <summary>One full check: cone test, then (only inside the cone) the line-of-sight ray.</summary>
        public void Denetle()
        {
            bool gorunur = Hesapla();
            // Hafta 5: the cone only looks at the angle; a hill between the two does not matter to it.
            if (gorunur && engelKatmani.value != 0)
            {
                IsinSayisi++;
                EngelVar = GorusEngelliMi(Goz, hedef.position, engelKatmani);
                if (EngelVar)
                    gorunur = false;
            }
            else
                EngelVar = false;

            if (gorunur != HedefGorunur)
                Debug.Log($"[Radar] {name}: hedef {(gorunur ? "görüş alanına girdi" : EngelVar ? "engelin arkasında" : "görüş alanından çıktı")}");
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
            if (engelKatmani.value == 0)
            {
                Gizmos.color = GizmoRengi(Hesapla());
                Gizmos.DrawLine(konum, hedef.position);
                return;
            }
            // yeşil: görüyor · sarı: koni içinde ama ENGEL var · kırmızı: koni dışında
            bool koni = Hesapla();
            bool engel = koni && GorusEngelliMi(Goz, hedef.position, engelKatmani);
            Gizmos.color = GizmoRengi(koni && !engel, engel);
            Gizmos.DrawLine(Goz, hedef.position);
        }
    }
}
