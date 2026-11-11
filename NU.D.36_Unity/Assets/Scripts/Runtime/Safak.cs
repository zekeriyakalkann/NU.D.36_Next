using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Bölüm 3 dawn ("şafak"): the night lighting turns into dawn over the last part of the mission clock
    /// (<see cref="GeceGorevi.SafakOrani"/> from <c>baslangic</c> to 1): the moonlight swings into a low sun in the east and
    /// warms, the ambient light, the fog and the sky brighten, the stars fade and a warm glow rises on the eastern horizon.
    /// Display only; the dawn rule itself (mission over at dawn) is in <see cref="GeceGorevi"/>. Works on a copy of the sky
    /// material, so the asset is never changed.
    /// </summary>
    public class Safak : MonoBehaviour
    {
        [SerializeField] GeceGorevi gorev;
        [SerializeField] Light ay;
        [Tooltip("Share of the night (0..1) at which the sky starts to brighten.")]
        [SerializeField, Range(0f, 0.99f)] float baslangic = 0.72f;
        [Header("Şafak (oran 1)")]
        [SerializeField] Vector3 gunesAcisi = new Vector3(7f, 272f, 0f);   // light travelling west = sun just over the eastern horizon
        [SerializeField] Color gunesRengi = new Color(1f, 0.70f, 0.48f);
        [SerializeField, Min(0f)] float gunesParlakligi = 1.15f;
        [SerializeField] Color gokRengi = new Color(0.46f, 0.44f, 0.50f);
        [SerializeField] Color ufukRengi = new Color(0.46f, 0.36f, 0.32f);
        [SerializeField] Color zeminRengi = new Color(0.14f, 0.12f, 0.10f);
        [SerializeField] Color sisRengi = new Color(0.47f, 0.40f, 0.40f);
        [SerializeField] Color tepeRengi = new Color(0.20f, 0.30f, 0.50f);
        [SerializeField, Min(0f)] float sisYogunlugu = 0.00038f;

        Quaternion ayAcisi;
        Color ayRengi, gok0, ufuk0, zemin0, sis0, tepe0;
        float ayParlakligi, sisYogunlugu0, yildiz0, aySkyParlakligi0;
        Material gok;
        float sonOran = -1f, sonOrtam = -10f;

        public GeceGorevi Gorev { get => gorev; set => gorev = value; }
        /// <summary>0 = full night, 1 = dawn (this frame's blend).</summary>
        public float Aydinlik { get; private set; }

        /// <summary>Dawn blend for a night share <paramref name="oran"/>: 0 before <paramref name="baslangic"/>, smooth to 1 at the end.</summary>
        public static float AydinlikHesapla(float oran, float baslangic) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(baslangic, 1f, oran));

        void Start()
        {
            if (ay != null)
            {
                ayAcisi = ay.transform.rotation;
                ayRengi = ay.color;
                ayParlakligi = ay.intensity;
            }
            gok0 = RenderSettings.ambientSkyColor;
            ufuk0 = RenderSettings.ambientEquatorColor;
            zemin0 = RenderSettings.ambientGroundColor;
            sis0 = RenderSettings.fogColor;
            sisYogunlugu0 = RenderSettings.fogDensity;
            if (RenderSettings.skybox != null)
            {
                gok = new Material(RenderSettings.skybox) { name = RenderSettings.skybox.name + " (şafak)" };
                RenderSettings.skybox = gok;
                tepe0 = gok.HasProperty("_Tepe") ? gok.GetColor("_Tepe") : Color.black;
                yildiz0 = gok.HasProperty("_YildizParlakligi") ? gok.GetFloat("_YildizParlakligi") : 0f;
                aySkyParlakligi0 = gok.HasProperty("_AyParlakligi") ? gok.GetFloat("_AyParlakligi") : 0f;
                if (gok.HasProperty("_SafakYonu"))
                    gok.SetVector("_SafakYonu", -(Quaternion.Euler(gunesAcisi) * Vector3.forward));
            }
        }

        void OnDestroy()
        {
            if (gok != null)
                Destroy(gok);
        }

        void LateUpdate()
        {
            if (gorev == null)
                return;
            float a = AydinlikHesapla(gorev.SafakOrani, baslangic);
            Aydinlik = a;
            if (Mathf.Abs(a - sonOran) < 0.002f && !(a >= 1f && sonOran < 1f))
                return;   // small steps are skipped, but the end of the dawn is always applied
            sonOran = a;
            if (ay != null)
            {
                ay.transform.rotation = Quaternion.Slerp(ayAcisi, Quaternion.Euler(gunesAcisi), a);
                ay.color = Color.Lerp(ayRengi, gunesRengi, a);
                ay.intensity = Mathf.Lerp(ayParlakligi, gunesParlakligi, a);
            }
            RenderSettings.ambientSkyColor = Color.Lerp(gok0, gokRengi, a);
            RenderSettings.ambientEquatorColor = Color.Lerp(ufuk0, ufukRengi, a);
            RenderSettings.ambientGroundColor = Color.Lerp(zemin0, zeminRengi, a);
            RenderSettings.fogColor = Color.Lerp(sis0, sisRengi, a);
            RenderSettings.fogDensity = Mathf.Lerp(sisYogunlugu0, sisYogunlugu, a);
            if (gok != null)
            {
                if (gok.HasProperty("_Tepe")) gok.SetColor("_Tepe", Color.Lerp(tepe0, tepeRengi, a));
                if (gok.HasProperty("_Ufuk")) gok.SetColor("_Ufuk", RenderSettings.fogColor);
                if (gok.HasProperty("_YildizParlakligi")) gok.SetFloat("_YildizParlakligi", Mathf.Lerp(yildiz0, 0f, a * 1.4f));
                if (gok.HasProperty("_AyParlakligi")) gok.SetFloat("_AyParlakligi", Mathf.Lerp(aySkyParlakligi0, aySkyParlakligi0 * 0.25f, a));
                if (gok.HasProperty("_Safak")) gok.SetFloat("_Safak", a);
            }
            // The ambient probe follows the Trilight colours only after an environment update (not every frame).
            if (Time.unscaledTime - sonOrtam > 0.5f || a >= 1f)
            {
                sonOrtam = Time.unscaledTime;
                DynamicGI.UpdateEnvironment();
            }
        }
    }
}
