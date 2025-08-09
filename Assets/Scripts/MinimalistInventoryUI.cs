using UnityEngine;
using UnityEngine.UI;

public class MinimalistInventoryUI : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform inventoryPanel;
    public RectTransform inventoryGlow;
    public Image[] inventorySlots;
    
    [Header("Animation Settings")]
    [Range(0.1f, 3f)]
    public float fadeInSpeed = 2f;
    [Range(0.1f, 2f)]
    public float hoverGlowIntensity = 1.5f;
    public AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Visual Settings")]
    public Color normalSlotColor = new Color(0.35f, 0.35f, 0.35f, 0.4f);
    public Color highlightSlotColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
    public Color selectedSlotColor = new Color(0.8f, 0.8f, 0.9f, 0.7f);

    private int selectedSlotIndex = 0;
    private float[] slotAnimationTimes;
    private bool isVisible = true;
    private Color originalGlowColor;

    void Start()
    {
        InitializeSlots();
        
        if (inventoryGlow)
            originalGlowColor = inventoryGlow.GetComponent<Image>().color;
    }

    void InitializeSlots()
    {
        if (inventorySlots == null) return;
        
        slotAnimationTimes = new float[inventorySlots.Length];
        
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            slotAnimationTimes[i] = 0f;
            if (inventorySlots[i])
            {
                int slotIndex = i; // Capture for closure
                
                // Add subtle hover animation
                var button = inventorySlots[i].gameObject.GetComponent<Button>();
                if (button == null)
                    button = inventorySlots[i].gameObject.AddComponent<Button>();
                
                // Remove default button graphics
                button.targetGraphic = null;
                button.transition = Selectable.Transition.None;
                
                // Add click handler
                button.onClick.AddListener(() => SelectSlot(slotIndex));
            }
        }
        
        // Set initial selection
        UpdateSlotVisuals();
    }

    void Update()
    {
        HandleInput();
        AnimateSlots();
        
        // Add subtle glow animation
        if (inventoryGlow && inventoryGlow.GetComponent<Image>())
        {
            var glowImage = inventoryGlow.GetComponent<Image>();
            float glowPulse = (Mathf.Sin(Time.time * 0.8f) + 1f) * 0.5f;
            Color targetColor = originalGlowColor;
            targetColor.a = originalGlowColor.a * (0.7f + glowPulse * 0.3f);
            glowImage.color = targetColor;
        }
    }

    void HandleInput()
    {
        // Handle number key selection (1-9)
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) && i <= inventorySlots.Length)
            {
                SelectSlot(i - 1);
                break;
            }
        }

        // Handle scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            int direction = scroll > 0 ? -1 : 1;
            int newIndex = selectedSlotIndex + direction;
            
            if (newIndex < 0) newIndex = inventorySlots.Length - 1;
            if (newIndex >= inventorySlots.Length) newIndex = 0;
            
            SelectSlot(newIndex);
        }
    }

    void AnimateSlots()
    {
        if (inventorySlots == null) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;

            // Update animation time
            if (i == selectedSlotIndex)
                slotAnimationTimes[i] = Mathf.Min(slotAnimationTimes[i] + Time.deltaTime * fadeInSpeed, 1f);
            else
                slotAnimationTimes[i] = Mathf.Max(slotAnimationTimes[i] - Time.deltaTime * fadeInSpeed, 0f);

            // Apply smooth animation curve
            float animValue = smoothCurve.Evaluate(slotAnimationTimes[i]);
            
            // Interpolate colors
            Color targetColor = i == selectedSlotIndex ? selectedSlotColor : normalSlotColor;
            Color currentColor = Color.Lerp(normalSlotColor, targetColor, animValue);
            
            inventorySlots[i].color = currentColor;

            // Add subtle scale animation for selected slot
            if (i == selectedSlotIndex)
            {
                float scale = 1f + (Mathf.Sin(Time.time * 3f) * 0.02f * animValue);
                inventorySlots[i].transform.localScale = Vector3.one * scale;
            }
            else
            {
                inventorySlots[i].transform.localScale = Vector3.one;
            }
        }
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= inventorySlots.Length) return;
        
        selectedSlotIndex = index;
        UpdateSlotVisuals();
        
        // Add subtle screen shake or haptic feedback here if desired
        Debug.Log($"Selected inventory slot: {index + 1}");
    }

    void UpdateSlotVisuals()
    {
        // This will be handled by the animation system in Update()
    }

    public void SetInventoryVisibility(bool visible)
    {
        isVisible = visible;
        
        if (inventoryPanel) inventoryPanel.gameObject.SetActive(visible);
        if (inventoryGlow) inventoryGlow.gameObject.SetActive(visible);
    }

    public void ToggleInventoryVisibility()
    {
        SetInventoryVisibility(!isVisible);
    }

    public int GetSelectedSlotIndex()
    {
        return selectedSlotIndex;
    }

    // Method to add items to slots (placeholder for future expansion)
    public void SetSlotItem(int slotIndex, Sprite itemSprite)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Length) return;
        
        // You can extend this to add item icons to slots
        // For now, it just changes the slot color slightly
        if (itemSprite != null)
        {
            // Could create a child Image component for the item icon
        }
    }
}
