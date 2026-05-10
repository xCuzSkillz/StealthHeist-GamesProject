using UnityEngine;

public class CameraDetection : MonoBehaviour
{
    private CameraController controller;

    [Header("Raycast Settings")]
    public Transform cameraEye;
    public LayerMask obstacleMask;
    public LayerMask playerMask;

    private bool playerInside = false;
    private Transform player;

    void Start()
    {
        controller = GetComponentInParent<CameraController>();
    }

    void Update()
    {
        if (!controller.enableDetection) return;

        if (playerInside)
        {
            CheckLineOfSight();
        }
    }

    void CheckLineOfSight()
    {
        Vector3 direction = (player.position - cameraEye.position).normalized;

        float distance = Vector3.Distance(cameraEye.position, player.position);

        Ray ray = new Ray(cameraEye.position, direction);

        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            // If ray hits player first
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
            {
                UIManager.Instance.ShowDetectionMessage();
            }
            else
            {
                UIManager.Instance.HideDetectionMessage();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!controller.enableDetection) return;

        if (other.CompareTag("Player"))
        {
            playerInside = true;
            player = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            player = null;

            UIManager.Instance.HideDetectionMessage();
        }
    }
}