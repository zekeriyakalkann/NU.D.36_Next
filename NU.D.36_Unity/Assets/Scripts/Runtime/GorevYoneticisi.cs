using System;
using System.Collections.Generic;
using UnityEngine;

namespace NU36
{
    public enum GorevDurumu { Aktif, Donus, Tamamlandi, Basarisiz }

    /// <summary>
    /// Lab 4: mission state. Counts the required targets, publishes the remaining count, opens the
    /// return phase (enables the base trigger) once all required targets are collected, and
    /// completes the mission once when the base is reached afterwards. Phase 2: owns the countdown and
    /// the failure state (timeout or crash); success and failure exclude each other. It does not touch the score.
    /// </summary>
    public class GorevYoneticisi : MonoBehaviour
    {
        public const string NedenCakildi = "ÇAKILDIN";
        public const string NedenSureDoldu = "SÜRE DOLDU";

        [Tooltip("Every reconnaissance target of the mission (required and optional).")]
        [SerializeField] KesifHedefi[] hedefler;
        [Tooltip("Base trigger; kept inactive until the return phase.")]
        [SerializeField] UsGirisi ussu;
        [Tooltip("Optional: crash source. A crash fails the mission.")]
        [SerializeField] InisAlgilayici ucakAlgilayici;
        [Tooltip("Mission duration in seconds. Zero or less disables the countdown.")]
        [SerializeField] float sure = 150f;

        readonly HashSet<KesifHedefi> zorunlular = new HashSet<KesifHedefi>();
        readonly HashSet<KesifHedefi> toplananlar = new HashSet<KesifHedefi>();
        readonly List<KesifHedefi> abone = new List<KesifHedefi>();
        bool etkin;

        /// <summary>Raised with the remaining required-target count whenever it changes.</summary>
        public event Action<int> KalanDegisti;
        /// <summary>Raised once when the last required target is collected.</summary>
        public event Action DonusAsamasiBasladi;
        /// <summary>Raised once when the base is reached during the return phase.</summary>
        public event Action GorevTamamlandi;
        /// <summary>Raised once with the reason ("ÇAKILDIN", "SÜRE DOLDU") when the mission fails.</summary>
        public event Action<string> GorevBasarisiz;

        /// <summary>Crash source; a crash fails the mission.</summary>
        public InisAlgilayici Algilayici
        {
            get => ucakAlgilayici;
            set
            {
                Abone(false);
                ucakAlgilayici = value;
                if (etkin)
                    Abone(true);
            }
        }

        public int ZorunluSayisi => zorunlular.Count;
        public int Kalan => zorunlular.Count - toplananlar.Count;
        public bool DonusAsamasi { get; private set; }
        public bool Tamamlandi { get; private set; }
        public bool Basarisiz { get; private set; }
        public string BasarisizNedeni { get; private set; }

        /// <summary>The single mission state; derived from the flags above.</summary>
        public GorevDurumu Durum => Basarisiz ? GorevDurumu.Basarisiz : Tamamlandi ? GorevDurumu.Tamamlandi
            : DonusAsamasi ? GorevDurumu.Donus : GorevDurumu.Aktif;

        public bool GeriSayimAktif => sure > 0f;
        public float Sure => sure;
        /// <summary>Seconds left; 0 when the countdown is disabled.</summary>
        public float KalanSure { get; private set; }

        void Awake() => Kur(hedefler, ussu);

        void OnEnable()
        {
            etkin = true;
            Abone(true);
        }

        void OnDisable()
        {
            etkin = false;
            Abone(false);
        }

        void Update() => Adim(Time.deltaTime);

        /// <summary>Sets the duration (seconds; zero or less disables the countdown) and restarts it.</summary>
        public void SureAyarla(float saniye)
        {
            sure = saniye;
            KalanSure = GeriSayimAktif ? sure : 0f;
        }

        /// <summary>Advances the countdown. Does nothing once the mission is completed or failed.</summary>
        public void Adim(float dt)
        {
            if (!GeriSayimAktif || Tamamlandi || Basarisiz || dt <= 0f)
                return;
            KalanSure = Mathf.Max(0f, KalanSure - dt);
            if (KalanSure <= 0f)
                GoreviBasarisizYap(NedenSureDoldu);
        }

