using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 3: health with Observer-style events. Listeners subscribe to <see cref="CanDegisti"/>
    /// and <see cref="Oldu"/>; Saglik does not know who listens.
    /// </summary>
    public class Saglik : MonoBehaviour
    {
        public const int VarsayilanCan = 100;

        [SerializeField, Min(1)] int baslangicCani = VarsayilanCan;

        int can = VarsayilanCan;

        /// <summary>Raised with the new health value after every change.</summary>
        public event Action<int> CanDegisti;

        /// <summary>Raised once, when health reaches zero.</summary>
        public event Action Oldu;

        public int Can => can;
        public bool OlduMu => can <= 0;

        void Awake() => can = baslangicCani;

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
    }
}
