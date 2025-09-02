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
    
    private UnifiedPlayerInventory inventory;
    private InventoryEntry currentEntry;
    private Canvas parentCanvas;
    
    // Temporary compatibility property for migration  
    private ItemStack currentStack 
    {
        get 
        {
            if (currentEntry.entryType == InventoryEntryType.Block)
                return new ItemStack(currentEntry.blockType, currentEntry.count);
            else
                return new ItemStack(BlockType.Air, 0); // Items show as empty for compatibility
        }
    }
    
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
            inventory = FindFirstObjectByType<UnifiedPlayerInventory>();
            
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
        
        currentEntry = inventory.GetSlot(slotIndex);
        
        // For hotbar slots, let HotbarUI handle the visual updates
        // For other slots, handle the updates here using the same logic as HotbarUI
        if (!inventory.IsHotbarSlot(slotIndex))
        {
            // Handle Icon component - assign sprites for both blocks and items
            if (icon != null)
            {
                if (currentEntry.IsEmpty)
                {
                    icon.enabled = false;
                }
                else
                {
                    icon.enabled = true;
                    icon.sprite = currentEntry.GetSprite(); // Use the unified sprite method
                    icon.color = Color.white;
                    icon.preserveAspect = true;  // Match HotbarUI setting
                }
            }
            
            // Handle count text (same logic as HotbarUI)
            if (countText != null)
            {
                if (currentEntry.IsEmpty)
                {
                    countText.text = string.Empty;  // Match HotbarUI
                }
                else
                {
                    countText.text = (currentEntry.count > 1 ? currentEntry.count.ToString() : string.Empty);  // Match HotbarUI logic
                }
            }
            
            // Background stays with normalColor (no selection highlighting for inventory slots)
            if (background != null)
            {
                background.color = normalColor;
            }
        }
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
        if (!currentEntry.IsEmpty || InventoryCursor.HasItem())
        {
            HandleInventoryClick(eventData);
            return;
        }
        
        // Handle hotbar selection only for empty hotbar slots when cursor is empty
        if (inventory.IsHotbarSlot(slotIndex) && currentEntry.IsEmpty && !InventoryCursor.HasItem())
        {
            inventory.SetSelectedIndex(slotIndex);
            return;
        }
    }
    
    void HandleResultSlotClick(PointerEventData eventData)
    {
        var craftingManager = FindFirstObjectByType<CraftingManager>();
        if (craftingManager == null || currentEntry.IsEmpty) return;
        
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isShiftHeld)
            {
                // Shift + Left Click: Craft as many as possible and add to inventory
                HandleShiftClickCrafting(craftingManager);
            }
            else if (!InventoryCursor.HasItem())
            {
                // Regular left click: pick up result items to cursor
                InventoryCursor.SetCursorEntry(currentEntry);
                
                // Consume materials and clear result
                craftingManager.ConsumeCraftingMaterials();
                craftingManager.SetResultSlot(new ItemStack());
                
                // Check for new valid recipe after consuming materials
                craftingManager.CheckForValidRecipe();
                
                // Animation handled by InventoryCursor system
            }
            else if (InventoryCursor.HasItem() && InventoryCursor.GetCursorEntry().CanStackWith(currentEntry))
            {
                // Left click with compatible items in cursor: stack more crafted items
                HandleStackCrafting(craftingManager);
            }
        }
        // Right click not supported on result slot
    }
    
    void HandleShiftClickCrafting(CraftingManager craftingManager)
    {
        // Craft as many as possible and add directly to inventory
        int craftCount = 0;
        int maxCrafts = 64; // Safety limit to prevent infinite loops
        
        while (craftCount < maxCrafts && !currentEntry.IsEmpty && CanCraftMore(craftingManager))
        {
            // Try to add result to inventory
            if (currentEntry.entryType == InventoryEntryType.Block ? 
                inventory.AddBlock(currentEntry.blockType, currentEntry.count) :
                inventory.AddItem(currentEntry.itemType, currentEntry.count))
            {
                // Successfully added to inventory, consume materials
                craftingManager.ConsumeCraftingMaterials();
                craftingManager.CheckForValidRecipe();
                craftCount++;
            }
            else
            {
                // Inventory full, stop crafting
                break;
            }
        }
        
        Debug.Log($"Shift-click crafted {craftCount} times");
    }
    
    void HandleStackCrafting(CraftingManager craftingManager)
    {
        // Try to add more crafted items to cursor stack
        var cursorEntry = InventoryCursor.GetCursorEntry();
        int spaceAvailable = ItemStack.MaxStackSize - cursorEntry.count;
        int canAdd = Mathf.Min(spaceAvailable, currentEntry.count);
        
        if (canAdd > 0)
        {
            // Add to cursor stack
            var newEntry = new InventoryEntry
            {
                entryType = cursorEntry.entryType,
                blockType = cursorEntry.blockType,
                itemType = cursorEntry.itemType,
                count = cursorEntry.count + canAdd
            };
            InventoryCursor.SetCursorEntry(newEntry);
            
            // Consume materials and check for new recipe
            craftingManager.ConsumeCraftingMaterials();
            craftingManager.CheckForValidRecipe();
        }
    }
    
    bool CanCraftMore(CraftingManager craftingManager)
    {
        // Check if we have enough materials for another craft by checking if any recipe is still valid
        // This is more generic than hardcoding specific materials
        for (int i = 0; i < 4; i++) // 4 crafting slots
        {
            var slot = craftingManager.GetCraftingSlot(i);
            if (!slot.IsEmpty && slot.count > 0)
            {
                return true; // Found materials that could potentially make more
            }
        }
        return false;
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
        var cursorEntry = InventoryCursor.GetCursorEntry();
        
        if (!InventoryCursor.HasItem())
        {
            // Pick up entire stack
            if (!currentEntry.IsEmpty)
            {
                InventoryCursor.SetCursorEntry(currentEntry);
                inventory.SetSlot(slotIndex, InventoryEntry.Empty);
                
                // Animation handled by InventoryCursor system
            }
        }
        else
        {
            // Place entire cursor stack
            if (currentEntry.IsEmpty)
            {
                // Empty slot - place all cursor items
                string entryDesc = cursorEntry.entryType == InventoryEntryType.Block ? cursorEntry.blockType.ToString() : cursorEntry.itemType.ToString();
                Debug.Log($"InventorySlot: Placing {entryDesc} x{cursorEntry.count} in slot {slotIndex}");
                inventory.SetSlot(slotIndex, cursorEntry);
                InventoryCursor.Clear();
            }
            else if (currentEntry.CanStackWith(cursorEntry))
            {
                // Same type - try to merge stacks
                int maxStack = currentEntry.entryType == InventoryEntryType.Block ? 
                               ItemStack.MaxStackSize : 
                               ItemManager.GetMaxStackSize(currentEntry.itemType);
                int spaceAvailable = maxStack - currentEntry.count;
                int toAdd = Mathf.Min(spaceAvailable, cursorEntry.count);
                
                Debug.Log($"InventorySlot: Stacking - current has {currentEntry.count}, cursor has {cursorEntry.count}, space = {spaceAvailable}, adding = {toAdd}");
                
                if (toAdd > 0)
                {
                    var mergedEntry = new InventoryEntry
                    {
                        entryType = currentEntry.entryType,
                        blockType = currentEntry.blockType,
                        itemType = currentEntry.itemType,
                        count = currentEntry.count + toAdd
                    };
                    inventory.SetSlot(slotIndex, mergedEntry);
                    
                    if (cursorEntry.count <= toAdd)
                    {
                        InventoryCursor.Clear();
                    }
                    else
                    {
                        var remainingEntry = new InventoryEntry
                        {
                            entryType = cursorEntry.entryType,
                            blockType = cursorEntry.blockType,
                            itemType = cursorEntry.itemType,
                            count = cursorEntry.count - toAdd
                        };
                        InventoryCursor.SetCursorEntry(remainingEntry);
                    }
                }
            }
            else
            {
                // Different types - swap stacks
                var slotEntryCopy = new InventoryEntry
                {
                    entryType = currentEntry.entryType,
                    blockType = currentEntry.blockType,
                    itemType = currentEntry.itemType,
                    count = currentEntry.count
                };
                inventory.SetSlot(slotIndex, cursorEntry);
                InventoryCursor.SetCursorEntry(slotEntryCopy);
            }
        }
    }
    
    void HandleRightClick()
    {
        var cursorEntry = InventoryCursor.GetCursorEntry();
        
        if (!InventoryCursor.HasItem())
        {
            // Pick up half stack (or split stack)
            if (!currentEntry.IsEmpty && currentEntry.count > 1)
            {
                int halfCount = Mathf.CeilToInt(currentEntry.count / 2f);
                var halfEntry = new InventoryEntry
                {
                    entryType = currentEntry.entryType,
                    blockType = currentEntry.blockType,
                    itemType = currentEntry.itemType,
                    count = halfCount
                };
                InventoryCursor.SetCursorEntry(halfEntry);
                
                var remainingEntry = new InventoryEntry
                {
                    entryType = currentEntry.entryType,
                    blockType = currentEntry.blockType,
                    itemType = currentEntry.itemType,
                    count = currentEntry.count - halfCount
                };
                inventory.SetSlot(slotIndex, remainingEntry);
                
                // Animation handled by InventoryCursor system
            }
            else if (!currentEntry.IsEmpty)
            {
                // Single item - pick it up
                InventoryCursor.SetCursorEntry(currentEntry);
                inventory.SetSlot(slotIndex, InventoryEntry.Empty);
                
                // Animation handled by InventoryCursor system
            }
        }
        else
        {
            // Place single item from cursor
            if (currentEntry.IsEmpty)
            {
                // Empty slot - place one item
                var singleEntry = new InventoryEntry
                {
                    entryType = cursorEntry.entryType,
                    blockType = cursorEntry.blockType,
                    itemType = cursorEntry.itemType,
                    count = 1
                };
                inventory.SetSlot(slotIndex, singleEntry);
                
                if (cursorEntry.count <= 1)
                {
                    InventoryCursor.Clear();
                }
                else
                {
                    var remainingEntry = new InventoryEntry
                    {
                        entryType = cursorEntry.entryType,
                        blockType = cursorEntry.blockType,
                        itemType = cursorEntry.itemType,
                        count = cursorEntry.count - 1
                    };
                    InventoryCursor.SetCursorEntry(remainingEntry);
                }
            }
            else if (currentEntry.CanStackWith(cursorEntry))
            {
                // Same type - add one if space available
                int maxStack = currentEntry.entryType == InventoryEntryType.Block ? 
                               ItemStack.MaxStackSize : 
                               ItemManager.GetMaxStackSize(currentEntry.itemType);
                if (currentEntry.count < maxStack)
                {
                    var incrementedEntry = new InventoryEntry
                    {
                        entryType = currentEntry.entryType,
                        blockType = currentEntry.blockType,
                        itemType = currentEntry.itemType,
                        count = currentEntry.count + 1
                    };
                    inventory.SetSlot(slotIndex, incrementedEntry);
                    
                    if (cursorEntry.count <= 1)
                    {
                        InventoryCursor.Clear();
                    }
                    else
                    {
                        var remainingEntry = new InventoryEntry
                        {
                            entryType = cursorEntry.entryType,
                            blockType = cursorEntry.blockType,
                            itemType = cursorEntry.itemType,
                            count = cursorEntry.count - 1
                        };
                        InventoryCursor.SetCursorEntry(remainingEntry);
                    }
                }
            }
        }
    }
    
    void HandleQuickTransfer()
    {
        if (currentEntry.IsEmpty) return;
        
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
            var hotbarEntry = inventory.hotbar[i];
            if (!hotbarEntry.IsEmpty && hotbarEntry.CanStackWith(currentEntry))
            {
                int maxStack = hotbarEntry.entryType == InventoryEntryType.Block ? 
                               ItemStack.MaxStackSize : 
                               ItemManager.GetMaxStackSize(hotbarEntry.itemType);
                int spaceAvailable = maxStack - hotbarEntry.count;
                if (spaceAvailable > 0)
                {
                    int toTransfer = Mathf.Min(spaceAvailable, currentEntry.count);
                    var mergedEntry = new InventoryEntry
                    {
                        entryType = hotbarEntry.entryType,
                        blockType = hotbarEntry.blockType,
                        itemType = hotbarEntry.itemType,
                        count = hotbarEntry.count + toTransfer
                    };
                    inventory.SetSlot(i, mergedEntry);
                    
                    if (currentEntry.count <= toTransfer)
                    {
                        inventory.SetSlot(slotIndex, InventoryEntry.Empty);
                        return true;
                    }
                    else
                    {
                        var remainingEntry = new InventoryEntry
                        {
                            entryType = currentEntry.entryType,
                            blockType = currentEntry.blockType,
                            itemType = currentEntry.itemType,
                            count = currentEntry.count - toTransfer
                        };
                        inventory.SetSlot(slotIndex, remainingEntry);
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
                inventory.SetSlot(i, currentEntry);
                inventory.SetSlot(slotIndex, InventoryEntry.Empty);
                return true;
            }
        }
        
        return false;
    }
    
    bool TryTransferToMainInventory()
    {
        // Try to merge with existing stacks first
        for (int i = inventory.hotbar.Length; i < inventory.hotbar.Length + inventory.mainInventorySize; i++)
        {
            var mainEntry = inventory.GetSlot(i);
            if (!mainEntry.IsEmpty && mainEntry.CanStackWith(currentEntry))
            {
                int maxStack = mainEntry.entryType == InventoryEntryType.Block ? 
                               ItemStack.MaxStackSize : 
                               ItemManager.GetMaxStackSize(mainEntry.itemType);
                int spaceAvailable = maxStack - mainEntry.count;
                if (spaceAvailable > 0)
                {
                    int toTransfer = Mathf.Min(spaceAvailable, currentEntry.count);
                    var mergedEntry = new InventoryEntry
                    {
                        entryType = mainEntry.entryType,
                        blockType = mainEntry.blockType,
                        itemType = mainEntry.itemType,
                        count = mainEntry.count + toTransfer
                    };
                    inventory.SetSlot(i, mergedEntry);
                    
                    if (currentEntry.count <= toTransfer)
                    {
                        inventory.SetSlot(slotIndex, InventoryEntry.Empty);
                        return true;
                    }
                    else
                    {
                        var remainingEntry = new InventoryEntry
                        {
                            entryType = currentEntry.entryType,
                            blockType = currentEntry.blockType,
                            itemType = currentEntry.itemType,
                            count = currentEntry.count - toTransfer
                        };
                        inventory.SetSlot(slotIndex, remainingEntry);
                        return true;
                    }
                }
            }
        }
        
        // Try to find empty slot
        for (int i = inventory.hotbar.Length; i < inventory.hotbar.Length + inventory.mainInventorySize; i++)
        {
            var mainEntry = inventory.GetSlot(i);
            if (mainEntry.IsEmpty)
            {
                inventory.SetSlot(i, currentEntry);
                inventory.SetSlot(slotIndex, InventoryEntry.Empty);
                return true;
            }
        }
        
        return false;
    }
    
    bool TryTransferToCrafting()
    {
        // Only transfer to crafting input slots (36-39), not result slot (40)
        int craftingStartIndex = inventory.hotbar.Length + inventory.mainInventorySize;
        for (int i = craftingStartIndex; i < craftingStartIndex + 4; i++)
        {
            var craftingEntry = inventory.GetSlot(i);
            if (!craftingEntry.IsEmpty && craftingEntry.CanStackWith(currentEntry))
            {
                int maxStack = craftingEntry.entryType == InventoryEntryType.Block ? 
                               ItemStack.MaxStackSize : 
                               ItemManager.GetMaxStackSize(craftingEntry.itemType);
                int spaceAvailable = maxStack - craftingEntry.count;
                if (spaceAvailable > 0)
                {
                    int toTransfer = Mathf.Min(spaceAvailable, currentEntry.count);
                    var mergedEntry = new InventoryEntry
                    {
                        entryType = craftingEntry.entryType,
                        blockType = craftingEntry.blockType,
                        itemType = craftingEntry.itemType,
                        count = craftingEntry.count + toTransfer
                    };
                    inventory.SetSlot(i, mergedEntry);
                    
                    if (currentEntry.count <= toTransfer)
                    {
                        inventory.SetSlot(slotIndex, InventoryEntry.Empty);
                        return true;
                    }
                    else
                    {
                        var remainingEntry = new InventoryEntry
                        {
                            entryType = currentEntry.entryType,
                            blockType = currentEntry.blockType,
                            itemType = currentEntry.itemType,
                            count = currentEntry.count - toTransfer
                        };
                        inventory.SetSlot(slotIndex, remainingEntry);
                        return true;
                    }
                }
            }
        }
        
        // Try to find empty crafting slot
        for (int i = craftingStartIndex; i < craftingStartIndex + 4; i++)
        {
            var craftingEntry = inventory.GetSlot(i);
            if (craftingEntry.IsEmpty)
            {
                inventory.SetSlot(i, currentEntry);
                inventory.SetSlot(slotIndex, InventoryEntry.Empty);
                return true;
            }
        }
        
        return false;
    }
    
    // Minecraft-style click-based inventory system
    // First click picks up items to cursor, second click places them
}