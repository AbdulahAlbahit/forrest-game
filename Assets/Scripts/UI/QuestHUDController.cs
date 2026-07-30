using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Core;
using ForrestGame.Managers;

/// <summary>
/// Sağ üstte Görevler ve Müşteri Siparişlerini gösteren HUD widget kontrolcüsü.
/// Toggle butonuna her basıldığında:
/// 1. Basış: 3 Aktif Görev (ShowQuests)
/// 2. Basış: 3 Bekleyen Müşteri Siparişi (ShowOrders)
/// 3. Basış: Paneli Kapat (Hidden)
/// döngüsünde geçiş yapar.
/// </summary>
public class QuestHUDController : MonoBehaviour
{
    public static QuestHUDController Instance { get; private set; }

    /// <summary>
    /// Görüntüleme modları: 3 Görev → 3 Müşteri Siparişi → Gizli.
    /// </summary>
    public enum QuestDisplayMode
    {
        ShowQuests,     // 3 Aktif Görev
        ShowOrders,     // 3 Bekleyen Müşteri Siparişi
        Hidden          // Paneli Kapat
    }

    [Header("--- UI Referansları ---")]
    [Tooltip("Görev listesini içeren ana panel objesi.")]
    [SerializeField] private GameObject questPanel;

    [Tooltip("Görev gösterme modunu değiştiren toggle butonu (Sağ Üst).")]
    [SerializeField] private Button questToggleButton;

    [Tooltip("Toggle butonunun üzerindeki ikon/metin. Moda göre değişir.")]
    [SerializeField] private TextMeshProUGUI toggleButtonText;

    [Header("--- Tüm Görevler Paneli Açma Butonu ---")]
    [Tooltip("Tüm Görevler detay penceresini açan buton.")]
    [SerializeField] private Button openFullQuestPanelButton;

    [Tooltip("Tüm Görevler büyük detay paneli objesi (FullQuestPanelController bileşeni içerir).")]
    [SerializeField] private GameObject fullQuestPanel;

    [Header("--- Manuel Görev / Sipariş Satırları ---")]
    [Tooltip("1. satır paneli (QuestRow1).")]
    [SerializeField] private GameObject questRow1;
    [Tooltip("1. ad metni (QuestName1).")]
    [SerializeField] private TextMeshProUGUI questName1;
    [Tooltip("1. ilerleme metni (QuestProgress1).")]
    [SerializeField] private TextMeshProUGUI questProgress1;
    [Tooltip("1. ilerleme barı (Image1).")]
    [SerializeField] private Image questFill1;

    [Tooltip("2. satır paneli (QuestRow2).")]
    [SerializeField] private GameObject questRow2;
    [Tooltip("2. ad metni (QuestName2).")]
    [SerializeField] private TextMeshProUGUI questName2;
    [Tooltip("2. ilerleme metni (QuestProgress2).")]
    [SerializeField] private TextMeshProUGUI questProgress2;
    [Tooltip("2. ilerleme barı (Image2).")]
    [SerializeField] private Image questFill2;

    [Tooltip("3. satır paneli (QuestRow3).")]
    [SerializeField] private GameObject questRow3;
    [Tooltip("3. ad metni (QuestName3).")]
    [SerializeField] private TextMeshProUGUI questName3;
    [Tooltip("3. ilerleme metni (QuestProgress3).")]
    [SerializeField] private TextMeshProUGUI questProgress3;
    [Tooltip("3. ilerleme barı (Image3).")]
    [SerializeField] private Image questFill3;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX ismi.")]
    [SerializeField] private string buttonSfxName = "";

    // Dahili durum
    private QuestDisplayMode currentMode = QuestDisplayMode.Hidden;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (questToggleButton != null)
            questToggleButton.onClick.AddListener(OnToggleButtonClicked);

        if (openFullQuestPanelButton != null)
            openFullQuestPanelButton.onClick.AddListener(OpenFullQuestPanel);

