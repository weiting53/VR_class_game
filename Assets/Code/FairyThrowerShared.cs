    using System;
    using UnityEngine;

    public class FairyThrowerShared : MonoBehaviour
    {
        public enum SpawnOriginMode { UseTransform, MidpointOfTwo, VolumeBox }
        public enum DirectionMode { UseForwardOfRef, AveragePlayersForward, TowardsPlayersCentroid, RandomConeAroundRef }

        [Header("Spawn Origin (不綁定玩家視角)")]
        public SpawnOriginMode spawnOriginMode = SpawnOriginMode.UseTransform;

        [Tooltip("UseTransform 模式下的錨點；或作為其它模式的備援參考")]
        public Transform origin;

        [Tooltip("MidpointOfTwo / AveragePlayersForward / TowardsPlayersCentroid 會用到")]
        public Transform playerA;
        public Transform playerB;

        [Tooltip("VolumeBox 模式用：請放一個 BoxCollider（IsTrigger=true/false皆可）表示投擲區域")]
        public BoxCollider spawnVolume;

        [Header("Direction")]
        public DirectionMode directionMode = DirectionMode.RandomConeAroundRef;

        [Tooltip("UseForwardOfRef / RandomConeAroundRef 模式使用的方向參考；可放場景中的方向物件")]
        public Transform forwardRef;

        [Range(0f, 45f)]
        [Tooltip("方向錐半角（度），僅在 RandomConeAroundRef 模式生效")]
        public float angleDeg = 12f;

        [Header("Speed / Spin")]
        public Vector2 speedRange = new Vector2(12f, 15f);
        public Vector2 randomAngularVelRangeDeg = new Vector2(0f, 360f);

        [Header("Weighted Prefabs")]
        public WeightedPrefab[] weightedPrefabs = new WeightedPrefab[6];

        [Header("Auto Spawn")]
        public bool autoSpawn = false;
        [Min(0f)] public float spawnsPerSecond = 2f;

        [Header("Debug / Keys")]
        public bool drawGizmos = true;
        public KeyCode throwKey = KeyCode.Space;
        public int randomSeed = -1;

        private float _accum;
        private System.Random _rnd;

        void Awake()
        {
            _rnd = (randomSeed >= 0) ? new System.Random(randomSeed) : new System.Random();
        }

        void Update()
        {
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
            if (pf == null) { Debug.LogWarning("[FairyThrowerShared] 沒有有效的 Prefab/權重"); return; }

            Vector3 pos = GetSpawnPosition();
            Vector3 dir = GetDirection(pos);
            float v = UnityEngine.Random.Range(speedRange.x, speedRange.y);

            var go = Instantiate(pf, pos, Quaternion.identity);

            var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.drag = 0f; 
            rb.angularVelocity = Vector3.zero;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.velocity = dir * v;

            if (randomAngularVelRangeDeg.y > 0f)
            {
                Vector3 axis = UnityEngine.Random.onUnitSphere;
                float w = UnityEngine.Random.Range(randomAngularVelRangeDeg.x, randomAngularVelRangeDeg.y);
                rb.angularVelocity = axis * Mathf.Deg2Rad * w;
            }
        }

        // --------- Spawn Position ----------
        Vector3 GetSpawnPosition()
        {
            switch (spawnOriginMode)
            {
                case SpawnOriginMode.UseTransform:
                    if (origin != null) return origin.position;
                    break;

                case SpawnOriginMode.MidpointOfTwo:
                    if (playerA != null && playerB != null)
                        return 0.5f * (playerA.position + playerB.position);
                    if (origin != null) return origin.position;
                    break;

                case SpawnOriginMode.VolumeBox:
                    if (spawnVolume != null)
                    {
                        // 在 BoxCollider 內取世界座標
                        Vector3 c = spawnVolume.center;
                        Vector3 e = spawnVolume.size * 0.5f;

                        Vector3 local = new Vector3(
                            UnityEngine.Random.Range(-e.x, e.x),
                            UnityEngine.Random.Range(-e.y, e.y),
                            UnityEngine.Random.Range(-e.z, e.z)
                        ) + c;

                        return spawnVolume.transform.TransformPoint(local);
                    }
                    if (origin != null) return origin.position;
                    break;
            }
            // 後備：用自己 transform
            return transform.position;
        }

        // --------- Direction ----------
        Vector3 GetDirection(Vector3 spawnPos)
        {
            Vector3 fwdRef = (forwardRef != null ? forwardRef.forward : transform.forward);

            switch (directionMode)
            {
                case DirectionMode.UseForwardOfRef:
                    return fwdRef.normalized;

                case DirectionMode.AveragePlayersForward:
                    if (playerA != null && playerB != null)
                    {
                        Vector3 f = (playerA.forward.normalized + playerB.forward.normalized);
                        if (f.sqrMagnitude > 1e-6f) return f.normalized;
                    }
                    return fwdRef.normalized;

                case DirectionMode.TowardsPlayersCentroid:
                    if (playerA != null && playerB != null)
                    {
                        Vector3 centroid = 0.5f * (playerA.position + playerB.position);
                        Vector3 dir = (centroid - spawnPos);
                        if (dir.sqrMagnitude > 1e-6f) return dir.normalized;
                    }
                    return fwdRef.normalized;

                case DirectionMode.RandomConeAroundRef:
                    return RandomDirectionInCone(fwdRef, angleDeg);
            }
            return fwdRef.normalized;
        }

        // --------- Utils ----------
        GameObject PickPrefabByWeight()
        {
            float sum = 0f;
            foreach (var e in weightedPrefabs)
                if (e != null && e.prefab != null && e.weight > 0f) sum += e.weight;
            if (sum <= 0f) return null;

            float r = (float)_rnd.NextDouble() * sum, acc = 0f;
            foreach (var e in weightedPrefabs)
            {
                if (e == null || e.prefab == null || e.weight <= 0f) continue;
                acc += e.weight;
                if (r <= acc) return e.prefab;
            }
            return weightedPrefabs[^1]?.prefab;
        }

        public static Vector3 RandomDirectionInCone(Vector3 forward, float angleDeg)
        {
            forward = forward.normalized;
            if (angleDeg <= 0f) return forward;

            float a = angleDeg * Mathf.Deg2Rad;
            float u = UnityEngine.Random.value;
            float v = UnityEngine.Random.value;
            float cosTheta = Mathf.Lerp(1f, Mathf.Cos(a), u);
            float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta);
            float phi = 2f * Mathf.PI * v;

            Vector3 up = (Mathf.Abs(Vector3.Dot(Vector3.up, forward)) > 0.999f) ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
            Vector3 up2 = Vector3.Cross(forward, right);

            return (right * (sinTheta * Mathf.Cos(phi)) +
                    up2   * (sinTheta * Mathf.Sin(phi)) +
                    forward * cosTheta).normalized;
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            // 畫出 spawn 區與方向
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
            Vector3 pos = Application.isPlaying ? GetSpawnPosition() : PreviewSpawnPosInEditor();
            Gizmos.DrawSphere(pos, 0.06f);

            Vector3 dir = GetDirection(pos);
            float v = Mathf.Lerp(speedRange.x, speedRange.y, 0.5f);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawRay(pos, dir * (v * 0.1f));

            if (spawnOriginMode == SpawnOriginMode.VolumeBox && spawnVolume != null)
            {
                Gizmos.color = new Color(0f, 1f, 0.2f, 0.15f);
                Matrix4x4 m = spawnVolume.transform.localToWorldMatrix;
                Gizmos.matrix = m;
                Gizmos.DrawCube(spawnVolume.center, spawnVolume.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }

        Vector3 PreviewSpawnPosInEditor()
        {
            switch (spawnOriginMode)
            {
                case SpawnOriginMode.UseTransform:
                    if (origin != null) return origin.position; break;
                case SpawnOriginMode.MidpointOfTwo:
                    if (playerA != null && playerB != null) return 0.5f * (playerA.position + playerB.position);
                    if (origin != null) return origin.position; break;
                case SpawnOriginMode.VolumeBox:
                    if (spawnVolume != null) return spawnVolume.bounds.center;
                    if (origin != null) return origin.position; break;
            }
            return transform.position;
        }
    }

    [Serializable]
    public class WeightedPrefab
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }
