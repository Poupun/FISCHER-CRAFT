using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal, reliable crosshair: a simple centered "+" drawn with two UI Image lines.
/// Auto-creates a Canvas if none exists and avoids duplicate instances across scene loads.
/// </summary>
[DefaultExecutionOrder(-50)]
public class SimpleCrosshair : MonoBehaviour
{
    private static SimpleCrosshair _instance;

    [Header("Style")]
    public Color color = Color.white;
    [Min(1f)] public float lineLength = 24f;
    [Min(1f)] public float thickness = 2f;

    [Header("Placement")]
    public int sortingOrder = 5000; // draw on top of most UI

    [Header("Interaction Scaling")]
    public bool scaleOnInteract = true;
    public float normalScale = 1.1f;
    public float interactScale = 1.4f;
    public float scaleLerpSpeed = 10f;
    [Tooltip("Override interaction range. If <= 0, uses PlayerController.interactionRange.")]
    public float interactionRangeOverride = -1f;
    [Tooltip("Override block layer mask. If 0, uses PlayerController.blockLayerMask; if neither available, requires BlockInfo on hit.")]
    public LayerMask blockLayerMaskOverride = 0;
    [Header("Transition (Bezier)")]
    [Tooltip("Time it takes to go between default and hover states.")]
    [Range(0.01f, 1f)] public float transitionDuration = 0.12f;
    [Tooltip("Bezier-like ease curve from 0 (default) to 1 (hover).")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Canvas _canvas;
    private RectTransform _root;
    private Image _hLine;
    private Image _vLine;
    private float _currentScale;
    private PlayerController _playerController;
    private Camera _mainCam;
    private float _transitionT; // 0 = default, 1 = hover

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("SimpleCrosshair (Auto)");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SimpleCrosshair>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        _mainCam = Camera.main;
            if (_mainCam == null)
            {
                var cam = FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
                if (cam != null) _mainCam = cam;
            }
            _playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        _currentScale = normalScale;

        EnsureCanvas();
        BuildCrosshair();
        TryDisableOldCrosshairs();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Make sure the canvas is still valid after scene changes; keep crosshair visible.
        if (_canvas == null) EnsureCanvas();
        if (_root == null || _hLine == null || _vLine == null) BuildCrosshair();
        if (_mainCam == null) _mainCam = Camera.main;
        if (_playerController == null) _playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
    }

    private void EnsureCanvas()
    {
        // Always use our own dedicated canvas to avoid touching existing UI
        var child = transform.Find("SimpleCrosshairCanvas");
        GameObject canvasGO;
        if (child == null)
        {
            canvasGO = new GameObject("SimpleCrosshairCanvas");
            canvasGO.transform.SetParent(transform, false);
        }
        else
        {
            canvasGO = child.gameObject;
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        canvasGO.layer = uiLayer >= 0 ? uiLayer : 5; // fallback to default UI layer index

        _canvas = canvasGO.GetComponent<Canvas>();
        if (_canvas == null) _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = sortingOrder;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var raycaster = canvasGO.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = canvasGO.AddComponent<GraphicRaycaster>();
    }

    private void BuildCrosshair()
    {
        // Root holder under canvas
        var rootGO = GameObject.Find("SimpleCrosshair");
        if (rootGO == null)
        {
            rootGO = new GameObject("SimpleCrosshair");
            rootGO.transform.SetParent(_canvas.transform, false);
        }

        _root = rootGO.GetComponent<RectTransform>();
        if (_root == null) _root = rootGO.AddComponent<RectTransform>();

        _root.anchorMin = new Vector2(0.5f, 0.5f);
        _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0.5f);
        _root.anchoredPosition = Vector2.zero;
    _root.sizeDelta = Vector2.zero;
    // Keep root at 1: length-only scaling will be applied to lines to avoid changing thickness
    _root.localScale = Vector3.one;

        // Horizontal line
        _hLine = CreateOrGetLine(_root, "H");
        ApplyLineStyle(_hLine, new Vector2(lineLength, thickness));

        // Vertical line
        _vLine = CreateOrGetLine(_root, "V");
        ApplyLineStyle(_vLine, new Vector2(thickness, lineLength));
    }

    private Image CreateOrGetLine(RectTransform parent, string key)
    {
        var name = $"Line_{key}";
        var child = parent.Find(name);
        GameObject go;
        if (child == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = child.gameObject;
        }

        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = color;

        return img;
    }

    private void ApplyLineStyle(Image img, Vector2 size)
    {
        if (img == null) return;
        var rt = img.rectTransform;
        rt.sizeDelta = size;
        img.color = color;
    }

    private void Update()
    {
        // In case settings changed at runtime in inspector
        if (_hLine != null && _vLine != null)
        {
            ApplyLineStyle(_hLine, new Vector2(lineLength, thickness));
            ApplyLineStyle(_vLine, new Vector2(thickness, lineLength));
        }

        // Interaction-based scaling (length only, thickness remains constant)
        if (scaleOnInteract && _root != null)
        {
            bool canInteract = CheckCanInteract();
            // Advance transition parameter toward target state
            float dir = canInteract ? 1f : -1f;
            if (transitionDuration <= 0f) transitionDuration = 0.01f;
            _transitionT = Mathf.Clamp01(_transitionT + (Time.unscaledDeltaTime / transitionDuration) * (canInteract ? 1f : -1f));

            // Bezier-eased interpolation between normal and interact scales
            float eased = transitionCurve != null ? transitionCurve.Evaluate(_transitionT) : _transitionT;
            _currentScale = Mathf.LerpUnclamped(normalScale, interactScale, eased);

            // Apply length-only scaling (keep thickness constant)
            if (_hLine != null)
            {
                var rt = _hLine.rectTransform;
                rt.sizeDelta = new Vector2(lineLength * _currentScale, thickness);
            }
            if (_vLine != null)
            {
                var rt = _vLine.rectTransform;
                rt.sizeDelta = new Vector2(thickness, lineLength * _currentScale);
            }
        }
    }

    private bool CheckCanInteract()
    {
        if (_mainCam == null) _mainCam = Camera.main;
        if (_playerController == null) _playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        if (_mainCam == null) return false;

        float range = interactionRangeOverride > 0 ? interactionRangeOverride : (_playerController != null ? _playerController.interactionRange : 5f);
        bool hasMask = blockLayerMaskOverride.value != 0 || (_playerController != null && _playerController.blockLayerMask.value != 0);
        LayerMask mask = blockLayerMaskOverride.value != 0 ? blockLayerMaskOverride : (_playerController != null ? _playerController.blockLayerMask : ~0);

        // Center screen ray
        Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = _mainCam.ScreenPointToRay(center);

        if (Physics.Raycast(ray, out RaycastHit hit, range, hasMask ? mask : ~0))
        {
            if (!hasMask)
            {
                // If we didn't have a mask, require BlockInfo to qualify as interactable
                return hit.collider != null && hit.collider.GetComponent<BlockInfo>() != null;
            }
            return true;
        }
        return false;
    }

    private void TryDisableOldCrosshairs()
    {
    // Best-effort: find any MonoBehaviours named like legacy crosshairs and disable the components only.
        string[] legacyNames = { "MinimalistCrosshair", "EnhancedCrosshair", "Crosshair" };
    var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in behaviours)
        {
            if (mb == null) continue;
            var t = mb.GetType();
            if (t == typeof(SimpleCrosshair)) continue;
            foreach (var name in legacyNames)
            {
                if (t.Name == name)
                {
            // Disable only the component to avoid deactivating entire canvases
            if (mb.enabled) mb.enabled = false;
                    break;
                }
            }
        }
    }

}
