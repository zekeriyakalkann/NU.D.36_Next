using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NU36
{
    /// <summary>
    /// Lab 7: end-of-mission screen (OnGUI, like the earlier labs' end screens): result, time, targets, resupplies, health and
    /// score; the button or the R key restarts the mission, the second button returns to the main menu (shown only when
    /// the menu scene is in the build). Hidden while the pause menu is open.
    /// </summary>
    public class GeceSonuEkrani : MonoBehaviour
    {
        [SerializeField] GeceGorevi gorev;
        [SerializeField] string sahne = "Rota07";
        [SerializeField] string menuSahnesi = "AnaMenu";
        [SerializeField] InputAction yenidenTusu = new InputAction("YenidenBasla", InputActionType.Button, "<Keyboard>/r");

        /// <summary>Loads a scene by name; replaceable by tests.</summary>
        public Action<string> SahneYukle = ad => SceneManager.LoadScene(ad);

        public GeceGorevi Gorev { get => gorev; set => gorev = value; }
        public string Sahne => sahne;
        public string MenuSahnesi => menuSahnesi;

        void OnEnable() => yenidenTusu.Enable();
        void OnDisable() => yenidenTusu.Disable();

        void Update()
        {
            if (gorev != null && gorev.Bitti && !DuraklatmaMenusu.Duraklatildi && yenidenTusu.WasPressedThisFrame())
                YenidenBaslat();
        }

        public void YenidenBaslat() => SahneYukle(sahne);

        public void AnaMenuyeDon() => SahneYukle(menuSahnesi);

        public static string Ozet(float sure, int yikilan, int toplam, int ikmal, int can, int puan) =>
            $"Süre: {Mathf.FloorToInt(sure / 60f)}:{Mathf.FloorToInt(sure % 60f):00}\nHedef: {yikilan}/{toplam}\nİkmal: {ikmal}\nKalan can: {can}\nToplam puan: {puan}";

        void OnGUI()
        {
            if (gorev == null || !gorev.Bitti || DuraklatmaMenusu.Duraklatildi)
                return;
            const float w = 460f, h = 380f;
            var alan = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            Color eski = GUI.color;
            GUI.color = gorev.Basarili ? new Color(0.3f, 0.85f, 0.4f, 1f) : new Color(1f, 0.3f, 0.25f, 1f);
            GUI.Box(alan, GUIContent.none);
            GUI.Box(alan, GUIContent.none);
            GUI.color = eski;
            var baslik = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, fontStyle = FontStyle.Bold };
            var alt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 20 };
            GUI.Label(new Rect(alan.x, alan.y + 14f, w, 46f), gorev.Neden, baslik);
            string neden = !gorev.Basarili && !string.IsNullOrEmpty(gorev.Ayrinti) ? $"({gorev.Ayrinti})\n" : "";
            string kopru = gorev.AnaHedef != null
                ? $"Köprü: {(gorev.AnaHedef.Yikik ? "yıkıldı" : "ayakta")}   Şafağa: {GeceHud.SureYazisi(gorev.KalanSure)}\n"
                : "";
            GUI.Label(new Rect(alan.x, alan.y + 66f, w, 210f),
                neden + kopru + Ozet(gorev.GecenSure, gorev.YikilanHedef, gorev.ToplamHedef, gorev.IkmalSayisi, gorev.KalanCan, gorev.Puan), alt);
            bool menu = MenuArayuzu.SahneVar(menuSahnesi);
            float dw = menu ? 200f : 240f;
            float x0 = menu ? alan.x + (w - 2f * dw - 16f) / 2f : alan.x + (w - dw) / 2f;
            if (GUI.Button(new Rect(x0, alan.y + h - 82f, dw, 50f), "Yeniden Başla (R)"))
                YenidenBaslat();
            if (menu && GUI.Button(new Rect(x0 + dw + 16f, alan.y + h - 82f, dw, 50f), "Ana Menü"))
                AnaMenuyeDon();
        }
    }
}
