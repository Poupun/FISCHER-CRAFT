using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HotbarUI : MonoBehaviour
{
    public UnifiedPlayerInventory inventory;
    public RectTransform slotContainer; // If null, will use own RectTransform
    public GameObject slotPrefab;       // Optional: if omitted but children exist, we'll use them
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    [Header("Debug")] public bool showTypeLabel = false; public Color typeLabelColor = Color.white; public int typeLabelFontSize = 14;
    [Header("Selection Animation")] public float selectedScale = 1.25f; public float normalScale = 1f; public float scaleLerpSpeed = 12f;

    private SlotUI[] slots;

    [System.Serializable]
    private class SlotUI
    {
        public Image background;
        public Image icon;
        public TextMeshProUGUI countText;
        public BlockType shownType;
        public int shownCount;
        public RectTransform rect;
    }

    void Awake()
    {
        if (slotContainer == null) slotContainer = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (inventory == null) inventory = FindFirstObjectByType<UnifiedPlayerInventory>();
        if (inventory != null) inventory.OnInventoryChanged += HandleInventoryChanged;
        EnsureSlots();
        RefreshAll(force: true);
        ForceInitialScales();
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.OnInventoryChanged -= HandleInventoryChanged;
    }

    void HandleInventoryChanged()
    {
        RefreshAll(force: true);
    }

    void Update()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<UnifiedPlayerInventory>();
            if (inventory == null) return;
        }

        if (slots == null || slots.Length == 0) EnsureSlots();
        if (slots == null) return;

        int maxKeySlots = Mathf.Min(9, slots.Length);
        for (int i = 0; i < maxKeySlots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) inventory.SetSelectedIndex(i);
        }
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.1f) inventory.Cycle(scroll > 0 ? -1 : 1);

        if (Input.GetKeyDown(KeyCode.F9)) DumpSlotIconMapping();
        RefreshAll(force: false);
        AnimateSelectionScale();
    }

    private bool _postPreloadRefreshDone = false; // retained for potential future use (no-op now)

    void EnsureSlots()
    {
        if (inventory == null || slotContainer == null) return;
        if (slotContainer.childCount > 0 && slotPrefab == null)
        {
            slots = new SlotUI[slotContainer.childCount];
            for (int i = 0; i < slotContainer.childCount; i++)
            {
                Transform child = slotContainer.GetChild(i);
                var images = child.GetComponentsInChildren<Image>(true);
                Image bg = null, icon = null;
                
                // Direct name-based detection - find the Icon component specifically
                foreach (var img in images)
                {
                    if (img.name.ToLower().Contains("background") || img.name.ToLower().Contains("bg"))
                        bg = img;
                    else if (img.name.ToLower().Contains("icon"))
                        icon = img;
                }
                
                var text = child.GetComponentInChildren<TextMeshProUGUI>(true);
                slots[i] = new SlotUI { background = bg, icon = icon, countText = text, shownType = BlockType.Air, shownCount = -999, rect = child as RectTransform };
                
                
                
                // Add drag & drop functionality to existing slots
                SetupDragDrop(child.gameObject, i, bg, icon, text);
            }
            return;
        }
        if (slotPrefab != null)
        {
            foreach (Transform c in slotContainer) Destroy(c.gameObject);
            slots = new SlotUI[inventory.hotbar.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                var go = Instantiate(slotPrefab, slotContainer);
                go.name = $"Slot_{i}";
                var images = go.GetComponentsInChildren<Image>(true);
                Image bg = images.Length > 0 ? images[0] : null;
                Image icon = images.Length > 1 ? images[1] : null;
                var text = go.GetComponentInChildren<TextMeshProUGUI>(true);
                slots[i] = new SlotUI { background = bg, icon = icon, countText = text, shownType = BlockType.Air, shownCount = -999, rect = go.transform as RectTransform };
                
                // Add drag & drop functionality to prefab slots
                SetupDragDrop(go, i, bg, icon, text);
            }
            return;
        }

        // Fallback: auto-generate simple slots if none exist and no prefab provided.
        if ((slots == null || slots.Length == 0) && slotPrefab == null)
        {
            slots = new SlotUI[inventory.hotbar.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                var slotGO = new GameObject($"Slot_{i}", typeof(RectTransform));
                slotGO.transform.SetParent(slotContainer, false);
                var bgGO = new GameObject("BG", typeof(RectTransform), typeof(Image));
                bgGO.transform.SetParent(slotGO.transform, false);
                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(slotGO.transform, false);
                var textGO = new GameObject("Count", typeof(RectTransform));
                textGO.transform.SetParent(slotGO.transform, false);
                TextMeshProUGUI tmp = null;
                try { tmp = textGO.AddComponent<TextMeshProUGUI>(); } catch { }
                var bgImg = bgGO.GetComponent<Image>();
                bgImg.color = new Color(0.15f,0.15f,0.15f,0.8f);
                var iconImg = iconGO.GetComponent<Image>();
                var rect = slotGO.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(48,48);
                slots[i] = new SlotUI { background = bgImg, icon = iconImg, countText = tmp, shownType = BlockType.Air, shownCount = -999, rect = rect };
                if (showTypeLabel && tmp != null) { tmp.fontSize = typeLabelFontSize; tmp.color = typeLabelColor; }
                
                // Add drag & drop functionality to generated slots
                SetupDragDrop(slotGO, i, bgImg, iconImg, tmp);
            }
        }
    }

    void RefreshAll(bool force)
    {
        if (inventory == null || slots == null) return;
        int len = Mathf.Min(slots.Length, inventory.hotbar.Length);
        for (int i = 0; i < len; i++) UpdateSlot(i, force);
        RefreshVisualSelection();
    }

    void UpdateSlot(int index, bool force)
    {
        if (index < 0 || index >= slots.Length || index >= inventory.hotbar.Length) return;
        var entry = inventory.hotbar[index];
        var ui = slots[index];
        if (ui == null) return;
        
        // Convert InventoryEntry to ItemStack-like values for UI compatibility
        BlockType currentType = entry.entryType == InventoryEntryType.Block ? entry.blockType : BlockType.Air;
        int currentCount = entry.count;
        
        bool typeChanged = ui.shownType != currentType;
        bool countChanged = ui.shownCount != currentCount;
        if (!force && !typeChanged && !countChanged) return;
        ui.shownType = currentType;
        ui.shownCount = currentCount;
        // Handle Icon component - assign sprites for both blocks and items using unified system
        if (ui.icon != null)
        {
            if (entry.IsEmpty)
            {
                ui.icon.enabled = false;
            }
            else
            {
                ui.icon.enabled = true;
                ui.icon.sprite = entry.GetSprite(); // Use unified sprite system
                ui.icon.color = Color.white;
                ui.icon.preserveAspect = true;
            }
        }
        if (ui.countText != null)
        {
            if (entry.IsEmpty) ui.countText.text = string.Empty;
            else if (showTypeLabel) ui.countText.text = currentType + (currentCount > 1 ? " x" + currentCount : "");
            else ui.countText.text = (currentCount > 1 ? currentCount.ToString() : string.Empty);
        }
    }

    void RefreshVisualSelection()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var ui = slots[i];
            if (ui?.background != null)
            {
                ui.background.color = (i == inventory.selectedIndex) ? selectedColor : normalColor;
            }
        }
    }

    void AnimateSelectionScale()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var ui = slots[i]; if (ui?.rect == null) continue;
            float target = (i == inventory.selectedIndex) ? selectedScale : normalScale;
            Vector3 current = ui.rect.localScale;
            Vector3 tgt = Vector3.one * target;
            ui.rect.localScale = Vector3.Lerp(current, tgt, Time.unscaledDeltaTime * scaleLerpSpeed);
        }
    }

    void ForceInitialScales()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            var ui = slots[i]; if (ui?.rect == null) continue;
            ui.rect.localScale = Vector3.one * ((i == inventory.selectedIndex) ? selectedScale : normalScale);
        }
    }


    void DumpSlotIconMapping()
    {
        if (inventory == null || slots == null) { Debug.Log("[HotbarUI] Cannot dump mapping; inventory or slots missing."); return; }
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("[HotbarUI] Slot -> (BlockType,count) -> SpriteName");
        for (int i=0;i<Mathf.Min(slots.Length, inventory.hotbar.Length);i++)
        {
            var entry = inventory.hotbar[i]; var ui = slots[i];
            string spriteName = ui?.icon != null && ui.icon.sprite != null ? ui.icon.sprite.name : "<none>";
            string entryDesc = entry.entryType == InventoryEntryType.Block ? $"{entry.blockType}" : $"{entry.itemType}";
            sb.AppendLine($" {i}: {entryDesc} x{entry.count} -> {spriteName}");
        }
        Debug.Log(sb.ToString());
    }

    void SetupDragDrop(GameObject slotObject, int slotIndex, Image background, Image icon, TextMeshProUGUI countText)
    {
        // Add InventorySlot component for drag & drop functionality
        InventorySlot inventorySlot = slotObject.GetComponent<InventorySlot>();
        if (inventorySlot == null)
        {
            inventorySlot = slotObject.AddComponent<InventorySlot>();
        }
        
        // Configure the InventorySlot component
        inventorySlot.background = background;
        inventorySlot.icon = icon;
        inventorySlot.countText = countText;
        inventorySlot.slotIndex = slotIndex;
        inventorySlot.normalColor = normalColor;
        inventorySlot.highlightColor = selectedColor;
        
       
    }
}
