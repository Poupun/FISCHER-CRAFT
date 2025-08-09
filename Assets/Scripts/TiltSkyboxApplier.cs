using UnityEngine;

// Apply a tilt-capable 6-sided skybox using the existing Fantasy Skybox textures at runtime.
// Add this to an empty GameObject in the scene; set material or let it clone from current skybox.
public class TiltSkyboxApplier : MonoBehaviour
{
    [Tooltip("Skybox material using 'Skybox/Tilted 6 Sided'. If none, a clone will be created from current skybox.")]
    public Material tiltedSkyboxMaterial;

    [Range(-30f,30f)] public float tiltX = -8f; // negative lowers horizon
    [Range(0f,360f)] public float rotationY = 0f; // keep your yaw control
    [Range(0.2f,2f)] public float exposure = 1f;

    void Awake()
    {
        EnsureMaterial();
        Apply();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EnsureMaterial();
            Apply();
        }
    }
#endif

    void EnsureMaterial()
    {
        if (tiltedSkyboxMaterial == null)
        {
            Shader sh = Shader.Find("Skybox/Tilted 6 Sided");
            if (sh == null)
            {
                Debug.LogWarning("Tilted skybox shader not found. Ensure 'Skybox/Tilted 6 Sided' exists.");
                return;
            }

            if (RenderSettings.skybox != null)
            {
                // Clone and transfer textures
                tiltedSkyboxMaterial = new Material(sh);
                CopyCubemapFrom(RenderSettings.skybox, tiltedSkyboxMaterial);
            }
            else
            {
                tiltedSkyboxMaterial = new Material(sh);
            }
        }
    }

    void Apply()
    {
        if (tiltedSkyboxMaterial == null) return;
        tiltedSkyboxMaterial.SetFloat("_TiltX", tiltX);
        tiltedSkyboxMaterial.SetFloat("_RotationY", rotationY);
        tiltedSkyboxMaterial.SetFloat("_Exposure", exposure);
        RenderSettings.skybox = tiltedSkyboxMaterial;
        DynamicGI.UpdateEnvironment();
    }

    static void CopyCubemapFrom(Material src, Material dst)
    {
        if (src == null || dst == null) return;
        // Fantasy Skybox uses 6-sides; try to map by common names
        TryCopyTex(src, dst, "_FrontTex");
        TryCopyTex(src, dst, "_BackTex");
        TryCopyTex(src, dst, "_LeftTex");
        TryCopyTex(src, dst, "_RightTex");
        TryCopyTex(src, dst, "_UpTex");
        TryCopyTex(src, dst, "_DownTex");
    }

    static void TryCopyTex(Material src, Material dst, string name)
    {
        if (src.HasProperty(name) && dst.HasProperty(name))
        {
            dst.SetTexture(name, src.GetTexture(name));
        }
    }
}
