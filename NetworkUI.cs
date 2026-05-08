using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Thêm thư viện này

public class NetworkUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    
    // Gõ đúng tên Scene Chính của bạn vào đây (ví dụ: "GameScene", "MainScene")
    public string mainSceneName = "Online"; 

    private void Awake()
    {
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            
            // Host sẽ là người ra lệnh đổi Scene, các client sẽ tự động load theo
            NetworkManager.Singleton.SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
            
            Hide();
        });

        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            // Client không cần viết lệnh LoadScene, tự động sẽ được Host kéo sang
            Hide();
        });
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
