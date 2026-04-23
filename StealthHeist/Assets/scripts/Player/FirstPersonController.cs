using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float gravity = -15f;
    public float jumpHeight = 1.2f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.15f;
    public float maxLookAngle = 85f;

    [Header("Crouch")]
    public float crouchHeight = 1.1f;
    public float crouchTransitionSpeed = 12f;

    private CharacterController controller;
    private Transform cameraTransform;
    private float verticalVelocity;
    private float xRotation;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private bool isCrouching;

    private float standingHeight;
    private float standingCameraY;
    private float crouchCameraY;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>().transform;

        standingHeight = controller.height;
        standingCameraY = cameraTransform.localPosition.y;
        crouchCameraY = standingCameraY - (standingHeight - crouchHeight);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleCrouchInput();
        HandleMouseLook();
        HandleMovement();
        UpdateCameraHeight();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    void OnJump(InputValue value)
    {
        if (isCrouching) return;
        if (controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    void HandleCrouchInput()
    {
        if (Keyboard.current == null) return;
        bool wantsCrouch = Keyboard.current.leftCtrlKey.isPressed;

        if (wantsCrouch && !isCrouching)
        {
            SetCrouched(true);
        }
        else if (!wantsCrouch && isCrouching && CanStandUp())
        {
            SetCrouched(false);
        }
    }

    void SetCrouched(bool crouched)
    {
        isCrouching = crouched;
        float h = crouched ? crouchHeight : standingHeight;
        controller.height = h;
        // Keep feet planted by pinning center to half-height.
        controller.center = new Vector3(controller.center.x, h * 0.5f, controller.center.z);
    }

    bool CanStandUp()
    {
        float gap = standingHeight - controller.height;
        if (gap <= 0.01f) return true;
        // Raycast from just above the crouched capsule upward; if anything is in the gap, stay crouched.
        Vector3 origin = transform.position + Vector3.up * (controller.height + 0.01f);
        return !Physics.Raycast(origin, Vector3.up, gap, ~0, QueryTriggerInteraction.Ignore);
    }

    void UpdateCameraHeight()
    {
        float target = isCrouching ? crouchCameraY : standingCameraY;
        Vector3 local = cameraTransform.localPosition;
        local.y = Mathf.Lerp(local.y, target, Time.deltaTime * crouchTransitionSpeed);
        cameraTransform.localPosition = local;
    }

    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        float speed = isCrouching ? crouchSpeed
                    : isSprinting ? sprintSpeed
                                  : moveSpeed;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
