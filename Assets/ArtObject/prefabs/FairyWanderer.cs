using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FairyWanderer : MonoBehaviour
{
    [Header("Zone & Motion")]
    public BoxCollider zone;
    [Range(0.1f, 3f)] public float baseMoveSpeed = 0.8f; // 基礎速度
    [Tooltip("整體速度倍率（想更慢就調低）")]
    [Range(0.1f, 1f)] public float speedScale = 0.45f;

    [Tooltip("每隔幾秒換一次目標點（大一點=更悠閒）")]
    public float changeTargetEvery = 4.5f;

    [Header("Wander (平滑飄移)")]
    [Tooltip("Perlin 噪聲頻率，越低越平穩")]
    public float wanderNoiseFreq = 0.35f;
    [Tooltip("Perlin 噪聲對方向的影響強度")]
    public float wanderNoiseStrength = 0.8f;
    [Tooltip("最大轉向角速度（度/秒），越小越柔和")]
    public float maxTurnDegPerSec = 90f;
    [Tooltip("速度平滑係數（越大越貼目標，越小越慵懶）")]
    public float velocityLerp = 4f;

    [Header("Separation (避免重疊)")]
    public float avoidRadius = 0.9f;
    public float avoidStrength = 2.2f;
    public LayerMask fairyLayer = 0;

    [Header("Bob (上下漂浮)")]
    public float bobAmplitude = 0.08f;
    public float bobFrequency = 1.0f;

    [Header("Model 外觀旋轉(子物件)")]
    public Transform model;
    public Vector2 spinYRange = new Vector2(-15f, 20f);     // ⚠️ 大幅降低
    public Vector2 wobbleAmpRange = new Vector2(3f, 7f);    // 度
    public Vector2 wobbleFreqRange = new Vector2(0.6f, 1.1f);

    [Header("Stunt (偶爾小特技，超溫和)")]
    public Vector2 stuntInterval = new Vector2(6f, 12f);    // 更少觸發
    public Vector2 stuntDuration = new Vector2(1.2f, 2.0f);
    [Range(3f, 18f)] public float bankMaxAngle = 12f;       // 小角度
    public float bankSideDrift = 0.18f;                     // 輕微側滑
    public int spinTurnsMin = 1;
    public int spinTurnsMax = 1;                            // 只轉一圈，避免暈

    enum StuntType { None, BankLeft, BankRight, Spin }

    // ---- 内部狀態 ----
    Rigidbody rb;
    Vector3 target;
    Vector3 currentVel;             // 平滑速度
    float bobPhase;

    // 外觀
    float spinY, wobbleAmp, wobbleFreq;
    Vector3 wobbleAxis = Vector3.right;

    // 特技
    StuntType stunt = StuntType.None;
    float stuntStart, stuntEnd;
    int spinTurns = 1;

    // Perlin 种子
    float noiseSeedX, noiseSeedZ;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        bobPhase   = Random.value * Mathf.PI * 2f;
        noiseSeedX = Random.value * 100f;
        noiseSeedZ = Random.value * 100f;

        // 外觀參數（溫和版）
        spinY      = Random.Range(spinYRange.x, spinYRange.y);
        wobbleAmp  = Random.Range(wobbleAmpRange.x, wobbleAmpRange.y);
        wobbleFreq = Random.Range(wobbleFreqRange.x, wobbleFreqRange.y);
        wobbleAxis = Random.onUnitSphere; wobbleAxis.y = 0f;
        if (wobbleAxis.sqrMagnitude < 1e-3f) wobbleAxis = Vector3.right;
    }

    void Start()
    {
        PickNewTarget();
        InvokeRepeating(nameof(PickNewTarget), changeTargetEvery, changeTargetEvery);
        ScheduleNextStunt();
    }

    void FixedUpdate()
    {
        if (!zone) return;

        Vector3 pos = rb.position;
        Bounds b = zone.bounds;

        // 目標方向（水平）
        Vector3 flatPos    = new Vector3(pos.x, 0f, pos.z);
        Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
        Vector3 desiredDir = flatTarget - flatPos;
        if (desiredDir.sqrMagnitude > 1e-4f) desiredDir.Normalize();

        // 平滑 wander：用 Perlin 讓方向慢慢偏
        float t = Time.time;
        float nx = Mathf.PerlinNoise(noiseSeedX, t * wanderNoiseFreq) * 2f - 1f;
        float nz = Mathf.PerlinNoise(noiseSeedZ, t * wanderNoiseFreq) * 2f - 1f;
        Vector3 noise = new Vector3(nx, 0f, nz).normalized * wanderNoiseStrength;

        desiredDir = (desiredDir + noise);
        if (desiredDir.sqrMagnitude > 1e-4f) desiredDir.Normalize();

        // 分離力（溫和）
        Vector3 avoid = Vector3.zero;
        var hits = Physics.OverlapSphere(pos, avoidRadius, fairyLayer, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.attachedRigidbody && h.attachedRigidbody != rb)
            {
                Vector3 away = (pos - h.attachedRigidbody.position);
                float d = away.magnitude + 1e-4f;
                avoid += away / (d * d);
            }
        }
        avoid *= avoidStrength;

        // 合成期望速度
        Vector3 desiredVel = (desiredDir + avoid).normalized
                             * (baseMoveSpeed * speedScale);

        // 平滑到目標速度（不再每幀猛跳）
        currentVel = Vector3.Lerp(currentVel, desiredVel, velocityLerp * Time.fixedDeltaTime);

        // 特技（溫和）
        float now = Time.time;
        if (stunt == StuntType.None && now >= stuntStart)
        {
            stunt = (StuntType)Random.Range(0, 3);  // 0L、1R、2Spin
            float dur = Random.Range(stuntDuration.x, stuntDuration.y);
            stuntEnd = now + dur;
            if (stunt == StuntType.Spin) spinTurns = Random.Range(spinTurnsMin, spinTurnsMax + 1);
        }

        if ((stunt == StuntType.BankLeft || stunt == StuntType.BankRight) && currentVel.sqrMagnitude > 1e-4f)
        {
            Vector3 fwd = currentVel.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            float dirSign = (stunt == StuntType.BankLeft) ? -1f : 1f;
            currentVel += right * dirSign * bankSideDrift;
        }
        if (stunt != StuntType.None && now >= stuntEnd) ScheduleNextStunt();

        // 上下擺動
        float baseY = Mathf.Lerp(b.min.y + 0.3f, b.max.y - 0.3f, 0.5f);
        float bob = Mathf.Sin(Time.time * bobFrequency + bobPhase) * bobAmplitude;

        // 邊界限制 + 移動
        Vector3 next = pos + currentVel * Time.fixedDeltaTime;
        next.y = baseY + bob;
        next = ClampTo(b, next);

        // 限制轉向角速度，避免瞬轉
        if (currentVel.sqrMagnitude > 1e-4f)
        {
            Quaternion to = Quaternion.LookRotation(currentVel.normalized);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, to, maxTurnDegPerSec * Time.fixedDeltaTime));
        }

        rb.MovePosition(next);
    }

    void Update()
    {
        if (!model) return;

        // 自轉 + 擺動（溫和）
        float spinAngle = spinY * Time.time;
        Quaternion spinQ = Quaternion.Euler(0f, spinAngle, 0f);

        float wobble = Mathf.Sin(Time.time * wobbleFreq) * wobbleAmp;
        Quaternion wobbleQ = Quaternion.AngleAxis(wobble, wobbleAxis);

        // 特技外觀
        Quaternion stuntQ = Quaternion.identity;
        if (stunt == StuntType.BankLeft || stunt == StuntType.BankRight)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(stuntStart, stuntEnd, Time.time));
            float ang = Mathf.Sin(t * Mathf.PI) * bankMaxAngle * (stunt == StuntType.BankLeft ? -1f : 1f);
            stuntQ = Quaternion.AngleAxis(ang, Vector3.forward);
        }
        else if (stunt == StuntType.Spin)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(stuntStart, stuntEnd, Time.time));
            float ang = 360f * spinTurns * t;
            stuntQ = Quaternion.AngleAxis(ang, Vector3.up);
        }

        model.localRotation = stuntQ * wobbleQ * spinQ;
    }

    // ---- Helpers ----
    void PickNewTarget()
    {
        if (!zone) return;
        var b = zone.bounds;
        target = new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z)
        );
    }

    void ScheduleNextStunt()
    {
        float wait = Random.Range(stuntInterval.x, stuntInterval.y);
        stuntStart = Time.time + wait;
        stuntEnd = 0f;
        stunt = StuntType.None;
    }

    static Vector3 ClampTo(Bounds b, Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, b.min.x, b.max.x);
        p.y = Mathf.Clamp(p.y, b.min.y, b.max.y);
        p.z = Mathf.Clamp(p.z, b.min.z, b.max.z);
        return p;
    }
}
