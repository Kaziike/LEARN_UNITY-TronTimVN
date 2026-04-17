using UnityEngine;

public class CameraPivot : MonoBehaviour
{
    public Transform target;              // Player
    public Transform pivot;               // CameraPivot
    public float distance = 3f;           // khoảng cách camera
    public float mouseSensitivity = 200f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    float yaw;    // xoay trái/phải
    float pitch;  // xoay lên/xuống

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
        // Lấy chuột
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

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
