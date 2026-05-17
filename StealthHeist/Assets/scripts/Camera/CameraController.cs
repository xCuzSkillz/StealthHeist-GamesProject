using UnityEngine;

namespace StealthHeist.Cameras
{
[ExecuteAlways]
[SelectionBase]
public class CameraController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public bool enableRotation = true;

    public enum RotationMode { PingPong, Loop, Static }
    public RotationMode rotationMode = RotationMode.PingPong;

    public float rotationSpeed = 2f;
    public float maxAngle = 45f;
    public float startAngle = 0f;
    public bool invertDirection = false;
    public float verticalAngle = 0f;

    [Header("Pause Settings")]
    public float pauseTime = 0f;

    [Header("Vision Settings")]
    public GameObject visionCone;
    public bool showVisionCone = true;

    [Header("Cone Aim (relative to this GameObject)")]
    [Tooltip("Where the cone tip sits, relative to this GameObject. Tune so the tip is on the camera lens.")]
    public Vector3 coneLocalPosition = Vector3.zero;
    [Tooltip("Cone rotation, relative to this GameObject.\n" +
             "X (Pitch): negative = look UP, positive = look DOWN\n" +
             "Y (Yaw):   negative = turn LEFT, positive = turn RIGHT\n" +
             "Z (Roll):  twist the cone sideways (usually leave at 0)")]
    public Vector3 coneLocalEuler = Vector3.zero;

    [Header("Detection Settings")]
    public bool enableDetection = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private float currentAngle;
    private int direction = 1;
    private float pauseTimer = 0f;

    void Start()
    {
        currentAngle = startAngle;

        if (invertDirection)
            direction = -1;

        if (visionCone != null)
            visionCone.SetActive(showVisionCone);
    }

    void LateUpdate()
    {
        if (visionCone == null) return;
        visionCone.transform.localPosition = coneLocalPosition;

        // Apply sweep as an additive yaw offset on top of the tuned aim
        Vector3 finalEuler = coneLocalEuler;
        if (enableRotation && rotationMode != RotationMode.Static)
            finalEuler.y += currentAngle;

        visionCone.transform.localRotation = Quaternion.Euler(finalEuler);
    }

    void OnDrawGizmosSelected()
    {
        if (visionCone == null) return;
        Vector3 origin = visionCone.transform.position;
        Vector3 dir = visionCone.transform.forward;
        Gizmos.color = new Color(1f, 0.4f, 0.2f);
        Gizmos.DrawLine(origin, origin + dir * 1.5f);
        Gizmos.DrawSphere(origin + dir * 1.5f, 0.05f);
    }

    void Update()
    {
        if (!enableRotation || rotationMode == RotationMode.Static) return;

        switch (rotationMode)
        {
            case RotationMode.PingPong:
                HandlePingPong();
                break;

            case RotationMode.Loop:
                HandleLoop();
                break;
        }
        // currentAngle is the swept yaw offset; applied to the cone in LateUpdate
    }

    void HandlePingPong()
    {
        if (pauseTimer > 0)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }

        currentAngle += rotationSpeed * direction * Time.deltaTime;

        if (Mathf.Abs(currentAngle) >= maxAngle)
        {
            currentAngle = Mathf.Clamp(currentAngle, -maxAngle, maxAngle);
            direction *= -1;

            if (pauseTime > 0)
                pauseTimer = pauseTime;

            if (showDebugLogs)
                Debug.Log("Camera reached edge, switching direction");
        }
    }

    void HandleLoop()
    {
        currentAngle += rotationSpeed * Time.deltaTime;

        if (currentAngle >= 360f)
            currentAngle -= 360f;
    }

}
}