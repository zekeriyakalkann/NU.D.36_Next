using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4), plan §5.6 "GPS": a north-up map of the whole level in the bottom-right corner (M shows/hides it).
    /// The map is drawn once at the start from the terrain (height colours with hill shading) and the railway (line, safe
    /// zone in green, departure station, base); markers move on it: the aircraft (arrow), the train, the live enemies of the
    /// awake zones (red: tank, orange: AA gun, magenta: aircraft) and the selected target (yellow, larger) with a line to it.
    /// No image assets: the map and the marker sprites are made in code.
    /// </summary>
    public class Bolum4Gps : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField] RawImage harita;
        [SerializeField] RectTransform isaretKoku;
        [SerializeField] Text bilgi;
        [SerializeField] UcusKontrol ucus;
        [SerializeField] Tren tren;
        [SerializeField] KursunDaglariGorevi gorev;
        [SerializeField] Bolum4Nisan nisan;
        [Tooltip("Half size of the shown square (m), centred on the map centre.")]
        [SerializeField, Min(100f)] float yariBoy = 3000f;
        [SerializeField, Range(64, 512)] int cozunurluk = 256;
        [SerializeField] bool acik = true;
        [SerializeField] InputAction tus = new InputAction("Gps", InputActionType.Button, "<Keyboard>/m");

        Texture2D doku;
        Sprite daire, ok;
        Image ucakIsareti, trenIsareti, hedefIsareti, rota;
        readonly List<Image> dusmanlar = new List<Image>();

        /// <summary>Shown or hidden (M). Only the contents are hidden, so this component keeps listening for the key.</summary>
        public bool Acik
        {
            get => acik;
            set
            {
                acik = value;
                if (panel == null) return;
                if (panel.TryGetComponent(out Image arka)) arka.enabled = value;
                foreach (Transform c in panel) c.gameObject.SetActive(value);
            }
        }
        public Texture2D Doku => doku;
        public int GorunenDusman { get; private set; }

        static readonly Color RenkTank = new Color(1f, 0.25f, 0.2f), RenkAa = new Color(1f, 0.6f, 0.1f), RenkUcak = new Color(1f, 0.3f, 0.9f);

        void OnEnable() => tus.Enable();
        void OnDisable() => tus.Disable();

        void Start()
        {
            daire = Sprite.Create(Sekil(32, (x, y) => x * x + y * y <= 1f), new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            ok = Sprite.Create(Sekil(32, (x, y) => y > -0.8f && Mathf.Abs(x) < (1f - y) * 0.5f && y < 1f), new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            doku = HaritaCiz();
            if (harita != null) harita.texture = doku;
            rota = Isaret("Rota", null, new Color(1f, 0.92f, 0.3f, 0.8f), 2f);
            trenIsareti = Isaret("Tren", daire, Color.white, 10f);
            hedefIsareti = Isaret("Hedef", daire, new Color(1f, 0.92f, 0.3f, 0.9f), 18f);
            ucakIsareti = Isaret("Ucak", ok, new Color(0.3f, 0.95f, 1f), 16f);
            if (bilgi != null)
            {
                // Two short lines inside the panel (the first version ran out of it).
                bilgi.horizontalOverflow = HorizontalWrapMode.Wrap;
                bilgi.verticalOverflow = VerticalWrapMode.Truncate;
                bilgi.fontSize = 15;
                bilgi.rectTransform.sizeDelta = new Vector2(336f, 44f);
                bilgi.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            }
            Acik = acik;
        }

        static Texture2D Sekil(int n, System.Func<float, float, bool> icinde)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                    t.SetPixel(i, j, icinde((i + 0.5f) / n * 2f - 1f, (j + 0.5f) / n * 2f - 1f) ? Color.white : Color.clear);
            t.Apply();
            return t;
        }

        Image Isaret(string ad, Sprite s, Color renk, float boy)
        {
            var go = new GameObject(ad, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(isaretKoku != null ? isaretKoku : (RectTransform)transform, false);
            rt.sizeDelta = new Vector2(boy, boy);
            var im = go.GetComponent<Image>();
            im.sprite = s;
            im.color = renk;
            im.raycastTarget = false;
            return im;
        }

        /// <summary>World (x, z) → position on the map rectangle (local, centred).</summary>
        public Vector2 Harita(Vector3 dunya)
        {
            Vector2 boy = isaretKoku != null ? isaretKoku.rect.size : new Vector2(300f, 300f);
            return new Vector2(dunya.x / (2f * yariBoy) * boy.x, dunya.z / (2f * yariBoy) * boy.y);
        }

        Texture2D HaritaCiz()
        {
            int n = cozunurluk;
            var t = new Texture2D(n, n, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Terrain arazi = Terrain.activeTerrain;
            float hucre = 2f * yariBoy / n;
            var h = new float[n, n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = new Vector3(-yariBoy + (i + 0.5f) * hucre, 0f, -yariBoy + (j + 0.5f) * hucre);
                    h[i, j] = arazi != null ? arazi.SampleHeight(p) + arazi.transform.position.y : 0f;
                }
            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float y = h[i, j];
                    float dx = h[Mathf.Min(i + 1, n - 1), j] - h[Mathf.Max(i - 1, 0), j];
                    float dz = h[i, Mathf.Min(j + 1, n - 1)] - h[i, Mathf.Max(j - 1, 0)];
                    float golge = Mathf.Clamp(0.75f + (-dx + dz) / (4f * hucre) * 0.9f, 0.35f, 1.15f);
                    Color c = y < 160f ? new Color(0.36f, 0.45f, 0.27f) : y < 500f ? Color.Lerp(new Color(0.42f, 0.44f, 0.28f), new Color(0.47f, 0.40f, 0.30f), (y - 160f) / 340f)
                            : y < 1000f ? Color.Lerp(new Color(0.47f, 0.40f, 0.30f), new Color(0.55f, 0.53f, 0.50f), (y - 500f) / 500f)
                            : Color.Lerp(new Color(0.55f, 0.53f, 0.50f), new Color(0.85f, 0.85f, 0.86f), (y - 1000f) / 400f);
                    px[j * n + i] = c * golge;
                }
            // Railway, safe zone, base.
            if (tren != null && tren.Hat != null)
            {
                RayEgrisi e = tren.Hat.Egri;
                for (float s = 0f; s <= e.Uzunluk; s += hucre * 0.5f)
                {
                    Color c = s >= tren.KapiKonumu ? new Color(0.25f, 0.95f, 0.35f) : new Color(0.12f, 0.1f, 0.08f);
                    Boya(px, n, e.Nokta(s), hucre, s >= tren.KapiKonumu ? 2 : 1, c);
                }
                Boya(px, n, e.Nokta(KursunDaglariHaritasi.IstasyonBoyu * 0.5f), hucre, 2, new Color(0.95f, 0.95f, 0.95f));
            }
            Vector3 us = gorev != null ? gorev.UsMerkezi : KursunDaglariHaritasi.UsMerkezi;
            for (float u = -KursunDaglariHaritasi.PistBoyu / 2f; u <= KursunDaglariHaritasi.PistBoyu / 2f; u += hucre * 0.5f)
                Boya(px, n, us + KursunDaglariHaritasi.PistEkseni * u, hucre, 1, new Color(0.3f, 0.55f, 1f));
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        void Boya(Color[] px, int n, Vector3 p, float hucre, int yaricap, Color c)
        {
            int ci = Mathf.FloorToInt((p.x + yariBoy) / hucre), cj = Mathf.FloorToInt((p.z + yariBoy) / hucre);
            for (int dj = -yaricap + 1; dj < yaricap; dj++)
                for (int di = -yaricap + 1; di < yaricap; di++)
                {
                    int i = ci + di, j = cj + dj;
                    if (i >= 0 && j >= 0 && i < n && j < n) px[j * n + i] = c;
                }
        }

        void Update()
        {
            if (Time.timeScale > 0f && tus.WasPressedThisFrame())
                Acik = !acik;
            if (!acik || ucakIsareti == null)
                return;

            Vector3 up = ucus != null ? ucus.transform.position : Vector3.zero;
            ucakIsareti.rectTransform.anchoredPosition = Harita(up);
            float yon = ucus != null ? Mathf.Atan2(ucus.transform.forward.x, ucus.transform.forward.z) * Mathf.Rad2Deg : 0f;
            ucakIsareti.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -yon);
            if (tren != null && tren.Araclar.Length > 0)
                trenIsareti.rectTransform.anchoredPosition = Harita(tren.Araclar[0].transform.position);

            int k = 0;
            foreach (AtesBolgesi b in AtesBolgesi.Hepsi)
            {
                if (b.Simdiki != AtesBolgesi.Durum.Etkin) continue;
                foreach (Susturulabilir s in b.Kaynaklar())
                {
                    if (s.Sustu || !s.gameObject.activeInHierarchy) continue;
                    if (k == dusmanlar.Count) dusmanlar.Add(Isaret("Dusman", daire, Color.red, 8f));
                    Image im = dusmanlar[k++];
                    im.gameObject.SetActive(true);
                    im.color = s.Tur == DusmanTuru.Tank ? RenkTank : s.Tur == DusmanTuru.Ucaksavar ? RenkAa : RenkUcak;
                    im.rectTransform.anchoredPosition = Harita(s.transform.position);
                }
            }
            GorunenDusman = k;
            for (int i = k; i < dusmanlar.Count; i++) dusmanlar[i].gameObject.SetActive(false);

            var secili = nisan != null ? nisan.Secili : null;
            bool hedefVar = secili != null && secili.Canli;
            hedefIsareti.gameObject.SetActive(hedefVar);
            rota.gameObject.SetActive(hedefVar);
            string satir;
            if (hedefVar)
            {
                Vector2 a = Harita(up), h = Harita(secili.Nokta);
                hedefIsareti.rectTransform.anchoredPosition = h;
                Vector2 d = h - a;
                rota.rectTransform.anchoredPosition = (a + h) * 0.5f;
                rota.rectTransform.sizeDelta = new Vector2(2f, d.magnitude);
                rota.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg);
                satir = $"HEDEF {secili.Ad}  {nisan.Mesafe / 1000f:F1} km\n{nisan.Durum}";
            }
            else if (gorev != null && gorev.Simdiki == KursunDaglariGorevi.Asama.UsseDon)
                satir = $"ÜS  {gorev.UsUzakligi / 1000f:F1} km";
            else
                satir = "Hedef yok — treni izle";
            if (bilgi != null) bilgi.text = satir;
        }
    }
}
