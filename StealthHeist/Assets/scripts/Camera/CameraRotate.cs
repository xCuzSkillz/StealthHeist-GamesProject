using UnityEngine;

public class CameraRotate : MonoBehaviour
{
    public float rotationSpeed = 30f;
    public float maxAngle = 45f; // 45 + 45 = 90 total

    private float currentAngle = 0f;
    private int direction = 1;

    void Update()
    {
        float rotation = rotationSpeed * Time.deltaTime * direction;
        transform.Rotate(0, rotation, 0);

        currentAngle += rotation;

        if (Mathf.Abs(currentAngle) >= maxAngle)
        {
            direction *= -1; // flip direction
        }
    }
}