using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4): the HUD. Worksheet step 08: five items always on screen — the aircraft's health, the train's
    /// health, rounds and bombs, the time left to the safe zone and the current task. Also the Lab 7 airspeed dial and
    /// throttle lever (texts under them), a flight line (height above ground) and short announcements.
    /// Phase 3 (plan §5.7): warnings (AA fire → climb, too high to bomb, the train under fire, no rounds), markers over the
    /// live enemies with their kind and distance, the lead ring of the machine-gun assist, and the "Görev tamam" panel
    /// (train health left, fire sources silenced, rounds and bombs left, time).
    /// </summary>
    public class Bolum4Hud : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [SerializeField] Saglik ucakSagligi;
        [SerializeField] Tren tren;
        [SerializeField] BombaYuku yuk;
        [SerializeField] Makineli makineli;
        [SerializeField] KursunDaglariGorevi gorevAkisi;
        [SerializeField] Bolum4Nisan nisan;
        [Header("Metinler")]
        [SerializeField] Text durumMetni;
        [SerializeField] Text gorevMetni;
        [SerializeField] Text bildirimMetni;
        [SerializeField] Text ucusMetni;
        [SerializeField] Text hizMetni;
        [SerializeField] Text gazMetni;
        [SerializeField] GameObject gostergeler;
        [Header("Faz 3")]
        [SerializeField] Text uyariMetni;
        [SerializeField] RectTransform isaretKoku;
        [SerializeField] Text oncuHalka;
        [SerializeField] GameObject tamamPaneli;
        [SerializeField] Text tamamMetni;
        [SerializeField] Font font;

        string gorev;
        string bildirim;
        float bildirimBitis;
        float trenVurulmaZamani = -100f, cokYuksekZamani = -100f;
        readonly List<Text> isaretler = new List<Text>();
        Text trenIsareti, hedefOku;

        /// <summary>The train marker is on screen or at its edge (always shown during the escort).</summary>
        public bool TrenIsaretiGorunuyor => trenIsareti != null && trenIsareti.gameObject.activeSelf;
        public string TrenIsaretiYazisi => trenIsareti != null ? trenIsareti.text : "";

        /// <summary>Text of the current task: set by hand, else from the mission (<see cref="GorevKaynagi"/>), else from the train.</summary>
        public string Gorev { get => gorev ?? GorevKaynagi?.Invoke() ?? VarsayilanGorev(); set => gorev = value; }

        /// <summary>Source of the task line (the mission sets it at start).</summary>
        public System.Func<string> GorevKaynagi { get; set; }

        /// <summary>Source of the rounds left; defaults to the machine gun, null shows "—".</summary>
        public System.Func<int> MermiKaynagi { get; set; }

        public Text DurumMetni => durumMetni;
        public string SonBildirim => bildirim;
        public string SonUyari { get; private set; } = "";
        public int GorunenIsaret { get; private set; }
        public bool TamamGorunuyor => tamamPaneli != null && tamamPaneli.activeSelf;
        public string TamamYazisiSon { get; private set; } = "";

        void Awake()
        {
            if (MermiKaynagi == null && makineli != null)
                MermiKaynagi = () => makineli.Kalan;
            if (tamamPaneli != null) tamamPaneli.SetActive(false);
        }

        void OnEnable()
        {
            if (tren != null && tren.Saglik != null) tren.Saglik.CanDegisti += TrenVuruldu;
            if (yuk != null) yuk.CokYuksekteReddedildi += CokYuksek;
        }

        void OnDisable()
        {
            if (tren != null && tren.Saglik != null) tren.Saglik.CanDegisti -= TrenVuruldu;
            if (yuk != null) yuk.CokYuksekteReddedildi -= CokYuksek;
        }

        void TrenVuruldu(int _) => trenVurulmaZamani = Time.time;
        void CokYuksek() => cokYuksekZamani = Time.time;

        string VarsayilanGorev()
        {
            if (tren == null) return "";
            if (tren.Vuruldu) return "Tren durdu";
            return tren.Vardi ? "Üsse dön" : "Treni güvenli bölgeye ulaştır";
        }

        public void Bildir(string metin, float sure = 4f)
        {
            bildirim = metin;
            bildirimBitis = Time.time + sure;
        }

        public static string Yuzde(Saglik s) => s == null ? "—" : $"%{Mathf.RoundToInt(100f * s.Can / Mathf.Max(1, s.BaslangicCani))}";
        public static string SureYazisi(float sn)
        {
            if (float.IsInfinity(sn) || float.IsNaN(sn)) return "—";
            int t = Mathf.CeilToInt(Mathf.Max(0f, sn));
            return $"{t / 60}:{t % 60:00}";
        }

        /// <summary>The five lines of worksheet step 08.</summary>
        public string DurumYazisi()
        {
            string mermi = MermiKaynagi == null ? "—" : MermiKaynagi() > 0 ? MermiKaynagi().ToString() : "YOK";
            string bomba = yuk != null ? yuk.Kalan.ToString() : "—";
            string sure = tren == null ? "—" : tren.Vardi ? "VARDI" : SureYazisi(tren.KalanSure);
            return $"UÇAK  {Yuzde(ucakSagligi)}\n" +
                   $"TREN  {(tren != null ? Yuzde(tren.Saglik) : "—")}\n" +
                   $"MERMİ {mermi}  ·  BOMBA {bomba}\n" +
                   $"GÜVENLİ BÖLGE  {sure}\n" +
                   $"GÖREV  {Gorev}";
        }

        /// <summary>Warnings of plan §5.7, most urgent first ("" when none).</summary>
        public string UyariYazisi()
        {
            var u = new List<string>();
            if (gorevAkisi != null && gorevAkisi.Bitti) return "";
            foreach (AtesBolgesi b in AtesBolgesi.Hepsi)
            {
                if (b.Simdiki != AtesBolgesi.Durum.Etkin) continue;
                bool ates = false;
                foreach (GameObject g in b.YerBirimleri)
                    if (g != null && g.activeInHierarchy && g.TryGetComponent(out UcaksavarMevzii a) && a.AtesEdiyor) ates = true;
                if (ates) { u.Add($"UÇAKSAVAR ATEŞİ — {KursunDaglariHaritasi.UcaksavarTavani:F0} m üstüne yüksel"); break; }
            }
            if (tren != null && !tren.Vuruldu && Time.time - trenVurulmaZamani < 2f) u.Add("TREN VURULUYOR");
            if (yuk != null && yuk.Kalan > 0 && (Time.time - cokYuksekZamani < 2.5f || (nisan != null && nisan.Secili != null && nisan.Secili.Bomba && nisan.Kilitli && yuk.CokYuksek)))
                u.Add($"ÇOK YÜKSEK — {KursunDaglariHaritasi.BombaTavani:F0} m altında bomba bırakılır");
            if (makineli != null && makineli.Bos) u.Add("MERMİ YOK");
            return string.Join("   ·   ", u);
        }

        /// <summary>The "Görev tamam" panel text (worksheet 08: train health left, fire sources silenced, rounds left).</summary>
        public string TamamYazisi()
        {
            var g = gorevAkisi;
            string sayi = g == null ? "—" :
                $"{g.Susturulan} / {g.ToplamKaynak}   (tank {g.SusturulanTank}/4 · uçaksavar {g.SusturulanUcaksavar}/4 · uçak {g.SusturulanUcak}/4)";
            return "GÖREV TAMAM\n\n" +
                   $"Trenin kalan canı:  {(tren != null ? Yuzde(tren.Saglik) : "—")}\n" +
                   $"Susturulan ateş kaynağı:  {sayi}\n" +
                   $"Kalan cephane:  {(makineli != null ? makineli.Kalan.ToString() : "—")} mermi · {(yuk != null ? yuk.Kalan.ToString() : "—")} bomba\n" +
                   $"Süre:  {(g != null ? SureYazisi(g.Sure) : "—")}\n\n" +
                   "R  yeniden başla   ·   Esc  menü";
        }

        void LateUpdate()
        {
            if (durumMetni != null) durumMetni.text = DurumYazisi();
            if (gorevMetni != null) gorevMetni.text = Gorev;
            if (bildirimMetni != null)
                bildirimMetni.text = Time.time < bildirimBitis ? bildirim : "";
            if (ucus != null)
            {
                if (hizMetni != null) hizMetni.text = GeceHud.HizYazisi(ucus.Hiz);
                if (gazMetni != null) gazMetni.text = GeceHud.GazYazisi(ucus.GazOrani);
                if (ucusMetni != null)
                    ucusMetni.text = $"YER {ZeminOlcer.Irtifa(ucus.transform.position):F0} m   İRTİFA {ucus.transform.position.y:F0} m   ·   {ucus.ModAdi} (K)";
            }
            SonUyari = UyariYazisi();
            if (uyariMetni != null) uyariMetni.text = SonUyari;

            bool tamam = gorevAkisi != null && gorevAkisi.Simdiki == KursunDaglariGorevi.Asama.Tamam;
            if (tamamPaneli != null && tamamPaneli.activeSelf != tamam) tamamPaneli.SetActive(tamam);
            if (tamam)
            {
                TamamYazisiSon = TamamYazisi();
                if (tamamMetni != null) tamamMetni.text = TamamYazisiSon;
            }
            IsaretleriGuncelle(tamam || (gorevAkisi != null && gorevAkisi.Bitti));
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

        /// <summary>Screen position (in the marker root's space) of a world point; false when behind the camera.</summary>
        bool Ekranda(Camera cam, Vector3 dunya, out Vector2 yerel)
        {
            yerel = Vector2.zero;
            Vector3 s = cam.WorldToScreenPoint(dunya);
            if (s.z <= 0f) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(isaretKoku, s, null, out yerel);
        }

        /// <summary>
        /// Puts a marker over a world point, or — when the point is off screen or behind — at the screen edge in its direction
        /// with an arrow. Returns true when the point is on screen.
        /// </summary>
        bool KenarIsaret(Camera cam, Text t, Vector3 dunya, string metin, Color renk, bool ekrandaGoster)
        {
            Vector3 v = cam.WorldToViewportPoint(dunya);
            bool arkada = v.z < 0f;
            Vector2 p = new Vector2(v.x - 0.5f, v.y - 0.5f);
            if (arkada) p = -p;
            bool ekranda = !arkada && Mathf.Abs(p.x) < 0.46f && Mathf.Abs(p.y) < 0.44f;
            string ok = "";
            if (!ekranda)
            {
                float olcek = Mathf.Max(Mathf.Abs(p.x) / 0.44f, Mathf.Abs(p.y) / 0.40f, 1e-3f);
                p /= olcek;
                ok = Mathf.Abs(p.x) / 0.44f > Mathf.Abs(p.y) / 0.40f ? (p.x > 0f ? "  >>" : "<<  ") : (p.y > 0f ? "^^\n" : "\nvv");
            }
            Vector2 boy = isaretKoku.rect.size;
            t.rectTransform.anchoredPosition = new Vector2(p.x * boy.x, p.y * boy.y);
            t.color = renk;
            if (!ekranda)
                t.text = ok.StartsWith("<") ? ok + metin : ok.EndsWith(">") ? metin + ok : ok.StartsWith("^") ? ok + metin : metin + ok;
            else
                t.text = ekrandaGoster ? $"v\n{metin}" : metin;
            return ekranda;
        }

        void IsaretleriGuncelle(bool gizle)
        {
            Camera cam = Camera.main;
            int k = 0;
            if (!gizle && isaretKoku != null && cam != null)
            {
                Vector3 benim = ucus != null ? ucus.transform.position : cam.transform.position;
                var secili = nisan != null ? nisan.Secili : null;
                foreach (AtesBolgesi b in AtesBolgesi.Hepsi)
                {
                    if (b.Simdiki != AtesBolgesi.Durum.Etkin) continue;
                    foreach (Susturulabilir s in b.Kaynaklar())
                    {
                        if (s.Sustu || !s.gameObject.activeInHierarchy) continue;
                        if (!Ekranda(cam, s.transform.position + Vector3.up * 4f, out Vector2 p)) continue;
                        if (k == isaretler.Count) isaretler.Add(Isaret());
                        Text t = isaretler[k++];
                        t.gameObject.SetActive(true);
                        t.rectTransform.anchoredPosition = p;
                        bool sec = secili != null && (secili.Kaynak == s || secili.Cift.Contains(s));
                        string ad = s.Tur == DusmanTuru.Tank ? "TANK" : s.Tur == DusmanTuru.Ucaksavar ? "UÇAKSAVAR" : "UÇAK";
                        float d = Vector3.Distance(benim, s.transform.position);
                        t.text = sec ? $">[ ]<\n{ad} {d / 1000f:F1} km" : $"[ ]\n{ad} {d / 1000f:F1} km";
                        t.color = sec ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.35f, 0.25f);
                    }
                }
            }
            GorunenIsaret = k;
            for (int i = k; i < isaretler.Count; i++) isaretler[i].gameObject.SetActive(false);

            // Train: always marked (on screen, or an arrow at the edge pointing to it). Selected target off screen: arrow.
            if (isaretKoku != null && cam != null)
            {
                if (trenIsareti == null) { trenIsareti = Isaret(); trenIsareti.fontSize = 20; }
                if (hedefOku == null) hedefOku = Isaret();
                bool trenGoster = !gizle && tren != null && tren.Araclar.Length > 0 && !tren.Vardi;
                trenIsareti.gameObject.SetActive(trenGoster);
                if (trenGoster)
                {
                    Vector3 lok = tren.Araclar[0].transform.position + Vector3.up * 8f;
                    float d = ucus != null ? Vector3.Distance(ucus.transform.position, lok) : 0f;
                    KenarIsaret(cam, trenIsareti, lok, $"TREN {Yuzde(tren.Saglik)} · {d / 1000f:F1} km", new Color(0.4f, 1f, 0.45f), true);
                }
                var sec = nisan != null ? nisan.Secili : null;
                bool okGoster = !gizle && sec != null && sec.Canli;
                hedefOku.gameObject.SetActive(okGoster);
                if (okGoster)
                {
                    bool ekranda = KenarIsaret(cam, hedefOku, sec.Nokta + Vector3.up * 4f, $"HEDEF: {sec.Ad}", new Color(1f, 0.9f, 0.3f), false);
                    if (ekranda) hedefOku.gameObject.SetActive(false);   // on screen its own marker shows it
                }
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
