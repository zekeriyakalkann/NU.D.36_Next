using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 3: Observer listener. Subscribes to <see cref="Saglik.CanDegisti"/> while enabled and
    /// logs the new health value. Uses only Saglik's public event.
    /// </summary>
    public class CanYazisi : MonoBehaviour
    {
        [Tooltip("Health to listen to. If empty, the Saglik on this GameObject is used.")]
        [SerializeField] Saglik saglik;

        Saglik abone; // the instance actually subscribed to, so OnDisable unsubscribes from the same one

        public Saglik Saglik { get => saglik; set => saglik = value; }

        void OnEnable()
        {
            if (saglik == null)
                saglik = GetComponent<Saglik>();
            if (saglik == null)
            {
                Debug.LogWarning($"[CanYazisi] {name}: no Saglik to listen to.", this);
                return;
            }
            abone = saglik;
            abone.CanDegisti += Yaz;
        }

        void OnDisable()
        {
            if (abone != null)
                abone.CanDegisti -= Yaz;
            abone = null;
        }

        void Yaz(int can) => Debug.Log($"[CanYazisi] {name}: can = {can}");
    }
}
