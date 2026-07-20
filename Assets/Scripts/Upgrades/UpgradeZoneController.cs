using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UpgradeZoneController : MonoBehaviour
{
    [Header("--- UPGRADE BİLGİLERİ ---")]
    public string upgradeTitle = "Masa Yükseltme";
    public float targetCost = 500f;
    public float transferRatePerSecond = 250f;

    [Header("--- GÖRSEL GERİ BİLDİRİM ---")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color completedColor = new Color(0.1f, 0.9f, 0.2f, 1f);
    
    [Tooltip("Dolan çember efekti için World Space Canvas içindeki Filled Image'ı sürükleyin.")]
    public Image fillImage;

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

        // Eğer Canvas resmi kullanıyorsak, başlangıçta sıfırla ve rengini saf yeşil yap
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.color = completedColor; 
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCompleted) return;
        Debug.Log($"<color=yellow>---> [TETİKLENDİ] {other.name} ({other.tag}) ZEMİNE GİRDİ! <---</color>");
    }

    void OnTriggerStay(Collider other)
    {
        if (isCompleted) return;

        if (other.CompareTag("Player") || other.name.ToLower().Contains("player") || other.name.ToLower().Contains("mert") || other.name.ToLower().Contains("fake"))
        {
            currentPaid += transferRatePerSecond * Time.deltaTime;
            float progress = Mathf.Clamp01(currentPaid / targetCost);

            if (fillImage != null)
            {
                // UI Canvas Resmi kullanılıyorsa: Renk karıştırma (Lerp) YAPMA, pasta dilimi gibi doldur!
                fillImage.fillAmount = progress;
            }
            else
            {
                // Eski 3D Zemin kullanılıyorsa: Renk geçişi yap
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
        
        if (fillImage != null) fillImage.fillAmount = 1f;
        UpdatePadColor(completedColor);

        Debug.Log($"<color=green>---> [UPGRADE TAMAMLANDI] : {upgradeTitle} başarıyla açıldı! <---</color>");
        OnUpgradeComplete?.Invoke();

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}
