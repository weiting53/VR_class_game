using Unity.Netcode;
using UnityEngine;

public class NetworkRingSqueezer : NetworkBehaviour
{
    public enum Side { A, B }

    [Header("References")]
    public NetworkObject bubblePrefab;

    [Header("Squeeze Logic")]
    [Tooltip("���e��Z <= ��l��Z �� �o�Ӥ�� �N��������")]
    [Range(0.2f, 0.99f)] public float squeezeDistanceRatio = 0.75f;
    [Tooltip("����ݫ���h�[�~Ĳ�o(��)")]
    public float sustainTime = 0.15f;
    [Tooltip("Ĳ�o��N�o(��)")]
    public float cooldown = 1f;

    [Header("Bubble Launch")]
    public float bubbleImpulse = 1.5f;

    [Header("Debug")]
    public bool debugLogs = true;

    // ���A���ݫO�s���u����ɤ⪺��m�v
    Vector3 _handPosA, _handPosB;
    bool _hasA, _hasB;
    float _lastA, _lastB;          // �^���ɶ��W�A�קK�¸��
    float _restHandDist = -1f;     // ����l�Z��
    float _sustain, _cd;

    const float STALE = 0.25f;     // �h�֬����������Ħ^��

    void Update()
    {
        if (!IsServer) return;

        if (_cd > 0f) _cd -= Time.deltaTime;

        bool aValid = _hasA && (Time.time - _lastA) <= STALE;
        bool bValid = _hasB && (Time.time - _lastB) <= STALE;

        if (!(aValid && bValid)) { _sustain = 0f; return; }

        float cur = Vector3.Distance(_handPosA, _handPosB);

        // �Ĥ@������P�ɦ��� �� �]��l�Z��
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

    // === �����b���/��}�ɩI�s�]�|�q Client ���� Server�^ ===
    public void NotifyGrabbed(ulong clientId, Side side)
    {
        // �u�аO���֦b��F��m�H UpdateHandPosServerRpc �^������
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

    // ====== �u���g�J�]�u�b Server ����^ ======
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
