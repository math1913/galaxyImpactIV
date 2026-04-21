using UnityEngine;
using UnityEngine.EventSystems;

public class SkinSelectionOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SkinSelectionController controller;
    private int skinIndex;

    public void Initialize(SkinSelectionController owner, int index)
    {
        controller = owner;
        skinIndex = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller?.SetHover(skinIndex, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller?.SetHover(skinIndex, false);
    }
}
