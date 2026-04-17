using UnityEngine;

public class HiderMechanic : MonoBehaviour
{
    public string hiderName = "Player"; // Tên của Hider (Ví dụ: "An", "Bình")
    
    public bool isEliminated = false;
    public bool isSafe = false;
    public bool isMarked = false; // Đã lọt vào tầm ngắm bị gọi tên nhưng chưa đập cột

    void Start()
    {
        // Khi spawn, đăng ký Hider với GameManager
        if (HideAndSeekManager.Instance != null)
        {
            HideAndSeekManager.Instance.RegisterHider(this);
        }
    }

    public void OnMarkedAsTarget()
    {
        isMarked = true;
        // Bật một UI nhỏ (VD: Dấu chấm than đỏ trên đầu Hider hoặc thông báo lên màn hình Hider)
        Debug.Log("HIDER: Bạn đã bị Seeker đoán tên!");
        if (this.gameObject.CompareTag("Player"))
        {
             UIManager.Instance.ShowMessage("CẢNH BÁO: BẠN ĐÃ BỊ ĐỌC TÊN, HÃY CHẠY VỀ CỘT NHANH!!!");
        }
    }

    public void OnUnmarked()
    {
        // Nhờ Seeker đoán sai, ta được huỷ lệnh
        isMarked = false;
         if (this.gameObject.CompareTag("Player"))
        {
             UIManager.Instance.ShowMessage("May quá, Seeker đoán sai tên của bạn rồi!");
        }
    }

    public void OnEliminated()
    {
        isEliminated = true;
        isMarked = false;
        Debug.Log(hiderName + " đã bị LOẠI!");
        
        // Disable renderer or play death animation, etc.
        this.gameObject.SetActive(false);
    }

    public void OnSafe()
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

        // Báo cho Game Manager là đã về đích
        HideAndSeekManager.Instance.SafelyCheckInHider(this);
    }
}
