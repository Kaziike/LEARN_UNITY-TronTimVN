using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Thêm thư viện này
using TMPro; // Thêm TMPro

public class NetworkUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    
    [Header("Player Settings")]
    [SerializeField] private TMP_InputField nameInputField; // Ô nhập tên

    // Gõ đúng tên Scene Chính của bạn vào đây (ví dụ: "GameScene", "MainScene")
    public string mainSceneName = "Online"; 

    private void Awake()
    {
        // Tải tên đã lưu trước đó (nếu có)
        if (nameInputField != null)
        {
            nameInputField.text = PlayerPrefs.GetString("PlayerName", "Player_" + Random.Range(100, 999));
            nameInputField.onValueChanged.AddListener((val) => 
            {
                PlayerPrefs.SetString("PlayerName", val);
            });
        }
        hostButton.onClick.AddListener(() =>
        {
            SaveName();
            NetworkManager.Singleton.StartHost();
            
            // Host sẽ là người ra lệnh đổi Scene, các client sẽ tự động load theo
            NetworkManager.Singleton.SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
            
            Hide();
        });

        clientButton.onClick.AddListener(() =>
        {
            SaveName();
            NetworkManager.Singleton.StartClient();
            // Client không cần viết lệnh LoadScene, tự động sẽ được Host kéo sang
            Hide();
        });
    }

    private void SaveName()
    {
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            PlayerPrefs.SetString("PlayerName", nameInputField.text);
            PlayerPrefs.Save();
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
