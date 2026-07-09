using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks enemies in front of the Hunter with screen-space outline icons.
/// </summary>
public static class HunterMarkSystem
{
    const float MaxRangeMeters = 300f;

    static readonly List<Transform> _markedTargets = new List<Transform>();
    static readonly List<Vector3> _markedWorldPositions = new List<Vector3>();

    public static void ClearAllMarks()
    {
        _markedTargets.Clear();
        _markedWorldPositions.Clear();
        HunterMarkOverlay.ClearMarks();
    }

    public static void ApplyMark(ThirdPersonController hunter, float durationSeconds)
    {
        ClearAllMarks();
        if (hunter == null || hunter.viewCamera == null)
        {
            return;
        }

        Vector3 origin = hunter.transform.position;
        Vector3 forward = hunter.viewCamera.transform.forward;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        foreach (var dummy in GetRangeDummies())
        {
            if (dummy == null || dummy.IsDown)
            {
                continue;
            }

            TryMarkTarget(dummy.transform, dummy.HeadMarkCenter, origin, forward);
        }

        foreach (var controller in Object.FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None))
        {
            if (controller == null || controller == hunter)
            {
                continue;
            }

            Vector3 markPosition = controller.transform.position + new Vector3(0f, 1.52f, 0f);
            TryMarkTarget(controller.transform, markPosition, origin, forward);
        }

        if (_markedTargets.Count > 0)
        {
            HunterMarkOverlay.ShowMarks(hunter.viewCamera, _markedTargets, _markedWorldPositions);
        }
    }

    static IEnumerable<ShootingRangeDummy> GetRangeDummies()
    {
        if (ShootingRangeSession.Dummies.Count > 0)
        {
            return ShootingRangeSession.Dummies;
        }

        return Object.FindObjectsByType<ShootingRangeDummy>(FindObjectsSortMode.None);
    }

    static void TryMarkTarget(Transform target, Vector3 markPosition, Vector3 origin, Vector3 forward)
    {
        if (target == null)
        {
            return;
        }

        Vector3 toTarget = markPosition - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.01f || distance > MaxRangeMeters)
        {
            return;
        }

        if (Vector3.Dot(forward, toTarget / distance) <= 0f)
        {
            return;
        }

        if (!_markedTargets.Contains(target))
        {
            _markedTargets.Add(target);
            _markedWorldPositions.Add(markPosition);
        }
    }
}
