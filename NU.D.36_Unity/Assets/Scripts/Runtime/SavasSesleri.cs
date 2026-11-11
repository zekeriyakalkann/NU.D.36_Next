using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (2026-11-11): the battle sounds of Bölüm 4, without touching the shared weapon scripts: it watches the shot
    /// counters of the player's machine gun, the tanks, the AA guns and the enemy aircraft, and the last explosion
    /// (<see cref="Patlama.SonZaman"/>), and plays the synthesised clips of <see cref="SesBankasi"/> where they happen.
    /// </summary>
    public class SavasSesleri : MonoBehaviour
    {
        [SerializeField] Makineli makineli;
        [SerializeField, Range(0f, 1f)] float makineliSesi = 0.45f;
        [SerializeField, Range(0f, 1f)] float dusmanSesi = 0.8f;
        [SerializeField, Range(0f, 1f)] float patlamaSesi = 1f;

        int mkSayi;
        float sonPatlama = -1000f;
        readonly System.Collections.Generic.Dictionary<Object, int> sayilar = new System.Collections.Generic.Dictionary<Object, int>();

        public int CalinanSes { get; private set; }

        void Start()
        {
            if (makineli != null) mkSayi = makineli.AtisSayisi;
            sonPatlama = Patlama.SonZaman;
        }

        void Cal(AudioClip k, Vector3 p, float ses, float enAz, float perde)
        {
            SesBankasi.Cal(k, p, ses, enAz, 1200f, perde);
            CalinanSes++;
        }

        bool Yeni(Object o, int sayi)
        {
            sayilar.TryGetValue(o, out int once);
            sayilar[o] = sayi;
            return sayi > once;
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (makineli != null && makineli.AtisSayisi > mkSayi)
            {
                mkSayi = makineli.AtisSayisi;
                Cal(SesBankasi.Mermi, makineli.transform.position, makineliSesi, 25f, 1f + Random.Range(-0.06f, 0.06f));
            }
            foreach (AtesBolgesi b in AtesBolgesi.Hepsi)
            {
                if (b.Simdiki != AtesBolgesi.Durum.Etkin) continue;
                foreach (GameObject g in b.YerBirimleri)
                {
                    if (g == null || !g.activeInHierarchy) continue;
                    if (g.TryGetComponent(out Tank t) && Yeni(t, t.AtisSayisi)) Cal(SesBankasi.Top, t.transform.position, dusmanSesi, 40f, 1f);
                    if (g.TryGetComponent(out UcaksavarMevzii a) && Yeni(a, a.AtisSayisi)) Cal(SesBankasi.Mermi, a.transform.position, dusmanSesi, 30f, 0.7f);
                }
                foreach (DusmanUcagi u in b.Ucaklar)
                    if (u != null && u.gameObject.activeInHierarchy && Yeni(u, u.AtisSayisi)) Cal(SesBankasi.Mermi, u.transform.position, dusmanSesi * 0.8f, 30f, 0.85f);
            }
            if (Patlama.SonZaman > sonPatlama)
            {
                sonPatlama = Patlama.SonZaman;
                Cal(SesBankasi.Patlama, Patlama.SonNokta, patlamaSesi, 60f, 1f);
            }
        }
    }
}
