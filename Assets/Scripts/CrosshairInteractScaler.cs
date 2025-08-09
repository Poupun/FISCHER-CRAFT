using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scales the crosshair slightly when the player can interact with a block (raycast hit within range & mask).
/// Attach this to your Crosshair root GameObject (the one with a RectTransform) or to the Canvas and
/// leave targetRoot empty to auto-find the crosshair at runtime (supports existing Crosshair/EnhancedCrosshair/MinimalistCrosshair).
/// </summary>
[DefaultExecutionOrder(50)]
public class CrosshairInteractScaler : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("RectTransform to scale. If left empty, the script will try to find a Crosshair object at runtime.")]
    public RectTransform targetRoot;

    [Header("Scale Settings")] 
    [Tooltip("Normal scale of the crosshair.")]
    public float normalScale = 1f;
    [Tooltip("Scale when an interactable block is under the crosshair and in range.")]
    public float interactScale = 1.12f;
    [Tooltip("How fast the scale interpolates.")]
    public float scaleLerpSpeed = 10f;

    [Header("Detection (optional overrides)")]
    [Tooltip("Override interaction range. If <= 0, uses PlayerController.interactionRange.")]
    public float interactionRangeOverride = -1f;
    [Tooltip("Override layer mask for blocks. If value is 0, uses PlayerController.blockLayerMask.")]
    public LayerMask blockLayerMaskOverride = 0;

    private PlayerController playerController;
    private Camera mainCam;
    private float currentScale;

    void Awake()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            var cam = FindObjectOfType<Camera>();
            if (cam != null) mainCam = cam;
        }

        playerController = FindObjectOfType<PlayerController>();
        currentScale = normalScale;
    }

    void Start()
    {
        // Try to auto-resolve crosshair target if not assigned
        if (targetRoot == null)
        {
            targetRoot = AutoFindCrosshairRoot();
        }

        // Ensure we start at normal scale
        if (targetRoot != null)
        {
            targetRoot.localScale = Vector3.one * normalScale;
        }
    }

    void Update()
    {
        if (targetRoot == null || mainCam == null)
            return;

        bool canInteract = CheckCanInteract();
        float targetScale = canInteract ? interactScale : normalScale;

        currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleLerpSpeed);
        targetRoot.localScale = Vector3.one * currentScale;
    }

    private bool CheckCanInteract()
    {
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        float range = interactionRangeOverride > 0 ? interactionRangeOverride : (playerController != null ? playerController.interactionRange : 5f);
        LayerMask mask = blockLayerMaskOverride.value != 0 ? blockLayerMaskOverride : (playerController != null ? playerController.blockLayerMask : ~0);

        // Center screen raycast
        Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = mainCam.ScreenPointToRay(center);

        if (Physics.Raycast(ray, out RaycastHit hit, range, mask))
        {
            if (hit.collider != null)
            {
                var block = hit.collider.GetComponent<BlockInfo>();
                return block != null || true; // Return true if we hit something on the mask
            }
        }
        return false;
    }

    private RectTransform AutoFindCrosshairRoot()
    {
        var go = GameObject.Find("Crosshair");
        if (go != null)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) return rt;
        }

        var min = FindObjectOfType<MinimalistCrosshair>();
        if (min != null)
        {
            var rt = min.GetComponent<RectTransform>();
            if (rt != null) return rt;
        }

        var enh = FindObjectOfType<EnhancedCrosshair>();
        if (enh != null)
        {
            var rt = enh.GetComponent<RectTransform>();
            if (rt != null) return rt;
        }

        var allImages = FindObjectsOfType<Image>();
        foreach (var img in allImages)
        {
            var rt = img.rectTransform;
            if (rt != null && Mathf.Approximately(rt.anchorMin.x, 0.5f) && Mathf.Approximately(rt.anchorMax.x, 0.5f)
                && Mathf.Approximately(rt.anchorMin.y, 0.5f) && Mathf.Approximately(rt.anchorMax.y, 0.5f))
            {
                return rt;
            }
        }

        return null;
    }
}
