using System;
using System.Collections.Generic;
using UnityEngine;
using H = NU36.KartalGecidiHaritasi;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5 · Kartal Geçidi): the landing fleet. Worksheet:
    /// <list type="bullet">
    /// <item>"Koyda aynı anda 4 çıkarma gemisi var; aralıklı duruyorlar": four lanes 300 m apart along the beach; at the start
    /// one ship in each, at staggered distances (they reach the beach one after the other);</item>
    /// <item>"Durdurulan geminin yerine yenisi geliyor; o da tank indiriyor": when a lane's ship is stopped — or (INFERRED,
    /// D-127) has finished unloading — a new one appears out at sea after <see cref="yenilemeGecikmesi"/> s;</item>
    /// <item>12 ships in all. <see cref="Tamamlandi"/> once all of them have come and are done (stopped, or unloaded
    /// with their tanks ashore).</item>
    /// </list>
    /// Counts the tanks that reached dry land (<see cref="KaradakiTank"/>).
    /// </summary>
    public class CikarmaFilosu : MonoBehaviour
    {
        [SerializeField] CikarmaGemisi gemiOn;
        [SerializeField, Min(1)] int toplamGemi = H.GemiSayisi;
        [SerializeField] float[] ilkUzakliklar = (float[])H.IlkUzakliklar.Clone();
        [SerializeField, Min(10f)] float yeniUzaklik = H.YeniGemiUzakligi;
        [SerializeField, Min(0f)] float yenilemeGecikmesi = H.YenilemeGecikmesi;
        [Tooltip("Start the fleet in Start (tests switch it off and call Baslat).")]
        [SerializeField] bool otomatikBasla = true;

        CikarmaGemisi[] seritteki = new CikarmaGemisi[0];
        float[] yeniZamani = new float[0];
        int[] seritParki = new int[0];
        readonly List<CikarmaGemisi> gemiler = new List<CikarmaGemisi>();
        readonly List<CikarmaTanki> karadakiler = new List<CikarmaTanki>();
        bool basladi;

        public event Action<CikarmaGemisi> GemiGeldi;
        public event Action<CikarmaGemisi> GemiDurdu;
        /// <summary>A tank reached dry land; the argument is the new total.</summary>
        public event Action<CikarmaTanki, int> TankKarada;

        public int SeritSayisi => H.SeritZ.Length;
        public int ToplamGemi => toplamGemi;
        /// <summary>Ships that have come so far (spawned).</summary>
        public int Gelen => gemiler.Count;
        public int Durdurulan { get; private set; }
        public int Bosaltan { get; private set; }
        public int KaradakiTank => karadakiler.Count;
        public IReadOnlyList<CikarmaGemisi> Gemiler => gemiler;
        public IReadOnlyList<CikarmaTanki> Karadakiler => karadakiler;
        public CikarmaGemisi SeritGemisi(int i) => i >= 0 && i < seritteki.Length ? seritteki[i] : null;
        public float YenilemeGecikmesi { get => yenilemeGecikmesi; set => yenilemeGecikmesi = Mathf.Max(0f, value); }

        /// <summary>Ships still heading for the beach, beached or unloading.</summary>
        public int TehditSayisi { get { int n = 0; foreach (var g in gemiler) if (g != null && g.Tehdit) n++; return n; } }

        /// <summary>All 12 ships have come and every one is done; no released tank is still on its way to the beach.</summary>
        public bool Tamamlandi
        {
            get
            {
                if (gemiler.Count < toplamGemi) return false;
                foreach (var g in gemiler)
                    if (g != null && (!g.Sonuclandi || g.YoldakiTank > 0)) return false;
                return true;
            }
        }

        void Start()
        {
            if (otomatikBasla) Baslat();
        }

        /// <summary>The first four ships, one per lane, at staggered distances.</summary>
        public void Baslat()
        {
            if (basladi) return;
            basladi = true;
            int n = SeritSayisi;
            seritteki = new CikarmaGemisi[n];
            yeniZamani = new float[n];
            seritParki = new int[n];
            for (int i = 0; i < n && gemiler.Count < toplamGemi; i++)
                Gonder(i, ilkUzakliklar[i % ilkUzakliklar.Length]);
        }

        void Update()
        {
            if (!basladi) return;
            for (int i = 0; i < seritteki.Length; i++)
            {
                CikarmaGemisi g = seritteki[i];
                if (g != null && !g.Sonuclandi) continue;
                if (gemiler.Count >= toplamGemi) continue;
                if (yeniZamani[i] > 0f && Time.time >= yeniZamani[i])
                    Gonder(i, yeniUzaklik);
            }
        }

        /// <summary>Spawns a ship in lane <paramref name="serit"/>, <paramref name="uzaklik"/> metres out from its stop point.</summary>
        public CikarmaGemisi Gonder(int serit, float uzaklik)
        {
            if (gemiOn == null || gemiler.Count >= toplamGemi) return null;
            Vector3 dur = H.YanasmaNoktasi(serit);
            Vector3 bas = dur - H.KiyiyaDogru * uzaklik;
            CikarmaGemisi g = Instantiate(gemiOn, bas, Quaternion.LookRotation(H.KiyiyaDogru), transform);
            g.name = $"Cikarma Gemisi {gemiler.Count + 1} (serit {serit + 1})";
            int s = serit;
            g.ParkYeri = _ => H.ParkYeri(s, seritParki[s]++);
            g.TankKarayaCikti += TankCikti;
            g.Durduruldu += Durdu;
            g.Bosaldi += SeritBosaldi;
            g.Gorevlendir(serit, bas, dur, H.KaraNoktasi(serit));
            seritteki[serit] = g;
            yeniZamani[serit] = -1f;
            gemiler.Add(g);
            GemiGeldi?.Invoke(g);
            return g;
        }

        void TankCikti(CikarmaGemisi g, CikarmaTanki t)
        {
            karadakiler.Add(t);
            TankKarada?.Invoke(t, karadakiler.Count);
        }

        void Durdu(CikarmaGemisi g)
        {
            Durdurulan++;
            GemiDurdu?.Invoke(g);
        }

        void SeritBosaldi(CikarmaGemisi g)
        {
            if (g.Simdiki == CikarmaGemisi.Durum.Ayriliyor) Bosaltan++;
            if (g.Serit >= 0 && g.Serit < yeniZamani.Length && seritteki[g.Serit] == g)
                yeniZamani[g.Serit] = Time.time + yenilemeGecikmesi;
        }

        /// <summary>Ships beached or unloading right now.</summary>
        public int YanasikSayisi
        {
            get
            {
                int n = 0;
                foreach (var g in gemiler)
                    if (g != null && (g.Simdiki == CikarmaGemisi.Durum.Yanasti || g.Simdiki == CikarmaGemisi.Durum.Indiriyor)) n++;
                return n;
            }
        }
    }
}
