using TMPro;
using UnityEngine;

public class TooltipView : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Clamp")]
    [SerializeField] private Vector2 padding = new Vector2(12f, 12f);

    private Canvas _canvas;
    private RectTransform _canvasRect;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasRect = _canvas.transform as RectTransform;
    }

    public void Show(Vector2 screenPos, string title, string body)
    {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;

        gameObject.SetActive(true);

        // Convert screen position to local point in canvas
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out localPoint
        );

        // Start at cursor position
        root.anchoredPosition = localPoint;

        // Clamp inside canvas bounds
        ClampToCanvas();
    }

    private void ClampToCanvas()
    {
        if (_canvasRect == null || root == null) return;

        // Canvas rect in local space
        Rect canvasRect = _canvasRect.rect;

        // Tooltip size
        Vector2 size = root.rect.size;

        // Because pivot might not be centered, compute min/max based on pivot
        float left = canvasRect.xMin + padding.x + size.x * root.pivot.x;
        float right = canvasRect.xMax - padding.x - size.x * (1f - root.pivot.x);
        float bottom = canvasRect.yMin + padding.y + size.y * root.pivot.y;
        float top = canvasRect.yMax - padding.y - size.y * (1f - root.pivot.y);

        Vector2 pos = root.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, left, right);
        pos.y = Mathf.Clamp(pos.y, bottom, top);

        root.anchoredPosition = pos;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
