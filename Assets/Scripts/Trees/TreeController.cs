using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using ForrestGame.Utils;

public class TreeController : MonoBehaviour
{
    [Header("Tree Health")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Tree Visuals")]
    public GameObject healthyTreeVisual;
    public GameObject stumpPrefab;
    public GameObject fallenTreePrefab;

    [Header("Effect Settings")]
    public float fallDuration = 0.8f;
    public float disappearDelay = 4f;
    public float respawnTime = 0f;

    [Header("Unity Events")]
    public UnityEvent OnTreeHit;
    public UnityEvent OnTreeFelled;

    // SFX Actions for AudioManager
    public static event Action OnTreeHitAction;
    public static event Action OnTreeFelledAction;

    private bool _isFelled = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeHit(int damage = 1)
    {
        if (_isFelled) return;

        currentHealth -= damage;
        OnTreeHit?.Invoke();
        OnTreeHitAction?.Invoke();

        StartCoroutine(ShakeTree());

        GameLogger.Log($"[Tree] {gameObject.name} hit. Health: {currentHealth}/{maxHealth}", this);

        if (currentHealth <= 0)
        {
            StartCoroutine(FellTree());
        }
    }

    private IEnumerator ShakeTree()
    {
        Vector3 originalRotation = transform.eulerAngles;
        float elapsed = 0f;
        float shakeDuration = 0.25f;
        float shakeMagnitude = 3f;

        while (elapsed < shakeDuration)
        {
            float shakeAngle = Mathf.Sin(elapsed * 40f) * shakeMagnitude * (1f - elapsed / shakeDuration);
            transform.eulerAngles = new Vector3(originalRotation.x, originalRotation.y, originalRotation.z + shakeAngle);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.eulerAngles = originalRotation;
    }

    private IEnumerator FellTree()
    {
        _isFelled = true;
        GameLogger.Log($"[Tree] {gameObject.name} fell down!", this);

        if (healthyTreeVisual != null)
        {
            healthyTreeVisual.SetActive(false);
        }

        if (stumpPrefab != null)
        {
            Instantiate(stumpPrefab, transform.position, transform.rotation);
        }

        if (fallenTreePrefab != null)
        {
            Vector3 fallPosition = transform.position + transform.right * 1.5f;
            GameObject fallen = Instantiate(fallenTreePrefab, transform.position + Vector3.up * 3f, transform.rotation);

            float elapsed = 0f;
            Quaternion startRot = fallen.transform.rotation;
            Quaternion endRot = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 90f);
            Vector3 startPos = fallen.transform.position;

            while (elapsed < fallDuration)
            {
                float t = elapsed / fallDuration;
                float easeT = 1f - Mathf.Cos(t * Mathf.PI * 0.5f); 

                fallen.transform.rotation = Quaternion.Lerp(startRot, endRot, easeT);
                fallen.transform.position = Vector3.Lerp(startPos, fallPosition, easeT);
                elapsed += Time.deltaTime;
                yield return null;
            }

            fallen.transform.rotation = endRot;
            fallen.transform.position = fallPosition;
        }

        OnTreeFelled?.Invoke();
        OnTreeFelledAction?.Invoke();

        yield return new WaitForSeconds(disappearDelay);
        Destroy(gameObject);

        if (respawnTime > 0f)
        {
            GameLogger.Log($"[Tree] Respawning in {respawnTime} seconds...", this);
        }
    }
}
