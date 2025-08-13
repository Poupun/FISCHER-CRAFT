using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("References")]
    private CharacterController characterController;
    private PlayerController playerController;
    private Camera playerCamera;
    
    [Header("Head Bobbing Settings")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 15f;
    [SerializeField] private float sprintBobAmount = 0.075f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    
    [Header("Camera Tilt Settings")]
    [SerializeField] private bool enableTilt = true;
    [SerializeField] private float tiltAmount = 5f;
    [SerializeField] private float tiltSpeed = 10f;
    
    [Header("Jump & Landing Settings")]
    [SerializeField] private bool enableJumpEffects = true;
    [SerializeField] private float jumpBobAmount = 0.2f;
    [SerializeField] private float jumpBobSpeed = 10f;
    [SerializeField] private float landingBobAmount = 0.15f;
    [SerializeField] private float landingRecoverySpeed = 5f;
    
    [Header("Camera Sway Settings")]
    [SerializeField] private bool enableMouseSway = true;
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float maxSwayAmount = 0.06f;
    [SerializeField] private float swaySmooth = 4f;
    
    // Private variables
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    private float bobTimer = 0f;
    private float currentTilt = 0f;
    private float targetTilt = 0f;
    private bool wasGrounded = true;
    private float jumpEffectTimer = 0f;
    private float landingEffectAmount = 0f;
    private Vector3 swayPosition;
    private float lastGroundedTime = 0f;
    private float velocityY = 0f;
    private float lastYPosition = 0f;
    
    // Movement state tracking
    private bool isMoving = false;
    private bool isSprinting = false;
    private bool isCrouching = false;
    private float currentSpeed = 0f;
    
    void Start()
    {
        // Get references
        characterController = GetComponentInParent<CharacterController>();
        playerController = GetComponentInParent<PlayerController>();
        playerCamera = GetComponent<Camera>();
        
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        // Store initial camera transform
        initialCameraPosition = transform.localPosition;
        initialCameraRotation = transform.localRotation;
        
        lastGroundedTime = Time.time;
        lastYPosition = transform.position.y;
    }
    
    void Update()
    {
        if (characterController == null) return;
        
        // Update movement state
        UpdateMovementState();
        
        // Calculate current velocity for effects
        velocityY = (transform.position.y - lastYPosition) / Time.deltaTime;
        lastYPosition = transform.position.y;
        
        // Apply all camera effects
        if (enableHeadBob)
            ApplyHeadBob();
            
        if (enableTilt)
            ApplyCameraTilt();
            
        if (enableJumpEffects)
            ApplyJumpAndLandingEffects();
            
        if (enableMouseSway)
            ApplyMouseSway();
    }
    
    void UpdateMovementState()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0, vertical);
        
        // Check if moving
        isMoving = inputDirection.magnitude > 0.1f && characterController.isGrounded;
        
        // Check sprint (you can modify this based on your input system)
        isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift);
        
        // Check crouch (you can modify this based on your input system)
        isCrouching = Input.GetKey(KeyCode.LeftControl);
        
        // Calculate current speed for smooth transitions
        if (characterController.velocity.magnitude > 0.1f)
        {
            currentSpeed = characterController.velocity.magnitude;
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 10f);
        }
    }
    
    void ApplyHeadBob()
    {
        if (!isMoving)
        {
            // Smoothly return to center when not moving
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 3f);
            Vector3 targetPos = initialCameraPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * 6f);
            return;
        }
        
        // Determine bob parameters based on movement state
        float bobSpeed = walkBobSpeed;
        float bobAmount = walkBobAmount;
        
        if (isSprinting)
        {
            bobSpeed = sprintBobSpeed;
            bobAmount = sprintBobAmount;
        }
        else if (isCrouching)
        {
            bobSpeed = crouchBobSpeed;
            bobAmount = crouchBobAmount;
        }
        
        // Apply bob motion
        bobTimer += Time.deltaTime * bobSpeed;
        
        // Create figure-8 motion for more realistic head movement
        float bobOffsetY = Mathf.Sin(bobTimer) * bobAmount;
        float bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;
        
        // Apply the bob
        Vector3 targetPosition = initialCameraPosition + new Vector3(bobOffsetX, bobOffsetY, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * 10f);
    }
    
    void ApplyCameraTilt()
    {
        // Get horizontal input for tilt
        float horizontal = Input.GetAxis("Horizontal");
        
        // Calculate target tilt based on input and movement
        if (Mathf.Abs(horizontal) > 0.1f && characterController.velocity.magnitude > 0.1f)
        {
            targetTilt = -horizontal * tiltAmount; // Negative for natural tilt direction
        }
        else
        {
            targetTilt = 0f;
        }
        
        // Smoothly interpolate to target tilt
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
        
        // Apply tilt as Z rotation
        Vector3 currentRotation = transform.localRotation.eulerAngles;
        currentRotation.z = currentTilt;
        transform.localRotation = Quaternion.Euler(currentRotation);
    }
    
    void ApplyJumpAndLandingEffects()
    {
        bool isGrounded = characterController.isGrounded;
        
        // Detect jump
        if (!isGrounded && wasGrounded && velocityY > 0.1f)
        {
            // Player just jumped
            jumpEffectTimer = 0.3f;
        }
        
        // Detect landing
        if (isGrounded && !wasGrounded)
        {
            // Player just landed
            float fallSpeed = Mathf.Abs(velocityY);
            landingEffectAmount = Mathf.Clamp01(fallSpeed / 20f) * landingBobAmount;
            lastGroundedTime = Time.time;
        }
        
        // Apply jump effect (camera goes up slightly)
        if (jumpEffectTimer > 0)
        {
            jumpEffectTimer -= Time.deltaTime;
            float jumpOffset = Mathf.Sin(jumpEffectTimer * jumpBobSpeed) * jumpBobAmount * (jumpEffectTimer / 0.3f);
            transform.localPosition += Vector3.up * jumpOffset * Time.deltaTime;
        }
        
        // Apply landing effect (camera dips down)
        if (landingEffectAmount > 0)
        {
            landingEffectAmount -= Time.deltaTime * landingRecoverySpeed;
            float landingOffset = -landingEffectAmount;
            transform.localPosition += Vector3.up * landingOffset * Time.deltaTime * 5f;
        }
        
        wasGrounded = isGrounded;
    }
    
    void ApplyMouseSway()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        // Calculate sway based on mouse movement
        Vector3 targetSwayPosition = new Vector3(
            -mouseX * swayAmount,
            -mouseY * swayAmount,
            0
        );
        
        // Clamp sway amount
        targetSwayPosition = Vector3.ClampMagnitude(targetSwayPosition, maxSwayAmount);
        
        // Smoothly interpolate sway
        swayPosition = Vector3.Lerp(swayPosition, targetSwayPosition, Time.deltaTime * swaySmooth);
        
        // Apply sway to camera position
        transform.localPosition += swayPosition;
    }
    
    // Public method to trigger screen shake (useful for explosions, hits, etc.)
    public void TriggerScreenShake(float intensity, float duration)
    {
        StartCoroutine(ScreenShake(intensity, duration));
    }
    
    System.Collections.IEnumerator ScreenShake(float intensity, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(intensity, 0f, elapsed / duration);
            
            transform.localPosition += (Vector3)Random.insideUnitCircle * strength;
            
            yield return null;
        }
    }
    
    // Reset camera to initial position (useful for respawning, teleporting, etc.)
    public void ResetCamera()
    {
        transform.localPosition = initialCameraPosition;
        transform.localRotation = initialCameraRotation;
        bobTimer = 0f;
        currentTilt = 0f;
        targetTilt = 0f;
        jumpEffectTimer = 0f;
        landingEffectAmount = 0f;
        swayPosition = Vector3.zero;
    }
}