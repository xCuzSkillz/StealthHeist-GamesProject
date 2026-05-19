using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ControlsRebindManager : MonoBehaviour
{
    [Header("Input Setup")]
    public InputActionAsset inputActions;
    public string actionMapName = "Player";
    [Tooltip("Only show bindings whose group contains this string. Leave empty to show all.")]
    public string controlSchemeFilter = "Keyboard&Mouse";
    [Tooltip("Action names to hide from the rebind list.")]
    public string[] excludedActionNames = new[] { "Attack", "Next", "Previous", "Sprint", "Look" };
    [Tooltip("Hide arrow-key bindings (upArrow/downArrow/leftArrow/rightArrow).")]
    public bool hideArrowKeys = true;

    [Header("UI")]
    public RectTransform rowsContainer;
    public TMP_FontAsset font;
    public Button resetButton;

    private const string PrefsKey = "Rebinds.OverridesJson";
    private InputActionRebindingExtensions.RebindingOperation activeRebind;

    private class RowRefs
    {
        public InputAction action;
        public int bindingIndex;
        public TextMeshProUGUI bindingLabel;
    }

    private readonly List<RowRefs> rows = new List<RowRefs>();

    void OnEnable()
    {
        LoadOverrides();
        BuildRows();
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetAll);
        }
    }

    void OnDisable()
    {
        if (activeRebind != null)
        {
            activeRebind.Cancel();
            activeRebind.Dispose();
            activeRebind = null;
        }
    }

    void LoadOverrides()
    {
        if (inputActions == null) return;
        string json = PlayerPrefs.GetString(PrefsKey, "");
        if (!string.IsNullOrEmpty(json))
            inputActions.LoadBindingOverridesFromJson(json);
    }

    void SaveOverrides()
    {
        if (inputActions == null) return;
        PlayerPrefs.SetString(PrefsKey, inputActions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }

    void BuildRows()
    {
        if (rowsContainer == null || inputActions == null) return;
        for (int i = rowsContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(rowsContainer.GetChild(i).gameObject);
        rows.Clear();

        var map = inputActions.FindActionMap(actionMapName);
        if (map == null) { Debug.LogWarning($"[ControlsRebind] no action map '{actionMapName}'"); return; }

        foreach (var action in map.actions)
        {
            if (excludedActionNames != null && System.Array.IndexOf(excludedActionNames, action.name) >= 0)
                continue;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite) continue;
                if (!string.IsNullOrEmpty(controlSchemeFilter) &&
                    !string.IsNullOrEmpty(b.groups) &&
                    !b.groups.Contains(controlSchemeFilter))
                    continue;
                if (hideArrowKeys && !string.IsNullOrEmpty(b.effectivePath) && b.effectivePath.Contains("Arrow"))
                    continue;
                CreateRow(action, i);
            }
        }
    }

    void CreateRow(InputAction action, int bindingIndex)
    {
        var rowGO = new GameObject($"Row_{action.name}_{bindingIndex}",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGO.transform.SetParent(rowsContainer, false);
        var hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.padding = new RectOffset(12, 12, 6, 6);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        var rowLE = rowGO.GetComponent<LayoutElement>();
        rowLE.minHeight = 58;
        rowLE.preferredHeight = 58;

        CreateLabel(rowGO.transform, "ActionLabel", DisplayName(action, bindingIndex), 340, 240, Color.white, TextAlignmentOptions.MidlineLeft);
        var keyLabel = CreateLabel(rowGO.transform, "KeyLabel", BindingDisplayText(action, bindingIndex), 260, 200, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Midline);
        var btn = CreateButton(rowGO.transform, "Rebind", 150, 50);

        rows.Add(new RowRefs { action = action, bindingIndex = bindingIndex, bindingLabel = keyLabel });
        btn.onClick.AddListener(() => StartRebind(action, bindingIndex, keyLabel));
    }

    string DisplayName(InputAction action, int bindingIndex)
    {
        var b = action.bindings[bindingIndex];
        if (b.isPartOfComposite) return $"{action.name} ({b.name})";
        return action.name;
    }

    string BindingDisplayText(InputAction action, int bindingIndex)
    {
        return InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    TextMeshProUGUI CreateLabel(Transform parent, string name, string text, int prefW, int minW, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = prefW;
        le.minWidth = minW;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 32;
        tmp.color = color;
        tmp.alignment = align;
        return tmp;
    }

    Button CreateButton(Transform parent, string label, int prefW, int prefH)
    {
        var go = new GameObject("RebindButton",
            typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = prefW;
        le.preferredHeight = prefH;
        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.13f, 0.12f, 0.18f, 1f);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go.GetComponent<Button>();
    }

    void StartRebind(InputAction action, int bindingIndex, TextMeshProUGUI bindingLabel)
    {
        if (activeRebind != null) return;

        action.Disable();
        bindingLabel.text = "Press a key…";

        activeRebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                bindingLabel.text = BindingDisplayText(action, bindingIndex);
                action.Enable();
                op.Dispose();
                activeRebind = null;
                SaveOverrides();
            })
            .OnCancel(op =>
            {
                bindingLabel.text = BindingDisplayText(action, bindingIndex);
                action.Enable();
                op.Dispose();
                activeRebind = null;
            })
            .Start();
    }

    public void ResetAll()
    {
        if (inputActions == null) return;
        foreach (var map in inputActions.actionMaps)
            map.RemoveAllBindingOverrides();
        SaveOverrides();
        BuildRows();
    }
}
