using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Atölye panelinin alt tarafındaki üretim sırasındaki (Queue) tek bir kutucuğu temsil eder.
/// Şuan üretilen aktif ürün ise üzerindeki canlı % doluluk barını ve metnini günceller.
/// </summary>
public class QueueItemUI : MonoBehaviour
{
    [Header("--- UI Elemanları ---")]
    [Tooltip("Üretilen mobilya ikonu.")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Üretilen mobilya adı.")]
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Tooltip("Mevcut sipariş adedi. Örn: x3")]
    [SerializeField] private TextMeshProUGUI countText;

    [Header("--- İlerleme Barı (%) ---")]
    [Tooltip("Canlı dolan yüzde barı görseli (Image Type = Filled, Fill Method = Horizontal).")]
    [SerializeField] private Image progressFillImage;

    [Tooltip("Tamamlanma yüzdesi metni. Örn: %45")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Tooltip("Bu siparişin aktif üretilip üretilmediğini belirten görsel parlaması.")]
    [SerializeField] private GameObject activeIndicator;

    private string itemID;
    private int amount;
    private bool isActiveItem;

    /// <summary>
    /// Kutucuğu üretilen ürün verileri ile kurar.
    /// </summary>
    public void Setup(string id, int count, bool isCurrentlyProcessing)
    {
        itemID = id;
        amount = count;
        isActiveItem = isCurrentlyProcessing;

        if (itemNameText != null)
            itemNameText.text = id;

        if (countText != null)
            countText.text = count > 1 ? $"x{count}" : "";

        if (activeIndicator != null)
            activeIndicator.SetActive(isCurrentlyProcessing);

        // Aktif üretilmiyorsa barı ve metni sıfırla
        if (!isCurrentlyProcessing)
        {
            UpdateProgress(0f);
        }
    }

    /// <summary>
    /// Üretim yüzdesini günceller (0.0f - 1.0f).
    /// </summary>
    public void UpdateProgress(float progressRatio)
    {
        progressRatio = Mathf.Clamp01(progressRatio);

        if (progressFillImage != null)
            progressFillImage.fillAmount = progressRatio;

        if (progressText != null)
        {
            int percentage = Mathf.RoundToInt(progressRatio * 100f);
            progressText.text = isActiveItem ? $"%{percentage}" : "%0";
        }
    }

    public string GetItemID() => itemID;
}
