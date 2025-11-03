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
    private FullRing fr;

    // Start is called before the first frame update
    void Start()
    {
        ring = Instantiate(ringPrefab);
        ring.SetActive(false);
        
        fr = ring.GetComponent<FullRing>();
    }

    // Update is called once per frame
    void Update()
    {
        float dotProd = Vector3.Dot(leftHandRing.up, rightHandRing.up);
        float dist = Vector3.Distance(leftHandRing.position, rightHandRing.position);

        if (!ringFormed)
        {
            if (Mathf.Abs(dotProd) < 0.1f && Mathf.Abs(dist - ringRadius * 1.41421f) < 0.05f)
            {
                // Spawn ring based on color
                ring.SetActive(true);
                ring.transform.position = (leftHandRing.position + rightHandRing.position) * 0.5f;
                Color c = GameColorExtensions.ToColor(GameColorExtensions.Add(leftHandRing.GetComponent<QuarterRing>().color, rightHandRing.GetComponent<QuarterRing>().color));

                ringFormed = true;
            }
        } else
        {
            ring.transform.position = (leftHandRing.position + leftHandRing.up * ringRadius + rightHandRing.position + rightHandRing.up * ringRadius) * 0.5f;

            if (Mathf.Abs(dotProd) > 0.12f || Mathf.Abs(dist - ringRadius * 1.41421f) > 0.07f)
            {
                ring.SetActive(false);

                ringFormed = false;
            }

            // Set ring position and rotation


        }
    }
}
