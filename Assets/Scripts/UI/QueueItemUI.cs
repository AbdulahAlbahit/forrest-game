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
    private RecipeConfig linkedRecipe;

    /// <summary>
    /// Mevcut mobilya prefab referansı. Üretim tamamlandığında sahneye spawn için kullanılabilir.
    /// </summary>
    public GameObject FurniturePrefab => linkedRecipe != null ? linkedRecipe.FurniturePrefab : null;

    /// <summary>
    /// Kutucuğu RecipeConfig ile kurar. İkon ve prefab otomatik bağlanır.
    /// </summary>
    public void Setup(RecipeConfig recipe, int count, bool isCurrentlyProcessing)
    {
        linkedRecipe = recipe;
        string id = recipe != null ? recipe.ItemID : "???";

        SetupInternal(id, count, isCurrentlyProcessing);

        // RecipeConfig'teki ikonu yükle
        if (itemIcon != null && recipe != null && recipe.ItemIcon != null)
            itemIcon.sprite = recipe.ItemIcon;
    }

    /// <summary>
    /// Kutucuğu string ID ile kurar (geriye uyumluluk).
    /// WorkshopManager.tumTarifler içinde eşleşen tarifi arar.
    /// </summary>
    public void Setup(string id, int count, bool isCurrentlyProcessing)
    {
        // tumTarifler içinde RecipeConfig'i bulmaya çalış
        linkedRecipe = FindRecipeByID(id);

        SetupInternal(id, count, isCurrentlyProcessing);

        // Eşleşen tarif varsa ikonunu yükle
        if (itemIcon != null && linkedRecipe != null && linkedRecipe.ItemIcon != null)
            itemIcon.sprite = linkedRecipe.ItemIcon;
    }

    private void SetupInternal(string id, int count, bool isCurrentlyProcessing)
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
    public RecipeConfig GetLinkedRecipe() => linkedRecipe;

    private RecipeConfig FindRecipeByID(string id)
    {
        if (WorkshopManager.Instance == null || WorkshopManager.Instance.tumTarifler == null)
            return null;

        foreach (var recipe in WorkshopManager.Instance.tumTarifler)
        {
            if (recipe != null && recipe.ItemID == id)
                return recipe;
        }
        return null;
    }
}

