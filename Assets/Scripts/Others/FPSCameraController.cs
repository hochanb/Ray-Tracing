using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    [SerializeField] RayTracingManager rayTracingManager;
    [SerializeField] Focus focus;
    public float mouseSensitivity = 100f; // Sensitivity of the mouse
    public float moveSpeed = 5f;          // Speed of the movement

    private float xRotation = 0f;        // To track vertical rotation


    bool run = true;


    void Start()
    {
        // Lock the cursor to the game window
        rayTracingManager.accumulate = false;
        focus.SetFocus();
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("Press Tab to toggle Move mode");
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            run = !run;
            if (run)
            {
                rayTracingManager.accumulate = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                rayTracingManager.accumulate = true;
                Cursor.lockState = CursorLockMode.None;
            }

        }
        if (!run) return;

        // Handle camera rotation
        RotateCamera();

        // Handle movement
        MoveCamera();

        focus.SetFocus();

    }

    void RotateCamera()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate the camera around the X-axis (pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Clamp to prevent over-rotation
        transform.localRotation = Quaternion.Euler(xRotation, transform.localRotation.eulerAngles.y, 0f);

        // Rotate the camera around the Y-axis (yaw)
        transform.Rotate(Vector3.up * mouseX, Space.World);
    }

    void MoveCamera()
    {
        // Get WASD input
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

        moveX = moveX > 0.1f ? 1 : moveX < -0.1f ? -1 : 0;
        moveZ = moveZ > 0.1f ? 1 : moveZ < -0.1f ? -1 : 0;

        // Calculate movement direction relative to the camera's orientation
        Vector3 moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;

        // Move the camera
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
}
