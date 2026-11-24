using System;
using System.Collections.Generic;
using UnityEngine;
using H = NU36.KartalGecidiHaritasi;

namespace NU36
{
    /// <summary>
    /// Lab 9 (Bölüm 5 · Kartal Geçidi): a tank landing ship (worksheet: "kıyıya yanaşan gemi karaya 3 tank indiriyor";
    /// "vurulan gemi duruyor: kalan tanklarını indiremiyor"). Kinematic, on the sea surface:
    /// <list type="bullet">
    /// <item><b>Yolda</b>: sails to its stop point at the beach, slowing down in the last metres;</item>
    /// <item><b>Yanasti</b>: runs onto the beach (a splash at the bow, it settles bow-up and stops rolling) and lowers its bow
    /// ramp with an eased motion; the ramp angle is found so that its tip rests on the sand
    /// (<see cref="RampaAcisiBul"/>), where sand and water splash;</item>
    /// <item><b>Indiriyor</b>: the tanks drive off one by one (<see cref="CikarmaTanki"/>);</item>
    /// <item><b>Ayriliyor</b>: empty — no longer a target — raises the ramp, backs off and sails back out;</item>
    /// <item><b>Durdu</b>: hit (health 0) while sailing, beaching or unloading: it loses way, burns, lists and settles;
    /// the tanks still aboard stay aboard.</item>
    /// </list>
    /// No crew model anywhere on the ship.
    /// </summary>
    public class CikarmaGemisi : MonoBehaviour
    {
        public enum Durum { Bekliyor, Yolda, Yanasti, Indiriyor, Ayriliyor, Durdu, Gitti }

        /// <summary>Default beached pose: bow-up trim (°) and lift (m).</summary>
        public const float OturmaEgimi = 0.6f, OturmaYuksekligi = 0.1f;

        [SerializeField] Saglik saglik;
        [SerializeField] Susturulabilir susturulabilir;
        [Tooltip("Bow ramp pivot (hinge at the bow, local X rotation).")]
        [SerializeField] Transform rampa;
        [Tooltip("Tip of the ramp (child of the ramp).")]
        [SerializeField] Transform rampaUcu;
        [Tooltip("Tanks on the well deck, front first.")]
        [SerializeField] CikarmaTanki[] tanklar = new CikarmaTanki[0];
        [SerializeField] ParticleSystem kopuk;
        [SerializeField] ParticleSystem yangin;
        [Tooltip("Splash of sand and water (prefab, removes itself): at the bow when it beaches, at the ramp tip when it lands.")]
        [SerializeField] GameObject sicramaOn;
        [Header("Seyir")]
        [SerializeField, Min(0.5f)] float hiz = 7f;
        [Tooltip("Speed near the stop point = min(hiz, 1.2 + this × remaining distance).")]
        [SerializeField, Min(0.01f)] float yanasmaYavaslamasi = 0.08f;
        [SerializeField, Min(0.5f)] float geriHiz = 3f;
        [SerializeField, Min(10f)] float geriMesafe = 140f;
        [SerializeField, Min(1f)] float donmeHizi = 18f;
        [Header("Karaya oturma")]
        [Tooltip("Bow-up trim (°) and lift (m) once beached; time to settle (s).")]
        [SerializeField] float oturmaEgimi = OturmaEgimi, oturmaYuksekligi = OturmaYuksekligi;
        [SerializeField, Min(0.1f)] float oturmaSuresi = 1.5f;
        [Header("Rampa ve indirme")]
        [SerializeField] float rampaKapali = -80f;
        [Tooltip("Largest downward ramp angle (°) the search may return.")]
        [SerializeField, Range(5f, 60f)] float rampaEnFazla = 40f;
        [SerializeField, Min(0.1f)] float rampaSuresi = 4f;
        [Tooltip("Seconds between two tanks leaving the deck.")]
        [SerializeField, Min(0.1f)] float tankAraligi = 5f;
        [Tooltip("Local z of the bow edge on the deck.")]
        [SerializeField] float pruvaZ = 20f;
        [SerializeField] float guverteY = 0.7f;
        [Header("Durma")]
        [SerializeField, Min(0.1f)] float durmaSuresi = 4f;
        [SerializeField, Min(0f)] float batma = 2f, yatma = 9f;
        [SerializeField, Min(1f)] float batmaSuresi = 25f;

        Rigidbody govde;
        Vector3 baslangic, yanasma, kara;
        Vector3 hizVektoru;
        float durumZamani, oturmaZamani = -1f, kalkmaZamani = -1f, sonrakiTank, rampaAci, rampaBaslangic, rampaHedef, dalga, durmaHizi, yonAci;
        int indirilen, karadaki;
        readonly List<CikarmaTanki> yoldakiler = new List<CikarmaTanki>();

