using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 5: makes the ground-contact probe use <see cref="ZeminOlcer"/> (terrain colliders and hills) instead of the
    /// bare terrain function, so flying into a hill slope is a crash. Lab 4 scenes do not have this component.
    /// </summary>
    [RequireComponent(typeof(InisAlgilayici))]
    public class ZeminBaglayici : MonoBehaviour
    {
        void Awake() => GetComponent<InisAlgilayici>().ZeminYuksekligi = ZeminOlcer.Yukseklik;
    }
}
