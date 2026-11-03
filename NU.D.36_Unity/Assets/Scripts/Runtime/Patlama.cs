using System.Collections.Generic;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 5: explosion. <see cref="Yarat"/> damages every <see cref="Saglik"/> whose collider lies within the radius
    /// on the given layers (each health once) and optionally spawns the visual (a prefab with this component), which
    /// grows and fades out by itself.
    /// The worksheet supplies its own Patlama.cs; that file was not available, so this is a minimal stand-in with the
    /// call shape used in the worksheet: Patlama.Yarat(point, radius, damage, layers).
    /// </summary>
    public class Patlama : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] float sure = 0.9f;
        [Tooltip("Visual diameter / damage radius at full size (2 = the sphere shows the damage radius).")]
        [SerializeField, Min(0f)] float buyume = 2f;

        float gecen;
        float olcek = 1f;
        static readonly Collider[] Tampon = new Collider[64];
        static readonly HashSet<Saglik> Vurulanlar = new HashSet<Saglik>();

        /// <summary>Damages every Saglik in range once and spawns the visual. Returns how many were damaged.</summary>
        public static int Yarat(Vector3 nokta, float yaricap, int hasar, LayerMask etkilenen, Patlama gorsel = null)
        {
            int n = Physics.OverlapSphereNonAlloc(nokta, yaricap, Tampon, etkilenen, QueryTriggerInteraction.Collide);
            Vurulanlar.Clear();
            for (int i = 0; i < n; i++)
            {
                var s = Tampon[i].GetComponentInParent<Saglik>();
                if (s != null && Vurulanlar.Add(s))
                    s.Vur(hasar);
            }
            int sayi = Vurulanlar.Count;
            Vurulanlar.Clear();

            if (gorsel != null)
            {
                Patlama p = Instantiate(gorsel, nokta, Quaternion.identity);
                p.olcek = yaricap;
                p.transform.localScale = Vector3.one * (yaricap * 0.3f);
            }
            Debug.Log($"[Patlama] {nokta:F0}, yarıçap {yaricap:F0} m, {sayi} hedef etkilendi");
            return sayi;
        }

        void Update()
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            // Fast growth to the full radius, then shrink away.
            float s = t < 0.25f ? Mathf.Lerp(0.3f, 1f, t / 0.25f) : Mathf.Lerp(1f, 0.1f, (t - 0.25f) / 0.75f);
            transform.localScale = Vector3.one * (olcek * s * buyume);
        }
    }
}
