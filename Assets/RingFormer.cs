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
        float dotProdUp = Vector3.Dot(leftHandRing.up, rightHandRing.up);
        float dotProdForward = Vector3.Dot(leftHandRing.forward, rightHandRing.forward);
        float dist = Vector3.Distance(leftHandRing.position, rightHandRing.position);

        if (!ringFormed)
        {
            if (
                Mathf.Abs(dist - ringRadius * 1.41421f) < 0.02f &&
                Mathf.Abs(dotProdUp) < 0.1f &&
                Mathf.Abs(dotProdForward) > 0.9f
            )
            {
                // Spawn ring based on color
                ring.SetActive(true);
                // ring.transform.position = (leftHandRing.position + rightHandRing.position) * 0.5f;
                

                ringFormed = true;
            }
        } else
        {
            ring.transform.position = (leftHandRing.position - leftHandRing.up * ringRadius + rightHandRing.position + rightHandRing.up * ringRadius) * 0.5f;

            GameColor c = GameColorExtensions.Add(leftHandRing.GetComponent<QuarterRing>().color, rightHandRing.GetComponent<QuarterRing>().color);
            fr.SetColor(c);
            // ring.transform.position = leftHandRing.position + leftHandRing.up * ringRadius;

            if (
                Mathf.Abs(dist - ringRadius * 1.41421f) > 0.04f ||
                Mathf.Abs(dotProdUp) > 0.12f ||
                Mathf.Abs(dotProdForward) < 0.88f
            )
            {
                ring.SetActive(false);

                ringFormed = false;
            }

            // Set ring position and rotation


        }
    }
}
