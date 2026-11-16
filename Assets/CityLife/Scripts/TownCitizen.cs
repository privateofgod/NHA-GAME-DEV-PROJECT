using UnityEngine;
using UnityEngine.AI;

namespace TownLife
{
    [RequireComponent(typeof(Animator))]
    public class TownCitizen : MonoBehaviour
    {
        public float radius = 12f;
        public bool stationary;
        public string idleState = "Idle";
        public float phase;
        Vector3 home;
        NavMeshAgent agent;
        Animator animator;
        float nextDecision;
        bool greeted;
        Transform player;
        void Start()
        {
            home = transform.position;
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            agent = GetComponent<NavMeshAgent>();
            player = GameObject.FindWithTag("Player")?.transform;
            animator.Play(idleState, 0, phase);
            nextDecision = Time.time + 2 + phase * 8;
            if (agent != null)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(home, out hit, 4, NavMesh.AllAreas)) agent.Warp(hit.position);
                else agent.enabled = false;
            }
        }
        void Update()
        {
            bool near = player != null && (player.position - transform.position).sqrMagnitude < 20;
            if (near && !greeted && idleState == "Idle") animator.SetTrigger("Wave");
            greeted = near;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = stationary || near;
            animator.SetFloat("Speed_f", agent.velocity.magnitude);
            if (stationary || near || Time.time < nextDecision || agent.pathPending) return;
            if (agent.hasPath && agent.remainingDistance > 1) return;
            nextDecision = Time.time + Random.Range(3f, 8f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                NavMeshHit hit;
                if (!NavMesh.SamplePosition(home + new Vector3(offset.x, 0, offset.y), out hit, 2, NavMesh.AllAreas)) continue;
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                { agent.SetPath(path); break; }
            }
        }
    }
}
