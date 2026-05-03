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

    [Tooltip("Speeds are tuned for this reference height in meters. Speeds will scale with the player's actual CharacterController height.")]
    public float referenceHeight = 1.0f;

    [Header("Footsteps")]
    public AudioClip walkClip;
    [Range(0f, 1f)] public float footstepVolume = 0.7f;
    public float walkPitch = 1f;
    public float sprintPitch = 1.3f;
    public float crouchPitch = 0.75f;
    public float minMoveSpeedToPlay = 0.1f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.15f;
    public float maxLookAngle = 85f;

    [Header("Crouch")]
    [Range(0.3f, 0.9f)] public float crouchHeightRatio = 0.55f;
    public float minCrouchHeight = 0.5f;
    public float cameraCrouchOffset = 0f;
    public float crouchTransitionSpeed = 12f;

    private CharacterController controller;
    private Transform cameraTransform;
    private AudioSource footstepSource;
    private float verticalVelocity;
    private float xRotation;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private bool isCrouching;

    private float standingHeight;
    private float standingCameraY;
    private float crouchHeight;
    private float crouchCameraY;
    private float speedScale;
    private float airTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>().transform;

        standingHeight = controller.height;
        standingCameraY = cameraTransform.localPosition.y;
        float minValidHeight = controller.radius * 2f + 0.01f;
        crouchHeight = Mathf.Max(minCrouchHeight, standingHeight * crouchHeightRatio);
        crouchHeight = Mathf.Max(crouchHeight, minValidHeight);
        crouchHeight = Mathf.Min(crouchHeight, standingHeight - 0.05f);
        crouchCameraY = standingCameraY * (crouchHeight / standingHeight) + cameraCrouchOffset;

        speedScale = Mathf.Max(0.2f, standingHeight / Mathf.Max(0.1f, referenceHeight));

        footstepSource = GetComponent<AudioSource>();
        if (footstepSource == null) footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = walkClip;
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
        footstepSource.volume = footstepVolume;
        footstepSource.spatialBlend = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleCrouchInput();
        HandleMouseLook();
        HandleMovement();
        UpdateCameraHeight();
        UpdateFootsteps();

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
        float oldHeight = controller.height;
        float newHeight = crouched ? crouchHeight : standingHeight;
        // Preserve foot position: capsule bottom = center.y - height/2 must stay constant.
        float newCenterY = controller.center.y + (newHeight - oldHeight) * 0.5f;
        controller.height = newHeight;
        controller.center = new Vector3(controller.center.x, newCenterY, controller.center.z);
    }

    bool CanStandUp()
    {
        float gap = standingHeight - controller.height;
        if (gap <= 0.01f) return true;
        Vector3 origin = transform.position + Vector3.up * (controller.height + 0.01f);
        return !Physics.Raycast(origin, Vector3.up, gap, ~0, QueryTriggerInteraction.Ignore);
    }

    void UpdateFootsteps()
    {
        if (footstepSource == null || footstepSource.clip == null) return;

        if (controller.isGrounded) airTime = 0f;
        else airTime += Time.deltaTime;

        bool moving = moveInput.sqrMagnitude > 0.01f;
        bool onGround = airTime < 0.12f;

        if (moving && onGround)
        {
            float pitch = isCrouching ? crouchPitch : (isSprinting ? sprintPitch : walkPitch);
            footstepSource.pitch = pitch;
            footstepSource.volume = footstepVolume * (isCrouching ? 0.4f : 1f);
            if (!footstepSource.isPlaying) footstepSource.Play();
        }
        else if (footstepSource.isPlaying)
        {
            footstepSource.Pause();
        }
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
        speed *= speedScale;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
