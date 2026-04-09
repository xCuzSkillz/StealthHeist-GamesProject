using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip mouseclick;

    [Header("Panels")]
    public GameObject optionsPanel;

    void Start()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false); // hide options at start
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
        SceneManager.LoadScene("GameScene"); // Replace with your main game scene name
    }

    public void LevelSelect()
    {
        PlayClickSound();
        SceneManager.LoadScene("LevelSelectScene"); // Replace with your level select scene name
    }

    public void Options()
    {
        PlayClickSound();
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        PlayClickSound();
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        PlayClickSound();
        Application.Quit();
        Debug.Log("Quit Game pressed");
    }
}
