using UnityEngine;

namespace NU36
{
    /// <summary>
    /// Lab 7: makes the scene's ambient light (Trilight colours) and the skybox reflection take effect when the scene starts.
    /// The scene is built in batch mode without a lighting bake, so the stored ambient probe is empty; DynamicGI recomputes it
    /// from the current RenderSettings.
    /// </summary>
    [ExecuteAlways]
    public class GeceOrtami : MonoBehaviour
    {
        void OnEnable() => DynamicGI.UpdateEnvironment();
    }
}
