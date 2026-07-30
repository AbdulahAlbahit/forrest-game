using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Envanter panelindeki (InventoryPanel) Yükseltme / Ekipman sekmesindeki tek bir upgrade kartını yönetir.
/// Eşyanın adı (Axe, Basket, Truck, Workshop vb.), ikonu, mevcut seviyesi, stat değeri, yükseltme maliyeti
/// ve 'Yükselt' butonunu içerir.
/// </summary>
public class UpgradeItemUI : MonoBehaviour
{
    [Header("--- UI Elemanları ---")]
    [Tooltip("Eşya ikonu.")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Eşya / Upgrade adı (Örn: 'Sepet Kapasitesi').")]
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Tooltip("Mevcut seviye metni (Örn: 'Seviye 3').")]
    [SerializeField] private TextMeshProUGUI itemLevelText;

    [Tooltip("Mevcut stat değeri metni (Örn: 'Kapasite: 15 Odun').")]
    [SerializeField] private TextMeshProUGUI itemStatText;

    [Header("--- Aksiyon Butonu ---")]
    [Tooltip("Yükseltme butonu.")]
    [SerializeField] private Button upgradeButton;

    [Tooltip("Yükseltme butonu metni (Örn: 'Yükselt (150 Altın)').")]
    [SerializeField] private TextMeshProUGUI upgradeButtonText;

    [Header("--- Durum Görselleri ---")]
    [Tooltip("Maksimum seviyeye ulaşıldı vurgusu / rozeti.")]
    [SerializeField] private GameObject maxLevelBadge;

    private string currentItemId = "";
    private System.Action<string> onUpgradeClickedCallback;
    private float lastClickTime = 0f;

    private void Awake()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }
    }

    /// <summary>
    /// Kartı verilen upgrade verisi ile doldurur.
    /// </summary>
    public void Setup(
        string itemId,
        string displayName,
        Sprite icon,
        int currentLevel,
        float statValue,
        int upgradeCost,
        bool isMaxLevel,
        int currentGold,
        System.Action<string> onUpgradeClicked)
    {
        currentItemId = itemId;
        onUpgradeClickedCallback = onUpgradeClicked;

        if (itemNameText != null)
            itemNameText.text = string.IsNullOrEmpty(displayName) ? itemId : displayName;

        if (itemIcon != null)
        {
            if (icon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemIcon.gameObject.SetActive(false);
            }
        }

        if (itemLevelText != null)
            itemLevelText.text = isMaxLevel ? $"Seviye {currentLevel} (MAX)" : $"Seviye {currentLevel}";

        if (itemStatText != null)
            itemStatText.text = $"Mevcut Değer: {statValue:F1}";

        if (maxLevelBadge != null)
            maxLevelBadge.SetActive(isMaxLevel);

        if (isMaxLevel)
        {
            if (upgradeButton != null) upgradeButton.interactable = false;
            if (upgradeButtonText != null) upgradeButtonText.text = "MAKS SEVİYE";
        }
        else
        {
            bool canAfford = currentGold >= upgradeCost;
            if (upgradeButton != null) upgradeButton.interactable = canAfford;
            if (upgradeButtonText != null)
            {
                upgradeButtonText.text = canAfford ? $"Yükselt ({upgradeCost:N0} Gold)" : $"Yetersiz Gold ({upgradeCost:N0})";
            }
        }
    }

    private void OnUpgradeClicked()
    {
        if (Time.unscaledTime - lastClickTime < 0.25f) return;
        lastClickTime = Time.unscaledTime;

        onUpgradeClickedCallback?.Invoke(currentItemId);
    }
}
