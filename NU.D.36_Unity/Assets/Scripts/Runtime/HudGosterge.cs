using UnityEngine;

namespace NU36
{
    public enum HudKaynak { Hiz, Irtifa }

    /// <summary>
    /// Lab 4 HUD: one dial needle. Reads UcusKontrol.Hiz (m/s) or the aircraft's world y (m) every frame after the flight
    /// update and rotates the needle about its own centre (the sprite pivot is the hub). Display only.
    /// The altitude is the aircraft pivot's world y, not a height above the terrain; near the start the ground is at y = -1.
    /// </summary>
    public class HudGosterge : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [SerializeField] HudKaynak kaynak = HudKaynak.Hiz;
        [SerializeField] RectTransform ibre;
        [SerializeField, Min(1f)] float azami = 50f;

        public UcusKontrol Ucus { get => ucus; set => ucus = value; }
        public HudKaynak Kaynak { get => kaynak; set => kaynak = value; }
        public RectTransform Ibre { get => ibre; set => ibre = value; }
        public float Azami { get => azami; set => azami = Mathf.Max(1f, value); }

        /// <summary>Last displayed value (m/s or m).</summary>
        public float Deger { get; private set; }
        /// <summary>Last needle rotation (Unity z, degrees).</summary>
        public float IbreZ { get; private set; }

        void LateUpdate() => Guncelle();

        public void Guncelle()
        {
            if (ucus == null || ibre == null)
                return;
            Deger = kaynak == HudKaynak.Hiz ? ucus.Hiz : ucus.transform.position.y;
            IbreZ = HudHesap.IbreZ(Deger, azami);
            ibre.localRotation = Quaternion.Euler(0f, 0f, IbreZ);
        }
    }
}
