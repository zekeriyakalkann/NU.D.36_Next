using System;
using UnityEngine;

namespace NU36
{
    public enum DusmanTuru { Tank, Ucaksavar, Ucak }

    /// <summary>
    /// Lab 8 (Bölüm 4), worksheet 04: a fire source that can be silenced. When its health runs out the firing components
    /// are switched off for good, smoke starts and (tank) the turret sags; an enemy aircraft withdraws by itself
    /// (<see cref="DusmanUcagi"/>). Every silencing is announced (<see cref="HerhangiSusturuldu"/>) for the mission's count.
    /// </summary>
    public class Susturulabilir : MonoBehaviour
    {
        [SerializeField] DusmanTuru tur;
        [SerializeField] Saglik saglik;
        [Tooltip("Switched off when silenced (the gun, the turret, the radar).")]
        [SerializeField] Behaviour[] atesBilesenleri = new Behaviour[0];
        [Tooltip("Started when silenced (inactive child).")]
        [SerializeField] ParticleSystem duman;
        [Tooltip("Optional part that sags when silenced (tank turret, gun barrel).")]
        [SerializeField] Transform egilen;
        [SerializeField] Vector3 egilme = new Vector3(14f, 0f, 0f);

        /// <summary>Raised for every silenced fire source in the scene.</summary>
        public static event Action<Susturulabilir> HerhangiSusturuldu;
        public event Action<Susturulabilir> Susturuldu;

        public DusmanTuru Tur { get => tur; set => tur = value; }
        public Saglik Saglik => saglik;
        public bool Sustu { get; private set; }
        public ParticleSystem Duman => duman;

        void Awake()
        {
            if (saglik == null)
                saglik = GetComponent<Saglik>();
        }

        void OnEnable() { if (saglik != null) saglik.Oldu += Sus; }
        void OnDisable() { if (saglik != null) saglik.Oldu -= Sus; }

        /// <summary>Silences the source (also called when the health runs out).</summary>
        public void Sus()
        {
            if (Sustu)
                return;
            Sustu = true;
            foreach (Behaviour b in atesBilesenleri)
                if (b != null)
                    b.enabled = false;
            if (duman != null)
            {
                duman.gameObject.SetActive(true);
                duman.Play(true);
            }
            if (egilen != null)
                egilen.localRotation *= Quaternion.Euler(egilme);
            Susturuldu?.Invoke(this);
            HerhangiSusturuldu?.Invoke(this);
        }
    }
}
