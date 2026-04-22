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

    // Vị trí cuối cùng nhìn thấy hider để sau khi về cột có thể quay lại thẳng chỗ đó
    private Vector3? lastKnownTargetLocation;
 
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
        // Liên tục kiểm tra xem có ai không, ưu tiên việc bắt người đầu tiên nhìn thấy
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
            if (angle <= viewAngle * 1f)
            {
                Ray ray = new Ray(transform.position + Vector3.up * 1.2f, direction);
                if (Physics.Raycast(ray, out RaycastHit hit, detectionRadius, obstructionMask))
                {
                    if (hit.transform == hider.transform || hit.collider.GetComponent<HiderMechanic>() == hider)
                    {
                        targetHider = hider;
                        // Lưu lại để có thể nhớ sau này
                        lastKnownTargetLocation = hider.transform.position;
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

        // Bắt đầu chu trình đuổi bắt mới
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
        // 1. Ưu tiên thăm lại chỗ nhìn thấy Hider lúc nãy (để xử lý những người núp chung vị trí!)
        if (lastKnownTargetLocation.HasValue)
        {
            if (NavMesh.SamplePosition(lastKnownTargetLocation.Value, out NavMeshHit kHit, patrolRadius, NavMesh.AllAreas))
            {
                patrolPoint = kHit.position;
                agent.SetDestination(patrolPoint);
                isPatrolling = true;
                isIdle = false;
                lastKnownTargetLocation = null; // Huỷ trí nhớ sau khi đã bắt đầu đi tới đó
                return;
            }
            lastKnownTargetLocation = null;
        }

        // 2. Đi dò các vật cản gần đây có thể núp được (obstructionMask)
        Collider[] obstacles = Physics.OverlapSphere(transform.position, patrolRadius, obstructionMask);
        if (obstacles.Length > 0)
        {
            // Tránh Seeker check nhầm chính mình hoặc Hider đang đứng
            System.Collections.Generic.List<Collider> validObstacles = new System.Collections.Generic.List<Collider>();
            foreach(var obs in obstacles)
            {
                if (obs.gameObject != this.gameObject && obs.GetComponent<HiderMechanic>() == null)
                    validObstacles.Add(obs);
            }

            if (validObstacles.Count > 0)
            {
                Collider targetObstacle = validObstacles[Random.Range(0, validObstacles.Count)];
                
                // Múi giờ dò tìm: Random một góc nhìn xung quanh tâm vật cản
                Vector3 randomOffset = Random.insideUnitSphere * (targetObstacle.bounds.extents.magnitude + 2.5f);
                randomOffset.y = 0;
                Vector3 offsetPoint = targetObstacle.bounds.center + randomOffset;

                if (NavMesh.SamplePosition(offsetPoint, out NavMeshHit obsHit, 5f, NavMesh.AllAreas))
                {
                    patrolPoint = obsHit.position;
                    agent.SetDestination(patrolPoint);
                    isPatrolling = true;
                    isIdle = false;
                    return;
                }
            }
        }

        // 3. (Fallback) Nếu trót ở bãi trống thì đi random
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
