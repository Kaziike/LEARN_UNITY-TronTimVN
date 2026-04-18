using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class SeekerMechanic : MonoBehaviour
{
    public float interactionRange = 10f;
    public LayerMask hiderLayer;
    public Camera mainCamera; // Camera của Seeker để Raycast

    // Dữ liệu khi Seeker đánh dấu một ai đó
    public HiderMechanic currentMarkedHider = null;
    public string markedNameGuess = "";

    // Quản lý số lần đoán sai
    private int wrongGuessCount = 0;
    public int maxWrongGuesses = 3;
    public float blindfoldPenaltyTime = 5f;
    private bool isBlindedPenalty = false;
    private float blindTimer = 0f;

    private InputAction interactAction;

    void Awake()
    {
        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Mouse>/leftButton");
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonWest"); // Nút vuông (PS) hoặc X (Xbox)
    }

    void OnEnable()
    {
        interactAction.Enable();
    }

    void OnDisable()
    {
        interactAction.Disable();
    }

    void Update()
    {
        // Nếu không phải là chặng Seeking hoặc đang bị phạt mù thì không cho đánh dấu
        if (HideAndSeekManager.Instance.currentState != HideAndSeekManager.GameState.SeekingPhase)
            return;

        if (isBlindedPenalty)
        {
            blindTimer -= Time.deltaTime;
            if (blindTimer <= 0)
            {
                isBlindedPenalty = false;
                UIManager.Instance.ShowBlindfold(false);
                ControllNPC npc = GetComponent<ControllNPC>();
                if (npc != null) npc.canMove = true;
            }
            return; // Chưa hết mù thì không làm gì được
        }

        // Bấm chuột trái (hoặc phím E) để phát hiện người trốn
        if (interactAction.WasPressedThisFrame())
        {
            RaycastForHider();
        }
    }

    void RaycastForHider()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, hiderLayer))
        {
            HiderMechanic targetHider = hit.collider.GetComponent<HiderMechanic>();
            if (targetHider != null && !targetHider.isEliminated && !targetHider.isSafe)
            {
                // Mở UI để Seeker đoán tên
                List<string> hiderNames = GetAllHiderNames();
                currentMarkedHider = targetHider;
                UIManager.Instance.OpenGuessNamePanel(hiderNames, OnNameGuessed);
            }
        }
    }

    private List<string> GetAllHiderNames()
    {
        List<string> names = new List<string>();
        foreach (var h in HideAndSeekManager.Instance.activeHiders)
        {
            names.Add(h.hiderName);
        }
        return names;
    }

    private void OnNameGuessed(string nameGuess)
    {
        markedNameGuess = nameGuess;
        Debug.Log("Seeker đoán mục tiêu tên là: " + nameGuess + ". Hãy quay về Base để xác nhận.");
        UIManager.Instance.ShowMessage("Đã đánh dấu báo cáo: " + nameGuess + ". Chạy về Cột để xác nhận!");
        
        // Thông báo cho Hider rằng họ đã bị đánh dấu
        if (currentMarkedHider != null)
        {
            currentMarkedHider.OnMarkedAsTarget();
        }
    }

    // Được gọi khi Seeker đập cột (Interact với BasePillar)
    public void ConfirmMarkAtBase()
    {
        if (currentMarkedHider == null || string.IsNullOrEmpty(markedNameGuess))
        {
            UIManager.Instance.ShowMessage("Chưa đánh dấu ai!");
            return;
        }

        // Kiểm tra đúng / sai
        if (currentMarkedHider.hiderName == markedNameGuess)
        {
            // Đoán đúng
            UIManager.Instance.ShowMessage("Đoán ĐÚNG! " + markedNameGuess + " đã bị loại!");
            HideAndSeekManager.Instance.EliminateHider(currentMarkedHider);
        }
        else
        {
            // Đoán sai
            wrongGuessCount++;
            UIManager.Instance.ShowMessage("Đoán SAI! " + markedNameGuess + " không phải là người đó!");

            // Hủy trạng thái bị đánh dấu của hider, vì seeker đã đoán sai
            currentMarkedHider.OnUnmarked();

            if (wrongGuessCount >= maxWrongGuesses)
            {
                ApplyBlindfoldPenalty();
                wrongGuessCount = 0; // Reset lại nấc đếm
            }
            else
            {
                // Có thể trừ thêm thời gian của trận đấu ở GameManager
                Debug.Log("Trừ thời gian do đoán sai!");
            }
        }

        // Reset bộ nhớ
        currentMarkedHider = null;
        markedNameGuess = "";
    }

    private void ApplyBlindfoldPenalty()
    {
        isBlindedPenalty = true;
        blindTimer = blindfoldPenaltyTime;
        UIManager.Instance.ShowBlindfold(true, "Mù tạm thời vì đoán sai quá 3 lần!");
        UIManager.Instance.ShowMessage("Bạn bị phạt mù tạm thời " + blindfoldPenaltyTime + " giây!");
        
        ControllNPC npc = GetComponent<ControllNPC>();
        if (npc != null) npc.canMove = false;
    }
}
