using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Unity.Netcode;

public class RingSpawner : MonoBehaviour
{
    [Header("Hand Anchors")]
    public GameObject leftHandAnchor;
    public GameObject rightHandAnchor;

    [Header("Ring Prefabs")]
    public GameObject leftHalfRingPrefab;
    public GameObject rightHalfRingPrefab;

    private GameObject leftRingInstance;
    private GameObject rightRingInstance;

    public void InitializeHalfRings()
    {
        // --- IMPORTANT ---
        // You only want the SERVER (or Host) to spawn objects.
        // Add this check to prevent clients from running this code.
        // if (!IsServer)
        // {
        //     return;
        // }

        // Now it's safe to call your spawning logic
        Debug.Log("Spawn Half Rings");
        // SpawnHalfRings();
    }

    void Start()
    {
        Debug.Log("Start");
        SpawnHalfRings();
    }

    void SpawnHalfRings()
    {
        if (leftHalfRingPrefab != null && leftHandAnchor != null)
        {
            leftRingInstance = Instantiate(leftHalfRingPrefab);
            leftRingInstance.transform.SetParent(leftHandAnchor.transform, false);
            leftRingInstance.transform.localPosition = Vector3.zero;
            Vector3 targetLocalRotation = new Vector3(-45f, 180f, 0f);
            leftRingInstance.transform.localRotation = Quaternion.Euler(targetLocalRotation);
        }

        if (rightHalfRingPrefab != null && rightHandAnchor != null)
        {
            rightRingInstance = Instantiate(rightHalfRingPrefab);
            rightRingInstance.transform.SetParent(rightHandAnchor.transform, false);
            rightRingInstance.transform.localPosition = Vector3.zero;
            Vector3 targetLocalRotation = new Vector3(45f, 0f, 0f);
            rightRingInstance.transform.localRotation = Quaternion.Euler(targetLocalRotation);
        }
    }

    public void SwitchLeftColor()
    {
        leftRingInstance.GetComponent<HalfRing>().SwitchColor();
    }

    public void SwitchRightColor()
    {
        rightRingInstance.GetComponent<HalfRing>().SwitchColor();
    }
}
