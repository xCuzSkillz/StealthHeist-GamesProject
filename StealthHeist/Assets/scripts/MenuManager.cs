using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip mouseclick;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject levelSelectPanel;

    void Start()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
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

    public void QuitGame()
    {
        PlayClickSound();
        Application.Quit();
        Debug.Log("Quit Game pressed");
    }
}
