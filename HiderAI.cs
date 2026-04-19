using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class HiderAI : MonoBehaviour
{
    public LayerMask obstructionMask;
    public float coverSearchRadius = 30f;
    public float safeDistanceFromSeeker = 15f;
    public float seekRadiusForBase = 40f; // Khoảng cách xa so với Seeker để Hider bắt đầu chạy về cột
    
    private NavMeshAgent agent;
    private HiderMechanic hiderMechanic;
    private Transform pillar;
    
    private enum State { Idle, FindCover, MoveToCover, Hide, RunToPillar }
    private State currentState;
    
    private Vector3 currentCoverPosition;

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
        if (seeker == null) return;

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
    }

    void FindCoverSpot(Vector3 seekerPosition)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, coverSearchRadius, obstructionMask);
        
        Vector3 bestSpot = transform.position;
        float bestScore = float.MinValue;
        bool foundCover = false;

        foreach (var col in colliders)
        {
            // Tránh tìm kiếm cover trên các collider không phù hợp (vd: chính bản thân, seeker)
            if (col.gameObject == gameObject || (HideAndSeekManager.Instance != null && HideAndSeekManager.Instance.seeker != null && col.gameObject == HideAndSeekManager.Instance.seeker.gameObject)) 
                continue;

            // Tìm vị trí nấp phía sau vật cản so với góc nhìn của seeker
            Vector3 directionFromSeekerToObstacle = (col.transform.position - seekerPosition).normalized;
            
            // Lấy khoảng cách xa hơn một chút so với mép collider
            Vector3 potentialCoverPos = col.transform.position + directionFromSeekerToObstacle * (col.bounds.extents.magnitude + 1.5f);

            if (NavMesh.SamplePosition(potentialCoverPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                // Ưu tiên nấp vị trí xa seeker, và gần với mình
                float distToSeeker = Vector3.Distance(hit.position, seekerPosition);
                float distFromMe = Vector3.Distance(transform.position, hit.position);

                float score = distToSeeker * 2f - distFromMe;
                
                // Đảm bảo tại vị trí nấp mới này không bị seeker nhìn tới
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
            // Fallback: Nếu không tìm thấy, chạy né ra xa Seeker
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
        
        // Raycast từ vị trí với độ cao nhỏ (vd: tầm eo) về hướng seeker
        Ray ray = new Ray(pos + Vector3.up * 1f, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, distance, obstructionMask))
        {
            // Hit vào vật cản là bị khuất tầm nhìn (tốt)
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
            // Nếu đang di chuyển mà Seeker phát hiện / lại gần thì tìm góc núp khác
            currentState = State.FindCover;
        }
    }

    void Hide(Vector3 seekerPosition)
    {
        float distanceToSeeker = Vector3.Distance(transform.position, seekerPosition);
        
        // Nếu seeker tìm thấy hoặc tới quá gần, bỏ chạy và tìm chỗ khác
        if (!IsPositionHiddenFromSeeker(transform.position, seekerPosition))
        {
            if (distanceToSeeker < safeDistanceFromSeeker * 1.5f) 
            {
                currentState = State.FindCover;
                return;
            }
        }

        // Logic Mạo Hiểm (Thắng): Nếu Seeker đi quá xa, ta thử chạy về Cột
        if (pillar != null && distanceToSeeker > seekRadiusForBase)
        {
            float distanceToPillar = Vector3.Distance(transform.position, pillar.position);
            
            // Quyết định chạy khi Cột cách gần ta hơn so với Seeker cách ta
            if (distanceToPillar < distanceToSeeker * 0.8f) // Dư dả 1 chút an toàn
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
        
        // Tới nơi đập Cột
        if (Vector3.Distance(transform.position, pillar.position) <= 2.5f)
        {
            if (hiderMechanic != null && !hiderMechanic.isSafe && !hiderMechanic.isEliminated)
            {
                hiderMechanic.CheckInAtBase();
                agent.ResetPath();
            }
        }
    }
}
