using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NU36
{
    /// <summary>
    /// Lab 5, Step 04: the aircraft's bomb load (Hurricane Mk IIB: 2 × 250 lb). The release key drops one bomb with the
    /// aircraft's current velocity; <see cref="Ikmal"/> refills the load at the base. The key is an InputAction owned by
    /// this component (default Space), so the shared "Ucus" action map of Lab 3 is not changed.
    /// Release window (GDD §9.2 / worksheet): dive 45°–70°, 120–260 m above the ground.
    /// </summary>
    public class BombaYuku : MonoBehaviour
    {
        public const float PencereEnAzAci = 45f, PencereEnCokAci = 70f;
        public const float PencereEnAzIrtifa = 120f, PencereEnCokIrtifa = 260f;

        [SerializeField] Bomba bombaOn;
        [SerializeField, Min(0)] int kapasite = 2;
        [SerializeField] UcusKontrol ucus;
        [SerializeField] Saglik saglik;
        [Tooltip("Release point relative to the aircraft (local, m): below the fuselage.")]
        [SerializeField] Vector3 birakmaOfseti = new Vector3(0f, -1.6f, 0f);
        [SerializeField] InputAction birakTusu = new InputAction("BombaBirak", InputActionType.Button, "<Keyboard>/space");

        int kalan;
        bool baslatildi;

        /// <summary>Raised after every release with the new bomb.</summary>
        public event Action<Bomba> BombaBirakildi;
        /// <summary>Raised whenever the remaining count changes (release or resupply).</summary>
        public event Action<int> KalanDegisti;

        public int Kapasite => kapasite;
        public int Kalan { get { Baslat(); return kalan; } }
        public Bomba BombaOn { get => bombaOn; set => bombaOn = value; }
        public UcusKontrol Ucus { get => ucus; set => ucus = value; }
        public int BirakilanSayisi { get; private set; }

        /// <summary>Dive angle in degrees (positive = nose below the horizon) of a forward vector.</summary>
        public static float DalisAcisi(Vector3 ileri) => -Mathf.Asin(Mathf.Clamp(ileri.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;

        /// <summary>True inside the release window: 45–70° dive and 120–260 m above the ground.</summary>
        public static bool PencereIcindeMi(float dalisAcisi, float irtifa) =>
            dalisAcisi >= PencereEnAzAci && dalisAcisi <= PencereEnCokAci &&
            irtifa >= PencereEnAzIrtifa && irtifa <= PencereEnCokIrtifa;

        void Baslat()
        {
            if (baslatildi)
                return;
            baslatildi = true;
            kalan = kapasite;
        }

        void Awake()
        {
            Baslat();
            if (ucus == null)
                ucus = GetComponent<UcusKontrol>();
            if (saglik == null)
                saglik = GetComponent<Saglik>();
        }

        void OnEnable() => birakTusu.Enable();
        void OnDisable() => birakTusu.Disable();

        void Update()
        {
            if (birakTusu.WasPressedThisFrame())
                Birak();
        }

        /// <summary>Velocity the bomb inherits: the aircraft's forward speed along its nose.</summary>
        public Vector3 UcakHizi => ucus != null ? transform.forward * ucus.Hiz : Vector3.zero;

        /// <summary>World position where a bomb leaves the aircraft.</summary>
        public Vector3 BirakmaNoktasi => transform.TransformPoint(birakmaOfseti);

        /// <summary>Drops one bomb. Returns null when empty, when the aircraft is stopped (crash, end of the level) or without a prefab.</summary>
        public Bomba Birak()
        {
            Baslat();
            if (kalan <= 0)
            {
                Debug.Log("[BombaYuku] Bomba kalmadı: ikmal için üsse dön.");
                return null;
            }
            if (bombaOn == null || (ucus != null && !ucus.enabled))
                return null;

            Vector3 nokta = BirakmaNoktasi;
            float irtifa = ZeminOlcer.Irtifa(nokta);
            float aci = DalisAcisi(transform.forward);
            Bomba b = Instantiate(bombaOn, nokta, transform.rotation);
            b.Birak(UcakHizi, irtifa, saglik);
            kalan--;
            BirakilanSayisi++;
            bool pencere = PencereIcindeMi(aci, irtifa);
            Debug.Log($"[BombaYuku] Bomba bırakıldı: dalış {aci:F0}°, irtifa {irtifa:F0} m, hız {(ucus != null ? ucus.Hiz : 0f):F0} m/s, " +
                      $"pencere {(pencere ? "İÇİNDE" : "DIŞINDA")}; kalan {kalan}/{kapasite}");
            KalanDegisti?.Invoke(kalan);
            BombaBirakildi?.Invoke(b);
            return b;
        }

        /// <summary>Refills the load (resupply at the base).</summary>
        public void Ikmal()
        {
            Baslat();
            kalan = kapasite;
            KalanDegisti?.Invoke(kalan);
            Debug.Log($"[BombaYuku] İkmal: {kalan}/{kapasite} bomba");
        }
    }
}
