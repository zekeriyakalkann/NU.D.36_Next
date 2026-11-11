using System;
using UnityEngine;

namespace NU36
{
    public enum GorevAsamasi { Kalkis, Saldiri, Inis, Bitti }

    /// <summary>
    /// Lab 7 (Hafta 7): the night mission loop of Bölüm 3 (GDD §9.4: night, three searchlights, a narrow target, dawn).
    ///   Kalkış  — the aircraft stands on the runway; the phase ends when it leaves the ground.
    ///   Saldırı — destroy the main target (<c>anaHedef</c>: the H5–J5 bridge). The searchlight towers are optional targets
    ///             (points; a destroyed tower puts its light out). A low pass over the own airfield refills the bombs.
    ///   İniş    — "üsse dönüş": when the bridge is down the runway lights and the landing zone come on: land and stop in it.
    /// The mission fails when the aircraft crashes (ground, water, structure, hard landing), its health reaches zero, or dawn
    /// comes (<c>safakSuresi</c> seconds after the start) before it has landed.
    /// Without an <c>anaHedef</c> every target must be destroyed (the first Lab 7 rule).
    /// Score: points of the targets + landing bonus + 2 × remaining health (INFERRED, documented in Lab7_Implementation.md).
    /// </summary>
    public class GeceGorevi : MonoBehaviour
    {
        public const string NedenCakildi = "ÇAKILDIN";
        public const string NedenVuruldu = "VURULDUN";
        public const string NedenBasari = "GÖREV TAMAMLANDI";
        public const string NedenSafak = "ŞAFAK SÖKTÜ";

        [SerializeField] Yikilabilir[] hedefler;
        [Tooltip("The target that must fall (the bridge). Empty = every target must fall.")]
        [SerializeField] Yikilabilir anaHedef;
        [Tooltip("Seconds from the start to dawn: the mission fails if the aircraft has not landed by then. 0 = no dawn.")]
        [SerializeField, Min(0f)] float safakSuresi = 480f;
        [Tooltip("A warning is shown this many seconds before dawn.")]
        [SerializeField, Min(0f)] float safakUyarisi = 90f;
        [SerializeField] BombaYuku yuk;
        [SerializeField] Saglik ucakSagligi;
        [SerializeField] InisAlgilayici algilayici;
        [SerializeField] UcusKontrol ucus;
        [SerializeField] InisBolgesi inisBolgesi;
        [Tooltip("Runway lights: on for the take-off and the landing, off (blackout) during the attack.")]
        [SerializeField] GameObject pistIsiklari;
        [Tooltip("Speed (m/s) below which the aircraft counts as stopped.")]
        [SerializeField, Min(0.1f)] float durmaHizi = 3f;
        [SerializeField, Min(0)] int inisPuani = 300;

        public event Action<GorevAsamasi> AsamaDegisti;
        public event Action<Yikilabilir> HedefYikildi;
        public event Action<bool, string> GorevBitti;   // (success, reason)

        public GorevAsamasi Asama { get; private set; } = GorevAsamasi.Kalkis;
        public float GecenSure { get; private set; }
        public int Puan { get; private set; }
        public int IkmalSayisi { get; private set; }
        public bool Bitti => Asama == GorevAsamasi.Bitti;
        public bool Basarili { get; private set; }
        public string Neden { get; private set; }
        /// <summary>What happened in a crash, for the end screen ("suya düştü" over the river, else the ground probe's reason).</summary>
        public string Ayrinti { get; private set; }
        /// <summary>Message for the HUD after a landing outside the zone or a landing before the targets are down.</summary>
        public string Bildirim { get; private set; }
        public float BildirimZamani { get; private set; } = -100f;
        public Yikilabilir[] Hedefler => hedefler;
        public Yikilabilir AnaHedef => anaHedef;
        public float SafakSuresi { get => safakSuresi; set => safakSuresi = Mathf.Max(0f, value); }
        /// <summary>Seconds left until dawn (infinite without a dawn).</summary>
        public float KalanSure => safakSuresi > 0f ? Mathf.Max(0f, safakSuresi - GecenSure) : float.PositiveInfinity;
        /// <summary>0 at the start of the night, 1 at dawn.</summary>
        public float SafakOrani => safakSuresi > 0f ? Mathf.Clamp01(GecenSure / safakSuresi) : 0f;
        /// <summary>True when the main target (or, without one, every target) is down.</summary>
        public bool HedefTamam => anaHedef != null ? anaHedef.Yikik : KalanHedef <= 0;
        bool safakUyarildi;
        public BombaYuku Yuk => yuk;
        public UcusKontrol Ucus => ucus;
        public InisAlgilayici Algilayici => algilayici;
        public InisBolgesi InisBolgesi => inisBolgesi;
        public int KalanCan => ucakSagligi != null ? ucakSagligi.Can : 0;
        public int ToplamHedef => hedefler == null ? 0 : hedefler.Length;
        public int YikilanHedef { get; private set; }
        public int KalanHedef => ToplamHedef - YikilanHedef;

