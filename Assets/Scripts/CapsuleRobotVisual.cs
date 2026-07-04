using UnityEngine;

/// <summary>
/// Builds a capsule-based robot with a torn pen-and-ink jersey and back number.
/// </summary>
public class CapsuleRobotVisual : MonoBehaviour
{
    Material _metalMaterial;
    Material _jerseyFrontMaterial;
    Material _jerseyBackMaterial;

    public void Build(GameSession.Team team, int jerseyNumber)
    {
        Color teamColor = GameSession.TeamColor(team);
        _metalMaterial = CreateBodyMaterial(new Color(0.62f, 0.66f, 0.72f));
        _jerseyFrontMaterial = CreateJerseyMaterial(JerseyInkUtility.CreateJerseyPanel(teamColor, false, jerseyNumber));
        _jerseyBackMaterial = CreateJerseyMaterial(JerseyInkUtility.CreateJerseyPanel(teamColor, true, jerseyNumber));

        CreatePart("Head", PrimitiveType.Capsule, new Vector3(0f, 1.52f, 0f), new Vector3(0.34f, 0.22f, 0.34f), _metalMaterial);
        CreatePart("Eye", PrimitiveType.Sphere, new Vector3(0f, 1.54f, 0.15f), new Vector3(0.12f, 0.12f, 0.05f), _metalMaterial);

        CreatePart("Torso", PrimitiveType.Capsule, new Vector3(0f, 1.05f, 0f), new Vector3(0.52f, 0.34f, 0.38f), _metalMaterial);
        CreatePart("Hips", PrimitiveType.Capsule, new Vector3(0f, 0.72f, 0f), new Vector3(0.42f, 0.14f, 0.34f), _metalMaterial);

        CreatePart("Jersey Front", PrimitiveType.Cube, new Vector3(0f, 1.02f, 0.2f), new Vector3(0.5f, 0.42f, 0.06f), _jerseyFrontMaterial);
        CreatePart("Jersey Back", PrimitiveType.Cube, new Vector3(0f, 1.02f, -0.2f), new Vector3(0.5f, 0.42f, 0.06f), _jerseyBackMaterial);

        CreatePart("Shoulder L", PrimitiveType.Sphere, new Vector3(-0.34f, 1.18f, 0f), new Vector3(0.12f, 0.12f, 0.12f), _metalMaterial);
        CreatePart("Shoulder R", PrimitiveType.Sphere, new Vector3(0.34f, 1.18f, 0f), new Vector3(0.12f, 0.12f, 0.12f), _metalMaterial);
        CreatePart("Arm L", PrimitiveType.Capsule, new Vector3(-0.42f, 0.92f, 0f), new Vector3(0.1f, 0.22f, 0.1f), _metalMaterial);
        CreatePart("Arm R", PrimitiveType.Capsule, new Vector3(0.42f, 0.92f, 0f), new Vector3(0.1f, 0.22f, 0.1f), _metalMaterial);

        CreatePart("Leg L", PrimitiveType.Capsule, new Vector3(-0.16f, 0.34f, 0f), new Vector3(0.14f, 0.28f, 0.14f), _metalMaterial);
        CreatePart("Leg R", PrimitiveType.Capsule, new Vector3(0.16f, 0.34f, 0f), new Vector3(0.14f, 0.28f, 0.14f), _metalMaterial);
        CreatePart("Foot L", PrimitiveType.Capsule, new Vector3(-0.16f, 0.08f, 0.04f), new Vector3(0.16f, 0.08f, 0.22f), _metalMaterial);
        CreatePart("Foot R", PrimitiveType.Capsule, new Vector3(0.16f, 0.08f, 0.04f), new Vector3(0.16f, 0.08f, 0.22f), _metalMaterial);
    }

    void CreatePart(string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(part.GetComponent<Collider>());
    }

    static Material CreateBodyMaterial(Color color)
    {
        var shader = Shader.Find("CoreWar/VoxelFaceLit") ?? Shader.Find("Standard");
        var material = new Material(shader) { mainTexture = CreateSolidTexture(color) };
        if (material.HasProperty("_ShadowLevel"))
        {
            material.SetFloat("_ShadowLevel", 0.58f);
        }
        return material;
    }

    static Texture2D CreateSolidTexture(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    static Material CreateJerseyMaterial(Texture2D texture)
    {
        var shader = Shader.Find("CoreWar/VoxelFaceLit") ?? Shader.Find("Standard");
        var material = new Material(shader) { mainTexture = texture };
        if (material.HasProperty("_ShadowLevel"))
        {
            material.SetFloat("_ShadowLevel", 0.52f);
        }
        return material;
    }
}
