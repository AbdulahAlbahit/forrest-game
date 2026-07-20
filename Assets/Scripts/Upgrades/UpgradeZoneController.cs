using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ForrestGame.Utils;

public class UpgradeZoneController : MonoBehaviour
{
    [Header("Upgrade Info")]
    public string upgradeTitle = "Table Upgrade";
    public float targetCost = 500f;
    public float transferRatePerSecond = 250f;

    [Header("Visual Feedback")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color completedColor = new Color(0.1f, 0.9f, 0.2f, 1f);
    public Image fillImage;

    [Header("Status")]
    public float currentPaid = 0f;
    public bool isCompleted = false;

    [Header("Unity Events")]
    public UnityEvent OnUpgradeComplete;

    // SFX Action for AudioManager
    public static event Action OnUpgradeCompletedAction;

    private MeshRenderer _padRenderer;
    private Material _padMaterialInstance;

    private void Start()
    {
        _padRenderer = GetComponent<MeshRenderer>();
        if (_padRenderer != null)
        {
            _padMaterialInstance = _padRenderer.material;
            UpdatePadColor(emptyColor);
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.color = completedColor; 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCompleted) return;
        GameLogger.Log($"[UpgradeZone] {other.name} entered the upgrade zone.", this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCompleted) return;

        if (other.CompareTag("Player") || other.name.ToLower().Contains("player") || other.name.ToLower().Contains("mert") || other.name.ToLower().Contains("fake"))
        {
            currentPaid += transferRatePerSecond * Time.deltaTime;
            float progress = Mathf.Clamp01(currentPaid / targetCost);

            if (fillImage != null)
            {
                fillImage.fillAmount = progress;
            }
            else
            {
                Color lerpedColor = Color.Lerp(emptyColor, completedColor, progress);
                UpdatePadColor(lerpedColor);
            }

            if (currentPaid >= targetCost)
            {
                currentPaid = targetCost;
                CompleteUpgrade();
            }
        }
    }

    private void UpdatePadColor(Color c)
    {
        if (_padMaterialInstance != null)
        {
            _padMaterialInstance.color = c;
            if (_padMaterialInstance.HasProperty("_BaseColor"))
            {
                _padMaterialInstance.SetColor("_BaseColor", c);
            }
        }
    }

    private void CompleteUpgrade()
    {
        isCompleted = true;
        
        if (fillImage != null) fillImage.fillAmount = 1f;
        UpdatePadColor(completedColor);

        GameLogger.Log($"[UpgradeZone] Upgrade Complete: {upgradeTitle} unlocked!", this);
        
        OnUpgradeComplete?.Invoke();
        OnUpgradeCompletedAction?.Invoke();

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}
