using System.Collections.Generic;
using UnityEngine;

public class UIGraphPool : MonoBehaviour
{
    [SerializeField] private RectTransform pointsRoot;
    [SerializeField] private RectTransform segmentsRoot;
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private GameObject segmentPrefab;

    private readonly List<RectTransform> _points = new();
    private readonly List<RectTransform> _segments = new();

    public void EnsurePoints(int count)
    {
        while (_points.Count < count)
        {
            var go = Instantiate(pointPrefab, pointsRoot);
            _points.Add(go.GetComponent<RectTransform>());
        }

        for (int i = 0; i < _points.Count; i++)
            _points[i].gameObject.SetActive(i < count);
    }

    public void EnsureSegments(int count)
    {
        while (_segments.Count < count)
        {
            var go = Instantiate(segmentPrefab, segmentsRoot);
            _segments.Add(go.GetComponent<RectTransform>());
        }

        for (int i = 0; i < _segments.Count; i++)
            _segments[i].gameObject.SetActive(i < count);
    }

    public RectTransform GetPoint(int index) => _points[index];
    public RectTransform GetSegment(int index) => _segments[index];
}