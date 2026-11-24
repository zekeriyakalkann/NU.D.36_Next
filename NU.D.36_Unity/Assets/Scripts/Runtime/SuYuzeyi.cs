using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5): slow ripples on the sea. Scrolls the texture offset of the water's own material instance (the
    /// ripple normal map tiles with the base map), so the sun's glitter moves. The shared material asset is not changed.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class SuYuzeyi : MonoBehaviour
    {
        [SerializeField] Vector2 hiz = new Vector2(0.012f, 0.007f);

        Material malzeme;

        void Start() => malzeme = GetComponent<Renderer>().material;

        void Update()
        {
            if (malzeme != null)
                malzeme.mainTextureOffset = hiz * Time.time;
        }

        void OnDestroy()
        {
            if (malzeme != null) Destroy(malzeme);
        }
    }
}
