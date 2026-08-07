using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    // --- Temel Oyuncu Verileri ---
    public int Para;
    public int XP;
    public int OyuncuSeviyesi;
    public long LastSaveTime = 0;

    // --- Jenerik Upgrade Seviyeleri ---
    // Tum esyalarin seviyeleri bu sozlukte tutulur.
    // Ornek: ItemLevels["Axe"] = 3, ItemLevels["Basket"] = 5
    public SerializableDict ItemLevels;

    // --- Zaman Verileri ---
    public float MevsimZamani;
    public float OyunSuresi;

    // --- Acilan Bolgeler ---
    public List<string> AcilanBolgeler;

    // --- Odun Stoklari ---
    /// <summary>
    /// Oyuncunun ayri "ambar" deposu (EfeWoodStorageBridge ile beslenir).
    /// Kamyona yuklenen/tasinan odundan (EfeOdunTasima, TruckRouteController) BAGIMSIZDIR;
    /// atolye ile de dogrudan iliskisi YOKTUR.
    /// </summary>
    public int PlayerWoodStock;

    /// <summary>
    /// Atolye ham odun stogu. SADECE kamyonun atolyeye bıraktığı odunları tutar.
    /// Uretim baslarken buradan duser, PlayerWoodStock ile karistirilmamalidir.
    /// </summary>
    public int WorkshopRawWood;

    // --- Diger Core System Verileri ---
    public SerializableDict ShopInventory;
    public SerializableDict WorkshopQueue;
    public SerializableDict ReadyForTruck;

    /// <summary>
    /// Musteri siparislerinin FIFO kuyrugu (once acilan once karsilanir).
    /// </summary>
    public List<CustomerOrderData> PendingCustomerOrders;
    public int NextCustomerOrderId;

    /// <summary>
    /// Son gunluk siparis sifirlamasinin yapildigi UTC gun numarasi (DateTime.UtcNow.DayOfYear bazli).
    /// Her 24 saatte bir PendingCustomerOrders bu sayac kontrol edilerek sifirlanir.
    /// </summary>
    public long LastOrderResetTime;

    // --- Gorev (Quest) Verileri ---
    public long LastQuestResetTime;
    /// <summary>Gorev ilerleme sayaclari. Ornek: ActiveQuests["satilan_masa"] = 4</summary>
    public SerializableDict ActiveQuests;
    /// <summary>Odulu alinmis gorev ID'lerinin listesi. Ornek: ["q_ilk_masa", "q_buyuk_satis"]</summary>
    public List<string> CompletedQuests;

    public PlayerData()
    {
        Para = 0;
        XP = 0;
        OyuncuSeviyesi = 1;

        ItemLevels = new SerializableDict();

        MevsimZamani = 0f;
        OyunSuresi = 0f;

        AcilanBolgeler = new List<string>();
        
        PlayerWoodStock = 0;
        WorkshopRawWood = 0;
        ShopInventory = new SerializableDict();
        WorkshopQueue = new SerializableDict();
        ReadyForTruck = new SerializableDict();
        PendingCustomerOrders = new List<CustomerOrderData>();
        NextCustomerOrderId = 1;
        LastOrderResetTime = 0;
        LastQuestResetTime = 0;
        ActiveQuests = new SerializableDict();
        CompletedQuests = new List<string>();
    }

    /// <summary>
    /// Belirtilen esyanin mevcut seviyesini dondurur.
    /// Esya henuz kaydedilmemisse varsayilan olarak 1 dondurur.
    /// </summary>
    public int GetItemLevel(string itemId)
    {
        if (ItemLevels == null)
        {
            ItemLevels = new SerializableDict();
        }
        return ItemLevels.Get(itemId, 1);
    }

    /// <summary>
    /// Belirtilen esyanin seviyesini ayarlar.
    /// </summary>
    public void SetItemLevel(string itemId, int level)
    {
        if (ItemLevels == null)
        {
            ItemLevels = new SerializableDict();
        }
        ItemLevels.Set(itemId, level);
    }
}
