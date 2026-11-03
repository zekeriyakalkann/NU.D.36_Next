using UnityEngine;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 4 HUD: compass ribbon. A Repeat texture shows a window of the ribbon; the pointer is a separate element and
    /// is not part of the texture. With <c>kaydir</c> on, the window follows the aircraft's yaw (eulerAngles.y, 0 = world +Z);
    /// with it off the window stays at heading 0.
    /// Whether the compass has to move and whether world +Z is "north" are not confirmed requirements: both are
    /// switchable here (kaydir, sifirOfseti) and listed as open decisions in the Lab 4 documents.
    /// </summary>
    public class HudPusula : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [SerializeField] RawImage serit;
        [SerializeField, Range(0.05f, 1f)] float pencere = 0.5f;
        [SerializeField] bool kaydir = true;
        [SerializeField] float sifirOfseti;

        public UcusKontrol Ucus { get => ucus; set => ucus = value; }
        public RawImage Serit { get => serit; set => serit = value; }
        public float Pencere => pencere;
        public bool Kaydir { get => kaydir; set => kaydir = value; }
        public float SifirOfseti { get => sifirOfseti; set => sifirOfseti = value; }

        /// <summary>Last displayed heading in degrees (0..360).</summary>
        public float Yon { get; private set; }

        void LateUpdate() => Guncelle();

        public void Guncelle()
        {
            if (serit == null)
                return;
            Yon = kaydir && ucus != null ? HudHesap.Yon(ucus.transform.eulerAngles.y, sifirOfseti) : 0f;
            serit.uvRect = new Rect(HudHesap.PusulaUvX(Yon, pencere), 0f, pencere, 1f);
        }
    }
}
