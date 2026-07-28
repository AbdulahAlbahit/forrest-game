using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Balta butonuna tek dokunma davranisini bozmadan basili tutarak
/// art arda saldiri destegi ekler. Gercek saldiri araligini EfeBaltaKontrol
/// icindeki mevcut guvenlik suresi belirler.
/// </summary>
[DisallowMultipleComponent]
public sealed class EfeAxeHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private EfeBaltaKontrol baltaKontrol;
    [SerializeField, Min(0.02f)] private float tekrarKontrolAraligi = 0.05f;

    private Coroutine _holdRoutine;
    private int _activePointerId = int.MinValue;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || _activePointerId != int.MinValue)
        {
            return;
        }

        _activePointerId = eventData.pointerId;
        baltaKontrol?.BaltaSalla();
        _holdRoutine = StartCoroutine(HoldAttackRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerId != _activePointerId)
        {
            return;
        }

        StopHolding();
    }

    private IEnumerator HoldAttackRoutine()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(tekrarKontrolAraligi);

        while (_activePointerId != int.MinValue)
        {
            yield return wait;
            baltaKontrol?.BaltaSalla();
        }
    }

    private void StopHolding()
    {
        _activePointerId = int.MinValue;

        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
        }
    }

    private void OnDisable()
    {
        StopHolding();
    }

    private void OnValidate()
    {
        tekrarKontrolAraligi = Mathf.Max(0.02f, tekrarKontrolAraligi);
    }
}
