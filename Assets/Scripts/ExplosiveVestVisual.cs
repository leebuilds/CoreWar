using UnityEngine;

/// <summary>
/// World-space explosive vest: toroidal strap tube with C4-style pocket bricks.
/// </summary>
public class ExplosiveVestVisual : MonoBehaviour
{
    const int PocketCount = 8;
    const int RingSegmentCount = 12;
    const float TorsoY = 1.05f;
    const float UpperRingY = 1.16f;
    const float LowerRingY = 0.94f;
    const float RadiusX = 0.34f;
    const float RadiusZ = 0.3f;

    static Material _strapMaterial;
    static Material _pocketMaterial;
    static Material _buttonMaterial;

    public static void ShowOn(Transform hostRoot)
    {
        if (hostRoot == null)
        {
            return;
        }

        Transform parent = ResolveVisualParent(hostRoot);
        ExplosiveVestVisual visual = parent.GetComponentInChildren<ExplosiveVestVisual>(true);
        if (visual == null)
        {
            var go = new GameObject("Explosive Vest Visual");
            go.transform.SetParent(parent, false);
            visual = go.AddComponent<ExplosiveVestVisual>();
            visual.Build();
        }

        visual.gameObject.SetActive(true);
    }

    public static void HideOn(Transform hostRoot)
    {
        if (hostRoot == null)
        {
            return;
        }

        Transform parent = ResolveVisualParent(hostRoot);
        ExplosiveVestVisual visual = parent.GetComponentInChildren<ExplosiveVestVisual>(true);
        if (visual != null)
        {
            visual.gameObject.SetActive(false);
        }
    }

    static Transform ResolveVisualParent(Transform hostRoot)
    {
        var controller = hostRoot.GetComponent<ThirdPersonController>();
        if (controller != null && controller.characterVisual != null)
        {
            return controller.characterVisual;
        }

        Transform dummyVisual = hostRoot.Find("Dummy Visual");
        if (dummyVisual != null)
        {
            return dummyVisual;
        }

        return hostRoot;
    }

    void Build()
    {
        EnsureMaterials();
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        CreateRingStrap(UpperRingY, RadiusX, RadiusZ);
        CreateRingStrap(LowerRingY, RadiusX * 0.96f, RadiusZ * 0.96f);

        for (int i = 0; i < PocketCount; i++)
        {
            float angle = (i / (float)PocketCount) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * RadiusX;
            float z = Mathf.Sin(angle) * RadiusZ;
            CreatePocket(new Vector3(x, TorsoY, z), angle);
            CreateVerticalStrap(
                new Vector3(x, UpperRingY, z * (UpperRingY / TorsoY)),
                new Vector3(x, LowerRingY, z * (LowerRingY / TorsoY)));
        }
    }

    void CreatePocket(Vector3 localPosition, float yawRadians)
    {
        var pocket = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pocket.name = "Vest Pocket";
        pocket.transform.SetParent(transform, false);
        pocket.transform.localPosition = localPosition;
        pocket.transform.localRotation = Quaternion.Euler(0f, yawRadians * Mathf.Rad2Deg, 0f);
        pocket.transform.localScale = new Vector3(0.11f, 0.09f, 0.07f);
        pocket.GetComponent<Renderer>().sharedMaterial = _pocketMaterial;
        Destroy(pocket.GetComponent<Collider>());

        var strap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strap.name = "Vest Pocket Strap";
        strap.transform.SetParent(pocket.transform, false);
        strap.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        strap.transform.localScale = new Vector3(0.92f, 0.18f, 1.08f);
        strap.GetComponent<Renderer>().sharedMaterial = _strapMaterial;
        Destroy(strap.GetComponent<Collider>());

        var button = GameObject.CreatePrimitive(PrimitiveType.Cube);
        button.name = "Vest Pocket Button";
        button.transform.SetParent(pocket.transform, false);
        button.transform.localPosition = new Vector3(-0.18f, 0.18f, 0f);
        button.transform.localScale = new Vector3(0.34f, 0.2f, 0.34f);
        button.GetComponent<Renderer>().sharedMaterial = _buttonMaterial;
        Destroy(button.GetComponent<Collider>());
    }

    void CreateRingStrap(float y, float radiusX, float radiusZ)
    {
        for (int i = 0; i < RingSegmentCount; i++)
        {
            float angle0 = (i / (float)RingSegmentCount) * Mathf.PI * 2f;
            float angle1 = ((i + 1f) / RingSegmentCount) * Mathf.PI * 2f;
            Vector3 start = new Vector3(Mathf.Cos(angle0) * radiusX, y, Mathf.Sin(angle0) * radiusZ);
            Vector3 end = new Vector3(Mathf.Cos(angle1) * radiusX, y, Mathf.Sin(angle1) * radiusZ);
            CreateStrapSegment(start, end, new Vector3(0.04f, 0.035f, 0.05f));
        }
    }

    void CreateVerticalStrap(Vector3 top, Vector3 bottom)
    {
        CreateStrapSegment(top, bottom, new Vector3(0.035f, 0.03f, 0.04f));
    }

    void CreateStrapSegment(Vector3 start, Vector3 end, Vector3 thickness)
    {
        Vector3 delta = end - start;
        if (delta.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        var strap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strap.name = "Vest Strap";
        strap.transform.SetParent(transform, false);
        strap.transform.localPosition = (start + end) * 0.5f;
        strap.transform.localRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        strap.transform.localScale = new Vector3(thickness.x, thickness.y, delta.magnitude + thickness.z);
        strap.GetComponent<Renderer>().sharedMaterial = _strapMaterial;
        Destroy(strap.GetComponent<Collider>());
    }

    static void EnsureMaterials()
    {
        if (_strapMaterial == null)
        {
            _strapMaterial = CreateMaterial("Explosive Vest Strap", new Color(0.58f, 0.58f, 0.6f, 1f));
        }

        if (_pocketMaterial == null)
        {
            _pocketMaterial = CreateMaterial("Explosive Vest Pocket", new Color(0.08f, 0.08f, 0.09f, 1f));
        }

        if (_buttonMaterial == null)
        {
            _buttonMaterial = CreateMaterial("Explosive Vest Button", new Color(0.92f, 0.12f, 0.1f, 1f));
        }
    }

    static Material CreateMaterial(string name, Color color)
    {
        var shader = Shader.Find("CoreWar/VoxelFaceLit") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = name,
            mainTexture = VoxelMaterialUtility.GetSolidTexture(color)
        };

        if (material.HasProperty("_ShadowLevel"))
        {
            material.SetFloat("_ShadowLevel", 0.58f);
        }

        return material;
    }
}
