using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NU36
{
    /// <summary>
    /// Lab 4 phase 2: reacts to the mission's single failure event (timeout or crash). It stops the
    /// aircraft, waits <c>bekleme</c> seconds so the failure message can be read, then reloads the scene once.
    /// </summary>
    public class YenidenBasla : MonoBehaviour
    {
        public const float VarsayilanBekleme = 2.5f;

        [SerializeField] GorevYoneticisi yonetici;
        [SerializeField] UcusKontrol ucus;
        [SerializeField, Min(0f)] float bekleme = VarsayilanBekleme;
        [SerializeField] string sahne = GorevSonuEkrani.VarsayilanSahne;

        float gecen;
        bool etkin;

        /// <summary>Loads a scene by name; replaceable by tests.</summary>
        public Action<string> SahneYukle = ad => SceneManager.LoadScene(ad);

        public GorevYoneticisi Yonetici { get => yonetici; set => Bagla(value); }
        public UcusKontrol Ucus { get => ucus; set => ucus = value; }
        public float Bekleme { get => bekleme; set => bekleme = Mathf.Max(0f, value); }
        public string Sahne => sahne;
        public bool Bekliyor { get; private set; }
        public bool Yuklendi { get; private set; }
        public string Neden { get; private set; }

        void Awake()
        {
            if (yonetici == null)
                Debug.LogError("[YenidenBasla] GorevYoneticisi atanmamış.", this);
        }

        void OnEnable()
        {
            etkin = true;
            Abone(true);
        }

        void OnDisable()
        {
            etkin = false;
            Abone(false);
        }

        void Bagla(GorevYoneticisi y)
        {
            Abone(false);
            yonetici = y;
            if (etkin)
                Abone(true);
        }

        void Abone(bool ekle)
        {
            if (yonetici == null)
                return;
            yonetici.GorevBasarisiz -= Basarisiz;
            if (ekle)
                yonetici.GorevBasarisiz += Basarisiz;
        }

        void Basarisiz(string neden)
        {
            if (Bekliyor)
                return;
            Bekliyor = true;
            Neden = neden;
            gecen = 0f;
            if (ucus != null)
                ucus.enabled = false; // no more flight updates after failure
        }

        void Update() => Adim(Time.deltaTime);

        public void Adim(float dt)
        {
            if (!Bekliyor || Yuklendi)
                return;
            gecen += dt;
            if (gecen >= bekleme)
            {
                Yuklendi = true;
                SahneYukle(sahne);
            }
        }
    }
}
