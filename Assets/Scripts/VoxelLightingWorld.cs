using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Manages voxel occupancy and grid building. Lighting is handled entirely
/// by the directional light + shadow map through the two-level voxel shader.
/// </summary>
public class VoxelLightingWorld : MonoBehaviour
{
    readonly Dictionary<Vector3Int, GameObject> _voxels = new Dictionary<Vector3Int, GameObject>();
    readonly HashSet<Vector3Int> _playerPlaced = new HashSet<Vector3Int>();
    readonly Dictionary<BuildPieceSlot, GameObject> _buildPieces = new Dictionary<BuildPieceSlot, GameObject>();

    int _gridWidth;
    int _gridLength;
    int _maxBuildHeight;
    float _voxelSize;
    Vector3 _gridOrigin;
    Material _voxelMaterial;
    Material _wallMaterial;
    Material _windowMaterial;
    Material _ceilingMaterial;
    Material _doorMaterial;
    Material _trapDoorMaterial;
    Material _ladderMaterial;
    PhysicsMaterial _colliderMaterial;
    Transform _builtRoot;

    const float PanelThicknessScale = 0.08f;
    const float LadderSurfaceOffset = 0.055f;

    public enum BuildPieceType
    {
        Wall,
        Window,
        Ceiling,
        Door,
        TrapDoor,
        Ladder
    }

    public struct BuildPieceCandidate
    {
        public bool HasTarget;
        public bool CanPlace;
        public Vector3Int Cell;
        public Vector3Int FaceNormal;
        public BuildPieceType PieceType;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public struct BuildPieceSlot : IEquatable<BuildPieceSlot>
    {
        public Vector3Int Cell;
        public Vector3Int FaceNormal;
        public BuildPieceType PieceType;

        public BuildPieceSlot(Vector3Int cell, Vector3Int faceNormal, BuildPieceType pieceType)
        {
            Cell = cell;
            FaceNormal = faceNormal;
            PieceType = pieceType;
        }

        public bool Equals(BuildPieceSlot other)
        {
            return Cell == other.Cell && FaceNormal == other.FaceNormal && PieceType == other.PieceType;
        }

        public override bool Equals(object obj)
        {
            return obj is BuildPieceSlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Cell.GetHashCode();
                hash = (hash * 397) ^ FaceNormal.GetHashCode();
                hash = (hash * 397) ^ (int)PieceType;
                return hash;
            }
        }
    }

    /// <summary>
    /// Voxels are rendered slightly oversized so neighbors overlap and
    /// shadow maps cannot leak light through sub-voxel corner/edge cracks.
    /// </summary>
    public const float SealOverlap = 1.002f;

    public float VoxelSize => _voxelSize;

    public void Initialize(
        int gridWidth,
        int gridLength,
        int maxBuildHeight,
        float voxelSize,
        Vector3 gridOrigin,
        Material voxelMaterial,
        PhysicsMaterial colliderMaterial,
        Transform builtRoot)
    {
        _gridWidth = gridWidth;
        _gridLength = gridLength;
        _maxBuildHeight = maxBuildHeight;
        _voxelSize = voxelSize;
        _gridOrigin = gridOrigin;
        _voxelMaterial = voxelMaterial;
        _wallMaterial = CreateBuildMaterial("Built Wall", new Color(0.86f, 0.86f, 0.88f, 1f));
        _windowMaterial = CreateBuildMaterial("Built Window", new Color(0.5f, 0.72f, 0.95f, 0.55f));
        _ceilingMaterial = CreateBuildMaterial("Built Ceiling", new Color(0.9f, 0.9f, 0.92f, 1f));
        _doorMaterial = CreateBuildMaterial("Built Door", new Color(0.58f, 0.43f, 0.25f, 1f));
        _trapDoorMaterial = CreateBuildMaterial("Built Trap Door", new Color(0.46f, 0.32f, 0.18f, 1f));
        _ladderMaterial = CreateBuildMaterial("Built Ladder", new Color(0.28f, 0.2f, 0.12f, 1f));
        _colliderMaterial = colliderMaterial;
        _builtRoot = builtRoot;
    }

