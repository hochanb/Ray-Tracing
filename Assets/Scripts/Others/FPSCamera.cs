using UnityEngine;

public class FPSCamera : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 100f; // Adjust sensitivity
    public Transform playerBody;         // Reference to the player's body (parent object)
    public bool lockCursor = true;       // Whether to lock the cursor

    private float xRotation = 0f;        // Track the vertical rotation to clamp it

    bool run;


    void Start()
    {
        // Lock and hide the cursor if enabled
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
            run = !run;

        if (!run) return;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Adjust vertical rotation and clamp it
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply rotations
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // Vertical rotation (up/down)
        playerBody.Rotate(Vector3.up * mouseX);                        // Horizontal rotation (left/right)
        
    }
} 
