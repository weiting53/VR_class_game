using UnityEngine;

public class FairyController : MonoBehaviour
{
    public ParticleSystem vanishEffect;
    public StarController star;
    private bool isVanishing = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ring") && !isVanishing)
        {
            Debug.Log("touched");
            isVanishing = true;
            StartCoroutine(Vanish());
        }
    }

    private System.Collections.IEnumerator Vanish()
    {
        if (vanishEffect != null)
            vanishEffect.Play();

        // 關閉渲染跟碰撞
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (star != null)
            star.MakeBright();

        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
}
