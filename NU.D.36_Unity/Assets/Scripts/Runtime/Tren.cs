using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4): the supply train. The locomotive follows a speed schedule along <see cref="RayHatti"/>: it pulls
    /// away from the station (<see cref="kalkisSuresi"/>), runs at a constant speed and passes the safe zone's gate exactly
    /// <see cref="varisSuresi"/> seconds after departure (worksheet: 5 minutes), then brakes to a stop inside the zone. The
    /// cruise speed is computed from the actual line length, so the arrival time does not depend on the drawing.
    /// The wagons follow the locomotive at fixed distances along the line (<see cref="TrenAraci"/>). The train has health
    /// (<see cref="Saglik"/>); when it runs out the train brakes to a stop ("Tren durdu").
    /// </summary>
    public class Tren : MonoBehaviour
    {
        [SerializeField] RayHatti hat;
        [Tooltip("Index 0 = locomotive (front), then the wagons in order.")]
        [SerializeField] TrenAraci[] araclar = new TrenAraci[0];
        [SerializeField] Saglik saglik;
        [SerializeField, Min(0f)] float aralik = 0.8f;
        [SerializeField, Min(0f)] float rayUstu = KursunDaglariHaritasi.RayUstu;
        [Header("Tarife")]
        [SerializeField, Min(1f)] float varisSuresi = KursunDaglariHaritasi.VarisSuresi;
        [SerializeField, Min(0f)] float kalkisSuresi = KursunDaglariHaritasi.KalkisSuresi;
        [Tooltip("Locomotive (middle) position on the line at the start, metres.")]
        [SerializeField, Min(0f)] float baslangicKonumu = KursunDaglariHaritasi.TrenBaslangici;
        [Tooltip("Safe zone gate, metres from the end of the line: the locomotive passes it at the arrival time.")]
        [SerializeField, Min(0f)] float kapiPayi = KursunDaglariHaritasi.GuvenliBolgeBoyu;
        [Tooltip("Stopping point, metres from the end of the line.")]
        [SerializeField, Min(0f)] float durmaPayi = KursunDaglariHaritasi.TrenBitisPayi;
        [SerializeField, Min(0.5f)] float olunceDurmaSuresi = 6f;
        [SerializeField] bool otomatikBasla = true;

        public event Action GuvenliBolgeyeVardi;
        public event Action Durdu;

        public RayHatti Hat { get => hat; set => hat = value; }
        public TrenAraci[] Araclar { get => araclar; set => araclar = value; }
        public Saglik Saglik => saglik;
        public float VarisSuresi => varisSuresi;

        /// <summary>Seconds since departure.</summary>
        public float Gecen { get; private set; }
        /// <summary>Locomotive (middle) position on the line, metres.</summary>
        public float Konum { get; private set; }
        public float Hiz { get; private set; }
        public bool Calisiyor { get; private set; }
        public bool Vardi { get; private set; }
        public bool Vuruldu => saglik != null && saglik.OlduMu;
        public bool DurduMu { get; private set; }
        /// <summary>Seconds left to the safe zone (0 after arrival, +∞ when the train has been stopped by fire).</summary>
        public float KalanSure => Vardi ? 0f : Vuruldu ? float.PositiveInfinity : Mathf.Max(0f, varisSuresi - Gecen);

        public float KapiKonumu => hat != null ? hat.Uzunluk - kapiPayi : 0f;
        public float DurmaKonumu => hat != null ? hat.Uzunluk - durmaPayi : 0f;
        public float SeyirHizi => SeyirHiziHesapla(KapiKonumu - baslangicKonumu, varisSuresi, kalkisSuresi);
        public float FrenSuresi => SeyirHizi > 0f ? 2f * (DurmaKonumu - KapiKonumu) / SeyirHizi : 0f;

        bool vardiBildirildi, durduBildirildi;
        float olumYavaslamasi;

        // ---- schedule (pure functions, tested) --------------------------------------------------------------

        /// <summary>Cruise speed that covers <paramref name="yol"/> metres in <paramref name="sure"/> seconds after a
        /// constant-acceleration start of <paramref name="kalkis"/> seconds.</summary>
        public static float SeyirHiziHesapla(float yol, float sure, float kalkis) => yol / Mathf.Max(0.01f, sure - kalkis * 0.5f);

        /// <summary>Distance from the start at time t: accelerate, cruise up to the gate (reached at <paramref name="sure"/>),
        /// then brake uniformly for <paramref name="fren"/> seconds.</summary>
        public static float Yol(float t, float yol, float sure, float kalkis, float fren)
        {
            float v = SeyirHiziHesapla(yol, sure, kalkis);
            if (t <= 0f) return 0f;
            if (t < kalkis) return 0.5f * v / kalkis * t * t;
            if (t <= sure) return 0.5f * v * kalkis + v * (t - kalkis);
            float u = Mathf.Min(t - sure, fren);
            return yol + v * u - (fren > 0f ? 0.5f * v / fren * u * u : 0f);
        }

        public static float HizAt(float t, float yol, float sure, float kalkis, float fren)
        {
            float v = SeyirHiziHesapla(yol, sure, kalkis);
            if (t <= 0f) return 0f;
            if (t < kalkis) return v * t / kalkis;
            if (t <= sure) return v;
            return fren > 0f ? Mathf.Max(0f, v * (1f - (t - sure) / fren)) : 0f;
        }

        // ---- life cycle -------------------------------------------------------------------------------------

        void Awake()
        {
            if (saglik == null)
                saglik = GetComponent<Saglik>();
        }

        void OnEnable() { if (saglik != null) saglik.Oldu += Vurulunca; }
        void OnDisable() { if (saglik != null) saglik.Oldu -= Vurulunca; }

        void Start()
        {
            Konum = baslangicKonumu;
            Yerlestir(false);
            if (otomatikBasla)
                Basla();
        }

        public void Basla() => Calisiyor = true;

        void Vurulunca()
        {
            olumYavaslamasi = Hiz / olunceDurmaSuresi;
        }

        void FixedUpdate()
        {
            if (Calisiyor)
                Ilerlet(Time.fixedDeltaTime);
        }

        /// <summary>Advances the train by dt seconds (public for tests).</summary>
        public void Ilerlet(float dt)
        {
            if (hat == null || dt <= 0f || DurduMu)
                return;
            if (Vuruldu)
            {
                Hiz = Mathf.Max(0f, Hiz - olumYavaslamasi * dt);
                Konum = Mathf.Min(Konum + Hiz * dt, DurmaKonumu);
                if (Hiz <= 0f)
                {
                    DurduMu = true;
                    if (!durduBildirildi) { durduBildirildi = true; Durdu?.Invoke(); }
                }
            }
            else
            {
                Gecen += dt;
                float yol = KapiKonumu - baslangicKonumu;
                Konum = baslangicKonumu + Yol(Gecen, yol, varisSuresi, kalkisSuresi, FrenSuresi);
                Hiz = HizAt(Gecen, yol, varisSuresi, kalkisSuresi, FrenSuresi);
                if (Gecen >= varisSuresi && !vardiBildirildi)
                {
                    vardiBildirildi = true;
                    Vardi = true;
                    GuvenliBolgeyeVardi?.Invoke();
                }
                if (Gecen >= varisSuresi + FrenSuresi)
                    DurduMu = true;
            }
            Yerlestir(true);
        }

        /// <summary>Positions every vehicle behind the locomotive at <see cref="Konum"/>.</summary>
        public void Yerlestir(bool fizikle)
        {
            if (hat == null || araclar == null || araclar.Length == 0)
                return;
            RayEgrisi egri = hat.Egri;
            float s = Konum;
            for (int i = 0; i < araclar.Length; i++)
            {
                TrenAraci a = araclar[i];
                if (a == null) continue;
                if (i > 0)
                    s -= araclar[i - 1].Uzunluk * 0.5f + aralik + a.Uzunluk * 0.5f;
                a.Yerlestir(egri, s, rayUstu, fizikle);
            }
        }

        /// <summary>Test helper: jumps the schedule to <paramref name="t"/> seconds after departure (no events for the
        /// skipped stretch except the arrival, which <see cref="Ilerlet"/> raises on the next step).</summary>
        public void ZamanaAtla(float t)
        {
            if (Vuruldu || DurduMu) return;
            Gecen = Mathf.Max(0f, t) - Time.fixedDeltaTime;
            Ilerlet(Time.fixedDeltaTime);
        }

        /// <summary>Editor/test helper: places the train at the start position.</summary>
        public void BaslangicaKoy()
        {
            Konum = baslangicKonumu;
            Yerlestir(false);
        }

        /// <summary>Distance along the line of vehicle i's middle.</summary>
        public float AracKonumu(int i)
        {
            float s = Konum;
            for (int k = 1; k <= i && k < araclar.Length; k++)
                s -= araclar[k - 1].Uzunluk * 0.5f + aralik + araclar[k].Uzunluk * 0.5f;
            return s;
        }
    }
}
