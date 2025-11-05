using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FairyWanderer : MonoBehaviour
{
    [Header("Zone")]
    public BoxCollider zone;

    [Header("Base Move")]
    [Tooltip("名義水平速度(公尺/秒)")]
    public float speed = 0.9f;

    [Header("Speed Modulation (知名遊戲常用作法)")]
    [Range(0f, 0.8f)] public float speedJitter = 0.25f;
    public float speedNoiseFreq = 0.35f;
    public float speedSmoothTime = 0.6f;

    [Header("Heading / Wander（方向漫步）")]
    public Vector2 retargetEvery = new Vector2(5f, 7f);
    public float maxTurnDegPerSec = 120f;
    public float dirNoiseFreq = 0.25f;
    [Range(0f, 1f)] public float dirNoiseStrength = 0.55f;

    [Header("Vertical Bob（上下漂浮）")]
    public float bobAmp1 = 0.07f, bobFreq1 = 0.5f;
    public float bobAmp2 = 0.03f, bobFreq2 = 0.87f;
    public float bobPerlinAmp = 0.015f;
    public float bobSmoothTime = 0.25f;

    [Header("Separation（避免靠太近；碰撞仍交給物理）")]
    public float separationRadius = 0.45f;
    public float separationMaxAccel = 1.8f;
    public float separationFalloff = 1.2f;
    public LayerMask avoidLayers = ~0;
    [Tooltip("最小舒適距（小於此距離會強烈分離）")]
    public float minSeparation = 0.28f;
    [Tooltip("核心區的彈簧係數（越大越快推開）")]
    public float separationSpring = 3.5f;

    [Header("Wall Avoid（牆面預判）")]
    public float wallProbeDist = 0.35f;
    public float wallAvoidAccel = 2.0f;

    [Header("Dynamics 限制")]
    [Tooltip("最大水平加速度（m/s²）")]
    public float maxAccel = 5f;                 // ← 給合理值（原本 0 會讓速度長不起來）
    [Tooltip("最大水平速度（m/s）")]
    public float maxSpeed = 1.6f;

    [Header("Idle 微旋轉設定")]
    public float idleSpeedThreshold = 0.05f;
    public float idleRotationAngle = 6f;
    public float idleRotFreq1 = 0.45f;
    public float idleRotFreq2 = 0.82f;
    float idleRotSmoothVel, idleRotPhaseSeed;

    [Header("Scale（開局自動縮到人 1/5）")]
    public float humanHeight = 1.7f;
    public float scaleToHuman = 0.2f;
    public float scaleMultiplier = 2.5f;

    // ==== 內部狀態 ====
    Rigidbody rb;
    Vector3 dirXZ;                       // 當前水平朝向（單位向量）
    float baseY;                         // 中線高度
    float nextRetargetTime;
    Vector3 desiredHeading;              // 目標朝向（平面）
    float speedTarget;                   // 目標速度（會隨 Perlin 起伏）
    float speedCurrent;                  // 平滑後當前速度
    float speedVelRef;                   // SmoothDamp 參考
    float bobVelRef;                     // 垂直高度平滑參考
    float perlinSeedSpeed, perlinSeedDir, perlinSeedBob;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // ==== 重要：改用動態剛體（會彼此碰撞/推擠） ====
        rb.isKinematic = false;                  // ← 讓物理解碰撞，彼此會互推
        rb.useGravity = false;
        rb.mass = 0.2f;                          // 可依感覺調
        rb.drag = 0.2f;                          // 微阻尼
        rb.angularDrag = 0.2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 保持直立

        // Fairy 的 Collider 請保持「非 Trigger」；要彼此碰撞，請確保 Layer 矩陣允許 Fairy↔Fairy 碰撞

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

        idleRotPhaseSeed = Random.value * 10f;    // 給 idle 旋轉相位
        ScheduleNextRetarget();
    }

    void FixedUpdate()
    {
        if (!zone) return;
        float dt = Time.fixedDeltaTime;
        Vector3 pos = rb.position;

        // === 1) Wander 方向（限角速度） ===
        if (Time.time >= nextRetargetTime) ScheduleNextRetarget();
        Vector3 noiseDir = Perlin2D(Time.time * dirNoiseFreq, perlinSeedDir) * dirNoiseStrength;
        Vector3 wantedDir = (desiredHeading + noiseDir).normalized;
        dirXZ = TurnToward(dirXZ, wantedDir, maxTurnDegPerSec * Mathf.Deg2Rad * dt);

        // === 2) 速度曲線（Perlin 因子 + 平滑） ===
        float perlin = Mathf.PerlinNoise(Time.time * speedNoiseFreq, perlinSeedSpeed); // 0~1
        float factor = Mathf.Lerp(1f - speedJitter, 1f + speedJitter, perlin);
        speedTarget = Mathf.Clamp(speed * factor, 0.01f, maxSpeed * 0.9f);
        speedCurrent = Mathf.SmoothDamp(speedCurrent, speedTarget, ref speedVelRef, speedSmoothTime);

        // 目標水平速度
        Vector3 desiredVelXZ = dirXZ * speedCurrent;

        // === 3) 軟分離力（禮貌讓位；真正推擠交給物理） ===
        if (separationRadius > 0f)
        {
            var hits = Physics.OverlapSphere(
                pos, separationRadius, avoidLayers, QueryTriggerInteraction.Ignore // 彼此是非 Trigger，Ignore 即可
            );

            Vector3 sepAccel = Vector3.zero;
            foreach (var h in hits)
            {
                if (!h || h.attachedRigidbody == rb) continue;
                Vector3 p = h.ClosestPoint(pos);
                Vector3 away = pos - p; away.y = 0f;
                float d = away.magnitude + 1e-4f;
                Vector3 n = away / d;

                float invSq = separationFalloff / (d * d);
                float repel = invSq;

                if (d < minSeparation)
                {
                    float pen = (minSeparation - d) / Mathf.Max(minSeparation, 1e-4f);
                    repel += separationSpring * pen;
                }
                sepAccel += n * repel;
            }
            sepAccel = ClampMagnitude(sepAccel, separationMaxAccel);
            desiredVelXZ += sepAccel * dt; // 轉成速度修正（讓位）
        }

        // === 4) 牆面預判：沿牆滑行偏轉（速度修正） ===
        Vector3 curVel = rb.velocity;
        Vector3 fwd = (new Vector3(curVel.x, 0f, curVel.z).sqrMagnitude > 1e-6f ? new Vector3(curVel.x, 0f, curVel.z).normalized : dirXZ);
        if (Physics.Raycast(pos, fwd, out RaycastHit hit, wallProbeDist, avoidLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, hit.normal).normalized;
            desiredVelXZ += tangent * wallAvoidAccel * dt;
        }

        // === 5) 用「加速度上限」把目前速度推向目標速度（交給物理解碰撞） ===
        Vector2 curXZ = new Vector2(curVel.x, curVel.z);
        Vector2 wantXZ = new Vector2(desiredVelXZ.x, desiredVelXZ.z);
        Vector2 newXZ = Vector2.MoveTowards(curXZ, wantXZ, maxAccel * dt); // 加速度夾限
        if (newXZ.magnitude > maxSpeed) newXZ = newXZ.normalized * maxSpeed;

        // === 6) 區域邊界（只“輕拉回”） ===
        var bnds = zone.bounds;
        if (!bnds.Contains(new Vector3(pos.x, Mathf.Clamp(pos.y, bnds.min.y, bnds.max.y), pos.z)))
        {
            // 若跑出外面，輕拉回中心方向一點點（別硬 teleport，讓碰撞持續自然）
            Vector3 center = bnds.center; center.y = pos.y;
            Vector3 back = (center - pos);
            Vector2 backXZ = new Vector2(back.x, back.z);
            newXZ += Vector2.ClampMagnitude(backXZ, 1f) * 0.2f; // 小小拉回
        }

        // === 7) 垂直漂浮：算出目標 y，再轉成 vy 指令 ===
        float t = Time.time;
        float bob =
            bobAmp1 * Mathf.Sin(2f * Mathf.PI * bobFreq1 * t + 0.8f) +
            bobAmp2 * Mathf.Sin(2f * Mathf.PI * bobFreq2 * t + 2.1f) +
            (Mathf.PerlinNoise(t * 0.35f, perlinSeedBob) - 0.5f) * 2f * bobPerlinAmp;

        float targetY = Mathf.Clamp(baseY + bob, bnds.min.y + 0.02f, bnds.max.y - 0.02f);
        float newY = Mathf.SmoothDamp(pos.y, targetY, ref bobVelRef, bobSmoothTime);
        float vy = (newY - pos.y) / Mathf.Max(dt, 1e-4f); // 把目標高度轉成速度

        // === 8) 指派剛體速度（交給物理引擎處理互撞/去穿透） ===
        rb.velocity = new Vector3(newXZ.x, vy, newXZ.y);

        // === 9) 朝向（看水平速度） + Idle 微旋轉 ===
        Vector3 face = (new Vector3(rb.velocity.x, 0f, rb.velocity.z).sqrMagnitude > 1e-6f)
                        ? new Vector3(rb.velocity.x, 0f, rb.velocity.z)
                        : dirXZ;

        float mag = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        float idleAngle = 0f;
        if (mag < idleSpeedThreshold)
        {
            float t3 = Time.time + idleRotPhaseSeed;
            float wobble =
                Mathf.Sin(2f * Mathf.PI * idleRotFreq1 * t3) * 0.7f +
                Mathf.Sin(2f * Mathf.PI * idleRotFreq2 * t3 + 1.4f) * 0.3f +
                (Mathf.PerlinNoise(t3 * 0.3f, idleRotPhaseSeed) - 0.5f) * 0.2f;
            float targetIdle = wobble * idleRotationAngle;
            idleAngle = Mathf.SmoothDamp(idleAngle, targetIdle, ref idleRotSmoothVel, 0.25f);
        }
        else idleRotSmoothVel = 0f;

        Quaternion wantRot = Quaternion.LookRotation(new Vector3(face.x, 0f, face.z));
        Quaternion idleRot = Quaternion.Euler(0f, idleAngle, 0f);
        Quaternion finalRot = wantRot * idleRot;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, finalRot, 6f * dt)); // 旋轉可用 MoveRotation 保持平滑
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

    Vector3 Perlin2D(float t, float seed)
    {
        float a = Mathf.PerlinNoise(t, seed);
        float b = Mathf.PerlinNoise(seed, t);
        Vector2 v = new Vector2(a - 0.5f, b - 0.5f);
        if (v.sqrMagnitude < 1e-6f) v = Vector2.right;
        return new Vector3(v.normalized.x, 0f, v.normalized.y);
    }

    Vector3 TurnToward(Vector3 currentDir, Vector3 targetDir, float maxRadPerStep)
    {
        currentDir.y = 0; targetDir.y = 0;
        currentDir.Normalize(); targetDir.Normalize();
        float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(currentDir, targetDir), -1f, 1f));
        if (angle <= maxRadPerStep) return targetDir;
        float t = maxRadPerStep / Mathf.Max(angle, 1e-6f);
        return Vector3.Slerp(currentDir, targetDir, t).normalized;
    }

    // 舊介面相容
    public float moveSpeed { get => speed; set => speed = value; }
}
