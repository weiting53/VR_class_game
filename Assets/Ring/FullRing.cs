using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullRing : MonoBehaviour
{
    public GameColor color;
    public GameObject redRing;
    public GameObject yellowRing;
    public GameObject blueRing;
    public GameObject orangeRing;
    public GameObject greenRing;
    public GameObject purpleRing;

    public void SetColor(GameColor c)
    {
        color = c;

        // disable all rings first
        if (redRing) redRing.SetActive(false);
        if (yellowRing) yellowRing.SetActive(false);
        if (blueRing) blueRing.SetActive(false);
        if (orangeRing) orangeRing.SetActive(false);
        if (greenRing) greenRing.SetActive(false);
        if (purpleRing) purpleRing.SetActive(false);

        // enable only the corresponding ring
        switch (color)
        {
            case GameColor.Red:
                if (redRing) redRing.SetActive(true);
                break;
            case GameColor.Yellow:
                if (yellowRing) yellowRing.SetActive(true);
                break;
            case GameColor.Blue:
                if (blueRing) blueRing.SetActive(true);
                break;
            case GameColor.Orange:
                if (orangeRing) orangeRing.SetActive(true);
                break;
            case GameColor.Green:
                if (greenRing) greenRing.SetActive(true);
                break;
            case GameColor.Purple:
                if (purpleRing) purpleRing.SetActive(true);
                break;
            default:
                // no ring active for unknown colors
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // public void SwitchColor()
    // {
    //     GameColor nextColor = (GameColor)(((int)color + 1) % 4);
    //     SetColor(nextColor);
    // }
}
