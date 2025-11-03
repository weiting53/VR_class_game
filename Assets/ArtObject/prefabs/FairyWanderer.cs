using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FairyWanderer : MonoBehaviour
{
    [Header("Zone")]
    public BoxCollider zone;                 // 活動邊界（必填）

    [Header("Base Move")]
    [Tooltip("名義水平速度(公尺/秒)")]
    public float speed = 0.9f;

    [Header("Speed Modulation (知名遊戲常用作法)")]
    [Tooltip("Perlin 噪聲的速度擾動比例：0.2 表示在 0.8x~1.2x 範圍內波動")]
    [Range(0f, 0.8f)] public float speedJitter = 0.25f;
    [Tooltip("Perlin 變化頻率（越小越慵懶）")]
    public float speedNoiseFreq = 0.35f;
    [Tooltip("速度目標的平滑時間（越大越慢變）")]
    public float speedSmoothTime = 0.6f;

    [Header("Heading / Wander（方向漫步）")]
    [Tooltip("每隔幾秒換一次『目標方向』")]
    public Vector2 retargetEvery = new Vector2(5f, 7f);
    [Tooltip("換方向的角速度上限（度/秒）")]
    public float maxTurnDegPerSec = 120f;
    [Tooltip("隨機方向偏擾的噪聲頻率（越小越平穩）")]
    public float dirNoiseFreq = 0.25f;
    [Tooltip("隨機方向偏擾強度（0~1）")]
    [Range(0f, 1f)] public float dirNoiseStrength = 0.55f;

    [Header("Vertical Bob（上下漂浮）")]
    [Tooltip("主振幅（公尺）")]
    public float bobAmp1 = 0.07f;
    [Tooltip("主頻率（Hz）")]
    public float bobFreq1 = 0.5f;
    [Tooltip("次諧波振幅（公尺）")]
    public float bobAmp2 = 0.03f;
    [Tooltip("次諧波頻率（Hz）")]
    public float bobFreq2 = 0.87f;
    [Tooltip("Perlin 漂浮擾動（公尺）")]
    public float bobPerlinAmp = 0.015f;
    [Tooltip("上下高度平滑時間")]
    public float bobSmoothTime = 0.25f;

    [Header("Separation（避免靠太近）")]
    [Tooltip("偵測半徑")]
    public float separationRadius = 0.45f;
    [Tooltip("分離最大推力（m/s²）")]
    public float separationMaxAccel = 1.8f;
    [Tooltip("反平方衰減係數（越大越敏感）")]
    public float separationFalloff = 1.2f;
    [Tooltip("要避開的 Layer（沒有就 Everything）")]
    public LayerMask avoidLayers = ~0;

    [Header("Wall Avoid（牆面預判）")]
    [Tooltip("預判距離（公尺），太小容易碰牆才轉向")]
    public float wallProbeDist = 0.35f;
    [Tooltip("牆面轉向權重（m/s²）")]
    public float wallAvoidAccel = 2.0f;

    [Header("Dynamics 限制")]
    [Tooltip("最大水平加速度（m/s²）")]
    public float maxAccel = 0f;
    [Tooltip("最大水平速度（m/s），一般可=名義速度的 1.3~1.6 倍")]
    public float maxSpeed = 1.6f;

    [Header("Scale（開局自動縮到人 1/5）")]
    [Header("Idle 微旋轉設定")]
    [Tooltip("速度低於此值時視為 idle")]
    public float idleSpeedThreshold = 0.05f;

    [Tooltip("idle 微旋轉最大角度 (度)")]
    public float idleRotationAngle = 6f; // 建議 4~8 度

    [Tooltip("主旋轉頻率 (Hz)")]
    public float idleRotFreq1 = 0.45f;

    [Tooltip("副旋轉頻率 (Hz)")]
    public float idleRotFreq2 = 0.82f;

    float idleRotSmoothVel;    // internal smoothing
    float idleRotPhaseSeed;    // random offset

    public float humanHeight = 1.7f;
    public float scaleToHuman = 0.2f;
    public float scaleMultiplier = 2.5f;
    [Tooltip("最小舒適距（小於此距離會強烈分離）")]
    public float minSeparation = 0.28f;   // 先試 0.26~0.32

    [Tooltip("核心區的彈簧係數（越大越快推開）")]
    public float separationSpring = 3.5f; // 先試 3~5

    // ==== 內部狀態 ====
    Rigidbody rb;
    Vector3 dirXZ;                       // 當前水平朝向（單位向量）
    Vector3 velXZ;                       // 當前水平速度（m/s）
    float baseY;                         // 中線高度
    float nextRetargetTime;
    Vector3 desiredHeading;              // 目標朝向（平面）
    float speedTarget;                   // 目標速度（會隨 Perlin 起伏）
    float speedCurrent;                  // 平滑後當前速度
    float speedVelRef;                   // SmoothDamp 參考

    float bobVelRef;                     // 垂直高度平滑參考
    float perlinSeedSpeed;
    float perlinSeedDir;
    float perlinSeedBob;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
#if UNITY_2021_2_OR_NEWER
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // kinematic 推薦
#endif

        // 比例
        var originalScale = transform.localScale;
        transform.localScale = originalScale * scaleToHuman * scaleMultiplier;

        // 高度基準
        if (zone != null)
        {
            var b = zone.bounds;
            baseY = 0.5f * (b.min.y + b.max.y);
            var p = rb.position; p = ClampTo(b, p); p.y = baseY; rb.position = p;
        }

        // 隨機種子
        perlinSeedSpeed = Random.value * 10f;
        perlinSeedDir   = Random.value * 10f;
        perlinSeedBob   = Random.value * 10f;

        // 初始朝向與速度
        dirXZ = RandomHorizontal();
        desiredHeading = dirXZ;
        speedTarget = speed;
        speedCurrent = speed;

        ScheduleNextRetarget();
    }

    void FixedUpdate()
    {
        if (!zone) return;
        float dt = Time.fixedDeltaTime;
        Vector3 pos = rb.position;

        // === 1) 更新目標朝向（Wander + Perlin 偏擾），並限制轉向角速度 ===
        if (Time.time >= nextRetargetTime) ScheduleNextRetarget();
        Vector3 noiseDir = Perlin2D(Time.time * dirNoiseFreq, perlinSeedDir);
        noiseDir *= dirNoiseStrength;
        Vector3 wantedDir = (desiredHeading + noiseDir).normalized;
        dirXZ = TurnToward(dirXZ, wantedDir, maxTurnDegPerSec * Mathf.Deg2Rad * dt);

        // === 2) 速度曲線：Perlin 速度因子 + 平滑 ===
        float perlin = Mathf.PerlinNoise(Time.time * speedNoiseFreq, perlinSeedSpeed); // 0~1
        float factor = Mathf.Lerp(1f - speedJitter, 1f + speedJitter, perlin);         // e.g. 0.75~1.25
        speedTarget = Mathf.Clamp(speed * factor, 0.01f, maxSpeed*0.9f);
        speedCurrent = Mathf.SmoothDamp(speedCurrent, speedTarget, ref speedVelRef, speedSmoothTime);

        // 想要的基礎水平速度
        Vector3 desiredVel = dirXZ * speedCurrent;

        // === 3) 分離力（反平方 + 核心彈簧，最後再限幅） ===
        if (separationRadius > 0f)
        {
            var hits = Physics.OverlapSphere(pos, separationRadius, avoidLayers, QueryTriggerInteraction.Ignore);
            Vector3 sep = Vector3.zero;

            foreach (var h in hits)
            {
                if (!h || h.attachedRigidbody == rb) continue;

                // 用 ClosestPoint 避免凸包/邊緣造成方向錯亂
                Vector3 p = h.ClosestPoint(pos);
                Vector3 away = pos - p; away.y = 0f;
                float d = away.magnitude + 1e-4f;
                Vector3 n = away / d;

                // 反平方衰減（遠距柔和）
                float invSq = separationFalloff / (d * d);
                float repel = invSq;

                // 軟式核心距（近距再加一層線性彈簧，避免真的貼合重疊）
                if (d < minSeparation)
                {
                    float pen = (minSeparation - d) / Mathf.Max(minSeparation, 1e-4f); // 0~1
                    repel += separationSpring * pen; // 疊加到加速度權重
                }

                sep += n * repel;
            }

            // 限制最大「等效加速度」
            sep = ClampMagnitude(sep, separationMaxAccel);
            desiredVel += sep * dt;
        }


        // === 4) 牆面預判：往前射線，太近時往「切線方向」滑開 ===
        Vector3 fwd = (velXZ.sqrMagnitude > 1e-6f ? velXZ.normalized : dirXZ);
        if (Physics.Raycast(pos, fwd, out RaycastHit hit, wallProbeDist, avoidLayers, QueryTriggerInteraction.Ignore))
        {
            // 切線方向（沿牆走），避免直撞
            Vector3 tangent = Vector3.Cross(Vector3.up, hit.normal).normalized;
            Vector3 steer = tangent * wallAvoidAccel;
            desiredVel += steer * dt;
        }

        // === 5) 限制最大水平加速度與速度，並平滑（SmoothDamp 比 Lerp 更穩） ===
        Vector3 wantAccel = (desiredVel - velXZ) / Mathf.Max(dt, 1e-4f);
        wantAccel = ClampMagnitude(wantAccel, maxAccel);
        velXZ += wantAccel * dt;
        velXZ = ClampMagnitude(velXZ, maxSpeed);
        Vector3 next = pos + velXZ * dt;

        // === 6) 邊界處理：盡量不硬夾牆，超出才反彈修正 ===
        var bnds = zone.bounds;
        if (!bnds.Contains(new Vector3(next.x, Mathf.Clamp(next.y, bnds.min.y, bnds.max.y), next.z)))
        {
            // 反彈到邊界面內，並把速度反向一點（像彈性小球但很軟）
            Vector3 clamped = ClampTo(bnds, next);
            Vector3 pushBack = (clamped - next);
            velXZ += pushBack / Mathf.Max(dt, 1e-4f);
            velXZ *= 0.8f; // 吸收一點能量，避免來回震盪
            next = clamped;
       }

        // ====== A) 每幀最大位移上限，避免巨量跳動 ======
        float maxStep = maxSpeed * 0.8f * dt;      // 一幀最多前進 1.5 倍最大速（你也可用 1.2~2.0 調）
        Vector3 displacement = next - pos;
        if (displacement.magnitude > maxStep)
        {
            next = pos + displacement.normalized * maxStep;
        }

        // ====== B) 邊界小幅回正（若已在你的程式較前面做完邊界處理，這段留作保險即可）======
        if (!bnds.Contains(new Vector3(next.x, Mathf.Clamp(next.y, bnds.min.y, bnds.max.y), next.z)))
        {
            // 僅把超出的分量拉回一點點，而不是整個硬夾
            Vector3 clamped = ClampTo(bnds, next);
            Vector3 corr = clamped - next;
            // 小幅回正＋吸能，避免彈弓效應
            next += corr * 0.85f;
            velXZ *= 0.85f;
        }

        // ====== C) 最終再保一次速度上限（避免下一幀爆衝）======
        velXZ = ClampMagnitude(velXZ, maxSpeed);


        // === 7) 垂直漂浮（多諧波 + Perlin），再平滑高度 ===
        float t = Time.time;
        float bob = bobAmp1 * Mathf.Sin(2f * Mathf.PI * bobFreq1 * t + 0.8f)    // 主諧波
        + bobAmp2 * Mathf.Sin(2f * Mathf.PI * bobFreq2 * t + 2.1f)    // 次諧波
        + (Mathf.PerlinNoise(t * 0.35f, perlinSeedBob) - 0.5f) * 2f * bobPerlinAmp;  // 少量 Perlin 打破規律

        float targetY = baseY + bob;
        float newY = Mathf.SmoothDamp(pos.y, targetY, ref bobVelRef, bobSmoothTime);
        next.y = Mathf.Clamp(newY, bnds.min.y + 0.02f, bnds.max.y - 0.02f);

        // === 8) 朝向：只看水平速度，避免上下抖動影響朝向 ===
        Vector3 face = velXZ.sqrMagnitude > 1e-6f ? velXZ : dirXZ;
        // 朝向處理

        // idle 微旋轉：當速度很小時（基本停著看）才做
        float mag = velXZ.magnitude;
        float idleAngle = 0f;

        if (mag < idleSpeedThreshold)
        {
            float t3 = Time.time + idleRotPhaseSeed;

            // 多諧波：主 + 次 + 微Perlin → AAA常用 idle
            float wobble =
                Mathf.Sin(2f * Mathf.PI * idleRotFreq1 * t3) * 0.7f +
                Mathf.Sin(2f * Mathf.PI * idleRotFreq2 * t3 + 1.4f) * 0.3f +
                (Mathf.PerlinNoise(t3 * 0.3f, idleRotPhaseSeed) - 0.5f) * 0.2f;

            float targetIdle = wobble * idleRotationAngle;
            idleAngle = Mathf.SmoothDamp(idleAngle, targetIdle, ref idleRotSmoothVel, 0.25f);
        }
        else
        {
            idleRotSmoothVel = 0f;
        }

        // 計算最終朝向
        Quaternion wantRot = Quaternion.LookRotation(new Vector3(face.x, 0f, face.z));
        Quaternion idleRot = Quaternion.Euler(0f, idleAngle, 0f);
        Quaternion finalRot = wantRot * idleRot;

        rb.MoveRotation(Quaternion.Slerp(rb.rotation, finalRot, 6f * dt));


        rb.MovePosition(next);
    }

    // === Helpers ===
    void ScheduleNextRetarget()
    {
        nextRetargetTime = Time.time + Random.Range(retargetEvery.x, retargetEvery.y);
        desiredHeading = RandomHorizontal();
    }

    Vector3 RandomHorizontal()
    {
        Vector3 v = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        return (v.sqrMagnitude < 1e-6f) ? Vector3.forward : v.normalized;
    }

    static Vector3 ClampTo(Bounds b, Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, b.min.x, b.max.x);
        p.y = Mathf.Clamp(p.y, b.min.y, b.max.y);
        p.z = Mathf.Clamp(p.z, b.min.z, b.max.z);
        return p;
    }

    static Vector3 ClampMagnitude(Vector3 v, float maxMag)
    {
        float m = v.magnitude;
        return (m > maxMag) ? (v * (maxMag / (m + 1e-6f))) : v;
    }

    // 把 Perlin(0~1) 做成 2D 單位向量（平面）
    Vector3 Perlin2D(float t, float seed)
    {
        float a = Mathf.PerlinNoise(t, seed);
        float b = Mathf.PerlinNoise(seed, t);
        Vector2 v = new Vector2(a - 0.5f, b - 0.5f);
        if (v.sqrMagnitude < 1e-6f) v = Vector2.right;
        return new Vector3(v.normalized.x, 0f, v.normalized.y);
    }

    // 以角速度限制轉向（避免瞬間折返造成閃動）
    Vector3 TurnToward(Vector3 currentDir, Vector3 targetDir, float maxRadPerStep)
    {
        currentDir.y = 0; targetDir.y = 0;
        currentDir.Normalize(); targetDir.Normalize();
        float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(currentDir, targetDir), -1f, 1f));
        if (angle <= maxRadPerStep) return targetDir;
        float t = maxRadPerStep / Mathf.Max(angle, 1e-6f);
        return Vector3.Slerp(currentDir, targetDir, t).normalized;
    }

    // 還保留舊介面相容
    public float moveSpeed { get => speed; set => speed = value; }
}      
