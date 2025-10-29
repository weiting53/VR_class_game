using UnityEngine;

public class BallShooter : MonoBehaviour
{
    [Header("References")]
    public GameObject ballPrefab;     // 指向含 Rigidbody + SphereCollider + Bouncy material 的 prefab
    public Transform muzzle;          // 發射起點（可用此物件自身 Transform）

    [Header("Shoot Settings")]
    public float muzzleSpeed = 6.0f;  // 初速（m/s）
    public float spawnOffset = 0.05f; // 從發射點往前偏一點，避免與手/牆重疊

    void Reset()
    {
        muzzle = transform;
    }

    void Update()
    {
        // 右手扳機：按下就發射
        bool triggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        if (triggerDown)
        {
            Fire();
        }
    }

    void Fire()
    {
        var origin = muzzle ? muzzle : transform;
        Vector3 spawnPos = origin.position + origin.forward * spawnOffset;

        var ball = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
        var rb = ball.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.velocity = origin.forward * muzzleSpeed;
            rb.maxAngularVelocity = 50f;
            rb.solverIterations = 12;
            rb.solverVelocityIterations = 12;
        }
    }
}

