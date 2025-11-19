using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HalfRing : MonoBehaviour
{
    public GameColor color;
    public GameObject redHalfRing;
    public GameObject yellowHalfRing;
    public GameObject blueHalfRing;
    public GameObject transHalfRing;

    public HalfRingCollider collider1;
    public HalfRingCollider collider2;

    public GameObject fullRingPrefab;
    private GameObject fullRingInstance;   

    public float randomId { get; private set; } = -1;

    private CombinationState state = CombinationState.Separated;

    private enum CombinationState
    {
        Separated,
        Master,
        Slave
    }

    public void SetColor(GameColor c)
    {
        color = c;

        // disable all rings first
        if (redHalfRing) redHalfRing.SetActive(false);
        if (yellowHalfRing) yellowHalfRing.SetActive(false);
        if (blueHalfRing) blueHalfRing.SetActive(false);
        if (transHalfRing) transHalfRing.SetActive(false);

        // enable only the corresponding ring
        switch (color)
        {
            case GameColor.Red:
                if (redHalfRing) redHalfRing.SetActive(true);
                break;
            case GameColor.Yellow:
                if (yellowHalfRing) yellowHalfRing.SetActive(true);
                break;
            case GameColor.Blue:
                if (blueHalfRing) blueHalfRing.SetActive(true);
                break;
            case GameColor.Transparent:
                if (transHalfRing) transHalfRing.SetActive(true);
                break;
            default:
                // no ring active
                break;
        }
    }

    public void SetColor()
    {
        SetColor(color);
    }

    public void SetInvisible()
    {
        if (redHalfRing) redHalfRing.SetActive(false);
        if (yellowHalfRing) yellowHalfRing.SetActive(false);
        if (blueHalfRing) blueHalfRing.SetActive(false);
        if (transHalfRing) transHalfRing.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        SetColor(color);
        randomId = Random.Range(0f, 1000f);
    }

    // Update is called once per frame
    void Update()
    {
        if (collider1.CollidedRing != null && collider2.CollidedRing != null &&
            collider1.CollidedRing.randomId == collider2.CollidedRing.randomId)
        {
            HalfRing other = collider1.CollidedRing;
            if (randomId > other.randomId)
            {
                state = CombinationState.Master;

                if (fullRingPrefab == null) return;

                if (fullRingInstance == null)
                {
                    fullRingInstance = Instantiate(fullRingPrefab);
                }

                if (fullRingInstance != null)
                {
                    fullRingInstance.transform.position = (transform.position + other.transform.position) * 0.5f;

                    Vector3 avgForward = (transform.forward + other.transform.forward) * 0.5f;
                    Vector3 avgUp = (transform.up + other.transform.up) * 0.5f;
                    
                    if (Vector3.Dot(transform.forward, other.transform.forward) < 0)
                        avgForward = (transform.forward - other.transform.forward) * 0.5f;
                    
                    if (Vector3.Dot(transform.up, other.transform.up) < 0)
                        avgUp = (transform.up - other.transform.up) * 0.5f;
                    
                    fullRingInstance.transform.rotation = Quaternion.LookRotation(avgForward, avgUp);
                    fullRingInstance.GetComponent<FullRing>().SetColor(GameColorExtensions.Add(color, other.color));
                }
            } 
            else
            {
               state = CombinationState.Slave; 
            }
            
            SetInvisible();
        } 
        else
        {
            if(state == CombinationState.Master)
            {
                if (fullRingInstance != null)
                {
                    Destroy(fullRingInstance);
                } 
            }

            state = CombinationState.Separated;

            SetColor();
        }
    }

    public void SwitchColor()
    {
        GameColor nextColor = (GameColor)(((int)color + 1) % 4);
        SetColor(nextColor);
    }
}
