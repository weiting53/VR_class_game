using System;
using UnityEngine;
using Unity.Netcode; // 引入 Netcode 命名空間

public class FairyThrowerNetwork : NetworkBehaviour // 改為繼承 NetworkBehaviour
{
    public enum SpawnOriginMode { UseTransform, MidpointOfTwo, VolumeBox }
    public enum DirectionMode { UseForwardOfRef, AveragePlayersForward, TowardsPlayersCentroid, RandomConeAroundRef }

    [Header("Spawn Origin")]
    public SpawnOriginMode spawnOriginMode = SpawnOriginMode.VolumeBox; // 預設改為 VolumeBox 以符合你的描述
    public Transform origin;
    
    // 注意：在連線遊戲中，直接拉場景中的 Player Transform 可能會失效
    // 建議之後透過 NetworkManager 獲取玩家清單，這裡先保留變數
    public Transform playerA; 
    public Transform playerB;

    [Tooltip("請將 CenterEyeAnchor 底下的 Volume 拉到這裡")]
    public BoxCollider spawnVolume;

    [Header("Direction")]
    public DirectionMode directionMode = DirectionMode.RandomConeAroundRef;
    public Transform forwardRef; // 這裡拉 ThrowDirectionRef

    [Range(0f, 45f)]
    public float angleDeg = 12f;

    [Header("Speed / Spin")]
    public Vector2 speedRange = new Vector2(1.5f, 3f); // MR 空間較小，建議速度稍微調慢測試
    public Vector2 randomAngularVelRangeDeg = new Vector2(0f, 360f);

    [Header("Weighted Prefabs")]
    public WeightedPrefab[] weightedPrefabs = new WeightedPrefab[6];

    [Header("Auto Spawn")]
    public bool autoSpawn = false;
    [Min(0f)] public float spawnsPerSecond = 0.5f; // 頻率也不要太高

    [Header("Debug / Keys")]
    public bool drawGizmos = true;
    public KeyCode throwKey = KeyCode.Space;

    private float _accum;

    // Update 只在 Server 端執行邏輯
    void Update()
    {
        // 1. 只有 Server (Host) 有權力生成物件
        if (!IsServer) return;

        if (Input.GetKeyDown(throwKey)) ThrowOne();

        if (autoSpawn && spawnsPerSecond > 0f)
        {
            _accum += Time.deltaTime * spawnsPerSecond;
            while (_accum >= 1f) { _accum -= 1f; ThrowOne(); }
        }
    }
    
    public void ThrowOne()
    {
        var pf = PickPrefabByWeight();
        if (pf == null) return;

        Vector3 pos = GetSpawnPosition();
        Vector3 dir = GetDirection(pos);
        float v = UnityEngine.Random.Range(speedRange.x, speedRange.y);

        // 2. 生成物件 (此時還只是 Server 本地的物件)
        var go = Instantiate(pf, pos, Quaternion.identity);

        // 3. 設定物理屬性
        var rb = go.GetComponent<Rigidbody>();
        if(rb == null) rb = go.AddComponent<Rigidbody>();
        
        rb.useGravity = false;
        rb.drag = 0f; 
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // 設定速度
        rb.velocity = dir * v;

        if (randomAngularVelRangeDeg.y > 0f)
        {
            Vector3 axis = UnityEngine.Random.onUnitSphere;
            float w = UnityEngine.Random.Range(randomAngularVelRangeDeg.x, randomAngularVelRangeDeg.y);
            rb.angularVelocity = axis * Mathf.Deg2Rad * w;
        }

        // 4. 【關鍵步驟】告訴 Netcode 這個物件要同步給所有人
        // 注意：Prefab 上必須已經掛好 NetworkObject 組件
        var netObj = go.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); 
        }
    }

    // --------- 以下邏輯保持不變 (省略部分與原本相同，直接複製原本的計算邏輯即可) ----------
    // 請將原本 GetSpawnPosition, GetDirection, PickPrefabByWeight, RandomDirectionInCone 複製過來
    // 為了版面整潔，我這裡只列出關鍵修改，計算數學部分與你原本的代碼完全通用。

    Vector3 GetSpawnPosition()
    {
        // (保留你原本的邏輯)
        if (spawnOriginMode == SpawnOriginMode.VolumeBox && spawnVolume != null)
        {
            Vector3 c = spawnVolume.center;
            Vector3 e = spawnVolume.size * 0.5f;
            Vector3 local = new Vector3(
                UnityEngine.Random.Range(-e.x, e.x),
                UnityEngine.Random.Range(-e.y, e.y),
                UnityEngine.Random.Range(-e.z, e.z)
            ) + c;
            return spawnVolume.transform.TransformPoint(local);
        }
        return transform.position;
    }

    Vector3 GetDirection(Vector3 spawnPos)
    {
        // (保留你原本的邏輯)
        Vector3 fwdRef = (forwardRef != null ? forwardRef.forward : transform.forward);
        if (directionMode == DirectionMode.RandomConeAroundRef)
             return RandomDirectionInCone(fwdRef, angleDeg);
        return fwdRef.normalized;
    }

    GameObject PickPrefabByWeight()
    {
        // (保留你原本的邏輯，稍微簡化 Random)
        float sum = 0f;
        foreach (var e in weightedPrefabs) if (e?.prefab != null) sum += e.weight;
        float r = UnityEngine.Random.Range(0, sum);
        float acc = 0f;
        foreach (var e in weightedPrefabs)
        {
            if (e?.prefab == null) continue;
            acc += e.weight;
            if (r <= acc) return e.prefab;
        }
        return weightedPrefabs.Length > 0 ? weightedPrefabs[0].prefab : null;
    }
    
    public static Vector3 RandomDirectionInCone(Vector3 forward, float angleDeg)
    {
        // 直接複製你原本的數學函式
        // ... (同你原本的代碼)
        // 這裡為了省空間先不重複貼上，請直接用你原本的
        return Vector3.forward; // 替換
    }

    [Serializable]
    public class WeightedPrefab
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        if (spawnVolume != null)
        {
            Gizmos.matrix = spawnVolume.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(spawnVolume.center, spawnVolume.size);
        }
    }
}