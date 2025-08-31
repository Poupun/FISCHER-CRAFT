using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    public Image background;
    public Image icon;
    public TextMeshProUGUI countText;
    
    [Header("Settings")]
    public Color normalColor = new Color(0, 0, 0, 0.5f);
    public Color highlightColor = Color.yellow;
    public int slotIndex;
    
    private PlayerInventory inventory;
    private ItemStack currentStack;
    private Canvas parentCanvas;
    
    void Start()
    {
        // Auto-assign UI components if they're not set
        if (background == null)
        {
            var backgroundChild = transform.Find("Background");
            if (backgroundChild != null)
                background = backgroundChild.GetComponent<Image>();
            else
                background = GetComponent<Image>(); // Fallback to self
        }
        
        if (icon == null)
        {
            var iconChild = transform.Find("Icon");
            if (iconChild != null)
                icon = iconChild.GetComponent<Image>();
        }
        
        if (countText == null)
        {
            var countChild = transform.Find("Count");
            if (countChild != null)
                countText = countChild.GetComponent<TextMeshProUGUI>();
        }
        
        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();
            
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();
            
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshSlot;
            
        RefreshSlot();
    }
    
    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshSlot;
    }
    
    public void SetSlotIndex(int index)
    {
        slotIndex = index;
        RefreshSlot();
    }
    
    void RefreshSlot()
    {
        if (inventory == null) return;
        
        currentStack = inventory.GetSlot(slotIndex);
        
        // For hotbar slots, let HotbarUI handle the visual updates
        // For other slots, handle the updates here using the same logic as HotbarUI
        if (!inventory.IsHotbarSlot(slotIndex))
        {
            // Handle Icon component only - assign block sprites (same as HotbarUI)
            if (icon != null)
            {
                if (currentStack.IsEmpty)
                {
                    icon.enabled = false;
                }
                else
                {
                    icon.enabled = true;
                    icon.sprite = GetSpriteForBlock(currentStack.blockType);
                    icon.color = Color.white;
                    icon.preserveAspect = true;  // Match HotbarUI setting
                }
            }
            
            // Handle count text (same logic as HotbarUI)
            if (countText != null)
            {
                if (currentStack.IsEmpty)
                {
                    countText.text = string.Empty;  // Match HotbarUI
                }
                else
                {
                    countText.text = (currentStack.count > 1 ? currentStack.count.ToString() : string.Empty);  // Match HotbarUI logic
                }
            }
            
            // Background stays with normalColor (no selection highlighting for inventory slots)
            if (background != null)
            {
                background.color = normalColor;
            }
        }
    }
    
    Sprite GetSpriteForBlock(BlockType type)
    {
        if (type == BlockType.Air) return null;
        
        Texture2D tex = null;
        if ((int)type < BlockDatabase.blockTypes.Length)
        {
            tex = BlockDatabase.blockTypes[(int)type].blockTexture;
        }
        
        if (tex == null)
        {
            var worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
            if (worldGenerator != null)
            {
                switch (type)
                {
                    case BlockType.Grass: tex = worldGenerator.grassTexture; break;
                    case BlockType.Dirt: tex = worldGenerator.dirtTexture; break;
                    case BlockType.Stone: tex = worldGenerator.stoneTexture; break;
                    case BlockType.Sand: tex = worldGenerator.sandTexture; break;
                    case BlockType.Coal: tex = worldGenerator.coalTexture; break;
                    case BlockType.Log: tex = worldGenerator.logTexture; break;
                    case BlockType.Leaves: tex = worldGenerator.leavesTexture; break;
                    case BlockType.WoodPlanks: tex = worldGenerator.woodPlanksTexture; break;
                }
            }
        }
        
        if (tex == null) return null;
        
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventory == null) return;
        
        // Handle crafting result slot specially
        if (slotIndex == 40) // Result slot
        {
            HandleResultSlotClick(eventData);
            return;
        }
        
        // Handle regular inventory interactions first (picking up/placing items)
        if (!currentStack.IsEmpty || InventoryCursor.HasItem())
        {
            HandleInventoryClick(eventData);
            return;
        }
        
        // Handle hotbar selection only for empty hotbar slots when cursor is empty
        if (inventory.IsHotbarSlot(slotIndex) && currentStack.IsEmpty && !InventoryCursor.HasItem())
        {
            inventory.SetSelectedIndex(slotIndex);
            return;
        }
    }
    
    void HandleResultSlotClick(PointerEventData eventData)
    {
        var craftingManager = FindFirstObjectByType<CraftingManager>();
        if (craftingManager == null || currentStack.IsEmpty) return;
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Left click: pick up result items to cursor (simple approach for now)
            if (!InventoryCursor.HasItem())
            {
                // For now, let the existing TryCollectResult add to inventory
                // This maintains the current working behavior
                craftingManager.TryCollectResult();
            }
        }
        // Right click not supported on result slot
    }
    
    void HandleInventoryClick(PointerEventData eventData)
    {
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        if (isShiftHeld && eventData.button == PointerEventData.InputButton.Left)
        {
            // Shift + Left Click: Quick transfer to appropriate inventory section
            HandleQuickTransfer();
            return;
        }
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick();
        }
    }
    
    void HandleLeftClick()
    {
        var cursorStack = InventoryCursor.GetCursorStack();
        
        if (!InventoryCursor.HasItem())
        {
            // Pick up entire stack
            if (!currentStack.IsEmpty)
            {
                InventoryCursor.SetCursorStack(currentStack);
                inventory.SetSlot(slotIndex, new ItemStack());
                
                // Trigger pickup animation from this slot's position to mouse
                CursorDisplay.StartPickupAnimation(transform.position);
            }
        }
        else
        {
            // Place entire cursor stack
            if (currentStack.IsEmpty)
            {
                // Empty slot - place all cursor items
                inventory.SetSlot(slotIndex, cursorStack);
                InventoryCursor.Clear();
            }
            else if (currentStack.blockType == cursorStack.blockType)
            {
                // Same type - try to merge stacks
                int spaceAvailable = ItemStack.MaxStackSize - currentStack.count;
                int toAdd = Mathf.Min(spaceAvailable, cursorStack.count);
                
                if (toAdd > 0)
                {
                    inventory.SetSlot(slotIndex, new ItemStack(currentStack.blockType, currentStack.count + toAdd));
                    
                    if (cursorStack.count <= toAdd)
                    {
                        InventoryCursor.Clear();
                    }
                    else
                    {
                        InventoryCursor.SetCursorStack(new ItemStack(cursorStack.blockType, cursorStack.count - toAdd));
                    }
                }
            }
            else
            {
                // Different types - swap stacks
                inventory.SetSlot(slotIndex, cursorStack);
                InventoryCursor.SetCursorStack(currentStack);
            }
        }
    }
    
    void HandleRightClick()
    {
        var cursorStack = InventoryCursor.GetCursorStack();
        
        if (!InventoryCursor.HasItem())
        {
            // Pick up half stack (or split stack)
            if (!currentStack.IsEmpty && currentStack.count > 1)
            {
                int halfCount = Mathf.CeilToInt(currentStack.count / 2f);
                InventoryCursor.SetCursorStack(new ItemStack(currentStack.blockType, halfCount));
                inventory.SetSlot(slotIndex, new ItemStack(currentStack.blockType, currentStack.count - halfCount));
                
                // Trigger pickup animation from this slot's position to mouse
                CursorDisplay.StartPickupAnimation(transform.position);
            }
            else if (!currentStack.IsEmpty)
            {
                // Single item - pick it up
                InventoryCursor.SetCursorStack(currentStack);
                inventory.SetSlot(slotIndex, new ItemStack());
                
                // Trigger pickup animation from this slot's position to mouse
                CursorDisplay.StartPickupAnimation(transform.position);
            }
        }
        else
        {
            // Place single item from cursor
            if (currentStack.IsEmpty)
            {
                // Empty slot - place one item
                inventory.SetSlot(slotIndex, new ItemStack(cursorStack.blockType, 1));
                
                if (cursorStack.count <= 1)
                {
                    InventoryCursor.Clear();
                }
                else
                {
                    InventoryCursor.SetCursorStack(new ItemStack(cursorStack.blockType, cursorStack.count - 1));
                }
            }
            else if (currentStack.blockType == cursorStack.blockType)
            {
                // Same type - add one if space available
                if (currentStack.count < ItemStack.MaxStackSize)
                {
                    inventory.SetSlot(slotIndex, new ItemStack(currentStack.blockType, currentStack.count + 1));
                    
                    if (cursorStack.count <= 1)
                    {
                        InventoryCursor.Clear();
                    }
                    else
                    {
                        InventoryCursor.SetCursorStack(new ItemStack(cursorStack.blockType, cursorStack.count - 1));
                    }
                }
            }
        }
    }
    
    void HandleQuickTransfer()
    {
        if (currentStack.IsEmpty) return;
        
        // Determine target inventory section based on current slot
        if (inventory.IsHotbarSlot(slotIndex))
        {
            // From hotbar -> try main inventory first, then crafting
            if (!TryTransferToMainInventory()) TryTransferToCrafting();
        }
        else if (inventory.IsCraftingSlot(slotIndex))
        {
            // From crafting -> try hotbar first, then main inventory
            if (!TryTransferToHotbar()) TryTransferToMainInventory();
        }
        else
        {
            // From main inventory -> try hotbar first, then crafting
            if (!TryTransferToHotbar()) TryTransferToCrafting();
        }
    }
    
    bool TryTransferToHotbar()
    {
        // Try to merge with existing stacks first
        for (int i = 0; i < inventory.hotbar.Length; i++)
        {
            var hotbarStack = inventory.hotbar[i];
            if (!hotbarStack.IsEmpty && hotbarStack.blockType == currentStack.blockType)
            {
                int spaceAvailable = ItemStack.MaxStackSize - hotbarStack.count;
                if (spaceAvailable > 0)
                {
                    int toTransfer = Mathf.Min(spaceAvailable, currentStack.count);
                    inventory.SetSlot(i, new ItemStack(hotbarStack.blockType, hotbarStack.count + toTransfer));
                    
                    if (currentStack.count <= toTransfer)
                    {
                        inventory.SetSlot(slotIndex, new ItemStack());
                        return true;
                    }
                    else
                    {
                        inventory.SetSlot(slotIndex, new ItemStack(currentStack.blockType, currentStack.count - toTransfer));
                        return true;
                    }
                }
            }
        }
        
        // Try to find empty slot
        for (int i = 0; i < inventory.hotbar.Length; i++)
        {
            if (inventory.hotbar[i].IsEmpty)
            {
                inventory.SetSlot(i, currentStack);
                inventory.SetSlot(slotIndex, new ItemStack());
                return true;
            }
        }
        
        return false;
    }
    
    bool TryTransferToMainInventory()
    {
        // Try to merge with existing stacks first
        for (int i = inventory.hotbar.Length; i < inventory.mainInventorySize; i++)
        {
            var mainStack = inventory.GetSlot(i);
            if (!mainStack.IsEmpty && mainStack.blockType == currentStack.blockType)
            {
                int spaceAvailable = ItemStack.MaxStackSize - mainStack.count;
                if (spaceAvailable > 0)
                {
                    int toTransfer = Mathf.Min(spaceAvailable, currentStack.count);
                    inventory.SetSlot(i, new ItemStack(mainStack.blockType, mainStack.count + toTransfer));
                    
                    if (currentStack.count <= toTransfer)
                    {
                        inventory.SetSlot(slotIndex, new ItemStack());
                        return true;
                    }
                    else
                    {
                        inventory.SetSlot(slotIndex, new ItemStack(currentStack.blockType, currentStack.count - toTransfer));
                        return true;
                    }
                }
            }
        }
        
        // Try to find empty slot
        for (int i = inventory.hotbar.Length; i < inventory.mainInventorySize; i++)
        {
            var mainStack = inventory.GetSlot(i);
            if (mainStack.IsEmpty)
            {
                inventory.SetSlot(i, currentStack);
                inventory.SetSlot(slotIndex, new ItemStack());
                return true;
            }
        }
        
        return false;
    }
    
    bool TryTransferToCrafting()
    {
        // Only transfer to crafting input slots (36-39), not result slot (40)
        for (int i = 36; i < 40; i++)
        {
            var craftingStack = inventory.GetSlot(i);
            if (!craftingStack.IsEmpty && craftingStack.blockType == currentStack.blockType)
            {
                int spaceAvailable = ItemStack.MaxStackSize - craftingStack.count;
                if (spaceAvailable > 0)
                {
                    int toTransfer = Mathf.Min(spaceAvailable, currentStack.count);
                    inventory.SetSlot(i, new ItemStack(craftingStack.blockType, craftingStack.count + toTransfer));
                    
                    if (currentStack.count <= toTransfer)
                    {
                        inventory.SetSlot(slotIndex, new ItemStack());
                        return true;
                    }
                    else
                    {
                        inventory.SetSlot(slotIndex, new ItemStack(currentStack.blockType, currentStack.count - toTransfer));
                        return true;
                    }
                }
            }
        }
        
        // Try to find empty crafting slot
        for (int i = 36; i < 40; i++)
        {
            var craftingStack = inventory.GetSlot(i);
            if (craftingStack.IsEmpty)
            {
                inventory.SetSlot(i, currentStack);
                inventory.SetSlot(slotIndex, new ItemStack());
                return true;
            }
        }
        
        return false;
    }
    
    // Minecraft-style click-based inventory system
    // First click picks up items to cursor, second click places them
}