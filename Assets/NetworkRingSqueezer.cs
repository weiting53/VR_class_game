using Unity.Netcode;
using UnityEngine;

public class NetworkRingSqueezer : NetworkBehaviour
{
    public enum Side { A, B }

    [Header("References")]
    public NetworkObject bubblePrefab;

    [Header("Squeeze Logic")]
    [Tooltip("當前手距 <= 初始手距 × 這個比例 就視為擠壓")]
    [Range(0.2f, 0.99f)] public float squeezeDistanceRatio = 0.75f;
    [Tooltip("條件需持續多久才觸發(秒)")]
    public float sustainTime = 0.15f;
    [Tooltip("觸發後冷卻(秒)")]
    public float cooldown = 1f;

    [Header("Bubble Launch")]
    public float bubbleImpulse = 1.5f;

    [Header("Debug")]
    public bool debugLogs = true;

    // 伺服器端保存的「抓住時手的位置」
    Vector3 _handPosA, _handPosB;
    bool _hasA, _hasB;
    float _lastA, _lastB;          // 回報時間戳，避免舊資料
    float _restHandDist = -1f;     // 兩手初始距離
    float _sustain, _cd;

    const float STALE = 0.25f;     // 多少秒內視為有效回報

    void Update()
    {
        if (!IsServer) return;

        if (_cd > 0f) _cd -= Time.deltaTime;

        bool aValid = _hasA && (Time.time - _lastA) <= STALE;
        bool bValid = _hasB && (Time.time - _lastB) <= STALE;

        if (!(aValid && bValid)) { _sustain = 0f; return; }

        float cur = Vector3.Distance(_handPosA, _handPosB);

        // 第一次雙手同時有效 → 設初始距離
        if (_restHandDist < 0f) _restHandDist = cur;

        float thr = _restHandDist * squeezeDistanceRatio;
        bool compressed = cur <= thr;

        if (debugLogs)
            Debug.Log($"[Ring] hands cur={cur:F3} thr={thr:F3} cmp={compressed} sustain={_sustain:F2} cd={_cd:F2}");

        if (_cd <= 0f && compressed)
        {
            _sustain += Time.deltaTime;
            if (_sustain >= sustainTime)
            {
                _sustain = 0f; _cd = cooldown;
                SpawnBubble((_handPosA + _handPosB) * 0.5f);
            }
        }
        else
        {
            _sustain = 0f;
        }
    }

    void SpawnBubble(Vector3 pos)
    {
        if (!bubblePrefab) return;
        var rot = Quaternion.identity;
        var b = Instantiate(bubblePrefab, pos, rot);
        b.Spawn();
        if (b.TryGetComponent<Rigidbody>(out var rb) && bubbleImpulse > 0f)
            rb.AddForce(Vector3.up * bubbleImpulse, ForceMode.Impulse);
        if (debugLogs) Debug.Log($"[Ring] SpawnBubble at {pos}");
    }

    // === 橋接在抓住/放開時呼叫（會從 Client 走到 Server） ===
    public void NotifyGrabbed(ulong clientId, Side side)
    {
        // 只標記有誰在抓；位置以 UpdateHandPosServerRpc 回報為準
        if (IsServer) SetHas(side, true);
        else SetHasServerRpc(side, true);
    }

    public void NotifyReleased(ulong clientId, Side side)
    {
        if (IsServer) SetHas(side, false);
        else SetHasServerRpc(side, false);
    }

    public void ReportHandPosition(Side side, Vector3 worldPos)
    {
        if (IsServer) UpdateHandPos(side, worldPos);
        else UpdateHandPosServerRpc(side, worldPos);
    }

    [ServerRpc(RequireOwnership = false)]
    void SetHasServerRpc(Side side, bool on) => SetHas(side, on);

    [ServerRpc(RequireOwnership = false)]
    void UpdateHandPosServerRpc(Side side, Vector3 worldPos) => UpdateHandPos(side, worldPos);

    // ====== 真正寫入（只在 Server 執行） ======
    void SetHas(Side side, bool on)
    {
        if (side == Side.A) { _hasA = on; if (!on) _restHandDist = -1f; }
        else { _hasB = on; if (!on) _restHandDist = -1f; }
        if (debugLogs) Debug.Log($"[Ring] {(on ? "+" : "-")} {side}");
    }

    void UpdateHandPos(Side side, Vector3 p)
    {
        if (side == Side.A) { _handPosA = p; _lastA = Time.time; }
        else { _handPosB = p; _lastB = Time.time; }
    }
}
