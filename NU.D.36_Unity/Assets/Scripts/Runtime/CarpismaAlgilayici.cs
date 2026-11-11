using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 7: crash against structures (towers, bridges, buildings, falling debris). The aircraft moves by its own code, not by
    /// the physics engine, so it sweeps a sphere from last frame's position to this frame's (the Hafta 5 tunnelling lesson: no
    /// gap for a fast aircraft to pass through) on the given layers; a hit is a crash through <see cref="InisAlgilayici.Carp"/>.
    /// The ground itself is the ground probe's business.
    /// </summary>
    [RequireComponent(typeof(InisAlgilayici))]
    public class CarpismaAlgilayici : MonoBehaviour
    {
        [SerializeField] LayerMask engeller;
        [Tooltip("Radius of the swept sphere (m): about half the fuselage and the inner wing.")]
        [SerializeField, Min(0.1f)] float yaricap = 2.5f;

        InisAlgilayici algilayici;
        Vector3 onceki;
        bool hazir;

        public LayerMask Engeller { get => engeller; set => engeller = value; }
        public string SonCarpilan { get; private set; }

        void Awake() => algilayici = GetComponent<InisAlgilayici>();

        void OnEnable() => hazir = false;

        void LateUpdate() => Denetle();

        /// <summary>Sweeps from the previous position to the current one. Returns true on a hit (and crashes the aircraft).</summary>
        public bool Denetle()
        {
            Vector3 p = transform.position;
            if (!hazir)
            {
                onceki = p;
                hazir = true;
            }
            if (algilayici == null || algilayici.Cakti || engeller.value == 0)
            {
                onceki = p;
                return false;
            }
            Vector3 d = p - onceki;
            float uzaklik = d.magnitude;
            Collider vurulan = null;
            if (uzaklik > 1e-3f && Physics.SphereCast(onceki, yaricap, d / uzaklik, out RaycastHit vurus, uzaklik, engeller, QueryTriggerInteraction.Ignore))
                vurulan = vurus.collider;
            else
            {
                var tampon = new Collider[1];
                if (Physics.OverlapSphereNonAlloc(p, yaricap, tampon, engeller, QueryTriggerInteraction.Ignore) > 0)
                    vurulan = tampon[0];
            }
            onceki = p;
            if (vurulan == null)
                return false;
            SonCarpilan = vurulan.name;
            algilayici.Carp("yapıya çarptı");
            return true;
        }
    }
}
