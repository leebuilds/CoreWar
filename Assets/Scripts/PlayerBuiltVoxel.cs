using UnityEngine;

/// <summary>
/// Marker component used to identify structures placed by the player.
/// </summary>
public class PlayerBuiltVoxel : MonoBehaviour
{
    public Vector3Int Cell { get; private set; }
    public Vector3Int FaceNormal { get; private set; }
    public VoxelLightingWorld.BuildPieceType PieceType { get; private set; }
    public bool IsPanelPiece { get; private set; }

    public void Initialize(Vector3Int cell)
    {
        Cell = cell;
        FaceNormal = Vector3Int.zero;
        PieceType = VoxelLightingWorld.BuildPieceType.Wall;
        IsPanelPiece = false;
    }

    public void InitializePanel(Vector3Int cell, Vector3Int faceNormal, VoxelLightingWorld.BuildPieceType pieceType)
    {
        Cell = cell;
        FaceNormal = faceNormal;
        PieceType = pieceType;
        IsPanelPiece = true;
    }
}
