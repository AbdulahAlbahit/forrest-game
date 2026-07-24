using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ForrestGame.Utils;

public class TruckRouteController : MonoBehaviour
{
    [Header("Route & VFX")]
    public Transform routeContainer;
    public ParticleSystem exhaustVFX;

    [Header("Waypoints")]
    public int workshopIndex = 10;
    public int marketIndex = 22;

    [Header("Cargo Settings")]
    public int requiredWoodToDepart = 30;
    public int currentLoadedWood = 0;
    public bool isAtHomePosition = true;
    public bool isWorkshopProcessDone = false;

    public void OnWorkshopProcessComplete()
    {
        isWorkshopProcessDone = true;
    }

    [Header("Movement Settings")]
    public float maxSpeed = 12f;
    public float acceleration = 4f;
    public float rotationSpeed = 2.5f; 
    public float turnRadius = 1.8f;
    public float stopWaitTime = 3f; 
    public float homeWaitTime = 5f; 

    // SFX Actions for AudioManager
    public static event Action OnTruckDepartedHome;
    public static event Action OnTruckArrival;
    public static event Action OnTruckDeparture;
    public static event Action OnMarketSale;
    public static event Action OnTruckDepartedMarket;
    public static event Action OnTruckArrivedHome;

    /// <summary>
    /// Atolyeye varan kamyonu harici bir secim sistemi devralmak isterse true dondurur.
    /// Hicbir dinleyici yoksa veya tum dinleyiciler false dondururse eski otomatik
    /// uretim akisi degismeden calisir.
    /// </summary>
    public delegate bool WorkshopSelectionRequestHandler(TruckRouteController truck);
    public static event WorkshopSelectionRequestHandler OnWorkshopSelectionRequested;

    [Header("Wheels")]
    public List<Transform> wheels = new List<Transform>();

    private List<Transform> _waypoints = new List<Transform>();
    private float _currentSpeed = 0f;

    private void Start()
    {
        if (routeContainer == null)
        {
            GameObject foundRoute = GameObject.Find("Truck_Route");
            if (foundRoute == null) foundRoute = GameObject.Find("--- TRUCK_ROUTE ---");
            if (foundRoute != null) routeContainer = foundRoute.transform;
        }

        if (routeContainer != null)
        {
            foreach (Transform child in routeContainer) _waypoints.Add(child);
        }

        if (wheels.Count == 0) FindAllWheelsRecursively(transform);

        if (exhaustVFX == null)
        {
            Transform foundSmoke = transform.Find("VFX_Duman");
            if (foundSmoke != null) exhaustVFX = foundSmoke.GetComponent<ParticleSystem>();
            else exhaustVFX = GetComponentInChildren<ParticleSystem>(true);
        }

        if (exhaustVFX != null)
        {
            exhaustVFX.gameObject.SetActive(true);
            var emission = exhaustVFX.emission;
            emission.enabled = true; 
            if (!exhaustVFX.isPlaying) exhaustVFX.Play();
            
            GameLogger.Log($"[TruckRoute] Exhaust VFX connected: {exhaustVFX.name}", this);
        }

        StartCoroutine(RunRouteLoop());
    }

