using System;
using UnityEngine;
using H = NU36.KartalGecidiHaritasi;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5), worksheet: "Düşman uçakları gemileri koruyor: oyuncuya saldırıyor". Keeps <see cref="ayniAnda"/>
    /// enemy fighters over the bay in guard mode (<see cref="DusmanUcagi.KorumaAyarla"/>): they patrol over the landing and
    /// attack the player as soon as it comes within the guard radius. A fighter that is hit withdraws trailing smoke (as in
    /// Bölüm 4); after <see cref="yenileme"/> s the next one of the pool flies in from the open sea, until the pool is used up.
    /// The fighters are prefab instances (Bölüm 4's DusmanUcagi) kept inactive in the scene; this hands them the aircraft.
    /// </summary>
    public class KorumaUcuslari : MonoBehaviour
    {
        [SerializeField] UcusKontrol ucus;
        [SerializeField] DusmanUcagi[] ucaklar = new DusmanUcagi[0];
        [SerializeField] Vector3[] girisler = (Vector3[])H.KorumaGirisleri.Clone();
        [SerializeField, Min(1)] int ayniAnda = H.KorumaAyniAnda;
        [SerializeField, Min(0f)] float yenileme = H.KorumaYenileme;
        [SerializeField] bool otomatikBasla = true;

        int siradaki;
        readonly System.Collections.Generic.List<float> bekleyenler = new System.Collections.Generic.List<float>();
        bool basladi;

        public event Action<DusmanUcagi> Dusuruldu;
        public int Dusurulen { get; private set; }
        public int Toplam => ucaklar.Length;
        public int Gonderilen => siradaki;
        public DusmanUcagi[] Ucaklar => ucaklar;
        public float Yenileme { get => yenileme; set => yenileme = Mathf.Max(0f, value); }

        /// <summary>Fighters in the air and still fighting.</summary>
        public int Aktif
        {
            get
            {
                int n = 0;
                foreach (var u in ucaklar)
                    if (u != null && u.gameObject.activeInHierarchy && u.Aktif) n++;
                return n;
            }
        }

        void Awake()
        {
            foreach (DusmanUcagi u in ucaklar)
            {
                if (u == null) continue;
                u.Oyuncu = ucus;
                u.Tren = null;
                u.KorumaAyarla(H.KoyMerkezi, H.DevriyeYaricapi, H.DevriyeYuksekligi, H.KorumaYaricapi, H.HaritaMerkezi, H.CikisYaricapi);
                if (u.TryGetComponent(out Susturulabilir s))
                {
                    s.SerbestHedef = true;
                    s.Susturuldu += Vuruldu;
                }
                u.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            foreach (DusmanUcagi u in ucaklar)
                if (u != null && u.TryGetComponent(out Susturulabilir s)) s.Susturuldu -= Vuruldu;
        }

        void Start()
        {
            if (otomatikBasla) Baslat();
        }

        public void Baslat()
        {
            if (basladi) return;
            basladi = true;
            for (int i = 0; i < ayniAnda; i++) Gonder();
        }

        /// <summary>Sends the next fighter of the pool in (false when the pool is used up).</summary>
        public bool Gonder()
        {
            if (siradaki >= ucaklar.Length) return false;
            DusmanUcagi u = ucaklar[siradaki];
            Vector3 g = girisler.Length > 0 ? girisler[siradaki % girisler.Length] : H.KoyMerkezi + Vector3.up * 400f;
            siradaki++;
            if (u != null) u.Basla(g);
            return true;
        }

        void Vuruldu(Susturulabilir s)
        {
            Dusurulen++;
            if (s.TryGetComponent(out DusmanUcagi u)) Dusuruldu?.Invoke(u);
            bekleyenler.Add(Time.time + yenileme);   // one replacement per fighter shot down
        }

        void Update()
        {
            if (!basladi) return;
            for (int i = bekleyenler.Count - 1; i >= 0; i--)
                if (Time.time >= bekleyenler[i])
                {
                    bekleyenler.RemoveAt(i);
                    Gonder();
                }
        }
    }
}
