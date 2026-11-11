using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Shared uGUI building blocks of the main menu and the pause menu (built in code at run time, so one scene component
    /// is enough). Reference resolution 1920x1080, scaled by screen height (the 1080 px tall layout fits every aspect).
    /// Positions are reference px from the anchor; the "Ust" helpers take y from the top of the screen.
    /// </summary>
    public static class MenuArayuzu
    {
        public static readonly Color Krem = new Color(0.93f, 0.85f, 0.65f, 1f);
        public static readonly Color Soluk = new Color(0.62f, 0.60f, 0.52f, 1f);
        public static readonly Color Golge = new Color(0.02f, 0.015f, 0.01f, 0.95f);

        public static Font Yazi => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static readonly System.Globalization.CultureInfo Turkce = new System.Globalization.CultureInfo("tr-TR");

        /// <summary>Turkish upper case (i → İ, ı → I), unlike ToUpperInvariant.</summary>
        public static string Buyuk(string s) => s == null ? "" : s.ToUpper(Turkce);

        /// <summary>Screen-space overlay canvas with a raycaster, scaled by height.</summary>
        public static Canvas Tuval(Transform ust, string ad, int sira)
        {
            var go = new GameObject(ad, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(ust, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sira;
            var olcek = go.GetComponent<CanvasScaler>();
            olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            olcek.referenceResolution = new Vector2(1920f, 1080f);
            olcek.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            olcek.matchWidthOrHeight = 1f;
            return canvas;
        }

        public static RectTransform Rt(string ad, Transform ust)
        {
            var go = new GameObject(ad, typeof(RectTransform));
            go.transform.SetParent(ust, false);
            go.layer = 5;   // UI
            return (RectTransform)go.transform;
        }

        public static void Doldur(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        /// <summary>Centred at (<paramref name="x"/>, <paramref name="yUst"/>) measured from the anchor <paramref name="capa"/>'s x and the top of the screen.</summary>
        public static RectTransform Yerlestir(RectTransform r, float capaX, float x, float yUst, Vector2 boyut)
        {
            r.anchorMin = r.anchorMax = new Vector2(capaX, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = boyut;
            r.anchoredPosition = new Vector2(x, -yUst);
            return r;
        }

        public static Image Renk(RectTransform r, Color c)
        {
            var img = r.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Text Metin(RectTransform r, string yazi, int boyut, TextAnchor hiza, Color renk, bool kalin = true)
        {
            var t = r.gameObject.AddComponent<Text>();
            t.font = Yazi;
            t.text = yazi;
            t.fontSize = boyut;
            t.fontStyle = kalin ? FontStyle.Bold : FontStyle.Normal;
            t.alignment = hiza;
            t.color = renk;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = r.gameObject.AddComponent<Outline>();
            o.effectColor = Golge;
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        /// <summary>Button from the sheet with a cream bold label; the label shrinks to fit long texts.</summary>
        public static MenuDugmesi Dugme(RectTransform ust, string ad, string yazi, Texture dugmeSeti, float capaX, float x, float yUst,
            Vector2 boyut, UnityAction tik, int yaziBoyutu = 36)
        {
            RectTransform r = Yerlestir(Rt(ad, ust), capaX, x, yUst, boyut);
            var gorsel = r.gameObject.AddComponent<RawImage>();
            gorsel.texture = dugmeSeti;
            gorsel.uvRect = MenuDugmesi.Satir(MenuDugmesi.Normal);
            if (dugmeSeti == null)
                gorsel.color = new Color(0.25f, 0.2f, 0.15f, 0.95f);   // no sheet: plain plate, still usable
            var d = r.gameObject.AddComponent<MenuDugmesi>();
            d.transition = Selectable.Transition.None;
            d.targetGraphic = gorsel;
            // One line high, so best fit shrinks a long label instead of wrapping it onto two lines.
            RectTransform er = Rt("Yazi", r);
            er.anchorMin = new Vector2(0f, 0.5f);
            er.anchorMax = new Vector2(1f, 0.5f);
            er.pivot = new Vector2(0.5f, 0.5f);
            er.offsetMin = new Vector2(boyut.x * 0.1f, -yaziBoyutu * 0.7f);
            er.offsetMax = new Vector2(-boyut.x * 0.1f, yaziBoyutu * 0.7f);
            Text t = Metin(er, yazi, yaziBoyutu, TextAnchor.MiddleCenter, Krem);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(14, yaziBoyutu - 14);
            t.resizeTextMaxSize = yaziBoyutu;
            d.Etiket = t;
            if (tik != null)
                d.onClick.AddListener(tik);
            return d;
        }

        /// <summary>Makes sure an EventSystem with the Input System UI module exists (created under <paramref name="ust"/> if not).</summary>
        public static void OlaySisteminiHazirla(Transform ust)
        {
            if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            if (ust != null)
                go.transform.SetParent(ust, false);
            var modul = go.GetComponent<InputSystemUIInputModule>();
            if (modul.actionsAsset == null)
                modul.AssignDefaultActions();
        }

        /// <summary>Selects a control for keyboard and gamepad navigation.</summary>
        public static void Sec(Selectable s)
        {
            EventSystem es = EventSystem.current != null ? EventSystem.current : Object.FindAnyObjectByType<EventSystem>();
            if (es != null && s != null)
                es.SetSelectedGameObject(s.gameObject);
        }

        /// <summary>Quits the player (stops Play Mode in the editor).</summary>
        public static void UygulamadanCik()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>True when a scene of this name is in the build (and can be loaded by name).</summary>
        public static bool SahneVar(string ad) => !string.IsNullOrEmpty(ad) && Application.CanStreamedLevelBeLoaded(ad);
    }
}
