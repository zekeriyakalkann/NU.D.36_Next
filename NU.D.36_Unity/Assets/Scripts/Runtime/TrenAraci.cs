using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4): one vehicle of the train (locomotive with its tender, or a wagon). The pivot is the middle of the
    /// vehicle at rail-top height, +Z is the front. <see cref="Tren"/> sets the vehicle from its two bogies: each bogie
    /// sits on the line, the body is the chord between them, so the vehicle stays on the rails in the bends.
    /// </summary>
    public class TrenAraci : MonoBehaviour
    {
        [SerializeField, Min(1f)] float uzunluk = 10f;
        [Tooltip("Distance from the middle of the vehicle to each bogie.")]
        [SerializeField, Min(0f)] float bojiAraligi = 3.5f;

        public float Uzunluk { get => uzunluk; set => uzunluk = Mathf.Max(1f, value); }
        public float BojiAraligi { get => bojiAraligi; set => bojiAraligi = Mathf.Max(0f, value); }

        /// <summary>Front and rear bogie in world space (set by the train each step).</summary>
        public Vector3 OnBoji { get; private set; }
        public Vector3 ArkaBoji { get; private set; }

        Rigidbody govde;

        void Awake() => govde = GetComponent<Rigidbody>();

        /// <summary>Puts the vehicle on the line with its middle at <paramref name="s"/>.</summary>
        public void Yerlestir(RayEgrisi egri, float s, float rayUstu, bool fizikle)
        {
            OnBoji = egri.Nokta(s + bojiAraligi);
            ArkaBoji = egri.Nokta(s - bojiAraligi);
            Vector3 yon = OnBoji - ArkaBoji;
            Vector3 konum = (OnBoji + ArkaBoji) * 0.5f + Vector3.up * rayUstu;
            Quaternion donus = yon.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(yon, Vector3.up) : transform.rotation;
            if (fizikle && govde != null && govde.isKinematic && Application.isPlaying)
            {
                govde.MovePosition(konum);
                govde.MoveRotation(donus);
            }
            else
                transform.SetPositionAndRotation(konum, donus);
        }
    }
}
