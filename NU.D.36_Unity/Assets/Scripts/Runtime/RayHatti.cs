using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4): the railway in the scene. Holds the control points (world space) and builds the
    /// <see cref="RayEgrisi"/> from them; the train, the fire zones and the HUD read distances along the line from here.
    /// The rail meshes themselves are baked by the scene builder along the same curve.
    /// </summary>
    public class RayHatti : MonoBehaviour
    {
        [SerializeField] Vector3[] kontrolNoktalari = new Vector3[0];

        RayEgrisi egri;

        public Vector3[] KontrolNoktalari
        {
            get => kontrolNoktalari;
            set { kontrolNoktalari = value; egri = null; }
        }

        public RayEgrisi Egri => egri ??= new RayEgrisi(kontrolNoktalari);
        public float Uzunluk => Egri.Uzunluk;

        void OnValidate() => egri = null;

        void OnDrawGizmosSelected()
        {
            if (kontrolNoktalari == null || kontrolNoktalari.Length < 2)
                return;
            Gizmos.color = Color.yellow;
            RayEgrisi e = Egri;
            for (float s = 0f; s < e.Uzunluk; s += 20f)
                Gizmos.DrawLine(e.Nokta(s) + Vector3.up, e.Nokta(Mathf.Min(s + 20f, e.Uzunluk)) + Vector3.up);
        }
    }
}
