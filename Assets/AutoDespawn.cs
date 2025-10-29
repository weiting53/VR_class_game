using Unity.Netcode;
using UnityEngine;

public class AutoDespawn : NetworkBehaviour
{
    public float lifeTime = 4f;

    float _t;
    void Update()
    {
        if (!IsServer) return;
        _t += Time.deltaTime;
        if (_t >= lifeTime)
        {
            if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn();
            Destroy(gameObject);
        }
    }
}