        /// <summary>Parking spot for a tank that has left this ship (set by the fleet).</summary>
        public Func<CikarmaTanki, Vector3> ParkYeri;

        public event Action<CikarmaGemisi, CikarmaTanki> TankKarayaCikti;
        public event Action<CikarmaGemisi> Durduruldu;
        /// <summary>All tanks off (or the ship is done): the lane is free again.</summary>
        public event Action<CikarmaGemisi> Bosaldi;

        public Durum Simdiki { get; private set; } = Durum.Bekliyor;
        public Saglik Saglik => saglik;
        public Vector3 HizVektoru => hizVektoru;
        public int Serit { get; private set; } = -1;
        public int TankSayisi => tanklar.Length;
        /// <summary>Tanks that have left the deck (driving or landed).</summary>
        public int Indirilen => indirilen;
        public int KarayaCikan => karadaki;
        /// <summary>Tanks still on the deck.</summary>
        public int GemideKalan => tanklar.Length - indirilen;
        public IReadOnlyList<CikarmaTanki> Tanklar => tanklar;
        public float RampaAcisi => rampaAci;
        public float RampaHedefi => rampaHedef;
        public Vector3 RampaUcu => rampaUcu != null ? rampaUcu.position : transform.TransformPoint(new Vector3(0f, guverteY, pruvaZ + 5f));
        public float KalanMesafe => Vector3.Distance(new Vector3(Konum.x, 0f, Konum.z), new Vector3(yanasma.x, 0f, yanasma.z));
        /// <summary>Still a threat to the beach (sailing in, beached or unloading).</summary>
        public bool Tehdit => Simdiki == Durum.Yolda || Simdiki == Durum.Yanasti || Simdiki == Durum.Indiriyor;
        /// <summary>On the beach: ramp coming down or tanks driving off.</summary>
        public bool KiyidaMi => Simdiki == Durum.Yanasti || Simdiki == Durum.Indiriyor;
        /// <summary>Finished: stopped, unloaded and leaving, or gone (its tanks may still be driving to the beach).</summary>
        public bool Sonuclandi => Simdiki == Durum.Durdu || Simdiki == Durum.Ayriliyor || Simdiki == Durum.Gitti;
        /// <summary>Released tanks that have not reached dry land yet.</summary>
        public int YoldakiTank { get { int n = 0; foreach (var t in yoldakiler) if (t != null && !t.KaradaMi) n++; return n; } }

        void Awake()
        {
            govde = GetComponent<Rigidbody>();
            if (saglik == null) saglik = GetComponent<Saglik>();
            if (susturulabilir == null) susturulabilir = GetComponent<Susturulabilir>();
            rampaAci = rampaKapali;
            RampaAyarla();
        }

        void OnEnable() { if (saglik != null) saglik.Oldu += Vuruldu; }
        void OnDisable() { if (saglik != null) saglik.Oldu -= Vuruldu; }

        /// <summary>
        /// The smallest downward angle (°, local X) at which a ramp hinged at <paramref name="mentese"/>, <paramref name="uzunluk"/>
        /// long, on a hull turned by <paramref name="govdeYonu"/>, touches the ground given by <paramref name="zemin"/>.
        /// </summary>
        public static float RampaAcisiBul(Vector3 mentese, Quaternion govdeYonu, float uzunluk, Func<float, float, float> zemin, float enFazla = 40f)
        {
            for (float a = 0f; a <= enFazla; a += 0.25f)
            {
                Vector3 uc = mentese + govdeYonu * (Quaternion.Euler(a, 0f, 0f) * Vector3.forward) * uzunluk;
                if (uc.y <= zemin(uc.x, uc.z) + 0.05f) return a;
            }
            return enFazla;
        }

        /// <summary>Sends the ship from <paramref name="bas"/> to its stop point; its tanks land at <paramref name="karaNoktasi"/>.</summary>
        public void Gorevlendir(int serit, Vector3 bas, Vector3 yanasmaNoktasi, Vector3 karaNoktasi)
        {
            Serit = serit;
            baslangic = bas;
            yanasma = yanasmaNoktasi;
            kara = karaNoktasi;
            Vector3 d = yanasma - bas;
            d.y = 0f;
            yonAci = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Yerlestir(new Vector3(bas.x, H.DenizSeviyesi, bas.z), Quaternion.Euler(0f, yonAci, 0f), true);
            Simdiki = Durum.Yolda;
            dalga = UnityEngine.Random.value * 10f;
            if (kopuk != null) kopuk.Play(true);
        }

        void Yerlestir(Vector3 p, Quaternion q, bool isinla = false)
        {
            if (!isinla && govde != null && govde.isKinematic && Application.isPlaying && gameObject.activeInHierarchy)
            {
                govde.MovePosition(p);
                govde.MoveRotation(q);
                return;
            }
            transform.SetPositionAndRotation(p, q);
            if (govde != null)
            {
                govde.position = p;
                govde.rotation = q;
            }
        }

