using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Pause menu: Esc stops the game (Time.timeScale 0, audio paused) and shows DEVAM, YENİDEN BAŞLA, ANA MENÜ and ÇIKIŞ;
    /// Esc again or DEVAM resumes. The UI is built in code on first use from the Lab 4 button sheet.
    /// Two ways to exist:
    ///   in a scene (Rota07)  — the scene's own pause menu;
    ///   persistent           — created once by the main menu (DontDestroyOnLoad), so the older lab scenes, which have
    ///                          no pause menu of their own, can be paused and left too. It stays idle in the menu scene
    ///                          and while a scene's own pause menu exists.
    /// Loading any scene resumes the game, so a restart never starts frozen.
    /// </summary>
    public class DuraklatmaMenusu : MonoBehaviour
    {
        [SerializeField] Texture dugmeSeti;
        [SerializeField] string anaMenuSahnesi = "AnaMenu";
        [SerializeField] InputAction tus = new InputAction("Duraklat", InputActionType.Button, "<Keyboard>/escape");

        /// <summary>The persistent instance made by the main menu (null until the menu ran).</summary>
        public static DuraklatmaMenusu Kalici { get; private set; }
        /// <summary>True while any pause menu is open.</summary>
        public static bool Duraklatildi { get; private set; }

        static int sahneMenusuSayisi;

        /// <summary>Loads a scene by name; replaceable by tests.</summary>
        public Action<string> SahneYukle = ad => SceneManager.LoadScene(ad);
        /// <summary>Quits; replaceable by tests.</summary>
        public Action Cikis = MenuArayuzu.UygulamadanCik;

        bool kalici;
        float oncekiOlcek = 1f;
        Canvas tuval;
        MenuDugmesi devam, yeniden, anaMenu, cikis;

        public bool Acik { get; private set; }
        public bool KaliciMi => kalici;
        public string AnaMenuSahnesi => anaMenuSahnesi;
        public Texture DugmeSeti => dugmeSeti;
        public MenuDugmesi DevamDugmesi => devam;
        public MenuDugmesi YenidenDugmesi => yeniden;
        public MenuDugmesi AnaMenuDugmesi => anaMenu;
        public MenuDugmesi CikisDugmesi => cikis;

        /// <summary>True when Esc should act here: a scene menu always, the persistent one only outside the menu scene
        /// and when the active scene has no pause menu of its own.</summary>
        public bool Gorevde => !kalici || (sahneMenusuSayisi == 0 && SceneManager.GetActiveScene().name != anaMenuSahnesi);

        public static DuraklatmaMenusu KaliciOlustur(Texture dugmeSeti, string anaMenuSahnesi)
        {
            if (Kalici != null)
                return Kalici;
            var go = new GameObject("Duraklatma Menusu (kalici)");
            go.SetActive(false);   // so OnEnable sees kalici = true
            var m = go.AddComponent<DuraklatmaMenusu>();
            m.kalici = true;
            m.dugmeSeti = dugmeSeti;
            m.anaMenuSahnesi = anaMenuSahnesi;
            DontDestroyOnLoad(go);
            Kalici = m;
            go.SetActive(true);
            return m;
        }

        void OnEnable()
        {
            tus.Enable();
            if (!kalici) sahneMenusuSayisi++;
            SceneManager.sceneLoaded += Yuklendi;
        }

        void OnDisable()
        {
            tus.Disable();
            if (!kalici) sahneMenusuSayisi--;
            SceneManager.sceneLoaded -= Yuklendi;
            if (Acik) Devam();
        }

        void OnDestroy()
        {
            if (Kalici == this) Kalici = null;
        }

        void Yuklendi(Scene s, LoadSceneMode mod)
        {
            if (Acik) Devam();
        }

        void Update()
        {
            if (tus.WasPressedThisFrame() && Gorevde)
            {
                if (Acik) Devam();
                else Ac();
            }
        }

        public void Ac()
        {
            if (Acik) return;
            Kur();
            oncekiOlcek = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Acik = Duraklatildi = true;
            tuval.gameObject.SetActive(true);
            anaMenu.interactable = MenuArayuzu.SahneVar(anaMenuSahnesi) && SceneManager.GetActiveScene().name != anaMenuSahnesi;
            MenuArayuzu.Sec(devam);
        }

        public void Devam()
        {
            if (!Acik) return;
            Time.timeScale = oncekiOlcek;
            AudioListener.pause = false;
            Acik = Duraklatildi = false;
            if (tuval != null) tuval.gameObject.SetActive(false);
        }

        public void YenidenBaslat()
        {
            Devam();
            SahneYukle(SceneManager.GetActiveScene().name);
        }

        public void AnaMenuyeDon()
        {
            Devam();
            SahneYukle(anaMenuSahnesi);
        }

        void Kur()
        {
            if (tuval != null) return;
            MenuArayuzu.OlaySisteminiHazirla(transform);
            tuval = MenuArayuzu.Tuval(transform, "Duraklatma Tuvali", 100);
            var kok = (RectTransform)tuval.transform;
            RectTransform perde = MenuArayuzu.Rt("Perde", kok);
            MenuArayuzu.Doldur(perde);
            MenuArayuzu.Renk(perde, new Color(0.01f, 0.012f, 0.02f, 0.72f)).raycastTarget = true;   // blocks clicks to the game UI

            RectTransform levha = MenuArayuzu.Yerlestir(MenuArayuzu.Rt("Levha", kok), 0.5f, 0f, 540f, new Vector2(560f, 760f));
            MenuArayuzu.Renk(levha, new Color(0.07f, 0.06f, 0.05f, 0.92f));
            var cizgi = levha.gameObject.AddComponent<Outline>();
            cizgi.effectColor = new Color(0.74f, 0.55f, 0.25f, 0.9f);
            cizgi.effectDistance = new Vector2(3f, -3f);

            Text baslik = MenuArayuzu.Metin(MenuArayuzu.Yerlestir(MenuArayuzu.Rt("Baslik", kok), 0.5f, 0f, 240f, new Vector2(540f, 90f)),
                "DURAKLATILDI", 60, TextAnchor.MiddleCenter, MenuArayuzu.Krem);
            baslik.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            var boyut = new Vector2(372f, 116f);
            devam = MenuArayuzu.Dugme(kok, "Devam", "DEVAM", dugmeSeti, 0.5f, 0f, 360f, boyut, Devam);
            yeniden = MenuArayuzu.Dugme(kok, "Yeniden", "YENİDEN BAŞLA", dugmeSeti, 0.5f, 0f, 486f, boyut, YenidenBaslat);
            anaMenu = MenuArayuzu.Dugme(kok, "AnaMenu", "ANA MENÜ", dugmeSeti, 0.5f, 0f, 612f, boyut, AnaMenuyeDon);
            cikis = MenuArayuzu.Dugme(kok, "Cikis", "ÇIKIŞ", dugmeSeti, 0.5f, 0f, 738f, boyut, () => Cikis());
            MenuArayuzu.Metin(MenuArayuzu.Yerlestir(MenuArayuzu.Rt("Ipucu", kok), 0.5f, 0f, 860f, new Vector2(540f, 40f)),
                "Esc: devam", 24, TextAnchor.MiddleCenter, MenuArayuzu.Soluk, false);
            tuval.gameObject.SetActive(false);
        }
    }
}
