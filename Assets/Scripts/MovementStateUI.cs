using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MovementStateUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI speedText;
    
    [Header("Display Settings")]
    public bool showDebugInfo = true;
    public Color walkColor = Color.white;
    public Color sprintColor = Color.yellow;
    public Color crouchColor = Color.cyan;
    
    private PlayerController playerController;
    private CharacterController characterController;
    
    void Start()
    {
        // Find player controller
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }
        
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            characterController = player.GetComponent<CharacterController>();
        }
        
        // Try to find UI text components if not assigned
        if (stateText == null)
        {
            GameObject stateObj = GameObject.Find("StateText");
            if (stateObj != null)
                stateText = stateObj.GetComponent<TextMeshProUGUI>();
        }
        
        if (speedText == null)
        {
            GameObject speedObj = GameObject.Find("SpeedText");
            if (speedObj != null)
                speedText = speedObj.GetComponent<TextMeshProUGUI>();
        }
    }
    
    void Update()
    {
        if (!showDebugInfo || playerController == null)
        {
            if (stateText != null) stateText.gameObject.SetActive(false);
            if (speedText != null) speedText.gameObject.SetActive(false);
            return;
        }
        
        // Show debug info
        if (stateText != null)
        {
            stateText.gameObject.SetActive(true);
            
            string state = "Walking";
            Color textColor = walkColor;
            
            if (playerController.IsCrouching())
            {
                state = "Crouching";
                textColor = crouchColor;
            }
            else if (playerController.IsSprinting())
            {
                state = "Sprinting";
                textColor = sprintColor;
            }
            
            if (!characterController.isGrounded)
            {
                state += " (Airborne)";
            }
            
            stateText.text = $"State: {state}";
            stateText.color = textColor;
        }
        
        if (speedText != null)
        {
            speedText.gameObject.SetActive(true);
            float currentVelocity = characterController.velocity.magnitude;
            speedText.text = $"Speed: {currentVelocity:F1} m/s";
        }
    }
    
    // Toggle debug info with a key
    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            showDebugInfo = !showDebugInfo;
        }
    }
}