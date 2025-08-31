using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("References")]
    public GameObject inventoryPanel;
    public PlayerInventory playerInventory;
    
    [Header("Settings")]
    public KeyCode inventoryKey = KeyCode.E;
    
    private bool isInventoryOpen = false;
    private PauseManager pauseManager;
    private PlayerController playerController;
    
    void Start()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();
            
        if (pauseManager == null)
            pauseManager = FindFirstObjectByType<PauseManager>();
            
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
        
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }
    }
    
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);
        }
        
        if (isInventoryOpen)
        {
            OpenInventory();
        }
        else
        {
            CloseInventory();
        }
    }
    
    public void OpenInventory()
    {
        isInventoryOpen = true;
        
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (playerController != null)
            playerController.enabled = false;
        
        Time.timeScale = 1f;
    }
    
    public void CloseInventory()
    {
        isInventoryOpen = false;
        
        // Clear crafting grid when closing inventory
        var craftingManager = FindFirstObjectByType<CraftingManager>();
        if (craftingManager != null)
        {
            craftingManager.ClearCraftingGrid();
        }
        
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (playerController != null)
            playerController.enabled = true;
    }
    
    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }
}