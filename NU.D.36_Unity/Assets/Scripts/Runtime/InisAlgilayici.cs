using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 4 phase 2: ground contact for the Transform-moved aircraft. The terrain has no
    /// colliders, so the aircraft's lowest point (pivot height minus <c>tekerOfseti</c>) is compared with
    /// the exact height of the terrain mesh under it (<see cref="AraziUretici.YuzeyYuksekligi"/>).
    /// At the moment of contact the touchdown is classified:
    /// speed above <c>enFazlaHiz</c>, pitch error (nose angle minus terrain slope along the heading) above
    /// <c>enFazlaPitchHatasi</c>, bank above <c>enFazlaYatis</c> (or upside down), or ground slope above
    /// <c>enFazlaEgim</c> is a crash; otherwise a controlled landing. A crash freezes the aircraft and raises
    /// <see cref="Cakildi"/> once. After a landing the aircraft is held on the ground until it climbs
    /// clear again; only then can the next contact be classified.
    /// </summary>
    public class InisAlgilayici : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [Tooltip("Distance from the aircraft pivot down to the lowest point (wheels), in metres.")]
        [SerializeField, Min(0f)] float tekerOfseti = 1.5f;
        [Header("Güvenli iniş eşikleri")]
        [Tooltip("Maximum speed at touchdown (m/s). The flight controller speed range is 8-42 m/s.")]
        [SerializeField, Min(0f)] float enFazlaHiz = 16f;
        [Tooltip("Maximum |nose pitch - terrain slope along the heading| (degrees).")]
        [SerializeField, Range(0f, 90f)] float enFazlaPitchHatasi = 18f;
        [Tooltip("Maximum bank angle (degrees); upside down always crashes.")]
        [SerializeField, Range(0f, 90f)] float enFazlaYatis = 27f;
        [Tooltip("Maximum slope of the ground under the aircraft (degrees).")]
        [SerializeField, Range(0f, 90f)] float enFazlaEgim = 20f;
        [Tooltip("Height above the ground (metres) the wheels must clear before the next contact counts.")]
        [SerializeField, Min(0f)] float yenidenSilahlanma = 3f;

        /// <summary>Ground height at (x, z). Replaceable, e.g. by tests.</summary>
        public Func<float, float, float> ZeminYuksekligi = (x, z) => AraziUretici.YuzeyYuksekligi(x, z);

        /// <summary>Raised once with the reason when the aircraft crashes.</summary>
        public event Action<string> Cakildi;
        /// <summary>Raised when a contact is classified as a controlled landing.</summary>
        public event Action InisYapildi;

        public UcusKontrol Ucus { get => ucus; set => ucus = value; }
        public float TekerOfseti { get => tekerOfseti; set => tekerOfseti = Mathf.Max(0f, value); }
        public float EnFazlaHiz { get => enFazlaHiz; set => enFazlaHiz = value; }
        public float EnFazlaPitchHatasi { get => enFazlaPitchHatasi; set => enFazlaPitchHatasi = value; }
        public float EnFazlaYatis { get => enFazlaYatis; set => enFazlaYatis = value; }
        public float EnFazlaEgim { get => enFazlaEgim; set => enFazlaEgim = value; }
        public bool Yerde { get; private set; }
        public bool Cakti { get; private set; }
        public string CakmaNedeni { get; private set; }

        void Awake()
        {
            if (ucus == null)
                ucus = GetComponent<UcusKontrol>();
            if (ucus == null)
                Debug.LogError("[InisAlgilayici] UcusKontrol atanmamış; hız okunamıyor.", this);
        }

        void LateUpdate() => Adim();

        /// <summary>Returns null for a controlled landing, otherwise the crash reason.</summary>
        public string Siniflandir(float hiz, float pitchHatasi, float yatis, float egim)
        {
            if (hiz > enFazlaHiz) return "çok hızlı";
            if (pitchHatasi > enFazlaPitchHatasi) return "dik açı";
            if (yatis > enFazlaYatis) return "aşırı yatış";
            if (egim > enFazlaEgim) return "dik arazi";
            return null;
        }

        /// <summary>One detection step for the current transform. Called every LateUpdate.</summary>
        public void Adim()
        {
            if (Cakti)
                return;

            Vector3 p = transform.position;
            float zemin = ZeminYuksekligi(p.x, p.z);
            float taban = p.y - tekerOfseti;

            if (Yerde)
            {
                if (taban > zemin + yenidenSilahlanma)
                    Yerde = false;
                else if (taban < zemin)
                    transform.position = new Vector3(p.x, zemin + tekerOfseti, p.z); // roll on the surface
                return;
            }
            if (taban > zemin)
                return;

            // Contact. Measure the touchdown.
            const float Mesafe = 8f;
            Vector3 ileri = transform.forward;
            float pitch = Mathf.Asin(Mathf.Clamp(ileri.y, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 yatayIleri = new Vector3(ileri.x, 0f, ileri.z);
            float egim = 0f; // terrain slope along the heading
            if (yatayIleri.sqrMagnitude > 1e-4f)
            {
                yatayIleri.Normalize();
                float oteki = ZeminYuksekligi(p.x + yatayIleri.x * Mesafe, p.z + yatayIleri.z * Mesafe);
                egim = Mathf.Atan2(oteki - zemin, Mesafe) * Mathf.Rad2Deg;
            }
            float dx = ZeminYuksekligi(p.x + Mesafe, p.z) - zemin;
            float dz = ZeminYuksekligi(p.x, p.z + Mesafe) - zemin;
            float zeminEgimi = Mathf.Atan2(Mathf.Sqrt(dx * dx + dz * dz), Mesafe) * Mathf.Rad2Deg;

            float yatis = Mathf.Abs(Mathf.Atan2(transform.right.y, transform.up.y) * Mathf.Rad2Deg);
            float hiz = ucus != null ? ucus.Hiz : 0f;

            string neden = Siniflandir(hiz, Mathf.Abs(pitch - egim), yatis, zeminEgimi);
            if (neden != null)
            {
                Cakti = true;
                CakmaNedeni = neden;
                if (ucus != null)
                    ucus.enabled = false; // stop the aircraft
                Debug.Log($"[InisAlgilayici] Çakıldı ({neden}): hız {hiz:F1}, yunuslama {pitch:F1}, eğim {egim:F1}, yatış {yatis:F1}");
                Cakildi?.Invoke(neden);
            }
            else
            {
                Yerde = true;
                transform.position = new Vector3(p.x, zemin + tekerOfseti, p.z);
                Debug.Log($"[InisAlgilayici] Kontrollü iniş: hız {hiz:F1}, yunuslama {pitch:F1}, yatış {yatis:F1}");
                InisYapildi?.Invoke();
            }
        }
    }
}
