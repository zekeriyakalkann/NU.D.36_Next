using System;
using UnityEngine;

namespace NU36
{
    public enum Bolum { KesifDongusu, MuhimmatAvi }

    /// <summary>
    /// Lab 5, Step 05: the level loop of GDD §9.2 (Bölüm 2, "Mühimmat Avı"): destroy the 4 ammunition dumps within
    /// 180 s; resupply at the base gives a full bomb load and +90 s (half the starting time), only when the load is not
    /// full. Fails on time-out, when the aircraft's health reaches zero, or on a crash. Score: points of the destroyed
    /// dumps, plus the remaining whole seconds on success (INFERRED: the worksheet asks for a total score but gives no
    /// formula). The worksheet supplies its own SeviyeYoneticisi.cs; that file was not available, so this is a stand-in
    /// that keeps the worksheet's UsseVarildi() code.
    /// </summary>
    public class SeviyeYoneticisi : MonoBehaviour
    {
        public const string NedenSure = "SÜRE DOLDU";
        public const string NedenVuruldu = "VURULDUN";
        public const string NedenCakildi = "ÇAKILDIN";

        [SerializeField] Bolum bolum = Bolum.MuhimmatAvi;
        [SerializeField] MuhimmatMevzii[] mevziler;
        [SerializeField] BombaYuku yuk;
        [SerializeField] Saglik ucakSagligi;
        [SerializeField] InisAlgilayici algilayici;
        [SerializeField] UcusKontrol ucus;
        [Tooltip("Starting time (s). Worksheet experiment: 180 → 150.")]
        [SerializeField, Min(1f)] float baslangicSuresi = 180f;
        [Tooltip("Time added by a resupply (s): half of the starting time.")]
        [SerializeField, Min(0f)] float ikmalEkSuresi = 90f;

        public event Action<int> KalanMevziDegisti;
        public event Action<bool, string> SeviyeBitti; // (success, reason)

        public Bolum Bolum => bolum;
        public float KalanSure { get; private set; }
        public float GecenSure { get; private set; }
        public int IkmalSayisi { get; private set; }
        public bool Bitti { get; private set; }
        public bool Basarili { get; private set; }
        public string Neden { get; private set; }
        public int ImhaSayisi { get; private set; }
        public int MevziSayisi => mevziler == null ? 0 : mevziler.Length;
        public int KalanMevzi => MevziSayisi - ImhaSayisi;
        public int Puan { get; private set; }
        public int KalanCan => ucakSagligi != null ? ucakSagligi.Can : 0;
        public BombaYuku Yuk => yuk;
        public float BaslangicSuresi => baslangicSuresi;
        public float IkmalEkSuresi => ikmalEkSuresi;

        void Awake() => Kur(mevziler, yuk, ucakSagligi, algilayici, ucus);

        /// <summary>(Re)initialises the level. Also used by tests.</summary>
        public void Kur(MuhimmatMevzii[] yeniMevziler, BombaYuku yeniYuk, Saglik saglik, InisAlgilayici yeniAlgilayici, UcusKontrol yeniUcus)
        {
            Abone(false);
            mevziler = yeniMevziler;
            yuk = yeniYuk;
            ucakSagligi = saglik;
            algilayici = yeniAlgilayici;
            ucus = yeniUcus;
            KalanSure = baslangicSuresi;
            GecenSure = 0f;
            IkmalSayisi = 0;
            ImhaSayisi = 0;
            Puan = 0;
            Bitti = false;
            Basarili = false;
            Neden = null;
            if (mevziler == null || mevziler.Length == 0)
                Debug.LogError("[SeviyeYoneticisi] Mühimmat mevzii listesi boş.", this);
            Abone(true);
            KalanMevziDegisti?.Invoke(KalanMevzi);
        }

        void OnDestroy() => Abone(false);

        void Abone(bool ekle)
        {
            if (mevziler != null)
                foreach (MuhimmatMevzii m in mevziler)
                    if (m != null)
                    {
                        m.ImhaEdildi -= MevziImha;
                        if (ekle) m.ImhaEdildi += MevziImha;
                    }
            if (ucakSagligi != null)
            {
                ucakSagligi.Oldu -= Vuruldu;
                if (ekle) ucakSagligi.Oldu += Vuruldu;
            }
            if (algilayici != null)
            {
                algilayici.Cakildi -= Cakildi;
                if (ekle) algilayici.Cakildi += Cakildi;
            }
        }

        void Update() => Adim(Time.deltaTime);

        /// <summary>Advances the clock. Does nothing after the end.</summary>
        public void Adim(float dt)
        {
            if (Bitti || dt <= 0f)
                return;
            GecenSure += dt;
            KalanSure = Mathf.Max(0f, KalanSure - dt);
            if (KalanSure <= 0f)
                Bitir(false, NedenSure);
        }

        public void SureAyarla(float saniye)
        {
            baslangicSuresi = Mathf.Max(1f, saniye);
            KalanSure = baslangicSuresi;
        }

        /// <summary>Called by the base trigger (<see cref="IkmalNoktasi"/>).</summary>
        public void UsseVarildi()
        {
            if (Bitti || bolum != Bolum.MuhimmatAvi) return;
            if (yuk == null || yuk.Kalan >= yuk.Kapasite) return;   // yük doluysa ikmal yok

            yuk.Ikmal();
            KalanSure += ikmalEkSuresi;      // +90 s: başlangıcın YARISI
            IkmalSayisi++;
            Debug.Log($"[SeviyeYoneticisi] İkmal #{IkmalSayisi}: +{ikmalEkSuresi:F0} s, kalan süre {KalanSure:F0} s");
        }

        void MevziImha(MuhimmatMevzii m)
        {
            if (Bitti)
                return;
            ImhaSayisi++;
            Puan += m.Puan;
            KalanMevziDegisti?.Invoke(KalanMevzi);
            if (KalanMevzi <= 0)
                Bitir(true, "TÜM MEVZİLER İMHA");
        }

        void Vuruldu() => Bitir(false, NedenVuruldu);
        void Cakildi(string neden) => Bitir(false, NedenCakildi);

        /// <summary>Ends the level once. Success adds the remaining whole seconds to the score.</summary>
        public void Bitir(bool basari, string neden)
        {
            if (Bitti)
                return;
            Bitti = true;
            Basarili = basari;
            Neden = neden;
            if (basari)
                Puan += Mathf.FloorToInt(KalanSure);
            else if (ucus != null)
                ucus.enabled = false; // the aircraft stops on failure
            Debug.Log($"[SeviyeYoneticisi] Bölüm {(basari ? "TAMAMLANDI" : "BAŞARISIZ")} ({neden}). " +
                      $"Süre {GecenSure:F1} s, ikmal {IkmalSayisi}, kalan can {KalanCan}, puan {Puan}");
            SeviyeBitti?.Invoke(basari, neden);
        }
    }
}
