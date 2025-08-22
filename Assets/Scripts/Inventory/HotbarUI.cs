using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HotbarUI : MonoBehaviour
{
    public PlayerInventory inventory;
    public RectTransform slotContainer; // If null, will use own RectTransform
    public GameObject slotPrefab;       // Optional: if omitted but children exist, we'll use them
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    [Header("Debug")] public bool showTypeLabel = false; public Color typeLabelColor = Color.white; public int typeLabelFontSize = 14;
    [Header("Selection Animation")] public float selectedScale = 1.25f; public float normalScale = 1f; public float scaleLerpSpeed = 12f;

    private SlotUI[] slots;
    private Dictionary<BlockType, Sprite> spriteCache = new Dictionary<BlockType, Sprite>();
    private WorldGenerator worldGenerator; // to access textures

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
        if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
        worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);
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
            inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory == null) return;
        }
    if (worldGenerator == null) worldGenerator = FindFirstObjectByType<WorldGenerator>(FindObjectsInactive.Exclude);

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
                if (images.Length > 0) bg = images[0];
                if (images.Length > 1) icon = images[1];
                var text = child.GetComponentInChildren<TextMeshProUGUI>(true);
                slots[i] = new SlotUI { background = bg, icon = icon, countText = text, shownType = BlockType.Air, shownCount = -999, rect = child as RectTransform };
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
        var stack = inventory.hotbar[index];
        var ui = slots[index];
        if (ui == null) return;
        bool typeChanged = ui.shownType != stack.blockType;
        bool countChanged = ui.shownCount != stack.count;
        if (!force && !typeChanged && !countChanged) return;
        ui.shownType = stack.blockType;
        ui.shownCount = stack.count;
        if (ui.icon != null)
        {
            if (stack.IsEmpty)
            {
                ui.icon.enabled = false;
            }
            else
            {
                ui.icon.enabled = true;
                ui.icon.sprite = GetSpriteForBlock(stack.blockType);
                ui.icon.color = Color.white;
                ui.icon.preserveAspect = true;
            }
        }
        if (ui.countText != null)
        {
            if (stack.IsEmpty) ui.countText.text = string.Empty;
            else if (showTypeLabel) ui.countText.text = stack.blockType + (stack.count > 1 ? " x" + stack.count : "");
            else ui.countText.text = (stack.count > 1 ? stack.count.ToString() : string.Empty);
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

    Sprite GetSpriteForBlock(BlockType type)
    {
        if (type == BlockType.Air) return null;
    // Using only flat texture sprites now (3D generator removed)
        // Fallback: existing texture->sprite simple icon
        if (spriteCache.TryGetValue(type, out var spc) && spc != null) return spc;
        Texture2D tex = null;
        if ((int)type < BlockDatabase.blockTypes.Length)
        {
            tex = BlockDatabase.blockTypes[(int)type].blockTexture;
        }
        if (tex == null && worldGenerator != null)
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
            }
        }
        if (tex == null) { spriteCache[type] = null; return null; }
        var sprite = Sprite.Create(tex, new Rect(0,0,tex.width, tex.height), new Vector2(0.5f,0.5f), 32f);
        sprite.name = type + "_SpriteFallback";
        spriteCache[type] = sprite;
        return sprite;
    }

    void DumpSlotIconMapping()
    {
        if (inventory == null || slots == null) { Debug.Log("[HotbarUI] Cannot dump mapping; inventory or slots missing."); return; }
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("[HotbarUI] Slot -> (BlockType,count) -> SpriteName");
        for (int i=0;i<Mathf.Min(slots.Length, inventory.hotbar.Length);i++)
        {
            var st = inventory.hotbar[i]; var ui = slots[i];
            string spriteName = ui?.icon != null && ui.icon.sprite != null ? ui.icon.sprite.name : "<none>";
            sb.AppendLine($" {i}: {st.blockType} x{st.count} -> {spriteName}");
        }
        Debug.Log(sb.ToString());
    }
}
