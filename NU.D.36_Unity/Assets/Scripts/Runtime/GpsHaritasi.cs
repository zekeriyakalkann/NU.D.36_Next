using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// "GPS" moving map of the night mission (user request 2026-11-10: "hedefleri net gösteren bir GPS"). North up, centred on
    /// the aircraft, drawn into the RectTransform it sits on (built at start, display only):
    ///   • the terrain colour map (the same texture the terrain uses) and the paper map's 200 m grid with the current square;
    ///   • the runway, and in the landing phase the green landing zone;
    ///   • every target: towers orange, the bridge yellow, a fallen one grey; the selected target in a ring that blinks when it
    ///     is locked, with a route line from the aircraft; targets outside the window stick to its edge;
    ///   • the AA guns with their 800 m firing range and the sound locator with its 1000 m listening range;
    ///   • the aircraft as an arrow turned to its heading.
    /// M switches the window width (<c>olcekler</c>).
    /// </summary>
    public class GpsHaritasi : MonoBehaviour
    {
        [SerializeField] GeceGorevi gorev;
        [SerializeField] UcusKontrol ucus;
        [SerializeField] NisanSistemi nisan;
        [Tooltip("Top-down colour map of the terrain square (GeceHaritasi.Boyut, centred on the origin).")]
        [SerializeField] Texture haritaDokusu;
        [SerializeField] Color haritaTonu = new Color(0.62f, 0.7f, 0.66f, 1f);
        [SerializeField] Font yazi;
        [Tooltip("Window widths (m) the M key cycles through.")]
        [SerializeField] float[] olcekler = { 3200f, 6400f };
        [SerializeField] InputAction olcekTusu = new InputAction("GpsOlcek", InputActionType.Button, "<Keyboard>/m");
        [SerializeField] Color kuleRengi = new Color(1f, 0.55f, 0.2f, 1f);
        [SerializeField] Color kopruRengi = new Color(1f, 0.86f, 0.3f, 1f);
        [SerializeField] Color inisRengi = new Color(0.35f, 1f, 0.45f, 1f);
        [SerializeField] Color tehlikeRengi = new Color(1f, 0.2f, 0.15f, 1f);
        [SerializeField] Color sesRengi = new Color(0.8f, 0.45f, 1f, 1f);

        const float BaslikBoyu = 34f, AltBoyu = 54f, Kenar = 6f;

        sealed class HedefIsareti
        {
            public Yikilabilir hedef;
            public RectTransform kok;
            public Image simge;
            public Text etiket;
        }

        RectTransform gorus, harita, ucak, rota, secimHalkasi, pist, inisBolgesi;
        RawImage zemin;
        Text baslik, alt;
        readonly List<HedefIsareti> hedefIsaretleri = new List<HedefIsareti>();
        readonly List<RectTransform> izgaraDikey = new List<RectTransform>(), izgaraYatay = new List<RectTransform>();
        readonly List<(Transform kaynak, RectTransform halka, RectTransform nokta, float yaricap)> bolgeler =
            new List<(Transform, RectTransform, RectTransform, float)>();
        int olcek;
        bool kuruldu;
        static Sprite ucgen, halka, kalinHalka, daire;

        /// <summary>Window width in metres.</summary>
        public float Pencere => olcekler != null && olcekler.Length > 0 ? olcekler[Mathf.Clamp(olcek, 0, olcekler.Length - 1)] : 3200f;
        /// <summary>Side of the square map view in UI units.</summary>
        public float GorusBoyu => gorus != null ? gorus.rect.width : 0f;
        public int IsaretSayisi => hedefIsaretleri.Count;
        public RectTransform UcakIsareti => ucak;
        public RectTransform SecimHalkasi => secimHalkasi;
        public RawImage Zemin => zemin;
        public string Baslik => baslik != null ? baslik.text : "";
        public string AltYazi => alt != null ? alt.text : "";

        /// <summary>UI position (in the map view, centre = aircraft) of a world point for a window of <paramref name="pencere"/> metres.</summary>
        public static Vector2 HaritaKonumu(Vector3 dunya, Vector3 merkez, float pencere, float boy) =>
            new Vector2(dunya.x - merkez.x, dunya.z - merkez.z) * (boy / pencere);

        /// <summary>The colour map rectangle (UV) of a window of <paramref name="pencere"/> m around <paramref name="merkez"/>.</summary>
        public static Rect DokuAlani(Vector3 merkez, float pencere, float dokuBoyu = GeceHaritasi.Boyut) =>
            new Rect((merkez.x - pencere / 2f + dokuBoyu / 2f) / dokuBoyu, (merkez.z - pencere / 2f + dokuBoyu / 2f) / dokuBoyu,
                pencere / dokuBoyu, pencere / dokuBoyu);

        public Yikilabilir IsaretHedefi(int i) => hedefIsaretleri[i].hedef;
        public RectTransform Isaret(int i) => hedefIsaretleri[i].kok;
        public Color IsaretRengi(int i) => hedefIsaretleri[i].simge.color;

        void OnEnable() => olcekTusu.Enable();
        void OnDisable() => olcekTusu.Disable();

        void Start() => Kur();

        /// <summary>Builds the map's UI objects (once).</summary>
        public void Kur()
        {
            if (kuruldu)
                return;
            kuruldu = true;
            if (yazi == null)
                yazi = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Spritelar();
            var kok = (RectTransform)transform;
            float boy = Mathf.Min(kok.rect.width, kok.rect.height - BaslikBoyu - AltBoyu) - 2f * Kenar;

            Image arka = Resim("Arka", kok, null, new Color(0.02f, 0.03f, 0.03f, 0.78f));
            Doldur(arka.rectTransform);
            var cerceve = arka.gameObject.AddComponent<Outline>();
            cerceve.effectColor = new Color(0.93f, 0.85f, 0.65f, 0.75f);
            cerceve.effectDistance = new Vector2(2f, -2f);
            baslik = Yazi("Baslik", kok, 22, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(12f, -BaslikBoyu / 2f), new Vector2(kok.rect.width - 24f, BaslikBoyu));
            alt = Yazi("Alt", kok, 19, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(12f, AltBoyu / 2f + 2f), new Vector2(kok.rect.width - 24f, AltBoyu));
            alt.lineSpacing = 0.95f;

            gorus = Rt("Gorus", kok);
            gorus.anchorMin = gorus.anchorMax = gorus.pivot = new Vector2(0.5f, 1f);
            gorus.sizeDelta = new Vector2(boy, boy);
            gorus.anchoredPosition = new Vector2(0f, -BaslikBoyu);
            gorus.gameObject.AddComponent<RectMask2D>();
            zemin = new GameObject("Zemin", typeof(RectTransform)).AddComponent<RawImage>();
            zemin.rectTransform.SetParent(gorus, false);
            Doldur(zemin.rectTransform);
            zemin.texture = haritaDokusu;
            zemin.color = haritaDokusu != null ? haritaTonu : new Color(0.08f, 0.1f, 0.09f, 1f);
            zemin.raycastTarget = false;

            harita = Rt("Katman", gorus);   // centre of the view = the aircraft
            harita.anchorMin = harita.anchorMax = new Vector2(0.5f, 0.5f);
            harita.sizeDelta = Vector2.zero;

            var izgaraRengi = new Color(1f, 1f, 1f, 0.16f);
            for (int i = 0; i <= Bolum3Harita.Sutun; i++)
                izgaraDikey.Add(Resim("Dikey" + i, harita, null, izgaraRengi).rectTransform);
            for (int j = 0; j <= Bolum3Harita.Satir; j++)
                izgaraYatay.Add(Resim("Yatay" + j, harita, null, izgaraRengi).rectTransform);

            pist = Resim("Pist", harita, null, new Color(0.85f, 0.85f, 0.8f, 0.95f)).rectTransform;
            inisBolgesi = Resim("InisBolgesi", harita, null, new Color(inisRengi.r, inisRengi.g, inisRengi.b, 0.55f)).rectTransform;

            foreach (UcaksavarMevzii u in FindObjectsByType<UcaksavarMevzii>(FindObjectsInactive.Exclude))
                BolgeEkle(u.transform, GeceHaritasi.UcaksavarYaricapi, tehlikeRengi, "Ucaksavar");
            SesDinleyici kulak = FindAnyObjectByType<SesDinleyici>();
            if (kulak != null)
                BolgeEkle(kulak.transform, GeceHaritasi.SesYaricapi, sesRengi, "SesDinleyici");

            rota = Resim("Rota", harita, null, new Color(1f, 1f, 1f, 0.75f)).rectTransform;
            rota.pivot = new Vector2(0f, 0.5f);
            if (gorev != null && gorev.Hedefler != null)
                foreach (Yikilabilir h in gorev.Hedefler)
                    if (h != null)
                        hedefIsaretleri.Add(HedefIsaretiYap(h));
            secimHalkasi = Resim("SecimHalkasi", harita, kalinHalka, Color.white).rectTransform;
            secimHalkasi.sizeDelta = new Vector2(46f, 46f);

            ucak = Resim("Ucak", harita, ucgen, new Color(0.75f, 0.95f, 1f, 1f)).rectTransform;
            ucak.sizeDelta = new Vector2(26f, 26f);
            Text kuzey = Yazi("Kuzey", gorus, 20, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(40f, 26f));
            kuzey.text = "K";
            Guncelle();
        }

        HedefIsareti HedefIsaretiYap(Yikilabilir h)
        {
            RectTransform k = Rt("Hedef_" + h.name, harita);
            k.sizeDelta = new Vector2(16f, 16f);
            Image s = Resim("Simge", k, null, h.Tur == HedefTuru.Kule ? kuleRengi : kopruRengi);
            s.rectTransform.sizeDelta = h.Tur == HedefTuru.Kule ? new Vector2(13f, 13f) : new Vector2(15f, 15f);
            s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Text e = Yazi("Etiket", k, 17, TextAnchor.LowerCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(120f, 22f));
            Vector3 c = h.IsaretNoktasi;
            e.text = h.Tur == HedefTuru.Kopru ? "KÖPRÜ" : GeceHaritasi.KareAdi(c.x, c.z) ?? "KULE";
            return new HedefIsareti { hedef = h, kok = k, simge = s, etiket = e };
        }

        void BolgeEkle(Transform kaynak, float yaricap, Color renk, string ad)
        {
            Image h = Resim(ad + "Menzili", harita, halka, new Color(renk.r, renk.g, renk.b, 0.45f));
            Image n = Resim(ad, harita, daire, renk);
            n.rectTransform.sizeDelta = new Vector2(8f, 8f);
            bolgeler.Add((kaynak, h.rectTransform, n.rectTransform, yaricap));
        }

        void Update()
        {
            if (Time.timeScale > 0f && olcekTusu.WasPressedThisFrame() && olcekler != null && olcekler.Length > 0)
                olcek = (olcek + 1) % olcekler.Length;
        }

        void LateUpdate()
        {
            if (kuruldu)
                Guncelle();
        }

        /// <summary>Moves everything to this frame's aircraft position (also called by tests).</summary>
        public void Guncelle()
        {
            if (!kuruldu || ucus == null)
                return;
            Vector3 merkez = ucus.transform.position;
            float pencere = Pencere, boy = GorusBoyu;
            float m = boy / pencere;   // UI units per metre
            if (zemin.texture != null)
                zemin.uvRect = DokuAlani(merkez, pencere);
            Vector2 K(Vector3 p) => HaritaKonumu(p, merkez, pencere, boy);

            // Paper grid (A–L × 1–9, 200 m squares).
            Rect a = GeceHaritasi.KagitAlani;
            for (int i = 0; i < izgaraDikey.Count; i++)
                Cizgi(izgaraDikey[i], K(new Vector3(a.xMin + i * Bolum3Harita.Kare, 0f, a.yMin)), K(new Vector3(a.xMin + i * Bolum3Harita.Kare, 0f, a.yMax)), 1.5f);
            for (int j = 0; j < izgaraYatay.Count; j++)
                Cizgi(izgaraYatay[j], K(new Vector3(a.xMin, 0f, a.yMin + j * Bolum3Harita.Kare)), K(new Vector3(a.xMax, 0f, a.yMin + j * Bolum3Harita.Kare)), 1.5f);

            Cizgi(pist, K(GeceHaritasi.PistBasi), K(GeceHaritasi.PistSonu), Mathf.Max(3f, GeceHaritasi.PistEni * m));
            bool inis = gorev != null && gorev.Asama == GorevAsamasi.Inis;
            inisBolgesi.gameObject.SetActive(inis);
            if (inis)
                Cizgi(inisBolgesi, K(GeceHaritasi.PistSonu), K(GeceHaritasi.PistSonu - GeceHaritasi.PistEkseni * GeceHaritasi.InisBolgesiBoyu),
                    Mathf.Max(7f, GeceHaritasi.InisBolgesiEni * m));

            foreach (var b in bolgeler)
            {
                if (b.kaynak == null) continue;
                Vector2 k = K(b.kaynak.position);
                b.halka.anchoredPosition = b.nokta.anchoredPosition = k;
                b.halka.sizeDelta = Vector2.one * (2f * b.yaricap * m);
            }

            float yari = boy / 2f - 10f;
            Yikilabilir secili = nisan != null ? nisan.Hedef : null;
            Vector2 seciliKonum = Vector2.zero;
            foreach (HedefIsareti i in hedefIsaretleri)
            {
                if (i.hedef == null) continue;
                Vector2 k = K(i.hedef.IsaretNoktasi);
                bool disarida = Mathf.Abs(k.x) > yari || Mathf.Abs(k.y) > yari;
                if (disarida)
                    k *= yari / Mathf.Max(Mathf.Abs(k.x), Mathf.Abs(k.y));   // to the edge, along the bearing
                i.kok.anchoredPosition = k;
                bool yikik = i.hedef.Yikik;
                Color renk = yikik ? new Color(0.55f, 0.55f, 0.55f, 0.8f) : i.hedef.Tur == HedefTuru.Kule ? kuleRengi : kopruRengi;
                i.simge.color = renk;
                i.etiket.color = renk;
                i.kok.localScale = Vector3.one * (i.hedef == secili ? 1.35f : disarida ? 0.85f : 1f);
                if (i.hedef == secili)
                    seciliKonum = k;
            }

            bool goster = secili != null && gorev != null && !gorev.Bitti;
            secimHalkasi.gameObject.SetActive(goster);
            rota.gameObject.SetActive(goster);
            if (goster)
            {
                secimHalkasi.anchoredPosition = seciliKonum;
                bool kilit = nisan.Kilitli;
                var c = nisan.Menzilde ? new Color(1f, 0.25f, 0.2f, 1f) : kilit ? new Color(1f, 0.9f, 0.3f, 1f) : new Color(1f, 1f, 1f, 0.8f);
                if (kilit && Mathf.Repeat(Time.unscaledTime, 0.5f) > 0.3f) c.a = 0.35f;
                secimHalkasi.GetComponent<Image>().color = c;
                Cizgi(rota, Vector2.zero, seciliKonum, 2f);
                rota.GetComponent<Image>().color = new Color(c.r, c.g, c.b, 0.6f);
            }

            ucak.anchoredPosition = Vector2.zero;
            float yon = Mathf.Repeat(ucus.transform.eulerAngles.y, 360f);
            ucak.localRotation = Quaternion.Euler(0f, 0f, -yon);
            ucak.SetAsLastSibling();

            string kare = GeceHaritasi.KareAdi(merkez.x, merkez.z);
            baslik.text = $"GPS   KARE {kare ?? "--"}   YÖN {yon:000}°   {pencere / 1000f:F1} km (M)";
            alt.text = AltYazisi(merkez);
        }

        string AltYazisi(Vector3 merkez)
        {
            if (gorev != null && gorev.Asama == GorevAsamasi.Inis && (nisan == null || !nisan.Kilitli))
            {
                float d = Vector3.Distance(merkez, GeceHaritasi.InisBolgesiMerkezi) / 1000f;
                return $"İNİŞ BÖLGESİ   {d:F1} km\npist yönü {Mathf.Repeat(GeceHaritasi.PistYonu + 180f, 360f):000}° (yeşil)";
            }
            if (nisan == null || nisan.Hedef == null)
                return "Hedef yok";
            return $"{nisan.Hedef.GorunenAd}   {nisan.Mesafe / 1000f:F1} km   {nisan.Kerteriz:000}°\n{nisan.Durum}";
        }

        // ---------------------------------------------------------------- UI helpers

        /// <summary>Stretches a pivot-left image from a to b with the given thickness.</summary>
        static void Cizgi(RectTransform r, Vector2 a, Vector2 b, float kalinlik)
        {
            Vector2 d = b - a;
            r.pivot = new Vector2(0f, 0.5f);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = a;
            r.sizeDelta = new Vector2(d.magnitude, kalinlik);
            r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        static RectTransform Rt(string ad, Transform ust)
        {
            var go = new GameObject(ad, typeof(RectTransform));
            go.layer = ust.gameObject.layer;
            go.transform.SetParent(ust, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            return r;
        }

        static void Doldur(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        static Image Resim(string ad, Transform ust, Sprite s, Color renk)
        {
            var img = Rt(ad, ust).gameObject.AddComponent<Image>();
            img.sprite = s;
            img.color = renk;
            img.raycastTarget = false;
            return img;
        }

        Text Yazi(string ad, RectTransform ust, int boyut, TextAnchor hiza, Vector2 capa, Vector2 konum, Vector2 alan)
        {
            RectTransform r = Rt(ad, ust);
            r.anchorMin = r.anchorMax = capa;
            r.pivot = new Vector2(capa.x, 0.5f);
            r.sizeDelta = alan;
            r.anchoredPosition = konum;
            var t = r.gameObject.AddComponent<Text>();
            t.font = yazi;
            t.fontSize = boyut;
            t.fontStyle = FontStyle.Bold;
            t.alignment = hiza;
            t.color = new Color(0.93f, 0.87f, 0.7f, 1f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = r.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            return t;
        }

        /// <summary>Procedural sprites: an arrow (aircraft), a ring (ranges, selection) and a dot.</summary>
        static void Spritelar()
        {
            if (ucgen != null)
                return;
            ucgen = SpriteYap(64, (x, y) =>
            {
                // Arrow pointing up: inside the triangle (0,1)-(−0.8,−1)-(0.8,−1), notched at the back.
                float yy = (y + 1f) / 2f;                       // 0 at the back, 1 at the nose
                float yarim = 0.8f * (1f - yy);
                float kenar = Mathf.Clamp01((yarim - Mathf.Abs(x)) * 20f);
                float centik = y < -0.45f ? Mathf.Clamp01((Mathf.Abs(x) - (y + 1f) * 0.9f) * 20f) : 1f;
                return kenar * centik;
            });
            halka = SpriteYap(128, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                return Mathf.Clamp01(1f - Mathf.Abs(r - 0.93f) * 40f);
            });
            kalinHalka = SpriteYap(64, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                return Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) * 7f);
            });
            daire = SpriteYap(32, (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 12f));
        }

        static Sprite SpriteYap(int n, System.Func<float, float, float> alfa)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) / n * 2f - 1f, y = (j + 0.5f) / n * 2f - 1f;
                    px[j * n + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa(x, y)) * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