        /// <summary>Fails the mission once. Ignored after completion or an earlier failure. Returns true if it failed now.</summary>
        public bool GoreviBasarisizYap(string neden)
        {
            if (Tamamlandi || Basarisiz)
                return false;
            Basarisiz = true;
            BasarisizNedeni = neden;
            if (hedefler != null)
                foreach (KesifHedefi h in hedefler)
                    if (h != null)
                        h.Kapat();
            if (ussu != null)
                ussu.gameObject.SetActive(false);
            Debug.Log($"[GorevYoneticisi] Görev başarısız: {neden}");
            GorevBasarisiz?.Invoke(neden);
            return true;
        }

        void UcakCakildi(string neden) => GoreviBasarisizYap(NedenCakildi);

        /// <summary>(Re)initialises the mission from the given targets and base trigger. Also used by tests.</summary>
        public void Kur(KesifHedefi[] yeniHedefler, UsGirisi yeniUssu)
        {
            Abone(false);
            hedefler = yeniHedefler;
            ussu = yeniUssu;
            zorunlular.Clear();
            toplananlar.Clear();
            DonusAsamasi = false;
            Tamamlandi = false;
            Basarisiz = false;
            BasarisizNedeni = null;
            KalanSure = GeriSayimAktif ? sure : 0f;

            if (hedefler != null)
            {
                foreach (KesifHedefi h in hedefler)
                {
                    if (h == null)
                        Debug.LogError("[GorevYoneticisi] Hedef listesinde boş (None) eleman var.", this);
                    else if (h.Zorunlu)
                        zorunlular.Add(h);
                }
            }
            else
                Debug.LogError("[GorevYoneticisi] Hedef listesi atanmamış.", this);

            if (ussu == null)
                Debug.LogError("[GorevYoneticisi] Üs tetikleyicisi (UsGirisi) atanmamış; görev tamamlanamaz.", this);
            else
                ussu.gameObject.SetActive(false);

            if (etkin)
                Abone(true);
            KalanDegisti?.Invoke(Kalan);

            if (hedefler != null && zorunlular.Count == 0)
            {
                Debug.LogWarning("[GorevYoneticisi] Zorunlu hedef yok; dönüş aşaması hemen açılıyor.", this);
                DonusAsamasiniAc();
            }
        }

        void Abone(bool ekle)
        {
            if (ucakAlgilayici != null)
            {
                ucakAlgilayici.Cakildi -= UcakCakildi;
                if (ekle)
                    ucakAlgilayici.Cakildi += UcakCakildi;
            }
            foreach (KesifHedefi h in abone)
                if (h != null)
                    h.Toplandi -= HedefToplandi;
            abone.Clear();
            if (!ekle || hedefler == null)
                return;
            foreach (KesifHedefi h in hedefler)
                if (h != null)
                {
                    h.Toplandi += HedefToplandi;
                    abone.Add(h);
                }
        }

        void HedefToplandi(KesifHedefi hedef)
        {
            if (Basarisiz || !zorunlular.Contains(hedef) || !toplananlar.Add(hedef))
                return; // optional target, or already counted

            KalanDegisti?.Invoke(Kalan);
            Debug.Log($"[GorevYoneticisi] Kalan zorunlu hedef: {Kalan}");
            if (Kalan == 0)
                DonusAsamasiniAc();
        }

        void DonusAsamasiniAc()
        {
            if (DonusAsamasi)
                return;
            DonusAsamasi = true;
            if (ussu != null)
                ussu.gameObject.SetActive(true);
            Debug.Log("[GorevYoneticisi] Tüm zorunlu hedefler tamam: üsse dönün.");
            DonusAsamasiBasladi?.Invoke();
        }

        /// <summary>Called by the base trigger. Ignored before the return phase and after completion.</summary>
        public void UsseVarildi()
        {
            if (!DonusAsamasi || Tamamlandi || Basarisiz)
                return;
            Tamamlandi = true;
            Debug.Log("[GorevYoneticisi] Görev tamamlandı.");
            GorevTamamlandi?.Invoke();
        }
    }
}
