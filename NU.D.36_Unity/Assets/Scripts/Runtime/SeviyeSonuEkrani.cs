using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NU36
{
    /// <summary>
    /// Lab 5, Step 05: end-of-level screen (temporary OnGUI, like the Lab 4 end screens). Shows the result and the four
    /// numbers the worksheet asks to write down (time, resupplies, remaining health, total score) and restarts the
    /// level with the button or the R key.
    /// </summary>
    public class SeviyeSonuEkrani : MonoBehaviour
    {
        [SerializeField] SeviyeYoneticisi yonetici;
        [SerializeField] string sahne = "Rota05";
        [SerializeField] InputAction yenidenTusu = new InputAction("YenidenBasla", InputActionType.Button, "<Keyboard>/r");

        /// <summary>Loads a scene by name; replaceable by tests.</summary>
        public Action<string> SahneYukle = ad => SceneManager.LoadScene(ad);

        public SeviyeYoneticisi Yonetici { get => yonetici; set => yonetici = value; }
        public string Sahne => sahne;

        void OnEnable() => yenidenTusu.Enable();
        void OnDisable() => yenidenTusu.Disable();

        void Update()
        {
            if (yonetici != null && yonetici.Bitti && yenidenTusu.WasPressedThisFrame())
                YenidenBaslat();
        }

        public void YenidenBaslat() => SahneYukle(sahne);

        /// <summary>The four numbers of the worksheet, one per line.</summary>
        public static string Ozet(float sure, int ikmal, int can, int puan) =>
            $"Süre: {sure:F1} s\nİkmal: {ikmal}\nKalan can: {can}\nToplam puan: {puan}";

        void OnGUI()
        {
            if (yonetici == null || !yonetici.Bitti)
                return;
            const float w = 420f, h = 330f;
            var alan = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            Color eski = GUI.color;
            GUI.color = yonetici.Basarili ? new Color(0.35f, 0.8f, 0.4f, 1f) : new Color(1f, 0.3f, 0.25f, 1f);
            GUI.Box(alan, GUIContent.none);
            GUI.Box(alan, GUIContent.none);
            GUI.color = eski;
            var baslik = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 30, fontStyle = FontStyle.Bold };
            var alt = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 19 };
            GUI.Label(new Rect(alan.x, alan.y + 12f, w, 44f), yonetici.Basarili ? "BÖLÜM TAMAMLANDI" : yonetici.Neden, baslik);
            GUI.Label(new Rect(alan.x, alan.y + 66f, w, 130f),
                Ozet(yonetici.GecenSure, yonetici.IkmalSayisi, yonetici.KalanCan, yonetici.Puan), alt);
            if (GUI.Button(new Rect(alan.x + (w - 220f) / 2f, alan.y + h - 80f, 220f, 48f), "Yeniden Başla (R)"))
                YenidenBaslat();
        }
    }
}
