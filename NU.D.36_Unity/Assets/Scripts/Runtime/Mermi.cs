using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 5, Step 03: an anti-aircraft round moved by hand (no Rigidbody). Each physics step it covers
    /// adim = hiz × fixedDeltaTime metres; at 260 m/s that is 5.2 m, more than the 4.8 m diameter of the aircraft's
    /// body sphere. <c>supurme</c> = true (the fix) casts a ray along the whole step before moving, so nothing between
    /// the old and the new position is skipped. <c>supurme</c> = false is the broken version kept for the experiment:
    /// it only checks a small sphere at the new position, so the round can tunnel through the aircraft.
    /// </summary>
    public class Mermi : MonoBehaviour
    {
        [SerializeField, Min(0f)] float hiz = 260f;
        [SerializeField, Min(0)] int hasar = 8;
        [Tooltip("Layers the round can hit (aircraft and terrain).")]
        [SerializeField] LayerMask vurulacak;
        [Tooltip("true = sweep the whole step with a ray (fix); false = check only the end point (tunnels at high speed).")]
        [SerializeField] bool supurme = true;
        [Tooltip("Radius of the end-point check when supurme is off (m).")]
        [SerializeField, Min(0.01f)] float ucNoktaYaricapi = 0.15f;
        [Tooltip("Self-destruct after this many seconds.")]
        [SerializeField, Min(0.1f)] float omur = 6f;

        Vector3 yon = Vector3.forward;
        float yas;
        static readonly Collider[] Tampon = new Collider[4];

        public float Hiz => hiz;
        public int Hasar => hasar;
        public Vector3 Yon => yon;
        public bool Supurme { get => supurme; set => supurme = value; }
        public LayerMask Vurulacak { get => vurulacak; set => vurulacak = value; }
        /// <summary>Collider hit by this round (null until it hits something).</summary>
        public Collider Vurdugu { get; private set; }

        /// <summary>Distance covered per physics step: the number that decides whether tunnelling can happen.</summary>
        public static float AdimUzunlugu(float hiz, float fixedDeltaTime) => hiz * fixedDeltaTime;

        /// <summary>Sets direction, speed and damage. Called by the gun right after Instantiate.</summary>
        public void Ates(Vector3 yeniYon, float yeniHiz, int yeniHasar, LayerMask maske, bool supur)
        {
            yon = yeniYon.sqrMagnitude > 1e-8f ? yeniYon.normalized : transform.forward;
            hiz = yeniHiz;
            hasar = yeniHasar;
            vurulacak = maske;
            supurme = supur;
            transform.forward = yon;
        }

        // Update değil FixedUpdate: adım fizik saatine bağlı olmalı. Update'te olsaydı adım kare hızıyla
        // değişir, oyunun zorluğu oyuncunun ekranına bağlanırdı.
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

        /// <summary>One physics step. Returns true if the round hit something (it is then destroyed).</summary>
        public bool Adim(float dt)
        {
            float adim = hiz * dt;

            if (supurme)
            {
                if (Physics.Raycast(transform.position, yon, out RaycastHit vurus, adim, vurulacak, QueryTriggerInteraction.Ignore))
                {
                    Vur(vurus.collider);
                    return true;
                }
                transform.position += yon * adim;        // yol temiz: tam adımı at
                return false;
            }

            // Broken version: jump first, then look only where we landed.
            transform.position += yon * adim;
            int n = Physics.OverlapSphereNonAlloc(transform.position, ucNoktaYaricapi, Tampon, vurulacak, QueryTriggerInteraction.Ignore);
            if (n > 0)
            {
                Vur(Tampon[0]);
                return true;
            }
            return false;
        }

        void Vur(Collider c)
        {
            Vurdugu = c;
            var saglik = c.GetComponentInParent<Saglik>();
            if (saglik != null)
                saglik.Vur(hasar);

            var govde = c.attachedRigidbody;
            if (govde != null && !govde.isKinematic)
                govde.AddForce(yon * 40f, ForceMode.Impulse);

            Destroy(gameObject);
        }
    }
}
