using System.Collections.Generic;
using UnityEngine;

public class HideAndSeekManager : MonoBehaviour
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
        StartGame();
    }

    public void StartGame()
    {
        currentState = GameState.HidingPhase;
        currentTimer = hidingTimeLimit;

        // Bật màn hình đen (mù tạm thời) cho Seeker lúc bắt đầu đếm
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBlindfold(true, "Đang nhắm mắt...");
        }

        if (seeker != null)
        {
            ControllNPC npc = seeker.GetComponent<ControllNPC>();
            if (npc != null) npc.canMove = false; // Khóa Seeker đứng yên
        }

        Debug.Log("GAME START: Hiding Phase. Seeker is blindfolded.");
    }

    private void Update()
    {
        if (currentState == GameState.HidingPhase)
        {
            currentTimer -= Time.deltaTime;
            
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateTimerUI(currentTimer);

            if (currentTimer <= 0)
            {
                StartSeekingPhase();
            }
        }
    }

    public void StartSeekingPhase()
    {
        currentState = GameState.SeekingPhase;

        // Tắt mù cho Seeker, bắt đầu cho phép đi tìm
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBlindfold(false);
            UIManager.Instance.ShowMessage("Bắt đầu đi tìm!");
            UIManager.Instance.UpdateTimerUI(0); // Có thể đặt lại đếm thời gian trận đấu nếu muốn
        }

        if (seeker != null)
        {
            ControllNPC npc = seeker.GetComponent<ControllNPC>();
            if (npc != null) npc.canMove = true; // Thả cho đi tìm
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
            hider.OnEliminated();
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
            hider.OnSafe();
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
