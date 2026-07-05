using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the flat playing field as a grid of white voxel cubes,
/// plus lighting and a first-person player when the Game scene loads.
/// </summary>
public class VoxelFieldBuilder : MonoBehaviour
{
    [Header("Field")]
    public int gridWidth = 32;
    public int gridLength = 32;
    public float voxelSize = 1f;
    public int maxBuildHeight = 8;

    void Awake()
    {
        SceneFlow.InitializeGameScene();

        bool isRange = GameSession.IsShootingRange;
        if (isRange)
        {
            gridWidth = 48;
            gridLength = 680;
        }

        var voxelMaterial = CreateVoxelMaterial();
        var slipperyColliderMaterial = CreateSlipperyColliderMaterial();
        Vector3 gridOrigin = isRange
            ? new Vector3(-gridWidth * 0.5f * voxelSize, -0.5f * voxelSize, ShootingRangeSession.GridOriginWorldZ * voxelSize)
            : ComputeCenteredGridOrigin();

        var fieldRoot = new GameObject("Voxel Field").transform;
        var builtRoot = new GameObject("Built Voxels").transform;
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

        BuildField(voxelMaterial, slipperyColliderMaterial, voxelWorld, fieldRoot, gridOrigin, isRange);

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
                slipperyColliderMaterial);

            ShootingRangeBuilder.BuildTargets(
                transform,
                gridOrigin,
                voxelSize,
                gridWidth,
                slipperyColliderMaterial);
        }

        CreateLight(isRange);
        var player = CreatePlayer(voxelWorld, isRange);
        MatchClockHud.Create();

        if (GameSession.IsInPrepPhase)
        {
            MatchPrepController.Create();
        }
        else
        {
            GameSession.EnsureMatchClockStarted();
        }

        if (isRange && player != null)
        {
            var controller = player.GetComponent<ThirdPersonController>();
            ShootingRangeSession.Initialize(voxelWorld, controller);
        }
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
            GameSession.BeginMatch(GameSession.Team.Red);
        }

        var spawnPosition = isRange
            ? ShootingRangeSession.PlayerSpawnPosition
            : new Vector3(0f, 1.1f, -5f);

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

    static Material CreateVoxelMaterial()
    {
        var shader = Shader.Find("CoreWar/VoxelFaceLit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader)
        {
            name = "Voxel White Grid",
            mainTexture = CreateGridTexture()
        };
        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.05f);
        }
        return material;
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

    static Texture2D CreateGridTexture()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        var fill = new Color(0.99f, 0.99f, 0.99f);
        var line = new Color(0.62f, 0.62f, 0.66f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isEdge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                texture.SetPixel(x, y, isEdge ? line : fill);
            }
        }

        texture.Apply();
        return texture;
    }
}
