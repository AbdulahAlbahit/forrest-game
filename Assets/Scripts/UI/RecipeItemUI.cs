using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Atölye panelindeki mobilya/tarif listesinin tek bir kutucuğunu temsil eder.
/// Kutuya tıklandığında bağlı olduğu RecipeConfig verisini ana panele bildirir.
/// </summary>
public class RecipeItemUI : MonoBehaviour
{
    [Header("--- UI Elemanları ---")]
    [Tooltip("Mobilya ikonu / görseli.")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Mobilya adı metni.")]
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Tooltip("Seçim arka planı görseli (Seçilince parlar).")]
    [SerializeField] private GameObject selectionHighlight;

    [Tooltip("Tıklama butonu.")]
    [SerializeField] private Button clickButton;

    private RecipeConfig currentRecipe;
    private System.Action<RecipeConfig> onSelectedCallback;

    private void Awake()
    {
        if (clickButton != null)
            clickButton.onClick.AddListener(OnClick);
    }

    /// <summary>
    /// Kutucuğu verilen tarif verisi ile doldurur.
    /// </summary>
    public void Setup(RecipeConfig recipe, System.Action<RecipeConfig> onSelected)
    {
        currentRecipe = recipe;
        onSelectedCallback = onSelected;

        if (recipe != null)
        {
            if (itemNameText != null)
                itemNameText.text = recipe.ItemID;

            if (itemIcon != null)
                itemIcon.sprite = recipe.Icon;
        }

        SetSelected(false);
    }

    /// <summary>
    /// Bu kutucuğun seçili olup olmadığını ayarlar (Öne çıkarma görseli).
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(isSelected);
    }

    private void OnClick()
    {
        onSelectedCallback?.Invoke(currentRecipe);
    }
}
