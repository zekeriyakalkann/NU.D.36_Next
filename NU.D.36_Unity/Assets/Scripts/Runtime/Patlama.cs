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
        [Tooltip("Lab 7: optional effect spawned unscaled at the explosion point when this visual is used (smoke, sparks, flash). " +
                 "It removes itself. Empty in Labs 5–6.")]
        [SerializeField] GameObject kaliciEfekt;

        public GameObject KaliciEfekt { get => kaliciEfekt; set => kaliciEfekt = value; }

        float gecen;
        float olcek = 1f;
        static readonly Collider[] Tampon = new Collider[64];
        static readonly HashSet<Saglik> Vurulanlar = new HashSet<Saglik>();

        /// <summary>Point of the latest explosion (Lab 7: a long target collapses where it was hit).</summary>
        public static Vector3 SonNokta { get; private set; }
        /// <summary>Time.time of the latest explosion (negative before the first).</summary>
        public static float SonZaman { get; private set; } = -1000f;

        /// <summary>Damages every Saglik in range once and spawns the visual. Returns how many were in range.
        /// Lab 8: <paramref name="kaynak"/> = side of the bomb's owner (a health of the same side is not hurt).</summary>
        public static int Yarat(Vector3 nokta, float yaricap, int hasar, LayerMask etkilenen, Patlama gorsel = null, Taraf kaynak = Taraf.Notr)
        {
            SonNokta = nokta;
            SonZaman = Time.time;
            int n = Physics.OverlapSphereNonAlloc(nokta, yaricap, Tampon, etkilenen, QueryTriggerInteraction.Collide);
            Vurulanlar.Clear();
            for (int i = 0; i < n; i++)
            {
                var s = Tampon[i].GetComponentInParent<Saglik>();
                if (s != null && Vurulanlar.Add(s))
                    s.Vur(hasar, HasarTuru.Patlama, kaynak);
            }
            int sayi = Vurulanlar.Count;
            Vurulanlar.Clear();

            if (gorsel != null)
            {
                Patlama p = Instantiate(gorsel, nokta, Quaternion.identity);
                p.olcek = yaricap;
                p.transform.localScale = Vector3.one * (yaricap * 0.3f);
                if (gorsel.kaliciEfekt != null)
                    Instantiate(gorsel.kaliciEfekt, nokta, Quaternion.identity);
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
