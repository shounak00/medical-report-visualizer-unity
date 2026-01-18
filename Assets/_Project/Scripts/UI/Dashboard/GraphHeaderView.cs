using TMPro;
using UnityEngine;

public class GraphHeaderView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rangeText; // optional

    public void SetTitle(string title)
    {
        if (titleText != null) titleText.text = title;
    }

    public void SetRange(float min, float max, string unit = "")
    {
        if (rangeText == null) return;
        rangeText.text = $"{min:0.#}–{max:0.#}{unit}";
    }
}