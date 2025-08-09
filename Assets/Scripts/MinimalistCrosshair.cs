using UnityEngine;
using UnityEngine.UI;

public class MinimalistCrosshair : MonoBehaviour
{
    [Header("Crosshair Elements")]
    public Image centerDot;
    public Image topLine;
    public Image bottomLine;
    public Image leftLine;
    public Image rightLine;

    [Header("Animation Settings")]
    [Range(0.1f, 2f)]
    public float pulseSpeed = 1f;
    [Range(0.1f, 1f)]
    public float minOpacity = 0.6f;
    [Range(0.1f, 1f)]
    public float maxOpacity = 0.9f;

    [Header("Dynamic Response")]
    public bool respondToMovement = true;
    public float expansionOnMove = 2f;
    public float returnSpeed = 5f;

    private Vector3 originalTopPos;
    private Vector3 originalBottomPos;
    private Vector3 originalLeftPos;
    private Vector3 originalRightPos;

    private float baseOpacity;
    private float targetExpansion = 1f;
    private float currentExpansion = 1f;

    void Start()
    {
        // Store original positions
        if (topLine) originalTopPos = topLine.rectTransform.anchoredPosition;
        if (bottomLine) originalBottomPos = bottomLine.rectTransform.anchoredPosition;
        if (leftLine) originalLeftPos = leftLine.rectTransform.anchoredPosition;
        if (rightLine) originalRightPos = rightLine.rectTransform.anchoredPosition;

        baseOpacity = maxOpacity;
    }

    void Update()
    {
        // Subtle breathing animation
        float breathe = Mathf.Lerp(minOpacity, maxOpacity, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);

        // Apply breathing to all elements
        ApplyOpacity(breathe);

        // Dynamic expansion based on movement
        if (respondToMovement)
        {
            // Check for input movement
            float inputMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
            bool isMoving = inputMagnitude > 0.1f || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0;

            targetExpansion = isMoving ? expansionOnMove : 1f;
            currentExpansion = Mathf.Lerp(currentExpansion, targetExpansion, Time.deltaTime * returnSpeed);

            UpdateExpansion();
        }
    }

    void ApplyOpacity(float opacity)
    {
        if (centerDot)
        {
            Color color = centerDot.color;
            color.a = opacity;
            centerDot.color = color;
        }

        float lineOpacity = opacity * 0.85f; // Lines slightly more transparent
        ApplyOpacityToLine(topLine, lineOpacity);
        ApplyOpacityToLine(bottomLine, lineOpacity);
        ApplyOpacityToLine(leftLine, lineOpacity);
        ApplyOpacityToLine(rightLine, lineOpacity);
    }

    void ApplyOpacityToLine(Image line, float opacity)
    {
        if (line)
        {
            Color color = line.color;
            color.a = opacity;
            line.color = color;
        }
    }

    void UpdateExpansion()
    {
        if (topLine)
            topLine.rectTransform.anchoredPosition = originalTopPos * currentExpansion;
        if (bottomLine)
            bottomLine.rectTransform.anchoredPosition = originalBottomPos * currentExpansion;
        if (leftLine)
            leftLine.rectTransform.anchoredPosition = originalLeftPos * currentExpansion;
        if (rightLine)
            rightLine.rectTransform.anchoredPosition = originalRightPos * currentExpansion;
    }

    public void SetCrosshairVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetCrosshairColor(Color color)
    {
        if (centerDot) centerDot.color = new Color(color.r, color.g, color.b, centerDot.color.a);
        if (topLine) topLine.color = new Color(color.r, color.g, color.b, topLine.color.a);
        if (bottomLine) bottomLine.color = new Color(color.r, color.g, color.b, bottomLine.color.a);
        if (leftLine) leftLine.color = new Color(color.r, color.g, color.b, leftLine.color.a);
        if (rightLine) rightLine.color = new Color(color.r, color.g, color.b, rightLine.color.a);
    }
}
