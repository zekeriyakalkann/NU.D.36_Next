using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Main menu (scene AnaMenu). Built in code at start from the Lab 4 Blender art (menu-arkaplan.png background,
    /// dugme-seti.png buttons) in the background's dark left third. Panels:
    ///   Ana        — GÖREVE BAŞLA (the latest mission), BÖLÜM SEÇ, SEÇENEKLER, KONTROLLER, ÇIKIŞ;
    ///   Bolumler   — every lab scene of the project (a scene that is not in the build is shown disabled);
    ///   Secenekler — graphics quality, full screen, volume (<see cref="OyunAyarlari"/>);
    ///   Kontroller — the key table.
    /// Esc (or gamepad B / east) goes back from a sub-panel. The menu also creates the persistent pause menu, so the older
    /// lab scenes started from here can be paused and left with Esc as well.
    /// </summary>
    public class AnaMenu : MonoBehaviour
    {
        [Serializable]
        public struct Bolum
        {
            public string ad;
            public string sahne;
            public Bolum(string ad, string sahne) { this.ad = ad; this.sahne = sahne; }
        }

        public enum Panel { Ana, Bolumler, Secenekler, Kontroller }

        public static readonly string[] KontrolSatirlari =
        {
            "W / S\tBurun aşağı / yukarı",
            "A / D\tYatış (sola / sağa)",
            "Q / E\tDümen (yerde teker yönü)",
            "↑ / ↓\tGaz aç / kıs (rölantide yerde fren)",
            "F (B)\tFren: havada hava freni, yerde teker",
            "K\tKolay uçuş aç / kapa",
            "Ctrl / Sol tık\tMakineli (Bölüm 4)",
            "Space\tBomba (kilitliyken güdümlü)",
            "T / O\tHedef değiştir / atış yardımı",
            "M\tGPS haritası",
            "L\tİniş feneri aç / kapa",
            "Esc\tDuraklat / menü",
            "R\tGörev bitince yeniden başla",
        };

        [SerializeField] Texture arkaplan;
        [SerializeField] Texture dugmeSeti;
        [SerializeField] string baslik = "NU.D.36";
        [SerializeField] string altBaslik = "Bölüm 5 · Kartal Geçidi";
        [SerializeField] string gorevSahnesi = "Bolum5_KartalGecidi";
        [SerializeField] Bolum[] bolumler =
        {
            new Bolum("Hafta 2 · Rota 02", "Rota02"),
            new Bolum("Hafta 3 · Rota 03", "Rota03"),
            new Bolum("Hafta 4 · Keşif (HUD)", "Rota04Hud"),
            new Bolum("Hafta 5 · Sınır Hattı", "Rota05"),
            new Bolum("Hafta 6 · Gri Kutu", "Bolum3_GriKutu"),
            new Bolum("Hafta 7 · Gece Görevi", "Rota07"),
            new Bolum("Hafta 8 · Kurşun Dağları", "Bolum4_KursunDaglari"),
            new Bolum("Hafta 9 · Kartal Geçidi", "Bolum5_KartalGecidi"),
        };
        [SerializeField] InputAction geriTusu = new InputAction("Geri", InputActionType.Button, "<Keyboard>/escape");

        /// <summary>Loads a scene by name; replaceable by tests.</summary>
        public Action<string> SahneYukle = ad => SceneManager.LoadScene(ad);
        /// <summary>Quits; replaceable by tests.</summary>
        public Action Cikis = MenuArayuzu.UygulamadanCik;

        readonly Dictionary<Panel, RectTransform> paneller = new Dictionary<Panel, RectTransform>();
        readonly Dictionary<Panel, Selectable> ilkSecim = new Dictionary<Panel, Selectable>();
        readonly Dictionary<string, MenuDugmesi> dugmeler = new Dictionary<string, MenuDugmesi>();
        MenuDugmesi kaliteDugmesi, ekranDugmesi, sesDugmesi;

        public Panel Acik { get; private set; }
        public string GorevSahnesi => gorevSahnesi;
        public IReadOnlyList<Bolum> Bolumler => bolumler;
        public Texture DugmeSeti => dugmeSeti;
        public Texture Arkaplan => arkaplan;

        /// <summary>Button by its object name (e.g. "Basla", "Bolum_Rota07", "Geri_Secenekler"); for tests.</summary>
        public MenuDugmesi Dugme(string ad) => dugmeler.TryGetValue(ad, out MenuDugmesi d) ? d : null;

        void Awake()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            OyunAyarlari.Uygula();
            Kur();
            PanelAc(Panel.Ana);
        }

        void Start()
        {
            if (DuraklatmaMenusu.Kalici == null)
                DuraklatmaMenusu.KaliciOlustur(dugmeSeti, SceneManager.GetActiveScene().name);
        }

        void OnEnable() => geriTusu.Enable();
        void OnDisable() => geriTusu.Disable();

        void Update()
        {
            if (geriTusu.WasPressedThisFrame() && Acik != Panel.Ana)
                PanelAc(Panel.Ana);
        }

        // ------------------------------------------------------------------ actions

        public void GoreveBasla() => SahneYukle(gorevSahnesi);

        public void BolumAc(string sahne)
        {
            if (MenuArayuzu.SahneVar(sahne))
                SahneYukle(sahne);
        }

        public void PanelAc(Panel p)
        {
            Acik = p;
            foreach (KeyValuePair<Panel, RectTransform> kv in paneller)
                kv.Value.gameObject.SetActive(kv.Key == p);
            if (p == Panel.Secenekler)
                SecenekYazilariniYenile();
            if (ilkSecim.TryGetValue(p, out Selectable s))
                MenuArayuzu.Sec(s);
        }

        public void KaliteDegistir()
        {
            OyunAyarlari.Kalite = OyunAyarlari.SonrakiKalite(OyunAyarlari.Kalite, QualitySettings.names.Length);
            OyunAyarlari.Kaydet();
            SecenekYazilariniYenile();
        }

        public void EkranDegistir()
        {
            OyunAyarlari.TamEkran = !OyunAyarlari.TamEkran;
            OyunAyarlari.Kaydet();
            SecenekYazilariniYenile();
        }

        public void SesDegistir()
        {
            OyunAyarlari.Ses = OyunAyarlari.SonrakiSes(OyunAyarlari.Ses);
            OyunAyarlari.Kaydet();
            SecenekYazilariniYenile();
        }

        void SecenekYazilariniYenile()
        {
            if (kaliteDugmesi != null) kaliteDugmesi.Yazi = OyunAyarlari.KaliteYazisi;
            if (ekranDugmesi != null) ekranDugmesi.Yazi = OyunAyarlari.EkranYazisi;
            if (sesDugmesi != null) sesDugmesi.Yazi = OyunAyarlari.SesYazisi;
        }

        // ------------------------------------------------------------------ layout

        const float SutunX = 330f;   // centre of the button column, from the left edge (the dark third of the background)

        void Kur()
        {
            MenuArayuzu.OlaySisteminiHazirla(transform);
            Canvas tuval = MenuArayuzu.Tuval(transform, "Menu Tuvali", 50);
            var kok = (RectTransform)tuval.transform;

            // Background: covers the screen at any aspect (16:9 image, envelope).
            RectTransform zemin = MenuArayuzu.Rt("Arkaplan", kok);
            MenuArayuzu.Doldur(zemin);
            MenuArayuzu.Renk(zemin, new Color(0.05f, 0.045f, 0.04f, 1f));
            RectTransform resim = MenuArayuzu.Rt("Resim", zemin);
            var ri = resim.gameObject.AddComponent<RawImage>();
            ri.texture = arkaplan;
            ri.raycastTarget = false;
            ri.enabled = arkaplan != null;
            var oran = resim.gameObject.AddComponent<AspectRatioFitter>();
            oran.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            oran.aspectRatio = 16f / 9f;

            RectTransform ana = YeniPanel(kok, Panel.Ana, baslik, 150f, 96);
            Text alt = MenuArayuzu.Metin(MenuArayuzu.Yerlestir(MenuArayuzu.Rt("AltBaslik", ana), 0f, SutunX, 236f, new Vector2(620f, 50f)),
                altBaslik, 34, TextAnchor.MiddleCenter, MenuArayuzu.Krem, false);
            alt.fontStyle = FontStyle.Italic;
            var boyut = new Vector2(372f, 116f);
            Ekle(ana, Panel.Ana, "Basla", "GÖREVE BAŞLA", 350f, boyut, GoreveBasla);
            Ekle(ana, Panel.Ana, "Bolumler", "BÖLÜM SEÇ", 476f, boyut, () => PanelAc(Panel.Bolumler));
            Ekle(ana, Panel.Ana, "Secenekler", "SEÇENEKLER", 602f, boyut, () => PanelAc(Panel.Secenekler));
            Ekle(ana, Panel.Ana, "Kontroller", "KONTROLLER", 728f, boyut, () => PanelAc(Panel.Kontroller));
            Ekle(ana, Panel.Ana, "Cikis", "ÇIKIŞ", 854f, boyut, () => Cikis());
            MenuDugmesi basla = Dugme("Basla");
            if (basla != null) basla.interactable = MenuArayuzu.SahneVar(gorevSahnesi);
            MenuArayuzu.Metin(MenuArayuzu.Yerlestir(MenuArayuzu.Rt("Ipucu", ana), 0f, SutunX, 1030f, new Vector2(620f, 30f)),
                "Fare ya da ↑ / ↓ + Enter", 22, TextAnchor.MiddleCenter, MenuArayuzu.Soluk, false);

            // Chapters: all lab scenes; the list is centred in the column, GERİ at the bottom.
            RectTransform bp = YeniPanel(kok, Panel.Bolumler, "BÖLÜM SEÇ", 130f, 64);
            var bb = new Vector2(420f, 100f);
            for (int i = 0; i < bolumler.Length; i++)
            {
                string sahne = bolumler[i].sahne;
                MenuDugmesi d = Ekle(bp, Panel.Bolumler, "Bolum_" + sahne, MenuArayuzu.Buyuk(bolumler[i].ad), 240f + i * 108f, bb,
                    () => BolumAc(sahne), 30);
                d.interactable = MenuArayuzu.SahneVar(sahne);
            }
            Ekle(bp, Panel.Bolumler, "Geri_Bolumler", "GERİ", 240f + bolumler.Length * 108f + 24f, boyut, () => PanelAc(Panel.Ana));

            // Options.
            RectTransform sp = YeniPanel(kok, Panel.Secenekler, "SEÇENEKLER", 150f, 64);
            var sb = new Vector2(440f, 116f);
            kaliteDugmesi = Ekle(sp, Panel.Secenekler, "Kalite", OyunAyarlari.KaliteYazisi, 330f, sb, KaliteDegistir, 32);
            ekranDugmesi = Ekle(sp, Panel.Secenekler, "Ekran", OyunAyarlari.EkranYazisi, 456f, sb, EkranDegistir, 32);
            sesDugmesi = Ekle(sp, Panel.Secenekler, "Ses", OyunAyarlari.SesYazisi, 582f, sb, SesDegistir, 32);
            Ekle(sp, Panel.Secenekler, "Geri_Secenekler", "GERİ", 760f, boyut, () => PanelAc(Panel.Ana));
            MenuArayuzu.Metin(MenuArayuzu.Yerlestir(MenuArayuzu.Rt("Not", sp), 0f, SutunX, 870f, new Vector2(620f, 60f)),
                "Ayarlar kaydedilir ve oyun açılınca uygulanır.", 22, TextAnchor.MiddleCenter, MenuArayuzu.Soluk, false);

            // Controls: key table on a dark plate.
            RectTransform kp = YeniPanel(kok, Panel.Kontroller, "KONTROLLER", 150f, 64);
            RectTransform tablo = MenuArayuzu.Yerlestir(MenuArayuzu.Rt("Tablo", kp), 0f, 470f, 520f, new Vector2(780f, 520f));
            MenuArayuzu.Renk(tablo, new Color(0.03f, 0.025f, 0.02f, 0.72f));
            for (int i = 0; i < KontrolSatirlari.Length; i++)
            {
                string[] p = KontrolSatirlari[i].Split('\t');
                float adim = Mathf.Min(58f, 470f / KontrolSatirlari.Length);   // 12 rows fit the plate
                float y = 34f + i * adim;
                RectTransform tus = MenuArayuzu.Rt("Tus" + i, tablo);
                tus.anchorMin = tus.anchorMax = tus.pivot = new Vector2(0f, 1f);
                tus.sizeDelta = new Vector2(190f, 50f);
                tus.anchoredPosition = new Vector2(36f, -y + 22f);
                MenuArayuzu.Metin(tus, p[0], adim < 50f ? 27 : 32, TextAnchor.MiddleLeft, new Color(1f, 0.8f, 0.42f, 1f));
                RectTransform ne = MenuArayuzu.Rt("Is" + i, tablo);
                ne.anchorMin = ne.anchorMax = ne.pivot = new Vector2(0f, 1f);
                ne.sizeDelta = new Vector2(540f, 50f);
                ne.anchoredPosition = new Vector2(230f, -y + 22f);
                MenuArayuzu.Metin(ne, p[1], adim < 50f ? 25 : 30, TextAnchor.MiddleLeft, MenuArayuzu.Krem, false);
            }
            Ekle(kp, Panel.Kontroller, "Geri_Kontroller", "GERİ", 870f, boyut, () => PanelAc(Panel.Ana));
        }

        RectTransform YeniPanel(RectTransform kok, Panel p, string ust, float baslikY, int baslikBoyutu)
        {
            RectTransform r = MenuArayuzu.Rt("Panel_" + p, kok);
            MenuArayuzu.Doldur(r);
            Text t = MenuArayuzu.Metin(MenuArayuzu.Yerlestir(MenuArayuzu.Rt("Baslik", r), 0f, SutunX, baslikY, new Vector2(620f, 120f)),
                ust, baslikBoyutu, TextAnchor.MiddleCenter, MenuArayuzu.Krem);
            t.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            paneller[p] = r;
            return r;
        }

        MenuDugmesi Ekle(RectTransform panel, Panel p, string ad, string yazi, float yUst, Vector2 boyut, UnityEngine.Events.UnityAction tik, int yaziBoyutu = 36)
        {
            MenuDugmesi d = MenuArayuzu.Dugme(panel, ad, yazi, dugmeSeti, 0f, SutunX, yUst, boyut, tik, yaziBoyutu);
            dugmeler[ad] = d;
            if (!ilkSecim.ContainsKey(p))
                ilkSecim[p] = d;
            return d;
        }
    }
}
