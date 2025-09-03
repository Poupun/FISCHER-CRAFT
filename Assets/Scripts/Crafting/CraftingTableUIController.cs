using UnityEngine;
using System;

public class CraftingTableUIController : MonoBehaviour
{
    [Header("UI Panels")] public GameObject personalCraftingPanel; // existing 2x2 panel
    public GameObject craftingTablePanel; // new 3x3 panel

    [Header("Managers")] 
    [Tooltip("Optional: manual reference to CraftingTableManager; leave empty for auto-find via reflection")] public MonoBehaviour craftingTableManager;
    public CraftingManager personalCraftingManager;
    public InventoryManager inventoryManager;
    public UnifiedPlayerInventory playerInventory;

    [Header("Slot Offset Config")] [Tooltip("Start index in UnifiedPlayerInventory for crafting table input slots (maps to craftingTableInventory array)")] public int craftingTableInputOffset = 0; // relative to crafting table inventory (0-8)
    public int craftingTableResultLocalIndex = 9; // last slot in crafting table inventory

    private bool isCraftingTableOpen = false;

    void Awake()
    {
        // Auto-wire core references
        if (inventoryManager == null) inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (playerInventory == null) playerInventory = FindFirstObjectByType<UnifiedPlayerInventory>();
        if (personalCraftingManager == null) personalCraftingManager = FindFirstObjectByType<CraftingManager>();

        // Auto-find panels by common names if not assigned
        if (personalCraftingPanel == null)
        {
            var go = GameObject.Find("PlayerCraft");
            if (go != null) personalCraftingPanel = go;
        }
        if (craftingTablePanel == null)
        {
            var go = GameObject.Find("CraftingTablePanel");
            if (go != null) craftingTablePanel = go;
        }

        // Robust reflection-based find or create of CraftingTableManager
        if (craftingTableManager == null)
        {
            Type type = Type.GetType("CraftingTableManager");
            if (type == null)
            {
                // Search all loaded assemblies (Unity sometimes puts scripts in Assembly-CSharp)
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType("CraftingTableManager");
                    if (type != null) break;
                }
            }
            if (type != null)
            {
                // Try find existing instance
                var existing = FindFirstObjectByType(type);
                if (existing == null)
                {
                    // Create a new hidden GameObject to host it
                    var host = new GameObject("CraftingTableManager");
                    host.hideFlags = HideFlags.None; // visible in hierarchy for debugging
                    existing = host.AddComponent(type);
                }
                craftingTableManager = existing as MonoBehaviour;
            }
        }

        if (craftingTablePanel != null) craftingTablePanel.SetActive(false);
    }

    public void OpenCraftingTable()
    {
        if (isCraftingTableOpen) return;
        isCraftingTableOpen = true;

        // Ensure inventory is open
        if (inventoryManager != null && !inventoryManager.IsInventoryOpen())
        {
            inventoryManager.OpenInventory();
        }

        if (personalCraftingPanel != null) personalCraftingPanel.SetActive(false);
        if (craftingTablePanel != null) craftingTablePanel.SetActive(true);

    InvokeIfExists(craftingTableManager, "OnCraftingTableOpened");
    }

    public void CloseCraftingTable()
    {
        if (!isCraftingTableOpen) return;
        isCraftingTableOpen = false;

        InvokeIfExists(craftingTableManager, "OnCraftingTableClosed");
        if (craftingTablePanel != null) craftingTablePanel.SetActive(false);
        if (personalCraftingPanel != null) personalCraftingPanel.SetActive(true);
    }

    public bool IsCraftingTableOpen() => isCraftingTableOpen;

    void InvokeIfExists(MonoBehaviour target, string method)
    {
        if (target == null) return;
        var mi = target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        mi?.Invoke(target, null);
    }
}