using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPivot : MonoBehaviour
{
    public Transform target;              // Player
    public Transform pivot;               // CameraPivot
    public float distance = 3f;           // khoảng cách camera
    
    // LƯU Ý: Với Input System, Pointer delta có giá trị trả về khác với Input cũ.
    // Nếu trong game bạn thấy camera xoay quá nhanh hoặc chậm, bạn hãy tự điều chỉnh
    // giá trị mouseSensitivity này trong inspector nhé.
    public float mouseSensitivity = 100f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    float yaw;    // xoay trái/phải
    float pitch;  // xoay lên/xuống

    private InputAction lookAction;

    void Awake()
    {
        // Khởi tạo InputAction cho Look (camera)
        lookAction = new InputAction("Look", InputActionType.Value);
        // Lấy delta của chuột (hoặc touch)
        lookAction.AddBinding("<Pointer>/delta");
        // Hỗ trợ thêm cho Cần gạt bên phải của Gamepad
        lookAction.AddBinding("<Gamepad>/rightStick");
    }

    void OnEnable()
    {
        lookAction.Enable();
    }

    void OnDisable()
    {
        lookAction.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        HandleCameraRotation();
        FollowTarget();
    }

    void HandleCameraRotation()
    {
        // Lấy giá trị input x và y
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // Thay cho Input.GetAxis("Mouse X") và ("Mouse Y")
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // Xoay yaw quanh player
        yaw += mouseX;

        // Xoay pitch lên xuống
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Apply rotation
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        pivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void FollowTarget()
    {
        transform.position = target.position;
    }
}
