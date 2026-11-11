using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Easy flight ("Kolay uçuş", user request 2026-11-10): the control law <see cref="UcusKontrol"/> uses in its aerodynamic mode
    /// while <c>kolayUcus</c> is on. The physics (lift, drag, thrust, stall, ground roll) stay the same; only what the keys
    /// command changes:
    ///   A/D — a bank angle up to <c>enFazlaYatis</c> (less when the wing cannot carry the turn at low speed); released, the wings level.
    ///   W/S — a flight path angle (dive / climb) up to <c>enFazlaDalis</c> / <c>enFazlaTirmanma</c>; released, the aircraft flies level.
    ///   Speed protection: below <c>guvenliHizOrani</c> × the stall speed the allowed climb shrinks and turns into a gentle descent,
    ///   so the speed recovers instead of the wing stalling.
    ///   Upside down, the wings are rolled upright first.
    /// External limits (terrain floor, landing sink envelope, bank near the ground) come from <see cref="UcusYardimcisi"/>.
    /// All angles in degrees.
    /// </summary>
    [Serializable]
    public class UcusYardimi
    {
        [Header("Yatış")]
        [Tooltip("Bank angle at full roll key (degrees).")]
        [Range(10f, 80f)] public float enFazlaYatis = 60f;
        [Tooltip("Bank error → roll rate (1/s).")]
        [Min(0.1f)] public float yatisKazanci = 2.5f;
        [Tooltip("Lowest roll rate cap (degrees/s), so the wings still level at low speed.")]
        [Min(1f)] public float enAzYatisHizi = 30f;
        [Tooltip("Share of the wing's highest load the turn may use: limits the bank at low speed.")]
        [Range(0.3f, 1f)] public float donusPayi = 0.85f;

        [Header("Yunuslama: uçuş yolu açısı")]
        [Tooltip("Climb angle at full back stick (degrees).")]
        [Range(1f, 45f)] public float enFazlaTirmanma = 20f;
        [Tooltip("Dive angle at full forward stick (degrees).")]
        [Range(1f, 80f)] public float enFazlaDalis = 45f;
        [Tooltip("Flight path angle error → load factor (1/s).")]
        [Min(0.1f)] public float egimKazanci = 1.3f;
        [Tooltip("Speed (× stall speed) below which the allowed climb shrinks; further below it the aircraft descends.")]
        [Min(1f)] public float guvenliHizOrani = 1.25f;

        [Header("Hız koruması")]
        [Tooltip("In the air, below this multiple of the stall speed the engine adds power by itself (full power " +
                 "0.12 × stall speed lower), whatever the throttle lever says.")]
        [Min(1f)] public float hizKorumaOrani = 1.3f;
        [Tooltip("In the air, the air brake retracts by itself below this multiple of the stall speed.")]
        [Min(1f)] public float frenAltHizOrani = 1.4f;

        [Header("Kalkış")]
        [Tooltip("On the ground with the throttle at 50 % or more, the nose rises by itself above this multiple of the stall speed.")]
        [Min(1f)] public float kalkisHizOrani = 1.35f;

        /// <summary>
        /// Commanded bank (degrees, positive = right wing down) for a roll stick effect −1..1 at airspeed v: up to
        /// <c>enFazlaYatis</c>, the external limit, and the bank whose turn the wing can carry (n = 1/cosφ ≤ donusPayi·n_max).
        /// </summary>
        public float YatisKomutu(float etki, float v, UcakAerodinamigi a, float disSinir = 180f) =>
            Mathf.Clamp(etki, -1f, 1f) * YatisSiniri(v, a, disSinir);

        /// <summary>Largest bank the easy mode allows at airspeed v (degrees).</summary>
        public float YatisSiniri(float v, UcakAerodinamigi a, float disSinir = 180f)
        {
            float qs = UcakAerodinamigi.DinamikBasinc(Mathf.Max(v, 0f)) * a.kanatAlani;
            float nAzami = a.KaldirmaKatsayisi(a.CekisAcisiSiniri) * qs / a.Agirlik * donusPayi;
            float kanat = nAzami > 1f ? Mathf.Acos(1f / nAzami) * Mathf.Rad2Deg : 0f;
            return Mathf.Max(0f, Mathf.Min(enFazlaYatis, Mathf.Min(kanat, disSinir)));
        }

        /// <summary>Roll rate command (degrees/s) that turns the bank towards the commanded one, capped at ±<paramref name="azamiHiz"/>.</summary>
        public float YatisHiziKomutu(float hedefYatis, float yatis, float azamiHiz)
        {
            float sinir = Mathf.Max(azamiHiz, enAzYatisHizi);
            return Mathf.Clamp(yatisKazanci * Mathf.DeltaAngle(yatis, hedefYatis), -sinir, sinir);
        }

        /// <summary>Commanded flight path angle (degrees) for a pitch stick effect (+1 = nose up), limited by the speed protection.</summary>
        public float EgimKomutu(float etki, float v, float tutunmaHizi)
        {
            float e = Mathf.Clamp(etki, -1f, 1f);
            float istenen = e >= 0f ? e * enFazlaTirmanma : e * enFazlaDalis;
            return Mathf.Min(istenen, HizSiniri(v, tutunmaHizi));
        }

        /// <summary>
        /// Highest flight path angle the speed allows (degrees): the full climb from the safe speed + 35 % of the stall speed,
        /// shrinking to 0 at the safe speed, a descent of up to 20° below it (the aircraft trades height for speed).
        /// </summary>
        public float HizSiniri(float v, float tutunmaHizi)
        {
            float vs = Mathf.Max(tutunmaHizi, 1f);
            float oran = (v - guvenliHizOrani * vs) / (0.35f * vs);
            return oran >= 0f ? enFazlaTirmanma * Mathf.Min(oran, 1f) : Mathf.Max(oran * 15f, -20f);
        }

        /// <summary>
        /// Load factor that turns the flight path angle γ towards the commanded one: n = cosγ / cosφ (holds the path in a banked
        /// turn) + (v/g)·k·(γ_cmd − γ). Then dγ/dt ≈ k·cosφ·(γ_cmd − γ): a first-order approach.
        /// </summary>
        public float YukKomutu(float hedefEgim, float egim, float yatis, float v)
        {
            float cosYatis = Mathf.Max(Mathf.Cos(Mathf.Min(Mathf.Abs(yatis), 75f) * Mathf.Deg2Rad), 0.26f);
            float tutma = Mathf.Cos(egim * Mathf.Deg2Rad) / cosYatis;
            return tutma + Mathf.Max(v, 5f) / UcakAerodinamigi.YerCekimi * egimKazanci * Mathf.DeltaAngle(egim, hedefEgim) * Mathf.Deg2Rad;
        }

        /// <summary>Airspeed (m/s) above which the nose rises by itself on the ground.</summary>
        public float KalkisHizi(UcakAerodinamigi a) => kalkisHizOrani * a.TutunmaHizi();

        /// <summary>
        /// Throttle the engine actually runs at in the air: the lever, or more below <c>hizKorumaOrani</c> × the stall speed
        /// (rising to full power 0.12 × the stall speed lower).
        /// </summary>
        public float EtkinGaz(float gaz, float v, float tutunmaHizi)
        {
            float vs = Mathf.Max(tutunmaHizi, 1f);
            float koruma = Mathf.Clamp01((hizKorumaOrani * vs - v) / (0.12f * vs));
            return Mathf.Max(Mathf.Clamp01(gaz), koruma);
        }

        /// <summary>The air brake works at airspeed v (it retracts by itself below <c>frenAltHizOrani</c> × the stall speed).</summary>
        public bool FrenCalisir(float v, float tutunmaHizi) => v >= frenAltHizOrani * tutunmaHizi;
    }
}
