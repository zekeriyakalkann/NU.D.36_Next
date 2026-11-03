using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 5: ground height under a point. Casts a ray straight down against the "Arazi" layer (terrain chunks with
    /// Mesh Colliders and the hills), so hills count; where nothing is hit (no collider yet, or the layer does not
    /// exist) it falls back to the terrain function <see cref="AraziUretici.YuzeyYuksekligi"/>.
    /// </summary>
    public static class ZeminOlcer
    {
        public const string AraziKatmani = "Arazi";
        const float IsinBaslangici = 5000f;

        /// <summary>Mask of the "Arazi" layer (0 if the layer is not defined).</summary>
        public static int AraziMaskesi => LayerMask.GetMask(AraziKatmani);

        public static float Yukseklik(float x, float z)
        {
            int maske = AraziMaskesi;
            if (maske != 0 && Physics.Raycast(new Vector3(x, IsinBaslangici, z), Vector3.down, out RaycastHit vurus,
                    IsinBaslangici * 2f, maske, QueryTriggerInteraction.Ignore))
                return vurus.point.y;
            return AraziUretici.YuzeyYuksekligi(x, z);
        }

        /// <summary>Height of <paramref name="p"/> above the ground under it (metres).</summary>
        public static float Irtifa(Vector3 p) => p.y - Yukseklik(p.x, p.z);
    }
}
