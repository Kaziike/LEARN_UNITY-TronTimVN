using UnityEngine;
using Unity.Netcode;

public class HiderMechanic : NetworkBehaviour
{
    public string hiderName = "Player"; // Tên của Hider (Ví dụ: "An", "Bình")
    
    public bool isEliminated = false;
    public bool isSafe = false;
    public bool isMarked = false; // Đã lọt vào tầm ngắm bị gọi tên nhưng chưa đập cột

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Khi spawn, đăng ký Hider với GameManager (chỉ Server quản lý logic chính)
        if (IsServer && HideAndSeekManager.Instance != null)
        {
            HideAndSeekManager.Instance.RegisterHider(this);
        }
    }

    [ClientRpc]
    public void OnMarkedAsTargetClientRpc()
    {
        isMarked = true;
        // Bật một UI nhỏ (VD: Dấu chấm than đỏ trên đầu Hider hoặc thông báo lên màn hình Hider)
        Debug.Log("HIDER: Bạn đã bị Seeker đoán tên!");
        if (IsOwner && this.gameObject.CompareTag("Player"))
        {
             UIManager.Instance.ShowMessage("CẢNH BÁO: BẠN ĐÃ BỊ ĐỌC TÊN, HÃY CHẠY VỀ CỘT NHANH!!!");
        }
    }

    [ClientRpc]
    public void OnUnmarkedClientRpc()
    {
        // Nhờ Seeker đoán sai, ta được huỷ lệnh
        isMarked = false;
         if (IsOwner && this.gameObject.CompareTag("Player"))
        {
             UIManager.Instance.ShowMessage("May quá, Seeker đoán sai tên của bạn rồi!");
        }
    }

    [ClientRpc]
    public void OnEliminatedClientRpc()
    {
        isEliminated = true;
        isMarked = false;
        Debug.Log(hiderName + " đã bị LOẠI!");
        
        // Disable renderer or play death animation, etc.
        this.gameObject.SetActive(false);
    }

    [ClientRpc]
    public void OnSafeClientRpc()
    {
        isSafe = true;
        isMarked = false;
        Debug.Log(hiderName + " ĐÃ VỀ ĐÍCH AN TOÀN!");

        // Có thể đổi màu vật liệu, hoặc làm mờ đi chứng tỏ đang làm "hồn ma" đứng trong base
    }

    // Logic Đập Cột (Hider chủ động đập)
    public void CheckInAtBase()
    {
        if (isEliminated || isSafe) return;
        if (IsOwner)
        {
            CheckInAtBaseServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckInAtBaseServerRpc()
    {
        if (isEliminated || isSafe) return;
        HideAndSeekManager.Instance.SafelyCheckInHider(this);
    }
}
