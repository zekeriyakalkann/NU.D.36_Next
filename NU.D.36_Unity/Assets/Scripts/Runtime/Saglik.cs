using System;
using UnityEngine;

namespace NU36
{
    /// <summary>Side of a health or a shot (Lab 8). <see cref="Notr"/> hurts and is hurt by everything (Labs 3–7).</summary>
    public enum Taraf { Notr, Dost, Dusman }

    /// <summary>What caused the damage (Lab 8: a tank is hurt only by an explosion).</summary>
    public enum HasarTuru { Genel, Mermi, Patlama }

    /// <summary>
    /// Lab 3: health with Observer-style events. Listeners subscribe to <see cref="CanDegisti"/>
    /// and <see cref="Oldu"/>; Saglik does not know who listens.
    /// Lab 8 adds a side (no damage from the same side: the player's rounds do not hurt the train) and the
    /// "explosions only" rule; the defaults (Notr, false) keep the Lab 3–7 behaviour.
    /// </summary>
    public class Saglik : MonoBehaviour
    {
        public const int VarsayilanCan = 100;

        [SerializeField, Min(1)] int baslangicCani = VarsayilanCan;
        [Tooltip("Lab 8: shots of the same side do no damage. Notr = no sides.")]
        [SerializeField] Taraf taraf = Taraf.Notr;
        [Tooltip("Lab 8: only explosions hurt (tank). Rounds glance off (Sekti).")]
        [SerializeField] bool yalnizPatlama;

        int can = VarsayilanCan;

        /// <summary>Raised with the new health value after every change.</summary>
        public event Action<int> CanDegisti;

        /// <summary>Raised once, when health reaches zero.</summary>
        public event Action Oldu;

        /// <summary>Raised when a hit did no damage because of <see cref="YalnizPatlama"/> (e.g. sparks on armour).</summary>
        public event Action<HasarTuru> Sekti;

        public int Can => can;
        public int BaslangicCani => baslangicCani;
        public bool OlduMu => can <= 0;
        public Taraf Taraf { get => taraf; set => taraf = value; }
        public bool YalnizPatlama { get => yalnizPatlama; set => yalnizPatlama = value; }

        void Awake() => can = baslangicCani;

        /// <summary>Sets the starting health (builders and tests; before or right after Awake).</summary>
        public void BaslangicAyarla(int deger)
        {
            baslangicCani = Mathf.Max(1, deger);
            can = baslangicCani;
        }

        /// <summary>Applies damage. Ignored for non-positive amounts and after death; health never goes below zero.</summary>
        public void Vur(int miktar)
        {
            if (miktar <= 0 || OlduMu)
                return;

            can = Mathf.Max(0, can - miktar);
            CanDegisti?.Invoke(can);
            if (can == 0)
                Oldu?.Invoke();
        }

        /// <summary>True when a hit of this kind from this side would do damage.</summary>
        public bool EtkilenirMi(HasarTuru tur, Taraf kaynak) =>
            !(kaynak != Taraf.Notr && kaynak == taraf) && !(yalnizPatlama && tur != HasarTuru.Patlama);

        /// <summary>Lab 8: damage with its cause and the shooter's side. Returns true if it did damage.</summary>
        public bool Vur(int miktar, HasarTuru tur, Taraf kaynak)
        {
            if (kaynak != Taraf.Notr && kaynak == taraf)
                return false;
            if (yalnizPatlama && tur != HasarTuru.Patlama)
            {
                if (!OlduMu) Sekti?.Invoke(tur);
                return false;
            }
            if (miktar <= 0 || OlduMu)
                return false;
            Vur(miktar);
            return true;
        }
    }
}
