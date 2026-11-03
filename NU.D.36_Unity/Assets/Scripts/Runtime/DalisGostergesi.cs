using UnityEngine;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 5, Step 04: dive HUD. Shows only two raw numbers, the dive angle and the height above the ground; the frame
    /// turns green inside the release window (45–70°, 120–260 m). The aiming ring is drawn at the predicted impact
    /// point, found by running the bomb's own integration forward (<see cref="Bomba.DususNoktasi"/>).
    /// The worksheet supplies its own DalisGostergesi.cs; that file was not available, so this is a minimal stand-in.
    /// </summary>
    public class DalisGostergesi : MonoBehaviour
    {
        [SerializeField] BombaYuku yuk;
        [SerializeField] Camera kamera;
        [SerializeField] Image cerceve;
        [SerializeField] Text aciMetni;
        [SerializeField] Text irtifaMetni;
        [SerializeField] RectTransform halka;
        [SerializeField] Color disaridaRenk = new Color(0.08f, 0.07f, 0.05f, 0.55f);
        [SerializeField] Color pencereRengi = new Color(0.15f, 0.75f, 0.25f, 0.8f);
        [Tooltip("The ring is shown only when the nose is at least this many degrees below the horizon.")]
        [SerializeField] float halkaEnAzDalis = 10f;
        [Tooltip("Time step of the prediction (s).")]
        [SerializeField, Min(0.005f)] float tahminAdimi = 0.05f;

        static Sprite halkaSprite;

        public BombaYuku Yuk { get => yuk; set => yuk = value; }
        public float DalisAcisi { get; private set; }
        public float Irtifa { get; private set; }
        public bool PencereIcinde { get; private set; }
        public bool TahminVar { get; private set; }
        public Vector3 TahminNoktasi { get; private set; }

        void Awake()
        {
            if (kamera == null)
                kamera = Camera.main;
            if (halka != null)
            {
                var img = halka.GetComponent<Image>();
                if (img != null && img.sprite == null)
                    img.sprite = HalkaSprite();
            }
        }

        void LateUpdate()
        {
            if (yuk == null)
                return;
            DalisAcisi = BombaYuku.DalisAcisi(yuk.transform.forward);
            Irtifa = ZeminOlcer.Irtifa(yuk.BirakmaNoktasi);
            PencereIcinde = BombaYuku.PencereIcindeMi(DalisAcisi, Irtifa);

            if (aciMetni != null) aciMetni.text = $"DALIŞ {DalisAcisi:F0}°";
            if (irtifaMetni != null) irtifaMetni.text = $"YER {Irtifa:F0} m";
            if (cerceve != null) cerceve.color = PencereIcinde ? pencereRengi : disaridaRenk;

            TahminVar = false;
            if (DalisAcisi >= halkaEnAzDalis && yuk.Kalan > 0 &&
                Bomba.DususNoktasi(yuk.BirakmaNoktasi, yuk.UcakHizi, Physics.gravity, tahminAdimi, ZeminOlcer.Yukseklik,
                    out Vector3 nokta, out _, 15f))
            {
                TahminVar = true;
                TahminNoktasi = nokta;
            }
            HalkaYerlestir();
        }

        void HalkaYerlestir()
        {
            if (halka == null)
                return;
            bool goster = TahminVar && kamera != null;
            Vector3 ekran = goster ? kamera.WorldToScreenPoint(TahminNoktasi) : Vector3.zero;
            goster &= ekran.z > 0f;
            var ust = halka.parent as RectTransform;
            if (goster && ust != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(ust, ekran, null, out Vector2 yerel))
                halka.anchoredPosition = yerel;
            else
                goster = false;
            if (halka.gameObject.activeSelf != goster)
                halka.gameObject.SetActive(goster);
        }

        /// <summary>A white ring with a centre dot, generated once (no texture asset needed).</summary>
        static Sprite HalkaSprite()
        {
            if (halkaSprite != null)
                return halkaSprite;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "NisanHalkasi", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(1f - Mathf.Abs(r - 56f) / 3.5f) + Mathf.Clamp01(1f - r / 4f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            halkaSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return halkaSprite;
        }
    }
}
