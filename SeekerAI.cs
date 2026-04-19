using UnityEngine;
using UnityEngine.AI;
 
public class SeekerAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform pillar;
    public Animator animator;
 
    [Header("Settings")]
    public float detectionRadius = 15f;
    public float viewAngle = 120f;
    public LayerMask obstructionMask = Physics.DefaultRaycastLayers;
    public float patrolRadius = 20f;
    public float patrolIdleTime = 3f;
    public float rotationSpeed = 7f;
 
    private NavMeshAgent agent;
    private float idleTimer;
 
    private Vector3 patrolPoint;
    private bool isPatrolling;
    private bool isIdle;
    private bool targetMarked;
 
    private HiderMechanic markedHider;
    private SeekerMechanic seekerMechanic;
    private HiderMechanic targetHider;
 
    private enum State { Patrol, Chase }
    private State currentState;
 
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        seekerMechanic = GetComponent<SeekerMechanic>();
 
        if (pillar == null)
        {
            BasePillar basePillar = FindObjectOfType<BasePillar>();
            if (basePillar != null)
                pillar = basePillar.transform;
        }
 
        SetNewPatrolPoint();
        currentState = State.Patrol;
    }
 
    void Update()
    {
        if (!targetMarked && CanSeeAnyHider())
        {
            MarkHiderAndRunToPillar();
        }
 
        if (targetMarked)
        {
            ReturnToPillar();
        }
        else
        {
            switch (currentState)
            {
                case State.Patrol: Patrol(); break;
                case State.Chase: ChasePlayer(); break;
            }
        }
 
        if (animator != null)
            animator.SetBool("isWalking", agent.velocity.magnitude > 0.1f);
 
        RotateTowardsMovementDirection();
    }
 
    bool CanSeeAnyHider()
    {
        if (HideAndSeekManager.Instance == null) return false;

        foreach (HiderMechanic hider in HideAndSeekManager.Instance.activeHiders)
        {
            if (hider == null || hider.isEliminated || hider.isSafe) continue;

            float distance = Vector3.Distance(transform.position, hider.transform.position);
            if (distance > detectionRadius) continue;

            Vector3 direction = (hider.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, direction);
            if (angle <= viewAngle * 0.5f)
            {
                Ray ray = new Ray(transform.position + Vector3.up * 1.2f, direction);
                if (Physics.Raycast(ray, out RaycastHit hit, detectionRadius, obstructionMask))
                {
                    if (hit.transform == hider.transform || hit.collider.GetComponent<HiderMechanic>() == hider)
                    {
                        targetHider = hider;
                        return true;
                    }
                }
            }
        }

        return false;
    }
 
    void MarkHiderAndRunToPillar()
    {
        if (targetHider == null || pillar == null) return;
 
        markedHider = targetHider;
        markedHider.OnMarkedAsTarget();
        targetMarked = true;
        currentState = State.Chase;
        agent.SetDestination(pillar.position);
 
        if (UIManager.Instance != null)
            UIManager.Instance.ShowMessage("Seeker đã đánh dấu người trốn và đang chạy về cột!");
 
        if (seekerMechanic != null)
        {
            seekerMechanic.currentMarkedHider = markedHider;
            seekerMechanic.markedNameGuess = markedHider.hiderName;
        }
    }
 
    void ReturnToPillar()
    {
        if (pillar == null)
        {
            BasePillar basePillar = FindObjectOfType<BasePillar>();
            if (basePillar != null)
                pillar = basePillar.transform;
            else
                return;
        }
 
        if (agent.isOnNavMesh)
            agent.SetDestination(pillar.position);
 
        if (Vector3.Distance(transform.position, pillar.position) < 2f)
        {
            ConfirmMarkedTarget();
        }
    }
 
    void ConfirmMarkedTarget()
    {
        if (!targetMarked || markedHider == null)
            return;
 
        if (seekerMechanic != null)
        {
            seekerMechanic.ConfirmMarkAtBase();
        }
        else if (HideAndSeekManager.Instance != null)
        {
            HideAndSeekManager.Instance.EliminateHider(markedHider);
        }
 
        targetMarked = false;
        markedHider = null;
        SetNewPatrolPoint();
        currentState = State.Patrol;
    }
 
    void Patrol()
    {
        if (isIdle)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= patrolIdleTime)
            {
                SetNewPatrolPoint();
                idleTimer = 0f;
            }
            return;
        }
 
        if (!isPatrolling || Vector3.Distance(transform.position, patrolPoint) < 1.5f)
        {
            isIdle = true;
            isPatrolling = false;
            agent.ResetPath();
        }
    }
 
    void SetNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius + transform.position;
 
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
            agent.SetDestination(patrolPoint);
            isPatrolling = true;
            isIdle = false;
        }
    }
 
    void ChasePlayer()
    {
        isIdle = false;
        isPatrolling = false;
 
        if (agent.isOnNavMesh && targetHider != null)
        {
            agent.SetDestination(targetHider.transform.position);
        }
        else if (agent.isOnNavMesh && player != null)
        {
            // Fallback to player if targetHider isn't set somehow
            agent.SetDestination(player.position);
        }
    }
 
    void RotateTowardsMovementDirection()
    {
        if (agent == null) return;
 
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
