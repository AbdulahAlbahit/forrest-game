using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UpgradeZoneController : MonoBehaviour
{
    [Header("--- UPGRADE BİLGİLERİ ---")]
    public string upgradeTitle = "Masa Yükseltme";
    public float targetCost = 500f;
    public float transferRatePerSecond = 250f;

    [Header("--- GÖRSEL GERİ BİLDİRİM ---")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color completedColor = new Color(0.1f, 0.9f, 0.2f, 1f);

    [Header("--- YASİR İÇİN EVENT PRİZİ ---")]
    public UnityEvent OnUpgradeComplete;

    [Header("--- ANLIK DURUM ---")]
    public float currentPaid = 0f;
    public bool isCompleted = false;

    private MeshRenderer padRenderer;
    private Material padMaterialInstance;

    void Start()
    {
        padRenderer = GetComponent<MeshRenderer>();
        if (padRenderer != null)
        {
            padMaterialInstance = padRenderer.material;
            UpdatePadColor(emptyColor);
        }
    }

    // Karakter zemine ilk değdiği an Console'a kocaman mesaj at!
    void OnTriggerEnter(Collider other)
    {
        if (isCompleted) return;
        Debug.Log($"<color=yellow>---> [TETİKLENDİ] {other.name} ({other.tag}) ZEMİNE GİRDİ! <---</color>");
    }

    void OnTriggerStay(Collider other)
    {
        if (isCompleted) return;

        // Player etiketli veya adı içinde player/mert/fake geçen herhangi biri
        if (other.CompareTag("Player") || other.name.ToLower().Contains("player") || other.name.ToLower().Contains("mert") || other.name.ToLower().Contains("fake"))
        {
            currentPaid += transferRatePerSecond * Time.deltaTime;

            float progress = Mathf.Clamp01(currentPaid / targetCost);
            Color lerpedColor = Color.Lerp(emptyColor, completedColor, progress);
            UpdatePadColor(lerpedColor);

            if (currentPaid >= targetCost)
            {
                currentPaid = targetCost;
                CompleteUpgrade();
            }
        }
    }

    // URP ve Normal Shadera aynı anda rengi zorla uygula
    private void UpdatePadColor(Color c)
    {
        if (padMaterialInstance != null)
        {
            padMaterialInstance.color = c;
            if (padMaterialInstance.HasProperty("_BaseColor"))
            {
                padMaterialInstance.SetColor("_BaseColor", c);
            }
        }
    }

    private void CompleteUpgrade()
    {
        isCompleted = true;
        UpdatePadColor(completedColor);

        Debug.Log($"<color=green>---> [UPGRADE TAMAMLANDI] : {upgradeTitle} başarıyla açıldı! <---</color>");
        OnUpgradeComplete?.Invoke();

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}
