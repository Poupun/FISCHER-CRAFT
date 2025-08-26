using UnityEngine;

// Optional helper: attach to any GameObject in a scene to ensure ambient skybox lighting
// is applied at runtime/editor. This helps vegetation/quads receive proper skybox light
// if the scene's Environment Lighting settings are low or misconfigured.
[ExecuteAlways]
public class EnsureAmbientSkybox : MonoBehaviour
{
    [Header("Environment Lighting")]
    public bool setAmbientToSkybox = true;

    [Tooltip("Multiplies the contribution from the skybox to ambient/GI.")]
    [Range(0f, 3f)]
    public float ambientIntensity = 1.0f;

    [Tooltip("Strength of reflection probes / skybox reflections.")]
    [Range(0f, 1f)]
    public float reflectionIntensity = 1.0f;

    [Tooltip("How many times reflection rays bounce between reflection probes.")]
    [Min(1)]
    public int reflectionBounces = 1;

    [Space]
    public bool applyOnAwake = true;

#if UNITY_EDITOR
    [Tooltip("In Editor, continuously reapplies so Lighting window tweaks are reflected.")]
    public bool applyEveryUpdateInEditor = true;
#endif

    private void Awake()
    {
        if (applyOnAwake)
        {
            Apply();
        }
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Apply();
        }
#endif
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && applyEveryUpdateInEditor)
        {
            Apply();
        }
    }
#endif

    [ContextMenu("Apply Environment Settings Now")]
    public void Apply()
    {
        if (setAmbientToSkybox)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        }

        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.reflectionIntensity = reflectionIntensity;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        RenderSettings.reflectionBounces = Mathf.Max(1, reflectionBounces);

        // Ensure changes take effect immediately in Editor and at runtime
        DynamicGI.UpdateEnvironment();
    }
}
