using UnityEngine;

public class ControllerDistance : MonoBehaviour
{
    public Transform leftHand;   // 指到 LeftHandAnchor
    public Transform rightHand;  // 指到 RightHandAnchor
    public float triggerThreshold = 0.15f; // 小於這個距離就觸發事件(公尺)
    public bool drawLine = true;

    LineRenderer lr;

    void Awake()
    {
        if (drawLine)
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.005f;
            lr.endWidth = 0.005f;
            lr.useWorldSpace = true;
        }
    }

    void Update()
    {
        if (leftHand == null || rightHand == null) return;

        float d = Vector3.Distance(leftHand.position, rightHand.position);
        // 需要就印出或顯示在 UI
        // Debug.Log($"手把距離: {d:F3} m");

        if (drawLine)
        {
            lr.SetPosition(0, leftHand.position);
            lr.SetPosition(1, rightHand.position);
        }

        if (d <= triggerThreshold)
        {
            // TODO: 你要做的事（例如亮光/播放效果/抓物）
            // Debug.Log("兩手距離達到門檻，觸發事件");
        }
    }
}
