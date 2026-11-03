using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 5: an ammunition dump (the level's target). It owns a <see cref="Saglik"/>; when the health reaches zero
    /// (a bomb inside the blast radius) it is destroyed once: the intact model is hidden, the wreck is shown,
    /// a secondary explosion is spawned and <see cref="ImhaEdildi"/> is raised.
    /// The worksheet supplies its own MuhimmatMevzii.cs; that file was not available, so this is a minimal stand-in.
    /// </summary>
    [RequireComponent(typeof(Saglik))]
    public class MuhimmatMevzii : MonoBehaviour
    {
        [SerializeField, Min(0)] int puan = 100;
        [Tooltip("Shown while intact.")]
        [SerializeField] GameObject saglam;
        [Tooltip("Shown after destruction.")]
        [SerializeField] GameObject enkaz;
        [SerializeField] Patlama ikincilPatlama;
        [SerializeField, Min(0f)] float ikincilYaricap = 18f;

        Saglik saglik;

        /// <summary>Raised once when this dump is destroyed.</summary>
        public event Action<MuhimmatMevzii> ImhaEdildi;

        public int Puan => puan;
        public bool Imha { get; private set; }
        public Saglik Saglik => saglik != null ? saglik : saglik = GetComponent<Saglik>();

        void Awake()
        {
            saglik = GetComponent<Saglik>();
            if (enkaz != null)
                enkaz.SetActive(false);
        }

        void OnEnable() => Saglik.Oldu += Yok;
        void OnDisable() => Saglik.Oldu -= Yok;

        void Yok()
        {
            if (Imha)
                return;
            Imha = true;
            if (saglam != null)
                saglam.SetActive(false);
            if (enkaz != null)
                enkaz.SetActive(true);
            if (ikincilPatlama != null)
                Patlama.Yarat(transform.position + Vector3.up * 3f, ikincilYaricap, 0, 0, ikincilPatlama);
            Debug.Log($"[MuhimmatMevzii] {name} devre dışı bırakıldı (+{puan})");
            ImhaEdildi?.Invoke(this);
        }
    }
}
