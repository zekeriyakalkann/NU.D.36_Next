using UnityEngine;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 5 HUD texts (reuses the two Text objects of the Lab 4 HUD): "Mevzi k/4   Bomba b/2   Can c   Puan p" and the
    /// countdown. Display only; hidden once the level has ended (the end screen takes over).
    /// </summary>
    public class SeviyeHud : MonoBehaviour
    {
        [SerializeField] SeviyeYoneticisi yonetici;
        [SerializeField] Text durumMetni;
        [SerializeField] Text sureMetni;
        [SerializeField] Color normalRenk = new Color(0.93f, 0.85f, 0.65f, 1f);
        [SerializeField] Color uyariRenk = new Color(1f, 0.3f, 0.22f, 1f);
        [SerializeField, Min(0f)] float uyariSuresi = 30f;

        public SeviyeYoneticisi Yonetici { get => yonetici; set => yonetici = value; }
        public Text DurumMetni { get => durumMetni; set => durumMetni = value; }
        public Text SureMetni { get => sureMetni; set => sureMetni = value; }

        /// <summary>Status line text for the given values.</summary>
        public static string Metin(int imha, int toplam, int bomba, int kapasite, int can, int puan) =>
            $"Mevzi {imha}/{toplam}   Bomba {bomba}/{kapasite}   Can {can}   Puan {puan}";

        void LateUpdate() => Guncelle();

        public void Guncelle()
        {
            if (yonetici == null)
                return;
            bool aktif = !yonetici.Bitti;
            if (durumMetni != null)
            {
                durumMetni.enabled = aktif;
                BombaYuku y = yonetici.Yuk;
                string metin = Metin(yonetici.ImhaSayisi, yonetici.MevziSayisi, y != null ? y.Kalan : 0, y != null ? y.Kapasite : 0,
                    yonetici.KalanCan, yonetici.Puan);
                if (aktif && durumMetni.text != metin)
                    durumMetni.text = metin;
            }
            if (sureMetni != null)
            {
                sureMetni.enabled = aktif;
                string metin = HudHesap.SureMetni(yonetici.KalanSure);
                if (aktif && sureMetni.text != metin)
                    sureMetni.text = metin;
                sureMetni.color = yonetici.KalanSure <= uyariSuresi ? uyariRenk : normalRenk;
            }
        }
    }
}
