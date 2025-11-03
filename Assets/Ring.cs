using UnityEngine;

public class RingFollower : MonoBehaviour
{
    public Transform rightHand;

    void Update()
    {
        if (rightHand != null)
        {
            transform.position = rightHand.position;
            transform.rotation = rightHand.rotation;
        }
    }
}
