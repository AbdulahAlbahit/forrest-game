using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckRouteController : MonoBehaviour
{
    [Header("--- ROTA VE EFEKTLER ---")]
    public Transform routeContainer;
    public ParticleSystem exhaustVFX;

    [Header("--- DURAK NOKTALARI ---")]
    public int workshopIndex = 10;
    public int marketIndex = 22;

    [Header("--- KARGO VE YOLA ÇIKMA KURALLARI ---")]
    public int requiredWoodToDepart = 30;
    public int currentLoadedWood = 0;
    public bool isAtHomePosition = true;

    [Header("--- HIZ VE BEKLEME AYARLARI ---")]
    public float maxSpeed = 12f;
    public float acceleration = 4f;
    public float rotationSpeed = 2.5f; 
    public float turnRadius = 1.8f;
    public float stopWaitTime = 3f; 
    public float homeWaitTime = 5f; 

    [Header("--- TEKERLEKLER ---")]
    public List<Transform> wheels = new List<Transform>();

    private List<Transform> waypoints = new List<Transform>();
    private float currentSpeed = 0f;

    void Start()
    {
        if (routeContainer == null)
        {
            GameObject foundRoute = GameObject.Find("Truck_Route");
            if (foundRoute == null) foundRoute = GameObject.Find("--- TRUCK_ROUTE ---");
            if (foundRoute != null) routeContainer = foundRoute.transform;
        }

        if (routeContainer != null)
        {
            foreach (Transform child in routeContainer) waypoints.Add(child);
        }

        if (wheels.Count == 0) FindAllWheelsRecursively(transform);

        if (exhaustVFX == null)
        {
            Transform foundDuman = transform.Find("VFX_Duman");
            if (foundDuman != null) exhaustVFX = foundDuman.GetComponent<ParticleSystem>();
            else exhaustVFX = GetComponentInChildren<ParticleSystem>(true);
        }

        if (exhaustVFX != null)
        {
            exhaustVFX.gameObject.SetActive(true);
            var emission = exhaustVFX.emission;
            emission.enabled = true; // VANA ASLA KAPANMAZ!
            if (!exhaustVFX.isPlaying) exhaustVFX.Play();
            
            Debug.Log($"<color=green>[DUMAN SÜREKLİ AÇIK + HIZA DUYARLI] : {exhaustVFX.name} bağlandı.</color>");
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

    void Update()
    {
        // 1. TEKERLEKLERİN DÖNÜŞÜ
        if (currentSpeed > 0.05f && wheels.Count > 0)
        {
            float wheelRotationStep = currentSpeed * 200f * Time.deltaTime;
            foreach (Transform wheel in wheels)
            {
                if (wheel != null) wheel.Rotate(Vector3.right, wheelRotationStep, Space.Self);
            }
        }

        // 2. DUMAN - SÜREKLİ AÇIK, SABİT AYAR
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

    private IEnumerator RunRouteLoop()
    {
        while (true)
        {
            isAtHomePosition = true;
            Debug.Log($"<color=cyan>[KAMYONET EVDE] Kargo Bekleniyor: ({currentLoadedWood}/{requiredWoodToDepart} Odun)</color>");

            while (currentLoadedWood < requiredWoodToDepart)
            {
                yield return null;
            }

            isAtHomePosition = false;
            Debug.Log($"<color=green>[KAMYONET YOLA ÇIKTI] {currentLoadedWood}/{requiredWoodToDepart} Odun yüklendi!</color>");

            yield return StartCoroutine(DriveToPoints(1, workshopIndex, true));
            yield return StartCoroutine(StopAndWait(stopWaitTime));

            yield return StartCoroutine(DriveToPoints(workshopIndex + 1, marketIndex, true));
            yield return StartCoroutine(StopAndWait(stopWaitTime));

            yield return StartCoroutine(DriveToPointsReverse(marketIndex - 1, 0, true));

            yield return StartCoroutine(TurnToFaceFirstPoint());
            yield return StartCoroutine(StopAndWait(homeWaitTime));

            currentLoadedWood = 0;
        }
    }

    private IEnumerator DriveToPoints(int startIdx, int endIdx, bool stopAtEnd)
    {
        for (int i = startIdx; i <= endIdx && i < waypoints.Count; i++)
        {
            float threshold = (i == endIdx && stopAtEnd) ? 0.3f : turnRadius;
            yield return StartCoroutine(MoveToTarget(waypoints[i].position, threshold));
        }
    }

    private IEnumerator DriveToPointsReverse(int startIdx, int endIdx, bool stopAtEnd)
    {
        for (int i = startIdx; i >= endIdx && i >= 0; i--)
        {
            float threshold = (i == endIdx && stopAtEnd) ? 0.3f : turnRadius;
            yield return StartCoroutine(MoveToTarget(waypoints[i].position, threshold));
        }
    }

    private IEnumerator MoveToTarget(Vector3 targetPosition, float reachThreshold)
    {
        while (Vector3.Distance(transform.position, targetPosition) > reachThreshold)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);

            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator TurnToFaceFirstPoint()
    {
        if (waypoints.Count > 1)
        {
            Vector3 forwardDir = (waypoints[1].position - transform.position).normalized;
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
        while (currentSpeed > 0f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, (acceleration * 2) * Time.deltaTime);
            yield return null;
        }
        yield return new WaitForSeconds(waitSeconds);
    }
}
