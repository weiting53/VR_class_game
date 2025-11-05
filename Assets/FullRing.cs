using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullRing : MonoBehaviour
{
    public GameColor color;
    public Renderer rd;

    private Material mat;
    
    private void Start()
    {
        mat = rd.material;
        mat.SetColor("_Color", GameColorExtensions.ToColor(color));
    }

    public void SetColor(GameColor c)
    {
        color = c;
        mat.SetColor("_Color", GameColorExtensions.ToColor(color));
    }
}
