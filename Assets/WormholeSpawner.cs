using UnityEngine;

public class WormholeSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform left;           // 指向 LeftHandAnchor
    public Transform right;          // 指向 RightHandAnchor
    public GameObject wormholePrefab;

    [Header("Distance Thresholds (meters)")]
    public float appearThreshold = 0.30f;     // 距離 <= 這個值 → 出現
    public float disappearThreshold = 0.35f;  // 距離 >= 這個值 → 消失（設得比 appear 大一點避免抖動）

    [Header("Optional")]
    public bool faceAlongHands = true;        // 是否讓球的朝向沿著兩手方向
    public bool autoScaleByDistance = false;  // 是否依距離縮放
    public Vector2 scaleRange = new Vector2(0.05f, 0.20f); // 最小~最大縮放（公尺）

    GameObject wormholeInstance;
    bool hasInstance => wormholeInstance != null;

    void Start()
    {
        if (wormholePrefab == null)
        {
            Debug.LogError("[WormholeSpawner] wormholePrefab 未指定");
            enabled = false;
            return;
        }

        // 先建立，預設不顯示
        wormholeInstance = Instantiate(wormholePrefab);
        wormholeInstance.SetActive(false);
    }

    void Update()
    {
        if (left == null || right == null || !hasInstance) return;

        Vector3 lp = left.position;
        Vector3 rp = right.position;

        // 置中
        Vector3 mid = (lp + rp) * 0.5f;

        // 距離
        float d = Vector3.Distance(lp, rp);

        // 顯示/隱藏（雙門檻防抖）
        if (!wormholeInstance.activeSelf && d <= appearThreshold)
            wormholeInstance.SetActive(true);
        else if (wormholeInstance.activeSelf && d >= disappearThreshold)
            wormholeInstance.SetActive(false);

        if (!wormholeInstance.activeSelf) return;

        // 已顯示 → 更新位置/方向/縮放
        wormholeInstance.transform.position = mid;

        if (faceAlongHands)
        {
            Vector3 dir = (rp - lp).normalized;
            if (dir.sqrMagnitude > 1e-4f)
                wormholeInstance.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        if (autoScaleByDistance)
        {
            // 將距離映射到 scaleRange
            float t = Mathf.InverseLerp(appearThreshold, disappearThreshold, d);
            float s = Mathf.Lerp(scaleRange.x, scaleRange.y, t);
            wormholeInstance.transform.localScale = Vector3.one * s;
        }
    }
}
