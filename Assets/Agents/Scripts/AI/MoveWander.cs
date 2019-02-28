// MoveTo.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MoveWander : MonoBehaviour
{
    [SerializeField] private GameObject damaged;
    [SerializeField] private GameObject destroyed;

    [SerializeField] private Vector2 regionOffset = Vector2.zero;
    [SerializeField] private Vector2 regionScale = 256 * Vector2.one;

    private NavMeshAgent agent;
    private bool dead = false;
    private float safetyTimer;
    private int deepMask;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        safetyTimer = Time.time + 5f;
        deepMask = 1 << NavMesh.GetAreaFromName("Deep");
    }

    void Update()
    {
        if (Vector3.Distance(agent.destination, agent.transform.position) <= 2.0f)
        {
            Vector3 point = new Vector3(
                Random.value * regionScale.x + regionOffset.x,
                64f,
                Random.value * regionScale.y + regionOffset.y);

            NavMeshHit hit;

            if (NavMesh.SamplePosition(point, out hit, 128f, agent.areaMask))
            {
                if (hit.mask == deepMask) return;
                agent.destination = hit.position;
            }
        }
    }

    public void IsKill()
    {
        if (dead || Time.time <= safetyTimer) return;

        dead = true;
        Instantiate(damaged, transform.position, transform.rotation, transform.parent);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        trail.transform.parent = transform.parent;
        trail.autodestruct = true;
    }
}