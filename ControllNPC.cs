using UnityEngine;

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
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 camForward = cameraRoot.forward;
        camForward.y = 0;

        Vector3 camRight = cameraRoot.right;
        camRight.y = 0;

        Vector3 moveDir = (camForward * v + camRight * h).normalized;

        // chạy nhanh
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        if (moveDir.magnitude > 0.1f)
        {
            controller.Move(moveDir * speed * Time.deltaTime);

            // Nhân vật xoay theo hướng chạy
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
        // Animation
        if (moveDir.magnitude >= 0.1f && Input.GetKey(KeyCode.W))
        {
            animator.SetBool("Speed", true);
        }
        
        if (moveDir.magnitude < 0.1f)
        {
            animator.SetBool("Speed", false);
        }
        if (Input.GetKeyDown(KeyCode.Space) &&  IsReallyGrounded())
        {
            velocity.y = Mathf.Sqrt(-2f * gravity * 1.5f);
            animator.SetBool("IsJumping", true);
        }else
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
