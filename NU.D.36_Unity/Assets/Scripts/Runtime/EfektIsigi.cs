using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 7: drives a point light of an effect. Fire: a flicker around the base intensity (two noise frequencies). Flash: the
    /// intensity falls from the base to zero over <c>sonmeSuresi</c>. Optionally destroys the whole effect after <c>omur</c> seconds.
    /// </summary>
    public class EfektIsigi : MonoBehaviour
    {
        [SerializeField] Light isik;
        [SerializeField, Min(0f)] float temelParlaklik = 8f;
        [Tooltip("0 = steady fire flicker; > 0 = a flash that dies out in this many seconds.")]
        [SerializeField, Min(0f)] float sonmeSuresi;
        [SerializeField, Range(0f, 1f)] float titreme = 0.35f;
        [Tooltip("Destroy this object after this many seconds (0 = never).")]
        [SerializeField, Min(0f)] float omur;

        float yas;
        float tohum;

        void Awake()
        {
            if (isik == null)
                isik = GetComponentInChildren<Light>();
            tohum = Random.value * 100f;
        }

        void Update()
        {
            yas += Time.deltaTime;
            if (omur > 0f && yas >= omur)
            {
                Destroy(gameObject);
                return;
            }
            if (isik == null)
                return;
            float t = Time.time * 9f + tohum;
            float oynama = 1f + titreme * ((Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 1.6f + (Mathf.PerlinNoise(t * 2.7f, 5.1f) - 0.5f) * 0.6f);
            float sonme = sonmeSuresi > 0f ? Mathf.Clamp01(1f - yas / sonmeSuresi) : 1f;
            isik.intensity = temelParlaklik * oynama * sonme * sonme;
            if (sonmeSuresi > 0f && sonme <= 0f)
                isik.enabled = false;
        }
    }
}
