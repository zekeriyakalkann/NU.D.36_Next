using UnityEngine;

namespace NU36
{
    /// <summary>Lab 7: resupply over the own airfield (trigger box). A low pass refills the bomb load through <see cref="GeceGorevi"/>.</summary>
    public class IkmalAlani : MonoBehaviour
    {
        [SerializeField] GeceGorevi gorev;

        public GeceGorevi Gorev { get => gorev; set => gorev = value; }

        void OnTriggerEnter(Collider diger)
        {
            if (gorev != null && diger != null && diger.GetComponentInParent<UcusKontrol>() != null)
                gorev.Ikmal();
        }
    }
}
