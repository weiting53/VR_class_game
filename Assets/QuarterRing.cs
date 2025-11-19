using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuarterRing : MonoBehaviour
{
    public GameColor color;
    private Renderer renderer;

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        renderer.material.color = GameColorExtensions.ToColor(color);
    }

    public void SetColor(GameColor c)
    {
        Debug.Log("Set color");
        color = c;
        renderer.material.color = GameColorExtensions.ToColor(color);
    }

    public void SwitchColor()
    {
        if (color == GameColor.Red)
            SetColor(GameColor.Green);
        else if (color == GameColor.Green)
            SetColor(GameColor.Blue);
        else if (color == GameColor.Blue)
            SetColor(GameColor.Red);
    }
}
