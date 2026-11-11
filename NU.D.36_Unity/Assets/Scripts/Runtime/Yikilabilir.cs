using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NU36
{
    public enum HedefTuru { Kule, Kopru }

    /// <summary>
    /// Lab 7: a target that collapses (searchlight tower, bridge span). It owns a <see cref="Saglik"/>; when the health reaches
    /// zero it is destroyed once:
    ///   • its target collider is switched off and its lights (searchlight, lamps) go out;
    ///   • every piece in <c>dusenParcalar</c> gets a Box Collider and a Rigidbody and is thrown by the blast, so the span or the
    ///     tower falls under gravity onto the terrain or into the river; after <c>donmaSuresi</c> seconds the pieces are frozen
    ///     where they lie (kinematic) to keep the physics cheap;
    ///   • fires with smoke columns start at <c>yanginNoktalari</c> and keep burning;
    ///   • <see cref="Yikildi"/> is raised.
    /// </summary>
    [RequireComponent(typeof(Saglik))]
    public class Yikilabilir : MonoBehaviour
    {
        [SerializeField] HedefTuru tur;
        [SerializeField] string gorunenAd = "Hedef";
        [SerializeField, Min(0)] int puan = 150;
        [Tooltip("The collider bombs hit and the blast finds (layer Hedef); switched off when destroyed.")]
        [SerializeField] Collider hedefCarpisici;
        [SerializeField] Transform[] dusenParcalar;
        [SerializeField] GameObject[] kapanacaklar;
        [SerializeField] Isildak isildak;
        [SerializeField] GameObject yanginOn;
        [SerializeField] Transform[] yanginNoktalari;
        [Tooltip("Layer of the falling pieces (Yapi: they land on the terrain and the aircraft can hit them).")]
        [SerializeField] int parcaKatmani = -1;
        [Tooltip("Speed (m/s) the blast gives the pieces.")]
        [SerializeField, Min(0f)] float itme = 7f;
        [Tooltip("Where the blast pushes from (local); the pieces fly away from it.")]
        [SerializeField] Vector3 itmeMerkezi;
        [Tooltip("Extra spin given to every piece (local axis × rad/s), e.g. to topple a tower.")]
        [SerializeField] Vector3 donme;
        [SerializeField, Min(0.5f)] float donmaSuresi = 14f;
        [SerializeField, Min(1f)] float parcaYogunlugu = 600f;   // kg per m³ of the piece's box (timber/steel mix)
        [Tooltip("Colliders the falling pieces pass through (the water surface, so a span sinks to the riverbed).")]
        [SerializeField] Collider[] yoksayilanlar;
        [Tooltip("Long targets (the 600 m bridge): only the falling pieces within this horizontal distance (m) of the blast " +
                 "that destroyed it fall (at least the nearest one); 0 = all pieces fall. Without a blast this frame the " +
                 "target's centre counts as the hit point.")]
        [SerializeField, Min(0f)] float cokmeYaricapi;

        Saglik saglik;

        public event Action<Yikilabilir> Yikildi;

        public HedefTuru Tur => tur;
        public string GorunenAd => gorunenAd;
        public int Puan => puan;
        public bool Yikik { get; private set; }
        public Saglik Saglik => saglik != null ? saglik : saglik = GetComponent<Saglik>();
        public Transform[] DusenParcalar => dusenParcalar;
        /// <summary>The pieces that actually fell (all of <see cref="DusenParcalar"/> unless <c>cokmeYaricapi</c> limits them).</summary>
        public Transform[] Dusenler { get; private set; } = new Transform[0];
        public float CokmeYaricapi { get => cokmeYaricapi; set => cokmeYaricapi = Mathf.Max(0f, value); }

        /// <summary>
        /// Which pieces fall for a hit at <paramref name="darbe"/>: all when <paramref name="yaricap"/> is 0, otherwise those within
        /// that horizontal distance, and always at least the nearest one.
        /// </summary>
        public static List<Transform> Secilenler(Transform[] parcalar, Vector3 darbe, float yaricap)
        {
            var secilen = new List<Transform>();
            if (parcalar == null)
                return secilen;
            Transform enYakin = null;
            float enAz = float.MaxValue;
            foreach (Transform p in parcalar)
            {
                if (p == null) continue;
                Vector3 d = p.position - darbe;
                d.y = 0f;
                float m = d.magnitude;
                if (yaricap <= 0f || m <= yaricap)
                    secilen.Add(p);
                if (m < enAz) { enAz = m; enYakin = p; }
            }
            if (secilen.Count == 0 && enYakin != null)
                secilen.Add(enYakin);
            return secilen;
        }
        public Isildak Isildak => isildak;
        /// <summary>Point the HUD marks (the target collider's centre).</summary>
        public Vector3 IsaretNoktasi => hedefCarpisici != null ? hedefCarpisici.bounds.center : transform.position;

        void Awake() => saglik = GetComponent<Saglik>();
        void OnEnable() => Saglik.Oldu += Yik;
        void OnDisable() => Saglik.Oldu -= Yik;

        /// <summary>Destroys the target (also callable directly, e.g. by tests).</summary>
        public void Yik()
        {
            if (Yikik)
                return;
            Yikik = true;
            if (hedefCarpisici != null)
                hedefCarpisici.enabled = false;
            if (isildak != null)
                isildak.Sondur();
            if (kapanacaklar != null)
                foreach (GameObject g in kapanacaklar)
                    if (g != null) g.SetActive(false);

            var rastgele = new System.Random(gorunenAd.GetHashCode());
            Vector3 merkez = transform.TransformPoint(itmeMerkezi);
            // Where it was hit: this frame's blast, else the centre of the target collider.
            bool patlamaVar = Application.isPlaying && Time.time - Patlama.SonZaman < 0.05f;
            Vector3 darbe = patlamaVar ? Patlama.SonNokta : IsaretNoktasi;
            List<Transform> dusecek = Secilenler(dusenParcalar, darbe, cokmeYaricapi);
            Dusenler = dusecek.ToArray();
            if (cokmeYaricapi > 0f)
                merkez = new Vector3(darbe.x, merkez.y, darbe.z);
            foreach (Transform p in dusecek)
                {
                    if (p == null) continue;
                    if (parcaKatmani >= 0)
                        p.gameObject.layer = parcaKatmani;
                    var kutu = p.GetComponent<BoxCollider>();
                    if (kutu == null)
                    {
                        kutu = p.gameObject.AddComponent<BoxCollider>();
                        if (p.GetComponent<MeshFilter>() == null)
                            KutuyuSigdir(kutu, p);   // a group of meshes: fit the box to all of them
                    }
                    kutu.enabled = true;
                    if (yoksayilanlar != null)
                        foreach (Collider c in yoksayilanlar)
                            if (c != null && c != kutu) Physics.IgnoreCollision(kutu, c);
                    var rb = p.GetComponent<Rigidbody>();
                    if (rb == null)
                        rb = p.gameObject.AddComponent<Rigidbody>();
                    Vector3 boyut = Vector3.Scale(kutu.size, p.lossyScale);
                    rb.mass = Mathf.Clamp(boyut.x * boyut.y * boyut.z * parcaYogunlugu, 50f, 60000f);
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.isKinematic = false;
                    Vector3 disari = p.position - merkez;
                    disari = disari.sqrMagnitude > 0.01f ? disari.normalized : Vector3.up;
                    float r() => (float)rastgele.NextDouble();
                    rb.linearVelocity = (disari + Vector3.up * (0.6f + r())) .normalized * itme * (0.5f + r());
                    rb.angularVelocity = new Vector3(r() - 0.5f, r() - 0.5f, r() - 0.5f) * (donme.sqrMagnitude > 0f ? 0.4f : 2f) + transform.TransformDirection(donme);
                }

            // Fires: all fire points, or for a long target only the two nearest the hit.
            if (yanginOn != null && yanginNoktalari != null)
                foreach (Transform n in cokmeYaricapi > 0f ? EnYakinlar(yanginNoktalari, darbe, 2) : yanginNoktalari)
                    if (n != null)
                        Instantiate(yanginOn, n.position, Quaternion.identity, transform);

            Debug.Log($"[Yikilabilir] {gorunenAd} yıkıldı (+{puan})");
            Yikildi?.Invoke(this);
            if (Application.isPlaying && isActiveAndEnabled && dusenParcalar != null && dusenParcalar.Length > 0)
                StartCoroutine(Dondur());
        }

        static IEnumerable<Transform> EnYakinlar(Transform[] noktalar, Vector3 p, int adet)
        {
            var liste = new List<Transform>();
            foreach (Transform n in noktalar)
                if (n != null) liste.Add(n);
            liste.Sort((a, b) => (a.position - p).sqrMagnitude.CompareTo((b.position - p).sqrMagnitude));
            return liste.GetRange(0, Mathf.Min(adet, liste.Count));
        }

        /// <summary>Sizes a box collider on a group transform to the bounds of all meshes below it (in the group's local space).</summary>
        static void KutuyuSigdir(BoxCollider kutu, Transform grup)
        {
            bool ilk = true;
            var b = new Bounds();
            foreach (MeshFilter mf in grup.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                Bounds mb = mf.sharedMesh.bounds;
                Matrix4x4 m = grup.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var kose = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 y = m.MultiplyPoint3x4(kose);
                    if (ilk) { b = new Bounds(y, Vector3.zero); ilk = false; }
                    else b.Encapsulate(y);
                }
            }
            kutu.center = b.center;
            kutu.size = b.size;
        }

        IEnumerator Dondur()
        {
            yield return new WaitForSeconds(donmaSuresi);
            foreach (Transform p in dusenParcalar)
                if (p != null && p.TryGetComponent(out Rigidbody rb))
                    rb.isKinematic = true;
        }
    }
}
