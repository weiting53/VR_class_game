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
    public NetworkRingSqueezer target; // �� �֤ߡG�Τ�Z�P�_���}��
    public Transform gripA; // �ΨӧP�_�a�����
    public Transform gripB;

    [Header("Debug")]
    public bool debugLogs = true;

    Grabbable _grabbable;

    // �@�� interactor Transform ������@�Ӱ��O & �@���^����{
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
        // �����Ҧ���{
        foreach (var kv in _reportLoopBySide) if (kv.Value != null) StopCoroutine(kv.Value);
        _reportLoopBySide.Clear();
        _interactorToSide.Clear();
        _handTfBySide.Clear();
    }

    void OnPointer(PointerEvent evt)
    {
        if (NetworkManager.Singleton == null || target == null) return;

        // �q�ƥ󤤧�X�u�o����񪺬O���� interactor�]��^�v�� Transform
        var interactorTf = GetInteractorTransform(evt);
        if (interactorTf == null)
        {
            if (debugLogs) Debug.LogWarning("[Bridge] �L�k���o interactor Transform");
            return;
        }

        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (evt.Type == PointerEventType.Select)
        {
            // �� interactor ��U��m���t���O�]A �� B�^
            var side = NearestSide(interactorTf.position);
            _interactorToSide[interactorTf] = side;

            if (debugLogs) Debug.Log($"[Bridge] Select by {localId}, side={side}, interactor={interactorTf.name}");

            // �q���u�Ӱ��v�Q����
            target.NotifyGrabbed(localId, side);

            // �ҰʸӰ����^����{�]�Y�w�s�b�A�������A�Ұʡ^
            if (_reportLoopBySide.TryGetValue(side, out var co) && co != null)
                StopCoroutine(co);

            _handTfBySide[side] = interactorTf;
            _reportLoopBySide[side] = StartCoroutine(ReportLoop(side));
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            // ��^���O�������O�]�p�G������A�N�O�u�a�ⰼ������^
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
                // ���p�S��W�A�N�ⰼ����������B���^��
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
        var wait = new WaitForSeconds(1f / 30f); // 30Hz �^��
        while (true)
        {
            if (_handTfBySide.TryGetValue(side, out var hand) && hand != null && target != null)
                target.ReportHandPosition(side, hand.position);
            yield return wait;
        }
    }

    // �q PointerEvent �ɥi����X interactor �� Transform
    Transform GetInteractorTransform(PointerEvent evt)
    {
        // evt.Data �`�`�O�Y�� MonoBehaviour�]Interactor/Pointer�^
        if (evt.Data is Component c && c != null) return c.transform;
        if (evt.Data is MonoBehaviour mb && mb != null) return mb.transform;

        // ������N�h�ӨD�䦸�A�Ϊ��󥻨��]�~�P���I���^
        return null;
    }

    Side NearestSide(Vector3 p)
    {
        if (!gripA || !gripB) return Side.A;
        return (Vector3.Distance(p, gripA.position) <= Vector3.Distance(p, gripB.position))
            ? Side.A : Side.B;
    }
}
