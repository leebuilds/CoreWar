using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the flat playing field as a grid of white voxel cubes,
/// plus lighting and a first-person player when the Game scene loads.
/// </summary>
[DefaultExecutionOrder(-100)]
public class VoxelFieldBuilder : MonoBehaviour
{
    [Header("Field")]
    public int gridWidth = 32;
    public int gridLength = 32;
    public float voxelSize = 1f;
    public int maxBuildHeight = 8;

    [Header("Materials")]
    [SerializeField] Material voxelMaterialSource;

    void Awake()
    {
        BootTrace.Log("MAP", "VoxelFieldBuilder.Awake begin");
        GameSession.RestoreFromLifetimeIfNeeded();
        GameSession.LogDiagnostics("VoxelFieldBuilder.Awake (start)");

        if (SceneFlow.TryBlockUnauthorizedGameScene())
        {
            BootTrace.LogError("MAP", "VoxelFieldBuilder aborted: unauthorized Game scene");
            enabled = false;
            return;
        }

        BootTrace.ProbeCriticalShaders("VoxelFieldBuilder.Awake pre-material");

        if (!TryResolveVoxelMaterial(out Material voxelMaterial))
        {
            BootTrace.LogError(
                "MAP",
                "VoxelFieldBuilder aborted: voxel material unresolved. Returning to MainMenu instead of freezing.");
            enabled = false;
            SceneFlow.EnterMainMenu();
            return;
        }

        BootTrace.Log(
            "VOXELS",
            $"voxelMaterial ok name='{voxelMaterial.name}' shader='{voxelMaterial.shader?.name ?? "null"}'");

        SceneFlow.InitializeGameScene();
        GameUICanvas.EnsureExists();
        BootTrace.Log("UI", "GameUICanvas ensured");

        bool isRange = GameSession.IsShootingRange;
        bool isTestMap = !isRange;
        BootTrace.Log("MAP", $"mode isRange={isRange} isTestMap={isTestMap}");
        if (isRange)
        {
            gridWidth = 48;
            gridLength = 680;
        }
        else
        {
            gridWidth = 56;
            gridLength = 56;
        }

        var slipperyColliderMaterial = CreateSlipperyColliderMaterial();
        var floorColliderMaterial = CreateGrippyFloorColliderMaterial();
        Vector3 gridOrigin = ComputeCenteredGridOrigin();
        if (isRange)
        {
            gridOrigin.z = ShootingRangeSession.GridOriginWorldZ * voxelSize;
        }

        var fieldRoot = new GameObject("Voxel Field").transform;
        var builtRoot = new GameObject("Built Voxels").transform;
        BootTrace.Log("VOXELS", "VoxelLightingWorld.Initialize begin");
        var voxelWorld = gameObject.AddComponent<VoxelLightingWorld>();
        voxelWorld.Initialize(
            gridWidth,
            gridLength,
            maxBuildHeight,
            voxelSize,
            gridOrigin,
            voxelMaterial,
            slipperyColliderMaterial,
            builtRoot);
        BootTrace.Log("VOXELS", "VoxelLightingWorld.Initialize complete");

        if (isTestMap)
        {
            BootTrace.Log("MAP", "BuildTestMapOne begin");
            BuildTestMapOne(voxelMaterial, floorColliderMaterial, voxelWorld, fieldRoot, gridOrigin);
            BootTrace.Log("MAP", "BuildTestMapOne complete");
        }
        else
        {
            BootTrace.Log("MAP", "BuildField begin");
            BuildField(voxelMaterial, floorColliderMaterial, voxelWorld, fieldRoot, gridOrigin, isRange);
            BootTrace.Log("MAP", "BuildField complete");
        }

        if (isRange)
        {
            ShootingRangeTerrain.Build(
                transform,
                voxelWorld,
                gridOrigin,
                voxelSize,
                gridWidth,
                gridLength,
                voxelMaterial,
                floorColliderMaterial,
                slipperyColliderMaterial);

            ShootingRangeSession.Initialize(
                voxelWorld,
                null);

            ShootingRangeBuilder.BuildTargets(
                transform,
                gridOrigin,
                voxelSize,
                gridWidth,
                slipperyColliderMaterial);

            float wallInset = voxelSize * 0.5f + 0.35f;
            ShootingRangeSession.SetMovementBounds(
                gridOrigin.x + wallInset,
                gridOrigin.x + ((gridWidth - 1) * voxelSize) - wallInset);
        }

        CreateLight(isRange);

        GameObject player = null;
        if (MultiplayerSessionManager.IsNetworkSessionActive)
        {
            BootTrace.Log("PLAYER", "NetworkPlayerSpawner.Create (network session)");
            NetworkPlayerSpawner.Create(voxelWorld);
        }
        else
        {
            BootTrace.Log("PLAYER", "CreatePlayer (local session)");
            player = CreatePlayer(voxelWorld, isRange);
            BootTrace.Log("PLAYER", $"CreatePlayer done player={(player == null ? "null" : player.name)}");
        }

        BootTrace.Log("UI", "Creating gameplay HUD overlays");
        GameplayHud.Create();
        if (MultiplayerSessionManager.IsNetworkSessionActive)
        {
            MultiplayerSessionHud.Create();
        }

        if (isTestMap)
        {
            TestObjectiveHud.Create();
        }
        MatchClockHud.Create();
        PlayerBulletHitFlash.Create();

        if (GameSession.IsInPrepPhase)
        {
            BootTrace.Log("UI", "MatchPrepController.Create");
            MatchPrepController.Create();
        }
        else
        {
            GameSession.EnsureMatchClockStarted();
        }

        if (isRange && player != null)
        {
            var controller = player.GetComponent<ThirdPersonController>();
            ShootingRangeSession.SetPlayer(controller);
        }

        BootTrace.Log("MAP", "VoxelFieldBuilder.Awake complete — gameplay ready");
    }

