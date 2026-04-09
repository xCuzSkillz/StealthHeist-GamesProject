using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public TMP_Text buttonText;   // Drag your Text (TMP) here
    public Image buttonImage;     // Drag the Button's Image here

    [Header("Colors")]
    public Color normalTextColor = Color.white;
    public Color hoverTextColor = new Color32(33, 30, 47, 255); // dark text

    public Color normalButtonColor = new Color32(33, 30, 47, 255); // dark background
    public Color hoverButtonColor = Color.white;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null) buttonText.color = hoverTextColor;
        if (buttonImage != null) buttonImage.color = hoverButtonColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null) buttonText.color = normalTextColor;
        if (buttonImage != null) buttonImage.color = normalButtonColor;
    }
}