    public void RegisterBaseVoxel(Vector3Int cell, GameObject voxel)
    {
        _voxels[cell] = voxel;
    }

    public Vector3 CellToWorld(Vector3Int cell)
    {
        return new Vector3(
            _gridOrigin.x + cell.x * _voxelSize,
            _gridOrigin.y + cell.y * _voxelSize,
            _gridOrigin.z + cell.z * _voxelSize);
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - _gridOrigin.x) / _voxelSize);
        int y = Mathf.RoundToInt((worldPosition.y - _gridOrigin.y) / _voxelSize);
        int z = Mathf.RoundToInt((worldPosition.z - _gridOrigin.z) / _voxelSize);
        return new Vector3Int(x, y, z);
    }

    public bool TryPlaceVoxel(Vector3Int cell)
    {
        if (!CanBuildAt(cell))
        {
            return false;
        }

        var voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        voxel.name = $"Placed Voxel ({cell.x},{cell.y},{cell.z})";
        voxel.transform.SetParent(_builtRoot, false);
        voxel.transform.position = CellToWorld(cell);
        voxel.transform.localScale = Vector3.one * (_voxelSize * SealOverlap);

        var renderer = voxel.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _voxelMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        renderer.receiveShadows = true;

        voxel.GetComponent<Collider>().material = _colliderMaterial;

        var marker = voxel.AddComponent<PlayerBuiltVoxel>();
        marker.Initialize(cell);

        _voxels[cell] = voxel;
        _playerPlaced.Add(cell);
        return true;
    }

    public bool TryRemovePlayerVoxel(PlayerBuiltVoxel marker)
    {
        if (marker == null)
        {
            return false;
        }

        Vector3Int cell = marker.Cell;
        if (!_playerPlaced.Contains(cell))
        {
            return false;
        }

        if (_voxels.TryGetValue(cell, out var go))
        {
            _voxels.Remove(cell);
            _playerPlaced.Remove(cell);
            Destroy(go);
            return true;
        }

        return false;
    }

    public bool TryRemovePlayerBuiltObject(PlayerBuiltVoxel marker)
    {
        if (marker == null)
        {
            return false;
        }

        if (!marker.IsPanelPiece)
        {
            return TryRemovePlayerVoxel(marker);
        }

        var slot = new BuildPieceSlot(marker.Cell, marker.FaceNormal, marker.PieceType);
        if (_buildPieces.TryGetValue(slot, out var go))
        {
            _buildPieces.Remove(slot);
            Destroy(go);
            return true;
        }

        return false;
    }

    public bool TryGetBuildPieceCandidate(Ray ray, float range, BuildPieceType pieceType, out BuildPieceCandidate candidate)
    {
        return TryGetBuildPieceCandidate(ray, range, pieceType, Vector3Int.zero, out candidate);
    }

    public bool TryGetBuildPieceCandidate(
        Ray ray,
        float range,
        BuildPieceType pieceType,
        Vector3Int forcedFaceNormal,
        out BuildPieceCandidate candidate)
    {
        candidate = new BuildPieceCandidate
        {
            HasTarget = false,
            CanPlace = false,
            PieceType = pieceType,
            Rotation = Quaternion.identity
        };

        if (!Physics.Raycast(ray, out var hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3Int hitNormal = QuantizeNormal(hit.normal);
        Vector3Int supportCell = WorldToCell(hit.point - ((Vector3)hitNormal * (_voxelSize * 0.45f)));
        Vector3Int buildCell;
        Vector3Int faceNormal;

        if (pieceType == BuildPieceType.Ladder)
        {
            var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
            if (marker == null || !CanAttachLadderTo(marker))
            {
                return true;
            }

            buildCell = marker.Cell;
            faceNormal = marker.FaceNormal;
        }
        else if (IsHorizontalPiece(pieceType))
        {
            buildCell = supportCell + Vector3Int.up;
            faceNormal = Vector3Int.up;
        }
        else
        {
            faceNormal = forcedFaceNormal == Vector3Int.zero
                ? (hitNormal.y == 0 ? hitNormal : HorizontalFaceFromHitPoint(hit.point, supportCell))
                : forcedFaceNormal;
            buildCell = supportCell + Vector3Int.up;
        }

        return TryCreateBuildPieceCandidate(pieceType, buildCell, faceNormal, out candidate);
    }

    public bool TryCreateBuildPieceCandidate(
        BuildPieceType pieceType,
        Vector3Int cell,
        Vector3Int faceNormal,
        out BuildPieceCandidate candidate)
    {
        candidate = new BuildPieceCandidate
        {
            HasTarget = true,
            CanPlace = false,
            Cell = cell,
            FaceNormal = faceNormal,
            PieceType = pieceType,
            Rotation = Quaternion.identity
        };

        ApplyBuildPiecePose(ref candidate);
        candidate.CanPlace = CanPlaceBuildPiece(candidate);
        return true;
    }

    public bool TryPlaceBuildPiece(BuildPieceCandidate candidate)
    {
        if (!candidate.HasTarget || !CanPlaceBuildPiece(candidate))
        {
            return false;
        }

        PlaceBuildPieceUnchecked(candidate);
        return true;
    }

    public bool TryPlaceBuildPieceBatch(IReadOnlyList<BuildPieceCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return false;
        }

        var validFlags = new bool[candidates.Count];
        if (!ValidateBuildPieceBatch(candidates, validFlags))
        {
            return false;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            PlaceBuildPieceUnchecked(candidates[i]);
        }

        return true;
    }

    void PlaceBuildPieceUnchecked(BuildPieceCandidate candidate)
    {
        var slot = new BuildPieceSlot(candidate.Cell, candidate.FaceNormal, candidate.PieceType);
        var root = new GameObject($"Built {candidate.PieceType} ({candidate.Cell.x},{candidate.Cell.y},{candidate.Cell.z})");
        root.transform.SetParent(_builtRoot, false);
        root.transform.position = candidate.Position;
        root.transform.rotation = candidate.Rotation;

        if (candidate.PieceType == BuildPieceType.Window)
        {
            CreatePanelPart(root.transform, "Window Pane", candidate.Scale, _windowMaterial);
        }
        else if (candidate.PieceType == BuildPieceType.Ladder)
        {
            CreateLadder(root.transform, candidate.Scale);
        }
        else
        {
            Material material = MaterialForPiece(candidate.PieceType);
            CreatePanelPart(root.transform, candidate.PieceType.ToString(), candidate.Scale, material);
        }

        var marker = root.AddComponent<PlayerBuiltVoxel>();
        marker.InitializePanel(candidate.Cell, candidate.FaceNormal, candidate.PieceType);
        _buildPieces[slot] = root;
    }

    public bool CanBuildAt(Vector3Int cell)
    {
        if (cell.x < 0 || cell.x >= _gridWidth || cell.z < 0 || cell.z >= _gridLength)
        {
            return false;
        }

        if (cell.y < 0 || cell.y > _maxBuildHeight)
        {
            return false;
        }

        if (_voxels.ContainsKey(cell))
        {
            return false;
        }

        Vector3 center = CellToWorld(cell);
        Vector3 halfExtents = Vector3.one * (_voxelSize * 0.45f);
        return !Physics.CheckBox(center, halfExtents, Quaternion.identity, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }

    public bool ValidateBuildPieceBatch(IReadOnlyList<BuildPieceCandidate> candidates, bool[] validFlags)
    {
        if (validFlags == null || validFlags.Length < candidates.Count)
        {
            return false;
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        bool anyConnectedToWorld = false;
        for (int i = 0; i < candidates.Count; i++)
        {
            validFlags[i] = CanPlaceBuildPiece(candidates[i], candidates);
            anyConnectedToWorld |= HasBuildConnection(candidates[i]);
        }

        if (!anyConnectedToWorld)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                validFlags[i] = false;
            }
            return false;
        }

        bool allValid = true;
        for (int i = 0; i < candidates.Count; i++)
        {
            allValid &= validFlags[i];
        }
        return allValid;
    }

    public bool CanPlaceBuildPiece(BuildPieceCandidate candidate)
    {
        return CanPlaceBuildPiece(candidate, null);
    }

    public bool IsBuildSurfaceOccupied(BuildPieceCandidate candidate)
    {
        return IsBuildSlotOccupied(candidate);
    }

    bool CanPlaceBuildPiece(BuildPieceCandidate candidate, IReadOnlyList<BuildPieceCandidate> peerCandidates)
    {
        if (candidate.Cell.x < 0 || candidate.Cell.x >= _gridWidth || candidate.Cell.z < 0 || candidate.Cell.z >= _gridLength)
        {
            return false;
        }

        if (candidate.Cell.y < 1 || candidate.Cell.y > _maxBuildHeight)
        {
            return false;
        }

        if (IsBuildSlotOccupied(candidate) || IsPeerSlotOccupied(candidate, peerCandidates))
        {
            return false;
        }

        if (OverlapsPeerCandidate(candidate, peerCandidates))
        {
            return false;
        }

        if (!HasBuildConnection(candidate) && !HasPeerConnection(candidate, peerCandidates))
        {
            return false;
        }

        if (candidate.PieceType == BuildPieceType.Ladder)
        {
            return true;
        }

        Vector3 halfExtents = candidate.Scale * 0.45f;
        return !Physics.CheckBox(candidate.Position, halfExtents, candidate.Rotation, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }

    static bool IsPeerSlotOccupied(BuildPieceCandidate candidate, IReadOnlyList<BuildPieceCandidate> peerCandidates)
    {
        if (peerCandidates == null)
        {
            return false;
        }

        int matches = 0;
        for (int i = 0; i < peerCandidates.Count; i++)
        {
            BuildPieceCandidate peer = peerCandidates[i];
            if (peer.Cell == candidate.Cell &&
                peer.FaceNormal == candidate.FaceNormal &&
                peer.PieceType == candidate.PieceType)
            {
                matches++;
            }
        }

        return matches > 1;
    }

    static bool OverlapsPeerCandidate(BuildPieceCandidate candidate, IReadOnlyList<BuildPieceCandidate> peerCandidates)
    {
        if (peerCandidates == null || candidate.PieceType == BuildPieceType.Ladder)
        {
            return false;
        }

        for (int i = 0; i < peerCandidates.Count; i++)
        {
            BuildPieceCandidate peer = peerCandidates[i];
            if (peer.PieceType == BuildPieceType.Ladder ||
                (peer.Cell == candidate.Cell &&
                peer.FaceNormal == candidate.FaceNormal &&
                peer.PieceType == candidate.PieceType))
            {
                continue;
            }

            if (AxisAlignedBoxesOverlap(candidate.Position, candidate.Scale, peer.Position, peer.Scale))
            {
                return true;
            }
        }

        return false;
    }

    static bool AxisAlignedBoxesOverlap(Vector3 aCenter, Vector3 aScale, Vector3 bCenter, Vector3 bScale)
    {
        const float epsilon = 0.001f;
        Vector3 totalHalf = (aScale + bScale) * 0.5f;
        Vector3 delta = new Vector3(
            Mathf.Abs(aCenter.x - bCenter.x),
            Mathf.Abs(aCenter.y - bCenter.y),
            Mathf.Abs(aCenter.z - bCenter.z));

        return delta.x < totalHalf.x - epsilon &&
            delta.y < totalHalf.y - epsilon &&
            delta.z < totalHalf.z - epsilon;
    }

    bool HasPeerConnection(BuildPieceCandidate candidate, IReadOnlyList<BuildPieceCandidate> peerCandidates)
    {
        if (peerCandidates == null)
        {
            return false;
        }

        for (int i = 0; i < peerCandidates.Count; i++)
        {
            BuildPieceCandidate peer = peerCandidates[i];
            if (peer.Cell == candidate.Cell &&
                peer.FaceNormal == candidate.FaceNormal &&
                peer.PieceType == candidate.PieceType)
            {
                continue;
            }

            var peerSlot = new BuildPieceSlot(peer.Cell, peer.FaceNormal, peer.PieceType);
            if (SharesFullEdge(candidate, peerSlot))
            {
                return true;
            }
        }

        return false;
    }

    bool IsBuildSlotOccupied(BuildPieceCandidate candidate)
    {
        foreach (BuildPieceSlot slot in _buildPieces.Keys)
        {
            if (slot.Cell != candidate.Cell || slot.FaceNormal != candidate.FaceNormal)
            {
                continue;
            }

            if (candidate.PieceType == BuildPieceType.Ladder)
            {
                if (slot.PieceType == BuildPieceType.Ladder)
                {
                    return true;
                }

                continue;
            }

            if (slot.PieceType != BuildPieceType.Ladder)
            {
                return true;
            }
        }

        return false;
    }

    void ApplyBuildPiecePose(ref BuildPieceCandidate candidate)
    {
        float size = _voxelSize;
        float thickness = size * PanelThicknessScale;
        Vector3 cellCenter = CellToWorld(candidate.Cell);

        if (candidate.PieceType == BuildPieceType.Ceiling)
        {
            candidate.Position = cellCenter + Vector3.up * (size * 0.5f);
            candidate.Scale = new Vector3(size, thickness, size);
            candidate.Rotation = Quaternion.identity;
            return;
        }

        if (candidate.PieceType == BuildPieceType.TrapDoor)
        {
            candidate.Position = cellCenter - Vector3.up * ((size - thickness) * 0.5f);
            candidate.Scale = new Vector3(size, thickness, size);
            candidate.Rotation = Quaternion.identity;
            return;
        }

        Vector3 faceNormal = (Vector3)candidate.FaceNormal;
        candidate.Position = cellCenter + faceNormal * (size * 0.5f);
        candidate.Rotation = Quaternion.identity;

        if (candidate.PieceType == BuildPieceType.Ladder)
        {
            candidate.Position += faceNormal * LadderSurfaceOffset;
            candidate.Scale = candidate.FaceNormal.x != 0
                ? new Vector3(thickness, size * 0.9f, size * 0.58f)
                : new Vector3(size * 0.58f, size * 0.9f, thickness);
            return;
        }

        if (candidate.FaceNormal.x != 0)
        {
            float height = VerticalPieceHeight(candidate.PieceType, size);
            float width = VerticalPieceWidth(candidate.PieceType, size);
            candidate.Position += Vector3.up * ((height - size) * 0.5f);
            candidate.Scale = new Vector3(thickness, height, width);
        }
        else
        {
            float height = VerticalPieceHeight(candidate.PieceType, size);
            float width = VerticalPieceWidth(candidate.PieceType, size);
            candidate.Position += Vector3.up * ((height - size) * 0.5f);
            candidate.Scale = new Vector3(width, height, thickness);
        }
    }

    bool HasBuildConnection(BuildPieceCandidate candidate)
    {
        if (CanRestOnGround(candidate) && _voxels.ContainsKey(candidate.Cell + Vector3Int.down))
        {
            return true;
        }

        if (candidate.PieceType == BuildPieceType.Ladder)
        {
            return HasLadderSupport(candidate);
        }

        foreach (BuildPieceSlot slot in _buildPieces.Keys)
        {
            if (SharesFullEdge(candidate, slot))
            {
                return true;
            }
        }

        return false;
    }

    bool SharesFullEdge(BuildPieceCandidate candidate, BuildPieceSlot existing)
    {
        var candidateSlot = new BuildPieceSlot(candidate.Cell, candidate.FaceNormal, candidate.PieceType);

        if (IsHorizontalPiece(candidate.PieceType))
        {
            return (IsVerticalPanel(existing) && CeilingTouchesVerticalPanel(candidate.Cell, existing)) ||
                (IsHorizontalPiece(existing.PieceType) && HorizontalPiecesShareSide(candidateSlot, existing));
        }

        if (IsHorizontalPiece(existing.PieceType))
        {
            return CeilingTouchesVerticalPanel(existing.Cell, candidateSlot);
        }

        return VerticalPanelsShareEdge(candidateSlot, existing);
    }

    static bool IsVerticalPanel(BuildPieceSlot slot)
    {
        return !IsHorizontalPiece(slot.PieceType) && slot.PieceType != BuildPieceType.Ladder;
    }

    static bool IsHorizontalPiece(BuildPieceType pieceType)
    {
        return pieceType == BuildPieceType.Ceiling || pieceType == BuildPieceType.TrapDoor;
    }

    static bool CanRestOnGround(BuildPieceCandidate candidate)
    {
        return candidate.PieceType != BuildPieceType.Ladder &&
            (!IsHorizontalPiece(candidate.PieceType) || candidate.PieceType == BuildPieceType.TrapDoor);
    }

    static bool CanAttachLadderTo(PlayerBuiltVoxel marker)
    {
        return marker.IsPanelPiece &&
            marker.FaceNormal.y == 0 &&
            marker.PieceType != BuildPieceType.Ceiling &&
            marker.PieceType != BuildPieceType.TrapDoor &&
            marker.PieceType != BuildPieceType.Ladder;
    }

    bool HasLadderSupport(BuildPieceCandidate candidate)
    {
        foreach (BuildPieceSlot slot in _buildPieces.Keys)
        {
            if (slot.Cell == candidate.Cell &&
                slot.FaceNormal == candidate.FaceNormal &&
                IsVerticalPanel(slot))
            {
                return true;
            }
        }

        return false;
    }

    static bool CeilingTouchesVerticalPanel(Vector3Int ceilingCell, BuildPieceSlot verticalPanel)
    {
        return verticalPanel.Cell == ceilingCell || verticalPanel.Cell + verticalPanel.FaceNormal == ceilingCell;
    }

    static bool HorizontalPiecesShareSide(BuildPieceSlot a, BuildPieceSlot b)
    {
        if (a.Cell.y != b.Cell.y)
        {
            return false;
        }

        Vector3Int delta = b.Cell - a.Cell;
        bool adjacentX = Mathf.Abs(delta.x) == 1 && delta.z == 0;
        bool adjacentZ = delta.x == 0 && Mathf.Abs(delta.z) == 1;
        return adjacentX || adjacentZ;
    }

    static bool VerticalPanelsShareEdge(BuildPieceSlot a, BuildPieceSlot b)
    {
        if (a.FaceNormal == b.FaceNormal)
        {
            return SamePlanePanelsShareEdge(a, b);
        }

        if (a.FaceNormal == -b.FaceNormal)
        {
            return false;
        }

        return PerpendicularPanelsShareEdge(a, b);
    }

    static bool SamePlanePanelsShareEdge(BuildPieceSlot a, BuildPieceSlot b)
    {
        Vector3Int delta = b.Cell - a.Cell;

        if (a.FaceNormal.x != 0)
        {
            if (a.Cell.x != b.Cell.x)
            {
                return false;
            }

            bool stacked = delta.x == 0 && Mathf.Abs(delta.y) == 1 && delta.z == 0;
            bool sideBySide = delta.x == 0 && delta.y == 0 && Mathf.Abs(delta.z) == 1;
            return stacked || sideBySide;
        }

        if (a.Cell.z != b.Cell.z)
        {
            return false;
        }

        bool stackedOnZFace = delta.x == 0 && Mathf.Abs(delta.y) == 1 && delta.z == 0;
        bool sideBySideOnZFace = Mathf.Abs(delta.x) == 1 && delta.y == 0 && delta.z == 0;
        return stackedOnZFace || sideBySideOnZFace;
    }

    static bool PerpendicularPanelsShareEdge(BuildPieceSlot a, BuildPieceSlot b)
    {
        if (a.Cell.y != b.Cell.y)
        {
            return false;
        }

        Vector3Int aOuterCell = a.Cell + a.FaceNormal;
        Vector3Int bOuterCell = b.Cell + b.FaceNormal;
        return a.Cell == b.Cell ||
            aOuterCell == b.Cell ||
            bOuterCell == a.Cell ||
            aOuterCell == bOuterCell;
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

    Vector3Int HorizontalFaceFromHitPoint(Vector3 hitPoint, Vector3Int supportCell)
    {
        Vector3 local = hitPoint - CellToWorld(supportCell);
        if (Mathf.Abs(local.x) >= Mathf.Abs(local.z))
        {
            return local.x >= 0f ? Vector3Int.right : Vector3Int.left;
        }

        return local.z >= 0f ? new Vector3Int(0, 0, 1) : new Vector3Int(0, 0, -1);
    }

    GameObject CreatePanelPart(Transform parent, string partName, Vector3 scale, Material material)
    {
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = partName;
        panel.transform.SetParent(parent, false);
        panel.transform.localScale = scale;

        var renderer = panel.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        renderer.receiveShadows = true;

        panel.GetComponent<Collider>().material = _colliderMaterial;
        return panel;
    }

    void CreateLadder(Transform parent, Vector3 scale)
    {
        bool onXFace = scale.x <= scale.z;
        float rungLength = onXFace ? scale.z : scale.x;
        float depth = onXFace ? scale.x : scale.z;
        float railSpacing = rungLength * 0.35f;
        float railThickness = _voxelSize * 0.055f;

        for (int i = 0; i < 2; i++)
        {
            float offset = i == 0 ? -railSpacing : railSpacing;
            Vector3 position = onXFace ? new Vector3(0f, 0f, offset) : new Vector3(offset, 0f, 0f);
            Vector3 railScale = onXFace
                ? new Vector3(depth, scale.y, railThickness)
                : new Vector3(railThickness, scale.y, depth);
            CreatePanelPart(parent, $"Ladder Rail {i + 1}", railScale, _ladderMaterial).transform.localPosition = position;
        }

        for (int i = 0; i < 4; i++)
        {
            float y = Mathf.Lerp(-scale.y * 0.35f, scale.y * 0.35f, i / 3f);
            Vector3 rungScale = onXFace
                ? new Vector3(depth, railThickness, rungLength * 0.82f)
                : new Vector3(rungLength * 0.82f, railThickness, depth);
            CreatePanelPart(parent, $"Ladder Rung {i + 1}", rungScale, _ladderMaterial)
                .transform.localPosition = new Vector3(0f, y, 0f);
        }
    }

    Material MaterialForPiece(BuildPieceType pieceType)
    {
        switch (pieceType)
        {
            case BuildPieceType.Ceiling:
                return _ceilingMaterial;
            case BuildPieceType.Door:
                return _doorMaterial;
            case BuildPieceType.TrapDoor:
                return _trapDoorMaterial;
            default:
                return _wallMaterial;
        }
    }

    static float VerticalPieceHeight(BuildPieceType pieceType, float size)
    {
        switch (pieceType)
        {
            case BuildPieceType.Door:
                return size * 2f;
            default:
                return size;
        }
    }

    static float VerticalPieceWidth(BuildPieceType pieceType, float size)
    {
        switch (pieceType)
        {
            default:
                return size;
        }
    }

    static Material CreateBuildMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = materialName,
            color = color
        };

        if (color.a < 1f)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        return material;
    }
}
