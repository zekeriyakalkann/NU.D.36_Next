using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5 · Kartal Geçidi): the HUD. Worksheet: "Karaya çıkan tank sayısı ekranda yazıyor" — the line
    /// "KARADA TANK x / 12" is always on screen. Also (as in Bölüm 4): the aircraft's health, the ships (come / 12, stopped),
    /// rounds and bombs, the task; the airspeed dial and throttle lever; warnings (a ship on the beach unloading, too high to
    /// bomb, no rounds); markers over the ships and the guard fighters with their distance, the selected target, the lead
    /// ring of the machine-gun assist, an edge arrow to the nearest ship; the end panel ("BÖLÜM TAMAM" / "KIYI DÜŞTÜ").
    /// </summary>
    public class Bolum5Hud : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [SerializeField] Saglik ucakSagligi;
        [SerializeField] BombaYuku yuk;
        [SerializeField] Makineli makineli;
        [SerializeField] CikarmaFilosu filo;
        [SerializeField] KorumaUcuslari koruma;
        [SerializeField] KartalGecidiGorevi gorevAkisi;
        [SerializeField] Bolum4Nisan nisan;
        [Header("Metinler")]
        [SerializeField] Text durumMetni;
        [SerializeField] Text gorevMetni;
        [SerializeField] Text bildirimMetni;
        [SerializeField] Text ucusMetni;
        [SerializeField] Text hizMetni;
        [SerializeField] Text gazMetni;
        [SerializeField] Text uyariMetni;
        [SerializeField] RectTransform isaretKoku;
        [SerializeField] Text oncuHalka;
        [SerializeField] GameObject sonPaneli;
        [SerializeField] Text sonMetni;
        [SerializeField] Font font;

        string gorev, bildirim;
        float bildirimBitis, cokYuksekZamani = -100f;
        readonly List<Text> isaretler = new List<Text>();
        Text gemiOku, hedefOku;

        public string Gorev { get => gorev ?? GorevKaynagi?.Invoke() ?? ""; set => gorev = value; }
        public System.Func<string> GorevKaynagi { get; set; }
        public string SonBildirim => bildirim;
        public string SonUyari { get; private set; } = "";
        public string SonDurum { get; private set; } = "";
        public int GorunenIsaret { get; private set; }
        public bool SonPaneliGorunuyor => sonPaneli != null && sonPaneli.activeSelf;
        public string SonPaneliYazisi { get; private set; } = "";
        public bool GemiOkuGorunuyor => gemiOku != null && gemiOku.gameObject.activeSelf;

        void Awake()
        {
            if (sonPaneli != null) sonPaneli.SetActive(false);
        }

        void OnEnable() { if (yuk != null) yuk.CokYuksekteReddedildi += CokYuksek; }
        void OnDisable() { if (yuk != null) yuk.CokYuksekteReddedildi -= CokYuksek; }
        void CokYuksek() => cokYuksekZamani = Time.time;

        public void Bildir(string metin, float sure = 4f)
        {
            bildirim = metin;
            bildirimBitis = Time.time + sure;
        }

        /// <summary>The tank count line (worksheet: always on screen).</summary>
        public string TankSatiri() =>
            $"KARADA TANK  {KaradakiTank} / {(gorevAkisi != null ? gorevAkisi.KiyiDusmeSiniri : KartalGecidiHaritasi.KiyiDusmeSiniri)}";

        int KaradakiTank => gorevAkisi != null ? gorevAkisi.KaradakiTank : filo != null ? filo.KaradakiTank : 0;

        public string DurumYazisi()
        {
            int kalan = makineli != null ? makineli.Kalan : 0;
            string mermi = makineli == null ? "—" : kalan > 0 ? kalan.ToString() : "YOK";
            string bomba = yuk != null ? yuk.Kalan.ToString() : "—";
            string gemi = filo == null ? "—" : $"{filo.Gelen} / {filo.ToplamGemi} geldi  ·  {filo.Durdurulan} durduruldu";
            return $"UÇAK  {Bolum4Hud.Yuzde(ucakSagligi)}\n" +
                   $"{TankSatiri()}\n" +
                   $"GEMİ  {gemi}\n" +
                   $"MERMİ {mermi}  ·  BOMBA {bomba}\n" +
                   $"GÖREV  {Gorev}";
        }

        public string UyariYazisi()
        {
            if (gorevAkisi != null && gorevAkisi.Bitti) return "";
            var u = new List<string>();
            if (filo != null)
            {
                int y = filo.YanasikSayisi;
                if (y > 0) u.Add(y == 1 ? "GEMİ KIYIDA — TANK İNİYOR" : $"{y} GEMİ KIYIDA — TANK İNİYOR");
            }
            if (yuk != null && yuk.Kalan > 0 && (Time.time - cokYuksekZamani < 2.5f || (nisan != null && nisan.Secili != null && nisan.Secili.Bomba && nisan.Kilitli && yuk.CokYuksek)))
                u.Add($"ÇOK YÜKSEK — {KartalGecidiHaritasi.BombaTavani:F0} m altında bomba bırakılır");
            if (makineli != null && makineli.Bos) u.Add("MERMİ YOK");
            return string.Join("   ·   ", u);
        }

        public string SonYazisi()
        {
            var g = gorevAkisi;
            bool tamam = g != null && g.Simdiki == KartalGecidiGorevi.Asama.Tamam;
            string baslik = tamam ? KartalGecidiGorevi.YaziTamam : KartalGecidiGorevi.YaziKiyiDustu;
            int dusen = koruma != null ? koruma.Dusurulen : 0;
            return $"{baslik}\n\n" +
                   $"Karaya çıkan tank:  {KaradakiTank} / {(g != null ? g.KiyiDusmeSiniri : 12)}\n" +
                   $"Durdurulan gemi:  {(filo != null ? filo.Durdurulan : 0)} / {(filo != null ? filo.ToplamGemi : 12)}\n" +
                   $"Düşürülen düşman uçağı:  {dusen}\n" +
                   $"Kalan cephane:  {(makineli != null ? makineli.Kalan.ToString() : "—")} mermi · {(yuk != null ? yuk.Kalan.ToString() : "—")} bomba\n" +
                   $"Süre:  {(g != null ? Bolum4Hud.SureYazisi(g.Sure) : "—")}\n\n" +
                   (tamam ? "R  yeniden başla   ·   Esc  menü" : "Bölüm yeniden başlıyor   ·   R  hemen");
        }

        void LateUpdate()
        {
            SonDurum = DurumYazisi();
            if (durumMetni != null) durumMetni.text = SonDurum;
            if (gorevMetni != null) gorevMetni.text = Gorev;
            if (bildirimMetni != null) bildirimMetni.text = Time.time < bildirimBitis ? bildirim : "";
            if (ucus != null)
            {
                if (hizMetni != null) hizMetni.text = GeceHud.HizYazisi(ucus.Hiz);
                if (gazMetni != null) gazMetni.text = GeceHud.GazYazisi(ucus.GazOrani);
                if (ucusMetni != null)
                    ucusMetni.text = $"YER {ZeminOlcer.Irtifa(ucus.transform.position):F0} m   İRTİFA {ucus.transform.position.y:F0} m   ·   {ucus.ModAdi} (K)";
            }
            SonUyari = UyariYazisi();
            if (uyariMetni != null) uyariMetni.text = SonUyari;

            bool son = gorevAkisi != null && (gorevAkisi.Simdiki == KartalGecidiGorevi.Asama.Tamam || gorevAkisi.Simdiki == KartalGecidiGorevi.Asama.KiyiDustu);
            if (sonPaneli != null && sonPaneli.activeSelf != son) sonPaneli.SetActive(son);
            if (son)
            {
                SonPaneliYazisi = SonYazisi();
                if (sonMetni != null) sonMetni.text = SonPaneliYazisi;
            }
            IsaretleriGuncelle(gorevAkisi != null && gorevAkisi.Bitti);
        }

        Text Isaret()
        {
            var go = new GameObject("Isaret", typeof(RectTransform), typeof(Text), typeof(Outline));
            var rt = (RectTransform)go.transform;
            rt.SetParent(isaretKoku, false);
            rt.sizeDelta = new Vector2(260f, 60f);
            var t = go.GetComponent<Text>();
            t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            return t;
        }

        bool Ekranda(Camera cam, Vector3 dunya, out Vector2 yerel)
        {
            yerel = Vector2.zero;
            Vector3 s = cam.WorldToScreenPoint(dunya);
            if (s.z <= 0f) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(isaretKoku, s, null, out yerel);
        }

        /// <summary>Marker over a world point, or at the screen edge with an arrow when it is off screen. True when on screen.</summary>
        bool KenarIsaret(Camera cam, Text t, Vector3 dunya, string metin, Color renk)
        {
            Vector3 v = cam.WorldToViewportPoint(dunya);
            bool arkada = v.z < 0f;
            Vector2 p = new Vector2(v.x - 0.5f, v.y - 0.5f);
            if (arkada) p = -p;
            bool ekranda = !arkada && Mathf.Abs(p.x) < 0.46f && Mathf.Abs(p.y) < 0.44f;
            if (ekranda) return true;
            float olcek = Mathf.Max(Mathf.Abs(p.x) / 0.44f, Mathf.Abs(p.y) / 0.40f, 1e-3f);
            p /= olcek;
            bool yatay = Mathf.Abs(p.x) / 0.44f > Mathf.Abs(p.y) / 0.40f;
            t.text = yatay ? (p.x > 0f ? metin + "  >>" : "<<  " + metin) : (p.y > 0f ? "^^\n" + metin : metin + "\nvv");
            Vector2 boy = isaretKoku.rect.size;
            t.rectTransform.anchoredPosition = new Vector2(p.x * boy.x, p.y * boy.y);
            t.color = renk;
            return false;
        }

        static string GemiDurumu(CikarmaGemisi g) =>
            g.Simdiki == CikarmaGemisi.Durum.Indiriyor ? "TANK İNİYOR" : g.Simdiki == CikarmaGemisi.Durum.Yanasti ? "KIYIDA" : "";

        void IsaretleriGuncelle(bool gizle)
        {
            Camera cam = Camera.main;
            int k = 0;
            CikarmaGemisi enYakin = null;
            float enYakinD = float.MaxValue;
            if (!gizle && isaretKoku != null && cam != null)
            {
                Vector3 benim = ucus != null ? ucus.transform.position : cam.transform.position;
                var secili = nisan != null ? nisan.Secili : null;
                foreach (Susturulabilir s in Susturulabilir.Serbestler)
                {
                    if (s == null || s.Sustu || !s.gameObject.activeInHierarchy) continue;
                    bool gemiMi = s.Tur == DusmanTuru.Gemi;
                    CikarmaGemisi g = gemiMi ? s.GetComponent<CikarmaGemisi>() : null;
                    if (gemiMi && (g == null || !g.Tehdit)) continue;
                    float d = Vector3.Distance(benim, s.transform.position);
                    if (gemiMi && d < enYakinD) { enYakinD = d; enYakin = g; }
                    if (!Ekranda(cam, s.transform.position + Vector3.up * (gemiMi ? 12f : 4f), out Vector2 p)) continue;
                    if (k == isaretler.Count) isaretler.Add(Isaret());
                    Text t = isaretler[k++];
                    t.gameObject.SetActive(true);
                    t.rectTransform.anchoredPosition = p;
                    bool sec = secili != null && secili.Kaynak == s;
                    string ad = gemiMi ? "GEMİ" : "UÇAK";
                    string durum = gemiMi ? GemiDurumu(g) : "";
                    string satir = $"{ad} {d / 1000f:F1} km{(durum.Length > 0 ? " · " + durum : "")}";
                    t.text = sec ? $">[ ]<\n{satir}" : $"[ ]\n{satir}";
                    bool iniyor = gemiMi && durum.Length > 0;
                    t.color = sec ? new Color(1f, 0.9f, 0.3f) : iniyor ? new Color(1f, 0.15f, 0.15f) : gemiMi ? new Color(1f, 0.5f, 0.3f) : new Color(1f, 0.35f, 0.9f);
                }
            }
            GorunenIsaret = k;
            for (int i = k; i < isaretler.Count; i++) isaretler[i].gameObject.SetActive(false);

            if (isaretKoku != null && cam != null)
            {
                if (gemiOku == null) gemiOku = Isaret();
                if (hedefOku == null) hedefOku = Isaret();
                // The nearest ship heading for the beach: an arrow at the screen edge when it is off screen.
                bool gemiGoster = !gizle && enYakin != null &&
                                  !KenarIsaret(cam, gemiOku, enYakin.transform.position + Vector3.up * 12f, $"GEMİ {enYakinD / 1000f:F1} km", new Color(1f, 0.5f, 0.3f));
                gemiOku.gameObject.SetActive(gemiGoster);
                var sec = nisan != null ? nisan.Secili : null;
                bool okGoster = !gizle && sec != null && sec.Canli && sec.Kaynak != (enYakin != null ? enYakin.GetComponent<Susturulabilir>() : null) &&
                                !KenarIsaret(cam, hedefOku, sec.Nokta + Vector3.up * 4f, $"HEDEF: {sec.Ad}", new Color(1f, 0.9f, 0.3f));
                hedefOku.gameObject.SetActive(okGoster);
            }

            if (oncuHalka != null)
            {
                bool goster = !gizle && cam != null && nisan != null && nisan.OncuNokta.HasValue && nisan.Yardim;
                Vector2 p = Vector2.zero;
                goster = goster && Ekranda(cam, nisan.OncuNokta.Value, out p);
                oncuHalka.gameObject.SetActive(goster);
                if (goster)
                {
                    oncuHalka.rectTransform.anchoredPosition = p;
                    oncuHalka.color = nisan.KonideHedef ? new Color(0.35f, 1f, 0.4f) : new Color(1f, 1f, 1f, 0.7f);
                }
            }
        }
    }
}
