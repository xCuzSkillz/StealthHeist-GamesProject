using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Drop the PauseMenu prefab into any level scene. ESC (or the configured key) opens it.
/// Buttons can be wired in the inspector to RestartLevel(), LeaveToMainMenu(), QuitGame().
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The root panel GameObject to show / hide (usually a child of this object).")]
    public GameObject panel;

    [Tooltip("Optional click sound to play when toggling or pressing a menu button.")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    [Header("Settings")]
    [Tooltip("Scene name to load when 'Leave Game' is pressed.")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("If true, locks the cursor and pauses time when the pause menu is closed (gameplay mode).")]
    public bool lockCursorWhilePlaying = true;

    public bool IsOpen { get; private set; }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        SetGameplayCursor();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        IsOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayClick();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        IsOpen = false;
        Time.timeScale = 1f;
        SetGameplayCursor();
        PlayClick();
    }

    public void Resume() => Close();

    public void RestartLevel()
    {
        PlayClick();
        Time.timeScale = 1f;
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    public void LeaveToMainMenu()
    {
        PlayClick();
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("[PauseMenu] mainMenuSceneName not set");
            return;
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void SetGameplayCursor()
    {
        if (!lockCursorWhilePlaying) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void PlayClick()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }

    void OnDestroy()
    {
        // Make sure timeScale is restored if menu was open during scene transition.
        Time.timeScale = 1f;
    }
}
