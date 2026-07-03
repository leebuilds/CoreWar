using UnityEngine;

/// <summary>
/// Marker component used to identify voxels placed by the player,
/// so right click can remove only those blocks.
/// </summary>
public class PlayerBuiltVoxel : MonoBehaviour
{
    public Vector3Int Cell { get; private set; }

    public void Initialize(Vector3Int cell)
    {
        Cell = cell;
    }
}
