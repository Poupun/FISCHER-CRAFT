using UnityEngine;

public class MinecraftCamera : MonoBehaviour
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
    
    private CharacterController characterController;
    private Camera playerCamera;
    private Vector3 velocity;
    private float verticalRotation = 0f;
    
    // References that may not exist yet
    private WorldGenerator worldGenerator;
    
    void Start()
    {
        // Get required components
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        // Get optional components (may not exist)
    worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
        
        // Setup cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Ensure camera is positioned correctly
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
        }
        else
        {
            Debug.LogError("No Camera found as child of Player!");
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursor();
        }
        
        HandleMouseLook();
        HandleMovement();
        HandleInteraction();
    }
    
    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked || playerCamera == null) return;
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        if (invertY) mouseY = -mouseY;
        
        // Horizontal rotation (player body)
        transform.Rotate(0, mouseX, 0);
        
        // Vertical rotation (camera)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    void HandleMovement()
    {
        if (characterController == null) return;
        
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetButtonDown("Jump");
        
        // Calculate movement direction
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
        
        // Apply speed
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 move = moveDirection * currentSpeed;
        
        // Handle jumping and gravity
        if (characterController.isGrounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;
            
            if (jumpPressed)
                velocity.y = jumpForce;
        }
        
        velocity.y += gravity * Time.deltaTime;
        move.y = velocity.y;
        
        // Move
        characterController.Move(move * Time.deltaTime);
    }
    
    void HandleInteraction()
    {
        if (playerCamera == null) return;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        // Left click - break block
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hit, interactionRange, blockLayerMask))
            {
                // Try to find BlockInfo component
                BlockInfo blockInfo = hit.collider.GetComponent<BlockInfo>();
                if (blockInfo != null && worldGenerator != null)
                {
                    worldGenerator.PlaceBlock(blockInfo.position, BlockType.Air);
                    
                    // inventory removed: no collection
                }
            }
        }
        
        // Right click - place block
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(ray, out hit, interactionRange, blockLayerMask))
            {
                Vector3 hitPoint = hit.point + hit.normal * 0.5f;
                Vector3Int blockPosition = Vector3Int.RoundToInt(hitPoint);
                
                // Check distance to avoid placing inside player
                if (Vector3.Distance(blockPosition, transform.position) > 1.5f)
                {
                    // inventory removed: placement disabled
                }
            }
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
    
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange);
        }
    }
}