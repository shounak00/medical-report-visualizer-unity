using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LabRowView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image background;

    private LabResult _data;
    private System.Action<LabResult, Vector2> _onClicked;

    public void Bind(LabResult data, System.Action<LabResult, Vector2> onClicked)
    {
        _data = data;
        _onClicked = onClicked;

        nameText.text = data.name;
        valueText.text = data.value.ToString("0.##");
        rangeText.text = $"{data.normalMin:0.##}–{data.normalMax:0.##}";

        string status = data.IsLow ? "Low" : data.IsHigh ? "High" : "Normal";
        statusText.text = status;

        if (background != null)
        {
            background.color = data.IsAbnormal
                ? new Color(0.35f, 0.10f, 0.10f, 0.55f)
                : new Color(0.10f, 0.25f, 0.10f, 0.45f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClicked?.Invoke(_data, eventData.position);
    }
}