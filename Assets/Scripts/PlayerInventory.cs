using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public BlockType blockType;
    public int count;
    
    public InventorySlot()
    {
        blockType = BlockType.Air;
        count = 0;
    }
    
    public InventorySlot(BlockType type, int amount)
    {
        blockType = type;
        count = amount;
    }
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int inventorySize = 5;
    public InventorySlot[] inventory;
    public int currentSlot = 0;
    
    private InventoryUI inventoryUI;
    
    void Start()
    {
        inventory = new InventorySlot[inventorySize];
        for (int i = 0; i < inventorySize; i++)
        {
            inventory[i] = new InventorySlot();
        }
        
    // Add some starting blocks (no Grass in inventory)
    inventory[0] = new InventorySlot(BlockType.Dirt, 16);
    inventory[1] = new InventorySlot(BlockType.Stone, 16);
    inventory[2] = new InventorySlot(BlockType.Sand, 16);
    inventory[3] = new InventorySlot(BlockType.Coal, 8);
    inventory[4] = new InventorySlot(BlockType.Air, 0);
        
    inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Exclude);
        if (inventoryUI != null)
        {
            inventoryUI.UpdateInventoryDisplay(inventory, currentSlot);
        }
    }
    
    void Update()
    {
        HandleInventoryInput();
    }
    
    void HandleInventoryInput()
    {
        // Handle number keys 1-5 for slot selection
        for (int i = 1; i <= inventorySize; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                currentSlot = i - 1;
                if (inventoryUI != null)
                {
                    inventoryUI.UpdateInventoryDisplay(inventory, currentSlot);
                }
            }
        }
        
        // Handle scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0)
        {
            currentSlot = (currentSlot + 1) % inventorySize;
            if (inventoryUI != null)
            {
                inventoryUI.UpdateInventoryDisplay(inventory, currentSlot);
            }
        }
        else if (scroll < 0)
        {
            currentSlot = (currentSlot - 1 + inventorySize) % inventorySize;
            if (inventoryUI != null)
            {
                inventoryUI.UpdateInventoryDisplay(inventory, currentSlot);
            }
        }
    }
    
    public BlockType GetCurrentBlock()
    {
        if (currentSlot >= 0 && currentSlot < inventorySize)
        {
            return inventory[currentSlot].blockType;
        }
        return BlockType.Air;
    }
    
    public bool RemoveBlock(BlockType blockType)
    {
        if (currentSlot >= 0 && currentSlot < inventorySize)
        {
            if (inventory[currentSlot].blockType == blockType && inventory[currentSlot].count > 0)
            {
                inventory[currentSlot].count--;
                if (inventory[currentSlot].count <= 0)
                {
                    inventory[currentSlot].blockType = BlockType.Air;
                    inventory[currentSlot].count = 0;
                }
                
                if (inventoryUI != null)
                {
                    inventoryUI.UpdateInventoryDisplay(inventory, currentSlot);
                }
                return true;
            }
        }
        return false;
    }
    
    public void AddBlock(BlockType blockType)
    {
    if (blockType == BlockType.Air) return;
    if (blockType == BlockType.Grass) return; // Do not collect Grass as an item
        
        // First, try to add to existing stacks
        for (int i = 0; i < inventorySize; i++)
        {
            if (inventory[i].blockType == blockType)
            {
                inventory[i].count++;
                if (inventoryUI != null)
                {
                    inventoryUI.UpdateInventoryDisplay(inventory, currentSlot);
                }
                return;
            }
        }
        
        // If no existing stack, find empty slot
        for (int i = 0; i < inventorySize; i++)
        {
            if (inventory[i].blockType == BlockType.Air)
            {
                inventory[i].blockType = blockType;
                inventory[i].count = 1;
                if (inventoryUI != null)
                {
                    inventoryUI.UpdateInventoryDisplay(inventory, currentSlot);
                }
                return;
            }
        }
    }
}