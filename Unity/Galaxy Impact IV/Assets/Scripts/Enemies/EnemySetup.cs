using UnityEngine;
using Pathfinding;
using System.Collections;

[RequireComponent(typeof(Seeker), typeof(AILerp), typeof(AIDestinationSetter))]
public class EnemySetup : MonoBehaviour
{
    [SerializeField] private float retargetInterval = 0.25f;

    private AIDestinationSetter setter;
    private AILerp ai;
    private float nextRetargetTime;
    private bool initialized;

    private void Awake()
    {
        setter = GetComponent<AIDestinationSetter>();
        ai = GetComponent<AILerp>();
    }

    private IEnumerator Start()
    {
        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            yield break;

        yield return new WaitUntil(() => AstarPath.active != null && !AstarPath.active.isScanning);

        if (ai != null)
            ai.repathRate = 0.5f;

        RefreshTarget(forceSearch: true);
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (LanRuntime.IsActive && !LanRuntime.IsServer)
            return;

        if (Time.time < nextRetargetTime)
            return;

        nextRetargetTime = Time.time + Mathf.Max(0.05f, retargetInterval);
        RefreshTarget(forceSearch: true);
    }

    private void RefreshTarget(bool forceSearch)
    {
        if (setter == null)
            return;

        Transform player = LanRuntime.IsActive
            ? LanPlayerAvatar.GetClosestPlayerTransform(transform.position)
            : GameObject.FindGameObjectWithTag("Player")?.transform;

        bool targetChanged = setter.target != player;
        setter.target = player;

        if (ai == null)
            return;

        bool hasTarget = player != null;
        ai.canMove = hasTarget;
        ai.canSearch = hasTarget;
        ai.isStopped = !hasTarget;

        if (hasTarget && (forceSearch || targetChanged))
            ai.SearchPath();
    }
}
