using UnityEngine;

namespace NU36
{
    /// <summary>Lab 4: base trigger. Tells the mission manager when the player's aircraft enters.</summary>
    public class UsGirisi : MonoBehaviour
    {
        [SerializeField] GorevYoneticisi yonetici;

        public GorevYoneticisi Yonetici { get => yonetici; set => yonetici = value; }

        void OnTriggerEnter(Collider diger) => Dene(diger);

        /// <summary>Reports arrival if <paramref name="diger"/> belongs to the player aircraft. Returns true when reported.</summary>
        public bool Dene(Collider diger)
        {
            if (diger == null || diger.GetComponentInParent<UcusKontrol>() == null)
                return false;
            if (yonetici == null)
            {
                Debug.LogError("[UsGirisi] GorevYoneticisi atanmamış.", this);
                return false;
            }
            yonetici.UsseVarildi();
            return true;
        }
    }
}
