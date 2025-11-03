using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GuardianToFairyZone : MonoBehaviour
{
    public BoxCollider fairyZone;   // 指向你的 FairyZone 的 BoxCollider
    public float height = 2.2f;     // 精靈活動高度
    public float yCenter = 1.5f;    // 中心高度

    void Start()
    {
        if (fairyZone == null) return;

        // 取得 Guardian 幾何（PlayArea）
        var boundary = new OVRBoundary(OVRBoundary.BoundaryType.PlayArea);
        if (!boundary.GetConfigured())
        {
            Debug.LogWarning("Guardian 未配置，改用預設區域");
            return;
        }

        // 讀取外框點（世界座標，水平面）
        Vector3[] points = boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);

        // 轉到 Rig 的 local（FairyZone 是 Rig 的子物件更簡單）
        var rig = transform;
        Vector3 min = new Vector3(float.MaxValue, 0, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, 0, float.MinValue);
        Vector3 sum = Vector3.zero;

        foreach (var wp in points)
        {
            Vector3 p = rig.InverseTransformPoint(wp);
            min.x = Mathf.Min(min.x, p.x); max.x = Mathf.Max(max.x, p.x);
            min.z = Mathf.Min(min.z, p.z); max.z = Mathf.Max(max.z, p.z);
            sum += p;
        }
        Vector3 center = new Vector3((min.x + max.x) * 0.5f, yCenter, (min.z + max.z) * 0.5f);
        Vector3 size   = new Vector3((max.x - min.x), height, (max.z - min.z));

        fairyZone.transform.localPosition = center;
        fairyZone.center = Vector3.zero;
        fairyZone.size   = size;
        fairyZone.isTrigger = true; // 當活動邊界
    }
}
