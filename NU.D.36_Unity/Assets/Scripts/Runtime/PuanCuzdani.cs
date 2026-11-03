using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 4: owns the score. Listens to <see cref="KesifHedefi.Fotograflandi"/>, raises
    /// <see cref="PuanDegisti"/> on every change and <see cref="UcakKilidiAcildi"/> once when the
    /// unlock threshold is reached.
    /// </summary>
    public class PuanCuzdani : MonoBehaviour
    {
        [Header("Ekonomi")]
        [SerializeField, Min(0)] int inisPuani = 250;
        [SerializeField, Min(0)] int temizInisPuani = 150;

        [Header("Uçak kilidi")]
        [SerializeField] string kilitliUcakAdi = "Hawker Hurricane";
        [SerializeField, Min(0)] int kilitEsigi = 300;

        int puan;
        bool kilitAcildi;

        /// <summary>Raised with the new total after every change.</summary>
        public event Action<int> PuanDegisti;

        /// <summary>Raised once with the aircraft name when the threshold is reached.</summary>
        public event Action<string> UcakKilidiAcildi;

        public int Puan => puan;
        public bool KilitAcildiMi => kilitAcildi;
        public int InisPuani { get => inisPuani; set => inisPuani = Mathf.Max(0, value); }
        public int TemizInisPuani { get => temizInisPuani; set => temizInisPuani = Mathf.Max(0, value); }
        public int KilitEsigi { get => kilitEsigi; set => kilitEsigi = Mathf.Max(0, value); }
        public string KilitliUcakAdi { get => kilitliUcakAdi; set => kilitliUcakAdi = value; }

        void OnEnable()
        {
            KesifHedefi.Fotograflandi -= HedefFotograflandi; // never subscribe twice
            KesifHedefi.Fotograflandi += HedefFotograflandi;
        }

        void OnDisable() => KesifHedefi.Fotograflandi -= HedefFotograflandi;

        void HedefFotograflandi(int deger, bool zorunlu) => Ekle(deger);

        /// <summary>Landing reward (used by a later phase); a clean landing earns the extra amount.</summary>
        public void InisOdulu(bool temiz) => Ekle(inisPuani + (temiz ? temizInisPuani : 0));

        public void Ekle(int miktar)
        {
            if (miktar <= 0)
                return;

            puan += miktar;
            PuanDegisti?.Invoke(puan);

            if (!kilitAcildi && puan >= kilitEsigi)
            {
                kilitAcildi = true;
                Debug.Log($"[PuanCuzdani] {kilitliUcakAdi} kilidi açıldı ({puan} puan)");
                UcakKilidiAcildi?.Invoke(kilitliUcakAdi);
            }
        }
    }
}
