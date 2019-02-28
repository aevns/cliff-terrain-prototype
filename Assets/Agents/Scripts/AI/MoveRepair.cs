// MoveTo.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class MoveRepair : MonoBehaviour
{
    private NavMeshAgent agent;
    private MoveSwarm target;

    private static HashSet<MoveSwarm> currentTargets = new HashSet<MoveSwarm>();

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
            target.Repair();
            AcquireTarget();
        }
        else if (deltaTargetDistance > Mathf.Max(destinationDistance / 4, 2.0f))
        {
            agent.destination = target.transform.position;
        }
    }

    void AcquireTarget()
    {
        if (target)
        {
            currentTargets.Remove(target);
            target = null;
        }

        MoveSwarm[] agents = FindObjectsOfType<MoveSwarm>();
        if (agents.Length > 0)
        {
            foreach (MoveSwarm ag in agents)
            {
                if (!currentTargets.Contains(ag) && (!target || ag.GetHashCode() < target.GetHashCode()))
                    target = ag;
            }
            if (target)
            {
                agent.destination = target.transform.position;
                currentTargets.Add(target);
            }
        }
    }

    void OnDestroy()
    {
        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        trail.transform.parent = transform.parent;
        trail.autodestruct = true;
    }
}