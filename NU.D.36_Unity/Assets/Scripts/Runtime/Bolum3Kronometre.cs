using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NU36
{
    /// <summary>
    /// Lab 6, Step 05: stopwatch for the grey box flight "runway → bridge → runway". Starts with Play, stamps the
    /// moment the aircraft first enters the bridge zone and the moment it is back over the runway zone after that,
    /// and converts both to paper turns (1 turn = 15 s). Zones are the horizontal footprints of the given boxes,
    /// grown by <c>pay</c> metres. R reloads the scene. Display only; it does not change the flight.
    /// </summary>
    public class Bolum3Kronometre : MonoBehaviour
    {
        [SerializeField] Transform ucak;
        [SerializeField] Renderer[] kopru;
        [SerializeField] Renderer[] pist;
        [Tooltip("Zone margin around the bridge and runway footprints (m).")]
        [SerializeField, Min(0f)] float pay = 100f;
        [SerializeField, Min(1f)] float turSuresi = Bolum3Harita.TurSuresi;
        [SerializeField] Text durumMetni;
        [SerializeField] Text sureMetni;
        [SerializeField] InputAction yenidenTusu = new InputAction("YenidenBasla", InputActionType.Button, "<Keyboard>/r");

        float gecen;

        public Action<string> SahneYukle = ad => SceneManager.LoadScene(ad);

        public Transform Ucak { get => ucak; set => ucak = value; }
        public float Gecen => gecen;
        /// <summary>Seconds from the start to the first entry into the bridge zone (NaN until then).</summary>
        public float KopruSuresi { get; private set; } = float.NaN;
        /// <summary>Seconds from the start to the return over the runway after the bridge (NaN until then).</summary>
        public float DonusSuresi { get; private set; } = float.NaN;
        public bool Bitti => !float.IsNaN(DonusSuresi);
        public float TurSuresi => turSuresi;

        public event Action<float> KopruyeVarildi;
        public event Action<float> PisteDonuldu;

        /// <summary>Paper turns for a time in seconds.</summary>
        public static float TurKarsiligi(float saniye, float turSuresi = Bolum3Harita.TurSuresi) => saniye / turSuresi;

        /// <summary>True if the horizontal position of <paramref name="p"/> lies over any of the boxes (grown by <paramref name="kenar"/>).</summary>
        public static bool BolgedeMi(Vector3 p, Renderer[] kutular, float kenar)
        {
            if (kutular == null)
                return false;
            foreach (Renderer r in kutular)
            {
                if (r == null)
                    continue;
                Bounds b = r.bounds;
                if (p.x >= b.min.x - kenar && p.x <= b.max.x + kenar && p.z >= b.min.z - kenar && p.z <= b.max.z + kenar)
                    return true;
            }
            return false;
        }

        void OnEnable() => yenidenTusu.Enable();
        void OnDisable() => yenidenTusu.Disable();

        void Update()
        {
            if (yenidenTusu.WasPressedThisFrame())
                SahneYukle(SceneManager.GetActiveScene().name);
            Adim(Time.deltaTime);
            Yaz();
        }

        /// <summary>Advances the stopwatch by <paramref name="dt"/> and checks the zones.</summary>
        public void Adim(float dt)
        {
            if (ucak == null || Bitti)
                return;
            gecen += dt;
            Vector3 p = ucak.position;
            if (float.IsNaN(KopruSuresi))
            {
                if (BolgedeMi(p, kopru, pay))
                {
                    KopruSuresi = gecen;
                    Debug.Log($"[Kronometre] Köprüye varış: {gecen:F1} s = {TurKarsiligi(gecen, turSuresi):F1} tur");
                    KopruyeVarildi?.Invoke(gecen);
                }
            }
            else if (BolgedeMi(p, pist, pay))
            {
                DonusSuresi = gecen;
                Debug.Log($"[Kronometre] Piste dönüş: {gecen:F1} s = {TurKarsiligi(gecen, turSuresi):F1} tur (1 tur = {turSuresi:F0} s)");
                PisteDonuldu?.Invoke(gecen);
            }
        }

        void Yaz()
        {
            if (durumMetni != null)
            {
                string k = float.IsNaN(KopruSuresi) ? "--" : $"{KopruSuresi:F1} s ({TurKarsiligi(KopruSuresi, turSuresi):F1} tur)";
                string d = float.IsNaN(DonusSuresi) ? "--" : $"{DonusSuresi:F1} s ({TurKarsiligi(DonusSuresi, turSuresi):F1} tur)";
                durumMetni.text = $"Köprü: {k}   Dönüş: {d}   (1 tur = {turSuresi:F0} s)";
            }
            if (sureMetni != null)
                sureMetni.text = $"Kronometre: {gecen:F1} s";
        }
    }
}
