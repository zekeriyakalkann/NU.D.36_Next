using System;
using System.Collections.Generic;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4), worksheet 04: one fire zone along the line. Its enemies are hidden until the locomotive comes
    /// within <see cref="onden"/> metres of the zone (they "show up as the train approaches"); the zone's aircraft wave
    /// then flies in from its entry points. When the locomotive leaves the zone the ground units cease fire and the
    /// aircraft withdraw: the stretch to the next zone is calm. Silenced units stay silent.
    /// The zone hands the scene's aircraft and train to its members, so the enemy prefabs carry no scene references.
    /// </summary>
    public class AtesBolgesi : MonoBehaviour
    {
        public enum Durum { Bekliyor, Etkin, Bitti }

        [SerializeField] string ad = "Bölge";
        [SerializeField] Tren tren;
        [SerializeField] UcusKontrol ucus;
        [Tooltip("Zone along the line (m).")]
        [SerializeField, Min(0f)] float bas, son;
        [Tooltip("Enemies show up when the locomotive is this far before the zone (m).")]
        [SerializeField, Min(0f)] float onden = 150f;
        [SerializeField] GameObject[] yerBirimleri = new GameObject[0];
        [SerializeField] DusmanUcagi[] ucaklar = new DusmanUcagi[0];
        [Tooltip("Entry point of each aircraft (outside the zone).")]
        [SerializeField] Vector3[] ucakGirisleri = new Vector3[0];

        public static readonly List<AtesBolgesi> Hepsi = new List<AtesBolgesi>();

        public event Action<AtesBolgesi> Etkinlesti;
        public event Action<AtesBolgesi> Bitti;

        public string Ad => ad;
        public float Bas => bas;
        public float Son => son;
        public float Onden => onden;
        public Durum Simdiki { get; private set; } = Durum.Bekliyor;
        public GameObject[] YerBirimleri => yerBirimleri;
        public DusmanUcagi[] Ucaklar => ucaklar;
        public Tren Tren => tren;

        /// <summary>All fire sources of the zone (ground units and aircraft).</summary>
        public IEnumerable<Susturulabilir> Kaynaklar()
        {
            foreach (GameObject g in yerBirimleri)
                if (g != null && g.TryGetComponent(out Susturulabilir s)) yield return s;
            foreach (DusmanUcagi u in ucaklar)
                if (u != null && u.TryGetComponent(out Susturulabilir s)) yield return s;
        }

        public int Toplam { get { int n = 0; foreach (var _ in Kaynaklar()) n++; return n; } }
        public int Susturulan { get { int n = 0; foreach (var s in Kaynaklar()) if (s.Sustu) n++; return n; } }

        /// <summary>Short text for the HUD: "1 · Dar Vadi — 2 tank, 1 uçaksavar".</summary>
        public string Duyuru()
        {
            int t = 0, u = 0, d = 0;
            foreach (var s in Kaynaklar())
                switch (s.Tur) { case DusmanTuru.Tank: t++; break; case DusmanTuru.Ucaksavar: u++; break; default: d++; break; }
            var parca = new List<string>();
            if (t > 0) parca.Add($"{t} tank");
            if (u > 0) parca.Add($"{u} uçaksavar");
            if (d > 0) parca.Add($"{d} düşman uçağı");
            return $"{ad} — {string.Join(", ", parca)}";
        }

        void Awake()
        {
            foreach (GameObject g in yerBirimleri)
            {
                if (g == null) continue;
                if (g.TryGetComponent(out Tank tank)) tank.Tren = tren;
                if (g.TryGetComponent(out UcaksavarMevzii aa))
                {
                    aa.Ucak = ucus;
                    if (aa.Goz != null && ucus != null) aa.Goz.Hedef = ucus.transform;
                }
                g.SetActive(false);
            }
            foreach (DusmanUcagi u in ucaklar)
            {
                if (u == null) continue;
                u.Oyuncu = ucus;
                u.Tren = tren;
                u.gameObject.SetActive(false);
            }
        }

        void OnEnable() => Hepsi.Add(this);
        void OnDisable() => Hepsi.Remove(this);

        void Update()
        {
            if (tren == null)
                return;
            if (Simdiki == Durum.Bekliyor && tren.Konum >= bas - onden)
                Etkinlestir();
            else if (Simdiki == Durum.Etkin && (tren.Konum >= son || tren.DurduMu))
                Bitir();
        }

        /// <summary>Shows the zone's enemies and sends its aircraft in (public for tests).</summary>
        public void Etkinlestir()
        {
            if (Simdiki != Durum.Bekliyor)
                return;
            Simdiki = Durum.Etkin;
            foreach (GameObject g in yerBirimleri)
                if (g != null) g.SetActive(true);
            for (int i = 0; i < ucaklar.Length; i++)
                if (ucaklar[i] != null)
                    ucaklar[i].Basla(i < ucakGirisleri.Length ? ucakGirisleri[i] : transform.position + Vector3.up * 400f);
            Etkinlesti?.Invoke(this);
        }

        /// <summary>The train has left the zone: the ground units cease fire, the aircraft withdraw.</summary>
        public void Bitir()
        {
            if (Simdiki != Durum.Etkin)
                return;
            Simdiki = Durum.Bitti;
            foreach (GameObject g in yerBirimleri)
            {
                if (g == null) continue;
                if (g.TryGetComponent(out Tank t)) t.enabled = false;
                if (g.TryGetComponent(out UcaksavarMevzii a)) a.enabled = false;
            }
            foreach (DusmanUcagi u in ucaklar)
                if (u != null) u.Cekil();
            Bitti?.Invoke(this);
        }
    }
}
