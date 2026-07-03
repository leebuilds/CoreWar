using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// First-person Rigidbody controller with grid building.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class SimpleFlyCamera : MonoBehaviour
{
    [Header("References")]
    public Camera viewCamera;
    public Transform cameraPivot;
    public VoxelLightingWorld voxelWorld;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 40f;
    public float airAcceleration = 14f;
    public float jumpVelocity = 6f;
    public float lookSensitivity = 2.8f;

    [Header("Building")]
    public float buildRange = 8f;

    Rigidbody _rb;
    CapsuleCollider _capsule;
    float _yaw;
    float _pitch;
    bool _grounded;
    PhysicsMaterial _slipperyMaterial;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();

        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        if (cameraPivot == null && viewCamera != null)
        {
            cameraPivot = viewCamera.transform.parent;
        }

        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _slipperyMaterial = new PhysicsMaterial("PlayerSlide")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        _capsule.material = _slipperyMaterial;

        var euler = transform.localEulerAngles;
        _yaw = euler.y;
        _pitch = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
            return;
        }

        HandleLook();
        HandleBuildingInput();

        if (Input.GetButtonDown("Jump") && CanJump())
        {
            var velocity = _rb.linearVelocity;
            velocity.y = jumpVelocity;
            _rb.linearVelocity = velocity;
        }
    }

    void FixedUpdate()
    {
        UpdateGrounded();
        HandleMovement();
    }

    void HandleLook()
    {
        if (cameraPivot == null)
        {
            return;
        }

        _yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
        _pitch = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * lookSensitivity, -89f, 89f);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        var wishDirection = (transform.right * input.x + transform.forward * input.y);
        if (wishDirection.sqrMagnitude > 1f)
        {
            wishDirection.Normalize();
        }
        else if (wishDirection.sqrMagnitude > 0f)
        {
            wishDirection = wishDirection.normalized;
        }

        wishDirection = ProjectAgainstWall(wishDirection);

        var targetHorizontal = wishDirection * moveSpeed;

        var velocity = _rb.linearVelocity;
        var horizontal = new Vector3(velocity.x, 0f, velocity.z);

        float accel = _grounded ? acceleration : airAcceleration;
        horizontal = Vector3.MoveTowards(horizontal, targetHorizontal, accel * Time.fixedDeltaTime);

        _rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
    }

    void UpdateGrounded()
    {
        float radius = Mathf.Max(0.01f, _capsule.radius * 0.95f);
        float half = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        Vector3 top = worldCenter + Vector3.up * half;
        Vector3 bottom = worldCenter - Vector3.up * half;

        _grounded = Physics.CapsuleCast(
            top,
            bottom,
            radius,
            Vector3.down,
            out _,
            0.08f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

    }

    bool CanJump()
    {
        return _grounded && !HasCeilingClearanceBlocked();
    }

    bool HasCeilingClearanceBlocked()
    {
        float radius = Mathf.Max(0.01f, _capsule.radius * 0.92f);
        float half = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        Vector3 top = worldCenter + Vector3.up * half;
        Vector3 bottom = worldCenter - Vector3.up * half;

        return Physics.CapsuleCast(
            top,
            bottom,
            radius,
            Vector3.up,
            out _,
            0.12f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }

    Vector3 ProjectAgainstWall(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return direction;
        }

        float radius = Mathf.Max(0.01f, _capsule.radius * 0.95f);
        float half = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        Vector3 top = worldCenter + Vector3.up * half;
        Vector3 bottom = worldCenter - Vector3.up * half;

        if (Physics.CapsuleCast(top, bottom, radius, direction, out var hit, 0.3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < 0.35f && hit.normal.y > -0.2f)
            {
                return Vector3.ProjectOnPlane(direction, hit.normal).normalized;
            }
        }

        return direction;
    }

    void HandleBuildingInput()
    {
        if (viewCamera == null || voxelWorld == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceVoxel();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            TryRemoveVoxel();
        }
    }

    void TryPlaceVoxel()
    {
        if (!Physics.Raycast(viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)),
            out var hit, buildRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Vector3 targetPoint = hit.point + hit.normal * (voxelWorld.VoxelSize * 0.55f);
        Vector3Int cell = voxelWorld.WorldToCell(targetPoint);
        voxelWorld.TryPlaceVoxel(cell);
    }

    void TryRemoveVoxel()
    {
        if (!Physics.Raycast(viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)),
            out var hit, buildRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        var marker = hit.collider.GetComponent<PlayerBuiltVoxel>();
        if (marker != null)
        {
            voxelWorld.TryRemovePlayerVoxel(marker);
        }
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.15f, 0.15f, 0.15f) }
        };
        GUI.Label(new Rect(12, 8, 1000, 24),
            "WASD: move   Mouse: look   Space: jump   Left Click: place voxel   Right Click: remove   Esc: menu", style);

        GUI.Label(new Rect(Screen.width * 0.5f - 6f, Screen.height * 0.5f - 12f, 12f, 24f), "+", style);
    }

}
