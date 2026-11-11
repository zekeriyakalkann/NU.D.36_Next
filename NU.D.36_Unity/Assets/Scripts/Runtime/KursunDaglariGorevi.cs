using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4 · Kurşun Dağları): the mission flow. Escort (the train runs to the safe zone) → "Üsse dön" when the
    /// train is inside the safe zone (worksheet 08) → "Görev tamam" when the aircraft enters the base zone low (plan
    /// decision 3: within <see cref="usYaricapi"/> of the base, below <see cref="usTavani"/> above the ground).
    /// Failures (worksheet 05): the train's health runs out → "TREN DURDU"; the aircraft's → "VURULDUN"; a crash →
    /// "ÇAKILDIN". After a failure the level restarts after <see cref="yenidenSuresi"/> seconds (R at once).
    /// Counts the silenced fire sources of the scene's zones.
    /// </summary>
    public class KursunDaglariGorevi : MonoBehaviour
    {
        public enum Asama { Eskort, UsseDon, Tamam, TrenDurdu, Vuruldun, Cakildin }

        public const string YaziTrenDurdu = "TREN DURDU", YaziVuruldun = "VURULDUN", YaziCakildin = "ÇAKILDIN", YaziTamam = "GÖREV TAMAM";

        [SerializeField] Tren tren;
        [SerializeField] UcusKontrol ucus;
        [SerializeField] Saglik ucakSagligi;
        [SerializeField] InisAlgilayici algilayici;
        [SerializeField] Makineli makineli;
        [SerializeField] BombaYuku yuk;
        [SerializeField] Bolum4Hud hud;
        [SerializeField] AtesBolgesi[] bolgeler = new AtesBolgesi[0];
        [Header("Üs")]
        [SerializeField] Vector3 usMerkezi = KursunDaglariHaritasi.UsMerkezi;
        [SerializeField, Min(1f)] float usYaricapi = KursunDaglariHaritasi.UsYaricapi;
        [SerializeField, Min(1f)] float usTavani = KursunDaglariHaritasi.UsTavani;
        [Header("Yeniden başlama")]
        [SerializeField, Min(0.5f)] float yenidenSuresi = 4f;
        [SerializeField] string menuSahnesi = "AnaMenu";
        [SerializeField] InputAction yenidenTusu = new InputAction("YenidenBasla", InputActionType.Button, "<Keyboard>/r");

        float baslangic, yenidenZamani = -1f;
        Vector3 dususHizi;
        bool dusuyor;

        /// <summary>Loads a scene by name (tests replace it).</summary>
        public Action<string> SahneYukle = ad => SceneManager.LoadScene(ad);
        public event Action<Asama> AsamaDegisti;

        public Asama Simdiki { get; private set; } = Asama.Eskort;
        public bool Bitti => Simdiki != Asama.Eskort && Simdiki != Asama.UsseDon;
        public bool Basarisiz => Simdiki == Asama.TrenDurdu || Simdiki == Asama.Vuruldun || Simdiki == Asama.Cakildin;
        /// <summary>The shot-down aircraft is still falling (phase 4: it falls instead of freezing in the air).</summary>
        public bool UcakDusuyor => dusuyor;
        public int Susturulan { get; private set; }
        public int SusturulanTank { get; private set; }
        public int SusturulanUcaksavar { get; private set; }
        public int SusturulanUcak { get; private set; }
        /// <summary>Fire sources in all zones (worksheet: 4 tanks, 4 AA guns, 4 aircraft).</summary>
        public int ToplamKaynak { get { int n = 0; foreach (var b in bolgeler) if (b != null) n += b.Toplam; return n; } }
        /// <summary>Seconds since the start (frozen at the end).</summary>
        public float Sure { get; private set; }
        public AtesBolgesi[] Bolgeler => bolgeler;
        public Tren Tren => tren;
        public Vector3 UsMerkezi => usMerkezi;
        public float UsYaricapi => usYaricapi;
        public float UsTavani => usTavani;
        /// <summary>Horizontal distance from the aircraft to the base centre.</summary>
        public float UsUzakligi => ucus == null ? 0f : Vector2.Distance(new Vector2(ucus.transform.position.x, ucus.transform.position.z), new Vector2(usMerkezi.x, usMerkezi.z));
        /// <summary>The zone the train is in or about to enter (null in the calm stretches).</summary>
        public AtesBolgesi EtkinBolge { get { foreach (var b in bolgeler) if (b != null && b.Simdiki == AtesBolgesi.Durum.Etkin) return b; return null; } }

        /// <summary>Text of the current task line (worksheet 08).</summary>
        public string GorevYazisi()
        {
            switch (Simdiki)
            {
                case Asama.Eskort:
                    AtesBolgesi b = EtkinBolge;
                    return b != null ? $"Treni koru — {b.Ad}" : "Treni güvenli bölgeye ulaştır";
                case Asama.UsseDon: return $"Üsse dön ({UsUzakligi / 1000f:F1} km)";
                case Asama.Tamam: return "Görev tamam";
                case Asama.TrenDurdu: return "Tren durdu";
                case Asama.Vuruldun: return "Vuruldun";
                default: return "Çakıldın";
            }
        }

        void Awake()
        {
            if (algilayici == null && ucus != null) algilayici = ucus.GetComponent<InisAlgilayici>();
            if (ucakSagligi == null && ucus != null) ucakSagligi = ucus.GetComponent<Saglik>();
        }

        void OnEnable()
        {
            yenidenTusu.Enable();
            if (tren != null)
            {
                tren.GuvenliBolgeyeVardi += TrenVardi;
                if (tren.Saglik != null) tren.Saglik.Oldu += TrenVuruldu;
            }
            if (ucakSagligi != null) ucakSagligi.Oldu += UcakVuruldu;
            if (algilayici != null) algilayici.Cakildi += Cakildi;
            Susturulabilir.HerhangiSusturuldu += Susturuldu;
            foreach (var b in bolgeler)
                if (b != null) b.Etkinlesti += BolgeBasladi;
        }

        void OnDisable()
        {
            yenidenTusu.Disable();
            if (tren != null)
            {
                tren.GuvenliBolgeyeVardi -= TrenVardi;
                if (tren.Saglik != null) tren.Saglik.Oldu -= TrenVuruldu;
            }
            if (ucakSagligi != null) ucakSagligi.Oldu -= UcakVuruldu;
            if (algilayici != null) algilayici.Cakildi -= Cakildi;
            Susturulabilir.HerhangiSusturuldu -= Susturuldu;
            foreach (var b in bolgeler)
                if (b != null) b.Etkinlesti -= BolgeBasladi;
        }

        void Start()
        {
            baslangic = Time.time;
            if (hud != null) hud.GorevKaynagi = GorevYazisi;
        }

        void Update()
        {
            if (!Bitti)
                Sure = Time.time - baslangic;
            if (dusuyor)
                Dus(Time.deltaTime);
            if (Simdiki == Asama.UsseDon && UsBolgesinde())
                AsamaAyarla(Asama.Tamam, null);   // the HUD panel shows it
            if (yenidenZamani > 0f && Time.time >= yenidenZamani)
            {
                yenidenZamani = -1f;
                YenidenBaslat();
            }
            if (Bitti && yenidenTusu.WasPressedThisFrame())
                YenidenBaslat();
        }

        /// <summary>Plan decision 3: inside the base radius and low over the ground.</summary>
        public bool UsBolgesinde() =>
            ucus != null && UsUzakligi <= usYaricapi && ZeminOlcer.Irtifa(ucus.transform.position) <= usTavani;

        public void YenidenBaslat() => SahneYukle(SceneManager.GetActiveScene().name);
        public void AnaMenuyeDon() => SahneYukle(menuSahnesi);

        void AsamaAyarla(Asama a, string bildirim)
        {
            if (Simdiki == a || Bitti)
                return;
            Simdiki = a;
            if (hud != null && bildirim != null) hud.Bildir(bildirim, Basarisiz ? yenidenSuresi : 6f);
            if (Basarisiz)
                yenidenZamani = Time.time + yenidenSuresi;
            Debug.Log($"[KursunDaglariGorevi] {a} ({Sure:F0} s, susturulan {Susturulan}/{ToplamKaynak})");
            AsamaDegisti?.Invoke(a);
        }

        void TrenVardi() => AsamaAyarla(Asama.UsseDon, "Tren güvenli bölgede — ÜSSE DÖN");
        void TrenVuruldu() => AsamaAyarla(Asama.TrenDurdu, YaziTrenDurdu);
        void UcakVuruldu()
        {
            AsamaAyarla(Asama.Vuruldun, YaziVuruldun);
            if (ucus == null) return;
            dususHizi = ucus.HizVektoru;
            if (dususHizi.sqrMagnitude < 100f) dususHizi = ucus.transform.forward * 30f;
            ucus.enabled = false;
            if (algilayici != null) algilayici.enabled = false;
            dusuyor = true;
        }

        /// <summary>Ballistic fall with the speed it had, nose along the path, a slow roll; stops on the ground.</summary>
        void Dus(float dt)
        {
            Transform t = ucus.transform;
            dususHizi += Physics.gravity * dt;
            Vector3 p = t.position + dususHizi * dt;
            float zemin = ZeminOlcer.Yukseklik(p.x, p.z) + 1f;
            if (p.y <= zemin)
            {
                p.y = zemin;
                dusuyor = false;
            }
            Quaternion yon = Quaternion.LookRotation(dususHizi.normalized, Vector3.up);
            float yatis = t.eulerAngles.z + 70f * dt;
            t.SetPositionAndRotation(p, yon * Quaternion.Euler(0f, 0f, yatis));
        }
        void Cakildi(string neden) => AsamaAyarla(Asama.Cakildin, YaziCakildin);

        void Susturuldu(Susturulabilir s)
        {
            bool bizim = false;
            foreach (var b in bolgeler)
                if (b != null) foreach (var k in b.Kaynaklar()) if (k == s) bizim = true;
            if (!bizim) return;
            Susturulan++;
            switch (s.Tur)
            {
                case DusmanTuru.Tank: SusturulanTank++; break;
                case DusmanTuru.Ucaksavar: SusturulanUcaksavar++; break;
                default: SusturulanUcak++; break;
            }
            if (hud != null && !Bitti) hud.Bildir($"{TurAdi(s.Tur)} susturuldu ({Susturulan}/{ToplamKaynak})", 2.5f);
        }

        void BolgeBasladi(AtesBolgesi b)
        {
            if (hud != null && !Bitti) hud.Bildir(b.Duyuru(), 4f);
        }

        public static string TurAdi(DusmanTuru t) => t == DusmanTuru.Tank ? "Tank" : t == DusmanTuru.Ucaksavar ? "Uçaksavar" : "Düşman uçağı";
    }
}
