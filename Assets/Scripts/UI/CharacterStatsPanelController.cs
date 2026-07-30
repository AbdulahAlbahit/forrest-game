using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ForrestGame.Managers;
using ForrestGame.Core;
using ForrestGame.Data;

/// <summary>
/// Karakter İstatistik Paneli Kontrolcüsü.
/// Sol alttaki Seviye butonuna basıldığında açılır.
/// Oyuncunun mevcut tüm stat'larını (Karakter, Ekonomi, Balta, Envanter, Atölye, Mevsim Buff/Debuff'ları)
/// kategorilere ayrılmış koyu renkli şık satırlar halinde listeler.
/// </summary>
public class CharacterStatsPanelController : MonoBehaviour
{
    public static CharacterStatsPanelController Instance { get; private set; }

    [Header("--- Panel Referansları ---")]
    [Tooltip("İstatistik paneli ana GameObject'i.")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("Kapat (X) butonu.")]
    [SerializeField] private Button closeButton;

    [Tooltip("HUD üzerindeki Seviye butonu (Opsiyonel - Inspector'dan bağlanabilir veya HUD otomatik bağlar).")]
    [SerializeField] private Button levelButton;

    [Header("--- Liste İçeriği ---")]
    [Tooltip("Kaydırma alanı (ScrollRect).")]
    [SerializeField] private ScrollRect scrollRect;

    [Tooltip("Stat satırlarının ekleneceği Content transform'u.")]
    [SerializeField] private Transform statListContent;

    [Tooltip("Stat satırı prefab'ı (StatRowUI bileşeni içermelidir).")]
    [SerializeField] private GameObject statRowPrefab;

    [Header("--- Ses Ayarları ---")]
    [Tooltip("Butonlara basıldığında çalacak SFX sesi.")]
    [SerializeField] private string buttonSfxName = "";

    [Header("--- Renk Tema Ayarları ---")]
    [SerializeField] private Color headerColorCharacter = new Color(0.4f, 0.7f, 1f, 1f);   // Mavi
    [SerializeField] private Color headerColorEconomy   = new Color(1f, 0.85f, 0.2f, 1f);  // Altın
    [SerializeField] private Color headerColorAxe       = new Color(1f, 0.4f, 0.3f, 1f);   // Kırmızı / Hasar
    [SerializeField] private Color headerColorInventory = new Color(0.4f, 0.9f, 0.4f, 1f);   // Yeşil
    [SerializeField] private Color headerColorWorkshop  = new Color(0.9f, 0.5f, 0.9f, 1f);   // Mor
    [SerializeField] private Color headerColorSeason    = new Color(0.3f, 0.9f, 0.9f, 1f);   // Turkuaz
    [SerializeField] private Color headerColorStats     = new Color(0.8f, 0.8f, 0.8f, 1f);   // Gri / Gümüş

    [SerializeField] private Color normalValueColor     = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color buffValueColor       = new Color(0.3f, 0.95f, 0.4f, 1f);  // Pozitif Buff (Yeşil)
    [SerializeField] private Color debuffValueColor     = new Color(0.95f, 0.3f, 0.3f, 1f);  // Negatif Debuff (Kırmızı)

