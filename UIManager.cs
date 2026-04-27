using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject blindfoldPanel;    // Phủ đen màn hình
    public Text blindfoldMessageText;
    
    public GameObject guessNamePanel;    // Bảng để Seeker chọn tên người trốn
    public Transform namesContainer;     // Chứa các nút tên
    public GameObject nameButtonPrefab;  // Prefab nút có chứa Text để hiển thị tên
    public GameObject Time;
    public Text timerText;               // Hiện thời gian đếm ngược
    public Text gameMessageText;         // Hiện thông báo chung

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Ẩn các bảng ban đầu
        if(blindfoldPanel) blindfoldPanel.SetActive(false);
        if(guessNamePanel) guessNamePanel.SetActive(false);
        if(gameMessageText) gameMessageText.text = "";
    }

    public void ShowBlindfold(bool show, string message = "")
    {
        if (blindfoldPanel != null)
        {
            blindfoldPanel.SetActive(show);
            if (show && blindfoldMessageText != null)
            {
                blindfoldMessageText.text = message;
            }
        }
    }

    public void UpdateTimerUI(float time)
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(time).ToString() + "s";
        }
        if (timerText != null && timerText.text == "0s")
        {
            Time.SetActive(false);
        }
    }

    public void ShowMessage(string msg)
    {
        if (gameMessageText != null)
        {
            gameMessageText.text = msg;
            // Có thể dùng Coroutine để ẩn text sau vài giây
            Invoke(nameof(ClearMessage), 3f);
        }
    }

    private void ClearMessage()
    {
        if (gameMessageText != null) gameMessageText.text = "";
    }

    // Mở bảng chọn tên khi Seeker muốn đoán
    public void OpenGuessNamePanel(List<string> hiderNames, System.Action<string> onNameSelectedCallback)
    {
        if (guessNamePanel == null) return;
        
        guessNamePanel.SetActive(true);
        
        // Xóa các nút cũ trước khi tạo lại
        foreach (Transform child in namesContainer)
        {
            Destroy(child.gameObject);
        }

        // Tạo các nút tên mới
        foreach (string hName in hiderNames)
        {
            GameObject btnObj = Instantiate(nameButtonPrefab, namesContainer);
            Text btnText = btnObj.GetComponentInChildren<Text>();
            if (btnText != null) btnText.text = hName;

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string captureName = hName; // Capture biến để dùng trong closure
                btn.onClick.AddListener(() => 
                {
                    guessNamePanel.SetActive(false);
                    onNameSelectedCallback?.Invoke(captureName);
                });
            }
        }
    }
}
