using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public TextMeshProUGUI artifactText;

    void Update()
    {
        artifactText.text = ArtifactPickup.hasArtifact ? "Objective: Exfiltrate the level" : "Objective: Find the artifact";
        artifactText.color = ArtifactPickup.hasArtifact ? Color.green : Color.red;
        artifactText.alignment = TextAlignmentOptions.MidlineLeft;
    }
}
