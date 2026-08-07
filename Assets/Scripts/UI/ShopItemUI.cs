using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mağaza panelindeki (ShopPanel) tek bir ürün kartını yönetir.
/// Stoktaki ürünün ikonu, adı, stok adedi, birim satış fiyatı ve 'Sat' butonunu içerir.
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("--- UI Elemanları ---")]
    [Tooltip("Kartın tamamına tıklamayı sağlayan buton.")]
    [SerializeField] private Button cardButton;

    [Tooltip("Ürün görseli / ikonu.")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Ürün adı metni.")]
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Tooltip("Stok miktarı metni (Örn: 'Stok: 5').")]
    [SerializeField] private TextMeshProUGUI itemStockText;

    [Tooltip("Birim satış fiyatı metni (Örn: '250 Altın').")]
    [SerializeField] private TextMeshProUGUI itemPriceText;

    [Header("--- Satış Butonu ---")]
    [Tooltip("Satış aksiyon butonu.")]
    [SerializeField] private Button sellButton;

    [Tooltip("Satış butonu üzerindeki metin.")]
    [SerializeField] private TextMeshProUGUI sellButtonText;

    [Header("--- Vurgu & Durum Görselleri ---")]
    [Tooltip("Seçili ürün vurgu çerçevesi.")]
    [SerializeField] private GameObject selectionHighlight;

    [Tooltip("Stokta yoksa (0 stok) gösterilecek deaktif katman.")]
    [SerializeField] private GameObject emptyStockOverlay;

    private string currentItemId = "";
    private System.Action<string> onSellClickedCallback;
    private System.Action<string> onCardSelectedCallback;
    private float lastClickTime = 0f;

    private void Awake()
    {
        if (cardButton == null)
            cardButton = GetComponent<Button>();

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellClicked);
        }
    }

    /// <summary>
    /// Kartı ürün verileri ile doldurur.
    /// </summary>
    public void Setup(
        string itemId,
        Sprite icon,
        int stock,
        int unitPrice,
        bool isSelected,
        bool hasPendingOrder,
        System.Action<string> onSellClicked,
        System.Action<string> onCardSelected = null)
    {
        currentItemId = itemId;
        onSellClickedCallback = onSellClicked;
        onCardSelectedCallback = onCardSelected;

        if (itemNameText != null)
            itemNameText.text = string.IsNullOrEmpty(itemId) ? "Bilinmeyen Ürün" : itemId;

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

        if (itemStockText != null)
            itemStockText.text = $"Stok: {stock}";

        if (itemPriceText != null)
            itemPriceText.text = $"{unitPrice} Gold";

        if (selectionHighlight != null)
            selectionHighlight.SetActive(isSelected);

        bool hasStock = stock > 0;
        bool canSell = hasStock && hasPendingOrder;

        if (emptyStockOverlay != null)
            emptyStockOverlay.SetActive(!hasStock);

        if (sellButton != null)
            sellButton.interactable = canSell;

        if (sellButtonText != null)
            sellButtonText.text = !hasStock ? "Stok Yok" : (!hasPendingOrder ? "Sipariş Yok" : "Sat");
    }

    /// <summary>
    /// Kartın seçili durumunu günceller.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(isSelected);
    }

    private void OnCardClicked()
    {
        if (Time.unscaledTime - lastClickTime < 0.2f) return;
        lastClickTime = Time.unscaledTime;

        onCardSelectedCallback?.Invoke(currentItemId);
    }

    private void OnSellClicked()
    {
        if (Time.unscaledTime - lastClickTime < 0.2f) return;
        lastClickTime = Time.unscaledTime;

        onSellClickedCallback?.Invoke(currentItemId);
    }
}
