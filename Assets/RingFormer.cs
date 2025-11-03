using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingFormer : MonoBehaviour
{
    public Transform leftHandRing;
    public Transform rightHandRing;
    public GameObject ringPrefab;

    private float ringRadius = 0.2f;
    private bool ringFormed = false;
    private GameObject ring;

    // Start is called before the first frame update
    void Start()
    {
        ring = Instantiate(ringPrefab);
        ring.SetActive(false);
        // qr = ring.GetComponent<QuarterRing>();
    }

    // Update is called once per frame
    void Update()
    {
        float dotProd = Vector3.Dot(leftHandRing.forward, rightHandRing.forward);
        float dist = Vector3.Distance(leftHandRing.position, rightHandRing.position);

        if (!ringFormed)
        {
            if (Mathf.Abs(dotProd) < 0.2f && Mathf.Abs(dist - ringRadius * 1.41421f) < 0.1f)
            {
                // Spawn ring based on color
                ring.SetActive(true);
                ring.transform.position = (leftHandRing.position + rightHandRing.position) * 0.5f;

                ringFormed = true;
            }
        } else
        {
            ring.transform.position = (leftHandRing.position + rightHandRing.position) * 0.5f;

            if (Mathf.Abs(dotProd) > 0.22f || Mathf.Abs(dist - ringRadius * 1.41421f) > 0.12f)
            {
                ringFormed = false;
            }

            // Set ring position and rotation


        }
    }
}
