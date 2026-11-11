using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4), worksheets 06–07: the aircraft's machine guns. 1 200 rounds, fired in turn from the two wing
    /// muzzles while the fire key is held (left Ctrl or the left mouse button; an InputAction owned by this component,
    /// like the bomb key). Each round leaves with the aircraft's speed plus the muzzle speed, flies straight with a
    /// tracer, and ends at the range; every shot shows a muzzle flash. Empty = no more fire.
    /// Rounds carry the aircraft's side, so they do not hurt the train. <see cref="NisanYonu"/> lets an aim assist bend
    /// the shots (Lab 8 phase 3).
    /// </summary>
    public class Makineli : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [SerializeField] Saglik saglik;
        [SerializeField] Mermi mermiOn;
        [SerializeField] GameObject namluAlevi;
        [Tooltip("Muzzles in the aircraft's local space (m).")]
        [SerializeField] Vector3[] namlular = { new Vector3(-2.4f, 0.3f, 1.4f), new Vector3(2.4f, 0.3f, 1.4f) };
        [SerializeField, Min(0)] int kapasite = 1200;
        [Tooltip("Rounds per second (both guns together).")]
        [SerializeField, Min(1f)] float atisHizi = 16f;
        [SerializeField, Min(1f)] float mermiHizi = 600f;
        [SerializeField, Min(10f)] float menzil = 1000f;
        [SerializeField, Min(0)] int hasar = 10;
        [SerializeField] LayerMask vurulacak;
        [SerializeField] InputAction atesTusu = new InputAction("Ates", InputActionType.Button);

        int kalan;
        bool baslatildi;
        float birikim;
        int sira;

        public event Action<int> KalanDegisti;
        public event Action<Mermi> AtesEdildi;

        public int Kapasite => kapasite;
        public int Kalan { get { Baslat(); return kalan; } }
        public bool Bos => Kalan <= 0;
        public float AtisHizi => atisHizi;
        public float MermiHizi => mermiHizi;
        public float Menzil => menzil;
        public int Hasar => hasar;
        public int AtisSayisi { get; private set; }
        public LayerMask Vurulacak { get => vurulacak; set => vurulacak = value; }
        /// <summary>Held by code (tests, automatic fire) in addition to the key.</summary>
        public bool Tetik { get; set; }
        /// <summary>True while firing this frame (key, <see cref="Tetik"/> or automatic fire) and rounds are left.</summary>
        public bool Atesliyor { get; private set; }
        /// <summary>Optional aim assist: given the muzzle, returns the direction to fire in (null = along the nose).</summary>
        public Func<Vector3, Vector3?> NisanYonu { get; set; }
        /// <summary>Optional automatic trigger (aim assist "otomatik atış").</summary>
        public Func<bool> OtomatikTetik { get; set; }

        void Baslat()
        {
            if (baslatildi) return;
            baslatildi = true;
            kalan = kapasite;
        }

        void Awake()
        {
            Baslat();
            if (ucus == null) ucus = GetComponent<UcusKontrol>();
            if (saglik == null) saglik = GetComponent<Saglik>();
            if (atesTusu.bindings.Count == 0)
            {
                atesTusu.AddBinding("<Keyboard>/leftCtrl");
                atesTusu.AddBinding("<Mouse>/leftButton");
            }
        }

        void OnEnable() => atesTusu.Enable();
        void OnDisable() => atesTusu.Disable();

        void Update()
        {
            // Paused (Time.timeScale 0) or the aircraft stopped (crash, end): no fire.
            bool istek = Time.timeScale > 0f && (ucus == null || ucus.enabled) &&
                         (atesTusu.IsPressed() || Tetik || (OtomatikTetik != null && OtomatikTetik()));
            bool onceki = Atesliyor;
            Atesliyor = istek && Kalan > 0;
            if (!Atesliyor)
            {
                birikim = 0f;
                return;
            }
            // The first round leaves at once, then atisHizi rounds per second.
            birikim = onceki ? birikim + Time.deltaTime * atisHizi : 1f;
            while (birikim >= 1f && kalan > 0)
            {
                Ates();
                birikim -= 1f;
            }
        }

        /// <summary>Test helper: sets the rounds left.</summary>
        public void KalanAyarla(int n)
        {
            Baslat();
            kalan = Mathf.Clamp(n, 0, kapasite);
            KalanDegisti?.Invoke(kalan);
        }

        /// <summary>World position of muzzle i.</summary>
        public Vector3 NamluKonumu(int i) => transform.TransformPoint(namlular[i % namlular.Length]);

        /// <summary>Fires one round from the next muzzle. Returns null when empty.</summary>
        public Mermi Ates()
        {
            Baslat();
            if (kalan <= 0 || mermiOn == null || namlular.Length == 0)
                return null;
            Vector3 cikis = NamluKonumu(sira++);
            Vector3 yon = NisanYonu?.Invoke(cikis) ?? transform.forward;
            float v = mermiHizi + (ucus != null ? Mathf.Max(0f, Vector3.Dot(ucus.HizVektoru, yon)) : 0f);
            Mermi m = Instantiate(mermiOn, cikis, Quaternion.LookRotation(yon));
            m.Ates(yon, v, hasar, vurulacak, true, saglik != null ? saglik.Taraf : Taraf.Notr);
            m.Omur = menzil / v;
            if (namluAlevi != null)
                Instantiate(namluAlevi, cikis, Quaternion.LookRotation(yon), transform);
            kalan--;
            AtisSayisi++;
            KalanDegisti?.Invoke(kalan);
            AtesEdildi?.Invoke(m);
            return m;
        }
    }
}
