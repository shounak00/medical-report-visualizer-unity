using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGraphRenderer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform graphRect;
    [SerializeField] private UIGraphPool pool;

    [Header("Layout")]
    [SerializeField] private float padding = 16f;
    [SerializeField] private float pointSize = 10f;
    [SerializeField] private float lineThickness = 4f;

    [Header("Options")]
    [SerializeField] private bool normalizeToLocalMinMax = true;

    public void Render(IReadOnlyList<float> values, Color lineColor, Color pointColor)
    {
        if (values == null || values.Count < 2)
        {
            pool.EnsurePoints(0);
            pool.EnsureSegments(0);
            return;
        }

        float min = values[0], max = values[0];
        if (normalizeToLocalMinMax)
        {
            for (int i = 1; i < values.Count; i++)
            {
                min = Mathf.Min(min, values[i]);
                max = Mathf.Max(max, values[i]);
            }
        }

        float range = Mathf.Max(0.0001f, max - min);

        pool.EnsurePoints(values.Count);
        pool.EnsureSegments(values.Count - 1);

        float width = graphRect.rect.width - padding * 2f;
        float height = graphRect.rect.height - padding * 2f;

        Vector2 prevPos = Vector2.zero;

        for (int i = 0; i < values.Count; i++)
        {
            float tX = i / (float)(values.Count - 1);
            float tY = (values[i] - min) / range;

            var pos = new Vector2(
                padding + tX * width,
                padding + tY * height
            );

            var pt = pool.GetPoint(i);
            pt.anchoredPosition = pos;
            pt.sizeDelta = new Vector2(pointSize, pointSize);

            if (pt.TryGetComponent<Image>(out var ptImg))
                ptImg.color = pointColor;

            if (i > 0)
            {
                var seg = pool.GetSegment(i - 1);
                DrawSegment(seg, prevPos, pos, lineThickness, lineColor);
            }

            prevPos = pos;
        }
    }

    private static void DrawSegment(RectTransform seg, Vector2 a, Vector2 b, float thickness, Color color)
    {
        Vector2 dir = (b - a);
        float length = dir.magnitude;

        seg.anchoredPosition = a + dir * 0.5f;
        seg.sizeDelta = new Vector2(length, thickness);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        seg.localRotation = Quaternion.Euler(0, 0, angle);

        if (seg.TryGetComponent<Image>(out var img))
            img.color = color;
    }
}
