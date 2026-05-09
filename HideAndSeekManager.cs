using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class HideAndSeekManager : NetworkBehaviour
{
    public static HideAndSeekManager Instance { get; private set; }

    public enum GameState
    {
        Setup,          // Chuẩn bị
        HidingPhase,    // Người tìm đếm (nhắm mắt), người trốn đi trốn
        SeekingPhase,   // Bắt đầu đi tìm
        GameOver        // Kết thúc trò chơi
    }

    public GameState currentState;

    [Header("Game Settings")]
    public float hidingTimeLimit = 60f; // Thời gian Seeker phải nhắm mắt
    private float currentTimer;

    [Header("Players Tracking")]
    public Transform seeker; // Người đi bắt
    public List<HiderMechanic> activeHiders = new List<HiderMechanic>(); // Danh sách những người đang trốn
    public List<HiderMechanic> safeHiders = new List<HiderMechanic>();   // Danh sách những người đã về đích an toàn
    public List<HiderMechanic> eliminatedHiders = new List<HiderMechanic>(); // Những người bị bắt

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        currentState = GameState.Setup; // Bắt đầu ở chế độ chờ
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        if (currentState != GameState.Setup) return;
        StartGameClientRpc();
    }

    [ClientRpc]
    public void StartGameClientRpc()
    {
        currentState = GameState.HidingPhase;
        currentTimer = hidingTimeLimit;

        TeleportLocalPlayerToPillar();

        ControllNPC localPlayer = GetLocalPlayer();
        bool isLocalSeeker = (localPlayer != null && localPlayer.GetComponent<SeekerMechanic>() != null);

        if (isLocalSeeker)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowBlindfold(true, "Đang nhắm mắt...");
            
            if (localPlayer != null)
                localPlayer.canMove = false; // Khóa Seeker
        }
        else
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowMessage("Trò chơi bắt đầu! Nhanh đi trốn đi!");
        }

        Debug.Log("GAME START: Hiding Phase.");
    }

    private ControllNPC GetLocalPlayer()
    {
        ControllNPC[] players = FindObjectsOfType<ControllNPC>();
        foreach(var p in players)
        {
            if(p.IsOwner) return p;
        }
        return null;
    }

    private void TeleportLocalPlayerToPillar()
    {
        ControllNPC localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        BasePillar pillar = FindObjectOfType<BasePillar>();
        if (pillar != null)
        {
            CharacterController cc = localPlayer.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Random vị trí quanh cột (bán kính 3 -> 6)
            Vector2 randCircle = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
            Vector3 targetPos = pillar.transform.position + new Vector3(randCircle.x, 0, randCircle.y);
            
            if (Physics.Raycast(targetPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            {
                targetPos.y = hit.point.y + 0.1f;
            }

            localPlayer.transform.position = targetPos;
            localPlayer.transform.LookAt(pillar.transform.position);

            if (cc != null) cc.enabled = true;
        }
    }

    private void Update()
    {
        if (currentState == GameState.HidingPhase)
        {
            currentTimer -= Time.deltaTime;
            
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateTimerUI(currentTimer);

            if (IsServer && currentTimer <= 0)
            {
                currentState = GameState.SeekingPhase; // Để Server không gọi liên tục
                StartSeekingPhaseClientRpc();
            }
        }
    }

    [ClientRpc]
    public void StartSeekingPhaseClientRpc()
    {
        currentState = GameState.SeekingPhase;

        ControllNPC localPlayer = GetLocalPlayer();
        bool isLocalSeeker = (localPlayer != null && localPlayer.GetComponent<SeekerMechanic>() != null);

        if (isLocalSeeker)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowBlindfold(false);
                UIManager.Instance.ShowMessage("Bắt đầu đi tìm!");
                UIManager.Instance.UpdateTimerUI(0);
            }
            if (localPlayer != null)
                localPlayer.canMove = true;
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage("Seeker bắt đầu đi tìm rồi!");
                UIManager.Instance.UpdateTimerUI(0);
            }
        }

        Debug.Log("SEEKING PHASE: Seeker is released.");
    }

    public void RegisterHider(HiderMechanic hider)
    {
        if (!activeHiders.Contains(hider))
        {
            activeHiders.Add(hider);
        }
    }

    // Khi Seeker đập cột và xác nhận đúng người
    public void EliminateHider(HiderMechanic hider)
    {
        if (activeHiders.Contains(hider))
        {
            activeHiders.Remove(hider);
            eliminatedHiders.Add(hider);
            hider.OnEliminatedClientRpc();
            CheckGameOver();
        }
    }

    // Khi người trốn tự đập cột an toàn (Win)
    public void SafelyCheckInHider(HiderMechanic hider)
    {
        if (activeHiders.Contains(hider))
        {
            activeHiders.Remove(hider);
            safeHiders.Add(hider);
            hider.OnSafeClientRpc();
            CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        if (activeHiders.Count == 0)
        {
            currentState = GameState.GameOver;
            Debug.Log("Game Over!");
            
            // Hiện log ai thắng (Safe vs Eliminated)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage("Trò chơi kết thúc!");
            }
        }
    }
}
