using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Temporary mining drill objective for Test Map 1.
/// </summary>
public class TestMapDrill : MonoBehaviour
{
    GameSession.Team _team;
    bool _isWorking = true;
    Transform _bit;
    Transform _accentRing;

    public GameSession.Team Team => _team;
    public bool IsWorking => _isWorking;
    public Vector3 UsePoint => transform.position + Vector3.up * 0.8f;

    public static TestMapDrill Create(Transform parent, GameSession.Team team, Vector3 position)
    {
        var root = new GameObject($"{team} Drill");
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        var drill = root.AddComponent<TestMapDrill>();
        drill._team = team;
        drill.BuildVisual();
        TestMapObjectiveManager.Instance?.RegisterDrill(drill);
        return drill;
    }

    void Update()
    {
        if (_bit != null && _isWorking)
        {
            _bit.Rotate(Vector3.up, 360f * Time.deltaTime, Space.Self);
        }

        if (_accentRing != null)
        {
            float pulse = _isWorking ? (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f : 0.18f;
            _accentRing.localScale = new Vector3(1f + pulse * 0.08f, 0.08f, 1f + pulse * 0.08f);
        }
    }

    public void ToggleWorking()
    {
        SetWorking(!_isWorking);
    }

    public void SetWorking(bool working)
    {
        _isWorking = working;
    }

    void BuildVisual()
    {
        Color teamColor = GameSession.TeamColor(_team);
        var dark = CreateMaterial("Drill Black", new Color(0.015f, 0.015f, 0.018f, 1f));
        var metal = CreateMaterial("Drill Gunmetal", new Color(0.34f, 0.35f, 0.37f, 1f));
        var accent = CreateMaterial($"{_team} Drill Accent", teamColor);

        CreateCylinder("Base", new Vector3(0f, 0.1f, 0f), new Vector3(1.25f, 0.2f, 1.25f), metal);
        CreateCylinder("Column", new Vector3(0f, 0.65f, 0f), new Vector3(0.65f, 0.9f, 0.65f), dark);
        _accentRing = CreateCylinder("Accent Ring", new Vector3(0f, 1.08f, 0f), new Vector3(0.92f, 0.08f, 0.92f), accent).transform;
        CreateCube("Motor", new Vector3(0f, 1.35f, 0f), new Vector3(1.15f, 0.55f, 0.9f), metal);
        _bit = CreateCylinder("Drill Bit", new Vector3(0f, 0.22f, 0f), new Vector3(0.25f, 0.7f, 0.25f), dark).transform;
        _bit.rotation = Quaternion.Euler(0f, 0f, 180f);
        CreateCylinder("Team Beacon", new Vector3(0f, 1.78f, 0f), new Vector3(0.38f, 0.16f, 0.38f), accent);

        var trigger = gameObject.AddComponent<SphereCollider>();
        trigger.radius = TestMapObjectiveManager.DrillUseDistanceMeters;
        trigger.center = Vector3.up * 0.8f;
        trigger.isTrigger = true;
    }

    GameObject CreateCube(string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        ApplyMaterial(go, material);
        return go;
    }

    GameObject CreateCylinder(string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        ApplyMaterial(go, material);
        return go;
    }

    static void ApplyMaterial(GameObject go, Material material)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
            renderer.receiveShadows = true;
        }
    }

    static Material CreateMaterial(string name, Color color)
    {
        var material = new Material(Shader.Find("Standard"))
        {
            name = name,
            color = color
        };
        material.SetFloat("_Glossiness", 0.18f);
        return material;
    }
}
