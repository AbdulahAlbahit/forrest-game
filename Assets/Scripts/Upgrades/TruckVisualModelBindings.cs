using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kamyon model prefabinin yalnizca gorsel baglantilarini tarif eder.
/// Rota, yuk ve ekonomi mantigi TruckRouteController kokunde kalir.
/// </summary>
[DisallowMultipleComponent]
public class TruckVisualModelBindings : MonoBehaviour
{
    [SerializeField] private List<Transform> wheels = new List<Transform>();
    [SerializeField] private ParticleSystem exhaustVFX;

    public IReadOnlyList<Transform> Wheels => wheels;
    public ParticleSystem ExhaustVFX => exhaustVFX;
}
