using UnityEngine;

public class ArtifactPickup : MonoBehaviour
{
    public GameObject artifactVisual;   // the visual model (optional)
    public AudioClip pickupSound;       // optional
    public static bool hasArtifact = false;   // static so other scripts know

    void Start()
    {
        hasArtifact = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            hasArtifact = true;
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            if (artifactVisual != null)
                artifactVisual.SetActive(false);
            else
                gameObject.SetActive(false); // hide whole artifact

            Debug.Log("Artifact acquired! Now reach the extraction point.");
            // You can show UI text here
        }
    }
}