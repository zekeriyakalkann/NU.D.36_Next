using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 2, Task 6: banks a visual child toward the side of the target, using the sign of
    /// the cross product between this transform's forward vector and the target direction.
    /// Only the child's local rotation is changed, so heading control on this transform
    /// (<see cref="HedefeGit"/>) does not erase the roll.
    /// </summary>
    public class KanatYatisi : MonoBehaviour
    {
        [SerializeField] Transform hedef;
        [Tooltip("Visual child that receives the roll (the aircraft model).")]
        [SerializeField] Transform gorsel;
        [Tooltip("Bank angle used when the target is to one side, in degrees.")]
        [SerializeField, Range(0f, 90f)] float maksYatisAcisi = 30f;
        [Tooltip("How fast the bank angle changes, in degrees per second.")]
        [SerializeField, Min(0f)] float yatisHizi = 60f;
        [Tooltip("|Cross(forward, direction).y| below this counts as straight ahead (no bank).")]
        [SerializeField, Range(0f, 1f)] float oluBolge = 0.05f;

        public Transform Hedef { get => hedef; set => hedef = value; }
        public Transform Gorsel { get => gorsel; set => gorsel = value; }
        public float MaksYatisAcisi { get => maksYatisAcisi; set => maksYatisAcisi = Mathf.Clamp(value, 0f, 90f); }
        public float YatisHizi { get => yatisHizi; set => yatisHizi = Mathf.Max(0f, value); }

        /// <summary>Current bank angle in degrees; positive means the right wing is down.</summary>
        public float YatisAcisi { get; private set; }

        /// <summary>
        /// Side of the target relative to <paramref name="ileri"/>: +1 right, -1 left, 0 ahead
        /// or behind. In Unity's left-handed, Y-up system Cross(forward, right) points up, so a
        /// positive Y component means the target is on the right (verified by the Lab 2 tests).
        /// </summary>
        public static int YanIsareti(Vector3 ileri, Vector3 hedefYonu, float oluBolge)
        {
            float y = Vector3.Cross(ileri.normalized, hedefYonu.normalized).y;
            if (Mathf.Abs(y) < oluBolge)
                return 0;
            return y > 0f ? 1 : -1;
        }

        void LateUpdate()
        {
            Adim(Time.deltaTime);
        }

        /// <summary>Advances the bank angle by <paramref name="dt"/> seconds.</summary>
        public void Adim(float dt)
        {
            if (hedef == null || gorsel == null)
                return;

            int isaret = YanIsareti(transform.forward, hedef.position - transform.position, oluBolge);
            YatisAcisi = Mathf.MoveTowards(YatisAcisi, isaret * maksYatisAcisi, yatisHizi * dt);
            // A positive rotation about local +Z lifts the right wing, so banking right is negative.
            gorsel.localRotation = Quaternion.Euler(0f, 0f, -YatisAcisi);
        }
    }
}
