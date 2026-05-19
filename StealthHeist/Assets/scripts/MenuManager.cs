using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip mouseclick;

    [Header("Sensitivity")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;
    public float defaultSensitivity = 0.15f;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject levelSelectPanel;
    public GameObject controlsPanel; // legacy — no longer used after refactor; kept for inspector backwards-compat

    [Header("Options Sub-Sections")]
    public GameObject soundsGroup;
    public GameObject controlsGroup;

    void Start()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        // Default to showing sound sliders when options opens
        if (soundsGroup != null) soundsGroup.SetActive(true);
        if (controlsGroup != null) controlsGroup.SetActive(false);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
            UpdateSensitivityText(sensitivitySlider.value);
        }
    }

    public void ChangeSensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
        UpdateSensitivityText(value);
    }

    void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("0.00");
    }

    private void PlayClickSound()
    {
        if (audioSource != null && mouseclick != null)
        {
            audioSource.PlayOneShot(mouseclick);
        }
    }

    public void StartGame()
    {
        PlayClickSound();
        SceneManager.LoadScene("GameScene");
    }

    public void LevelSelect()
    {
        PlayClickSound();
        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
    }

    public void CloseLevelSelect()
    {
        PlayClickSound();
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void LoadLevelByName(string sceneName)
    {
        PlayClickSound();
        if (string.IsNullOrEmpty(sceneName)) return;
        SceneManager.LoadScene(sceneName);
    }

    public void Options()
    {
        PlayClickSound();
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        PlayClickSound();
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void Controls()
    {
        PlayClickSound();
        if (soundsGroup != null) soundsGroup.SetActive(false);
        if (controlsGroup != null) controlsGroup.SetActive(true);
    }

    public void Sounds()
    {
        PlayClickSound();
        if (controlsGroup != null) controlsGroup.SetActive(false);
        if (soundsGroup != null) soundsGroup.SetActive(true);
    }

    public void CloseControls()
    {
        PlayClickSound();
        if (controlsGroup != null) controlsGroup.SetActive(false);
        if (soundsGroup != null) soundsGroup.SetActive(true);
    }

    public void QuitGame()
    {
        PlayClickSound();
        Application.Quit();
        Debug.Log("Quit Game pressed");
    }
}
