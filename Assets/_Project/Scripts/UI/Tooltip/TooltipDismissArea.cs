using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipDismissArea : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private TooltipView tooltip;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (tooltip != null && tooltip.gameObject.activeSelf)
            tooltip.Hide();
    }
}