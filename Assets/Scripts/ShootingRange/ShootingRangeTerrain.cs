using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Fast shooting-range terrain using a handful of merged panels instead of per-voxel cubes.
/// </summary>
public static class ShootingRangeTerrain
{
    const int BackstopCellZ = 630;
    const int FiringLineCellZ = 10;
    const int WallHeightCells = 3;
    const int BackstopHeightCells = 4;

    public static void Build(
        Transform root,
        VoxelLightingWorld voxelWorld,
        Vector3 gridOrigin,
        float voxelSize,
        int gridWidth,
        int gridLength,
        Material floorMaterial,
        PhysicsMaterial floorColliderMaterial,
        PhysicsMaterial wallColliderMaterial)
    {
        var terrainRoot = new GameObject("Shooting Range Terrain").transform;
        terrainRoot.SetParent(root, false);

        float lengthMeters = gridLength * voxelSize;
        float widthMeters = gridWidth * voxelSize;
        float minX = gridOrigin.x - 0.5f * voxelSize;
        float minZ = gridOrigin.z - 0.5f * voxelSize;
        float centerX = minX + widthMeters * 0.5f;
        float centerZ = minZ + lengthMeters * 0.5f;
        float groundY = gridOrigin.y;

        var floorMaterialInstance = new Material(floorMaterial)
        {
            name = "Range Floor Grid"
        };
        if (floorMaterialInstance.HasProperty("_MainTex"))
        {
            floorMaterialInstance.mainTextureScale = new Vector2(gridWidth, gridLength);
            floorMaterialInstance.mainTextureOffset = Vector2.zero;
        }

        CreatePanel(
            terrainRoot,
            "Range Floor",
            new Vector3(centerX, groundY, centerZ),
            new Vector3(widthMeters * VoxelLightingWorld.SealOverlap, voxelSize, lengthMeters * VoxelLightingWorld.SealOverlap),
            floorMaterialInstance,
            floorColliderMaterial,
            castShadows: false);

        float wallHeight = WallHeightCells * voxelSize;
        float wallCenterY = groundY + wallHeight * 0.5f;
        float leftWallX = gridOrigin.x;
        float rightWallX = gridOrigin.x + (gridWidth - 1) * voxelSize;

        CreatePanel(
            terrainRoot,
            "Range Wall Left",
            new Vector3(leftWallX, wallCenterY, centerZ),
            new Vector3(voxelSize, wallHeight, lengthMeters),
            floorMaterial,
            wallColliderMaterial,
            castShadows: true);

        CreatePanel(
            terrainRoot,
            "Range Wall Right",
            new Vector3(rightWallX, wallCenterY, centerZ),
            new Vector3(voxelSize, wallHeight, lengthMeters),
            floorMaterial,
            wallColliderMaterial,
            castShadows: true);

        float backstopZ = gridOrigin.z + BackstopCellZ * voxelSize;
        float backstopHeight = BackstopHeightCells * voxelSize;
        CreatePanel(
            terrainRoot,
            "Range Backstop",
            new Vector3(centerX, groundY + backstopHeight * 0.5f, backstopZ),
            new Vector3(widthMeters, backstopHeight, voxelSize),
            floorMaterial,
            wallColliderMaterial,
            castShadows: true);

        BuildFiringLineFence(terrainRoot, wallColliderMaterial, gridOrigin, voxelSize, gridWidth);

        RegisterOccupancy(voxelWorld, gridWidth, gridLength);
    }

