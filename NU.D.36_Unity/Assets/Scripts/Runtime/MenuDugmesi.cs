using UnityEngine;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Menu button drawn with the Lab 4 button sheet (dugme-seti.png: four 768x256 rows, top to bottom normal, hover,
    /// pressed, disabled). A uGUI Button whose state transition shows the matching row of the sheet on its RawImage
    /// (hover and keyboard selection share the hover row) and dims the label when disabled.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MenuDugmesi : Button
    {
        public const int Normal = 0, Uzerinde = 1, Basili = 2, Pasif = 3;

        Text etiket;

        /// <summary>uvRect of one row of the sheet (row 0 is the top one).</summary>
        public static Rect Satir(int satir) => new Rect(0f, 0.75f - 0.25f * Mathf.Clamp(satir, 0, 3), 1f, 0.25f);

        /// <summary>Row shown at the moment; for tests.</summary>
        public int GorselSatir { get; private set; }

        public Text Etiket
        {
            get => etiket;
            set { etiket = value; Yenile(); }
        }

        public string Yazi
        {
            get => etiket != null ? etiket.text : "";
            set { if (etiket != null) etiket.text = value; }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            switch (state)
            {
                case SelectionState.Highlighted:
                case SelectionState.Selected: GorselSatir = Uzerinde; break;
                case SelectionState.Pressed: GorselSatir = Basili; break;
                case SelectionState.Disabled: GorselSatir = Pasif; break;
                default: GorselSatir = Normal; break;
            }
            Yenile();
        }

        void Yenile()
        {
            var gorsel = GetComponent<RawImage>();
            if (gorsel != null)
                gorsel.uvRect = Satir(GorselSatir);
            if (etiket != null)
                etiket.color = GorselSatir == Pasif ? MenuArayuzu.Soluk : MenuArayuzu.Krem;
        }
    }
}
