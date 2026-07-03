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

    int _gridWidth;
    int _gridLength;
    int _maxBuildHeight;
    float _voxelSize;
    Vector3 _gridOrigin;
    Material _voxelMaterial;
    PhysicsMaterial _colliderMaterial;
    Transform _builtRoot;

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
}
