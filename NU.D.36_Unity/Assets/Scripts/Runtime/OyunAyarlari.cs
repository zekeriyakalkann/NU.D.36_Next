using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Player settings of the menu (Seçenekler): graphics quality, full screen and master volume. Stored in PlayerPrefs and
    /// applied by <see cref="Uygula"/> when the main menu opens. The cycling rules are pure functions for the tests.
    /// </summary>
    public static class OyunAyarlari
    {
        public const string KaliteAnahtari = "NU36.Kalite", TamEkranAnahtari = "NU36.TamEkran", SesAnahtari = "NU36.Ses";

        /// <summary>Volume steps the SES button cycles through (1 → 0.75 → … → 0 → 1).</summary>
        public static readonly float[] SesAdimlari = { 1f, 0.75f, 0.5f, 0.25f, 0f };

        public static int Kalite
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KaliteAnahtari, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            set
            {
                int k = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
                PlayerPrefs.SetInt(KaliteAnahtari, k);
                if (QualitySettings.GetQualityLevel() != k)
                    QualitySettings.SetQualityLevel(k, true);
            }
        }

        public static bool TamEkran
        {
            get => PlayerPrefs.GetInt(TamEkranAnahtari, Screen.fullScreen ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(TamEkranAnahtari, value ? 1 : 0);
                Screen.fullScreen = value;
            }
        }

        public static float Ses
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(SesAnahtari, 1f));
            set
            {
                PlayerPrefs.SetFloat(SesAnahtari, Mathf.Clamp01(value));
                AudioListener.volume = Mathf.Clamp01(value);
            }
        }

        /// <summary>Applies the stored settings (quality, screen mode, volume).</summary>
        public static void Uygula()
        {
            if (QualitySettings.GetQualityLevel() != Kalite)
                QualitySettings.SetQualityLevel(Kalite, true);
            if (PlayerPrefs.HasKey(TamEkranAnahtari) && Screen.fullScreen != TamEkran)
                Screen.fullScreen = TamEkran;
            AudioListener.volume = Ses;
        }

        public static void Kaydet() => PlayerPrefs.Save();

        /// <summary>Next quality level, wrapping to 0 after the last.</summary>
        public static int SonrakiKalite(int k, int sayi) => sayi <= 0 ? 0 : (k + 1) % sayi;

        /// <summary>Next volume step after <paramref name="ses"/> (the nearest step is taken as the current one).</summary>
        public static float SonrakiSes(float ses)
        {
            int en = 0;
            for (int i = 1; i < SesAdimlari.Length; i++)
                if (Mathf.Abs(SesAdimlari[i] - ses) < Mathf.Abs(SesAdimlari[en] - ses))
                    en = i;
            return SesAdimlari[(en + 1) % SesAdimlari.Length];
        }

        /// <summary>Turkish label of a quality level: the project has Mobile (0) and PC (1).</summary>
        public static string KaliteAdi(int k, string[] adlar)
        {
            if (adlar == null || k < 0 || k >= adlar.Length) return "?";
            switch (adlar[k])
            {
                case "Mobile": return "DÜŞÜK";
                case "PC": return "YÜKSEK";
                default: return MenuArayuzu.Buyuk(adlar[k]);
            }
        }

        public static string KaliteYazisi => "GRAFİK: " + KaliteAdi(Kalite, QualitySettings.names);
        public static string EkranYazisi => TamEkran ? "EKRAN: TAM EKRAN" : "EKRAN: PENCERE";
        public static string SesYazisi => $"SES: %{Mathf.RoundToInt(Ses * 100f)}";
    }
}
