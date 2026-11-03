using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 4 HUD: pure display math, shared by the HUD components and the tests. It only converts values the flight
    /// code already owns (UcusKontrol.Hiz, GazOrani, the aircraft transform) into needle angles, knob positions and
    /// compass offsets; it computes no flight data of its own.
    /// </summary>
    public static class HudHesap
    {
        /// <summary>Dial sweep in degrees clockwise from 12 o'clock: minimum value at -135, maximum at +135 (the Blender dial faces).</summary>
        public const float SweepBasi = -135f, SweepSonu = 135f;

        /// <summary>Dial angle (clockwise from 12 o'clock) for a value in 0..maks; clamped to the dial.</summary>
        public static float KadranAcisi(float deger, float maks)
            => Mathf.Lerp(SweepBasi, SweepSonu, maks > 0f ? Mathf.Clamp01(deger / maks) : 0f);

        /// <summary>Unity z rotation (counter-clockwise positive) of a needle that points up at rotation 0.</summary>
        public static float IbreZ(float deger, float maks) => -KadranAcisi(deger, maks);

        /// <summary>Knob position between the 0 % and 100 % ends of the throttle slot.</summary>
        public static float KolY(float gazOrani, float altY, float ustY) => Mathf.Lerp(altY, ustY, Mathf.Clamp01(gazOrani));

        /// <summary>Heading in 0..360 from a world yaw (Unity eulerAngles.y; 0 = world +Z). <paramref name="sifirOfseti"/> shifts what counts as 0.</summary>
        public static float Yon(float yawDerece, float sifirOfseti = 0f) => Mathf.Repeat(yawDerece - sifirOfseti, 360f);

        /// <summary>
        /// uvRect.x for the compass ribbon (Repeat texture, 360 degrees across the width, heading h at u = h / 360)
        /// so that <paramref name="yon"/> sits in the middle of a window <paramref name="pencere"/> (0..1) wide.
        /// </summary>
        public static float PusulaUvX(float yon, float pencere) => Mathf.Repeat(yon / 360f - pencere * 0.5f, 1f);

        public static string SureMetni(float saniye)
        {
            int sn = Mathf.CeilToInt(Mathf.Max(0f, saniye));
            return $"Süre: {sn / 60}:{sn % 60:00}";
        }
    }
}
