using UnityEngine;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 4 HUD: mission line ("Kalan hedef: n   Puan: x" / "Üsse dönün   Puan: x") and the countdown ("Süre: m:ss"),
    /// the same texts the temporary OnGUI status line of GorevSonuEkrani draws (it is switched off in the HUD scene).
    /// Reads GorevYoneticisi and PuanCuzdani; hidden once the mission is completed or failed, because the end screens
    /// of GorevSonuEkrani take over. The current score only: no stored list.
    /// </summary>
    public class HudMetin : MonoBehaviour
    {
        [SerializeField] GorevYoneticisi yonetici;
        [SerializeField] PuanCuzdani cuzdan;
        [SerializeField] Text gorevMetni;
        [SerializeField] Text sureMetni;
        [SerializeField] Color normalRenk = new Color(0.93f, 0.85f, 0.65f, 1f);
        [SerializeField] Color uyariRenk = new Color(1f, 0.3f, 0.22f, 1f);

        public GorevYoneticisi Yonetici { get => yonetici; set => yonetici = value; }
        public PuanCuzdani Cuzdan { get => cuzdan; set => cuzdan = value; }
        public Text GorevMetni { get => gorevMetni; set => gorevMetni = value; }
        public Text SureMetni { get => sureMetni; set => sureMetni = value; }
        public Color NormalRenk => normalRenk;

        void LateUpdate() => Guncelle();

        public void Guncelle()
        {
            if (yonetici == null || cuzdan == null)
                return;
            bool aktif = yonetici.Durum == GorevDurumu.Aktif || yonetici.Durum == GorevDurumu.Donus;
            if (gorevMetni != null)
            {
                gorevMetni.enabled = aktif;
                if (aktif)
                {
                    string durum = yonetici.DonusAsamasi ? "Üsse dönün" : $"Kalan hedef: {yonetici.Kalan}";
                    string metin = $"{durum}   Puan: {cuzdan.Puan}";
                    if (gorevMetni.text != metin)
                        gorevMetni.text = metin;
                }
            }
            if (sureMetni != null)
            {
                bool goster = aktif && yonetici.GeriSayimAktif;
                sureMetni.enabled = goster;
                if (goster)
                {
                    string metin = HudHesap.SureMetni(yonetici.KalanSure);
                    if (sureMetni.text != metin)
                        sureMetni.text = metin;
                    sureMetni.color = yonetici.KalanSure <= GorevSonuEkrani.UyariSuresi ? uyariRenk : normalRenk;
                }
            }
        }
    }
}
