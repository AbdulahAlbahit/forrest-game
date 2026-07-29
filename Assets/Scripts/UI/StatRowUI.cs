using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Karakter İstatistik Paneli'ndeki tek bir istatistik satırını (veya kategori başlığını) yöneten bileşen.
/// Diablo / Path of Exile tarzı koyu temalı satır görünümünü destekler.
/// </summary>
public class StatRowUI : MonoBehaviour
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

    [Header("--- Varsayılan Renkler ---")]
    [SerializeField] private Color defaultLabelColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color defaultValueColor = new Color(1f, 0.84f, 0f, 1f); // Altın sarısı
    [SerializeField] private Color headerTextColor = new Color(1f, 0.6f, 0.1f, 1f); // Turuncu / Başlık
    [SerializeField] private Color headerBgColor = new Color(0.15f, 0.15f, 0.18f, 0.9f);
    [SerializeField] private Color rowBgColor = new Color(0.08f, 0.08f, 0.1f, 0.6f);

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
            backgroundImage = GetComponent<Image>();
        }
    }

    /// <summary>
    /// Normal bir istatistik satırı olarak ayarlar.
    /// </summary>
    public void Setup(string label, string value, Color? valColor = null, Color? lblColor = null)
    {
        AutoFindComponentsIfNull();
        gameObject.SetActive(true);

        if (labelText != null)
        {
            labelText.text = label;
            labelText.color = lblColor ?? defaultLabelColor;
            labelText.fontStyle = FontStyles.Normal;
        }

        if (valueText != null)
        {
            valueText.gameObject.SetActive(true);
            valueText.text = value;
            valueText.color = valColor ?? defaultValueColor;
            valueText.fontStyle = FontStyles.Bold;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = rowBgColor;
        }

        if (dividerLine != null)
        {
            dividerLine.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Kategori başlık satırı (Örn: ⚔ KARAKTER, 🪓 BALTA) olarak ayarlar.
    /// </summary>
    public void SetupAsHeader(string categoryTitle, Color? titleColor = null)
    {
        gameObject.SetActive(true);

        if (labelText != null)
        {
            labelText.text = categoryTitle.ToUpper();
            labelText.color = titleColor ?? headerTextColor;
            labelText.fontStyle = FontStyles.Bold;
        }

        if (valueText != null)
        {
            valueText.text = "";
            valueText.gameObject.SetActive(false);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = headerBgColor;
        }

        if (dividerLine != null)
        {
            dividerLine.gameObject.SetActive(true);
            dividerLine.color = titleColor ?? headerTextColor;
        }
    }
}
