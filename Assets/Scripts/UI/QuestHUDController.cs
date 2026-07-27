using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Managers;

/// <summary>
/// Sağ üstte görev (quest) bilgilerini gösteren HUD kontrolcüsü.
/// Toggle butonuna her basıldığında 3 görev → 1 görev → gizle döngüsünde geçiş yapar.
///
/// KULLANIM:
/// 1. CanvasUI altına bir QuestPanel (dikey liste) ve QuestToggleButton ekleyin.
/// 2. Bu scripti CanvasUI veya ayrı bir objeye ekleyin.
/// 3. Inspector'dan referansları sürükleyin.
/// </summary>
public class QuestHUDController : MonoBehaviour
{
    public static QuestHUDController Instance { get; private set; }

    /// <summary>
    /// Görev görüntüleme modları: 3 görev, 1 görev veya gizli.
    /// </summary>
    public enum QuestDisplayMode
    {
        ShowThree,
        ShowOne,
        Hidden
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

    [Header("--- Görev Satırları (Prefab Sistemi) ---")]
    [Tooltip("Görev satırlarının dizileceği Content objesi (VerticalLayoutGroup).")]
    [SerializeField] private Transform questListContent;

    [Tooltip("Her bir görev satırı için prefab. İçinde QuestItemTexts olmalı (aşağıya bakınız).")]
    [SerializeField] private GameObject questItemPrefab;

    [Header("--- Manuel Görev Satırları (Prefab Yoksa) ---")]
    [Tooltip("1. görev satırı paneli.")]
    [SerializeField] private GameObject questRow1;
    [Tooltip("1. görev adı metni.")]
    [SerializeField] private TextMeshProUGUI questName1;
    [Tooltip("1. görev ilerleme metni (ör: 2/5).")]
    [SerializeField] private TextMeshProUGUI questProgress1;
    [Tooltip("1. görev ilerleme barı (Image Type = Filled).")]
    [SerializeField] private Image questFill1;

    [Tooltip("2. görev satırı paneli.")]
    [SerializeField] private GameObject questRow2;
    [Tooltip("2. görev adı metni.")]
    [SerializeField] private TextMeshProUGUI questName2;
    [Tooltip("2. görev ilerleme metni.")]
    [SerializeField] private TextMeshProUGUI questProgress2;
    [Tooltip("2. görev ilerleme barı.")]
    [SerializeField] private Image questFill2;

    [Tooltip("3. görev satırı paneli.")]
    [SerializeField] private GameObject questRow3;
    [Tooltip("3. görev adı metni.")]
    [SerializeField] private TextMeshProUGUI questName3;
    [Tooltip("3. görev ilerleme metni.")]
    [SerializeField] private TextMeshProUGUI questProgress3;
    [Tooltip("3. görev ilerleme barı.")]
    [SerializeField] private Image questFill3;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX ismi.")]
    [SerializeField] private string buttonSfxName = "";

    // Dahili durum
    private QuestDisplayMode currentMode = QuestDisplayMode.Hidden;

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

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
        // Görev verileri değiştiğinde güncelle
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;
    }

