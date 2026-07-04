using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// First-person Rigidbody controller with camera-relative movement and grid building.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class ThirdPersonController : MonoBehaviour
{
    static readonly VoxelLightingWorld.BuildPieceType[] BuildPieceOptions =
    {
        VoxelLightingWorld.BuildPieceType.Wall,
        VoxelLightingWorld.BuildPieceType.Window,
        VoxelLightingWorld.BuildPieceType.Ceiling,
        VoxelLightingWorld.BuildPieceType.Door,
        VoxelLightingWorld.BuildPieceType.TrapDoor,
        VoxelLightingWorld.BuildPieceType.Ladder
    };

    static readonly Vector3Int[] BuildFaceNormals =
    {
        new Vector3Int(0, 0, 1),
        Vector3Int.right,
        new Vector3Int(0, 0, -1),
        Vector3Int.left
    };

    static readonly Vector3Int[] HorizontalAxes =
    {
        Vector3Int.right,
        new Vector3Int(0, 0, 1)
    };

    [Header("References")]
    public Camera viewCamera;
    public Transform cameraYawPivot;
    public Transform cameraPitchPivot;
    public Transform characterVisual;
    public VoxelLightingWorld voxelWorld;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 40f;
    public float airAcceleration = 14f;
    public float jumpVelocity = 6f;
    public float lookSensitivity = 2.8f;
    public float turnSpeed = 12f;

    [Header("Camera")]
    public Vector3 firstPersonCameraOffset = Vector3.zero;
    public float eyeHeight = 1.62f;
    public float fieldOfView = 75f;
    public bool hideLocalCharacterVisual = true;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Building")]
    public float buildRange = 8f;
    public int buildSnapRadius = 2;
    public int rectangleMaxCells = 12;

    [Header("Reticle")]
    public float crosshairGap = 5f;
    public float crosshairLength = 10f;
    public float crosshairThickness = 2f;
    public Color crosshairColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);

    [Header("Build Mode")]
    public Color validPreviewColor = new Color(0.1f, 0.85f, 0.2f, 0.9f);
    public Color invalidPreviewColor = new Color(0.9f, 0.08f, 0.08f, 0.9f);
    public float selectorMouseScale = 20f;
    public float selectorActivationDistance = 18f;
    public float selectorRadius = 84f;

    Rigidbody _rb;
    CapsuleCollider _capsule;
    float _yaw;
    float _pitch;
    bool _grounded;
    bool _buildMode;
    bool _selectorOpen;
    Vector2 _selectorDirection;
    VoxelLightingWorld.BuildPieceType _selectedPiece = VoxelLightingWorld.BuildPieceType.Wall;
    VoxelLightingWorld.BuildPieceCandidate _buildCandidate;
    bool _hasBuildCandidate;
    bool _scrollTargetLocked;
    Vector3Int _scrollLockedCell;
    bool _mouseMovedThisFrame;
    readonly List<GameObject> _previewRoots = new List<GameObject>();
    readonly List<VoxelLightingWorld.BuildPieceCandidate> _rectangleCandidates =
        new List<VoxelLightingWorld.BuildPieceCandidate>();
    bool[] _rectangleValidFlags = new bool[0];
    bool _rectangleDragActive;
    bool _rectangleAllValid;
    VoxelLightingWorld.BuildPieceCandidate _rectangleStartCandidate;
    VoxelLightingWorld.BuildPieceCandidate _rectangleEndCandidate;
    Vector3Int _rectangleFaceNormal;
    Vector3Int _rectangleWidthAxis;
    int _buildOrientationIndex;
    Texture2D _radialTexture;
    VoxelLightingWorld.BuildPieceType _radialTexturePiece;
    PhysicsMaterial _slipperyMaterial;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();

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

        _yaw = transform.eulerAngles.y;
        _pitch = 18f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        UpdateCameraTransform();
        UpdateCharacterAim();
        UpdateBuildPreview();
    }

    void Start()
    {
        if (viewCamera != null)
        {
            viewCamera.fieldOfView = fieldOfView;
        }

        if (hideLocalCharacterVisual && characterVisual != null)
        {
            foreach (var renderer in characterVisual.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
        }
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
        HandleBuildModeToggle();
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
        Vector2 lookDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        _mouseMovedThisFrame = lookDelta.sqrMagnitude > 0.0001f;

        if (_selectorOpen)
        {
            return;
        }

        _yaw += lookDelta.x * lookSensitivity;
        _pitch = Mathf.Clamp(_pitch - lookDelta.y * lookSensitivity, minPitch, maxPitch);
    }

    void UpdateCameraTransform()
    {
        if (cameraYawPivot == null || cameraPitchPivot == null || viewCamera == null)
        {
            return;
        }

        cameraYawPivot.position = transform.position + Vector3.up * eyeHeight;
        cameraYawPivot.rotation = Quaternion.Euler(0f, _yaw, 0f);
        cameraPitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        viewCamera.transform.localPosition = firstPersonCameraOffset;
        viewCamera.transform.localRotation = Quaternion.identity;
    }

    void UpdateCharacterAim()
    {
        if (characterVisual == null || viewCamera == null)
        {
            return;
        }

        Vector3 aimDirection = viewCamera.transform.forward;
        aimDirection.y = 0f;
        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
        characterVisual.rotation = Quaternion.Slerp(characterVisual.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    void HandleMovement()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 wishDirection = Vector3.zero;

        if (viewCamera != null && input.sqrMagnitude > 0.0001f)
        {
            Vector3 camForward = viewCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = viewCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();
            wishDirection = camForward * input.y + camRight * input.x;
            if (wishDirection.sqrMagnitude > 1f)
            {
                wishDirection.Normalize();
            }
        }

        wishDirection = ProjectAgainstWall(wishDirection);
        var targetHorizontal = wishDirection * moveSpeed;

        var velocity = _rb.linearVelocity;
        var horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float accel = _grounded ? acceleration : airAcceleration;
        horizontal = Vector3.MoveTowards(horizontal, targetHorizontal, accel * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

        // Character facing follows the crosshair in LateUpdate.
    }

    void UpdateGrounded()
    {
        float radius = Mathf.Max(0.01f, _capsule.radius * 0.95f);
        float half = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        Vector3 top = worldCenter + Vector3.up * half;
        Vector3 bottom = worldCenter - Vector3.up * half;

        _grounded = Physics.CapsuleCast(
            top, bottom, radius, Vector3.down, out _, 0.08f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    bool CanJump() => _grounded && !HasCeilingClearanceBlocked();

    bool HasCeilingClearanceBlocked()
    {
        float radius = Mathf.Max(0.01f, _capsule.radius * 0.92f);
        float half = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        Vector3 top = worldCenter + Vector3.up * half;
        Vector3 bottom = worldCenter - Vector3.up * half;

        return Physics.CapsuleCast(
            top, bottom, radius, Vector3.up, out _, 0.12f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
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

        if (!_buildMode)
        {
            _hasBuildCandidate = false;
            _scrollTargetLocked = false;
            return;
        }

        if (_mouseMovedThisFrame)
        {
            _scrollTargetLocked = false;
        }

        HandleBuildOrientationInput();
        UpdateBuildCandidate();
        HandleBuildSelectorInput();

        if (HandleRectangleBuildInput())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && !_selectorOpen && _hasBuildCandidate && _buildCandidate.CanPlace)
        {
            voxelWorld.TryPlaceBuildPiece(_buildCandidate);
            UpdateBuildCandidate();
        }
    }

    void HandleBuildModeToggle()
    {
        if (!Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        _buildMode = !_buildMode;
        _selectorOpen = false;
        _hasBuildCandidate = false;
        _rectangleDragActive = false;
        _scrollTargetLocked = false;
        if (!_buildMode)
        {
            DestroyPreviewRootsFrom(0);
        }
    }

    void HandleBuildOrientationInput()
    {
        if (!HasBuildOrientation(_selectedPiece))
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.01f)
        {
            TryLockScrollTarget();
            _buildOrientationIndex = (_buildOrientationIndex + 1) % BuildFaceNormals.Length;
        }
        else if (scroll < -0.01f)
        {
            TryLockScrollTarget();
            _buildOrientationIndex = (_buildOrientationIndex + BuildFaceNormals.Length - 1) % BuildFaceNormals.Length;
        }
    }

    void TryLockScrollTarget()
    {
        if (_mouseMovedThisFrame || _scrollTargetLocked || !_hasBuildCandidate || !_buildCandidate.HasTarget)
        {
            return;
        }

        _scrollLockedCell = _buildCandidate.Cell;
        _scrollTargetLocked = true;
    }

    bool HandleRectangleBuildInput()
    {
        if (!SupportsRectangleDrag(_selectedPiece))
        {
            _rectangleDragActive = false;
            return false;
        }

        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (!ctrlHeld)
        {
            if (_rectangleDragActive)
            {
                DestroyPreviewRootsFrom(1);
            }
            _rectangleDragActive = false;
            return false;
        }

        if (Input.GetMouseButtonDown(0) && !_selectorOpen && _hasBuildCandidate && _buildCandidate.HasTarget)
        {
            _rectangleDragActive = true;
            _rectangleStartCandidate = _buildCandidate;
            _rectangleEndCandidate = _buildCandidate;
            _rectangleFaceNormal = _buildCandidate.FaceNormal;
            _rectangleWidthAxis = _selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling
                ? Vector3Int.zero
                : CameraPreferredWidthAxis(_rectangleFaceNormal);
            RebuildRectangleCandidates();
            return true;
        }

        if (!_rectangleDragActive)
        {
            return false;
        }

        if (TryGetRectangleEndCandidate(out var endCandidate))
        {
            _rectangleEndCandidate = endCandidate;
            RebuildRectangleCandidates();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_rectangleAllValid)
            {
                voxelWorld.TryPlaceBuildPieceBatch(_rectangleCandidates);
                UpdateBuildCandidate();
            }

            _rectangleDragActive = false;
            DestroyPreviewRootsFrom(1);
        }

        return true;
    }

    void HandleBuildSelectorInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _selectorOpen = true;
            _selectorDirection = Vector2.zero;
        }

        if (_selectorOpen && Input.GetMouseButton(1))
        {
            _selectorDirection += new Vector2(
                Input.GetAxisRaw("Mouse X"),
                Input.GetAxisRaw("Mouse Y")) * selectorMouseScale;

            if (_selectorDirection.magnitude >= selectorActivationDistance)
            {
                SelectPieceFromDirection(_selectorDirection);
            }
        }

        if (_selectorOpen && Input.GetMouseButtonUp(1))
        {
            _selectorOpen = false;
        }
    }

    void SelectPieceFromDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var nextPiece = PieceFromRadialAngle(angle);
        if (nextPiece != _selectedPiece)
        {
            _scrollTargetLocked = false;
            _selectedPiece = nextPiece;
        }
    }

    void UpdateBuildCandidate()
    {
        Vector3Int faceNormal = HasBuildOrientation(_selectedPiece)
            ? BuildFaceNormals[_buildOrientationIndex]
            : Vector3Int.zero;
        Ray buildRay = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (_scrollTargetLocked && HasBuildOrientation(_selectedPiece))
        {
            _hasBuildCandidate = voxelWorld.TryCreateBuildPieceCandidate(
                _selectedPiece,
                _scrollLockedCell,
                faceNormal,
                out _buildCandidate);

            if (_hasBuildCandidate && _buildCandidate.HasTarget &&
                _buildCandidate.CanPlace &&
                !HasLineOfSightToBuildCandidate(_buildCandidate))
            {
                _buildCandidate.CanPlace = false;
            }
            return;
        }

        _hasBuildCandidate = voxelWorld.TryGetBuildPieceCandidate(
            buildRay,
            buildRange,
            _selectedPiece,
            faceNormal,
            out _buildCandidate);

        if (_hasBuildCandidate && _buildCandidate.HasTarget &&
            (!_buildCandidate.CanPlace || !HasLineOfSightToBuildCandidate(_buildCandidate)) &&
            (TryFindVisibleSideBuildCandidate(buildRay, _buildCandidate, out var snappedCandidate) ||
            TryFindSnappedBuildCandidate(_buildCandidate, out snappedCandidate)))
        {
            _buildCandidate = snappedCandidate;
        }

        if (_hasBuildCandidate && _buildCandidate.HasTarget &&
            (_buildCandidate.CanPlace || voxelWorld.IsBuildSurfaceOccupied(_buildCandidate)) &&
            !HasLineOfSightToBuildCandidate(_buildCandidate))
        {
            _buildCandidate.CanPlace = false;
        }

        if (_hasBuildCandidate && _buildCandidate.HasTarget && !_buildCandidate.CanPlace &&
            voxelWorld.IsBuildSurfaceOccupied(_buildCandidate))
        {
            _hasBuildCandidate = false;
        }
    }

    bool TryFindSnappedBuildCandidate(
        VoxelLightingWorld.BuildPieceCandidate rawCandidate,
        out VoxelLightingWorld.BuildPieceCandidate snappedCandidate)
    {
        snappedCandidate = rawCandidate;

        if (rawCandidate.CanPlace)
        {
            return true;
        }

        float bestDistance = float.MaxValue;
        bool found = false;
        int radius = Mathf.Max(0, buildSnapRadius);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    var cell = rawCandidate.Cell + new Vector3Int(x, y, z);
                    if (!voxelWorld.TryCreateBuildPieceCandidate(
                        rawCandidate.PieceType,
                        cell,
                        rawCandidate.FaceNormal,
                        out var candidate) ||
                        !candidate.CanPlace ||
                        !HasLineOfSightToBuildCandidate(candidate))
                    {
                        continue;
                    }

                    float distance = CrosshairDistance(candidate);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        snappedCandidate = candidate;
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    bool TryFindVisibleSideBuildCandidate(
        Ray buildRay,
        VoxelLightingWorld.BuildPieceCandidate rawCandidate,
        out VoxelLightingWorld.BuildPieceCandidate snappedCandidate)
    {
        snappedCandidate = rawCandidate;

        if (!CanUseVisibleSideSuggestion(rawCandidate.PieceType) ||
            !Physics.Raycast(buildRay, out var hit, buildRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3Int visibleNormal = QuantizeNormal(hit.normal);
        if (rawCandidate.PieceType == VoxelLightingWorld.BuildPieceType.Ceiling ||
            rawCandidate.PieceType == VoxelLightingWorld.BuildPieceType.TrapDoor)
        {
            return TryFindVisibleSideHorizontalCandidate(rawCandidate, visibleNormal, out snappedCandidate);
        }

        if (visibleNormal.y != 0)
        {
            return false;
        }

        Vector3Int startCell = rawCandidate.Cell + visibleNormal;
        bool orientationLocked = Input.GetKey(KeyCode.LeftShift);
        Vector3Int faceNormal = orientationLocked ? rawCandidate.FaceNormal : visibleNormal;
        var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
        if (!orientationLocked && marker != null && marker.IsPanelPiece && marker.FaceNormal.y == 0)
        {
            startCell = marker.Cell + visibleNormal;
            faceNormal = marker.FaceNormal;
        }

        Vector3Int widthAxis = faceNormal.x != 0 ? new Vector3Int(0, 0, 1) : Vector3Int.right;
        return TrySearchVisibleSideCandidates(
            rawCandidate.PieceType,
            startCell,
            faceNormal,
            visibleNormal,
            widthAxis,
            true,
            out snappedCandidate);
    }

    bool TryFindVisibleSideHorizontalCandidate(
        VoxelLightingWorld.BuildPieceCandidate rawCandidate,
        Vector3Int visibleNormal,
        out VoxelLightingWorld.BuildPieceCandidate snappedCandidate)
    {
        snappedCandidate = rawCandidate;

        Vector3Int startCell = rawCandidate.Cell;
        Vector3Int towardPlayer = Vector3Int.zero;
        Vector3Int sideAxis = Vector3Int.right;
        if (visibleNormal.y == 0)
        {
            startCell += visibleNormal;
            towardPlayer = visibleNormal;
            sideAxis = PerpendicularHorizontalAxis(visibleNormal);
        }

        return TrySearchVisibleSideCandidates(
            rawCandidate.PieceType,
            startCell,
            Vector3Int.up,
            towardPlayer,
            sideAxis,
            false,
            out snappedCandidate);
    }

    bool TrySearchVisibleSideCandidates(
        VoxelLightingWorld.BuildPieceType pieceType,
        Vector3Int startCell,
        Vector3Int faceNormal,
        Vector3Int towardPlayerAxis,
        Vector3Int sideSearchAxis,
        bool includeVerticalOffsets,
        out VoxelLightingWorld.BuildPieceCandidate snappedCandidate)
    {
        snappedCandidate = default;

        int radius = Mathf.Max(0, buildSnapRadius);
        int towardPlayerSteps = towardPlayerAxis == Vector3Int.zero ? 0 : Mathf.Max(1, radius + 1);

        float bestDistance = float.MaxValue;
        int bestTowardPlayerStep = int.MaxValue;
        bool found = false;

        for (int step = 0; step <= towardPlayerSteps; step++)
        {
            for (int sideOffset = -radius; sideOffset <= radius; sideOffset++)
            {
                int minY = includeVerticalOffsets ? -radius : 0;
                int maxY = includeVerticalOffsets ? radius : 0;
                for (int yOffset = minY; yOffset <= maxY; yOffset++)
                {
                    Vector3Int cell = startCell + (towardPlayerAxis * step);

                    if (includeVerticalOffsets)
                    {
                        cell += sideSearchAxis * sideOffset;
                        cell += Vector3Int.up * yOffset;
                    }
                    else
                    {
                        cell += sideSearchAxis * sideOffset;
                    }

                    if (!TryUseSuggestedCandidate(pieceType, cell, faceNormal, out var candidate))
                    {
                        continue;
                    }

                    float distance = CrosshairDistance(candidate);
                    if (distance < bestDistance ||
                        (Mathf.Approximately(distance, bestDistance) && step < bestTowardPlayerStep))
                    {
                        bestDistance = distance;
                        bestTowardPlayerStep = step;
                        snappedCandidate = candidate;
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    bool TryUseSuggestedCandidate(
        VoxelLightingWorld.BuildPieceType pieceType,
        Vector3Int cell,
        Vector3Int faceNormal,
        out VoxelLightingWorld.BuildPieceCandidate candidate)
    {
        return voxelWorld.TryCreateBuildPieceCandidate(pieceType, cell, faceNormal, out candidate) &&
            candidate.CanPlace &&
            HasLineOfSightToBuildCandidate(candidate);
    }

    float CrosshairDistance(VoxelLightingWorld.BuildPieceCandidate candidate)
    {
        if (viewCamera == null)
        {
            return 0f;
        }

        Vector3 viewportPoint = viewCamera.WorldToViewportPoint(candidate.Position);
        if (viewportPoint.z < 0f)
        {
            return float.MaxValue;
        }

        float dx = viewportPoint.x - 0.5f;
        float dy = viewportPoint.y - 0.5f;
        return (dx * dx) + (dy * dy);
    }

    bool TryGetRectangleEndCandidate(out VoxelLightingWorld.BuildPieceCandidate endCandidate)
    {
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var plane = new Plane((Vector3)_rectangleFaceNormal, _rectangleStartCandidate.Position);

        if (plane.Raycast(ray, out float enter) && enter >= 0f)
        {
            Vector3 point = ray.GetPoint(enter);
            Vector3 cellPoint = point - ((Vector3)_rectangleFaceNormal * (voxelWorld.VoxelSize * 0.5f));
            Vector3Int cell = voxelWorld.WorldToCell(cellPoint);
            cell = _selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling
                ? new Vector3Int(cell.x, _rectangleStartCandidate.Cell.y, cell.z)
                : ProjectCellToRectangleAxis(_rectangleStartCandidate.Cell, cell, _rectangleWidthAxis);
            return voxelWorld.TryCreateBuildPieceCandidate(_selectedPiece, cell, _rectangleFaceNormal, out endCandidate);
        }

        endCandidate = _rectangleEndCandidate;
        return false;
    }

    static Vector3Int ProjectCellToRectangleAxis(Vector3Int start, Vector3Int raw, Vector3Int widthAxis)
    {
        if (widthAxis.x != 0)
        {
            return new Vector3Int(raw.x, raw.y, start.z);
        }

        return new Vector3Int(start.x, raw.y, raw.z);
    }

    void RebuildRectangleCandidates()
    {
        _rectangleCandidates.Clear();

        if (!_rectangleStartCandidate.HasTarget || !_rectangleEndCandidate.HasTarget)
        {
            _rectangleAllValid = false;
            return;
        }

        Vector3Int start = _rectangleStartCandidate.Cell;
        Vector3Int end = _rectangleEndCandidate.Cell;
        Vector3Int faceNormal = _rectangleFaceNormal;

        int maxCells = Mathf.Max(1, rectangleMaxCells);
        int clampedEndY = _selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling
            ? start.y
            : ClampToRange(end.y, start.y, maxCells - 1);
        int minY = Mathf.Max(1, Mathf.Min(start.y, clampedEndY));
        int maxY = Mathf.Max(start.y, clampedEndY);

        if (_selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling)
        {
            int clampedEndX = ClampToRange(end.x, start.x, maxCells - 1);
            int clampedEndZ = ClampToRange(end.z, start.z, maxCells - 1);
            int minX = Mathf.Min(start.x, clampedEndX);
            int maxX = Mathf.Max(start.x, clampedEndX);
            int minZ = Mathf.Min(start.z, clampedEndZ);
            int maxZ = Mathf.Max(start.z, clampedEndZ);

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    AddRectangleCandidate(new Vector3Int(x, start.y, z), faceNormal);
                }
            }
        }
        else if (_rectangleWidthAxis.z != 0)
        {
            int clampedEndZ = ClampToRange(end.z, start.z, maxCells - 1);
            int minZ = Mathf.Min(start.z, clampedEndZ);
            int maxZ = Mathf.Max(start.z, clampedEndZ);
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    AddRectangleCandidate(new Vector3Int(start.x, y, z), faceNormal);
                }
            }
        }
        else
        {
            int clampedEndX = ClampToRange(end.x, start.x, maxCells - 1);
            int minX = Mathf.Min(start.x, clampedEndX);
            int maxX = Mathf.Max(start.x, clampedEndX);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    AddRectangleCandidate(new Vector3Int(x, y, start.z), faceNormal);
                }
            }
        }

        if (_rectangleValidFlags.Length < _rectangleCandidates.Count)
        {
            _rectangleValidFlags = new bool[_rectangleCandidates.Count];
        }

        _rectangleAllValid = voxelWorld.ValidateBuildPieceBatch(_rectangleCandidates, _rectangleValidFlags);
    }

    void AddRectangleCandidate(Vector3Int cell, Vector3Int faceNormal)
    {
        if (voxelWorld.TryCreateBuildPieceCandidate(_selectedPiece, cell, faceNormal, out var candidate))
        {
            if (voxelWorld.IsBuildSurfaceOccupied(candidate))
            {
                return;
            }

            if (!HasLineOfSightToBuildCandidate(candidate))
            {
                candidate.CanPlace = false;
            }

            _rectangleCandidates.Add(candidate);
        }
    }

    static int ClampToRange(int value, int origin, int maxDistance)
    {
        return Mathf.Clamp(value, origin - maxDistance, origin + maxDistance);
    }

    void UpdateBuildPreview()
    {
        if (!_buildMode)
        {
            HidePreviewRoots();
            return;
        }

        if (_rectangleDragActive)
        {
            if (_rectangleCandidates.Count == 0)
            {
                HidePreviewRoots();
                return;
            }

            for (int i = 0; i < _rectangleCandidates.Count; i++)
            {
                UpdatePreviewRoot(i, _rectangleCandidates[i], _rectangleAllValid);
            }

            HidePreviewRootsFrom(_rectangleCandidates.Count);
            return;
        }

        if (!_hasBuildCandidate || !_buildCandidate.HasTarget)
        {
            HidePreviewRoots();
            return;
        }

        UpdatePreviewRoot(0, _buildCandidate, _buildCandidate.CanPlace);
        HidePreviewRootsFrom(1);
    }

    void UpdatePreviewRoot(int index, VoxelLightingWorld.BuildPieceCandidate candidate, bool valid)
    {
        GameObject root = EnsurePreviewRoot(index);
        root.SetActive(true);
        root.transform.position = candidate.Position;
        root.transform.rotation = candidate.Rotation;

        SetPreviewColor(root, valid ? validPreviewColor : invalidPreviewColor);
        UpdatePreviewBars(root, candidate.Scale);
    }

    GameObject EnsurePreviewRoot(int index)
    {
        while (_previewRoots.Count <= index)
        {
            _previewRoots.Add(CreatePreviewRoot(_previewRoots.Count));
        }

        return _previewRoots[index];
    }

    GameObject CreatePreviewRoot(int index)
    {
        var root = new GameObject($"Build Preview Outline {index + 1}");

        for (int i = 0; i < 4; i++)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = $"Preview Edge {i + 1}";
            bar.transform.SetParent(root.transform, false);
            bar.GetComponent<MeshRenderer>().sharedMaterial = CreateTransparentMaterial("Build Preview Material", validPreviewColor);
            Destroy(bar.GetComponent<Collider>());
        }

        return root;
    }

    void HidePreviewRoots()
    {
        HidePreviewRootsFrom(0);
    }

    void HidePreviewRootsFrom(int startIndex)
    {
        for (int i = startIndex; i < _previewRoots.Count; i++)
        {
            _previewRoots[i].SetActive(false);
        }
    }

    void DestroyPreviewRootsFrom(int startIndex)
    {
        for (int i = _previewRoots.Count - 1; i >= startIndex; i--)
        {
            Destroy(_previewRoots[i]);
            _previewRoots.RemoveAt(i);
        }
    }

    void SetPreviewColor(GameObject root, Color color)
    {
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var renderer = root.transform.GetChild(i).GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = color;
            }
        }
    }

    void UpdatePreviewBars(GameObject root, Vector3 scale)
    {
        float thinAxis = Mathf.Min(scale.x, Mathf.Min(scale.y, scale.z));
        float barThickness = Mathf.Max(thinAxis * 1.2f, 0.03f);

        if (scale.y <= scale.x && scale.y <= scale.z)
        {
            SetPreviewBar(root, 0, new Vector3(0f, 0f, scale.z * 0.5f), new Vector3(scale.x, barThickness, barThickness));
            SetPreviewBar(root, 1, new Vector3(0f, 0f, -scale.z * 0.5f), new Vector3(scale.x, barThickness, barThickness));
            SetPreviewBar(root, 2, new Vector3(scale.x * 0.5f, 0f, 0f), new Vector3(barThickness, barThickness, scale.z));
            SetPreviewBar(root, 3, new Vector3(-scale.x * 0.5f, 0f, 0f), new Vector3(barThickness, barThickness, scale.z));
        }
        else if (scale.x <= scale.z)
        {
            SetPreviewBar(root, 0, new Vector3(0f, scale.y * 0.5f, 0f), new Vector3(barThickness, barThickness, scale.z));
            SetPreviewBar(root, 1, new Vector3(0f, -scale.y * 0.5f, 0f), new Vector3(barThickness, barThickness, scale.z));
            SetPreviewBar(root, 2, new Vector3(0f, 0f, scale.z * 0.5f), new Vector3(barThickness, scale.y, barThickness));
            SetPreviewBar(root, 3, new Vector3(0f, 0f, -scale.z * 0.5f), new Vector3(barThickness, scale.y, barThickness));
        }
        else
        {
            SetPreviewBar(root, 0, new Vector3(0f, scale.y * 0.5f, 0f), new Vector3(scale.x, barThickness, barThickness));
            SetPreviewBar(root, 1, new Vector3(0f, -scale.y * 0.5f, 0f), new Vector3(scale.x, barThickness, barThickness));
            SetPreviewBar(root, 2, new Vector3(scale.x * 0.5f, 0f, 0f), new Vector3(barThickness, scale.y, barThickness));
            SetPreviewBar(root, 3, new Vector3(-scale.x * 0.5f, 0f, 0f), new Vector3(barThickness, scale.y, barThickness));
        }
    }

    void SetPreviewBar(GameObject root, int index, Vector3 localPosition, Vector3 localScale)
    {
        Transform bar = root.transform.GetChild(index);
        bar.localPosition = localPosition;
        bar.localRotation = Quaternion.identity;
        bar.localScale = localScale;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.15f, 0.15f, 0.15f) }
        };
        GUI.Label(new Rect(12, 8, 1100, 24),
            _buildMode
                ? BuildModeHelpText()
                : "WASD: move   Mouse: look   Space: jump   F: build mode   Esc: menu",
            style);
        DrawCrosshair();
        DrawBuildSelector();
    }

    void DrawCrosshair()
    {
        Color previousColor = GUI.color;
        GUI.color = crosshairColor;

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        float halfThickness = crosshairThickness * 0.5f;

        GUI.DrawTexture(
            new Rect(centerX - crosshairGap - crosshairLength, centerY - halfThickness, crosshairLength, crosshairThickness),
            Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(centerX + crosshairGap, centerY - halfThickness, crosshairLength, crosshairThickness),
            Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(centerX - halfThickness, centerY - crosshairGap - crosshairLength, crosshairThickness, crosshairLength),
            Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(centerX - halfThickness, centerY + crosshairGap, crosshairThickness, crosshairLength),
            Texture2D.whiteTexture);

        GUI.color = previousColor;
    }

    void DrawBuildSelector()
    {
        if (!_selectorOpen)
        {
            return;
        }

        EnsureRadialTexture();
        float size = selectorRadius * 2f;
        var rect = new Rect(Screen.width * 0.5f - selectorRadius, Screen.height * 0.5f - selectorRadius, size, size);
        GUI.DrawTexture(rect, _radialTexture);

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black }
        };

        for (int i = 0; i < BuildPieceOptions.Length; i++)
        {
            float angle = i * (360f / BuildPieceOptions.Length) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (selectorRadius * 0.72f);
            var labelRect = new Rect(rect.center.x + offset.x - 46f, rect.center.y - offset.y - 12f, 92f, 24f);
            GUI.Label(labelRect, DisplayName(BuildPieceOptions[i]).ToUpperInvariant(), labelStyle);
        }
    }

    void EnsureRadialTexture()
    {
        if (_radialTexture != null && _radialTexturePiece == _selectedPiece)
        {
            return;
        }

        const int size = 192;
        if (_radialTexture == null)
        {
            _radialTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        _radialTexturePiece = _selectedPiece;
        float center = (size - 1) * 0.5f;
        float radius = size * 0.48f;
        float innerRadius = size * 0.16f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = new Vector2(x - center, y - center);
                float distance = offset.magnitude;
                if (distance > radius || distance < innerRadius)
                {
                    _radialTexture.SetPixel(x, y, Color.clear);
                    continue;
                }

                VoxelLightingWorld.BuildPieceType sector = PieceFromRadialAngle(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
                Color color = sector == _selectedPiece
                    ? new Color(0.1f, 0.85f, 0.2f, 0.45f)
                    : new Color(0.98f, 0.98f, 0.98f, 0.36f);
                _radialTexture.SetPixel(x, y, color);
            }
        }

        _radialTexture.Apply();
    }

    VoxelLightingWorld.BuildPieceType PieceFromRadialAngle(float angle)
    {
        float sectorSize = 360f / BuildPieceOptions.Length;
        float normalized = Mathf.Repeat(angle + (sectorSize * 0.5f), 360f);
        int index = Mathf.FloorToInt(normalized / sectorSize);
        return BuildPieceOptions[Mathf.Clamp(index, 0, BuildPieceOptions.Length - 1)];
    }

    static string DisplayName(VoxelLightingWorld.BuildPieceType pieceType)
    {
        switch (pieceType)
        {
            case VoxelLightingWorld.BuildPieceType.TrapDoor:
                return "Trap Door";
            default:
                return pieceType.ToString();
        }
    }

    static bool HasBuildOrientation(VoxelLightingWorld.BuildPieceType pieceType)
    {
        return pieceType == VoxelLightingWorld.BuildPieceType.Wall ||
            pieceType == VoxelLightingWorld.BuildPieceType.Window ||
            pieceType == VoxelLightingWorld.BuildPieceType.Door;
    }

    static bool SupportsRectangleDrag(VoxelLightingWorld.BuildPieceType pieceType)
    {
        return pieceType == VoxelLightingWorld.BuildPieceType.Wall ||
            pieceType == VoxelLightingWorld.BuildPieceType.Window ||
            pieceType == VoxelLightingWorld.BuildPieceType.Ceiling;
    }

    static bool CanUseVisibleSideSuggestion(VoxelLightingWorld.BuildPieceType pieceType)
    {
        return pieceType != VoxelLightingWorld.BuildPieceType.Ladder;
    }

    bool HasLineOfSightToBuildCandidate(VoxelLightingWorld.BuildPieceCandidate candidate)
    {
        if (viewCamera == null)
        {
            return true;
        }

        Vector3 origin = viewCamera.transform.position;
        Vector3 target = candidate.Position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return true;
        }

        direction /= distance;
        if (!Physics.Raycast(origin, direction, out var hit, distance - 0.03f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
        return marker != null &&
            marker.IsPanelPiece &&
            marker.Cell == candidate.Cell &&
            marker.FaceNormal == candidate.FaceNormal;
    }

    Vector3Int CameraForwardHorizontalAxis()
    {
        Vector3 forward = viewCamera != null ? viewCamera.transform.forward : transform.forward;
        forward.y = 0f;
        return Mathf.Abs(forward.x) >= Mathf.Abs(forward.z)
            ? Vector3Int.right
            : new Vector3Int(0, 0, 1);
    }

    Vector3Int CameraPreferredWidthAxis(Vector3Int faceNormal)
    {
        Vector3Int cameraAxis = CameraForwardHorizontalAxis();

        int dot = (cameraAxis.x * faceNormal.x) + (cameraAxis.z * faceNormal.z);
        if (dot == 0)
        {
            return cameraAxis;
        }

        // Width must stay inside the wall plane, so if camera-forward points
        // through the wall, use the only horizontal axis that lies on it.
        return faceNormal.x != 0 ? new Vector3Int(0, 0, 1) : Vector3Int.right;
    }

    string OrientationLabel()
    {
        Vector3Int normal = BuildFaceNormals[_buildOrientationIndex];
        if (normal == Vector3Int.right)
        {
            return "+X";
        }
        if (normal == Vector3Int.left)
        {
            return "-X";
        }
        if (normal.z > 0)
        {
            return "+Z";
        }
        return "-Z";
    }

    string OrientationHelpText()
    {
        if (_selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling)
        {
            return "Ceiling drag: horizontal rectangle";
        }

        if (HasBuildOrientation(_selectedPiece))
        {
            return $"Scroll: rotate {OrientationLabel()}";
        }

        return string.Empty;
    }

    string BuildModeHelpText()
    {
        string dragHelp = SupportsRectangleDrag(_selectedPiece)
            ? "   Ctrl+Left Drag: line/rectangle"
            : string.Empty;
        string lockHelp = HasBuildOrientation(_selectedPiece)
            ? "   Hold Left Shift: lock orientation"
            : string.Empty;
        string orientationHelp = OrientationHelpText();
        if (!string.IsNullOrEmpty(orientationHelp))
        {
            orientationHelp = $"   {orientationHelp}";
        }

        return $"BUILD MODE ({_selectedPiece})   F: exit   Left Click: place{dragHelp}{orientationHelp}{lockHelp}   Right Click + Mouse Direction: select   Esc: menu";
    }

    static Vector3Int PerpendicularHorizontalAxis(Vector3Int axis)
    {
        if (axis.x != 0)
        {
            return new Vector3Int(0, 0, 1);
        }

        return Vector3Int.right;
    }

    static Vector3Int QuantizeNormal(Vector3 normal)
    {
        float ax = Mathf.Abs(normal.x);
        float ay = Mathf.Abs(normal.y);
        float az = Mathf.Abs(normal.z);

        if (ay >= ax && ay >= az)
        {
            return normal.y >= 0f ? Vector3Int.up : Vector3Int.down;
        }

        if (ax >= az)
        {
            return normal.x >= 0f ? Vector3Int.right : Vector3Int.left;
        }

        return normal.z >= 0f ? new Vector3Int(0, 0, 1) : new Vector3Int(0, 0, -1);
    }

    static Material CreateTransparentMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader)
        {
            name = materialName,
            color = color
        };
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        return material;
    }
}