    private List<StatRowUI> spawnedRows = new List<StatRowUI>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (levelButton != null)
            levelButton.onClick.AddListener(TogglePanel);
    }

    private void Start()
    {
        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(false);
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += HandleStatChanged;

        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged += HandleStatChanged;
            XPManager.Instance.OnLevelUp += HandleStatChanged;
        }

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded += HandleItemUpgraded;

        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged += HandleBasketChanged;

        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged += HandleQueueChanged;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded += HandleDataLoaded;

        SeasonManager.OnMevsimDegisti += HandleMevsimDegisti;
        AxeVisualManager.OnAxeChanged += HandleAxeChanged;
        AxeTablePanelController.OnAxeEquipped += HandleAxeEquipped;
    }

    private void UnsubscribeEvents()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= HandleStatChanged;

        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged -= HandleStatChanged;
            XPManager.Instance.OnLevelUp -= HandleStatChanged;
        }

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnItemUpgraded -= HandleItemUpgraded;

        if (BasketManager.Instance != null)
            BasketManager.Instance.OnBasketChanged -= HandleBasketChanged;

        if (WorkshopManager.Instance != null)
            WorkshopManager.Instance.OnQueueChanged -= HandleQueueChanged;

        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.OnDataLoaded -= HandleDataLoaded;

        SeasonManager.OnMevsimDegisti -= HandleMevsimDegisti;
        AxeVisualManager.OnAxeChanged -= HandleAxeChanged;
        AxeTablePanelController.OnAxeEquipped -= HandleAxeEquipped;
    }

    private void HandleStatChanged(int val) => TriggerRefreshIfActive();
    private void HandleBasketChanged(int cur, int max) => TriggerRefreshIfActive();
    private void HandleQueueChanged(int cur, int max) => TriggerRefreshIfActive();
    private void HandleItemUpgraded(string id, int lvl) => TriggerRefreshIfActive();
    private void HandleDataLoaded(PlayerData data) => TriggerRefreshIfActive();
    private void HandleMevsimDegisti(MevsimTuru mevsim) => TriggerRefreshIfActive();
    private void HandleAxeChanged(string id) => TriggerRefreshIfActive();
    private void HandleAxeEquipped() => TriggerRefreshIfActive();

    private void TriggerRefreshIfActive()
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshPanel();
    }

    private void Update()
    {
    }

    // ═══════════════════════════════════════════
    //  Panel Aç / Kapat
    // ═══════════════════════════════════════════

    public void OpenPanel()
    {
        PlayButtonClickSFX();

        // 1. Script objesini ve mainPanel'i aktif et
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (mainPanel != null && !mainPanel.activeSelf)
            mainPanel.SetActive(true);

        // 2. Üst ebeveynlerin (parent transform'ların) ve CanvasGroup'ların aktif olduğundan emin ol
        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            CanvasGroup parentCG = current.GetComponent<CanvasGroup>();
            if (parentCG != null)
            {
                parentCG.alpha = 1f;
                parentCG.interactable = true;
                parentCG.blocksRaycasts = true;
            }

            current = current.parent;
        }

        // 3. Animator varsa animasyonu sıfırla/açık duruma getir
        Animator anim = GetComponent<Animator>() ?? (mainPanel != null ? mainPanel.GetComponent<Animator>() : null);
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // 5. Scale kontrolü
        RectTransform rt = mainPanel != null ? mainPanel.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
        }

        RefreshPanel();

        // Paneli ilk açışta scroll'u en üste al
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void ClosePanel()
    {
        PlayButtonClickSFX();

        if (mainPanel != null)
            mainPanel.SetActive(false);

        gameObject.SetActive(false);
    }

    public void TogglePanel()
    {
        bool isOpen = (mainPanel != null && mainPanel.activeInHierarchy) || gameObject.activeInHierarchy;
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    // ═══════════════════════════════════════════
    //  Panel Yenileme & Stat Listesi Doldurma
    // ═══════════════════════════════════════════

    public void RefreshPanel()
    {
        ClearSpawnedRows();

        // Fallback: statListContent atanmamışsa ScrollView içindeki Content'i otomatik bulmaya çalış
        if (statListContent == null)
        {
            if (scrollRect != null && scrollRect.content != null)
            {
                statListContent = scrollRect.content;
            }
            else
            {
                var contentObj = transform.Find("Scroll View/Viewport/Content") ?? transform.Find("Panel/Scroll View/Viewport/Content");
                if (contentObj != null) statListContent = contentObj;
            }
        }

        if (statListContent == null)
        {
            Debug.LogWarning("[CharacterStatsPanelController] 'statListContent' (Content Transform) Inspector'dan atanmamış! Statlar listelenemiyor.");
            return;
        }

        if (statRowPrefab == null)
        {
            Debug.LogWarning("[CharacterStatsPanelController] 'statRowPrefab' (StatRowUI Prefab) Inspector'dan atanmamış! Statlar listelenemiyor.");
            return;
        }

        // Layout bileşenlerini kontrol et (VerticalLayoutGroup & ContentSizeFitter yoksa koddan ekle)
        EnsureLayoutComponents(statListContent);

        // Verileri hazırla
        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;

        // 1. KARAKTER
        AddHeader("KARAKTER", headerColorCharacter);
        int playerLevel = XPManager.Instance != null ? XPManager.Instance.MevcutSeviye : (data?.OyuncuSeviyesi ?? 1);
        int currentXP = XPManager.Instance != null ? XPManager.Instance.MevcutXP : (data?.XP ?? 0);
        int reqXP = XPManager.Instance != null ? XPManager.Instance.GetRequiredXPForLevel(playerLevel) : 100;
        float xpPercent = reqXP > 0 ? ((float)currentXP / reqXP) * 100f : 100f;

        AddRow("Oyuncu Seviyesi", $"Level {playerLevel}", normalValueColor);
        AddRow("Mevcut XP", $"{currentXP:N0} / {reqXP:N0}", normalValueColor);
        AddRow("XP İlerleme", $"%{xpPercent:F1}", normalValueColor);

        // 2. EKONOMİ
        AddHeader("EKONOMİ", headerColorEconomy);
        int gold = EconomyManager.Instance != null ? EconomyManager.Instance.MevcutPara : (data?.Para ?? 0);
        AddRow("Altın Bakiyesi", $"{gold:N0} Gold", headerColorEconomy);

        // 3. BALTA
        AddHeader("BALTA İSTATİSTİKLERİ", headerColorAxe);
        string currentAxeId = AxeVisualManager.Instance != null ? AxeVisualManager.Instance.currentAxeId : "lowpoly_axe";
        int equippedTier = GetEquippedAxeTier(currentAxeId);
        float equippedPower = GetAxePowerForTier(equippedTier);

        int maxUnlockedLevel = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel("Axe") : 1;
        float maxUnlockedPower = GetAxePowerForTier(maxUnlockedLevel);

        MevsimTuru currentSeason = SeasonManager.Instance != null ? SeasonManager.Instance.aktifMevsim : MevsimTuru.Ilkbahar;
        SeasonSettings currentSettings = SeasonManager.Instance != null ? SeasonManager.Instance.ayarlar : null;
        float seasonAxeMult = GetAxeMultiplierForSeason(currentSeason, currentSettings);
        float effectiveEquippedDamage = equippedPower * seasonAxeMult;

        AddRow("Kuşanılan Balta", $"Seviye {equippedTier}", headerColorAxe);
        AddRow("Kuşanılan Balta Gücü", $"{equippedPower:F1} Hasar", headerColorAxe);

        Color netDamageColor = seasonAxeMult > 1.0f ? buffValueColor : (seasonAxeMult < 1.0f ? debuffValueColor : normalValueColor);
        AddRow("Net Hasar (Mevsim Etkisiyle)", $"{effectiveEquippedDamage:F1} Hasar", netDamageColor);

        AddRow("Maksimum Açılan Seviye", $"Seviye {maxUnlockedLevel}", normalValueColor);
        AddRow("Maksimum Balta Gücü", $"{maxUnlockedPower:F1} Hasar", normalValueColor);

        // 4. ENVANTER & DEPO
        AddHeader("ENVANTER & DEPO", headerColorInventory);
        int basketCur = BasketManager.Instance != null ? BasketManager.Instance.MevcutDoluluk : 0;
        int basketMax = BasketManager.Instance != null ? BasketManager.Instance.MaksKapasite : 5;
        int basketLvl = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel("Basket") : 1;
        int workshopRawWood = data != null ? data.WorkshopRawWood : 0;
        int workshopCap = WorkshopManager.Instance != null ? WorkshopManager.Instance.GetWorkshopCapacity() : 10;

        AddRow("Sepet Odun Miktarı", $"{basketCur} / {basketMax}", normalValueColor);
        AddRow("Sepet Seviyesi", $"Seviye {basketLvl}", normalValueColor);
        AddRow("Atölye Odun Stoku", $"{workshopRawWood} / {workshopCap}", headerColorInventory);

        // 5. ATÖLYE
        AddHeader("ATÖLYE", headerColorWorkshop);
        int workshopLvl = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel("Workshop") : 1;
        int queueLvl = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetItemLevel("WorkshopQueue") : 1;
        float prodSpeed = WorkshopManager.Instance != null ? WorkshopManager.Instance.GetProcessingSpeed() : 1f;

        AddRow("Atölye Seviyesi", $"Seviye {workshopLvl}", normalValueColor);
        AddRow("Kuyruk Seviyesi", $"Seviye {queueLvl}", normalValueColor);
        AddRow("Üretim Hızı", $"{prodSpeed:F2} /sn", headerColorWorkshop);
        AddRow("Atölye Kapasitesi", $"{workshopCap} Ürün", normalValueColor);

        // 6. MEVSİM & BUFF / DEBUFFS
        AddHeader("MEVSİM & ETKİLERİ", headerColorSeason);
        MevsimTuru activeSeason = SeasonManager.Instance != null ? SeasonManager.Instance.aktifMevsim : MevsimTuru.Ilkbahar;
        float remainingSec = SeasonManager.Instance != null ? SeasonManager.Instance.kalanSaniye : 0f;
        TimeSpan st = TimeSpan.FromSeconds(Mathf.Max(0, remainingSec));
        string timerStr = $"{st.Hours}s {st.Minutes}dk {st.Seconds}sn";

        AddRow("Aktif Mevsim", activeSeason.ToString(), headerColorSeason);
        AddRow("Mevsim Kalan Süre", timerStr, normalValueColor);

        // Mevsimsel Buff / Debuff göstergeleri
        SeasonSettings settings = SeasonManager.Instance != null ? SeasonManager.Instance.ayarlar : null;
        if (settings != null)
        {
            // Balta Hasarı Çarpanı
            float axeMult = GetAxeMultiplierForSeason(activeSeason, settings);
            Color axeMultColor = axeMult > 1.0f ? buffValueColor : (axeMult < 1.0f ? debuffValueColor : normalValueColor);
            AddRow("Balta Hasar Çarpanı", $"x{axeMult:F2}", axeMultColor);

            // Altın Çarpanı (Sonbahar)
            float goldMult = activeSeason == MevsimTuru.Sonbahar ? settings.sonbaharAltinCarpani : 1.0f;
            Color goldMultColor = goldMult > 1.0f ? buffValueColor : normalValueColor;
            AddRow("Altın Kazanma Çarpanı", $"x{goldMult:F2}", goldMultColor);

            // Üretim Hızı Çarpanı (Yaz)
            float prodMult = activeSeason == MevsimTuru.Yaz ? settings.summerProductionSpeed : 1.0f;
            Color prodMultColor = prodMult > 1.0f ? buffValueColor : normalValueColor;
            AddRow("Üretim Hızı Çarpanı", $"x{prodMult:F2}", prodMultColor);

            // Ağaç Büyüme Çarpanı (İlkbahar / Kış)
            float treeMult = GetTreeGrowthMultiplierForSeason(activeSeason, settings);
            Color treeMultColor = treeMult > 1.0f ? buffValueColor : (treeMult < 1.0f ? debuffValueColor : normalValueColor);
            AddRow("Ağaç Büyüme Çarpanı", $"x{treeMult:F2}", treeMultColor);
        }

        // 7. GENEL İSTATİSTİKLER
        AddHeader("GENEL İSTATİSTİKLER", headerColorStats);
        float playTime = data != null ? data.OyunSuresi : 0f;
        TimeSpan pt = TimeSpan.FromSeconds(playTime);
        string playTimeStr = $"{pt.Hours}s {pt.Minutes}dk {pt.Seconds}sn";

        int activeQuestCount = 0;
        int completedQuestCount = (data != null && data.CompletedQuests != null) ? data.CompletedQuests.Count : 0;

        if (QuestManager.Instance != null && QuestManager.Instance.tumGorevler != null)
        {
            foreach (var quest in QuestManager.Instance.tumGorevler)
            {
                if (quest == null) continue;
                bool isCompleted = data?.CompletedQuests != null && data.CompletedQuests.Contains(quest.QuestID);
                if (!isCompleted)
                    activeQuestCount++;
            }
        }
        else if (data != null && data.ActiveQuests != null)
        {
            activeQuestCount = data.ActiveQuests.keys.Count;
        }

        AddRow("Toplam Oyun Süresi", playTimeStr, normalValueColor);
        AddRow("Aktif Görevler", $"{activeQuestCount} Görev", normalValueColor);
        AddRow("Tamamlanan Görevler", $"{completedQuestCount} Görev", buffValueColor);

        // Rebuild layout ve canvas güncelle
        if (statListContent is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void EnsureLayoutComponents(Transform contentTransform)
    {
        if (contentTransform == null) return;

        // Content uzerinde VerticalLayoutGroup var mi kontrol et, yoksa ekle/ayarla
        VerticalLayoutGroup layout = contentTransform.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 4f;

        // Content uzerinde ContentSizeFitter var mi kontrol et, yoksa ekle/ayarla
        ContentSizeFitter fitter = contentTransform.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentTransform.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void AddHeader(string title, Color color)
    {
        GameObject obj = Instantiate(statRowPrefab, statListContent, false);
        obj.SetActive(true);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition3D = Vector3.zero;
        }

        LayoutElement layoutElement = obj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = obj.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 36f;
        layoutElement.preferredHeight = 40f;

        StatRowUI row = obj.GetComponent<StatRowUI>();
        if (row != null)
        {
            row.SetupAsHeader(title, color);
            spawnedRows.Add(row);
        }
    }

    private void AddRow(string label, string value, Color valColor)
    {
        GameObject obj = Instantiate(statRowPrefab, statListContent, false);
        obj.SetActive(true);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition3D = Vector3.zero;
        }

        LayoutElement layoutElement = obj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = obj.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = 32f;
        layoutElement.preferredHeight = 36f;

        StatRowUI row = obj.GetComponent<StatRowUI>();
        if (row != null)
        {
            row.Setup(label, value, valColor);
            spawnedRows.Add(row);
        }
    }

    private float GetAxeMultiplierForSeason(MevsimTuru season, SeasonSettings settings)
    {
        if (settings == null) return 1f;
        switch (season)
        {
            case MevsimTuru.Ilkbahar: return settings.ilkbaharBaltaHasari;
            case MevsimTuru.Yaz: return settings.yazBaltaHasari;
            case MevsimTuru.Sonbahar: return settings.sonbaharBaltaHasari;
            case MevsimTuru.Kis: return settings.kisBaltaHasari;
            default: return 1f;
        }
    }

    private int GetEquippedAxeTier(string axeId)
    {
        PlayerData data = CloudSaveManager.Instance != null ? CloudSaveManager.Instance.CurrentData : null;

        // 1. Kaydedilmiş kuşanılan balta tercihi (EquippedAxe) varsa onu al
        if (data != null && data.ItemLevels != null && data.ItemLevels.ContainsKey("EquippedAxe"))
        {
            return data.GetItemLevel("EquippedAxe");
        }

        // 2. Kuşanılan kaydedilmemişse, en yüksek açılmış balta seviyesini al
        if (data != null)
        {
            int maxAxeLevel = data.GetItemLevel("Axe");
            if (maxAxeLevel > 1) return maxAxeLevel;
        }

        // 3. AxeTablePanelController aktif kuşanılan seviyesi varsa onu al
        if (AxeTablePanelController.Instance != null && AxeTablePanelController.Instance.CurrentlyEquippedTier > 1)
        {
            return AxeTablePanelController.Instance.CurrentlyEquippedTier;
        }

        // 4. UpgradeManager seviyesi
        if (UpgradeManager.Instance != null)
        {
            int upgradeLvl = UpgradeManager.Instance.GetItemLevel("Axe");
            if (upgradeLvl > 1) return upgradeLvl;
        }

        // 5. Görsel ID'den çözümle (axe_tier_X)
        if (!string.IsNullOrEmpty(axeId) && axeId.StartsWith("axe_tier_"))
        {
            if (int.TryParse(axeId.Replace("axe_replace_", "").Replace("axe_tier_", ""), out int t))
                return t;
        }

        return 1;
    }


    private float GetAxePowerForTier(int tier)
    {
        if (UpgradeManager.Instance != null)
        {
            var configField = typeof(UpgradeManager).GetField("configs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                var configs = configField.GetValue(UpgradeManager.Instance) as List<ItemUpgradeConfig>;
                if (configs != null)
                {
                    var axeConfig = configs.Find(c => c != null && c.ItemID == "Axe");
                    if (axeConfig != null)
                        return axeConfig.GetStatValue(tier);
                }
            }
        }
        return 10f + (tier - 1) * 5f;
    }

    private float GetTreeGrowthMultiplierForSeason(MevsimTuru season, SeasonSettings settings)
    {
        if (settings == null) return 1f;
        switch (season)
        {
            case MevsimTuru.Ilkbahar: return settings.ilkbaharAgacBuyumeHizi;
            case MevsimTuru.Kis: return settings.kisAgacBuyumeHizi;
            default: return 1.0f;
        }
    }

    private void ClearSpawnedRows()
    {
        if (statListContent != null)
        {
            for (int i = statListContent.childCount - 1; i >= 0; i--)
            {
                Transform child = statListContent.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
        spawnedRows.Clear();
    }

    private void PlayButtonClickSFX()
    {
        if (!string.IsNullOrEmpty(buttonSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonSfxName);
        }
    }
}