        public int Sayi(HedefTuru tur, bool yikik)
        {
            int n = 0;
            if (hedefler != null)
                foreach (Yikilabilir h in hedefler)
                    if (h != null && h.Tur == tur && (!yikik || h.Yikik)) n++;
            return n;
        }

        void Awake()
        {
            if (hedefler == null || hedefler.Length == 0)
                Debug.LogError("[GeceGorevi] Hedef listesi boş.", this);
        }

        void OnEnable() => Abone(true);
        void OnDisable() => Abone(false);

        void Start()
        {
            if (inisBolgesi != null)
                inisBolgesi.Etkinlestir(false);
            if (pistIsiklari != null)
                pistIsiklari.SetActive(true);
            if (algilayici != null && !algilayici.Yerde)
                AsamaAyarla(GorevAsamasi.Saldiri);   // started in the air
        }

        void Abone(bool ekle)
        {
            if (hedefler != null)
                foreach (Yikilabilir h in hedefler)
                    if (h != null)
                    {
                        h.Yikildi -= HedefYikilinca;
                        if (ekle) h.Yikildi += HedefYikilinca;
                    }
            if (ucakSagligi != null)
            {
                ucakSagligi.Oldu -= Vuruldu;
                if (ekle) ucakSagligi.Oldu += Vuruldu;
            }
            if (algilayici != null)
            {
                algilayici.Cakildi -= Cakildi;
                algilayici.InisYapildi -= InisYapilinca;
                if (ekle)
                {
                    algilayici.Cakildi += Cakildi;
                    algilayici.InisYapildi += InisYapilinca;
                }
            }
        }

        void Update() => Adim(Time.deltaTime);

        /// <summary>Advances the mission clock and checks the phase conditions.</summary>
        public void Adim(float dt)
        {
            if (Bitti)
                return;
            GecenSure += Mathf.Max(0f, dt);
            if (safakSuresi > 0f)
            {
                if (GecenSure >= safakSuresi)
                {
                    Ayrinti = HedefTamam ? "köprü yıkıldı ama üsse dönülmedi" : "köprü hâlâ ayakta";
                    Bitir(false, NedenSafak);
                    return;
                }
                if (!safakUyarildi && KalanSure <= safakUyarisi)
                {
                    safakUyarildi = true;
                    Bildir(HedefTamam ? $"Şafağa {Mathf.CeilToInt(KalanSure)} saniye! Hemen piste in." : $"Şafağa {Mathf.CeilToInt(KalanSure)} saniye! Köprü hâlâ ayakta.");
                }
            }
            switch (Asama)
            {
                case GorevAsamasi.Kalkis:
                    if (algilayici != null && !algilayici.Yerde && ucus != null && ucus.Hiz > 10f)
                    {
                        AsamaAyarla(GorevAsamasi.Saldiri);
                        Bildir("Pist ışıkları karartıldı. Hedeflere yönel.");
                    }
                    break;
                case GorevAsamasi.Inis:
                    if (algilayici != null && algilayici.Yerde && ucus != null && ucus.Hiz < durmaHizi)
                    {
                        if (inisBolgesi == null || inisBolgesi.Iceride(ucus.transform.position))
                            Bitir(true, NedenBasari);
                        else if (Time.time - BildirimZamani > 4f)
                            Bildir("İşaretli iniş bölgesinin dışında durdun: bölgeye taksi yap (↑ gaz, Q/E yön).");
                    }
                    break;
            }
        }

