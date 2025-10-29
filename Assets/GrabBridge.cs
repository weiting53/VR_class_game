using Unity.Netcode;
using Oculus.Interaction;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Side = NetworkRingSqueezer.Side;

[RequireComponent(typeof(Grabbable))]
public class GrabBridge : MonoBehaviour
{
    [Header("Targets")]
    public NetworkRingSqueezer target; // ← 核心：用手距判斷的腳本
    public Transform gripA; // 用來判斷靠近哪側
    public Transform gripB;

    [Header("Debug")]
    public bool debugLogs = true;

    Grabbable _grabbable;

    // 一個 interactor Transform 對應到一個側別 & 一條回報協程
    readonly Dictionary<Transform, Side> _interactorToSide = new();
    readonly Dictionary<Side, Coroutine> _reportLoopBySide = new();
    readonly Dictionary<Side, Transform> _handTfBySide = new();

    void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _grabbable.WhenPointerEventRaised += OnPointer;
    }

    void OnDestroy()
    {
        if (_grabbable != null) _grabbable.WhenPointerEventRaised -= OnPointer;
        // 停掉所有協程
        foreach (var kv in _reportLoopBySide) if (kv.Value != null) StopCoroutine(kv.Value);
        _reportLoopBySide.Clear();
        _interactorToSide.Clear();
        _handTfBySide.Clear();
    }

    void OnPointer(PointerEvent evt)
    {
        if (NetworkManager.Singleton == null || target == null) return;

        // 從事件中抓出「這次抓放的是哪個 interactor（手）」的 Transform
        var interactorTf = GetInteractorTransform(evt);
        if (interactorTf == null)
        {
            if (debugLogs) Debug.LogWarning("[Bridge] 無法取得 interactor Transform");
            return;
        }

        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (evt.Type == PointerEventType.Select)
        {
            // 依 interactor 當下位置分配側別（A 或 B）
            var side = NearestSide(interactorTf.position);
            _interactorToSide[interactorTf] = side;

            if (debugLogs) Debug.Log($"[Bridge] Select by {localId}, side={side}, interactor={interactorTf.name}");

            // 通知「該側」被握住
            target.NotifyGrabbed(localId, side);

            // 啟動該側的回報協程（若已存在，先停掉再啟動）
            if (_reportLoopBySide.TryGetValue(side, out var co) && co != null)
                StopCoroutine(co);

            _handTfBySide[side] = interactorTf;
            _reportLoopBySide[side] = StartCoroutine(ReportLoop(side));
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            // 找回當初記錄的側別（如果拿不到，就保守地兩側都釋放）
            if (_interactorToSide.TryGetValue(interactorTf, out var side))
            {
                if (debugLogs) Debug.Log($"[Bridge] Unselect by {localId}, side={side}, interactor={interactorTf.name}");

                target.NotifyReleased(localId, side);

                if (_reportLoopBySide.TryGetValue(side, out var co) && co != null)
                    StopCoroutine(co);

                _reportLoopBySide[side] = null;
                _handTfBySide.Remove(side);
                _interactorToSide.Remove(interactorTf);
            }
            else
            {
                if (debugLogs) Debug.Log($"[Bridge] Unselect(unknown side) by {localId}, interactor={interactorTf.name}");
                // 假如沒對上，就兩側都嘗試釋放且停回報
                target.NotifyReleased(localId, Side.A);
                target.NotifyReleased(localId, Side.B);
                foreach (var kv in _reportLoopBySide)
                    if (kv.Value != null) StopCoroutine(kv.Value);
                _reportLoopBySide.Clear();
                _handTfBySide.Clear();
                _interactorToSide.Clear();
            }
        }
    }

    IEnumerator ReportLoop(Side side)
    {
        var wait = new WaitForSeconds(1f / 30f); // 30Hz 回報
        while (true)
        {
            if (_handTfBySide.TryGetValue(side, out var hand) && hand != null && target != null)
                target.ReportHandPosition(side, hand.position);
            yield return wait;
        }
    }

    // 從 PointerEvent 盡可能取出 interactor 的 Transform
    Transform GetInteractorTransform(PointerEvent evt)
    {
        // evt.Data 常常是某個 MonoBehaviour（Interactor/Pointer）
        if (evt.Data is Component c && c != null) return c.transform;
        if (evt.Data is MonoBehaviour mb && mb != null) return mb.transform;

        // 取不到就退而求其次，用物件本身（誤判風險高）
        return null;
    }

    Side NearestSide(Vector3 p)
    {
        if (!gripA || !gripB) return Side.A;
        return (Vector3.Distance(p, gripA.position) <= Vector3.Distance(p, gripB.position))
            ? Side.A : Side.B;
    }
}
