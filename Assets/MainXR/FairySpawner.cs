using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;

public class FairySpawner : MonoBehaviour
{
    public BoxCollider zone;                     // FairyZone
    public GameObject spiritBlue;
    public GameObject spiritGreen;
    public GameObject spiritRed;
    public GameObject spiritYellow;

    [Range(0.5f, 2f)] public float minSpeed = 0.8f;
    [Range(0.5f, 2f)] public float maxSpeed = 1.3f;

    void Start()
    {
        var list = new List<GameObject> { spiritBlue, spiritGreen, spiritRed, spiritYellow };
        var toSpawn = new List<GameObject>();

        // 每色 2 隻
        foreach (var p in list) { toSpawn.Add(p); toSpawn.Add(p); }

        // 再隨機 2 隻
        for (int i = 0; i < 2; i++)
            toSpawn.Add(list[Random.Range(0, list.Count)]);

        foreach (var prefab in toSpawn)
            SpawnOne(prefab);
    }

    void SpawnOne(GameObject prefab)
    {
        var b = zone.bounds;
        Vector3 pos = new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.center.y - 0.2f, b.center.y + 0.2f),
            Random.Range(b.min.z, b.max.z)
        );

        var go = Instantiate(prefab, pos, Quaternion.identity, transform);

        var fw = go.GetComponent<FairyWanderer>();
        if (fw == null) fw = go.AddComponent<FairyWanderer>();
        fw.zone = zone;
        fw.moveSpeed = Random.Range(minSpeed, maxSpeed);
        fw.fairyLayer = LayerMask.GetMask("Default"); // 或你的 Fairy 圖層
    }
}
