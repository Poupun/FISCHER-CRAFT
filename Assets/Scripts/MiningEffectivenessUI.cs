using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component to display tool effectiveness information
/// </summary>
public class MiningEffectivenessUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject effectivenessPanel;
    [SerializeField] private TextMeshProUGUI effectivenessText;
    [SerializeField] private Image effectivenessIcon;
    [SerializeField] private Color optimalColor = Color.green;
    [SerializeField] private Color suboptimalColor = Color.yellow;
    [SerializeField] private Color noToolColor = Color.white;
    
    [Header("Settings")]
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private bool showOnToolChange = true;
    [SerializeField] private bool showOnBlockLookAt = false;
    
    private UnifiedPlayerInventory playerInventory;
    private MiningSystem miningSystem;
    private ItemType lastToolType;
    private BlockType lastBlockType;
    private Coroutine hideCoroutine;
    
    void Start()
    {
        playerInventory = FindFirstObjectByType<UnifiedPlayerInventory>();
        miningSystem = FindFirstObjectByType<MiningSystem>();
        
        if (effectivenessPanel != null)
        {
            effectivenessPanel.SetActive(false);
        }
        
        // Subscribe to mining events
        if (miningSystem != null)
        {
            miningSystem.OnMiningStarted += OnMiningStarted;
        }
        
        // Subscribe to inventory changes for tool switching
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += OnInventoryChanged;
        }
    }
    
    void OnDestroy()
    {
        if (miningSystem != null)
        {
            miningSystem.OnMiningStarted -= OnMiningStarted;
        }
        
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= OnInventoryChanged;
        }
    }
    
    void Update()
    {
        if (showOnBlockLookAt)
        {
            CheckBlockLookAt();
        }
    }
    
    private void OnMiningStarted()
    {
        if (miningSystem != null && playerInventory != null)
        {
            BlockType blockType = miningSystem.CurrentMiningBlockType;
            var selectedEntry = playerInventory.GetSelectedEntry();
            
            if (selectedEntry.entryType == InventoryEntryType.Item)
            {
                ShowEffectiveness(selectedEntry.itemType, blockType, true);
            }
            else
            {
                ShowNoToolInfo(blockType);
            }
        }
    }
    
    private void OnInventoryChanged()
    {
        if (!showOnToolChange || playerInventory == null) return;
        
        var selectedEntry = playerInventory.GetSelectedEntry();
        ItemType currentToolType = selectedEntry.entryType == InventoryEntryType.Item ? selectedEntry.itemType : ItemType.Stick; // Use Stick as "no tool"
        
        // Only show if tool changed
        if (currentToolType != lastToolType)
        {
            lastToolType = currentToolType;
            
            if (selectedEntry.entryType == InventoryEntryType.Item && IsToolType(selectedEntry.itemType))
            {
                ShowToolInfo(selectedEntry.itemType);
            }
        }
    }
    
    private void CheckBlockLookAt()
    {
        // This would require raycasting to see what block the player is looking at
        // For now, we'll skip this feature to keep it simple
    }
    
    private void ShowEffectiveness(ItemType toolType, BlockType blockType, bool showDuration = true)
    {
        if (effectivenessPanel == null || effectivenessText == null) return;
        
        float multiplier = ToolEffectivenessSystem.GetMiningSpeedMultiplier(toolType, blockType);
        bool isOptimal = ToolEffectivenessSystem.IsToolOptimalForBlock(toolType, blockType);
        BlockCategory category = ToolEffectivenessSystem.GetBlockCategory(blockType);
        var toolData = ToolEffectivenessSystem.GetToolData(toolType);
        
        string toolName = toolData?.toolName ?? toolType.ToString();
        string blockName = blockType.ToString();
        string categoryName = category.ToString();
        
        // Format effectiveness text
        string effectivenessDesc;
        Color textColor;
        
        if (isOptimal)
        {
            effectivenessDesc = $"<b>{toolName}</b>\\nOptimal for {categoryName} blocks\\n<color=#{ColorUtility.ToHtmlStringRGB(optimalColor)}>{multiplier:F1}x Speed</color>";
            textColor = optimalColor;
        }
        else if (multiplier > 1.0f)
        {
            effectivenessDesc = $"<b>{toolName}</b>\\nSub-optimal for {categoryName} blocks\\n<color=#{ColorUtility.ToHtmlStringRGB(suboptimalColor)}>{multiplier:F1}x Speed</color>";
            textColor = suboptimalColor;
        }
        else
        {
            effectivenessDesc = $"<b>{toolName}</b>\\nNot effective for {categoryName} blocks\\n<color=#{ColorUtility.ToHtmlStringRGB(suboptimalColor)}>{multiplier:F1}x Speed</color>";
            textColor = suboptimalColor;
        }
        
        effectivenessText.text = effectivenessDesc;
        
        if (effectivenessIcon != null)
        {
            effectivenessIcon.color = textColor;
        }
        
        ShowPanel(showDuration);
    }
    
    private void ShowNoToolInfo(BlockType blockType)
    {
        if (effectivenessPanel == null || effectivenessText == null) return;
        
        BlockCategory category = ToolEffectivenessSystem.GetBlockCategory(blockType);
        var optimalTools = ToolEffectivenessSystem.GetOptimalToolsForBlock(blockType);
        
        string blockName = blockType.ToString();
        string categoryName = category.ToString();
        
        string toolSuggestion = "";
        if (optimalTools.Length > 0)
        {
            string toolName = optimalTools[0].ToString().Replace("Wood", "").Replace("Stone", "").Replace("Iron", "");
            toolSuggestion = $"\\nUse a {toolName} for better speed!";
        }
        
        effectivenessText.text = $"<b>Hand Mining</b>\\n{categoryName} block\\n<color=#{ColorUtility.ToHtmlStringRGB(noToolColor)}>1.0x Speed</color>{toolSuggestion}";
        
        if (effectivenessIcon != null)
        {
            effectivenessIcon.color = noToolColor;
        }
        
        ShowPanel(true);
    }
    
    private void ShowToolInfo(ItemType toolType)
    {
        if (effectivenessPanel == null || effectivenessText == null) return;
        
        var toolData = ToolEffectivenessSystem.GetToolData(toolType);
        if (toolData == null) return;
        
        string toolName = toolData.toolName;
        string optimalBlocks = "";
        
        foreach (var category in toolData.optimalCategories)
        {
            if (optimalBlocks.Length > 0) optimalBlocks += ", ";
            optimalBlocks += category.ToString();
        }
        
        effectivenessText.text = $"<b>{toolName}</b>\\nTier {toolData.tierLevel}\\n<color=#{ColorUtility.ToHtmlStringRGB(optimalColor)}>Best for: {optimalBlocks}</color>";
        
        if (effectivenessIcon != null)
        {
            effectivenessIcon.color = optimalColor;
        }
        
        ShowPanel(true);
    }
    
    private void ShowPanel(bool autohide)
    {
        if (effectivenessPanel == null) return;
        
        effectivenessPanel.SetActive(true);
        
        // Cancel any existing hide coroutine
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        
        // Start new hide coroutine if auto-hide is enabled
        if (autohide && displayDuration > 0)
        {
            hideCoroutine = StartCoroutine(HideAfterDelay());
        }
    }
    
    private System.Collections.IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        
        if (effectivenessPanel != null)
        {
            effectivenessPanel.SetActive(false);
        }
        
        hideCoroutine = null;
    }
    
    private bool IsToolType(ItemType itemType)
    {
        return itemType == ItemType.WoodPickaxe || itemType == ItemType.StonePickaxe || itemType == ItemType.IronPickaxe ||
               itemType == ItemType.WoodAxe || itemType == ItemType.StoneAxe || itemType == ItemType.IronAxe ||
               itemType == ItemType.WoodShovel || itemType == ItemType.StoneShovel || itemType == ItemType.IronShovel;
    }
    
    public void HidePanel()
    {
        if (effectivenessPanel != null)
        {
            effectivenessPanel.SetActive(false);
        }
        
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }
}