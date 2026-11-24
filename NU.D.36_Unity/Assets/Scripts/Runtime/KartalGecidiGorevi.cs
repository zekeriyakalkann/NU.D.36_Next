using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using H = NU36.KartalGecidiHaritasi;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5 · Kartal Geçidi): the mission flow. Worksheet:
    /// <list type="bullet">
    /// <item>"Karaya 12 tank çıkınca bölüm başarısız oluyor" → <see cref="Asama.KiyiDustu"/>;</item>
    /// <item>"12 geminin hepsi geldikten sonra kıyı düşmediyse bölüm tamamlanıyor" → <see cref="Asama.Tamam"/> when the fleet
    /// is done (<see cref="CikarmaFilosu.Tamamlandi"/>) with fewer than 12 tanks ashore.</item>
    /// </list>
    /// As in Bölüm 4 (INFERRED): the aircraft's health running out → "VURULDUN", a crash → "ÇAKILDIN"; after a failure the
    /// level restarts after <see cref="yenidenSuresi"/> s (R at once). Short announcements: entering the pass, reaching the bay,
    /// a ship stopped, tanks ashore.
    /// </summary>
    public class KartalGecidiGorevi : MonoBehaviour
    {
        public enum Asama { Yaklasma, Savunma, Tamam, KiyiDustu, Vuruldun, Cakildin }

        public const string YaziKiyiDustu = "KIYI DÜŞTÜ", YaziVuruldun = "VURULDUN", YaziCakildin = "ÇAKILDIN", YaziTamam = "BÖLÜM TAMAM";

        [SerializeField] UcusKontrol ucus;
        [SerializeField] Saglik ucakSagligi;
        [SerializeField] InisAlgilayici algilayici;
        [SerializeField] Makineli makineli;
        [SerializeField] BombaYuku yuk;
        [SerializeField] CikarmaFilosu filo;
        [SerializeField] KorumaUcuslari koruma;
        [SerializeField] Bolum5Hud hud;
        [SerializeField, Min(1)] int kiyiDusmeSiniri = H.KiyiDusmeSiniri;
        [Header("Yeniden başlama")]
        [SerializeField, Min(0.5f)] float yenidenSuresi = 4f;
        [SerializeField] string menuSahnesi = "AnaMenu";
        [SerializeField] InputAction yenidenTusu = new InputAction("YenidenBasla", InputActionType.Button, "<Keyboard>/r");

        float baslangic, yenidenZamani = -1f;
        int sonTank = -1;
        Vector3 dususHizi;
        bool dusuyor, gecitDuyuruldu;

        /// <summary>Loads a scene by name (tests replace it).</summary>
        public Action<string> SahneYukle = ad => SceneManager.LoadScene(ad);
        public event Action<Asama> AsamaDegisti;

        public Asama Simdiki { get; private set; } = Asama.Yaklasma;
        public bool Bitti => Simdiki != Asama.Yaklasma && Simdiki != Asama.Savunma;
        public bool Basarisiz => Simdiki == Asama.KiyiDustu || Simdiki == Asama.Vuruldun || Simdiki == Asama.Cakildin;
        public bool UcakDusuyor => dusuyor;
        public float Sure { get; private set; }
        public int KiyiDusmeSiniri => kiyiDusmeSiniri;
        public CikarmaFilosu Filo => filo;
        /// <summary>Tanks ashore; frozen when the level ends (tanks still on the ramps do not raise it past 12).</summary>
        public int KaradakiTank => sonTank >= 0 ? sonTank : filo != null ? filo.KaradakiTank : 0;
        public KorumaUcuslari Koruma => koruma;
        /// <summary>Horizontal distance from the aircraft to the bay centre.</summary>
        public float KoyUzakligi => ucus == null ? 0f : Vector2.Distance(new Vector2(ucus.transform.position.x, ucus.transform.position.z), new Vector2(H.KoyMerkezi.x, H.KoyMerkezi.z));

        /// <summary>The task line of the HUD.</summary>
        public string GorevYazisi()
        {
            switch (Simdiki)
            {
                case Asama.Yaklasma: return $"Geçitten koya uç ({KoyUzakligi / 1000f:F1} km)";
                case Asama.Savunma:
                    int n = filo != null ? filo.TehditSayisi : 0;
                    return n > 0 ? $"Çıkarmayı durdur — koyda {n} gemi" : "Çıkarmayı durdur — yeni gemi geliyor";
                case Asama.Tamam: return "Bölüm tamam";
                case Asama.KiyiDustu: return "Kıyı düştü";
                case Asama.Vuruldun: return "Vuruldun";
                default: return "Çakıldın";
            }
        }

        void Awake()
        {
            if (algilayici == null && ucus != null) algilayici = ucus.GetComponent<InisAlgilayici>();
            if (ucakSagligi == null && ucus != null) ucakSagligi = ucus.GetComponent<Saglik>();
            if (makineli == null && ucus != null) makineli = ucus.GetComponent<Makineli>();
            if (yuk == null && ucus != null) yuk = ucus.GetComponent<BombaYuku>();
        }

        void OnEnable()
        {
            yenidenTusu.Enable();
            if (ucakSagligi != null) ucakSagligi.Oldu += UcakVuruldu;
            if (algilayici != null) algilayici.Cakildi += Cakildi;
            if (filo != null)
            {
                filo.TankKarada += TankKarada;
                filo.GemiDurdu += GemiDurdu;
            }
            if (koruma != null) koruma.Dusuruldu += UcakDusuruldu;
        }

        void OnDisable()
        {
            yenidenTusu.Disable();
            if (ucakSagligi != null) ucakSagligi.Oldu -= UcakVuruldu;
            if (algilayici != null) algilayici.Cakildi -= Cakildi;
            if (filo != null)
            {
                filo.TankKarada -= TankKarada;
                filo.GemiDurdu -= GemiDurdu;
            }
            if (koruma != null) koruma.Dusuruldu -= UcakDusuruldu;
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
            if (!Bitti && ucus != null)
                Konumlar(ucus.transform.position);
            if (!Bitti && filo != null && filo.Tamamlandi && filo.KaradakiTank < kiyiDusmeSiniri)
                AsamaAyarla(Asama.Tamam, null);   // the HUD panel shows it
            if (yenidenZamani > 0f && Time.time >= yenidenZamani)
            {
                yenidenZamani = -1f;
                YenidenBaslat();
            }
            if (Bitti && yenidenTusu.WasPressedThisFrame())
                YenidenBaslat();
        }

        /// <summary>Announcements on the way: into the pass, out over the bay (then the defence starts).</summary>
        void Konumlar(Vector3 p)
        {
            if (!gecitDuyuruldu && p.x > H.SirtBati - 150f)
            {
                gecitDuyuruldu = true;
                if (hud != null) hud.Bildir("KARTAL GEÇİDİ", 3.5f);
            }
            if (Simdiki == Asama.Yaklasma && p.x > H.SirtDogu + 150f)
                AsamaAyarla(Asama.Savunma, "KARTAL KOYU — ÇIKARMAYI DURDUR");
        }

        public void YenidenBaslat() => SahneYukle(SceneManager.GetActiveScene().name);
        public void AnaMenuyeDon() => SahneYukle(menuSahnesi);

        void AsamaAyarla(Asama a, string bildirim)
        {
            if (Simdiki == a || Bitti)
                return;
            Simdiki = a;
            if (Bitti) sonTank = filo != null ? Mathf.Min(filo.KaradakiTank, kiyiDusmeSiniri) : 0;
            if (hud != null && bildirim != null) hud.Bildir(bildirim, Basarisiz ? yenidenSuresi : 4f);
            if (Basarisiz)
                yenidenZamani = Time.time + yenidenSuresi;
            Debug.Log($"[KartalGecidiGorevi] {a} ({Sure:F0} s; gemi {(filo != null ? filo.Gelen : 0)}/{(filo != null ? filo.ToplamGemi : 0)}, " +
                      $"durdurulan {(filo != null ? filo.Durdurulan : 0)}, karada tank {(filo != null ? filo.KaradakiTank : 0)}/{kiyiDusmeSiniri})");
            AsamaDegisti?.Invoke(a);
        }

        void TankKarada(CikarmaTanki t, int toplam)
        {
            if (Bitti) return;
            if (toplam >= kiyiDusmeSiniri)
            {
                AsamaAyarla(Asama.KiyiDustu, null);   // the HUD panel shows it
                return;
            }
            if (hud != null) hud.Bildir($"TANK KARAYA ÇIKTI — {toplam} / {kiyiDusmeSiniri}", 2f);
        }

        void GemiDurdu(CikarmaGemisi g)
        {
            if (Bitti || hud == null) return;
            hud.Bildir(g.GemideKalan > 0 ? $"GEMİ DURDURULDU — {g.GemideKalan} tank inemedi" : "GEMİ DURDURULDU", 2.5f);
        }

        void UcakDusuruldu(DusmanUcagi u)
        {
            if (!Bitti && hud != null) hud.Bildir("DÜŞMAN UÇAĞI VURULDU", 2f);
        }

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

        /// <summary>Ballistic fall with the speed it had, nose along the path, a slow roll; stops on the ground or the sea.</summary>
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
    }
}
