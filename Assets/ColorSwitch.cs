using UnityEngine;

public class ColorSwitch : MonoBehaviour
{
    // [Header("References")]
    // public GameObject ballPrefab;     // 指向含 Rigidbody + SphereCollider + Bouncy material 的 prefab
    // public Transform muzzle;          // 發射起點（可用此物件自身 Transform）

    // [Header("Shoot Settings")]
    // public float muzzleSpeed = 6.0f;  // 初速（m/s）
    // public float spawnOffset = 0.05f; // 從發射點往前偏一點，避免與手/牆重疊

    // void Reset()
    // {
    //     muzzle = transform;
    // }
    public QuarterRing quarterRingR;
    public QuarterRing quarterRingL;

    void Update()
    {
        // 右手扳機：按下就發射
        bool buttonDown = OVRInput.GetDown(OVRInput.Button.One);
        if (buttonDown)
        {
            Debug.Log("Right down");
            if (quarterRingR.color == GameColor.Red)
                quarterRingR.SetColor(GameColor.Green);
            else if (quarterRingR.color == GameColor.Green)
                quarterRingR.SetColor(GameColor.Blue);
            else if (quarterRingR.color == GameColor.Blue)
                quarterRingR.SetColor(GameColor.Red);
        }

        // bool triggerDown2 = OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.LTouch);
        // if (triggerDown2)
        // {
        //     Debug.Log("Left down");
        //     if (quarterRingL.color == GameColor.Red)
        //         quarterRingL.SetColor(GameColor.Green);
        //     else if (quarterRingL.color == GameColor.Green)
        //         quarterRingL.SetColor(GameColor.Blue);
        //     else if (quarterRingL.color == GameColor.Blue)
        //         quarterRingL.SetColor(GameColor.Red);
        // }
    }
}

