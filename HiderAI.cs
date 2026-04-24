using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class HiderAI : MonoBehaviour
{
    public LayerMask obstructionMask;
    public float coverSearchRadius = 30f;
    public float safeDistanceFromSeeker = 15f;
    public float seekRadiusForBase = 40f; 
    
    private NavMeshAgent agent;
    private HiderMechanic hiderMechanic;
    private Transform pillar;
    
    private enum State { Idle, FindCover, MoveToCover, Hide, RunToPillar }
    private State currentState;
    
    private Vector3 currentCoverPosition;
    
    // Thêm tham chiếu đến Animator
    public Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        hiderMechanic = GetComponent<HiderMechanic>();
        
        BasePillar basePillar = FindObjectOfType<BasePillar>();
        if (basePillar != null)
            pillar = basePillar.transform;
            
        currentState = State.FindCover;
    }

    void Update()
    {
        if (hiderMechanic != null && (hiderMechanic.isEliminated || hiderMechanic.isSafe))
        {
            if (agent.isOnNavMesh && !agent.isStopped) 
                agent.ResetPath();
            return;
        }

        

        Transform seeker = HideAndSeekManager.Instance != null ? HideAndSeekManager.Instance.seeker : null;

        switch (currentState)
        {
            case State.Idle:
            case State.FindCover:
                FindCoverSpot(seeker.position);
                break;
            case State.MoveToCover:
                MoveToCover(seeker.position);
                break;
            case State.Hide:
                Hide(seeker.position);
                break;
            case State.RunToPillar:
                RunToPillar(seeker.position);
                break;
        }
        if(currentState == State.FindCover || currentState == State.MoveToCover || currentState == State.RunToPillar)
        {
            animator.SetTrigger("isWalking");
            
        }
        if(currentState == State.Idle || currentState == State.Hide)
        {
            animator.SetTrigger("idle");
            
        }
    }

    void FindCoverSpot(Vector3 seekerPosition)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, coverSearchRadius, obstructionMask);
        
        Vector3 bestSpot = transform.position;
        float bestScore = float.MinValue;
        bool foundCover = false;

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject || (HideAndSeekManager.Instance != null && HideAndSeekManager.Instance.seeker != null && col.gameObject == HideAndSeekManager.Instance.seeker.gameObject)) 
                continue;

            Vector3 directionFromSeekerToObstacle = (col.transform.position - seekerPosition).normalized;
            
            Vector3 potentialCoverPos = col.transform.position + directionFromSeekerToObstacle * (col.bounds.extents.magnitude + 1.5f);

            if (NavMesh.SamplePosition(potentialCoverPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                float distToSeeker = Vector3.Distance(hit.position, seekerPosition);
                float distFromMe = Vector3.Distance(transform.position, hit.position);

                float score = distToSeeker * 2f - distFromMe;
                
                if (IsPositionHiddenFromSeeker(hit.position, seekerPosition))
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSpot = hit.position;
                        foundCover = true;
                    }
                }
            }
        }

        if (foundCover)
        {
            currentCoverPosition = bestSpot;
            if (agent.isOnNavMesh) agent.SetDestination(currentCoverPosition);
            currentState = State.MoveToCover;
        }
        else
        {
            Vector3 fleeDir = (transform.position - seekerPosition).normalized;
            Vector3 fleePos = transform.position + fleeDir * 15f;
            if (NavMesh.SamplePosition(fleePos, out NavMeshHit fleeHit, 10f, NavMesh.AllAreas))
            {
                currentCoverPosition = fleeHit.position;
                if (agent.isOnNavMesh) agent.SetDestination(currentCoverPosition);
                currentState = State.MoveToCover;
            }
        }
    }
    
    bool IsPositionHiddenFromSeeker(Vector3 pos, Vector3 seekerPos)
    {
        Vector3 direction = (seekerPos - pos).normalized;
        float distance = Vector3.Distance(pos, seekerPos);
        
        Ray ray = new Ray(pos + Vector3.up * 1f, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, distance, obstructionMask))
        {
            return true; 
        }
        return false;
    }

    void MoveToCover(Vector3 seekerPosition)
    {
        if (Vector3.Distance(transform.position, currentCoverPosition) <= agent.stoppingDistance + 0.5f)
        {
            currentState = State.Hide;
        }
        else if (Vector3.Distance(transform.position, seekerPosition) < safeDistanceFromSeeker && !IsPositionHiddenFromSeeker(transform.position, seekerPosition))
        {
            currentState = State.FindCover;
        }
    }

    void Hide(Vector3 seekerPosition)
    {
        float distanceToSeeker = Vector3.Distance(transform.position, seekerPosition);
        
        if (!IsPositionHiddenFromSeeker(transform.position, seekerPosition))
        {
            if (distanceToSeeker < safeDistanceFromSeeker * 1.5f) 
            {
                currentState = State.FindCover;
                return;
            }
        }

        if (pillar != null)
        {
            float distanceToPillar = Vector3.Distance(transform.position, pillar.position);
            bool shouldRunToBase = false;

            // Nếu người tìm ở rất xa, và ta gần Cột
            if (distanceToSeeker > safeDistanceFromSeeker * 1.5f && distanceToPillar < distanceToSeeker * 1.5f)
            {
                shouldRunToBase = true;
            }
            // HOẶC nếu Cột cực kì gần ngay trước mặt, mạo hiểm chạy vào luôn
            else if (distanceToPillar < distanceToSeeker * 0.7f && distanceToPillar < 20f)
            {
                shouldRunToBase = true;
            }

            if (shouldRunToBase)
            {
                currentState = State.RunToPillar;
                if (agent.isOnNavMesh) agent.SetDestination(pillar.position);
                return;
            }
        }
    }

    void RunToPillar(Vector3 seekerPosition)
    {
        if (pillar == null)
        {
            currentState = State.FindCover;
            return;
        }
        
        float distanceToSeeker = Vector3.Distance(transform.position, seekerPosition);
        
        // Nếu trong lúc chạy về Cột mà Seeker lại gần / nhìn thấy, ta huỷ kế hoạch và đi trốn lại
        if (!IsPositionHiddenFromSeeker(transform.position, seekerPosition) && distanceToSeeker < safeDistanceFromSeeker)
        {
            currentState = State.FindCover;
            return;
        }
        
        
    }
}
