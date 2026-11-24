using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5): the beach camera. Seen from the aircraft a landing 1–2 km away is a few pixels, so while a ship is on
    /// the beach (ramp coming down, tanks driving off) a live picture opens in the HUD: a second camera beside the ship,
    /// slowly swinging, looks at the ramp and the beach. It stays a few seconds after the last tank is ashore, then closes.
    /// The ship nearest to the aircraft is shown; C hides or shows the window.
    /// </summary>
    public class KiyiKamerasi : MonoBehaviour
    {
        [SerializeField] CikarmaFilosu filo;
        [SerializeField] UcusKontrol ucus;
        [SerializeField] Camera kamera;
        [SerializeField] GameObject panel;
        [SerializeField] RawImage ekran;
        [SerializeField] Text yazi;
        [SerializeField] Vector2Int cozunurluk = new Vector2Int(640, 360);
        [Tooltip("Seconds the window stays open after the ship has finished unloading.")]
        [SerializeField, Min(0f)] float sonrasiSure = 4f;
        [SerializeField] bool acik = true;
        [SerializeField] InputAction tus = new InputAction("KiyiKamerasi", InputActionType.Button, "<Keyboard>/c");

        RenderTexture doku;
        CikarmaGemisi izlenen;
        float kapanma = -1f, baslangic;

        public bool Gorunuyor => panel != null && panel.activeSelf;
        public CikarmaGemisi Izlenen => izlenen;
        public bool Acik { get => acik; set => acik = value; }
        public Camera Kamera => kamera;

        void Awake()
        {
            if (kamera != null)
            {
                doku = new RenderTexture(cozunurluk.x, cozunurluk.y, 24, RenderTextureFormat.ARGB32) { name = "KiyiKamerasi", antiAliasing = 2 };
                kamera.targetTexture = doku;
                kamera.enabled = false;
            }
            if (ekran != null) ekran.texture = doku;
            if (panel != null) panel.SetActive(false);
        }

        void OnEnable() => tus.Enable();
        void OnDisable() => tus.Disable();

        void OnDestroy()
        {
            if (kamera != null) kamera.targetTexture = null;
            if (doku != null) doku.Release();
        }

        CikarmaGemisi Sec()
        {
            if (filo == null) return null;
            Vector3 benim = ucus != null ? ucus.transform.position : Vector3.zero;
            CikarmaGemisi enIyi = null;
            float d = float.MaxValue;
            foreach (CikarmaGemisi g in filo.Gemiler)
            {
                if (g == null || !g.KiyidaMi) continue;
                float dd = (g.transform.position - benim).sqrMagnitude;
                if (dd < d) { d = dd; enIyi = g; }
            }
            return enIyi;
        }

        void LateUpdate()
        {
            if (Time.timeScale > 0f && tus.WasPressedThisFrame()) acik = !acik;

            bool canli = izlenen != null && izlenen.KiyidaMi;
            if (!canli)
            {
                CikarmaGemisi yeni = Sec();
                if (yeni != null) { izlenen = yeni; baslangic = Time.time; canli = true; }
            }
            if (canli) kapanma = Time.time + sonrasiSure;
            bool goster = acik && izlenen != null && izlenen.gameObject.activeInHierarchy && (canli || Time.time < kapanma);
            if (!goster && !canli && Time.time >= kapanma) izlenen = null;

            if (panel != null && panel.activeSelf != goster) panel.SetActive(goster);
            if (kamera != null && kamera.enabled != goster) kamera.enabled = goster;
            if (!goster || kamera == null) return;

            // Beside the ship, level with the bow, swinging slowly between the side and the beach.
            Transform g = izlenen.transform;
            Vector3 ileri = new Vector3(g.forward.x, 0f, g.forward.z).normalized;
            Vector3 sag = Vector3.Cross(Vector3.up, ileri);
            float aci = 0.5f + 0.35f * Mathf.Sin((Time.time - baslangic) * 0.25f);
            Vector3 hedef = g.position + ileri * 28f + Vector3.up * 1.5f;
            Vector3 konum = hedef + (sag * Mathf.Cos(aci) + ileri * Mathf.Sin(aci)) * 42f + Vector3.up * 12f;
            kamera.transform.SetPositionAndRotation(konum, Quaternion.LookRotation(hedef - konum));
            if (yazi != null)
            {
                string durum = izlenen.Simdiki == CikarmaGemisi.Durum.Yanasti ? "RAMPA İNİYOR"
                    : izlenen.Simdiki == CikarmaGemisi.Durum.Indiriyor ? $"TANK İNİYOR · {izlenen.KarayaCikan}/{izlenen.TankSayisi} KARADA"
                    : $"{izlenen.KarayaCikan}/{izlenen.TankSayisi} TANK KARADA";
                yazi.text = $"KIYI KAMERASI · {izlenen.Serit + 1}. ŞERİT · {durum}";
            }
        }
    }
}
