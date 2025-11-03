using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR.EnvironmentDepth;

namespace Meta.XR
{
    public class Laser : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] Transform muzzle;
        [SerializeField] LineRenderer beam;

        [Header("Hit FX")]
        [SerializeField] ParticleSystem hitEffect;
        [SerializeField] GameObject hitMarker;
        [SerializeField] float markerLife = 3f;
        [SerializeField] float surfaceOffset = 0.005f;

        [Header("Config")]
        [SerializeField] float maxLength = 12f;
        [SerializeField] float beamOnTime = 0.05f;
        [SerializeField] float fireCooldown = 0.08f;

        EnvironmentRaycastManager rayMgr;
        EnvironmentDepthManager depthMgr;
        bool canFire = true;

        void Awake()
        {
            rayMgr = FindObjectOfType<EnvironmentRaycastManager>();
#if UNITY_2022_3_OR_NEWER
            depthMgr = FindAnyObjectByType<EnvironmentDepthManager>(FindObjectsInactive.Include);
#else
            depthMgr = FindObjectOfType<EnvironmentDepthManager>();
#endif
            if (rayMgr == null) Debug.LogError("EnvironmentRaycastManager not found in scene.");
        }

        void OnEnable()
        {
            if (depthMgr) depthMgr.enabled = true;
            if (beam) beam.enabled = false;
        }

        void Update()
        {
            if (!canFire) return;

            // 改成右手食指扳機
            if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            {
                StartCoroutine(FireOnce());
            }
        }

        System.Collections.IEnumerator FireOnce()
        {
            canFire = false;

            Vector3 origin = muzzle.position;
            Vector3 dir = muzzle.forward;
            var ray = new Ray(origin, dir);

            beam.positionCount = 2;
            beam.SetPosition(0, origin);

            Vector3 endPos = origin + dir * maxLength;
            Vector3 hitNorm = -dir;

            if (rayMgr != null && rayMgr.Raycast(ray, out var hit))
            {
                endPos = hit.point + hit.normal * surfaceOffset;
                hitNorm = hit.normal;

                if (hitEffect != null)
                {
                    var fx = Instantiate(hitEffect, endPos, Quaternion.LookRotation(hitNorm));
                    Destroy(fx.gameObject, 2f);
                }

                if (hitMarker != null)
                {
                    var marker = Instantiate(hitMarker, endPos, Quaternion.LookRotation(hitNorm));
                    if (markerLife > 0f) Destroy(marker, markerLife);
                }
            }

            beam.SetPosition(1, endPos);

            beam.enabled = true;
            yield return new WaitForSeconds(beamOnTime);
            beam.enabled = false;

            yield return new WaitForSeconds(fireCooldown);
            canFire = true;
        }
    }
}
