using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullRing : MonoBehaviour
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
        color = c;
        renderer.material.color = GameColorExtensions.ToColor(color);
    }
}
