using UnityEngine;
using UnityEngine.SceneManagement;

namespace NU36
{
    /// <summary>
    /// Lab 4 (phase 1): temporary OnGUI screen. Shows a small status line during the mission and,
    /// after completion, "mission complete", the final score and a restart button that reloads
    /// Rota04. Meant to be replaced by the real UI in a later phase.
    /// </summary>
    public class GorevSonuEkrani : MonoBehaviour
    {
        public const string VarsayilanSahne = "Rota04";
        /// <summary>The countdown is shown as a warning at or below this many seconds.</summary>
        public const float UyariSuresi = 10f;

        [SerializeField] GorevYoneticisi yonetici;
        [SerializeField] PuanCuzdani cuzdan;
        [SerializeField] string yenidenBaslatSahnesi = VarsayilanSahne;
        [SerializeField] bool durumSatiriGoster = true;

        bool etkin;

        public bool Gorunur { get; private set; }
        public bool BasarisizGorunur { get; private set; }
        public string BasarisizNedeni { get; private set; }
        public int SonPuan { get; private set; }
        public string YenidenBaslatSahnesi => yenidenBaslatSahnesi;
        public GorevYoneticisi Yonetici { get => yonetici; set => Bagla(value, cuzdan); }
        public PuanCuzdani Cuzdan { get => cuzdan; set => cuzdan = value; }

        void Awake()
        {
            if (yonetici == null || cuzdan == null)
                Debug.LogError("[GorevSonuEkrani] GorevYoneticisi veya PuanCuzdani atanmamış.", this);
        }

        void OnEnable()
        {
            etkin = true;
            Abone(true);
        }

        void OnDisable()
        {
            etkin = false;
            Abone(false);
        }

        void Bagla(GorevYoneticisi y, PuanCuzdani c)
        {
            Abone(false);
            yonetici = y;
            cuzdan = c;
            if (etkin)
                Abone(true);
        }

        void Abone(bool ekle)
        {
            if (yonetici == null)
                return;
            yonetici.GorevTamamlandi -= GoreviBitir;
            yonetici.GorevBasarisiz -= GorevBasarisizOldu;
            if (ekle)
            {
                yonetici.GorevTamamlandi += GoreviBitir;
                yonetici.GorevBasarisiz += GorevBasarisizOldu;
            }
        }

        void GoreviBitir()
        {
            SonPuan = cuzdan != null ? cuzdan.Puan : 0;
            Gorunur = true;
        }

        void GorevBasarisizOldu(string neden)
        {
            BasarisizNedeni = neden;
            BasarisizGorunur = true;
        }

        public void YenidenBaslat() => SceneManager.LoadScene(yenidenBaslatSahnesi);

        void OnGUI()
        {
            if (Gorunur)
            {
                float w = 360f, h = 190f;
                var alan = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
                GUI.Box(alan, GUIContent.none);
                var baslik = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fontStyle = FontStyle.Bold };
                var puanStili = new GUIStyle(baslik) { fontSize = 20, fontStyle = FontStyle.Normal };
                GUI.Label(new Rect(alan.x, alan.y + 15f, w, 40f), "GÖREV TAMAMLANDI", baslik);
                GUI.Label(new Rect(alan.x, alan.y + 65f, w, 35f), $"Puan: {SonPuan}", puanStili);
                if (GUI.Button(new Rect(alan.x + 90f, alan.y + 120f, 180f, 45f), "Yeniden Başlat"))
                    YenidenBaslat();
            }
            else if (BasarisizGorunur)
            {
                float w = Mathf.Min(520f, Screen.width - 20f), h = 150f;
                var alan = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
                Color eski = GUI.color;
                GUI.color = new Color(1f, 0.25f, 0.2f, 1f);
                GUI.Box(alan, GUIContent.none);
                GUI.color = eski;
                var baslik = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 40, fontStyle = FontStyle.Bold };
                baslik.normal.textColor = Color.white;
                var alt = new GUIStyle(baslik) { fontSize = 18, fontStyle = FontStyle.Normal };
                GUI.Label(new Rect(alan.x, alan.y + 20f, w, 60f), BasarisizNedeni, baslik);
                GUI.Label(new Rect(alan.x, alan.y + 95f, w, 30f), "GÖREV BAŞARISIZ - yeniden başlıyor...", alt);
            }
            else if (durumSatiriGoster && yonetici != null && cuzdan != null)
            {
                string durum = yonetici.DonusAsamasi ? "Üsse dönün" : $"Kalan hedef: {yonetici.Kalan}";
                GUI.Label(new Rect(12f, 8f, 400f, 28f), $"{durum}   Puan: {cuzdan.Puan}");
                if (yonetici.GeriSayimAktif)
                {
                    int sn = Mathf.CeilToInt(yonetici.KalanSure);
                    bool uyari = yonetici.KalanSure <= UyariSuresi;
                    var stil = new GUIStyle(GUI.skin.label) { fontSize = uyari ? 34 : 20, fontStyle = uyari ? FontStyle.Bold : FontStyle.Normal };
                    if (uyari)
                        stil.normal.textColor = Color.red;
                    GUI.Label(new Rect(12f, 34f, 300f, 50f), $"Süre: {sn / 60}:{sn % 60:00}", stil);
                }
            }
        }
    }
}
