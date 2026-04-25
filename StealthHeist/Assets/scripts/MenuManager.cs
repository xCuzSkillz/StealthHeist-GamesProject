using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip mouseclick;

    [Header("Panels")]
    public GameObject optionsPanel;
    public GameObject selectLevelPanel;

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

    public void ToggleLevelSelect(bool state)
    {
        PlayClickSound();
        selectLevelPanel.SetActive(state); // Change Level select panel state
    }

    public void LoadScene(int index)
    {
        SceneManager.LoadScene(index); // Load selected level scene from Level Select Panel
    }

    public void ToggleOptions(bool state)
    {
        PlayClickSound();
        optionsPanel.SetActive(state); // change Option panel state
    }

    public void QuitGame()
    {
        PlayClickSound();
        Application.Quit();
        Debug.Log("Quit Game pressed");
    }
}
