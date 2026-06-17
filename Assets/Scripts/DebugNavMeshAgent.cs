using UnityEngine;
using UnityEngine.AI;

public class DebugNavMeshAgent : MonoBehaviour
{
    public NavMeshAgent agent;

    public bool path;
    public bool velocity;
    public bool desiredvelocity;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
    }


    void OnDrawGizmos()
    {
        if (velocity)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position,transform.position + agent.velocity);
        }

        if (desiredvelocity)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position,transform.position + agent.desiredVelocity);
        }

        if (path)
        {
            Gizmos.color = Color.black;
            var agentPath = agent.path;
            Vector3 prevCorner = transform.position;
            foreach (var corner in agentPath.corners)
            {
                Gizmos.DrawLine(prevCorner, corner);
                Gizmos.DrawSphere(corner, 0.1f);
                prevCorner = corner;
            }
        }
    }
}
