using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 4 HUD: throttle indicator. Moves the knob along the plate's slot (vertical only) to UcusKontrol.GazOrani.
    /// Display only; it does not read input and the knob is not clickable. Heights are in the plate's local canvas units.
    /// </summary>
    public class HudGazGostergesi : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [SerializeField] RectTransform kol;
        [SerializeField] float altY = -200f;   // knob y at 0 % throttle
        [SerializeField] float ustY = 200f;    // knob y at 100 % throttle

        public UcusKontrol Ucus { get => ucus; set => ucus = value; }
        public RectTransform Kol { get => kol; set => kol = value; }
        public float AltY => altY;
        public float UstY => ustY;

        void LateUpdate() => Guncelle();

        public void Guncelle()
        {
            if (ucus == null || kol == null)
                return;
            Vector2 p = kol.anchoredPosition;
            p.y = HudHesap.KolY(ucus.GazOrani, altY, ustY);
            kol.anchoredPosition = p;
        }
    }
}
