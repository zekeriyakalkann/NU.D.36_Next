using System.Collections.Generic;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Bölüm 3 single rule "Bulut" (paper v0.1): "E5, F5 ve G5 bulut karesidir: Orta irtifadaki uçağı bu 3 karede ışıldak
    /// yakalayamaz." One component per cloud square: a world box (the square at the Orta band). A searchlight cannot catch
    /// an aircraft inside any active cloud box and loses one that flies into it (<see cref="Isildak"/>). Visual: the
    /// particle cloud under this object (built by the scene builder).
    /// </summary>
    public class Bulut : MonoBehaviour
    {
        static readonly List<Bulut> Etkin = new List<Bulut>();

        [Tooltip("Paper square of this cloud (for the HUD and the tests).")]
        [SerializeField] string kare = "F5";
        [Tooltip("Size of the hiding box (m), centred on this object: one paper square wide, the Orta band high.")]
        [SerializeField] Vector3 boyut = new Vector3(200f, 130f, 200f);

        public string Kare => kare;
        public Bounds Alan => new Bounds(transform.position, boyut);

        public void Ayarla(string yeniKare, Vector3 yeniBoyut)
        {
            kare = yeniKare;
            boyut = yeniBoyut;
        }

        void OnEnable() => Etkin.Add(this);
        void OnDisable() => Etkin.Remove(this);

        /// <summary>True when <paramref name="p"/> is inside an active cloud box.</summary>
        public static bool Gizler(Vector3 p) => Icindeki(p) != null;

        /// <summary>The active cloud that contains <paramref name="p"/>, or null.</summary>
        public static Bulut Icindeki(Vector3 p)
        {
            foreach (Bulut b in Etkin)
                if (b != null && b.Alan.Contains(p))
                    return b;
            return null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.85f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, boyut);
        }
    }
}
