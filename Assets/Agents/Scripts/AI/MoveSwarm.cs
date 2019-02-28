using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MoveSwarm : MonoBehaviour
{
    [SerializeField] private GameObject repaired;

    private NavMeshAgent agent;
    private MoveSwarm target;

    private bool isRepaired;
    private float safetyTimer;


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        safetyTimer = Time.time + 5f;
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
            AcquireTarget();
        }

        else if (deltaTargetDistance > Mathf.Max(destinationDistance / 4, 2.0f))
        {
            agent.destination = target.transform.position;
        }
    }

    void AcquireTarget()
    {
        target = null;

        MoveSwarm[] agents = FindObjectsOfType<MoveSwarm>();
        if (agents.Length > 0)
        {
            target = agents[Mathf.FloorToInt(Random.value * agents.Length) % agents.Length];
            agent.destination = target.transform.position;
        }
    }

    public void Repair()
    {
        if (isRepaired || Time.time <= safetyTimer) return;

        isRepaired = true;
        Instantiate(repaired, transform.position, transform.rotation, transform.parent);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        trail.transform.parent = transform.parent;
        trail.autodestruct = true;
    }
}