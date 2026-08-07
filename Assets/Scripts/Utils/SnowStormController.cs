using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SnowStormController : MonoBehaviour
{
    [Header("Emission Fluctuation")]
    [SerializeField] private float minEmission = 10f;
    [SerializeField] private float maxEmission = 40f;
    [SerializeField] private float noiseSpeed = 0.15f;

    [Header("Wind Gust (Temporary Speed Boost)")]
    [SerializeField] private float normalSpeed = 1f;
    [SerializeField] private float gustSpeed = 3f;
    [SerializeField] private float gustDuration = 2f;
    [SerializeField] private float gustIntervalMin = 8f;
    [SerializeField] private float gustIntervalMax = 20f;

    private ParticleSystem snowParticles;
    private float noiseOffset;
    private float nextGustTime;
    private bool gustActive;
    private float intensityScale = 1f;

    public void SetIntensityScale(float scale)
    {
        intensityScale = Mathf.Clamp01(scale);
    }

    private void Awake()
    {
        snowParticles = GetComponent<ParticleSystem>();
        noiseOffset = Random.Range(0f, 1000f);
    }

    private void OnEnable()
    {
        nextGustTime = Time.time + Random.Range(gustIntervalMin, gustIntervalMax);
        gustActive = false;

        ParticleSystem.MainModule mainModule = snowParticles.main;
        mainModule.startSpeed = normalSpeed;
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * noiseSpeed, noiseOffset);
        float targetEmission = Mathf.Lerp(minEmission, maxEmission, noise) * intensityScale;

        ParticleSystem.EmissionModule emissionModule = snowParticles.emission;
        emissionModule.rateOverTime = targetEmission;

        if (!gustActive && Time.time >= nextGustTime)
        {
            StartCoroutine(ApplyWindGust());
        }
    }

    private IEnumerator ApplyWindGust()
    {
        gustActive = true;
        ParticleSystem.MainModule mainModule = snowParticles.main;

        float elapsed = 0f;
        while (elapsed < gustDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Sin(Mathf.Clamp01(elapsed / gustDuration) * Mathf.PI);
            mainModule.startSpeed = Mathf.Lerp(normalSpeed, gustSpeed, progress);
            yield return null;
        }

        mainModule.startSpeed = normalSpeed;
        gustActive = false;
        nextGustTime = Time.time + Random.Range(gustIntervalMin, gustIntervalMax);
    }
}
