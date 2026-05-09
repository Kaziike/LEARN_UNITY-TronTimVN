using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbyUI : MonoBehaviour
{
    public Button startGameButton;
    public Text waitingText;

    void Start()
    {
        // Ẩn nút đi trước
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        if (waitingText != null) waitingText.gameObject.SetActive(false);

        // Đợi 1 chút để NetworkManager load xong rồi mới kiểm tra IsServer
        Invoke(nameof(CheckHostStatus), 0.5f);
    }

    void CheckHostStatus()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        if (NetworkManager.Singleton.IsServer)
        {
            // Nếu là Host, hiện nút Bắt Đầu
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(true);
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }
        }
        else
        {
            // Nếu là Client, hiện chữ Đang chờ
            if (waitingText != null)
            {
                waitingText.gameObject.SetActive(true);
                waitingText.text = "Đang chờ Host bắt đầu game...";
            }
        }
    }

    void OnStartGameClicked()
    {
        if (HideAndSeekManager.Instance != null)
        {
            HideAndSeekManager.Instance.StartGameServerRpc();
            
            // Bấm xong thì ẩn nút đi
            if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Nếu game đã chuyển sang HidingPhase (đã bắt đầu), ẩn luôn chữ "Đang chờ" của Client
        if (HideAndSeekManager.Instance != null && HideAndSeekManager.Instance.currentState != HideAndSeekManager.GameState.Setup)
        {
            if (waitingText != null) waitingText.gameObject.SetActive(false);
            if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        }
    }
}