    private void OnDisable()
    {
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded(PlayerData data)
    {
        RefreshQuestDisplay();
    }

    // ═══════════════════════════════════════════
    //  Toggle Butonu İşlemi
    // ═══════════════════════════════════════════

    private void OnToggleButtonClicked()
    {
        PlayButtonClickSFX();

        // Döngü: ShowThree → ShowOne → Hidden → ShowThree ...
        switch (currentMode)
        {
            case QuestDisplayMode.Hidden:
                currentMode = QuestDisplayMode.ShowThree;
                break;
            case QuestDisplayMode.ShowThree:
                currentMode = QuestDisplayMode.ShowOne;
                break;
            case QuestDisplayMode.ShowOne:
                currentMode = QuestDisplayMode.Hidden;
                break;
        }

        ApplyDisplayMode();
    }

    // ═══════════════════════════════════════════
    //  Görev Gösterim Modu Uygulama
    // ═══════════════════════════════════════════

    private void ApplyDisplayMode()
    {
        switch (currentMode)
        {
            case QuestDisplayMode.ShowThree:
                if (questPanel != null) questPanel.SetActive(true);
                UpdateToggleButtonText("📋 3");
                RefreshQuestDisplay();
                break;

            case QuestDisplayMode.ShowOne:
                if (questPanel != null) questPanel.SetActive(true);
                UpdateToggleButtonText("📋 1");
                RefreshQuestDisplay();
                break;

            case QuestDisplayMode.Hidden:
                if (questPanel != null) questPanel.SetActive(false);
                UpdateToggleButtonText("📋");
                break;
        }

        Debug.Log($"[QuestHUD] Görev modu değişti: {currentMode}");
    }

    private void UpdateToggleButtonText(string text)
    {
        if (toggleButtonText != null)
            toggleButtonText.text = text;
    }

    // ═══════════════════════════════════════════
    //  Görev Verilerini Güncelleme
    // ═══════════════════════════════════════════

    /// <summary>
    /// Mevcut moda göre görev satırlarını günceller.
    /// </summary>
    public void RefreshQuestDisplay()
    {
        if (currentMode == QuestDisplayMode.Hidden) return;

        int maxVisible = currentMode == QuestDisplayMode.ShowThree ? 3 : 1;

        // Görev verilerini topla
        List<QuestDisplayData> displayData = CollectQuestData();

        // Satırları güncelle
        UpdateQuestRow(questRow1, questName1, questProgress1, questFill1, displayData, 0, maxVisible);
        UpdateQuestRow(questRow2, questName2, questProgress2, questFill2, displayData, 1, maxVisible);
        UpdateQuestRow(questRow3, questName3, questProgress3, questFill3, displayData, 2, maxVisible);
    }

    private void UpdateQuestRow(GameObject row, TextMeshProUGUI nameText, TextMeshProUGUI progressText,
                                 Image fillImage, List<QuestDisplayData> data, int index, int maxVisible)
    {
        if (row == null) return;

        // Bu satır görünür mü?
        bool shouldShow = index < maxVisible && index < data.Count;
        row.SetActive(shouldShow);

        if (!shouldShow) return;

        QuestDisplayData quest = data[index];

        if (nameText != null)
            nameText.text = quest.IsCompleted ? $"✅ {quest.Name}" : quest.Name;

        if (progressText != null)
            progressText.text = quest.IsCompleted ? "Tamamlandı!" : $"{quest.CurrentProgress} / {quest.TargetAmount}";

        if (fillImage != null)
            fillImage.fillAmount = quest.ProgressRatio;
    }

    // ═══════════════════════════════════════════
    //  Görev Verisi Toplama
    // ═══════════════════════════════════════════

    private List<QuestDisplayData> CollectQuestData()
    {
        List<QuestDisplayData> result = new List<QuestDisplayData>();

        // 1. Gerçek sistem aktifse QuestManager'dan oku
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
                    Name = quest.QuestName,
                    Description = quest.QuestDescription,
                    CurrentProgress = progress,
                    TargetAmount = quest.TargetAmount,
                    ProgressRatio = quest.TargetAmount > 0 ? Mathf.Clamp01((float)progress / quest.TargetAmount) : 0f,
                    IsCompleted = isCompleted
                });
            }
        }

        // 2. QuestManager yoksa veya görev listesi boşsa → test verileri göster
        if (result.Count == 0)
        {
            result.Add(new QuestDisplayData { Name = "İlk Masanı Üret", CurrentProgress = 0, TargetAmount = 1, ProgressRatio = 0f, IsCompleted = false });
            result.Add(new QuestDisplayData { Name = "3 Sandalye Sat", CurrentProgress = 1, TargetAmount = 3, ProgressRatio = 0.33f, IsCompleted = false });
            result.Add(new QuestDisplayData { Name = "5 Odun Topla", CurrentProgress = 5, TargetAmount = 5, ProgressRatio = 1f, IsCompleted = true });
        }

        // Tamamlanmamış görevleri öne al
        result.Sort((a, b) => a.IsCompleted.CompareTo(b.IsCompleted));

        return result;
    }

    // ═══════════════════════════════════════════
    //  Ses
    // ═══════════════════════════════════════════

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && ForrestGame.Core.AudioManager.Instance != null)
        {
            ForrestGame.Core.AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }

    // ═══════════════════════════════════════════
    //  Yardımcı Veri Sınıfı
    // ═══════════════════════════════════════════

    private class QuestDisplayData
    {
        public string Name;
        public string Description;
        public int CurrentProgress;
        public int TargetAmount;
        public float ProgressRatio;
        public bool IsCompleted;
    }
}
