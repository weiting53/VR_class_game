using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalRotation : MonoBehaviour
{
    [Header("Rotatation Setting")]
    [Tooltip("旋轉速度（度/秒）")]
    public float rotationSpeed = 90f; // 預設每秒旋轉90度

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 沿著自己的 y 軸旋轉
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
    }
}
