using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteractor : MonoBehaviour
{
    public float interactRange = 2.5f;
    public string promptKey = "E";
    public string promptText = "Open";

    Camera cam;
    Canvas promptCanvas;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();
        BuildPromptUI();
    }

    void Update()
    {
        if (cam == null) return;

        IInteractable interactable = null;
        var ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, interactRange, ~0, QueryTriggerInteraction.Collide))
            interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (promptCanvas != null) promptCanvas.enabled = interactable != null;

        if (interactable != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            interactable.Interact();
    }

    void BuildPromptUI()
    {
        var canvasGO = new GameObject("InteractPromptCanvas");
        canvasGO.transform.SetParent(transform, false);
        promptCanvas = canvasGO.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        float keySize = 32f;

        // Container row anchored to top-left
        var rowGO = new GameObject("PromptRow");
        rowGO.transform.SetParent(canvasGO.transform, false);
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(0f, 1f);
        rowRT.pivot = new Vector2(0f, 1f);
        rowRT.anchoredPosition = new Vector2(20f, -12f);
        rowRT.sizeDelta = new Vector2(220f, keySize);

        // Key "pill": white rounded box with black letter
        var keyGO = new GameObject("KeyBox");
        keyGO.transform.SetParent(rowGO.transform, false);
        var keyImg = keyGO.AddComponent<Image>();
        keyImg.color = Color.white;
        keyImg.raycastTarget = false;
        var keyRT = keyImg.rectTransform;
        keyRT.anchorMin = new Vector2(0f, 0.5f);
        keyRT.anchorMax = new Vector2(0f, 0.5f);
        keyRT.pivot = new Vector2(0f, 0.5f);
        keyRT.anchoredPosition = Vector2.zero;
        keyRT.sizeDelta = new Vector2(keySize, keySize);
        // Soft outline / border feel
        var keyOutline = keyGO.AddComponent<Outline>();
        keyOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
        keyOutline.effectDistance = new Vector2(1f, -1f);

        var keyTextGO = new GameObject("KeyText");
        keyTextGO.transform.SetParent(keyGO.transform, false);
        var keyText = keyTextGO.AddComponent<Text>();
        keyText.text = promptKey;
        keyText.font = font;
        keyText.fontStyle = FontStyle.Bold;
        keyText.fontSize = 20;
        keyText.color = Color.black;
        keyText.alignment = TextAnchor.MiddleCenter;
        keyText.raycastTarget = false;
        var keyTextRT = keyText.rectTransform;
        keyTextRT.anchorMin = Vector2.zero;
        keyTextRT.anchorMax = Vector2.one;
        keyTextRT.offsetMin = Vector2.zero;
        keyTextRT.offsetMax = Vector2.zero;

        // Caption next to the key
        var labelGO = new GameObject("PromptLabel");
        labelGO.transform.SetParent(rowGO.transform, false);
        var label = labelGO.AddComponent<Text>();
        label.text = promptText;
        label.font = font;
        label.fontSize = 20;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        var labelRT = label.rectTransform;
        labelRT.anchorMin = new Vector2(0f, 0.5f);
        labelRT.anchorMax = new Vector2(0f, 0.5f);
        labelRT.pivot = new Vector2(0f, 0.5f);
        labelRT.anchoredPosition = new Vector2(keySize + 8f, 0f);
        labelRT.sizeDelta = new Vector2(180f, keySize);
        var labelOutline = labelGO.AddComponent<Outline>();
        labelOutline.effectColor = Color.black;
        labelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        promptCanvas.enabled = false;
    }
}
