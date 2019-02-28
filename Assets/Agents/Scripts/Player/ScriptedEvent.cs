using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ScriptedEvent : MonoBehaviour
{
    [SerializeField] private MoveTo playerAgent;
    [SerializeField] private Vector3 target;
    [SerializeField] private GameObject[] uIElements;
    [SerializeField] private GameObject uIMessage;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = playerAgent.GetComponent<NavMeshAgent>();

        playerAgent.enabled = false;
        foreach (GameObject obj in uIElements)
        {
            obj.SetActive(false);
        }
        uIMessage.SetActive(false);
    }

    private IEnumerator Start()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 64f, agent.areaMask))
        {
            agent.destination = hit.position;
            yield return new WaitWhile(() => Vector3.Distance(agent.transform.position, agent.destination) > 2f);
            playerAgent.enabled = true;
            foreach (GameObject obj in uIElements)
            {
                obj.SetActive(true);
            }
            uIMessage.SetActive(true);

            yield return new WaitWhile(() => Vector3.Distance(agent.transform.position, agent.destination) <= 2f);
            uIMessage.SetActive(false);
        }
        else
        {
            playerAgent.enabled = true;
            foreach (GameObject obj in uIElements)
            {
                obj.SetActive(true);
            }
            uIMessage.SetActive(true);
        }
        Destroy(this);
    }
}