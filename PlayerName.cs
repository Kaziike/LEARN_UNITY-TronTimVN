using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public class PlayerName : NetworkBehaviour
{
    // Biến lưu trữ Tên được đồng bộ trên mạng
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>("Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("UI")]
    public TMP_Text nameText;


    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Lấy tên đã nhập từ MainMenu (được lưu tạm trong PlayerPrefs)
            string savedName = PlayerPrefs.GetString("PlayerName", "Player_" + Random.Range(100, 999));
            playerName.Value = savedName;
        }

        // Đăng ký sự kiện khi Tên thay đổi
        playerName.OnValueChanged += OnNameChanged;

        // Cập nhật tên ngay lập tức khi vừa vào
        UpdateNameUI(playerName.Value.ToString());

        
    }

    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged -= OnNameChanged;
    }

    private void OnNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        UpdateNameUI(newValue.ToString());
    }

    private void UpdateNameUI(string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
        }
    }

    private void Update()
    {
        
        

        // Tự động ẩn Text khi game bắt đầu (khi không còn ở Setup)
        if (HideAndSeekManager.Instance != null && nameText != null)
        {
            if (HideAndSeekManager.Instance.currentState != HideAndSeekManager.GameState.Setup)
            {
                if (nameText.gameObject.activeSelf)
                    nameText.gameObject.SetActive(false);
            }
            else
            {
                if (!nameText.gameObject.activeSelf)
                    nameText.gameObject.SetActive(true);
            }
        }
    }
}
