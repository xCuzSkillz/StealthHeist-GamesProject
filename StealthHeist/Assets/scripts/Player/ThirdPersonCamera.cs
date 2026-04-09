using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform player;
    public float mouseSensitivity = 2f;
    public float distance = 3f;
    public float heightOffset = 1.5f;
    private float currentX = 0f;
    private float currentY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        if (player == null) return;

        currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, -20f, 60f);

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 offset = rotation * new Vector3(0, heightOffset, -distance);
        transform.position = player.position + offset;

        transform.LookAt(player.position + Vector3.up * heightOffset);
    }
}