        Vector3 Konum => govde != null && Application.isPlaying ? govde.position : transform.position;

        /// <summary>Tests: puts a sailing ship <paramref name="kalan"/> metres before its stop point at once.</summary>
        public void Isinla(float kalan)
        {
            if (Simdiki != Durum.Yolda) return;
            Vector3 d = new Vector3(baslangic.x - yanasma.x, 0f, baslangic.z - yanasma.z).normalized;
            Vector3 p = new Vector3(yanasma.x, H.DenizSeviyesi, yanasma.z) + d * kalan;
            Yerlestir(p, Quaternion.Euler(0f, yonAci, 0f), true);
        }

        void FixedUpdate()
        {
            if (Simdiki != Durum.Bekliyor && Simdiki != Durum.Gitti)
                Adim(Time.fixedDeltaTime);
        }

        static float Yumusak(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        /// <summary>One step of the behaviour and the motion (public for tests).</summary>
        public void Adim(float dt)
        {
            Vector3 p = Konum;
            Vector3 yatay = new Vector3(p.x, 0f, p.z);
            Vector3 ileri = Quaternion.Euler(0f, yonAci, 0f) * Vector3.forward;
            float v = 0f;
            float roll = 0f, batis = 0f;
            dalga += dt;

            switch (Simdiki)
            {
                case Durum.Yolda:
                {
                    float kalan = KalanMesafe;
                    v = Mathf.Min(hiz, 1.2f + yanasmaYavaslamasi * kalan);
                    if (kalan <= v * dt + 0.05f)
                    {
                        yatay = new Vector3(yanasma.x, 0f, yanasma.z);
                        v = 0f;
                        Simdiki = Durum.Yanasti;
                        durumZamani = oturmaZamani = Time.time;
                        rampaBaslangic = rampaAci;
                        if (kopuk != null) kopuk.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                        Sicrat(transform.TransformPoint(new Vector3(0f, 0f, pruvaZ + 2f)), 0f);
                    }
                    else
                        yatay += (new Vector3(yanasma.x, 0f, yanasma.z) - yatay).normalized * (v * dt);
                    break;
                }
                case Durum.Yanasti:
                {
                    // The ramp comes down (eased) until its tip rests on the sand.
                    if (rampa != null)
                        rampaHedef = RampaAcisiBul(rampa.position, transform.rotation, RampaBoyu, H.Yukseklik, rampaEnFazla);
                    float t = (Time.time - durumZamani) / rampaSuresi;
                    rampaAci = Mathf.Lerp(rampaBaslangic, rampaHedef, Yumusak(t));
                    if (t >= 1f)
                    {
                        rampaAci = rampaHedef;
                        Simdiki = Durum.Indiriyor;
                        sonrakiTank = Time.time + 0.8f;
                        RampaAyarla();
                        Sicrat(RampaUcu, 0f);
                    }
                    break;
                }
                case Durum.Indiriyor:
                    if (indirilen < tanklar.Length && Time.time >= sonrakiTank)
                    {
                        TankIndir(tanklar[indirilen]);
                        indirilen++;
                        sonrakiTank = Time.time + tankAraligi;
                    }
                    // Empty once the last tank has rolled off the ramp onto the beach.
                    if (indirilen >= tanklar.Length && YoldakiTank == 0)
                    {
                        Simdiki = Durum.Ayriliyor;
                        durumZamani = Time.time;
                        rampaBaslangic = rampaAci;
                        HedefOlmaktanCik();
                        Bosaldi?.Invoke(this);
                    }
                    break;
                case Durum.Ayriliyor:
                {
                    float t = (Time.time - durumZamani) / rampaSuresi;
                    rampaAci = Mathf.Lerp(rampaBaslangic, rampaKapali, Yumusak(t));
                    if (t < 1f) break;   // ramp up first
                    if (kalkmaZamani < 0f) kalkmaZamani = Time.time;
                    float geri = Vector3.Distance(yatay, new Vector3(yanasma.x, 0f, yanasma.z));
                    if (geri < geriMesafe)
                    {
                        v = -geriHiz;
                        yatay -= ileri * (geriHiz * dt);
                    }
                    else
                    {
                        // Turn round, then sail out the way it came.
                        Vector3 disari = new Vector3(baslangic.x - yatay.x, 0f, baslangic.z - yatay.z);
                        float hedefAci = Mathf.Atan2(disari.x, disari.z) * Mathf.Rad2Deg;
                        yonAci = Mathf.MoveTowardsAngle(yonAci, hedefAci, donmeHizi * dt);
                        ileri = Quaternion.Euler(0f, yonAci, 0f) * Vector3.forward;
                        v = Mathf.Abs(Mathf.DeltaAngle(yonAci, hedefAci)) < 30f ? hiz : 2f;
                        yatay += ileri * (v * dt);
                        if (kopuk != null && !kopuk.isEmitting) kopuk.Play(true);
                        if (disari.magnitude < 20f)
                        {
                            Simdiki = Durum.Gitti;
                            gameObject.SetActive(false);
                            return;
                        }
                    }
                    break;
                }
                case Durum.Durdu:
                {
                    float t = Time.time - durumZamani;
                    v = durmaHizi * Mathf.Clamp01(1f - t / durmaSuresi);
                    yatay += ileri * (v * dt);
                    float k = Mathf.Clamp01(t / batmaSuresi);
                    k = k * (2f - k);
                    roll = yatma * k;
                    batis = batma * k;
                    break;
                }
            }

            // Beached: bow up a little, no more rolling in the swell; afloat again after it backs off.
            float otur = 0f;
            if (oturmaZamani >= 0f)
                otur = kalkmaZamani < 0f ? Yumusak((Time.time - oturmaZamani) / oturmaSuresi) : 1f - Yumusak((Time.time - kalkmaZamani) / oturmaSuresi);
            float yuz = (1f - otur) * (Simdiki == Durum.Durdu ? 0f : 1f);
            hizVektoru = ileri * v;
            float y = H.DenizSeviyesi + yuz * 0.12f * Mathf.Sin(dalga * 0.9f) + otur * oturmaYuksekligi - batis;
            float yalpa = yuz * 0.8f * Mathf.Sin(dalga * 0.6f);
            Yerlestir(new Vector3(yatay.x, y, yatay.z), Quaternion.Euler(-oturmaEgimi * otur, yonAci, roll + yalpa));
            RampaAyarla();
        }

        /// <summary>Length of the ramp plate (from the hinge to the tip child).</summary>
        float RampaBoyu => rampa != null && rampaUcu != null ? rampaUcu.localPosition.magnitude : 5.5f;

        void RampaAyarla()
        {
            if (rampa != null) rampa.localRotation = Quaternion.Euler(rampaAci, 0f, 0f);
        }

        void Sicrat(Vector3 p, float enAz)
        {
            if (sicramaOn != null)
                Instantiate(sicramaOn, new Vector3(p.x, Mathf.Max(p.y, enAz), p.z), Quaternion.identity);
        }

        void TankIndir(CikarmaTanki t)
        {
            if (t == null) return;
            Vector3 pruva = transform.TransformPoint(new Vector3(0f, guverteY, pruvaZ));
            Vector3 uc = RampaUcu;
            uc.y = Mathf.Max(uc.y, ZeminOlcer.Yukseklik(uc.x, uc.z));
            Vector3 park = ParkYeri != null ? ParkYeri(t) : kara + (kara - yanasma).normalized * 120f;
            Vector3 islak = new Vector3(kara.x, ZeminOlcer.Yukseklik(kara.x, kara.z), kara.z);
            Vector3 cikis = Serit >= 0 ? H.KumsalCikisi(Serit, park.z) : kara + (kara - yanasma).normalized * 50f;
            t.KarayaCikti += Karada;
            yoldakiler.Add(t);
            // Deck → bow edge → ramp tip → wet sand (the landing) → beach exit → parking spot (driven over the ground).
            t.IndirmeyeBasla(new[] { pruva, uc, islak, cikis, park }, 2);
        }

        void Karada(CikarmaTanki t)
        {
            t.KarayaCikti -= Karada;
            karadaki++;
            TankKarayaCikti?.Invoke(this, t);
        }

        void HedefOlmaktanCik()
        {
            if (susturulabilir != null) susturulabilir.ListedenCik();
            // Rounds and bombs no longer hit it (the HUD and the assist ignore it too).
            foreach (Transform c in GetComponentsInChildren<Transform>(true))
                if (c.GetComponent<ParticleSystem>() == null)
                    c.gameObject.layer = 0;
        }

        void Vuruldu()
        {
            if (!Tehdit)
                return;
            durmaHizi = Simdiki == Durum.Yolda ? Mathf.Min(hiz, 1.2f + yanasmaYavaslamasi * KalanMesafe) : 0f;
            Simdiki = Durum.Durdu;
            durumZamani = Time.time;
            if (kopuk != null) kopuk.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (yangin != null)
            {
                yangin.gameObject.SetActive(true);
                yangin.Play(true);
            }
            if (susturulabilir != null) susturulabilir.ListedenCik();
            Debug.Log($"[CikarmaGemisi] {name} durduruldu: {indirilen}/{tanklar.Length} tank indirmişti, {GemideKalan} gemide kaldı");
            Durduruldu?.Invoke(this);
            Bosaldi?.Invoke(this);
        }
    }
}
