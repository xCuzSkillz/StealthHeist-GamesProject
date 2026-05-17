using UnityEngine;

namespace StealthHeist.Cameras
{
public class CameraDetection : MonoBehaviour
{
    private CameraController controller;

    [Header("Detection Geometry")]
    public Transform cameraEye;
    public float viewDistance = 10f;
    [Range(0f, 180f)] public float viewAngle = 90f;

    [Header("Layers")]
    public LayerMask obstacleMask;
    public LayerMask playerMask;

    [Header("Audio (optional)")]
    public AudioSource alertSource;

    private bool playerDetected;

    void Start()
    {
        controller = GetComponentInParent<CameraController>();
        if (cameraEye == null && controller != null && controller.visionCone != null)
            cameraEye = controller.visionCone.transform;
        if (cameraEye == null) cameraEye = transform;
    }

    void Update()
    {
        if (controller == null || !controller.enableDetection) return;

        bool sees = CanSeePlayer();

        if (sees && !playerDetected)
        {
            playerDetected = true;
            if (UIManager.Instance != null) UIManager.Instance.ShowDetectionMessage();
            if (alertSource != null && !alertSource.isPlaying) alertSource.Play();
        }
        else if (!sees && playerDetected)
        {
            playerDetected = false;
            if (UIManager.Instance != null) UIManager.Instance.HideDetectionMessage();
        }
    }

    bool CanSeePlayer()
    {
        Collider[] hits = Physics.OverlapSphere(cameraEye.position, viewDistance, playerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Vector3 toPlayer = hits[i].transform.position - cameraEye.position;
            float dist = toPlayer.magnitude;
            if (dist < 0.001f) continue;

            Vector3 dir = toPlayer / dist;
            if (Vector3.Angle(cameraEye.forward, dir) > viewAngle * 0.5f) continue;

            if (obstacleMask.value != 0 &&
                Physics.Raycast(cameraEye.position, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore))
                continue;

            return true;
        }
        return false;
    }
}
}
