using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Arcade flight (2026-11-11, user request after three labs of hard keyboard flying: "daha esnek ve rahat").
    /// The keys move the aircraft directly — no lift, no angle of attack, no stall:
    ///   • A/D (Yatis) turn the heading at up to <see cref="donusHizi"/>°/s, Q/E (Sapma) add <see cref="sapmaHizi"/>°/s;
    ///     the aircraft banks into the turn by itself (bank = the coordinated-turn bank of that rate, visual only);
    ///   • W/S (Yunuslama, W = nose down as in all labs) change the climb angle at up to <see cref="egimHizi"/>°/s,
    ///     limited to +<see cref="enFazlaTirmanma"/>° / −<see cref="enFazlaDalis"/>°; released, the nose returns to level;
    ///   • the throttle sets the speed between <see cref="enAzHiz"/> and <see cref="enCokHiz"/>; the brake slows to the least;
    ///   • protections: the path stays <see cref="yerPayi"/> m above the ground looking <see cref="ileriBakis"/> s ahead (it
    ///     climbs over a ridge or a canyon wall by itself), never below <see cref="sertTaban"/> m; beyond
    ///     <see cref="sinirYaricapi"/> from the map centre, flying outwards, it turns back.
    /// The heading and climb angle are read from the transform every step, so teleports (tests, respawn) are respected.
    /// </summary>
    [System.Serializable]
    public class ArkadUcus
    {
        [Header("Hız (m/s)")]
        [Min(1f)] public float enAzHiz = 40f;
        [Min(1f)] public float enCokHiz = 95f;
        [Min(0.1f)] public float ivme = 18f;
        [Header("Dönüş (derece/s)")]
        [Min(1f)] public float donusHizi = 60f;
        [Min(0f)] public float sapmaHizi = 25f;
        [Min(1f)] public float egimHizi = 40f;
        [Range(5f, 80f)] public float enFazlaTirmanma = 40f;
        [Range(5f, 80f)] public float enFazlaDalis = 50f;
        [Tooltip("Return of the climb angle to level when W/S are released (1/s).")]
        [Min(0f)] public float duzelme = 1.6f;
        [Tooltip("How fast the turn and climb rates follow the keys (1/s): high = immediate, still without a jerk.")]
        [Min(0.5f)] public float tepki = 7f;
        [Range(0f, 85f)] public float enFazlaYatis = 65f;
        [Header("Korumalar")]
        [Min(0f)] public float yerPayi = 45f;
        [Min(0.5f)] public float ileriBakis = 2.5f;
        [Min(1f)] public float sertTaban = 15f;
        [Min(0f)] public float sinirYaricapi = 2700f;
        public Vector3 haritaMerkezi = Vector3.zero;

        float donusOrani, egimOrani, yatis;

        public float DonusOrani => donusOrani;
        public float Yatis => yatis;
        /// <summary>The ground protection raised the climb angle this step.</summary>
        public bool YerKorumasi { get; private set; }
        /// <summary>The map-edge turn took over this step.</summary>
        public bool SinirDonusu { get; private set; }

        public void Sifirla() { donusOrani = egimOrani = yatis = 0f; }

        public float HedefHiz(float gaz, bool fren) => fren ? enAzHiz : Mathf.Lerp(enAzHiz, enCokHiz, Mathf.Clamp01(gaz));

        /// <summary>
        /// One step. Inputs as in <see cref="UcusKontrol"/> (Yunuslama +1 = W, Yatis +1 = D, Sapma +1 = E). Returns the new
        /// speed; moves and turns <paramref name="t"/>. <paramref name="zemin"/> gives the ground height at (x, z).
        /// </summary>
        public float Adim(Transform t, float hiz, float dt, float yunuslama, float yatisGirdi, float sapma, bool yunuslamaTers,
                          float gaz, bool fren, System.Func<float, float, float> zemin)
        {
            float k = 1f - Mathf.Exp(-tepki * dt);
            float hedef = HedefHiz(gaz, fren);
            hiz = Mathf.MoveTowards(hiz, hedef, ivme * (fren ? 2f : 1f) * dt);

            Vector3 ileri = t.forward;
            float yon = Mathf.Atan2(ileri.x, ileri.z) * Mathf.Rad2Deg;
            float egim = Mathf.Asin(Mathf.Clamp(ileri.y, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 p = t.position;

            // Turn: the keys command a turn rate (no bank to hold, no pull to keep).
            float donusHedef = Mathf.Clamp(yatisGirdi, -1f, 1f) * donusHizi + Mathf.Clamp(sapma, -1f, 1f) * sapmaHizi;
            Vector3 r = p - haritaMerkezi;
            r.y = 0f;
            Vector3 yatayIleri = new Vector3(Mathf.Sin(yon * Mathf.Deg2Rad), 0f, Mathf.Cos(yon * Mathf.Deg2Rad));
            SinirDonusu = false;
            if (sinirYaricapi > 0f && r.magnitude > sinirYaricapi)
            {
                float hata = Vector3.SignedAngle(yatayIleri, -r, Vector3.up);
                if (Mathf.Abs(hata) > 60f) { donusHedef = Mathf.Sign(hata) * donusHizi; SinirDonusu = true; }
            }
            donusOrani = Mathf.Lerp(donusOrani, donusHedef, k);
            yon += donusOrani * dt;

            // Climb: the keys command a climb-angle rate; released, back to level.
            float egimGirdi = Mathf.Clamp(yunuslamaTers ? yunuslama : -yunuslama, -1f, 1f);
            egimOrani = Mathf.Lerp(egimOrani, egimGirdi * egimHizi, k);
            egim += egimOrani * dt;
            if (Mathf.Abs(egimGirdi) < 0.05f)
                egim *= Mathf.Exp(-duzelme * dt);
            egim = Mathf.Clamp(egim, -enFazlaDalis, enFazlaTirmanma);

            // Ground: stay yerPayi above the highest ground over the next ileriBakis seconds.
            YerKorumasi = false;
            if (zemin != null)
            {
                float enAz = -90f;
                for (int i = 0; i <= 5; i++)
                {
                    float d = hiz * ileriBakis * i / 5f;
                    Vector3 q = p + yatayIleri * d;
                    float gerek = zemin(q.x, q.z) + yerPayi;
                    enAz = Mathf.Max(enAz, Mathf.Atan2(gerek - p.y, Mathf.Max(d, hiz * 1.2f)) * Mathf.Rad2Deg);
                }
                enAz = Mathf.Min(enAz, enFazlaTirmanma + 15f);
                if (egim < enAz)
                {
                    egim = Mathf.MoveTowards(egim, enAz, 90f * dt);
                    egimOrani = Mathf.Max(egimOrani, 0f);
                    YerKorumasi = true;
                }
            }

            float bankHedef = Mathf.Clamp(Mathf.Atan(hiz * donusOrani * Mathf.Deg2Rad / 9.81f) * Mathf.Rad2Deg, -enFazlaYatis, enFazlaYatis);
            yatis = Mathf.Lerp(yatis, bankHedef, 1f - Mathf.Exp(-5f * dt));

            Quaternion q0 = Quaternion.Euler(-egim, yon, -yatis);
            Vector3 yeni = p + q0 * Vector3.forward * (hiz * dt);
            if (zemin != null)
                yeni.y = Mathf.Max(yeni.y, zemin(yeni.x, yeni.z) + sertTaban);
            t.SetPositionAndRotation(yeni, q0);
            return hiz;
        }
    }
}