        void AsamaAyarla(GorevAsamasi yeni)
        {
            if (Asama == yeni)
                return;
            Asama = yeni;
            if (pistIsiklari != null && yeni != GorevAsamasi.Bitti)
                pistIsiklari.SetActive(yeni != GorevAsamasi.Saldiri);
            if (inisBolgesi != null)
                inisBolgesi.Etkinlestir(yeni == GorevAsamasi.Inis);
            Debug.Log($"[GeceGorevi] Aşama: {yeni}");
            AsamaDegisti?.Invoke(yeni);
        }

        /// <summary>Shows a message in the HUD's message line (also used by the targeting assist).</summary>
        public void Bildir(string metin)
        {
            Bildirim = metin;
            BildirimZamani = Time.time;
        }

        void HedefYikilinca(Yikilabilir h)
        {
            if (Bitti)
                return;
            YikilanHedef++;
            Puan += h.Puan;
            HedefYikildi?.Invoke(h);
            Bildir(anaHedef != null && h != anaHedef ? $"{h.GorunenAd} söndü! (+{h.Puan})" : $"{h.GorunenAd} yıkıldı! Kalan hedef: {KalanHedef}");
            if (HedefTamam && (Asama == GorevAsamasi.Saldiri || Asama == GorevAsamasi.Kalkis))
            {
                AsamaAyarla(GorevAsamasi.Inis);
                Bildir(anaHedef != null ? "Köprü çöktü! Şafak sökmeden üsse dön: yeşil işaretli bölgeye in."
                                        : "Bütün hedefler yıkıldı! Piste dön ve yeşil işaretli bölgeye in.");
            }
        }

        /// <summary>Resupply (low pass over the airfield): full bomb load, only when it is not full.</summary>
        public void Ikmal()
        {
            if (Bitti || yuk == null || yuk.Kalan >= yuk.Kapasite)
                return;
            yuk.Ikmal();
            IkmalSayisi++;
            Bildir($"İkmal: {yuk.Kapasite} bomba yüklendi.");
        }

        void InisYapilinca()
        {
            if (Bitti || ucus == null)
                return;
            if (algilayici != null && algilayici.SonInisNoktasi.y - algilayici.TekerOfseti < GeceHaritasi.SuSeviyesi + 0.5f)
            {
                algilayici.Carp("suya düştü");   // the river is not a runway
                return;
            }
            if (Asama == GorevAsamasi.Inis)
                Bildir(inisBolgesi == null || inisBolgesi.Iceride(ucus.transform.position)
                    ? "Teker koydun! Gazı kes (↓) ve dur."
                    : "İniş bölgesinin dışına teker koydun: dur ve bölgeye taksi yap, ya da gaz verip yeniden yaklaş.");
            else if (Asama == GorevAsamasi.Saldiri)
                Bildir(anaHedef != null ? "Yere indin. Köprü ayakta: görev bitmedi." : "Yere indin. Görev bitmeden iniş sayılmaz: hedefler bekliyor.");
        }

        void Vuruldu()
        {
            Ayrinti = "uçaksavar ateşi";
            Bitir(false, NedenVuruldu);
        }
        void Cakildi(string neden)
        {
            Vector3 p = ucus != null ? ucus.transform.position : Vector3.zero;
            Ayrinti = ucus != null && GeceHaritasi.Yukseklik(p.x, p.z) < GeceHaritasi.SuSeviyesi ? "suya düştü" : neden;
            Bitir(false, NedenCakildi);
        }

        /// <summary>Ends the mission once.</summary>
        public void Bitir(bool basari, string neden)
        {
            if (Bitti)
                return;
            Basarili = basari;
            Neden = neden;
            if (basari)
                Puan += inisPuani + 2 * KalanCan;
            else if (ucus != null)
                ucus.enabled = false;
            AsamaAyarla(GorevAsamasi.Bitti);
            Debug.Log($"[GeceGorevi] Görev {(basari ? "TAMAMLANDI" : "BAŞARISIZ")} ({neden}). Süre {GecenSure:F1} s, " +
                      $"hedef {YikilanHedef}/{ToplamHedef}, ikmal {IkmalSayisi}, can {KalanCan}, puan {Puan}");
            GorevBitti?.Invoke(basari, neden);
        }
    }
}
