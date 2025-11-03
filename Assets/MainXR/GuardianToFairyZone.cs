using UnityEngine;

public class GuardianToFairyZone : MonoBehaviour
{
    public BoxCollider fairyZone;    // 指向你的 FairyZone（BoxCollider，IsTrigger=ON）
    public float height = 2.2f;      // 活動高度
    public float yCenter = 1.5f;     // 區域中心高度
    public bool runInEditor = false; // 只在裝置上跑，編輯器可關

    void Start()
    {
#if UNITY_EDITOR
        if (!runInEditor) { Debug.Log("GuardianToFairyZone: skip in Editor"); return; }
#endif
        if (fairyZone == null) { Debug.LogWarning("Assign FairyZone BoxCollider"); return; }

        // ✅ 正確作法：先 new 無參數，再在方法裡指 PlayArea
        var boundary = new OVRBoundary();
        if (!boundary.GetConfigured())
        {
            Debug.LogWarning("Guardian not configured on device.");
            return;
        }

        // 幾何點是世界座標的水平多邊形
        Vector3[] worldPts = boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
        if (worldPts == null || worldPts.Length == 0)
        {
            Debug.LogWarning("No guardian geometry points.");
            return;
        }

        // 轉到 Rig 的 local，再取 AABB
        var rig = transform;
        Vector3 min = new Vector3(float.MaxValue, 0, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, 0, float.MinValue);
        foreach (var wp in worldPts)
        {
            Vector3 p = rig.InverseTransformPoint(wp);
            if (p.x < min.x) min.x = p.x; if (p.z < min.z) min.z = p.z;
            if (p.x > max.x) max.x = p.x; if (p.z > max.z) max.z = p.z;
        }

        Vector3 localCenter = new Vector3((min.x + max.x) * 0.5f, yCenter, (min.z + max.z) * 0.5f);
        Vector3 size = new Vector3((max.x - min.x), height, (max.z - min.z));

        // 設定 FairyZone（建議 FairyZone 為 Camera Rig 的 child）
        fairyZone.transform.localPosition = localCenter;
        fairyZone.transform.localRotation = Quaternion.identity;
        fairyZone.center = Vector3.zero;
        fairyZone.size = size;
        fairyZone.isTrigger = true;

        Debug.Log($"FairyZone from Guardian: center={localCenter}, size={size}");
    }
}