    Vector3 ComputeCenteredGridOrigin()
    {
        float offsetX = (gridWidth - 1) * 0.5f * voxelSize;
        float offsetZ = (gridLength - 1) * 0.5f * voxelSize;
        return new Vector3(-offsetX, -0.5f * voxelSize, -offsetZ);
    }

    void CreateLight(bool isRange)
    {
        var go = new GameObject("Directional Light");
        go.transform.position = new Vector3(0f, 30f, 0f);
        go.transform.rotation = Quaternion.Euler(78f, -45f, 0f);

        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.color = Color.white;
        light.shadows = LightShadows.Hard;
        light.shadowStrength = 1f;
        light.shadowBias = 0.001f;
        light.shadowNormalBias = 0f;
        light.shadowNearPlane = 0.05f;
        light.shadowCustomResolution = 4096;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.47f);
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog = false;

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowProjection = ShadowProjection.CloseFit;
        QualitySettings.shadowDistance = isRange ? 150f : 120f;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
    }

    GameObject CreatePlayer(VoxelLightingWorld voxelWorld, bool isRange)
    {
        if (!GameSession.IsMatchActive)
        {
            return null;
        }

        var spawnPosition = isRange
            ? ShootingRangeSession.PlayerSpawnPosition
            : new Vector3(0f, 1.1f, -3f);

        var player = new GameObject("Player");
        player.transform.position = spawnPosition;

        var capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 1.8f;
        capsule.radius = 0.35f;
        capsule.center = new Vector3(0f, 0.9f, 0f);

        var rb = player.AddComponent<Rigidbody>();
        rb.mass = 70f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var visualRoot = new GameObject("Character Visual");
        visualRoot.transform.SetParent(player.transform, false);
        var robot = visualRoot.AddComponent<CapsuleRobotVisual>();
        robot.Build(GameSession.SelectedTeam, GameSession.JerseyNumber);

        var cameraRoot = new GameObject("Camera Rig");
        var yawPivot = new GameObject("Camera Yaw Pivot");
        var pitchPivot = new GameObject("Camera Pitch Pivot");
        yawPivot.transform.SetParent(cameraRoot.transform, false);
        pitchPivot.transform.SetParent(yawPivot.transform, false);

        var camObject = new GameObject("Main Camera") { tag = "MainCamera" };
        camObject.transform.SetParent(pitchPivot.transform, false);

        var cam = camObject.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.92f, 0.94f, 0.96f);
        cam.farClipPlane = isRange ? 750f : 500f;
        cam.nearClipPlane = 0.03f;

        camObject.AddComponent<AudioListener>();
        var effect = camObject.AddComponent<PenInkShadowEffect>();
        effect.voxelSize = voxelSize;
        effect.shadowThreshold = 0.26f;
        effect.centerDarkness = 0.78f;
        effect.circularFalloff = 3.2f;
        effect.topSurfaceThreshold = 0.84f;
        effect.hatchScale = 24f;
        effect.paperBlend = 0.03f;
        effect.inkColor = new Color(0.34f, 0.34f, 0.36f, 1f);
        effect.paperTint = new Color(0.985f, 0.985f, 0.985f, 1f);
        camObject.AddComponent<SniperScopePostEffect>();

        player.AddComponent<PlayerHealth>();

        var controller = player.AddComponent<ThirdPersonController>();
        controller.viewCamera = cam;
        controller.cameraYawPivot = yawPivot.transform;
        controller.cameraPitchPivot = pitchPivot.transform;
        controller.characterVisual = visualRoot.transform;
        controller.voxelWorld = voxelWorld;

        return player;
    }

    void BuildField(
        Material material,
        PhysicsMaterial colliderMaterial,
        VoxelLightingWorld voxelWorld,
        Transform fieldRoot,
        Vector3 gridOrigin,
        bool skipIndividualVoxels)
    {
        if (skipIndividualVoxels)
        {
            return;
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                var voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                voxel.name = $"Voxel ({x},{z})";
                voxel.transform.SetParent(fieldRoot, false);
                voxel.transform.position = new Vector3(
                    gridOrigin.x + x * voxelSize,
                    gridOrigin.y,
                    gridOrigin.z + z * voxelSize);
                voxel.transform.localScale = Vector3.one * (voxelSize * VoxelLightingWorld.SealOverlap);
                var renderer = voxel.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                renderer.receiveShadows = true;
                voxel.GetComponent<Collider>().material = colliderMaterial;
                voxelWorld.RegisterBaseVoxel(new Vector3Int(x, 0, z), voxel);
            }
        }
    }

    void BuildTestMapOne(
        Material material,
        PhysicsMaterial colliderMaterial,
        VoxelLightingWorld voxelWorld,
        Transform fieldRoot,
        Vector3 gridOrigin)
    {
        fieldRoot.name = TestMapObjectiveManager.MapName;

        BuildIsland(material, colliderMaterial, voxelWorld, fieldRoot, gridOrigin, Vector2.zero, 9, "Center Island");

        Vector2[] islandCenters =
        {
            new Vector2(0f, 18f),
            new Vector2(18f, 0f),
            new Vector2(0f, -18f),
            new Vector2(-18f, 0f)
        };

        for (int i = 0; i < islandCenters.Length; i++)
        {
            BuildIsland(
                material,
                colliderMaterial,
                voxelWorld,
                fieldRoot,
                gridOrigin,
                islandCenters[i],
                5,
                $"Outer Island {i + 1}");
        }

        TestMapObjectiveManager.Create();
        var drillsRoot = new GameObject("Test Map 1 Drills").transform;
        drillsRoot.SetParent(transform, false);

        int teamCount = TestMapObjectiveManager.ActiveTeamCount();
        for (int i = 0; i < teamCount; i++)
        {
            Vector2 islandCenter = islandCenters[i % islandCenters.Length];
            var drillPosition = new Vector3(islandCenter.x, 0.5f, islandCenter.y);
            TestMapDrill.Create(drillsRoot, TestMapObjectiveManager.TeamAt(i), drillPosition);
        }

        NetworkTestMapObjectiveSync.CreateIfNeeded();
    }

    void BuildIsland(
        Material material,
        PhysicsMaterial colliderMaterial,
        VoxelLightingWorld voxelWorld,
        Transform fieldRoot,
        Vector3 gridOrigin,
        Vector2 worldCenter,
        int radius,
        string islandName)
    {
        var islandRoot = new GameObject(islandName).transform;
        islandRoot.SetParent(fieldRoot, false);

        int centerCellX = Mathf.RoundToInt((worldCenter.x - gridOrigin.x) / voxelSize);
        int centerCellZ = Mathf.RoundToInt((worldCenter.y - gridOrigin.z) / voxelSize);
        int radiusSquared = radius * radius;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                float edgeNoise = Mathf.PerlinNoise((worldCenter.x + dx) * 0.21f, (worldCenter.y + dz) * 0.21f);
                int shapedRadiusSquared = radiusSquared + Mathf.RoundToInt((edgeNoise - 0.5f) * radius);
                if ((dx * dx) + (dz * dz) > shapedRadiusSquared)
                {
                    continue;
                }

                int x = centerCellX + dx;
                int z = centerCellZ + dz;
                if (x < 0 || x >= gridWidth || z < 0 || z >= gridLength)
                {
                    continue;
                }

                CreateMapVoxel(
                    material,
                    colliderMaterial,
                    voxelWorld,
                    islandRoot,
                    gridOrigin,
                    new Vector3Int(x, 0, z),
                    $"Island Voxel ({x},{z})");
            }
        }
    }

    void CreateMapVoxel(
        Material material,
        PhysicsMaterial colliderMaterial,
        VoxelLightingWorld voxelWorld,
        Transform parent,
        Vector3 gridOrigin,
        Vector3Int cell,
        string name)
    {
        var voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        voxel.name = name;
        voxel.transform.SetParent(parent, false);
        voxel.transform.position = new Vector3(
            gridOrigin.x + cell.x * voxelSize,
            gridOrigin.y + cell.y * voxelSize,
            gridOrigin.z + cell.z * voxelSize);
        voxel.transform.localScale = Vector3.one * (voxelSize * VoxelLightingWorld.SealOverlap);
        var renderer = voxel.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        renderer.receiveShadows = true;
        voxel.GetComponent<Collider>().material = colliderMaterial;
        voxelWorld.RegisterBaseVoxel(cell, voxel);
    }

    bool TryResolveVoxelMaterial(out Material voxelMaterial)
    {
        voxelMaterial = null;

        if (voxelMaterialSource == null)
        {
            Debug.LogError(
                "[VoxelFieldBuilder] voxelMaterialSource is not assigned. " +
                "Open Assets/Scenes/Game.unity, select the VoxelFieldBuilder object, " +
                "and drag Assets/Materials/VoxelWhiteGrid.mat into the Voxel Material Source field.");
            return false;
        }

        Debug.Log(
            $"[VoxelFieldBuilder] Using material '{voxelMaterialSource.name}' " +
            $"shader='{voxelMaterialSource.shader?.name ?? "null"}'.");

        if (voxelMaterialSource.shader == null)
        {
            Debug.LogError(
                $"[VoxelFieldBuilder] Material '{voxelMaterialSource.name}' has a null shader. " +
                "Rebuild Assets/Materials/VoxelWhiteGrid.mat so it references Assets/Scripts/VoxelFaceLit.shader.");
            return false;
        }

        voxelMaterial = voxelMaterialSource;
        return true;
    }

    static PhysicsMaterial CreateSlipperyColliderMaterial()
    {
        return new PhysicsMaterial("VoxelSlide")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
    }

    static PhysicsMaterial CreateGrippyFloorColliderMaterial()
    {
        // High friction for rolling bullets (Maximum combine on the ball wins).
        // Minimum combine keeps player movement slippery on the same floor.
        return new PhysicsMaterial("VoxelFloorGrip")
        {
            dynamicFriction = 2f,
            staticFriction = 2.2f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
    }
}
