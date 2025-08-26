using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Simple, reusable pause system.
// - Press Escape to toggle
// - Freezes time and audio
// - Disables common player controllers (FirstPersonController / PlayerController) if found
// - Manages cursor lock/visibility
// - Exposes OnPauseChanged for menus
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Tooltip("Key to toggle pause (fallback if using legacy Input)")]
    public KeyCode toggleKey = KeyCode.Escape;

    public bool IsPaused { get; private set; }

    public event Action<bool> OnPauseChanged;

    // Remember cursor state to restore on resume
    private CursorLockMode _prevLockState;
    private bool _prevCursorVisible;

    // Remember which controllers we disabled so we only re-enable those
    private Behaviour _firstPersonController;
    private bool _fpcPrevEnabled;
    private Behaviour _playerController;
    private bool _pcPrevEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("PauseManager");
        go.AddComponent<PauseManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Toggle on Escape via new Input System or legacy Input
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
            return;
        }
#endif
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused) Resume(); else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        // Freeze world time and audio
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Cursor: unlock and show
        _prevLockState = Cursor.lockState;
        _prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable common controllers if present
        TryCacheControllers();
        if (_firstPersonController != null) { _fpcPrevEnabled = _firstPersonController.enabled; _firstPersonController.enabled = false; }
        if (_playerController != null)     { _pcPrevEnabled  = _playerController.enabled;      _playerController.enabled = false; }

        OnPauseChanged?.Invoke(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        // Restore time and audio
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Restore cursor
        Cursor.lockState = _prevLockState;
        Cursor.visible = _prevCursorVisible;

        // Re-enable controllers that we disabled
        if (_firstPersonController != null) _firstPersonController.enabled = _fpcPrevEnabled;
        if (_playerController != null)     _playerController.enabled = _pcPrevEnabled;

        OnPauseChanged?.Invoke(false);
    }

    private void TryCacheControllers()
    {
        if (_firstPersonController == null)
        {
            var fpc = FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Exclude);
            if (fpc != null) _firstPersonController = fpc as Behaviour;
        }
        if (_playerController == null)
        {
            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
            if (pc != null) _playerController = pc as Behaviour;
        }

        // Fallback: try camera-attached controller behaviours by name (no hard dependency)
        if (_firstPersonController == null || _playerController == null)
        {
            var cam = Camera.main != null ? Camera.main.gameObject : null;
            if (cam != null)
            {
                if (_firstPersonController == null)
                {
                    var c = cam.GetComponent("FirstPersonController") as Behaviour; if (c != null) _firstPersonController = c;
                }
                if (_playerController == null)
                {
                    var c = cam.GetComponent("PlayerController") as Behaviour; if (c != null) _playerController = c;
                }
            }
        }
    }
}
