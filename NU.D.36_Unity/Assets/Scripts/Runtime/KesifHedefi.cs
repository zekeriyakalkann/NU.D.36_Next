using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 4: reconnaissance target. A trigger column (see the prefab: 30 x 180 x 30, centre Y 90)
    /// that the player's aircraft flies through at any altitude. It is collected once; it then
    /// raises <see cref="Fotograflandi"/> (listened to by the score wallet) and its own
    /// <see cref="Toplandi"/> (listened to by the mission manager) and hides itself.
    /// The target never changes the score itself.
    /// </summary>
    public class KesifHedefi : MonoBehaviour
    {
        [Tooltip("Points the wallet awards for this target.")]
        [SerializeField, Min(0)] int puan = 100;
        [Tooltip("Required targets must all be collected before the return phase opens.")]
        [SerializeField] bool zorunlu = true;
        [Tooltip("Visual shown until the target is collected; hidden afterwards.")]
        [SerializeField] GameObject gorsel;

        /// <summary>Raised with (points, required) when any target is photographed.</summary>
        public static event Action<int, bool> Fotograflandi;

        /// <summary>Raised with this target when it is photographed (per-instance identity for the mission manager).</summary>
        public event Action<KesifHedefi> Toplandi;

        public int Puan { get => puan; set => puan = Mathf.Max(0, value); }
        public bool Zorunlu { get => zorunlu; set => zorunlu = value; }
        public GameObject Gorsel { get => gorsel; set => gorsel = value; }
        public bool Fotografli { get; private set; }

        /// <summary>True after <see cref="Kapat"/>: the mission ended, so the target no longer scores.</summary>
        public bool Kapali { get; private set; }

        // Static state must not survive when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StatikDurumuSifirla() => Fotograflandi = null;

        void OnTriggerEnter(Collider diger) => Dene(diger);

        /// <summary>Stops the target from being collected (used when the mission ends). Does not change <see cref="Fotografli"/>.</summary>
        public void Kapat()
        {
            Kapali = true;
            foreach (Collider c in GetComponents<Collider>())
                c.enabled = false;
        }

        /// <summary>Collects the target if <paramref name="diger"/> belongs to the player aircraft. Returns true when collected.</summary>
        public bool Dene(Collider diger)
        {
            if (Fotografli || Kapali || diger == null || diger.GetComponentInParent<UcusKontrol>() == null)
                return false;

            Fotografli = true;
            if (gorsel != null)
                gorsel.SetActive(false);
            foreach (Collider c in GetComponents<Collider>())
                c.enabled = false;

            Debug.Log($"[KesifHedefi] {name} fotoğraflandı (+{puan}, {(zorunlu ? "zorunlu" : "isteğe bağlı")})");
            Fotograflandi?.Invoke(puan, zorunlu);
            Toplandi?.Invoke(this);
            return true;
        }
    }
}
