using UnityEngine;

/// <summary>
/// Builds shooting range target dummies spread across the lane.
/// </summary>
public static class ShootingRangeBuilder
{
    static readonly Color SignRed = new Color(0.82f, 0.12f, 0.12f);

    public static void BuildTargets(
        Transform root,
        Vector3 gridOrigin,
        float voxelSize,
        int gridWidth,
        PhysicsMaterial colliderMaterial)
    {
        var targetsRoot = new GameObject("Shooting Range Targets").transform;
        targetsRoot.SetParent(root, false);

        for (int i = 0; i < ShootingRangeSession.TargetDistancesMeters.Length; i++)
        {
            int distance = ShootingRangeSession.TargetDistancesMeters[i];
            float targetX = ShootingRangeSession.TargetWorldX(distance);
            float targetZ = ShootingRangeSession.TargetWorldZ(distance);
            var position = new Vector3(targetX, 0f, targetZ);
            BuildTargetDummy(targetsRoot, position, distance);
        }
    }

    static void BuildTargetDummy(Transform parent, Vector3 worldPosition, int distanceMeters)
    {
        var dummy = ShootingRangeDummy.Create(
            parent,
            worldPosition,
            distanceMeters,
            Quaternion.Euler(0f, 180f, 0f));
        CreateDistanceSign(dummy.transform, distanceMeters);
    }

    static void CreateDistanceSign(Transform dummyRoot, int distanceMeters)
    {
        var signRoot = new GameObject("Distance Sign");
        signRoot.transform.SetParent(dummyRoot, false);
        signRoot.transform.localPosition = new Vector3(0f, 1.02f, 0.24f);
        signRoot.transform.localRotation = Quaternion.identity;

        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Sign Board";
        board.transform.SetParent(signRoot.transform, false);
        board.transform.localPosition = Vector3.zero;
        board.transform.localScale = new Vector3(0.55f, 0.28f, 0.04f);
        board.GetComponent<Renderer>().sharedMaterial =
            VoxelMaterialUtility.CreateSolidMaterial(new Color(0.92f, 0.9f, 0.86f), "Range Sign Board");
        Object.Destroy(board.GetComponent<Collider>());

        var textGo = new GameObject("Sign Text");
        textGo.transform.SetParent(signRoot.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 0f, -0.03f);
        textGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        textGo.transform.localScale = Vector3.one;

        var text = textGo.AddComponent<TextMesh>();
        text.text = $"{distanceMeters}m";
        text.fontSize = 48;
        text.characterSize = 0.035f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = SignRed;
        text.fontStyle = FontStyle.Bold;
    }
}