    private void FindAllWheelsRecursively(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains("wheel")) wheels.Add(child);
            FindAllWheelsRecursively(child);
        }
    }

    private void Update()
    {
        if (_currentSpeed > 0.05f && wheels.Count > 0)
        {
            float wheelRotationStep = _currentSpeed * 200f * Time.deltaTime;
            foreach (Transform wheel in wheels)
            {
                if (wheel != null) wheel.Rotate(Vector3.right, wheelRotationStep, Space.Self);
            }
        }

        if (exhaustVFX != null)
        {
            var emission = exhaustVFX.emission;
            if (!emission.enabled) emission.enabled = true;
        }
    }

    public bool AddWoodToCargo(int amount)
    {
        if (!isAtHomePosition || currentLoadedWood >= requiredWoodToDepart) return false;
        currentLoadedWood += amount;
        return true;
    }

    private bool TryHandOffToWorkshopSelection()
    {
        WorkshopSelectionRequestHandler handlers = OnWorkshopSelectionRequested;
        if (handlers == null) return false;

        foreach (WorkshopSelectionRequestHandler handler in handlers.GetInvocationList())
        {
            try
            {
                if (handler(this)) return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        return false;
    }

    private IEnumerator RunRouteLoop()
    {
        while (true)
        {
            isAtHomePosition = true;
            GameLogger.Log($"[TruckRoute] Truck at home. Waiting for cargo: ({currentLoadedWood}/{requiredWoodToDepart})", this);

            while (currentLoadedWood < requiredWoodToDepart)
            {
                yield return null;
            }

            isAtHomePosition = false;
            GameLogger.Log($"[TruckRoute] Cargo full ({currentLoadedWood}/{requiredWoodToDepart}). Departing to workshop!", this);
            OnTruckDepartedHome?.Invoke();

            yield return StartCoroutine(DriveToPoints(1, workshopIndex, true));
            
            OnTruckArrival?.Invoke();

            isWorkshopProcessDone = false;
            WorkshopProductionManager workshop = FindObjectOfType<WorkshopProductionManager>();
            bool selectionSystemClaimed = TryHandOffToWorkshopSelection();

            if (selectionSystemClaimed)
            {
                GameLogger.Log("[TruckRoute] Workshop selection system took control. Waiting for player choice and production...", this);

                while (!isWorkshopProcessDone)
                {
                    yield return null;
                }

                GameLogger.Log("[TruckRoute] Selected package loaded! Departing to market.", this);
                OnTruckDeparture?.Invoke();
            }
            else if (workshop != null && workshop.StartProductionCycle(this.gameObject))
            {
                GameLogger.Log("[TruckRoute] Arrived at workshop. Waiting for production and packing...", this);

                while (!isWorkshopProcessDone)
                {
                    yield return null;
                }
                GameLogger.Log("[TruckRoute] Boxes loaded! Departing to market.", this);
                OnTruckDeparture?.Invoke();
            }
            else
            {
                yield return StartCoroutine(StopAndWait(stopWaitTime));
            }

            yield return StartCoroutine(DriveToPoints(workshopIndex + 1, marketIndex, true));
            
            OnMarketSale?.Invoke();
            GameLogger.Log("[TruckRoute] Arrived at market. Boxes sold, cargo emptied.", this);
            
            Transform cargoBoxes = transform.Find("Cargo_Boxes");
            if (cargoBoxes != null) cargoBoxes.gameObject.SetActive(false);

            if (workshop != null && workshop.largeBoxes != null) workshop.largeBoxes.SetActive(false);

            yield return StartCoroutine(StopAndWait(stopWaitTime));

            OnTruckDepartedMarket?.Invoke();
            yield return StartCoroutine(DriveToPointsReverse(marketIndex - 1, 0, true));

            yield return StartCoroutine(TurnToFaceFirstPoint());
            OnTruckArrivedHome?.Invoke();
            yield return StartCoroutine(StopAndWait(homeWaitTime));

            currentLoadedWood = 0;
        }
    }

    private IEnumerator DriveToPoints(int startIdx, int endIdx, bool stopAtEnd)
    {
        for (int i = startIdx; i <= endIdx && i < _waypoints.Count; i++)
        {
            float threshold = (i == endIdx && stopAtEnd) ? 0.3f : turnRadius;
            yield return StartCoroutine(MoveToTarget(_waypoints[i].position, threshold));
        }
    }

    private IEnumerator DriveToPointsReverse(int startIdx, int endIdx, bool stopAtEnd)
    {
        for (int i = startIdx; i >= endIdx && i >= 0; i--)
        {
            float threshold = (i == endIdx && stopAtEnd) ? 0.3f : turnRadius;
            yield return StartCoroutine(MoveToTarget(_waypoints[i].position, threshold));
        }
    }

    private IEnumerator MoveToTarget(Vector3 targetPosition, float reachThreshold)
    {
        while (Vector3.Distance(transform.position, targetPosition) > reachThreshold)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, maxSpeed, acceleration * Time.deltaTime);

            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _currentSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator TurnToFaceFirstPoint()
    {
        if (_waypoints.Count > 1)
        {
            Vector3 forwardDir = (_waypoints[1].position - transform.position).normalized;
            forwardDir.y = 0;
            if (forwardDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(forwardDir);
                while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, (rotationSpeed * 0.8f) * Time.deltaTime);
                    yield return null;
                }
                transform.rotation = targetRotation;
            }
        }
    }

    private IEnumerator StopAndWait(float waitSeconds)
    {
        while (_currentSpeed > 0f)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, (acceleration * 2) * Time.deltaTime);
            yield return null;
        }
        yield return new WaitForSeconds(waitSeconds);
    }
}
