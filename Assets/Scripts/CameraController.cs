using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The center point the camera revolves around.")]
    public Transform target;

    [Header("Distance & Zoom")]
    public float distance = 7.0f;
    public float minDistance = 3.0f;
    public float maxDistance = 15.0f;
    public float zoomSpeedMouse = 5.0f;
    public float zoomSpeedTouch = 0.05f;
    public float zoomSmoothTime = 0.1f; // How "heavy" the zoom feels

    [Header("Rotation Speed")]
    public float xSpeedMouse = 200.0f;
    public float ySpeedMouse = 200.0f;
    public float xSpeedTouch = 0.2f;
    public float ySpeedTouch = 0.2f;
    public float rotationSmoothTime = 0.12f; // How "heavy" the rotation feels

    [Header("Angle Limits")]
    public float yMinLimit = 15f;
    public float yMaxLimit = 85f;

    // --- State Variables ---
    private float targetX = 0.0f;
    private float targetY = 0.0f;
    private float targetDistance;

    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float currentDistance;

    // --- Velocity References for SmoothDamp ---
    private float xVelocity = 0.0f;
    private float yVelocity = 0.0f;
    private float zoomVelocity = 0.0f;

    // --- Starting Default Values (For Reset) ---
    private float startX;
    private float startY;
    private float startDistance;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        targetX = currentX = angles.y;
        targetY = currentY = angles.x;
        targetDistance = currentDistance = distance;

        // Save initial state for the Reset Button
        startX = targetX;
        startY = targetY;
        startDistance = targetDistance;

        if (target == null)
        {
            GameObject centerTarget = new GameObject("Camera Focal Point");
            centerTarget.transform.position = Vector3.zero;
            target = centerTarget.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // --- 1. CAPTURE DESKTOP CONTROLS ---
        if (Input.GetMouseButton(1)) // Right-Click
        {
            targetX += Input.GetAxis("Mouse X") * xSpeedMouse * 0.02f;
            targetY -= Input.GetAxis("Mouse Y") * ySpeedMouse * 0.02f;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= scroll * zoomSpeedMouse;
        }

        // --- 2. CAPTURE MOBILE TOUCH CONTROLS ---
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Pinch
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;
            targetDistance -= difference * zoomSpeedTouch;

            // Two-Finger Drag
            if (touchZero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved)
            {
                Vector2 averageDelta = (touchZero.deltaPosition + touchOne.deltaPosition) / 2.0f;
                targetX += averageDelta.x * xSpeedTouch;
                targetY -= averageDelta.y * ySpeedTouch;
            }
        }

        // --- 3. CLAMP TARGETS ---
        targetY = ClampAngle(targetY, yMinLimit, yMaxLimit);
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

        // --- 4. APPLY INERTIA (SMOOTH DAMPING) ---
        // This is what makes it feel like a Rigidbody gliding to a stop
        currentX = Mathf.SmoothDamp(currentX, targetX, ref xVelocity, rotationSmoothTime);
        currentY = Mathf.SmoothDamp(currentY, targetY, ref yVelocity, rotationSmoothTime);
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);

        // --- 5. MOVE THE CAMERA ---
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -currentDistance) + target.position;

        transform.rotation = rotation;
        transform.position = position;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    // --- CALL THIS FROM YOUR UI BUTTON ---
    public void ResetCameraView()
    {
        // By changing the targets, the camera will smoothly glide back to the start!
        targetX = startX;
        targetY = startY;
        targetDistance = startDistance;
    }
}