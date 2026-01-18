using System.Collections.Generic;
using UnityEngine;

public class LabTableRenderer : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LabRowView rowPrefab;
    [SerializeField] private TooltipView tooltip;

    private readonly List<LabRowView> _rows = new();

    public void Render(List<LabResult> labs)
    {
        tooltip?.Hide();
        if (labs == null) labs = new List<LabResult>();

        EnsureRowCount(labs.Count);

        for (int i = 0; i < _rows.Count; i++)
        {
            bool active = i < labs.Count;
            _rows[i].gameObject.SetActive(active);
            if (!active) continue;

            _rows[i].Bind(labs[i], OnRowClicked);
        }
    }

    private void EnsureRowCount(int count)
    {
        while (_rows.Count < count)
        {
            var row = Instantiate(rowPrefab, contentRoot);
            _rows.Add(row);
        }
    }

    private void OnRowClicked(LabResult lab, Vector2 screenPos)
    {
        if (tooltip == null) return;

        string status = lab.IsLow ? "Low" : lab.IsHigh ? "High" : "Normal";
        string body =
            $"Value: {lab.value:0.##}\n" +
            $"Normal Range: {lab.normalMin:0.##}–{lab.normalMax:0.##}\n" +
            $"Interpretation: {status}";

        tooltip.Show(screenPos, lab.name, body);
    }
}