using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BasePillar : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem ai chạm vào (Hoặc có thể đổi thành OnTriggerStay + Bấm E nếu muốn trải nghiệm thực tế hơn)
        
        SeekerMechanic seeker = other.GetComponent<SeekerMechanic>();
        if (seeker != null)
        {
            // Nếu người đi bắt đập cột, họ muốn "chốt" sổ một người đang bị đánh dấu
            seeker.ConfirmMarkAtBase();
            return;
        }

        HiderMechanic hider = other.GetComponent<HiderMechanic>();
        if (hider != null && !hider.isEliminated && !hider.isSafe)
        {
            // Nếu hider về kịp và chưa bị loại
            // Hoặc có thể thêm cơ chế: Nếu hider bị mark thì vẫn được về đập cột cứu mạng nếu nhanh chân hơn seeker
            hider.CheckInAtBase();
        }
    }
}
