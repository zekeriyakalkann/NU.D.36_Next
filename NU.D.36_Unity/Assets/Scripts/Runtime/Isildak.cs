using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 7: a searchlight on a tower. While searching, the head sweeps a sector (yaw back and forth, elevation slowly up and down).
    /// When the aircraft is inside the catch cone, within range and not hidden by the terrain, the light holds it: the head
    /// follows it with a limited slew rate. It loses the aircraft after <c>kayipSuresi</c> seconds outside the hold cone or behind
    /// a hill. The visible beam is shortened where it meets the ground. AA guns linked to it fire only while it holds the aircraft
    /// (<see cref="UcaksavarMevzii"/>, night rule). <see cref="Sondur"/> puts it out for good (the tower is destroyed).
    /// Bölüm 3 rules: an aircraft inside a cloud square at the Orta band (<see cref="Bulut"/>) cannot be caught; while the sound
    /// locator (<c>kulak</c>) hears the aircraft, the searching head turns to the heard point instead of its own sector.
    /// </summary>
    public class Isildak : MonoBehaviour
    {
        [SerializeField] Transform hedef;
        [Tooltip("Turning head; its forward axis is the beam.")]
        [SerializeField] Transform kafa;
        [SerializeField] Light isik;
        [Tooltip("Visible beam cone (apex at the lamp, unit length along +z, scaled uniformly to the beam length).")]
        [SerializeField] Transform huzme;
        [SerializeField] Renderer lamba;
        [SerializeField, Min(10f)] float menzil = 1500f;
        [Tooltip("Half-angle (degrees) inside which the beam catches the aircraft.")]
        [SerializeField, Range(0.5f, 20f)] float yakalamaAcisi = 4.5f;
        [Tooltip("Half-angle (degrees) the aircraft must leave for the light to start losing it.")]
        [SerializeField, Range(1f, 40f)] float tutmaAcisi = 9f;
        [SerializeField, Min(0f)] float kayipSuresi = 1.4f;
        [Tooltip("Largest turn rate of the head while holding the aircraft (degrees/s).")]
        [SerializeField, Min(1f)] float takipHizi = 20f;
        [Header("Tarama")]
        [Tooltip("Centre of the swept sector (compass degrees).")]
        [SerializeField] float taramaYonu;
        [SerializeField, Range(0f, 180f)] float taramaGenisligi = 65f;
        [SerializeField, Min(1f)] float taramaPeriyodu = 16f;
        [SerializeField] Vector2 yukselmeAraligi = new Vector2(10f, 38f);
        [SerializeField] float faz;
        [Tooltip("Layers that block the beam (terrain).")]
        [SerializeField] LayerMask engel;
        [Header("Ses dinleyici (Bölüm 3)")]
        [Tooltip("Optional: the sound locator. While searching, the head turns to the square where the aircraft is heard " +
                 "(the paper rule) and sweeps a narrow sector around it.")]
        [SerializeField] SesDinleyici kulak;
        [Tooltip("Half-width (degrees) of the sweep around the heard square.")]
        [SerializeField, Range(0f, 45f)] float duyulanGenislik = 10f;

        float kayip;
        float zaman;
        bool sondu;

        public event Action<Isildak, bool> YakalamaDegisti;

        public Transform Hedef { get => hedef; set => hedef = value; }
        public SesDinleyici Kulak { get => kulak; set => kulak = value; }
        public float TaramaYonu => taramaYonu;
        /// <summary>True while the head is turned to the heard square (not holding the aircraft).</summary>
        public bool SeseDonuk { get; private set; }
        /// <summary>The heard point the head turned to (valid while <see cref="SeseDonuk"/>).</summary>
        public Vector3 DuyulanNokta { get; private set; }
        public Transform Kafa => kafa;
        public float Menzil => menzil;
        public float YakalamaAcisi => yakalamaAcisi;
        public bool Yakaladi { get; private set; }
        public bool Sondu => sondu;
        /// <summary>Length of the visible beam this frame (m): the range, or less where it meets the ground.</summary>
        public float HuzmeBoyu { get; private set; }

        void Start() => HuzmeyiGuncelle();

        void Update() => Adim(Time.deltaTime);

        /// <summary>One update of <paramref name="dt"/> seconds (also used by tests).</summary>
        public void Adim(float dt)
        {
            if (sondu || kafa == null)
                return;
            zaman += dt;
            Vector3 kaynak = kafa.position;
            bool hedefVar = hedef != null && hedef.gameObject.activeInHierarchy;
            Vector3 fark = hedefVar ? hedef.position - kaynak : Vector3.zero;
            float mesafe = fark.magnitude;
            // Bölüm 3 single rule: inside a cloud square at the Orta band the aircraft cannot be caught (and is lost).
            bool gorunur = hedefVar && mesafe <= menzil && !Bulut.Gizler(hedef.position) && !Radar.GorusEngelliMi(kaynak, hedef.position, engel);

            SeseDonuk = false;
            if (Yakaladi)
            {
                if (mesafe > 1f)
                    kafa.rotation = Quaternion.RotateTowards(kafa.rotation, Quaternion.LookRotation(fark / mesafe), takipHizi * dt);
                float aci = mesafe > 1f ? Vector3.Angle(kafa.forward, fark) : 0f;
                kayip = gorunur && aci <= tutmaAcisi ? 0f : kayip + dt;
                if (kayip > kayipSuresi)
                    YakalamaAyarla(false);
            }
            else
            {
                Quaternion hedefYonelim = TaramaYonelimi(zaman);
                if (kulak != null && kulak.Duyuyor)
                {
                    // The locator heard the aircraft: turn to the heard square and sweep a narrow sector around it.
                    Vector3 duyulan = kulak.DuyulanKonum - kaynak;
                    if (duyulan.magnitude <= menzil * 1.2f && duyulan.sqrMagnitude > 1f)
                    {
                        SeseDonuk = true;
                        DuyulanNokta = kulak.DuyulanKonum;
                        float s = Mathf.Sin(2f * Mathf.PI * zaman / 5f + faz);
                        hedefYonelim = Quaternion.AngleAxis(duyulanGenislik * s, Vector3.up) * Quaternion.LookRotation(duyulan);
                    }
                }
                kafa.rotation = Quaternion.RotateTowards(kafa.rotation, hedefYonelim, (SeseDonuk ? 30f : 40f) * dt);
                if (gorunur && mesafe > 1f && Vector3.Angle(kafa.forward, fark) <= yakalamaAcisi)
                    YakalamaAyarla(true);
            }
            HuzmeyiGuncelle();
        }

        /// <summary>Head orientation of the search pattern at time t.</summary>
        public Quaternion TaramaYonelimi(float t)
        {
            float yaw = taramaYonu + taramaGenisligi * Mathf.Sin(2f * Mathf.PI * t / taramaPeriyodu + faz);
            float s = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / (taramaPeriyodu * 0.61f) + faz * 1.7f);
            float yukselme = Mathf.Lerp(yukselmeAraligi.x, yukselmeAraligi.y, s);
            return Quaternion.Euler(-yukselme, yaw, 0f);
        }

        void YakalamaAyarla(bool deger)
        {
            if (Yakaladi == deger)
                return;
            Yakaladi = deger;
            kayip = 0f;
            Debug.Log($"[Isildak] {name}: uçak {(deger ? "YAKALANDI" : "kaybedildi")}");
            YakalamaDegisti?.Invoke(this, deger);
        }

        void HuzmeyiGuncelle()
        {
            if (kafa == null)
                return;
            HuzmeBoyu = menzil;
            if (engel.value != 0 && Physics.Raycast(kafa.position, kafa.forward, out RaycastHit vurus, menzil, engel, QueryTriggerInteraction.Ignore))
                HuzmeBoyu = vurus.distance;
            if (huzme != null)
                huzme.localScale = Vector3.one * HuzmeBoyu;
        }

        /// <summary>Puts the light out permanently (tower destroyed): no beam, no catch.</summary>
        public void Sondur()
        {
            if (sondu)
                return;
            YakalamaAyarla(false);
            sondu = true;
            if (isik != null) isik.enabled = false;
            if (huzme != null) huzme.gameObject.SetActive(false);
            if (lamba != null) lamba.enabled = false;
        }
    }
}
