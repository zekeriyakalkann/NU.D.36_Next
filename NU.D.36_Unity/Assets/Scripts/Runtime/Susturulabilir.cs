using System;
using UnityEngine;

namespace NU36
{
    /// <summary>Lab 9 appends <see cref="Gemi"/> (landing ship); existing values keep their numbers.</summary>
    public enum DusmanTuru { Tank, Ucaksavar, Ucak, Gemi }

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
        [Tooltip("Lab 9: a target that belongs to no fire zone (landing ships, guard aircraft). While active it is listed in " +
                 "Serbestler for the targeting assist, the HUD and the GPS. Off in Bölüm 4.")]
        [SerializeField] bool serbestHedef;

        /// <summary>Raised for every silenced fire source in the scene.</summary>
        public static event Action<Susturulabilir> HerhangiSusturuldu;
        /// <summary>Lab 9: active fire sources outside fire zones (see <see cref="SerbestHedef"/>).</summary>
        public static readonly System.Collections.Generic.List<Susturulabilir> Serbestler = new System.Collections.Generic.List<Susturulabilir>();
        public event Action<Susturulabilir> Susturuldu;

        public DusmanTuru Tur { get => tur; set => tur = value; }
        public Saglik Saglik => saglik;
        public bool Sustu { get; private set; }
        public ParticleSystem Duman => duman;
        public bool SerbestHedef { get => serbestHedef; set => serbestHedef = value; }

        void Awake()
        {
            if (saglik == null)
                saglik = GetComponent<Saglik>();
        }

        void OnEnable()
        {
            if (saglik != null) saglik.Oldu += Sus;
            if (serbestHedef && !Serbestler.Contains(this)) Serbestler.Add(this);
        }

        void OnDisable()
        {
            if (saglik != null) saglik.Oldu -= Sus;
            Serbestler.Remove(this);
        }

        /// <summary>Lab 9: leaves the free-target list for good (a landing ship that has unloaded and sails away).</summary>
        public void ListedenCik()
        {
            serbestHedef = false;
            Serbestler.Remove(this);
        }

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
