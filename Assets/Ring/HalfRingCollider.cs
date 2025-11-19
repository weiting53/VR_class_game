using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HalfRingCollider : MonoBehaviour
{
    [SerializeField] private HalfRing selfRing;

    // The ring we are currently interacting with
    public HalfRing CollidedRing { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("flag a");

        // // Print basic collider and GameObject info
        // var go = other.gameObject;
        // Debug.Log($"Collided GameObject: name='{go.name}', tag='{go.tag}', layer='{LayerMask.LayerToName(go.layer)}', position={go.transform.position}");

        // // Print collider details
        // Debug.Log($"Collider: type={other.GetType().Name}, enabled={other.enabled}, isTrigger={other.isTrigger}");

        // // List components on the collided GameObject
        // var comps = go.GetComponents<Component>();
        // for (int i = 0; i < comps.Length; i++)
        //     Debug.Log($"Component[{i}] = {comps[i].GetType().Name}");

        // // If it has a HalfRing component, print some info (if present)
        // if (other.TryGetComponent<HalfRing>(out var ring))
        // {
        //     Debug.Log($"HalfRing found on collided object: {ring}");
        // }

        if (!other.TryGetComponent<HalfRingCollider>(out var half))
            return;
        
        Debug.Log("flag b");

        // Only set if nothing is currently assigned
        if (CollidedRing == null)
        {
            CollidedRing = half.selfRing;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<HalfRingCollider>(out var half))
            return;

        if (CollidedRing == half.selfRing)
        {
            CollidedRing = null;
        }
    }
}

