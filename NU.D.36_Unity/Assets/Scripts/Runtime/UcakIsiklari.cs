using UnityEngine;
using UnityEngine.InputSystem;

namespace NU36
{
    /// <summary>
    /// Lab 7: the aircraft's lights. The landing light (a spot light in the nose, with a faint visible cone) lights the way ahead
    /// and is switched with the L key; the navigation lights (red left wing tip, green right, white tail) stay on and the white
    /// tail light flashes. The key is an InputAction owned by this component, so the shared "Ucus" action map is not changed.
    /// </summary>
    public class UcakIsiklari : MonoBehaviour
    {
        [SerializeField] Light inisFeneri;
        [SerializeField] GameObject fenerHuzmesi;
        [SerializeField] Renderer kuyrukIsigi;
        [SerializeField, Min(0.1f)] float yanipSonmeAraligi = 1.2f;
        [SerializeField] bool baslangictaAcik = true;
        [SerializeField] InputAction anahtar = new InputAction("InisFeneri", InputActionType.Button, "<Keyboard>/l");

        public bool FenerAcik { get; private set; }
        public Light InisFeneri => inisFeneri;

        void Awake() => FeneriAyarla(baslangictaAcik);
        void OnEnable() => anahtar.Enable();
        void OnDisable() => anahtar.Disable();

        void Update()
        {
            if (anahtar.WasPressedThisFrame() && Time.timeScale > 0f)   // ignored while paused
                FeneriAyarla(!FenerAcik);
            if (kuyrukIsigi != null)
                kuyrukIsigi.enabled = Mathf.Repeat(Time.time, yanipSonmeAraligi) < 0.12f;
        }

        public void FeneriAyarla(bool acik)
        {
            FenerAcik = acik;
            if (inisFeneri != null) inisFeneri.enabled = acik;
            if (fenerHuzmesi != null) fenerHuzmesi.SetActive(acik);
        }
    }
}