    static void BuildFiringLineFence(
        Transform parent,
        PhysicsMaterial colliderMaterial,
        Vector3 gridOrigin,
        float voxelSize,
        int gridWidth)
    {
        var fenceRoot = new GameObject("Firing Line Fence").transform;
        fenceRoot.SetParent(parent, false);

        var fenceMaterial = VoxelMaterialUtility.CreateSolidMaterial(new Color(0.42f, 0.40f, 0.38f), "Range Fence");
        var postMaterial = VoxelMaterialUtility.CreateSolidMaterial(new Color(0.36f, 0.34f, 0.32f), "Range Fence Post");
        const float fenceZ = ShootingRangeSession.FiringLineWorldZ;
        const float postSpacing = 4f;
        const float lowerHeight = 0.55f;
        const float upperRailHeight = 0.1f;
        const float upperRailY = 1.05f;

        float fenceMinX = gridOrigin.x;
        float fenceMaxX = gridOrigin.x + (gridWidth - 1) * voxelSize;
        float fenceWidth = gridWidth * voxelSize;
        float fenceCenterX = gridOrigin.x + (gridWidth - 1) * 0.5f * voxelSize;

        CreateFencePanel(
            fenceRoot,
            "Fence Lower Panel",
            new Vector3(fenceCenterX, lowerHeight * 0.5f, fenceZ),
            new Vector3(fenceWidth, lowerHeight, 0.18f),
            fenceMaterial,
            colliderMaterial);

        CreateFencePanel(
            fenceRoot,
            "Fence Upper Rail",
            new Vector3(fenceCenterX, upperRailY, fenceZ),
            new Vector3(fenceWidth, upperRailHeight, 0.12f),
            postMaterial,
            colliderMaterial);

        int postCount = Mathf.FloorToInt(fenceWidth / postSpacing) + 1;
        float postStep = postCount > 1 ? (fenceMaxX - fenceMinX) / (postCount - 1) : 0f;
        for (int i = 0; i < postCount; i++)
        {
            float x = fenceMinX + i * postStep;
            CreateFencePanel(
                fenceRoot,
                $"Fence Post {i}",
                new Vector3(x, (lowerHeight + upperRailY) * 0.5f, fenceZ),
                new Vector3(0.14f, upperRailY + upperRailHeight * 0.5f - lowerHeight * 0.5f, 0.14f),
                postMaterial,
                colliderMaterial);
        }
    }

    static void CreateFencePanel(
        Transform parent,
        string name,
        Vector3 center,
        Vector3 size,
        Material material,
        PhysicsMaterial colliderMaterial)
    {
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = name;
        panel.transform.SetParent(parent, false);
        panel.transform.position = center;
        panel.transform.localScale = size;
        panel.GetComponent<Renderer>().sharedMaterial = material;
        panel.GetComponent<Collider>().material = colliderMaterial;
    }

    static void CreatePanel(
        Transform parent,
        string name,
        Vector3 center,
        Vector3 size,
        Material material,
        PhysicsMaterial colliderMaterial,
        bool castShadows)
    {
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = name;
        panel.transform.SetParent(parent, false);
        panel.transform.position = center;
        panel.transform.localScale = size;

        var renderer = panel.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = castShadows ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        var collider = panel.GetComponent<BoxCollider>();
        collider.material = colliderMaterial;
    }

    static void RegisterOccupancy(VoxelLightingWorld voxelWorld, int gridWidth, int gridLength)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                voxelWorld.RegisterOccupiedCell(new Vector3Int(x, 0, z));
            }
        }

        int[] wallColumns = { 0, gridWidth - 1 };
        for (int w = 0; w < wallColumns.Length; w++)
        {
            int x = wallColumns[w];
            for (int z = 0; z < gridLength; z++)
            {
                for (int y = 1; y <= WallHeightCells; y++)
                {
                    voxelWorld.RegisterOccupiedCell(new Vector3Int(x, y, z));
                }
            }
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 1; y <= BackstopHeightCells; y++)
            {
                voxelWorld.RegisterOccupiedCell(new Vector3Int(x, y, BackstopCellZ));
            }
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 1; y <= 2; y++)
            {
                voxelWorld.RegisterOccupiedCell(new Vector3Int(x, y, FiringLineCellZ));
            }
        }
    }
}
