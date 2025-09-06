using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;
    
    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    public bool invertY = false;
    public float maxLookAngle = 90f;
    
    [Header("Interaction")]
    public float interactionRange = 5f;
    public LayerMask blockLayerMask = -1;
    
    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;
    public float footstepRate = 0.5f;
    
    private CharacterController characterController;
    private Camera playerCamera;
    private CameraShake cameraShake;
    private Vector3 velocity;
    private float verticalRotation = 0f;
    private float footstepTimer;
    
    // References
    private WorldGenerator worldGenerator;
    
    // Input
    private Vector2 mouseInput;
    private Vector2 movementInput;
    private bool isRunning;
    private bool jumpPressed;
    
    void Start()
    {
        // Get components
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            cameraShake = playerCamera.GetComponent<CameraShake>();
        }
    worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
        
        // Setup cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Ensure camera is positioned correctly
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
        }
    }
    
    void Update()
    {
        HandleInput();
        HandleMouseLook();
        HandleMovement();
        HandleInteraction();
        HandleFootsteps();
        
        // ESC to unlock cursor for debugging
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursor();
        }
    }
    
    void HandleInput()
    {
        // Movement input
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        
        // Mouse input
        mouseInput.x = Input.GetAxis("Mouse X");
        mouseInput.y = Input.GetAxis("Mouse Y");
        
        // Other inputs
        isRunning = Input.GetKey(KeyCode.LeftShift);
        jumpPressed = Input.GetButtonDown("Jump");
    }
    
    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        // Horizontal rotation (Y-axis rotation of the player)
        float horizontalRotation = mouseInput.x * mouseSensitivity;
        transform.Rotate(0, horizontalRotation, 0);
        
        // Vertical rotation (X-axis rotation of the camera)
        float verticalInput = mouseInput.y * mouseSensitivity;
        if (invertY) verticalInput = -verticalInput;
        
        verticalRotation -= verticalInput;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }
    
    void HandleMovement()
    {
        // Calculate move direction
        Vector3 moveDirection = Vector3.zero;
        moveDirection += transform.right * movementInput.x;
        moveDirection += transform.forward * movementInput.y;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
        
        // Apply speed
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 move = moveDirection * currentSpeed;
        
        // Handle jumping and gravity
        if (characterController.isGrounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f; // Small negative value to stay grounded
            
            if (jumpPressed)
                velocity.y = jumpForce;
        }
        
        velocity.y += gravity * Time.deltaTime;
        move.y = velocity.y;
        
        // Move the character
        characterController.Move(move * Time.deltaTime);
    }
    
    void HandleInteraction()
    {
        // Mining is now handled by MiningSystem component
        // Block placement is now handled by InteractionManager to prioritize dropped items
        // Right-click interaction disabled here to prevent conflicts
    }
    
    void HandleFootsteps()
    {
        if (!characterController.isGrounded) return;
        if (movementInput.magnitude < 0.1f) return;
        if (footstepSource == null || footstepClips.Length == 0) return;
        
        footstepTimer += Time.deltaTime;
        
        float currentFootstepRate = isRunning ? footstepRate * 0.7f : footstepRate;
        
        if (footstepTimer >= currentFootstepRate)
        {
            footstepTimer = 0f;
            
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            footstepSource.PlayOneShot(clip, 0.5f);
        }
    }
    
    void ToggleCursor()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    // Public methods for external access
    public bool IsGrounded => characterController.isGrounded;
    public bool IsMoving => movementInput.magnitude > 0.1f && characterController.isGrounded;
    public bool IsRunning => isRunning && IsMoving;
    public Camera GetCamera() => playerCamera;
    
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            // Draw interaction range
            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange);
        }
    }
}