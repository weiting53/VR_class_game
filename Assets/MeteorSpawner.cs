using UnityEngine;
using System.Collections;

public class MeteorSpawnerFixedDirection : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject meteorPrefab;

    [Header("Spawn Region around this object")]
    public float spawnRadius = 8f;                 // 以本物件為圓心，球殼半徑
    public bool upperHemisphereOnly = true;        // 只在上半球生成（避免地底）

    [Header("Direction")]
    public Vector3 worldDirection = new Vector3(0f, -1f, -0.3f); // 固定飛行方向（世界座標）
    public bool normalizeDirection = true;         // 自動正規化方向
    public bool forceDownwardY = true;             // 強制 y < 0（確保斜向下）

    [Tooltip("隨機擾動角度（度）。讓每顆隕石方向有些微偏差。")]
    public float spreadDegrees = 8f;

    [Header("Timing")]
    public Vector2 spawnIntervalRange = new Vector2(0.4f, 1.2f); // 生成間隔

    [Header("Motion")]
    public Vector2 speedRange = new Vector2(2f, 5f);  // 初速度
    public Vector2 torqueRange = new Vector2(2f, 8f); // 旋轉力矩
    public bool useGravity = false;                   // 是否交給重力
    public float maxLifetime = 12f;

    [Header("Random Scale")]
    public bool randomScale = true;
    public Vector2 uniformScaleRange = new Vector2(0.6f, 1.4f);

    void Start()
    {
        if (meteorPrefab == null)
        {
            Debug.LogError("[MeteorSpawnerFixedDirection] meteorPrefab 未指定");
            enabled = false;
            return;
        }
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            yield return new WaitForSeconds(wait);

            // 1) 隨機生成位置（圍繞本物件的球殼）
            Vector3 dir = Random.onUnitSphere;
            if (upperHemisphereOnly && dir.y < 0f) dir.y = -dir.y;
            Vector3 spawnPos = transform.position + dir * spawnRadius;

            // 2) 固定方向（世界座標）
            Vector3 moveDir = worldDirection;
            if (forceDownwardY && moveDir.y > 0f) moveDir.y = -moveDir.y;
            if (normalizeDirection) moveDir = moveDir.normalized;

            // 2.1）加入散射角（把方向在錐形內隨機偏轉）
            if (spreadDegrees > 0.01f)
            {
                moveDir = RandomRotateWithinCone(moveDir, spreadDegrees);
            }

            // 3) 生成 + 設定剛體
            GameObject m = Instantiate(meteorPrefab, spawnPos, Random.rotation);

            if (randomScale)
            {
                float s = Random.Range(uniformScaleRange.x, uniformScaleRange.y);
                m.transform.localScale = Vector3.one * s;
            }

            float speed = Random.Range(speedRange.x, speedRange.y);

            if (m.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.useGravity = useGravity;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                rb.velocity = moveDir * speed;

                Vector3 randomTorque = Random.onUnitSphere * Random.Range(torqueRange.x, torqueRange.y);
                rb.AddTorque(randomTorque, ForceMode.VelocityChange);
            }

            Destroy(m, maxLifetime);
        }
    }

    // 在 worldDirection 的錐角內，做隨機微旋轉
    Vector3 RandomRotateWithinCone(Vector3 dir, float coneAngleDeg)
    {
        // 任意與 dir 不平行的向量
        Vector3 any = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) < 0.99f ? Vector3.up : Vector3.right;
        Vector3 right = Vector3.Normalize(Vector3.Cross(dir, any));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, dir));

        float ang = Random.Range(0f, coneAngleDeg) * Mathf.Deg2Rad;
        float az  = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        // 在錐面局部座標的偏移
        Vector3 offset = Mathf.Sin(ang) * (Mathf.Cos(az) * right + Mathf.Sin(az) * up);
        Vector3 result = (dir.normalized * Mathf.Cos(ang) + offset).normalized;
        return result;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // 畫出方向向量
        Vector3 d = worldDirection;
        if (forceDownwardY && d.y > 0f) d.y = -d.y;
        if (normalizeDirection) d = d.normalized;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, d * (spawnRadius * 0.8f));
    }
#endif
}
