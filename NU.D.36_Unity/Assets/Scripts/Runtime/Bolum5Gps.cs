using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using H = NU36.KartalGecidiHaritasi;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5): a north-up map in the bottom-right corner (M shows/hides it), as in Bölüm 4. Drawn once at the start
    /// from the terrain: sea in blue, beach in sand colour, land in height colours with hill shading. Markers: the aircraft
    /// (arrow), the ships (orange; red while unloading; grey when stopped), the guard fighters (magenta), the tanks ashore
    /// (dark red), the selected target (yellow) with a line to it. No image assets.
    /// </summary>
    public class Bolum5Gps : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField] RawImage harita;
        [SerializeField] RectTransform isaretKoku;
        [SerializeField] Text bilgi;
        [SerializeField] UcusKontrol ucus;
        [SerializeField] CikarmaFilosu filo;
        [SerializeField] KorumaUcuslari koruma;
        [SerializeField] Bolum4Nisan nisan;
        [SerializeField] KartalGecidiGorevi gorev;
        [Tooltip("Centre and half size of the shown square (m).")]
        [SerializeField] Vector3 merkez = new Vector3(300f, 0f, 0f);
        [SerializeField, Min(100f)] float yariBoy = 3300f;
        [SerializeField, Range(64, 512)] int cozunurluk = 256;
        [SerializeField] bool acik = true;
        [SerializeField] InputAction tus = new InputAction("Gps", InputActionType.Button, "<Keyboard>/m");

        Texture2D doku;
        Sprite daire, ok;
        Image ucakIsareti, hedefIsareti, rota;
        readonly List<Image> isaretler = new List<Image>();

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
        public int GorunenIsaret { get; private set; }

        void OnEnable() => tus.Enable();
        void OnDisable() => tus.Disable();

        void Start()
        {
            daire = Sprite.Create(Sekil(32, (x, y) => x * x + y * y <= 1f), new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            ok = Sprite.Create(Sekil(32, (x, y) => y > -0.8f && Mathf.Abs(x) < (1f - y) * 0.5f && y < 1f), new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            doku = HaritaCiz();
            if (harita != null) harita.texture = doku;
            rota = Isaret("Rota", null, new Color(1f, 0.92f, 0.3f, 0.8f), 2f);
            hedefIsareti = Isaret("Hedef", daire, new Color(1f, 0.92f, 0.3f, 0.9f), 18f);
            ucakIsareti = Isaret("Ucak", ok, new Color(0.3f, 0.95f, 1f), 16f);
            if (bilgi != null)
            {
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
            return new Vector2((dunya.x - merkez.x) / (2f * yariBoy) * boy.x, (dunya.z - merkez.z) / (2f * yariBoy) * boy.y);
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
                    Vector3 p = new Vector3(merkez.x - yariBoy + (i + 0.5f) * hucre, 0f, merkez.z - yariBoy + (j + 0.5f) * hucre);
                    h[i, j] = arazi != null ? arazi.SampleHeight(p) + arazi.transform.position.y : H.Yukseklik(p.x, p.z);
                }
            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float y = h[i, j];
                    Color c;
                    if (y < H.DenizSeviyesi)
                        c = Color.Lerp(new Color(0.22f, 0.42f, 0.55f), new Color(0.08f, 0.2f, 0.36f), Mathf.Clamp01(-y / 30f));
                    else
                    {
                        float dx = h[Mathf.Min(i + 1, n - 1), j] - h[Mathf.Max(i - 1, 0), j];
                        float dz = h[i, Mathf.Min(j + 1, n - 1)] - h[i, Mathf.Max(j - 1, 0)];
                        float golge = Mathf.Clamp(0.75f + (-dx + dz) / (4f * hucre) * 0.9f, 0.35f, 1.15f);
                        c = y < 4f ? new Color(0.78f, 0.72f, 0.55f)
                            : y < 160f ? new Color(0.38f, 0.47f, 0.28f)
                            : y < 600f ? Color.Lerp(new Color(0.42f, 0.44f, 0.28f), new Color(0.47f, 0.40f, 0.30f), (y - 160f) / 440f)
                            : y < 1100f ? Color.Lerp(new Color(0.47f, 0.40f, 0.30f), new Color(0.58f, 0.56f, 0.53f), (y - 600f) / 500f)
                            : Color.Lerp(new Color(0.58f, 0.56f, 0.53f), new Color(0.88f, 0.88f, 0.9f), (y - 1100f) / 400f);
                        c *= golge;
                    }
                    px[j * n + i] = c;
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        Image Sonraki(ref int k)
        {
            if (k == isaretler.Count) isaretler.Add(Isaret("Isaret", daire, Color.red, 8f));
            Image im = isaretler[k++];
            im.gameObject.SetActive(true);
            return im;
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
            ucakIsareti.transform.SetAsLastSibling();

            int k = 0;
            if (filo != null)
            {
                foreach (CikarmaGemisi g in filo.Gemiler)
                {
                    if (g == null || !g.gameObject.activeInHierarchy) continue;
                    Image im = Sonraki(ref k);
                    im.rectTransform.sizeDelta = new Vector2(11f, 11f);
                    im.color = g.Simdiki == CikarmaGemisi.Durum.Durdu ? new Color(0.55f, 0.55f, 0.55f)
                        : g.Simdiki == CikarmaGemisi.Durum.Yanasti || g.Simdiki == CikarmaGemisi.Durum.Indiriyor ? new Color(1f, 0.1f, 0.1f)
                        : g.Tehdit ? new Color(1f, 0.55f, 0.15f) : new Color(0.7f, 0.6f, 0.5f);
                    im.rectTransform.anchoredPosition = Harita(g.transform.position);
                }
                foreach (CikarmaTanki t in filo.Karadakiler)
                {
                    if (t == null) continue;
                    Image im = Sonraki(ref k);
                    im.rectTransform.sizeDelta = new Vector2(5f, 5f);
                    im.color = new Color(0.55f, 0.05f, 0.05f);
                    im.rectTransform.anchoredPosition = Harita(t.transform.position);
                }
            }
            if (koruma != null)
                foreach (DusmanUcagi u in koruma.Ucaklar)
                {
                    if (u == null || !u.gameObject.activeInHierarchy || !u.Aktif) continue;
                    Image im = Sonraki(ref k);
                    im.rectTransform.sizeDelta = new Vector2(8f, 8f);
                    im.color = new Color(1f, 0.3f, 0.9f);
                    im.rectTransform.anchoredPosition = Harita(u.transform.position);
                }
            GorunenIsaret = k;
            for (int i = k; i < isaretler.Count; i++) isaretler[i].gameObject.SetActive(false);

            var secili = nisan != null ? nisan.Secili : null;
            bool hedefVar = secili != null && secili.Canli;
            hedefIsareti.gameObject.SetActive(hedefVar);
            rota.gameObject.SetActive(hedefVar);
            string satir;
            if (hedefVar)
            {
                Vector2 a = Harita(up), hh = Harita(secili.Nokta);
                hedefIsareti.rectTransform.anchoredPosition = hh;
                Vector2 d = hh - a;
                rota.rectTransform.anchoredPosition = (a + hh) * 0.5f;
                rota.rectTransform.sizeDelta = new Vector2(2f, d.magnitude);
                rota.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg);
                satir = $"HEDEF {secili.Ad}  {nisan.Mesafe / 1000f:F1} km\n{nisan.Durum}";
            }
            else if (gorev != null && gorev.Simdiki == KartalGecidiGorevi.Asama.Yaklasma)
                satir = $"KARTAL KOYU  {gorev.KoyUzakligi / 1000f:F1} km — geçidi izle";
            else
                satir = "Hedef yok — koyu izle";
            if (bilgi != null) bilgi.text = satir;
        }
    }
}
