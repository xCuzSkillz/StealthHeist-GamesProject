using UnityEngine;

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

    void Update()
    {
        if (!enableRotation || rotationMode == RotationMode.Static)
        {
            ApplyRotation(currentAngle);
            return;
        }

        switch (rotationMode)
        {
            case RotationMode.PingPong:
                HandlePingPong();
                break;

            case RotationMode.Loop:
                HandleLoop();
                break;
        }

        ApplyRotation(currentAngle);
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

    void ApplyRotation(float angle)
    {
        transform.localRotation = Quaternion.Euler(verticalAngle, angle, 0);
    }
}