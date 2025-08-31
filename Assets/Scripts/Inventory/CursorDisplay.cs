using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CursorDisplay : MonoBehaviour
{
    [Header("UI Components")]
    public Image icon;
    public TextMeshProUGUI countText;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public float scaleMultiplier = 1.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Canvas canvas;
    private RectTransform rectTransform;
    private bool isAnimating = false;
    private Vector3 animationStartPos;
    private Vector3 targetMousePos;
    private float animationTime = 0f;
    private Vector3 originalScale;
    private static CursorDisplay instance;
    
    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        instance = this;
        
        // Initially hide the cursor display
        gameObject.SetActive(false);
    }
    
    void Update()
    {
        // Update cursor position to follow mouse
        if (InventoryCursor.HasItem())
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
                UpdateDisplay();
            }
            
            // Handle animation
            if (isAnimating)
            {
                UpdateAnimation();
            }
            else
            {
                // Normal mouse following when not animating
                UpdateMousePosition();
            }
        }
        else
        {
            if (gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                isAnimating = false;
            }
        }
    }
    
    void UpdateAnimation()
    {
        animationTime += Time.unscaledDeltaTime;
        float progress = animationTime / animationDuration;
        
        if (progress >= 1f)
        {
            // Animation complete
            progress = 1f;
            isAnimating = false;
        }
        
        // Animate position
        float moveProgress = moveCurve.Evaluate(progress);
        rectTransform.localPosition = Vector3.Lerp(animationStartPos, targetMousePos, moveProgress);
        
        // Animate scale
        float scaleProgress = scaleCurve.Evaluate(progress);
        float currentScale = Mathf.Lerp(scaleMultiplier, 1f, scaleProgress);
        rectTransform.localScale = originalScale * currentScale;
    }
    
    void UpdateMousePosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        if (canvas != null)
        {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                mousePosition,
                canvas.worldCamera,
                out localPosition);
            rectTransform.localPosition = localPosition;
        }
    }
    
    void UpdateDisplay()
    {
        var cursorStack = InventoryCursor.GetCursorStack();
        
        if (icon != null)
        {
            icon.sprite = GetSpriteForBlock(cursorStack.blockType);
            icon.color = new Color(1f, 1f, 1f, 0.8f); // Slightly transparent
        }
        
        if (countText != null)
        {
            countText.text = cursorStack.count > 1 ? cursorStack.count.ToString() : "";
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
    
    // Public method to trigger animation from a world position (like inventory slot)
    public static void StartPickupAnimation(Vector3 worldStartPos)
    {
        if (instance == null) return;
        instance.TriggerPickupAnimation(worldStartPos);
    }
    
    void TriggerPickupAnimation(Vector3 worldStartPos)
    {
        if (canvas == null) return;
        
        // Convert world position to canvas local position
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldStartPos);
        Vector2 localStartPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.worldCamera,
            out localStartPos);
        
        // Get current mouse position
        Vector2 mousePos = Input.mousePosition;
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            mousePos,
            canvas.worldCamera,
            out localMousePos);
        
        // Set up animation
        animationStartPos = localStartPos;
        targetMousePos = localMousePos;
        animationTime = 0f;
        isAnimating = true;
        
        // Start at slot position with larger scale
        rectTransform.localPosition = animationStartPos;
        rectTransform.localScale = originalScale * scaleMultiplier;
    }
}