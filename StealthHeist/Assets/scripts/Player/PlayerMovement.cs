using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float sprintSpeed = 7.5f;
    public float sneakSpeed = 1.5f;
    public float turnSmoothTime = 0.1f;

    private Rigidbody rb;
    private Transform cameraPivot;
    private float turnSmoothVelocity;
    private float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cameraPivot = transform.parent.Find("CameraPivot");
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        // Movement input (WASD)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Crouch / Sneak
        if (Input.GetKey(KeyCode.LeftControl))
            currentSpeed = sneakSpeed;
        else if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed = sprintSpeed;
        else
            currentSpeed = walkSpeed;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
                                + cameraPivot.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }
}