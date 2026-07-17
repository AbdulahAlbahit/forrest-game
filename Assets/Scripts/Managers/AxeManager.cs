using System;
using UnityEngine;

public class AxeManager : MonoBehaviour
{
    public static AxeManager Instance { get; private set; }

    [Header("Balta Ayarlari")]
    [Tooltip("Seviye 1'deki temel balta gucu (agac basina dusen odun).")]
    [SerializeField] private float temelGuc = 1f;

    [Tooltip("Her balta seviyesinde eklenen ekstra guc miktari.")]
    [SerializeField] private float seviyeCarpani = 0.5f;

    /// <summary>
    /// Balta seviyesi yukseltildiginde tetiklenir. Yeni seviyeyi parametre olarak verir.
    /// </summary>
    public event Action<int> OnAxeUpgraded;

    /// <summary>
    /// Mevcut balta seviyesini dogrudan bulut verisinden dondurur.
    /// </summary>
    public int BaltaSeviyesi
    {
        get
        {
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.CurrentData != null)
                return CloudSaveManager.Instance.CurrentData.BaltaSeviyesi;
            return 1;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Balta seviyesine bagli olarak agac basina ne kadar odun dusecegini hesaplar.
    /// Formul: temelGuc + (BaltaSeviyesi * seviyeCarpani)
    /// Ornek (temel=1, carpan=0.5):
    ///   Seviye 1: 1.5 guc
    ///   Seviye 2: 2.0 guc
    ///   Seviye 3: 2.5 guc
    ///   Seviye 4: 3.0 guc
    /// </summary>
    public float GetAxePower()
    {
        return temelGuc + (BaltaSeviyesi * seviyeCarpani);
    }

    /// <summary>
    /// Baltayi bir seviye yukseltir. Maliyet EconomyManager uzerinden kontrol edilir.
    /// Yeterli para varsa seviyeyi artirip buluta kaydeder.
    /// </summary>
    public bool UpgradeAxe(int cost)
    {
        if (EconomyManager.Instance == null)
        {
            Debug.LogError("AxeManager: EconomyManager bulunamadi!");
            return false;
        }

        if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null)
        {
            Debug.LogWarning("AxeManager: CloudSaveManager veya CurrentData hazir degil.");
            return false;
        }

        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            Debug.LogWarning($"AxeManager: Balta yukseltmek icin yeterli para yok! Gereken: {cost}");
            return false;
        }

        PlayerData data = CloudSaveManager.Instance.CurrentData;
        data.BaltaSeviyesi++;

        CloudSaveManager.Instance.SaveData(data);

        Debug.Log($"AxeManager: Balta yukseltildi! Yeni Seviye: {data.BaltaSeviyesi}, Yeni Guc: {GetAxePower()}");
        OnAxeUpgraded?.Invoke(data.BaltaSeviyesi);

        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
