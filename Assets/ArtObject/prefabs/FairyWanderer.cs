using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FairyWanderer : MonoBehaviour
{
    [Header("Zone")]
    public BoxCollider zone;                 // 活動邊界（必填）

    [Header("Move")]
    [Tooltip("基礎水平速度(公尺/秒)")]
    public float speed = 0.9f;
    [Tooltip("sin 平滑的振幅(0~1 建議)")]
    [Range(0f, 1f)] public float sinAmplitude = 0.35f;
    [Tooltip("sin 頻率(越小越慵懶)")]
    public float sinFrequency = 0.8f;
    [Tooltip("每隔多少秒重新挑方向(區間)")]
    public Vector2 retargetEvery = new Vector2(5f, 7f);

    [Header("Separation（避免靠太近）")]
    [Tooltip("偵測半徑")]
    public float separationRadius = 0.45f;
    [Tooltip("推開力道")]
    public float separationStrength = 1.6f;
    [Tooltip("要避開的 Layer（沒有就 Everything）")]
    public LayerMask avoidLayers = ~0;

    [Header("Scale（開局自動縮到人 1/5）")]
    [Tooltip("人的身高(用來估比例，僅輔助說明)")]
    public float humanHeight = 1.7f;
    [Tooltip("縮放倍率：人 1/5 = 0.2")]
    public float scaleToHuman = 0.2f;
    public float baseMoveSpeed = 0.8f;

    Rigidbody rb;
    Vector3 dirXZ;                 // 目前水平方向
    Vector3 smoothVel;             // 平滑速度
    float baseY;                   // 固定飛行高度（用邊界的中間）
    float nextRetargetTime;
    float sinPhase;                // 隨機相位
    Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        originalScale = transform.localScale;
        transform.localScale = originalScale * scaleToHuman;   // 人 1/5

        sinPhase = Random.value * Mathf.PI * 2f;
        if (zone != null)
        {
            var b = zone.bounds;
            baseY = (b.min.y + b.max.y) * 0.5f;                // 固定在盒子中間高度
            // 初始位置若超界，夾回盒內
            Vector3 p = rb.position; p = ClampTo(b, p); p.y = baseY; rb.position = p;
        }

        PickNewDirection();
    }

    void FixedUpdate()
    {
        if (zone == null) return;

        // 到時間就換方向
        if (Time.time >= nextRetargetTime)
            PickNewDirection();

        Vector3 pos = rb.position;

        // ====== 1) sin 平滑速度 ======
        float s = speed * (1f - sinAmplitude + sinAmplitude * (0.5f + 0.5f * Mathf.Sin(Time.time * sinFrequency + sinPhase)));
        Vector3 desired = dirXZ * s;

        // ====== 2) 分離力（避開其他剛體）======
        if (separationRadius > 0f)
        {
            var hits = Physics.OverlapSphere(pos, separationRadius, avoidLayers, QueryTriggerInteraction.Ignore);
            Vector3 push = Vector3.zero;
            foreach (var h in hits)
            {
                if (!h || h.attachedRigidbody == rb) continue;
                Vector3 away = (pos - h.transform.position); away.y = 0f;
                float d = away.magnitude;
                if (d < 1e-3f) continue;
                // 距離越近推力越大（1/d 衰減）
                push += away.normalized / d;
            }
            desired += push * separationStrength;
        }

        // 只保留水平速度
        desired.y = 0f;

        // 平滑到期望速度（避免突然跳動）
        smoothVel = Vector3.Lerp(smoothVel, desired, 6f * Time.fixedDeltaTime);

        // 嘗試前進
        Vector3 next = pos + smoothVel * Time.fixedDeltaTime;

        // 邊界處理：超出就夾回、並把方向往中心修正
        var bnds = zone.bounds;
        if (!bnds.Contains(new Vector3(next.x, Mathf.Clamp(next.y, bnds.min.y, bnds.max.y), next.z)))
        {
            // 往中心微調方向，避免黏牆
            Vector3 toCenter = (bnds.center - pos); toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 1e-4f)
                dirXZ = Vector3.Lerp(dirXZ, toCenter.normalized, 0.5f);
            next = ClampTo(bnds, next);
        }

        // 固定高度
        next.y = baseY;

        // 轉向：面向水平速度
        if (smoothVel.sqrMagnitude > 1e-4f)
        {
            Quaternion want = Quaternion.LookRotation(new Vector3(smoothVel.x, 0f, smoothVel.z));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, want, 6f * Time.fixedDeltaTime));
        }

        rb.MovePosition(next);
    }

    // === Helpers ===
    void PickNewDirection()
    {
        // 單純水平隨機向量
        Vector3 v = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        dirXZ = (v.sqrMagnitude < 1e-4f) ? Vector3.forward : v.normalized;

        nextRetargetTime = Time.time + Random.Range(retargetEvery.x, retargetEvery.y);
    }

    static Vector3 ClampTo(Bounds b, Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, b.min.x, b.max.x);
        p.y = Mathf.Clamp(p.y, b.min.y, b.max.y);
        p.z = Mathf.Clamp(p.z, b.min.z, b.max.z);
        return p;
    }

    // 讓舊程式若用到 moveSpeed 也能相容（可刪）
    public float moveSpeed { get => speed; set => speed = value; }
}
