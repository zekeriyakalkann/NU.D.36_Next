using System;
using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 7: aerodynamic data of the aircraft and the pure formulas of the flight model used by
    /// <see cref="UcusKontrol"/> when its aerodynamic mode is on. Defaults: Hurricane Mk I class single-seat fighter
    /// (≈3000 kg, wing 24 m², ≈615 kW at the propeller), rounded for the game; see Documentation/Lab7_Implementation.md.
    ///   Lift      L = q·S·C_L(α),   C_L = C_L0 + C_Lα·α up to the stall angle, then it falls away
    ///   Drag      D = q·S·(C_D0 + k·C_L² + extra drag beyond the stall)
    ///   Thrust    T = throttle · min(static thrust, P / v)   (a propeller delivers roughly constant power)
    ///   q = ½·ρ·v²
    /// </summary>
    [Serializable]
    public class UcakAerodinamigi
    {
        public const float HavaYogunlugu = 1.225f;   // kg/m³, sea level
        public const float YerCekimi = 9.81f;

        [Tooltip("Mass (kg).")]
        [Min(1f)] public float kutle = 3000f;
        [Tooltip("Wing area S (m²).")]
        [Min(0.1f)] public float kanatAlani = 24f;
        [Tooltip("Lift slope C_Lα (per radian).")]
        [Min(0.1f)] public float kaldirmaEgimi = 5f;
        [Tooltip("Lift coefficient at zero angle of attack C_L0 (cambered wing).")]
        public float sifirKaldirma = 0.25f;
        [Tooltip("Stall angle of attack (degrees). Above it the lift falls away and the drag rises.")]
        [Range(5f, 30f)] public float tutunmaAcisi = 16f;
        [Tooltip("Zero-lift drag coefficient C_D0.")]
        [Min(0f)] public float parazitSurukleme = 0.025f;
        [Tooltip("Induced drag factor k = 1/(π·e·AR).")]
        [Min(0f)] public float induklenmisKatsayi = 0.064f;
        [Tooltip("Extra drag coefficient per radian of angle of attack beyond the stall.")]
        [Min(0f)] public float tutunmaSuruklemesi = 1.2f;
        [Tooltip("Side force coefficient per unit sin(sideslip): pushes the flight path back along the nose.")]
        [Min(0f)] public float yanKuvvet = 1f;

        [Tooltip("Engine power delivered at the propeller (W).")]
        [Min(0f)] public float motorGucu = 615000f;
        [Tooltip("Static thrust limit at low speed (N).")]
        [Min(0f)] public float statikItki = 12000f;

        [Tooltip("Speed (m/s) at which the controls reach full effect; below it the authority falls with (v/vRef)².")]
        [Min(1f)] public float kontrolHizi = 60f;
        [Tooltip("Pitch stability (1/s): how quickly the nose returns to the trimmed angle of attack (tailplane).")]
        [Min(0f)] public float yunuslamaKararliligi = 3f;
        [Tooltip("Directional stability (1/s): how quickly the nose turns into the airflow (fin).")]
        [Min(0f)] public float sapmaKararliligi = 2.5f;
        [Tooltip("0 = the aircraft is trimmed to a fixed angle of attack (classic speed stability). 1 = the trim follows the load needed " +
                 "for the current bank and climb angle, so a banked aircraft turns without losing height while the wing can deliver it " +
                 "(a turn assist for keyboard players). The lift is always computed from the real angle of attack.")]
        [Range(0f, 1f)] public float donusYardimi = 1f;
        [Tooltip("Trimmed angle of attack used without the turn assist (degrees).")]
        public float trimAcisi = 2f;

        [Header("Kumanda (klavye → çubuk)")]
        [Tooltip("Stick travel per second while a key is held (1/s): 3 = full deflection after 0.33 s, so a tap is a small input.")]
        [Min(0.1f)] public float cubukYukselme = 3f;
        [Tooltip("Stick travel per second back to the centre when the key is released (1/s).")]
        [Min(0.1f)] public float cubukDonus = 12f;
        [Tooltip("Expo of the stick (0 = linear, 1 = quadratic): finer control around the centre.")]
        [Range(0f, 1f)] public float kumandaUssu = 0.35f;

        [Header("Yunuslama: yük faktörü komutu")]
        [Tooltip("Load factor at full back stick (g): the structural / stick-force limit at high speed.")]
        [Min(1f)] public float enBuyukYuk = 4.5f;
        [Tooltip("Load factor at full forward stick (g).")]
        public float enKucukYuk = -1.5f;
        [Tooltip("Pull is limited to the stall angle minus korumaPayi (the pilot pulls to the buffet). Off: full back stick goes 2° past the stall.")]
        public bool tutunmaKorumasi = true;
        [Tooltip("Margin below the stall angle kept by the protection (degrees).")]
        [Min(0f)] public float korumaPayi = 1f;
        [Tooltip("Short-period response (1/s) at kontrolHizi: how quickly the angle of attack follows the stick; grows with the airspeed.")]
        [Min(0.5f)] public float kisaPeriyot = 5f;

        [Header("Yatış")]
        [Tooltip("Roll helix angle pb/2V at full aileron (WWII fighters ≈ 0.06–0.09): the roll rate grows with the airspeed.")]
        [Min(0.01f)] public float yatisOrani = 0.07f;
        [Tooltip("Wing span b (m).")]
        [Min(1f)] public float kanatAcikligi = 12f;
        [Tooltip("Roll rate cap (degrees/s): aileron stick forces at high speed.")]
        [Min(1f)] public float enFazlaYatisHizi = 100f;
        [Tooltip("Roll mode time constant (s): roll damping of the wing.")]
        [Min(0.02f)] public float yatisZamanSabiti = 0.12f;
        [Tooltip("Below this bank (degrees) and without aileron, the dihedral slowly levels the wings; above it the bank holds (spirally neutral).")]
        [Min(0f)] public float kanatDuzeltmeBandi = 8f;
        [Tooltip("Rate of that levelling (1/s).")]
        [Min(0f)] public float kanatDuzeltme = 0.6f;

        /// <summary>Steady roll rate (degrees/s) at full aileron and airspeed v: p = (pb/2V)·2V/b, capped.</summary>
        public float YatisHiziSiniri(float v) => Mathf.Min(enFazlaYatisHizi, yatisOrani * 2f * Mathf.Max(v, 0f) / kanatAcikligi * Mathf.Rad2Deg);

        /// <summary>Stick deflection after one step: towards the key at cubukYukselme, back towards the centre at cubukDonus.</summary>
        public float Cubuk(float cubuk, float tus, float h)
        {
            bool merkezeDogru = Mathf.Abs(tus) < Mathf.Abs(cubuk) - 1e-4f || tus * cubuk < 0f;
            return Mathf.MoveTowards(cubuk, tus, (merkezeDogru ? cubukDonus : cubukYukselme) * h);
        }

        /// <summary>Control effect of a stick deflection with the expo curve (−1..1).</summary>
        public float Etki(float cubuk) => cubuk * (1f - kumandaUssu + kumandaUssu * Mathf.Abs(cubuk));

        /// <summary>Highest angle of attack (radians) the pull may ask for: below the stall with the protection, past it without.</summary>
        public float CekisAcisiSiniri => TutunmaAcisiRad + (tutunmaKorumasi ? -korumaPayi : 2f) * Mathf.Deg2Rad;
        /// <summary>Lowest angle of attack (radians) the push may ask for (mirror of the negative stall).</summary>
        public float ItisAcisiSiniri => -TutunmaAcisiRad + sifirKaldirma / kaldirmaEgimi + (tutunmaKorumasi ? korumaPayi : -2f) * Mathf.Deg2Rad;

        [Header("Yer")]
        [Tooltip("Rolling friction coefficient on the wheels.")]
        [Min(0f)] public float yuvarlanmaSurtunmesi = 0.03f;
        [Tooltip("Friction coefficient with the brakes on (throttle at idle, or the brake key).")]
        [Min(0f)] public float frenSurtunmesi = 0.45f;
        [Tooltip("Extra drag coefficient of the air brake while the brake key is held in the air (2026-11-10, user request). " +
                 "0.12 ≈ 5 m/s² at 100 m/s, 1.8 m/s² at 60 m/s.")]
        [Min(0f)] public float havaFreni = 0.12f;

        public float Agirlik => kutle * YerCekimi;
        public float TutunmaAcisiRad => tutunmaAcisi * Mathf.Deg2Rad;
        public float EnBuyukKaldirma => sifirKaldirma + kaldirmaEgimi * TutunmaAcisiRad;

        public static float DinamikBasinc(float v) => 0.5f * HavaYogunlugu * v * v;

        /// <summary>Lift coefficient for an angle of attack in radians; linear up to the stall, then falling to 55 % of the peak.</summary>
        public float KaldirmaKatsayisi(float alfa)
        {
            float a = TutunmaAcisiRad;
            float altSinir = -a + sifirKaldirma / kaldirmaEgimi; // negative stall, same distance from the zero-lift angle
            if (alfa > a)
                return EnBuyukKaldirma * Mathf.Max(0.55f, 1f - (alfa - a) / 0.35f);
            if (alfa < altSinir)
            {
                float enKucuk = sifirKaldirma + kaldirmaEgimi * altSinir;
                return enKucuk * Mathf.Max(0.55f, 1f - (altSinir - alfa) / 0.35f);
            }
            return sifirKaldirma + kaldirmaEgimi * alfa;
        }

        /// <summary>Drag coefficient: parasitic + induced (k·C_L²) + extra drag beyond the stall.</summary>
        public float SuruklemeKatsayisi(float cl, float alfa)
        {
            float asim = Mathf.Max(0f, Mathf.Abs(alfa) - TutunmaAcisiRad);
            return parazitSurukleme + induklenmisKatsayi * cl * cl + tutunmaSuruklemesi * asim;
        }

        /// <summary>Propeller thrust (N): constant power P/v, limited to the static thrust at low speed.</summary>
        public float Itki(float gaz, float v) => Mathf.Clamp01(gaz) * Mathf.Min(statikItki, motorGucu / Mathf.Max(v, 1f));

        /// <summary>Angle of attack (radians) whose lift carries <paramref name="yukFaktoru"/> times the weight at speed v.</summary>
        public float GerekenAci(float v, float yukFaktoru)
        {
            float qs = DinamikBasinc(Mathf.Max(v, 1f)) * kanatAlani;
            return (yukFaktoru * Agirlik / qs - sifirKaldirma) / kaldirmaEgimi;
        }

        /// <summary>Lowest speed at which the wing can carry the weight in level flight (C_L = C_Lmax), m/s.</summary>
        public float TutunmaHizi(float yukFaktoru = 1f) =>
            Mathf.Sqrt(2f * yukFaktoru * Agirlik / (HavaYogunlugu * kanatAlani * EnBuyukKaldirma));

        /// <summary>Drag (N) in steady level flight at speed v (lift = weight).</summary>
        public float DuzUcusSuruklemesi(float v)
        {
            float qs = DinamikBasinc(v) * kanatAlani;
            float cl = Agirlik / qs;
            return qs * (parazitSurukleme + induklenmisKatsayi * cl * cl);
        }

        /// <summary>Steady rate of climb (m/s) from the excess power: (T − D)·v / W.</summary>
        public float TirmanmaHizi(float v, float gaz) => (Itki(gaz, v) - DuzUcusSuruklemesi(v)) * v / Agirlik;

        /// <summary>Highest level-flight speed (m/s) at the given throttle (thrust = drag), found by bisection.</summary>
        public float EnYuksekHiz(float gaz)
        {
            float alt = TutunmaHizi(), ust = 400f;
            if (Itki(gaz, alt) < DuzUcusSuruklemesi(alt))
                return 0f;
            for (int i = 0; i < 60; i++)
            {
                float orta = 0.5f * (alt + ust);
                if (Itki(gaz, orta) > DuzUcusSuruklemesi(orta)) alt = orta; else ust = orta;
            }
            return alt;
        }
    }
}
