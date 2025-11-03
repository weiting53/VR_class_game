using System.Collections.Generic;
using UnityEngine;

public class FairySpawner : MonoBehaviour
{
    [Header("Zone")]
    public BoxCollider zone;   // 指向 FairyZone(BoxCollider, IsTrigger = ON)

    [Header("Prefabs")]
    public GameObject spiritBlue;
    public GameObject spiritGreen;
    public GameObject spiritRed;
    public GameObject spiritYellow;

    [Header("Move Speed (per-fairy random)")]
    [Range(0.5f, 2f)] public float minSpeed = 0.8f;
    [Range(0.5f, 2f)] public float maxSpeed = 1.3f;

    [Header("Uniform Scale ~ 人的 1/4–1/5")]
    public Vector2 uniformScaleRange = new Vector2(0.22f, 0.28f);

    [Header("Spawn scattering")]
    public float spawnMinSpacing = 0.6f;   // 彼此最小距離
    public int   spawnMaxTries   = 24;     // 嘗試次數

    [Header("Layer for separation")]
    public LayerMask fairyLayer = 0;       // 若有 Fairy 圖層可在 Inspector 指定

    // 記錄已生出的座標，用來避免出生重疊
    private readonly List<Vector3> _spawned = new List<Vector3>();

    void Start()
    {
        var list    = new List<GameObject> { spiritBlue, spiritGreen, spiritRed, spiritYellow };
        var toSpawn = new List<GameObject>();

        // 每色 2 隻
        foreach (var p in list) { toSpawn.Add(p); toSpawn.Add(p); }
        // 再隨機 2 隻
        for (int i = 0; i < 2; i++) toSpawn.Add(list[Random.Range(0, list.Count)]);

        foreach (var prefab in toSpawn) SpawnOne(prefab);
    }

    void SpawnOne(GameObject prefab)
    {
        if (zone == null || prefab == null) return;

        var b   = zone.bounds;
        var pos = Vector3.zero;
        bool ok = false;

        // 嘗試找到與既有點距離夠遠的位置
        for (int tries = 0; tries < spawnMaxTries; tries++)
        {
            var candidate = new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(Mathf.Lerp(b.min.y, b.max.y, 0.35f), Mathf.Lerp(b.min.y, b.max.y, 0.65f)),
                Random.Range(b.min.z, b.max.z)
            );

            ok = true;
            foreach (var p in _spawned)
            {
                if ((candidate - p).sqrMagnitude < spawnMinSpacing * spawnMinSpacing)
                {
                    ok = false;
                    break;
                }
            }
            if (ok) { pos = candidate; break; }
        }

        if (!ok)
        {
            pos = new Vector3(
                Random.Range(b.min.x, b.max.x),
                (b.min.y + b.max.y) * 0.5f,
                Random.Range(b.min.z, b.max.z)
            );
        }

        var go = Instantiate(prefab, pos, Quaternion.identity, transform);

        // 初始朝向隨機（Y 軸）
        go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // 等比隨機縮放（約 1/4–1/5 人）
        float s = Random.Range(uniformScaleRange.x, uniformScaleRange.y);
        go.transform.localScale = Vector3.one * s;

        // 掛/設定移動腳本
        var fw = go.GetComponent<FairyWanderer>();
        if (fw == null) fw = go.AddComponent<FairyWanderer>();
        fw.zone       = zone;
        fw.moveSpeed  = Random.Range(minSpeed, maxSpeed);
        fw.fairyLayer = fairyLayer;

        _spawned.Add(pos);
    }
}
