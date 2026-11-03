using UnityEngine;

namespace NU36
{
    /// <summary>Lab 5: base trigger for resupply. Tells the level manager when the player's aircraft enters.</summary>
    public class IkmalNoktasi : MonoBehaviour
    {
        [SerializeField] SeviyeYoneticisi yonetici;

        public SeviyeYoneticisi Yonetici { get => yonetici; set => yonetici = value; }

        void OnTriggerEnter(Collider diger)
        {
            if (diger == null || diger.GetComponentInParent<UcusKontrol>() == null)
                return;
            if (yonetici == null)
            {
                Debug.LogError("[IkmalNoktasi] SeviyeYoneticisi atanmamış.", this);
                return;
            }
            yonetici.UsseVarildi();
        }
    }
}
