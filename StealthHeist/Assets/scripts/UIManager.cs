using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI detectionText;

    void Start()
    {
        detectionText.gameObject.SetActive(false);
    }

    void Awake()
    {
        Instance = this;
    }

    public void ShowDetectionMessage()
    {
        detectionText.gameObject.SetActive(true);
    }

    public void HideDetectionMessage()
    {
        detectionText.gameObject.SetActive(false);
    }
}