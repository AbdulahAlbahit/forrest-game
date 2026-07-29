using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerWoodStock miktarini ev yanindaki Efe ambarinda odun yiginina donusturur.
/// Yalnizca gorseldir; asil stok EfeWoodStorageBridge ve PlayerData tarafindadir.
/// </summary>
[DisallowMultipleComponent]
public sealed class EfeWoodStorageVisual : MonoBehaviour
{
    [SerializeField] private Transform logStackAnchor;
    [Range(1, 30)] [SerializeField] private int visualUpperLimit = 18;
    [Min(0.05f)] [SerializeField] private float logScale = 0.7f;

    private readonly List<GameObject> _logs = new List<GameObject>();
    private int _requestedAmount;
    private Coroutine _prepareRoutine;

    public Vector3 PickupWorldPosition => logStackAnchor != null
        ? logStackAnchor.position + Vector3.up * 0.35f
        : transform.position + Vector3.up * 0.5f;

    private void OnEnable()
    {
        if (_prepareRoutine == null)
        {
            _prepareRoutine = StartCoroutine(PrepareWhenReady());
        }
    }

    private void OnDisable()
    {
        if (_prepareRoutine != null)
        {
            StopCoroutine(_prepareRoutine);
            _prepareRoutine = null;
        }
    }

    public void SetStoredAmount(int amount)
    {
        _requestedAmount = Mathf.Max(0, amount);
        if (TryPrepareVisuals())
        {
            RefreshVisibility();
        }
    }

    private IEnumerator PrepareWhenReady()
    {
        while (isActiveAndEnabled && !TryPrepareVisuals())
        {
            yield return new WaitForSecondsRealtime(0.25f);
        }

        _prepareRoutine = null;
        RefreshVisibility();
    }

    private bool TryPrepareVisuals()
    {
        if (_logs.Count > 0)
        {
            return true;
        }

        if (logStackAnchor == null || TreeManager.Instance == null || TreeManager.Instance.woodLogPrefab == null)
        {
            return false;
        }

        for (int i = 0; i < visualUpperLimit; i++)
        {
            GameObject log = Instantiate(TreeManager.Instance.woodLogPrefab, logStackAnchor);
            log.name = $"Efe_AmbarOdunu_{i + 1}";

            Rigidbody body = log.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.useGravity = false;
            }

            foreach (Collider logCollider in log.GetComponentsInChildren<Collider>(true))
            {
                logCollider.enabled = false;
            }

            foreach (MonoBehaviour behaviour in log.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            int column = i % 3;
            int depth = (i / 3) % 2;
            int layer = i / 6;
            log.transform.localPosition = new Vector3(
                (column - 1) * 0.42f,
                0.2f + layer * 0.22f,
                depth == 0 ? -0.18f : 0.18f);
            log.transform.localRotation = Quaternion.Euler(0f, 90f, (i % 2 == 0 ? -3f : 3f));
            log.transform.localScale = Vector3.one * logScale;
            log.SetActive(false);
            _logs.Add(log);
        }

        return true;
    }

    private void RefreshVisibility()
    {
        int visibleCount = Mathf.Min(_requestedAmount, _logs.Count);
        for (int i = 0; i < _logs.Count; i++)
        {
            _logs[i].SetActive(i < visibleCount);
        }
    }

    private void OnValidate()
    {
        visualUpperLimit = Mathf.Clamp(visualUpperLimit, 1, 30);
        logScale = Mathf.Max(0.05f, logScale);
    }
}
