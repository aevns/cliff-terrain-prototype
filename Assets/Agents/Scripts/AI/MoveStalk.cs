// MoveTo.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class MoveStalk : MonoBehaviour
{
    [SerializeField] private GameObject damaged;

    private NavMeshAgent agent;
    private MoveWander target;

    private static HashSet<MoveWander> currentTargets = new HashSet<MoveWander>();

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (agent.pathPending || !agent.isActiveAndEnabled) return;

        if (!target || !agent.hasPath)
        {
            AcquireTarget();
            return;
        }

        float destinationDistance = Vector3.Distance(agent.transform.position, agent.destination);
        float deltaTargetDistance = Vector3.Distance(agent.destination, target.transform.position);

        if (destinationDistance <= 2.5f)
        {
            target.IsKill();
            AcquireTarget();
        }
        else if (deltaTargetDistance > Mathf.Max(destinationDistance / 4, 2.0f))
        {
            AcquireTarget();
        }
    }

    void AcquireTarget()
    {
        if (target)
        {
            currentTargets.Remove(target);
            target = null;
        }

        MoveWander[] agents = FindObjectsOfType<MoveWander>();
        if (agents.Length > 0)
        {
            foreach (MoveWander ag in agents)
            {
                if (!currentTargets.Contains(ag) && (!target || Vector3.SqrMagnitude(ag.transform.position - transform.position) < Vector3.SqrMagnitude(target.transform.position - transform.position)))
                    target = ag;
            }
            if (!target)
                target = agents[Mathf.FloorToInt(Random.value * agents.Length) % agents.Length];
            agent.destination = target.transform.position;
            currentTargets.Add(target);
        }
    }

    void OnDestroy()
    {
        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        trail.transform.parent = transform.parent;
        trail.autodestruct = true;
    }
}