        // Başlangıçta gizli modda başla
        currentMode = QuestDisplayMode.Hidden;
        ApplyDisplayMode();
    }

    /// <summary>
    /// Büyük Tüm Görevler Panelini açar.
    /// </summary>
    public void OpenFullQuestPanel()
    {
        PlayButtonClickSFX();

        if (FullQuestPanelController.Instance != null)
        {
            FullQuestPanelController.Instance.OpenPanel();
        }
        else if (fullQuestPanel != null)
        {
            fullQuestPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[QuestHUD] FullQuestPanelController veya fullQuestPanel referansı bulunamadı!");
        }
    }

    private void OnEnable()
    {
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;

        ShopManager.OnItemSold += HandleItemSold;
        ShopManager.OnStockChanged += HandleStockChanged;
    }

    private void OnDisable()
    {
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;

        ShopManager.OnItemSold -= HandleItemSold;
        ShopManager.OnStockChanged -= HandleStockChanged;
    }

    private void HandleDataLoaded(PlayerData data)
    {
        RefreshQuestDisplay();
    }

    private void HandleItemSold(string itemId, int amount)
    {
        RefreshQuestDisplay();
    }

    private void HandleStockChanged(string itemId, int newStock)
    {
        RefreshQuestDisplay();
    }

    // ═══════════════════════════════════════════
    //  Toggle Butonu İşlemi (Döngü: 3 Görev -> 3 Sipariş -> Gizli)
    // ═══════════════════════════════════════════

    private void OnToggleButtonClicked()
    {
        PlayButtonClickSFX();

        // 3 Aşamalı Döngü: ShowQuests → ShowOrders → Hidden → ShowQuests ...
        switch (currentMode)
        {
            case QuestDisplayMode.Hidden:
                currentMode = QuestDisplayMode.ShowQuests;
                break;
            case QuestDisplayMode.ShowQuests:
                currentMode = QuestDisplayMode.ShowOrders;
                break;
            case QuestDisplayMode.ShowOrders:
                currentMode = QuestDisplayMode.Hidden;
                break;
        }

        ApplyDisplayMode();
    }

    // ═══════════════════════════════════════════
    //  Görev / Sipariş Gösterim Modu Uygulama
    // ═══════════════════════════════════════════

    private void ApplyDisplayMode()
    {
        switch (currentMode)
        {
            case QuestDisplayMode.ShowQuests:
                if (questPanel != null) questPanel.SetActive(true);
                if (openFullQuestPanelButton != null) openFullQuestPanelButton.gameObject.SetActive(true);
                UpdateToggleButtonText("Görevler (3)");
                RefreshQuestDisplay();
                break;

            case QuestDisplayMode.ShowOrders:
                if (questPanel != null) questPanel.SetActive(true);
                if (openFullQuestPanelButton != null) openFullQuestPanelButton.gameObject.SetActive(false);
                UpdateToggleButtonText("Siparişler (3)");
                RefreshQuestDisplay();
                break;

            case QuestDisplayMode.Hidden:
                if (questPanel != null) questPanel.SetActive(false);
                UpdateToggleButtonText("Görev/Sipariş");
                break;
        }
    }

    private void UpdateToggleButtonText(string text)
    {
        if (toggleButtonText != null)
            toggleButtonText.text = text;
    }

    // ═══════════════════════════════════════════
    //  Görev & Sipariş Verilerini Güncelleme
    // ═══════════════════════════════════════════

    public void RefreshQuestDisplay()
    {
        if (currentMode == QuestDisplayMode.Hidden) return;

        List<QuestDisplayData> displayData = currentMode == QuestDisplayMode.ShowQuests 
            ? CollectQuestData() 
            : CollectOrdersData();

        // 3 satırı güncelle
        UpdateRow(questRow1, questName1, questProgress1, questFill1, displayData, 0);
        UpdateRow(questRow2, questName2, questProgress2, questFill2, displayData, 1);
        UpdateRow(questRow3, questName3, questProgress3, questFill3, displayData, 2);
    }

    private void UpdateRow(GameObject row, TextMeshProUGUI nameText, TextMeshProUGUI progressText,
                           Image fillImage, List<QuestDisplayData> data, int index)
    {
        if (row == null) return;

        bool shouldShow = index < data.Count;
        row.SetActive(shouldShow);

        if (!shouldShow) return;

        QuestDisplayData item = data[index];

        if (nameText != null)
            nameText.text = item.Name;

        if (progressText != null)
            progressText.text = item.ProgressString;

        if (fillImage != null)
            fillImage.fillAmount = item.ProgressRatio;
    }

    // ═══════════════════════════════════════════
    //  Veri Toplama (Görevler vs Müşteri Siparişleri)
    // ═══════════════════════════════════════════

    private List<QuestDisplayData> CollectQuestData()
    {
        List<QuestDisplayData> result = new List<QuestDisplayData>();

        if (QuestManager.Instance != null && QuestManager.Instance.tumGorevler != null)
        {
            PlayerData data = CloudSaveManager.Instance?.CurrentData;

            foreach (QuestConfig quest in QuestManager.Instance.tumGorevler)
            {
                if (quest == null) continue;

                bool isCompleted = data?.CompletedQuests != null && data.CompletedQuests.Contains(quest.QuestID);
                int progress = 0;

                if (data?.ActiveQuests != null)
                    progress = data.ActiveQuests.Get(quest.QuestID, 0);

                result.Add(new QuestDisplayData
                {
                    Name = isCompleted ? $"{quest.QuestName} (Bitti)" : quest.QuestName,
                    ProgressString = isCompleted ? "Tamamlandı!" : $"{progress} / {quest.TargetAmount}",
                    ProgressRatio = quest.TargetAmount > 0 ? Mathf.Clamp01((float)progress / quest.TargetAmount) : 0f,
                    IsCompleted = isCompleted
                });
            }
        }

        result.Sort((a, b) => a.IsCompleted.CompareTo(b.IsCompleted));
        return result;
    }

    private List<QuestDisplayData> CollectOrdersData()
    {
        List<QuestDisplayData> result = new List<QuestDisplayData>();
        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;

        if (data != null && data.PendingCustomerOrders != null && data.PendingCustomerOrders.keys.Count > 0)
        {
            for (int i = 0; i < data.PendingCustomerOrders.keys.Count; i++)
            {
                string itemId = data.PendingCustomerOrders.keys[i];
                int pending = data.PendingCustomerOrders.values[i];

                if (pending > 0)
                {
                    int currentStock = data.ShopInventory != null ? data.ShopInventory.Get(itemId, 0) : 0;
                    bool canFulfill = currentStock >= pending;
                    float ratio = pending > 0 ? Mathf.Clamp01((float)currentStock / pending) : 1f;

                    result.Add(new QuestDisplayData
                    {
                        Name = itemId,
                        ProgressString = canFulfill ? $"Hazır! ({currentStock}/{pending})" : $"Bekleniyor: {pending} (Stok: {currentStock})",
                        ProgressRatio = ratio,
                        IsCompleted = canFulfill
                    });
                }
            }
        }

        if (result.Count == 0)
        {
            result.Add(new QuestDisplayData
            {
                Name = "Bekleyen Sipariş Yok",
                ProgressString = "Tüm siparişler karşılandı!",
                ProgressRatio = 1f,
                IsCompleted = true
            });
        }


        return result;
    }

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }

    private class QuestDisplayData
    {
        public string Name;
        public string ProgressString;
        public float ProgressRatio;
        public bool IsCompleted;
    }
}
