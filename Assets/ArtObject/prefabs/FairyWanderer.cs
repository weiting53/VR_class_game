using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FairyWanderer : MonoBehaviour
{
    public BoxCollider zone;
    public float moveSpeed = 1.2f;
    public float wanderStrength = 2.5f;    // 隨機亂飄
    public float avoidStrength = 2.8f;     // 避免靠太近
    public float avoidRadius = 0.7f;       // 距離太近分開
    public LayerMask fairyLayer;

    [Header("Float (上下漂浮)")]
    public float bobAmplitude = 0.12f;
    public float bobFrequency = 1.6f;

    Rigidbody rb;
    Vector3 velocity;
    float phase;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        phase = Random.value * Mathf.PI * 2f;
        velocity = Random.insideUnitSphere * moveSpeed;
    }

    void FixedUpdate()
    {
        if (!zone) return;

        Vector3 pos = rb.position;

        // ----- 自由飄移（隨機力） -----
        Vector3 randomDir = Random.insideUnitSphere * wanderStrength;

        // ----- 避免與其他精靈卡在一起 -----
        Vector3 separation = Vector3.zero;
        var hits = Physics.OverlapSphere(pos, avoidRadius, fairyLayer);
        foreach (var h in hits)
        {
            if (h.attachedRigidbody != null && h.attachedRigidbody != rb)
                separation += (pos - h.transform.position).normalized;
        }
        separation *= avoidStrength;

        // 計算目標速度
        velocity += (randomDir + separation) * Time.fixedDeltaTime;
        velocity = Vector3.ClampMagnitude(velocity, moveSpeed);

        // 上下漂浮
        float yBob = Mathf.Sin(Time.time * bobFrequency + phase) * bobAmplitude;
        Vector3 next = pos + velocity * Time.fixedDeltaTime;
        next.y += yBob;

        // 保持在區域內
        Bounds b = zone.bounds;
        next.x = Mathf.Clamp(next.x, b.min.x, b.max.x);
        next.y = Mathf.Clamp(next.y, b.min.y, b.max.y);
        next.z = Mathf.Clamp(next.z, b.min.z, b.max.z);
        // 加入 S 曲線漂移
        float curve = Mathf.Sin(Time.time * 0.7f + phase) * 0.8f;
        Vector3 curveDir = new Vector3(curve, 0, curve);
        velocity += curveDir;


        // 面向移動方向
        if (velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 4f * Time.fixedDeltaTime));
        }

        rb.MovePosition(next);
    }
}
