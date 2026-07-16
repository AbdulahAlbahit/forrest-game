using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckRouteController : MonoBehaviour
{
    [Header("--- ROTA VE EFEKTLER ---")]
    [Tooltip("Hierarchy'deki Truck_Route klasörünü buraya sürükle (Boş bırakırsan otomatik bulur)")]
    public Transform routeContainer;
    
    [Tooltip("Kamyonetin içindeki VFX_Exhaust (Particle System) objesini buraya sürükle")]
    public ParticleSystem exhaustVFX;

    [Header("--- DURAK NOKTALARI (WAYPOINT İNDEKSLERİ) ---")]
    public int workshopIndex = 10;
    public int marketIndex = 22;

    [Header("--- HIZ VE BEKLEME AYARLARI ---")]
    public float maxSpeed = 12f;
    public float acceleration = 4f;
    public float rotationSpeed = 2.5f; 
    public float turnRadius = 1.8f;
    
    public float stopWaitTime = 3f; 
    public float homeWaitTime = 10f; 

    [Header("--- TEKERLEKLER (OTOMATİK BULUNUR) ---")]
    public List<Transform> wheels = new List<Transform>();

    private List<Transform> waypoints = new List<Transform>();
    private float currentSpeed = 0f;

    void Start()
    {
        // 0. Sahnede "Truck_Route" adındaki klasörü otomatik bul!
        if (routeContainer == null)
        {
            GameObject foundRoute = GameObject.Find("Truck_Route");
            if (foundRoute == null) foundRoute = GameObject.Find("--- TRUCK_ROUTE ---"); // Alternatif isim kontrolü
            if (foundRoute != null) routeContainer = foundRoute.transform;
        }

        // 1. Rota noktalarını topla
        if (routeContainer != null)
        {
            foreach (Transform child in routeContainer)
            {
                waypoints.Add(child);
            }
        }
        else
        {
            Debug.LogError("UYARI: Sahnede 'Truck_Route' adında bir klasör bulunamadı!");
            return;
        }

        // 2. Eğer tekerlekler elle atanmadıysa, alt objelerde "wheel" ismini ara ve otomatik ekle
        if (wheels.Count == 0)
        {
            FindAllWheelsRecursively(transform);
        }

        StartCoroutine(RunRouteLoop());
    }

    private void FindAllWheelsRecursively(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains("wheel"))
            {
                wheels.Add(child);
            }
            FindAllWheelsRecursively(child);
        }
    }

    void Update()
    {
        if (currentSpeed > 0.05f && wheels.Count > 0)
        {
            float wheelRotationStep = currentSpeed * 200f * Time.deltaTime;
            foreach (Transform wheel in wheels)
            {
                if (wheel != null)
                {
                    wheel.Rotate(Vector3.right, wheelRotationStep, Space.Self);
                }
            }
        }
    }

    private IEnumerator RunRouteLoop()
    {
        while (true)
        {
            yield return StartCoroutine(DriveToPoints(1, workshopIndex, true));
            yield return StartCoroutine(StopAndWait(stopWaitTime));

            yield return StartCoroutine(DriveToPoints(workshopIndex + 1, marketIndex, true));
            yield return StartCoroutine(StopAndWait(stopWaitTime));

            yield return StartCoroutine(DriveToPointsReverse(marketIndex - 1, 0, true));

            yield return StartCoroutine(TurnToFaceFirstPoint());
            yield return StartCoroutine(StopAndWait(homeWaitTime));
        }
    }

    private IEnumerator DriveToPoints(int startIdx, int endIdx, bool stopAtEnd)
    {
        SetExhaust(true);
        for (int i = startIdx; i <= endIdx && i < waypoints.Count; i++)
        {
            float threshold = (i == endIdx && stopAtEnd) ? 0.3f : turnRadius;
            yield return StartCoroutine(MoveToTarget(waypoints[i].position, threshold));
        }
    }

    private IEnumerator DriveToPointsReverse(int startIdx, int endIdx, bool stopAtEnd)
    {
        SetExhaust(true);
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

        SetExhaust(false);
        yield return new WaitForSeconds(waitSeconds);
    }

    private void SetExhaust(bool active)
    {
        if (exhaustVFX != null)
        {
            if (active && !exhaustVFX.isPlaying) exhaustVFX.Play();
            else if (!active && exhaustVFX.isPlaying) exhaustVFX.Stop();
        }
    }
}
