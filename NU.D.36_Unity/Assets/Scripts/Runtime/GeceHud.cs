using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 7 night-mission HUD (display only; every number comes from the flight model, the mission and the lights):
    ///   top left — the bridge, searchlights out, bombs, health, score and the time left until dawn;
    ///   top centre — the current task with the distance to it and the last message;
    ///   bottom left — airspeed dial and throttle lever (HudGosterge / HudGazGostergesi, with digital read-outs), then
    ///                 height above the ground, vertical speed, heading, angle of attack (airspeed and throttle are written
    ///                 in this text block only when the scene has no gauges);
    ///   centre — blinking warnings (stall, searchlight, low altitude, pull up, heard by the sound locator, dawn);
    ///   on screen — a marker on every standing target and, in the landing phase, on the landing zone; off-screen markers stick
    ///   to the screen edge so the way to them is always visible; while the sound locator hears the aircraft, a red
    ///   "lead mark" stands where it was heard (the paper's "duyulan kare").
    /// Assists (2026-11-10, optional references): the targeting system's selected target gets a larger marker with its lock
    /// state (KİLİTLİ / MENZİLDE); the flight text shows easy flight, the brake and the automatic release; the warning line
    /// says when the terrain protection pulls the aircraft up.
    /// </summary>
    public class GeceHud : MonoBehaviour
    {
        [SerializeField] GeceGorevi gorev;
        [SerializeField] Camera kamera;
        [SerializeField] UcakIsiklari isiklar;
        [SerializeField] Isildak[] isildaklar;
        [SerializeField] SesDinleyici kulak;
        [SerializeField] NisanSistemi nisan;
        [SerializeField] UcusYardimcisi yardimci;
        [SerializeField] Color duyulanRengi = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] Text hedefMetni;
        [SerializeField] Text gorevMetni;
        [SerializeField] Text bildirimMetni;
        [SerializeField] Text ucusMetni;
        [SerializeField] Text uyariMetni;
        [SerializeField] GameObject gostergeler;
        [SerializeField] Text hizMetni;
        [SerializeField] Text gazMetni;
        [SerializeField] RectTransform isaretKatmani;
        [SerializeField] Font yazi;
        [SerializeField] Color kuleRengi = new Color(1f, 0.55f, 0.2f, 1f);
        [SerializeField] Color kopruRengi = new Color(1f, 0.86f, 0.3f, 1f);
        [SerializeField] Color inisRengi = new Color(0.35f, 1f, 0.45f, 1f);
        [SerializeField, Min(1f)] float bildirimSuresi = 6f;

        sealed class Isaret
        {
            public RectTransform kok;
            public Image simge;
            public Text etiket;
        }

        readonly List<Isaret> isaretler = new List<Isaret>();
        Isaret inisIsareti, duyulanIsareti;

        public GeceGorevi Gorev { get => gorev; set => gorev = value; }
        /// <summary>Last warning text (empty when none); for tests.</summary>
        public string Uyari { get; private set; } = "";

        void Start()
        {
            if (kamera == null)
                kamera = Camera.main;
            if (gorev != null && gorev.Hedefler != null)
                foreach (Yikilabilir h in gorev.Hedefler)
                    isaretler.Add(IsaretYap(h != null && h.Tur == HedefTuru.Kule ? kuleRengi : kopruRengi));
            inisIsareti = IsaretYap(inisRengi);
            duyulanIsareti = IsaretYap(duyulanRengi);
            duyulanIsareti.simge.rectTransform.localRotation = Quaternion.identity;   // a square, not a diamond
        }

        /// <summary>m:ss for the dawn clock.</summary>
        public static string SureYazisi(float saniye)
        {
            if (float.IsInfinity(saniye)) return "--:--";
            int s = Mathf.CeilToInt(Mathf.Max(0f, saniye));
            return $"{s / 60}:{s % 60:00}";
        }

        Isaret IsaretYap(Color renk)
        {
            var kok = new GameObject("Isaret", typeof(RectTransform)).GetComponent<RectTransform>();
            kok.SetParent(isaretKatmani, false);
            kok.sizeDelta = new Vector2(22f, 22f);
            var simge = new GameObject("Simge", typeof(RectTransform)).AddComponent<Image>();
            simge.rectTransform.SetParent(kok, false);
            simge.rectTransform.sizeDelta = new Vector2(18f, 18f);
            simge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            simge.color = renk;
            simge.raycastTarget = false;
            var etiket = new GameObject("Etiket", typeof(RectTransform)).AddComponent<Text>();
            etiket.rectTransform.SetParent(kok, false);
            etiket.rectTransform.sizeDelta = new Vector2(260f, 30f);
            etiket.verticalOverflow = VerticalWrapMode.Overflow;
            etiket.rectTransform.anchoredPosition = new Vector2(0f, -26f);
            etiket.font = yazi;
            etiket.fontSize = 18;
            etiket.alignment = TextAnchor.UpperCenter;
            etiket.color = renk;
            etiket.raycastTarget = false;
            etiket.horizontalOverflow = HorizontalWrapMode.Overflow;
            var golge = etiket.gameObject.AddComponent<Outline>();
            golge.effectColor = new Color(0f, 0f, 0f, 0.9f);
            return new Isaret { kok = kok, simge = simge, etiket = etiket };
        }

        void LateUpdate()
        {
            if (gorev == null)
                return;
            bool aktif = !gorev.Bitti;
            foreach (Text t in new[] { hedefMetni, gorevMetni, bildirimMetni, ucusMetni, uyariMetni, hizMetni, gazMetni })
                if (t != null && t.enabled != aktif) t.enabled = aktif;
            if (isaretKatmani != null && isaretKatmani.gameObject.activeSelf != aktif)
                isaretKatmani.gameObject.SetActive(aktif);
            if (gostergeler != null && gostergeler.activeSelf != aktif)
                gostergeler.SetActive(aktif);
            if (!aktif)
                return;

            UcusKontrol u = gorev.Ucus;
            Vector3 konum = u != null ? u.transform.position : Vector3.zero;
            float irtifa = ZeminOlcer.Irtifa(konum) - (gorev.Algilayici != null ? gorev.Algilayici.TekerOfseti : 0f);

            if (hedefMetni != null)
            {
                BombaYuku y = gorev.Yuk;
                string kopru = gorev.AnaHedef != null
                    ? $"KÖPRÜ    {(gorev.AnaHedef.Yikik ? "YIKILDI" : "AYAKTA")}\n"
                    : $"KÖPRÜ    {gorev.Sayi(HedefTuru.Kopru, true)}/{gorev.Sayi(HedefTuru.Kopru, false)}\n";
                hedefMetni.text =
                    kopru +
                    $"IŞILDAK  {gorev.Sayi(HedefTuru.Kule, true)}/{gorev.Sayi(HedefTuru.Kule, false)} söndü\n" +
                    $"BOMBA    {(y != null ? y.Kalan : 0)}/{(y != null ? y.Kapasite : 0)}\n" +
                    $"CAN      {gorev.KalanCan}\n" +
                    $"PUAN     {gorev.Puan}" +
                    (gorev.SafakSuresi > 0f ? $"\nŞAFAK    {SureYazisi(gorev.KalanSure)}" : "");
            }
            if (gorevMetni != null)
                gorevMetni.text = GorevYazisi(konum);
            if (bildirimMetni != null)
            {
                float yas = Time.time - gorev.BildirimZamani;
                bildirimMetni.text = yas < bildirimSuresi ? gorev.Bildirim : "";
                var c = bildirimMetni.color;
                c.a = Mathf.Clamp01((bildirimSuresi - yas) / 1.5f);
                bildirimMetni.color = c;
            }
            if (u != null)
            {
                if (hizMetni != null) hizMetni.text = HizYazisi(u.Hiz);
                if (gazMetni != null) gazMetni.text = GazYazisi(u.GazOrani);
            }
            if (ucusMetni != null && u != null)
            {
                float yon = Mathf.Repeat(u.transform.eulerAngles.y, 360f);
                bool kadran = gostergeler != null;
                ucusMetni.text =
                    (kadran ? "" : $"HIZ     {u.Hiz * 3.6f:F0} km/sa\n") +
                    $"İRTİFA  {Mathf.Max(0f, irtifa):F0} m\n" +
                    $"DİKEY   {u.DikeyHiz:+0.0;-0.0} m/s\n" +
                    (kadran ? "" : $"GAZ     {u.GazOrani * 100f:F0} %\n") +
                    $"YÖN     {yon:000}°\n" +
                    (u.Aerodinamik ? $"HÜCUM   {u.HucumAcisi:F1}°   {u.YukFaktoru:F1} g\n" : "") +
                    (isiklar != null ? $"FENER   {(isiklar.FenerAcik ? "AÇIK" : "KAPALI")} (L)\n" : "") +
                    YardimYazisi(u) +
                    (Bulut.Icindeki(konum) is Bulut b ? $"\nBULUT   {b.Kare} — ışıldak göremez" : "");
            }
            UyariGuncelle(u, irtifa);
            IsaretleriGuncelle(konum);
        }

        /// <summary>Assist lines of the flight text: easy flight, brake, automatic release.</summary>
        string YardimYazisi(UcusKontrol u)
        {
            string y = u.Aerodinamik ? $"KOLAY   {(u.KolayUcus ? "AÇIK" : "KAPALI")} (K)   FREN {(u.Fren ? "BASILI" : "—")} (F)" : "";
            string etkin = (u.HizKorumasi ? "HIZ KORUMASI   " : "") +
                           (yardimci != null && yardimci.isActiveAndEnabled && yardimci.OtomatikInis ? "OTOMATİK İNİŞ" : "");
            if (etkin.Length > 0)
                y += $"\nYARDIM  {etkin.TrimEnd()}";
            if (nisan != null && nisan.isActiveAndEnabled)
                y += $"\nOTO ATIŞ {(nisan.OtomatikAtis ? "AÇIK" : "KAPALI")} (O)   HEDEF (T)";
            return y;
        }

        /// <summary>Digital read-out under the airspeed dial (m/s in, km/h out).</summary>
        public static string HizYazisi(float hizMs) => $"{Mathf.Max(0f, hizMs) * 3.6f:F0} km/sa";

        /// <summary>Digital read-out under the throttle lever.</summary>
        public static string GazYazisi(float oran) => $"GAZ %{Mathf.RoundToInt(Mathf.Clamp01(oran) * 100f)}";

        string GorevYazisi(Vector3 konum)
        {
            switch (gorev.Asama)
            {
                case GorevAsamasi.Kalkis:
                    return gorev.Ucus != null && gorev.Ucus.KolayUcus
                        ? "KALKIŞ — gazı sonuna kadar aç (↑): uçak 170 km/sa'da kendisi kalkar"
                        : "KALKIŞ — gazı aç (↑), hız 150 km/sa olunca burnu kaldır (S)";
                case GorevAsamasi.Saldiri:
                    if (gorev.AnaHedef != null && !gorev.AnaHedef.Yikik)
                        return $"KÖPRÜYÜ YIK ({gorev.AnaHedef.GorunenAd}) — {Vector3.Distance(konum, gorev.AnaHedef.IsaretNoktasi) / 1000f:F1} km   ·   ışıldaklar isteğe bağlı";
                    Yikilabilir en = EnYakinHedef(konum, out float d);
                    return en == null ? "HEDEFLERİ YIK" : $"HEDEFLERİ YIK — en yakın: {en.GorunenAd}  {d / 1000f:F1} km";
                case GorevAsamasi.Inis:
                    float m = gorev.InisBolgesi != null ? Vector3.Distance(konum, gorev.InisBolgesi.transform.position) : 0f;
                    return $"ÜSSE DÖN VE YEŞİL BÖLGEYE İN — {m / 1000f:F1} km  (pist yönü {Mathf.Repeat(GeceHaritasi.PistYonu + 180f, 360f):000}°)";
                default:
                    return "";
            }
        }

        Yikilabilir EnYakinHedef(Vector3 konum, out float mesafe)
        {
            Yikilabilir en = null;
            mesafe = float.MaxValue;
            foreach (Yikilabilir h in gorev.Hedefler)
            {
                if (h == null || h.Yikik) continue;
                float d = Vector3.Distance(konum, h.IsaretNoktasi);
                if (d < mesafe) { mesafe = d; en = h; }
            }
            return en;
        }

        void UyariGuncelle(UcusKontrol u, float irtifa)
        {
            string uyari = "";
            if (u != null && u.enabled)
            {
                bool yerde = gorev.Algilayici != null && gorev.Algilayici.Yerde;
                bool isikta = false;
                if (isildaklar != null)
                    foreach (Isildak i in isildaklar)
                        isikta |= i != null && i.Yakaladi;
                if (u.TutunmaKaybi) uyari = "TUTUNMA KAYBI! Burnu indir (W), gaz ver";
                else if (u.TutunmaUyarisi) uyari = "HIZ DÜŞÜYOR — TUTUNMA SINIRI";
                else if (yardimci != null && yardimci.isActiveAndEnabled && yardimci.YerKorumasi) uyari = "YER KORUMASI — uçak kendini kaldırıyor";
                else if (yardimci != null && yardimci.isActiveAndEnabled && yardimci.PisteHizalan) uyari = "PİSTE HİZALAN — teker yalnız pistin üstünde iner";
                else if (yardimci != null && yardimci.isActiveAndEnabled && yardimci.SinirDonusu) uyari = "HARİTA SINIRI — uçak kendisi geri dönüyor";
                else if (!yerde && irtifa < 60f && u.DikeyHiz < -irtifa / 4f && !u.KolayUcus) uyari = "YERE ÇOK YAKIN — BURNU KALDIR!";
                if (isikta) uyari = uyari.Length > 0 ? uyari + "\nIŞILDAK SENİ YAKALADI!" : "IŞILDAK SENİ YAKALADI!";
                else if (kulak != null && kulak.Duyuyor && uyari.Length == 0) uyari = "SES DİNLEYİCİ SENİ DUYUYOR — gazı kıs ya da uzaklaş";
                if (gorev.SafakSuresi > 0f && gorev.KalanSure <= 60f)
                    uyari = (uyari.Length > 0 ? uyari + "\n" : "") + $"ŞAFAK YAKLAŞIYOR — {SureYazisi(gorev.KalanSure)}";
            }
            Uyari = uyari;
            if (uyariMetni != null)
            {
                uyariMetni.text = uyari;
                var c = uyariMetni.color;
                c.a = Mathf.Repeat(Time.time, 0.6f) < 0.42f ? 1f : 0.35f;
                uyariMetni.color = c;
            }
        }

        void IsaretleriGuncelle(Vector3 konum)
        {
            if (isaretKatmani == null || kamera == null)
                return;
            Yikilabilir secili = nisan != null && nisan.isActiveAndEnabled ? nisan.Hedef : null;
            for (int i = 0; i < isaretler.Count; i++)
            {
                Yikilabilir h = gorev.Hedefler[i];
                bool goster = h != null && !h.Yikik && (gorev.Asama == GorevAsamasi.Saldiri || gorev.Asama == GorevAsamasi.Kalkis || h == secili);
                Yerlestir(isaretler[i], goster, goster ? h.IsaretNoktasi : Vector3.zero, goster ? h.GorunenAd : "", konum);
                if (goster)
                    SeciliGoster(isaretler[i], h == secili, h);
            }
            bool inis = gorev.Asama == GorevAsamasi.Inis && gorev.InisBolgesi != null;
            Yerlestir(inisIsareti, inis, inis ? gorev.InisBolgesi.transform.position : Vector3.zero, "İNİŞ BÖLGESİ", konum);
            bool duydu = kulak != null && kulak.Duyuyor && gorev.Ucus != null && gorev.Ucus.enabled;
            Vector3 duyulan = duydu ? kulak.DuyulanKonum : Vector3.zero;
            Yerlestir(duyulanIsareti, duydu, duyulan, duydu ? $"DUYULDU {GeceHaritasi.KareAdi(duyulan.x, duyulan.z) ?? ""}" : "", konum);
        }

        /// <summary>The targeting system's target: a larger marker, its lock state, blinking while locked, red in release range.</summary>
        void SeciliGoster(Isaret i, bool secili, Yikilabilir h)
        {
            Color renk = h.Tur == HedefTuru.Kule ? kuleRengi : kopruRengi;
            if (!secili)
            {
                i.simge.color = i.etiket.color = renk;
                return;
            }
            i.simge.rectTransform.localScale *= 1.6f;
            i.etiket.text = $"[ {i.etiket.text} ]\n{nisan.Durum}";
            Color c = nisan.Menzilde ? new Color(1f, 0.3f, 0.25f, 1f) : nisan.Kilitli ? new Color(1f, 0.95f, 0.4f, 1f) : renk;
            if (nisan.Kilitli && Mathf.Repeat(Time.unscaledTime, 0.5f) > 0.32f)
                c = Color.white;
            i.simge.color = i.etiket.color = c;
        }

        void Yerlestir(Isaret i, bool goster, Vector3 dunya, string ad, Vector3 ucak)
        {
            if (i.kok.gameObject.activeSelf != goster)
                i.kok.gameObject.SetActive(goster);
            if (!goster)
                return;
            Vector3 ekran = kamera.WorldToScreenPoint(dunya);
            bool arkada = ekran.z < 0f;
            if (arkada)
                ekran = new Vector3(Screen.width - ekran.x, Screen.height - ekran.y, 0f);
            const float pay = 48f;
            bool kenarda = arkada || ekran.x < pay || ekran.x > Screen.width - pay || ekran.y < pay || ekran.y > Screen.height - pay;
            if (kenarda)
            {
                // Push the marker to the screen edge along the direction from the centre.
                var merkez = new Vector2(Screen.width / 2f, Screen.height / 2f);
                Vector2 yon = new Vector2(ekran.x, ekran.y) - merkez;
                if (arkada && yon.sqrMagnitude < 1f) yon = Vector2.down;
                float olcek = Mathf.Min((Screen.width / 2f - pay) / Mathf.Max(Mathf.Abs(yon.x), 1e-3f),
                                        (Screen.height / 2f - pay) / Mathf.Max(Mathf.Abs(yon.y), 1e-3f));
                Vector2 kenar = merkez + yon * Mathf.Min(olcek, arkada ? olcek : 1f);
                ekran = new Vector3(kenar.x, kenar.y, 0f);
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(isaretKatmani, ekran, null, out Vector2 yerel))
                i.kok.anchoredPosition = yerel;
            i.simge.rectTransform.localScale = Vector3.one * (kenarda ? 0.8f : 1f);
            float km = Vector3.Distance(ucak, dunya) / 1000f;
            i.etiket.text = $"{ad} {km:F1} km";
        }
    }
}
