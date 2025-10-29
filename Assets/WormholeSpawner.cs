using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WormholeSpawner : MonoBehaviour
{
    public Transform left;
    public Transform right;
    public GameObject wormholePrefab;

    GameObject wormholeInstance;

        // Start is called before the first frame update
    void Start()
    {
        wormholeInstance = Instantiate(wormholePrefab);
        
    }

    // Update is called once per frame
    void Update()
    {
        wormholeInstance.transform.position = (left.position + right.position) / 2;
    }
}
