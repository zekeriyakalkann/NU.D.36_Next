using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 7: the marked landing zone on the runway: a rectangle in this object's local x (width) / z (length) plane.
    /// <c>isaretler</c> (the green lights and the beacon) are shown only while the zone is active (landing phase).
    /// </summary>
    public class InisBolgesi : MonoBehaviour
    {
        [SerializeField, Min(1f)] float en = 70f;
        [SerializeField, Min(1f)] float boy = 750f;
        [Tooltip("Height above the zone that still counts as 'in the zone' (m).")]
        [SerializeField, Min(0f)] float yukseklik = 12f;
        [SerializeField] GameObject[] isaretler;

        public float En => en;
        public float Boy => boy;
        public bool Aktif { get; private set; }

        /// <summary>True when the point lies over the zone (within <c>yukseklik</c> above it).</summary>
        public bool Iceride(Vector3 p)
        {
            Vector3 y = transform.InverseTransformPoint(p);
            return Mathf.Abs(y.x) <= en / 2f && Mathf.Abs(y.z) <= boy / 2f && y.y > -5f && y.y <= yukseklik;
        }

        public void Etkinlestir(bool aktif)
        {
            Aktif = aktif;
            if (isaretler != null)
                foreach (GameObject g in isaretler)
                    if (g != null) g.SetActive(aktif);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, yukseklik / 2f, 0f), new Vector3(en, yukseklik, boy));
        }
    }
}
