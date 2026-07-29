using UnityEngine;

/// <summary>
/// Efe ambarinin temiz pickup zone'u. Mert'in upgrade/cargo davranislarini kullanmaz.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class EfeWoodStoragePickupZone : MonoBehaviour
{
    [SerializeField] private EfeWoodStorageBridge storageBridge;
    [SerializeField] private EfeWoodStorageVisual storageVisual;

    private EfeOdunTasima _carrierInside;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        EfeOdunTasima carrier = other.GetComponentInParent<EfeOdunTasima>();
        if (carrier == null || _carrierInside != null || storageBridge == null)
        {
            return;
        }

        _carrierInside = carrier;
        Vector3 pickupPosition = storageVisual != null
            ? storageVisual.PickupWorldPosition
            : transform.position + Vector3.up * 0.5f;
        storageBridge.TakeStoredWood(carrier, pickupPosition);
    }

    private void OnTriggerExit(Collider other)
    {
        EfeOdunTasima carrier = other.GetComponentInParent<EfeOdunTasima>();
        if (carrier != null && carrier == _carrierInside)
        {
            _carrierInside = null;
        }
    }

    private void OnDisable()
    {
        _carrierInside = null;
    }
}
