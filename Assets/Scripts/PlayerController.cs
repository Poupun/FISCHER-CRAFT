using UnityEngine;
//yo
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 1f;
    public float mouseSensitivity = 2f;
    public float jumpForce = 5f;
    
    [Header("Sprint Settings")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintFOVIncrease = 10f;
    public float fovTransitionSpeed = 10f;
    
    [Header("Crouch Settings")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = 1.3f;  // Reduced to 1.3 blocks tall when crouching
    public float standHeight = 1.3f;    // Standard Minecraft player height
    public float crouchTransitionSpeed = 10f;
    
    [Header("Interaction")]
    public float interactionRange = 5f;
    public LayerMask blockLayerMask = -1;
    
    private CharacterController characterController;
    private Camera playerCamera;
    private Vector3 velocity;
    private float xRotation = 0f;
    private PlayerInventory inventory;
    private WorldGenerator worldGenerator;
    
    // Movement state
    private bool isSprinting = false;
    private bool isCrouching = false;
    private float currentSpeed;
    private float baseFOV;
    private float targetHeight;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        inventory = GetComponent<PlayerInventory>();
        worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
        
        // Store initial values
        baseFOV = playerCamera.fieldOfView;
        targetHeight = standHeight;
        currentSpeed = walkSpeed;
        
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovementState();
        HandleMovement();
        HandleInteraction();
        UpdateCrouchHeight();
        UpdateSprintFOV();
    }
    
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    
    void HandleMovementState()
    {
        // Check if we're moving
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
        
        // Handle crouch
        if (Input.GetKeyDown(crouchKey))
        {
            isCrouching = !isCrouching;
            targetHeight = isCrouching ? crouchHeight : standHeight;
        }
        
        // Handle sprint (can't sprint while crouching or not moving)
        if (Input.GetKey(sprintKey) && !isCrouching && isMoving && characterController.isGrounded)
        {
            isSprinting = true;
            currentSpeed = sprintSpeed;
        }
        else if (isCrouching)
        {
            isSprinting = false;
            currentSpeed = crouchSpeed;
        }
        else
        {
            isSprinting = false;
            currentSpeed = walkSpeed;
        }
    }
    
    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Calculate movement direction
        Vector3 direction = transform.right * horizontal + transform.forward * vertical;
        direction = direction.normalized;
        
        // Apply movement with current speed
        Vector3 move = direction * currentSpeed;
        
        // Apply gravity
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Jump (can't jump while crouching)
        if (Input.GetButtonDown("Jump") && characterController.isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * -9.81f);
        }
        
        velocity.y += -9.81f * Time.deltaTime;
        move.y = velocity.y;
        
        characterController.Move(move * Time.deltaTime);
    }
    
    void UpdateCrouchHeight()
    {
        // Smoothly adjust character controller height
        float newHeight = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        characterController.height = newHeight;
        
        // Adjust center position based on height (center should be at half the height)
        Vector3 newCenter = characterController.center;
        newCenter.y = newHeight / 2f;
        characterController.center = newCenter;
        
        // Adjust camera position based on crouch state
        // Camera should be at about 90% of the player height (eye level)
        Vector3 cameraPos = playerCamera.transform.localPosition;
        float targetCameraY = isCrouching ? crouchHeight * 0.9f : standHeight * 0.9f;
        cameraPos.y = Mathf.Lerp(cameraPos.y, targetCameraY, Time.deltaTime * crouchTransitionSpeed);
        playerCamera.transform.localPosition = cameraPos;
    }
    
    void UpdateSprintFOV()
    {
        // Smoothly adjust FOV when sprinting
        float targetFOV = isSprinting ? baseFOV + sprintFOVIncrease : baseFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
    }
    
    void HandleInteraction()
    {
        // Left click - break block
        if (Input.GetMouseButtonDown(0))
        {
            BreakBlock();
        }
        
        // Right click - place block
        if (Input.GetMouseButtonDown(1))
        {
            PlaceBlock();
        }
    }
    
    void BreakBlock()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange, blockLayerMask))
        {
            BlockInfo blockInfo = hit.collider.GetComponent<BlockInfo>();
            if (blockInfo != null)
            {
                if (worldGenerator != null)
                {
                    worldGenerator.PlaceBlock(blockInfo.position, BlockType.Air);
                    
                    // Add block to inventory
                    if (inventory != null)
                    {
                        inventory.AddBlock(blockInfo.blockType);
                    }
                }
            }
        }
    }
    
    void PlaceBlock()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange, blockLayerMask))
        {
            Vector3 hitPoint = hit.point + hit.normal * 0.5f;
            Vector3Int blockPosition = Vector3Int.RoundToInt(hitPoint);
            
            if (inventory != null && worldGenerator != null)
            {
                BlockType blockToPlace = inventory.GetCurrentBlock();
                if (blockToPlace != BlockType.Air && inventory.RemoveBlock(blockToPlace))
                {
                    worldGenerator.PlaceBlock(blockPosition, blockToPlace);
                }
            }
        }
    }
    
    // Public methods for other scripts to check movement state
    public bool IsSprinting() { return isSprinting; }
    public bool IsCrouching() { return isCrouching; }
    public float GetCurrentSpeed() { return currentSpeed; }
}