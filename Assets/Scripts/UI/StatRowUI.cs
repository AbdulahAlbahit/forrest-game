using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// Karakter İstatistik Paneli'ndeki tek bir istatistik satırını (veya kategori başlığını) yöneten bileşen.
/// Diablo / Path of Exile tarzı koyu temalı satır görünümünü ve tıklanabilir akordiyon başlıklarını destekler.
/// Hem uGUI Button hem de IPointerClickHandler ile %100 kararlı tıklama algılar.
/// </summary>
public class StatRowUI : MonoBehaviour, IPointerClickHandler
{
    [Header("--- UI Bileşenleri ---")]
    [Tooltip("Stat başlığı / adı (Sol taraf). Örn: Balta Gücü")]
    [SerializeField] private TextMeshProUGUI labelText;

    [Tooltip("Stat değeri (Sağ taraf). Örn: 25.0")]
    [SerializeField] private TextMeshProUGUI valueText;

    [Tooltip("Satır arka plan görseli.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Kategori başlığı altındaki veya satır altındaki ayırıcı çizgi.")]
    [SerializeField] private Image dividerLine;

    [Tooltip("Satır ikonu görseli (Opsiyonel).")]
    [SerializeField] private Image iconImage;

    [Header("--- Varsayılan Renkler ---")]
    [SerializeField] private Color defaultLabelColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color defaultValueColor = new Color(1f, 0.84f, 0f, 1f); // Altın sarısı
    [SerializeField] private Color headerTextColor = new Color(1f, 0.6f, 0.1f, 1f); // Turuncu / Başlık
    [SerializeField] private Color headerBgColor = new Color(0.15f, 0.15f, 0.18f, 0.9f);
    [SerializeField] private Color rowBgColor = new Color(0.08f, 0.08f, 0.1f, 0.6f);

    private Action onHeaderClicked;
    private bool isHeaderRow = false;
    private Button rowButton;

    private void Awake()
    {
        AutoFindComponentsIfNull();
    }

    private void AutoFindComponentsIfNull()
    {
        if (labelText == null || valueText == null)
        {
            var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps != null && tmps.Length > 0)
            {
                if (labelText == null) labelText = tmps[0];
                if (valueText == null && tmps.Length > 1) valueText = tmps[1];
            }
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>() ?? GetComponentInChildren<Image>(true);
        }

        if (iconImage == null)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img != backgroundImage && img != dividerLine)
                {
                    iconImage = img;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Normal bir istatistik satırı olarak ayarlar.
    /// </summary>
    public void Setup(string label, string value, Color? valColor = null, Color? lblColor = null, Sprite icon = null)
    {
        AutoFindComponentsIfNull();
        isHeaderRow = false;
        onHeaderClicked = null;

        if (rowButton != null)
            rowButton.interactable = false;

        if (labelText != null)
        {
            labelText.text = label;
            labelText.color = lblColor ?? defaultLabelColor;
            labelText.fontStyle = FontStyles.Normal;
            labelText.raycastTarget = false;
        }

        if (valueText != null)
        {
            valueText.gameObject.SetActive(true);
            valueText.text = value;
            valueText.color = valColor ?? defaultValueColor;
            valueText.fontStyle = FontStyles.Bold;
            valueText.raycastTarget = false;
        }

        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
                iconImage.raycastTarget = false;
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = rowBgColor;
            backgroundImage.raycastTarget = false;
        }

        if (dividerLine != null)
        {
            dividerLine.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Kategori başlık satırı (Örn: ⚔ KARAKTER, 🪓 BALTA) ve akordiyon katlama desteği ile ayarlar.
    /// </summary>
    public void SetupAsHeader(string categoryTitle, Color? titleColor = null, Action onToggle = null, bool isExpanded = true)
    {
        AutoFindComponentsIfNull();
        gameObject.SetActive(true);

        isHeaderRow = true;
        onHeaderClicked = onToggle;

        string arrow = isExpanded ? "▼ " : "▶ ";

        if (labelText != null)
        {
            labelText.text = $"{arrow}{categoryTitle.ToUpper()}";
            labelText.color = titleColor ?? headerTextColor;
            labelText.fontStyle = FontStyles.Bold;
            labelText.raycastTarget = false; // Tıklamaların arka plana geçmesini sağla
        }

        if (valueText != null)
        {
            valueText.text = isExpanded ? "Gizle [-]" : "Göster [+]";
            valueText.gameObject.SetActive(true);
            valueText.color = titleColor ?? headerTextColor;
            valueText.fontStyle = FontStyles.Italic;
            valueText.raycastTarget = false;
        }

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        Image raycastGraphic = backgroundImage != null ? backgroundImage : GetComponent<Image>();
        if (raycastGraphic == null)
        {
            raycastGraphic = gameObject.AddComponent<Image>();
            raycastGraphic.color = headerBgColor;
            backgroundImage = raycastGraphic;
        }

        if (raycastGraphic != null)
        {
            raycastGraphic.color = headerBgColor;
            raycastGraphic.raycastTarget = true;
        }

        if (dividerLine != null)
        {
            dividerLine.gameObject.SetActive(true);
            dividerLine.color = titleColor ?? headerTextColor;
            dividerLine.raycastTarget = false;
        }

        // uGUI Button Bileşeni Kurulumu
        rowButton = GetComponent<Button>();
        if (rowButton == null)
        {
            rowButton = gameObject.AddComponent<Button>();
        }

        if (raycastGraphic != null)
        {
            rowButton.targetGraphic = raycastGraphic;
        }

        rowButton.interactable = true;
        rowButton.transition = Selectable.Transition.ColorTint;
        rowButton.onClick.RemoveAllListeners();
        rowButton.onClick.AddListener(HandleHeaderClicked);
    }

    private void HandleHeaderClicked()
    {
        if (isHeaderRow && onHeaderClicked != null)
        {
            onHeaderClicked.Invoke();
        }
    }

    /// <summary>
    /// EventSystem dökümü tıklama olayı.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        HandleHeaderClicked();
    }
}
