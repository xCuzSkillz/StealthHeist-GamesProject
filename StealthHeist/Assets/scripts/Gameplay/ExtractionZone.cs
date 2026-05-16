using UnityEngine;
using UnityEngine.SceneManagement;

public class ExtractionZone : MonoBehaviour
{
    public string nextLevelName = "";   // leave empty to just show victory
    public GameObject victoryPanel;     // optional UI panel

    void OnTriggerEnter(Collider other)
    {
        if (ArtifactPickup.hasArtifact && other.CompareTag("Player"))
        {
            Debug.Log("Mission Complete!");
            if (victoryPanel != null)
                victoryPanel.SetActive(true);
            else
                LevelComplete();
        }
        else if (!ArtifactPickup.hasArtifact && other.CompareTag("Player"))
        {
            Debug.Log("You need the artifact first!");
            // Optional: display warning message
        }
    }

    void LevelComplete()
    {
        if (!string.IsNullOrEmpty(nextLevelName))
            SceneManager.LoadScene(nextLevelName);
        else
        {
            // Simple completion – stop time or show message
            Time.timeScale = 0f;
            Debug.Log("Game Finished – you can reload menu here");
        }
    }
}