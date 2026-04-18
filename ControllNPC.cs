using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ControllNPC : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;
    public bool canMove = true; // Added to control movement lock

    public Transform cameraRoot;   // CameraRoot (not main camera)
    public Animator animator;
    CharacterController controller;
    Vector3 velocity;

    // Khai báo các Input Actions
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction jumpAction;

    void Awake()
    {
        // Khởi tạo Input Actions thông qua code để không cần phụ thuộc vào file InputActionAsset trong Editor
        
        // Di chuyển (WASD / Mũi tên / Left Stick trên Gamepad)
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        // Chạy nhanh (Left Shift hoặc Left Trigger trên Gamepad)
        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftTrigger");

        // Nhảy (Space hoặc Nút South/X(PS)/A(Xbox) trên Gamepad)
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
        jumpAction.Disable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (canMove)
        {
            HandleMovement();
        }
        else
        {
            // Vẫn áp dụng gia tốc trọng trường nhưng khóa input đi lại, ngưng animation
            if (animator != null) animator.SetBool("Speed", false);
        }
        ApplyGravity();
       
    }

    void HandleMovement()
    {
        // Đọc giá trị di chuyển thay vì dùng Input.GetAxis
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float h = moveInput.x;
        float v = moveInput.y;

        Vector3 camForward = cameraRoot.forward;
        camForward.y = 0;

        Vector3 camRight = cameraRoot.right;
        camRight.y = 0;

        Vector3 moveDir = (camForward * v + camRight * h).normalized;

        // chạy nhanh (thay vì Input.GetKey)
        float speed = moveSpeed;
        if (sprintAction.IsPressed()) speed *= sprintMultiplier;

        if (moveDir.magnitude > 0.1f)
        {
            controller.Move(moveDir * speed * Time.deltaTime);

            // Nhân vật xoay theo hướng chạy
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
        
        // Animation
        // Trước đây check `Input.GetKey(KeyCode.W)`, giờ mình check `moveDir.magnitude >= 0.1f` 
        // cho tất cả các hướng, giúp code bao quát hơn.
        if (moveDir.magnitude >= 0.1f)
        {
            animator.SetBool("Speed", true);
        }
        else
        {
            animator.SetBool("Speed", false);
        }
        
        // Thay vì Input.GetKeyDown(KeyCode.Space)
        if (jumpAction.WasPressedThisFrame() && IsReallyGrounded())
        {
            velocity.y = Mathf.Sqrt(-2f * gravity * 1.5f);
            animator.SetBool("IsJumping", true);
        }
        else
        {
            animator.SetBool("IsJumping", false);
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    bool IsReallyGrounded()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        return Physics.Raycast(ray, 0.3f);
    }
}
