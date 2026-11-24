using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 8 (Bölüm 4), worksheet 04: an enemy fighter that attacks the train and the player. Kinematic (no flight physics,
    /// plan §5.4): it flies at a constant speed and turns towards where it wants to go at a limited rate, banking into
    /// the turn. States: waiting (until its fire zone wakes it) → attack (dives at the target, fires short bursts while the
    /// target is inside its cone and range) → break away (climbs out past the target) → attack again; hit (health 0) or
    /// zone over → withdraw (smoke trail when hit, climbs and leaves the map; it never falls). It keeps
    /// <see cref="enAzYerYuksekligi"/> above the ground, looking ahead along its path.
    /// Target: the player when close and ahead of the nose, otherwise the train.
    /// Lab 9 (Bölüm 5) adds a guard mode (<see cref="Koruma"/>, off by default): with nothing to attack it patrols a circle
    /// over the guarded point (<see cref="Durum.Devriye"/>); the player becomes the target as soon as it is within
    /// <c>korumaYaricapi</c> of that point, wherever the nose points.
    /// </summary>
    public class DusmanUcagi : MonoBehaviour
    {
        public enum Durum { Bekliyor, Saldiri, Siyrilma, Cekilme, Gitti, Devriye }

        [SerializeField] UcusKontrol oyuncu;
        [SerializeField] Tren tren;
        [SerializeField] Saglik saglik;
        [SerializeField] Mermi mermiOn;
        [SerializeField] GameObject namluAlevi;
        [Tooltip("Muzzles (children), fired in turn.")]
        [SerializeField] Transform[] namlular = new Transform[0];
        [Tooltip("Trailing smoke, started when hit (inactive child, world simulation space).")]
        [SerializeField] ParticleSystem dumanIzi;
        [Header("Uçuş")]
        [SerializeField, Min(1f)] float hiz = 75f;
        [SerializeField, Min(1f)] float donusHizi = 38f;
        [SerializeField, Min(0f)] float enFazlaYatis = 60f;
        [SerializeField, Min(10f)] float enAzYerYuksekligi = 80f;
        [SerializeField, Min(1f)] float ileriBakis = 3f;
        [Header("Saldırı")]
        [SerializeField, Min(1f)] float menzil = 500f;
        [Tooltip("Fires while the target is inside this half-angle of the nose (degrees).")]
        [SerializeField, Min(0.5f)] float koni = 8f;
        [SerializeField, Min(0.02f)] float atisAraligi = 0.25f;
        [SerializeField, Min(1f)] float mermiHizi = 420f;
        [SerializeField, Min(0)] int hasar = 2;
        [SerializeField, Min(0f)] float sacilma = 1.2f;
        [SerializeField] LayerMask vurulacak;
        [SerializeField] Taraf taraf = Taraf.Dusman;
        [Tooltip("The player is the target when closer than this and ahead of the nose.")]
        [SerializeField, Min(0f)] float oyuncuMesafesi = 700f;
        [Tooltip("Breaks away when closer than this to the target.")]
        [SerializeField, Min(10f)] float siyrilmaMesafesi = 220f;
        [SerializeField, Min(0.5f)] float siyrilmaSuresi = 5f;
        [Tooltip("Withdrawal: leaves (and switches off) beyond this distance from the map centre.")]
        [SerializeField, Min(100f)] float cikisYaricapi = 3400f;
        [SerializeField] int tohum = 1;
        [Header("Koruma (Lab 9)")]
        [SerializeField] bool koruma;
        [SerializeField] Vector3 korumaMerkezi;
        [SerializeField, Min(50f)] float devriyeYaricapi = 800f;
        [Tooltip("Patrol height above the guarded point (m).")]
        [SerializeField, Min(50f)] float devriyeYuksekligi = 350f;
        [Tooltip("The player is attacked anywhere within this horizontal distance of the guarded point (m).")]
        [SerializeField, Min(0f)] float korumaYaricapi = 2400f;
        [Tooltip("Withdrawal heads away from this point (Bölüm 4: the origin).")]
        [SerializeField] Vector3 haritaMerkezi;

        Rigidbody govde;
        System.Random rastgele;
        float siyrilmaBitis, sonrakiAtis, yatis;
        int namluSirasi;
        Vector3 siyrilmaYonu;

        public Durum Simdiki { get; private set; } = Durum.Bekliyor;
        public UcusKontrol Oyuncu { get => oyuncu; set => oyuncu = value; }
        public Tren Tren { get => tren; set => tren = value; }
        public Saglik Saglik => saglik;
        public float Hiz => hiz;
        public float Koni => koni;
        public float Menzil => menzil;
        public float EnAzYerYuksekligi => enAzYerYuksekligi;
        public int AtisSayisi { get; private set; }
        public Transform SonHedef { get; private set; }
        public Vector3 HizVektoru => transform.forward * hiz;
        public bool Aktif => Simdiki == Durum.Saldiri || Simdiki == Durum.Siyrilma || Simdiki == Durum.Devriye;
        public bool Koruma => koruma;
        public Vector3 KorumaMerkezi => korumaMerkezi;
        public float KorumaYaricapi => korumaYaricapi;

        /// <summary>Lab 9: guard mode around <paramref name="merkez"/> (set by <see cref="KorumaUcuslari"/>).</summary>
        public void KorumaAyarla(Vector3 merkez, float devriyeR, float yukseklik, float korumaR, Vector3 harita, float cikis)
        {
            koruma = true;
            korumaMerkezi = merkez;
            devriyeYaricapi = devriyeR;
            devriyeYuksekligi = yukseklik;
            korumaYaricapi = korumaR;
            haritaMerkezi = harita;
            cikisYaricapi = cikis;
        }

        void Awake()
        {
            govde = GetComponent<Rigidbody>();
            rastgele = new System.Random(tohum);
            if (saglik == null) saglik = GetComponent<Saglik>();
        }

        void OnEnable() { if (saglik != null) saglik.Oldu += Vuruldu; }
        void OnDisable() { if (saglik != null) saglik.Oldu -= Vuruldu; }

        /// <summary>Wakes the aircraft at <paramref name="giris"/>, heading for the train (its fire zone started).</summary>
        public void Basla(Vector3 giris)
        {
            gameObject.SetActive(true);
            Vector3 hedef = koruma ? korumaMerkezi : tren != null && tren.Araclar.Length > 0 ? tren.Araclar[0].transform.position : Vector3.zero;
            Vector3 d = hedef - giris;
            d.y = 0f;
            Quaternion q = Quaternion.LookRotation(d.sqrMagnitude > 1f ? d : Vector3.forward, Vector3.up);
            transform.SetPositionAndRotation(giris, q);
            if (govde != null) { govde.position = giris; govde.rotation = q; }
            Simdiki = Durum.Saldiri;
            sonrakiAtis = Time.time + 1f;
        }

        /// <summary>Its zone is over: it leaves without being counted as silenced.</summary>
        public void Cekil()
        {
            if (Simdiki == Durum.Saldiri || Simdiki == Durum.Siyrilma || Simdiki == Durum.Devriye)
                Simdiki = Durum.Cekilme;
        }

        void Vuruldu()
        {
            Simdiki = Durum.Cekilme;
            if (dumanIzi != null)
            {
                dumanIzi.gameObject.SetActive(true);
                dumanIzi.Play(true);
            }
        }

        /// <summary>Current target: the player when close and ahead, otherwise the nearest train vehicle.</summary>
        public Transform HedefSec(out Vector3 hedefHizi)
        {
            hedefHizi = Vector3.zero;
            Vector3 p = transform.position;
            if (koruma)
            {
                if (oyuncu == null || !oyuncu.enabled) return null;
                Vector3 o = oyuncu.transform.position - korumaMerkezi;
                o.y = 0f;
                if (o.sqrMagnitude > korumaYaricapi * korumaYaricapi) return null;
                hedefHizi = oyuncu.HizVektoru;
                return oyuncu.transform;
            }
            if (oyuncu != null && oyuncu.enabled)
            {
                Vector3 d = oyuncu.transform.position - p;
                if (d.sqrMagnitude < oyuncuMesafesi * oyuncuMesafesi && Vector3.Dot(transform.forward, d.normalized) > 0.3f)
                {
                    hedefHizi = oyuncu.HizVektoru;
                    return oyuncu.transform;
                }
            }
            if (tren != null && !tren.DurduMu && Tank.HedefBul(tren, p, out Vector3 h, out hedefHizi))
            {
                float enIyi = float.MaxValue;
                Transform t = null;
                foreach (TrenAraci a in tren.Araclar)
                {
                    float dd = (a.transform.position - h).sqrMagnitude;
                    if (dd < enIyi) { enIyi = dd; t = a.transform; }
                }
                return t;
            }
            return null;
        }

        void FixedUpdate()
        {
            if (Simdiki == Durum.Bekliyor || Simdiki == Durum.Gitti)
                return;
            Adim(Time.fixedDeltaTime);
        }

        /// <summary>One step of the behaviour and the motion (public for tests).</summary>
        public void Adim(float dt)
        {
            Vector3 p = govde != null ? govde.position : transform.position;
            Vector3 ileri = govde != null ? govde.rotation * Vector3.forward : transform.forward;
            Vector3 istenen = ileri;
            Transform hedef = null;
            Vector3 nisan = Vector3.zero;

            switch (Simdiki)
            {
                case Durum.Saldiri:
                    hedef = HedefSec(out Vector3 hh);
                    SonHedef = hedef;
                    if (hedef == null) { Simdiki = koruma ? Durum.Devriye : Durum.Cekilme; break; }
                    nisan = Tank.OncuNokta(p, hedef.position, hh, mermiHizi);
                    istenen = (nisan - p).normalized;
                    if ((hedef.position - p).sqrMagnitude < siyrilmaMesafesi * siyrilmaMesafesi)
                    {
                        Simdiki = Durum.Siyrilma;
                        siyrilmaBitis = Time.time + siyrilmaSuresi;
                        Vector3 yatay = new Vector3(ileri.x, 0f, ileri.z).normalized;
                        float yan = rastgele.NextDouble() < 0.5 ? -1f : 1f;
                        siyrilmaYonu = (Quaternion.Euler(0f, yan * 35f, 0f) * yatay + Vector3.up * 0.45f).normalized;
                    }
                    break;
                case Durum.Siyrilma:
                    istenen = siyrilmaYonu;
                    if (Time.time >= siyrilmaBitis) Simdiki = Durum.Saldiri;
                    break;
                case Durum.Devriye:
                    if (HedefSec(out _) != null) { Simdiki = Durum.Saldiri; break; }
                    istenen = DevriyeYonu(p);
                    break;
                case Durum.Cekilme:
                    Vector3 disari = new Vector3(p.x - haritaMerkezi.x, 0f, p.z - haritaMerkezi.z);
                    disari = disari.sqrMagnitude > 1f ? disari.normalized : new Vector3(ileri.x, 0f, ileri.z).normalized;
                    istenen = (disari + Vector3.up * 0.35f).normalized;
                    if (new Vector2(p.x - haritaMerkezi.x, p.z - haritaMerkezi.z).magnitude > cikisYaricapi)
                    {
                        Simdiki = Durum.Gitti;
                        gameObject.SetActive(false);
                        return;
                    }
                    break;
            }

            // Terrain: never below the floor here or along the path ahead.
            float yerBurada = ZeminOlcer.Yukseklik(p.x, p.z);
            Vector3 onde = p + ileri * (hiz * ileriBakis);
            float yerOnde = Mathf.Max(ZeminOlcer.Yukseklik(onde.x, onde.z), yerBurada);
            if (p.y - yerOnde < enAzYerYuksekligi + 80f)
            {
                float zorunlu = Mathf.Clamp01((enAzYerYuksekligi + 80f - (p.y - yerOnde)) / 80f);
                Vector3 yatay = new Vector3(istenen.x, 0f, istenen.z);
                if (yatay.sqrMagnitude < 1e-4f) yatay = new Vector3(ileri.x, 0f, ileri.z);
                Vector3 yukari = (yatay.normalized + Vector3.up * 0.6f).normalized;
                if (istenen.y < yukari.y)
                    istenen = Vector3.Slerp(istenen, yukari, zorunlu).normalized;
            }

            Vector3 yeniIleri = Vector3.RotateTowards(ileri, istenen, donusHizi * Mathf.Deg2Rad * dt, 0f);
            Vector3 yeniKonum = p + yeniIleri * (hiz * dt);
            yeniKonum.y = Mathf.Max(yeniKonum.y, ZeminOlcer.Yukseklik(yeniKonum.x, yeniKonum.z) + enAzYerYuksekligi);

            // Bank into the turn (visual only): sideways turn rate → roll.
            float donus = Vector3.SignedAngle(new Vector3(ileri.x, 0f, ileri.z), new Vector3(yeniIleri.x, 0f, yeniIleri.z), Vector3.up) / Mathf.Max(dt, 1e-4f);
            yatis = Mathf.Lerp(yatis, Mathf.Clamp(-donus * 2f, -enFazlaYatis, enFazlaYatis), 1f - Mathf.Exp(-3f * dt));
            Quaternion q = Quaternion.LookRotation(yeniIleri, Vector3.up) * Quaternion.Euler(0f, 0f, yatis);
            if (govde != null && govde.isKinematic && Application.isPlaying)
            {
                govde.MovePosition(yeniKonum);
                govde.MoveRotation(q);
            }
            else
                transform.SetPositionAndRotation(yeniKonum, q);

            if (Simdiki == Durum.Saldiri && hedef != null)
                AtesDene(yeniKonum, yeniIleri, nisan);
        }

        /// <summary>Guard patrol: towards a point 30° further round the circle over the guarded point, at patrol height.</summary>
        Vector3 DevriyeYonu(Vector3 p)
        {
            Vector3 r = new Vector3(p.x - korumaMerkezi.x, 0f, p.z - korumaMerkezi.z);
            if (r.sqrMagnitude < 1f) r = Vector3.right;
            Vector3 ileri = Quaternion.Euler(0f, 30f, 0f) * r.normalized * devriyeYaricapi;
            Vector3 hedef = new Vector3(korumaMerkezi.x + ileri.x, korumaMerkezi.y + devriyeYuksekligi, korumaMerkezi.z + ileri.z);
            return (hedef - p).normalized;
        }

        /// <summary>True when the aim point is inside the cone and within range.</summary>
        public static bool AtisMenzilinde(Vector3 konum, Vector3 ileri, Vector3 nisan, float koni, float menzil)
        {
            Vector3 d = nisan - konum;
            return d.sqrMagnitude <= menzil * menzil && Vector3.Angle(ileri, d) <= koni;
        }

        void AtesDene(Vector3 konum, Vector3 ileri, Vector3 nisan)
        {
            if (mermiOn == null || Time.time < sonrakiAtis || !AtisMenzilinde(konum, ileri, nisan, koni, menzil))
                return;
            sonrakiAtis = Time.time + atisAraligi;
            Vector3 cikis = konum + ileri * 6f;
            if (namlular.Length > 0)
            {
                Transform n = namlular[namluSirasi++ % namlular.Length];
                if (n != null) cikis = n.position;
            }
            Vector3 yon = Tank.Sac((nisan - cikis).normalized, sacilma, rastgele);
            Mermi m = Instantiate(mermiOn, cikis, Quaternion.LookRotation(yon));
            m.Ates(yon, mermiHizi + hiz, hasar, vurulacak, true, taraf);
            m.Omur = menzil * 1.4f / (mermiHizi + hiz);
            if (namluAlevi != null)
                Instantiate(namluAlevi, cikis, Quaternion.LookRotation(yon), transform);
            AtisSayisi++;
        }
    }
}
