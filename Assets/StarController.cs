using UnityEngine;

public class StarController : MonoBehaviour
{
    private Material starMaterial;
    private Color originalColor = Color.black;
    private Color brightColor = Color.yellow;
    private float duration = 2f;

    void Start()
    {
        starMaterial = GetComponent<Renderer>().material;
        starMaterial.color = originalColor;
    }

    public void MakeBright()
    {
        StartCoroutine(ChangeColor());
    }

    private System.Collections.IEnumerator ChangeColor()
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            starMaterial.color = Color.Lerp(originalColor, brightColor, t / duration);
            yield return null;
        }
    }
}
