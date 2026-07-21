using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance { get; private set; }

    private FirebaseFirestore db;
    private string userId;

    private const string LOCAL_SAVE_KEY = "LocalPlayerData";

    [Header("Otomatik Kayit Ayarlari")]
    [Tooltip("Otomatik bulut kaydinin kac saniyede bir yapilacagi.")]
    [SerializeField] private float autoSaveInterval = 60f;

    private Coroutine autoSaveCoroutine;

    /// <summary>
    /// Bulut verisi basariyla yuklendiginde, yuklenlen PlayerData ile tetiklenir.
    /// </summary>
    public event Action<PlayerData> OnDataLoaded;

    /// <summary>
    /// Buluta kayit basarili oldugunda tetiklenir.
    /// </summary>
    public event Action OnDataSaved;

    /// <summary>
    /// Herhangi bir bulut islemi basarisiz oldugunda hata mesajiyla tetiklenir.
    /// </summary>
    public event Action<string> OnCloudError;

    /// <summary>
    /// Mevcut yuklenmis oyuncu verisini tutar.
    /// </summary>
    public PlayerData CurrentData { get; private set; }

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

    private bool isSubscribed = false;

    private void Start()
    {
        if (FirebaseAuthManager.Instance != null)
        {
            SubscribeToAuth();
        }
        else
        {
            // FirebaseAuthManager henuz hazir degil, hazir olana kadar bekle
            StartCoroutine(WaitForAuthManager());
        }
    }

    private IEnumerator WaitForAuthManager()
    {
        Debug.Log("CloudSaveManager: FirebaseAuthManager bekleniyor...");

        while (FirebaseAuthManager.Instance == null)
        {
            yield return null;
        }

        SubscribeToAuth();
        Debug.Log("CloudSaveManager: FirebaseAuthManager bulundu, event aboneligi yapildi.");
    }

    private void SubscribeToAuth()
    {
        if (!isSubscribed)
        {
            FirebaseAuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
            isSubscribed = true;
        }
    }

    private void OnDestroy()
    {
        if (isSubscribed && FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
        }

        if (Instance == this)
        {
            db = null;
            Instance = null;
        }
    }

    /// <summary>
    /// Giris basarili oldugunda Firestore baglantisini kurar ve veri yukler.
    /// </summary>
    private void HandleLoginSuccess()
    {
        Debug.Log("CloudSaveManager: Giris basarili, Firestore baglantisi kuruluyor...");

        db = FirebaseFirestore.DefaultInstance;
        userId = FirebaseAuthManager.Instance.CurrentUser.UserId;

        Debug.Log($"CloudSaveManager: Firestore hazir. Kullanici ID: {userId}");

        // Giris yapildi, buluttan veriyi cek
        LoadData();

        // Otomatik kayit dongusunu baslat
        if (autoSaveCoroutine != null)
            StopCoroutine(autoSaveCoroutine);
        autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
    }

    /// <summary>
    /// Kullaniciya ait Firestore dokuman referansini dondurur.
    /// Yol: players/{userId}
    /// </summary>
    private DocumentReference GetPlayerDocument()
    {
        return db.Collection("players").Document(userId);
    }

    /// <summary>
    /// Oyuncu verisini Firestore'a kaydeder.
    /// </summary>
    public void SaveData(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogError("CloudSaveManager: SaveData'ya null PlayerData gonderildi!");
            return;
        }

        // --- Lokal Kayit (Offline Save) ---
        // Firestore'dan bagimsiz olarak her zaman yerel yedegi guncelle
        SaveToLocal(data);

        // --- Bulut Kayit (Cloud Save) ---
        if (db == null || string.IsNullOrEmpty(userId))
        {
            string errorMsg = "Firestore baglantisi hazir degil veya kullanici girisi yapilmamis. Sadece lokal kayit yapildi.";
            Debug.LogWarning($"CloudSaveManager: {errorMsg}");
            OnCloudError?.Invoke(errorMsg);
            return;
        }

        Debug.Log("CloudSaveManager: Veriler buluta kaydediliyor...");

        Dictionary<string, object> playerDict = new Dictionary<string, object>
        {
            { "Para", data.Para },
            { "XP", data.XP },
            { "OyuncuSeviyesi", data.OyuncuSeviyesi },
            { "BaltaSeviyesi", data.BaltaSeviyesi },
            { "SepetSeviyesi", data.SepetSeviyesi },
            { "KamyonSeviyesi", data.KamyonSeviyesi },
            { "AtolyeSeviyesi", data.AtolyeSeviyesi },
            { "MevsimZamani", data.MevsimZamani },
            { "OyunSuresi", data.OyunSuresi },
            { "AcilanBolgeler", data.AcilanBolgeler },
            { "SonGuncelleme", FieldValue.ServerTimestamp }
        };

        GetPlayerDocument().SetAsync(playerDict).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMsg = $"Buluta kayit sirasinda hata olustu: {task.Exception?.Message}";
                Debug.LogError($"CloudSaveManager: {errorMsg}");
                OnCloudError?.Invoke(errorMsg);
                return;
            }

            CurrentData = data;
            Debug.Log("CloudSaveManager: Veriler buluta basariyla kaydedildi!");
            Debug.Log($"  -> Para: {data.Para}, XP: {data.XP}, Seviye: {data.OyuncuSeviyesi}");
            OnDataSaved?.Invoke();
        });
    }

    /// <summary>
    /// Oyuncu verisini Firestore'dan yukler.
    /// Eger bulutta veri yoksa yeni bir PlayerData olusturup kaydeder.
    /// </summary>
    public void LoadData()
    {
        if (db == null || string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("CloudSaveManager: Firestore baglantisi hazir degil. Lokal veri deneniyor...");
            LoadFromLocalFallback();
            return;
        }

        Debug.Log("CloudSaveManager: Buluttan veriler yukleniyor...");

        GetPlayerDocument().GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMsg = $"Buluttan veri yuklenirken hata olustu: {task.Exception?.Message}";
                Debug.LogError($"CloudSaveManager: {errorMsg}");
                OnCloudError?.Invoke(errorMsg);

                // Bulut basarisiz oldu, lokal yedege dus
                LoadFromLocalFallback();
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (snapshot.Exists)
            {
                Debug.Log("CloudSaveManager: Bulutta mevcut veri bulundu, yukleniyor...");

                PlayerData data = new PlayerData();
                Dictionary<string, object> dict = snapshot.ToDictionary();

                if (dict.ContainsKey("Para")) data.Para = Convert.ToInt32(dict["Para"]);
                if (dict.ContainsKey("XP")) data.XP = Convert.ToInt32(dict["XP"]);
                if (dict.ContainsKey("OyuncuSeviyesi")) data.OyuncuSeviyesi = Convert.ToInt32(dict["OyuncuSeviyesi"]);
                if (dict.ContainsKey("BaltaSeviyesi")) data.BaltaSeviyesi = Convert.ToInt32(dict["BaltaSeviyesi"]);
                if (dict.ContainsKey("SepetSeviyesi")) data.SepetSeviyesi = Convert.ToInt32(dict["SepetSeviyesi"]);
                if (dict.ContainsKey("KamyonSeviyesi")) data.KamyonSeviyesi = Convert.ToInt32(dict["KamyonSeviyesi"]);
                if (dict.ContainsKey("AtolyeSeviyesi")) data.AtolyeSeviyesi = Convert.ToInt32(dict["AtolyeSeviyesi"]);

                // Zaman verileri
                if (dict.ContainsKey("MevsimZamani")) data.MevsimZamani = Convert.ToSingle(dict["MevsimZamani"]);
                if (dict.ContainsKey("OyunSuresi")) data.OyunSuresi = Convert.ToSingle(dict["OyunSuresi"]);

                if (dict.ContainsKey("AcilanBolgeler") && dict["AcilanBolgeler"] is List<object> bolgelerRaw)
                {
                    data.AcilanBolgeler = new List<string>();
                    foreach (object bolge in bolgelerRaw)
                    {
                        data.AcilanBolgeler.Add(bolge.ToString());
                    }
                }

                CurrentData = data;

                // Buluttan basariyla yuklendi, lokal yedegi de guncelle
                SaveToLocal(data);

                Debug.Log("CloudSaveManager: Veriler basariyla yuklendi!");
                OnDataLoaded?.Invoke(data);
            }
            else
            {
                Debug.Log("CloudSaveManager: Bulutta veri bulunamadi. Yeni oyuncu verisi olusturuluyor...");

                PlayerData newData = new PlayerData();
                CurrentData = newData;

                // Yeni veriyi buluta kaydet
                SaveData(newData);

                Debug.Log("CloudSaveManager: Yeni oyuncu verisi olusturuldu ve buluta kaydedildi.");
                OnDataLoaded?.Invoke(newData);
            }
        });
    }

    // =============================================
    // LOKAL KAYIT / YUKLE YARDIMCI METOTLARI
    // =============================================

    /// <summary>
    /// PlayerData'yi JSON'a cevirip PlayerPrefs'e yazar.
    /// Her SaveData cagrisinda otomatik olarak tetiklenir.
    /// </summary>
    private void SaveToLocal(PlayerData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(LOCAL_SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("CloudSaveManager: Lokal yedek guncellendi (PlayerPrefs).");
        }
        catch (Exception e)
        {
            Debug.LogError($"CloudSaveManager: Lokal kayit sirasinda hata: {e.Message}");
        }
    }

    /// <summary>
    /// Bulut baglantisi basarisiz oldugunda PlayerPrefs'ten veri okuyup oyunu baslatir.
    /// Eger lokal veri de yoksa yeni bir PlayerData olusturur.
    /// </summary>
    private void LoadFromLocalFallback()
    {
        if (PlayerPrefs.HasKey(LOCAL_SAVE_KEY))
        {
            try
            {
                string json = PlayerPrefs.GetString(LOCAL_SAVE_KEY);
                PlayerData localData = JsonUtility.FromJson<PlayerData>(json);

                if (localData != null)
                {
                    CurrentData = localData;
                    Debug.LogWarning("CloudSaveManager: Lokal yedekten veri yuklendi (Offline mod).");
                    OnDataLoaded?.Invoke(localData);
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"CloudSaveManager: Lokal veri okuma hatasi: {e.Message}");
            }
        }

        // Lokal veri de yoksa sifirdan baslat
        Debug.LogWarning("CloudSaveManager: Lokal yedek bulunamadi. Yeni oyuncu verisi olusturuluyor...");
        PlayerData newData = new PlayerData();
        CurrentData = newData;
        OnDataLoaded?.Invoke(newData);
    }

    // =============================================
    // OTOMATIK KAYIT (AUTO-SAVE) COROUTINE
    // =============================================

    /// <summary>
    /// Oyun calistigi surece belirli aralikla (varsayilan 60sn) otomatik bulut kaydi yapar.
    /// HandleLoginSuccess icinde baslatilir.
    /// </summary>
    private IEnumerator AutoSaveRoutine()
    {
        Debug.Log($"CloudSaveManager: Otomatik kayit basladi. Aralik: {autoSaveInterval} saniye.");

        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);

            if (CurrentData != null)
            {
                Debug.Log("CloudSaveManager: [Auto-Save] Otomatik kayit tetiklendi...");
                SaveData(CurrentData);
            }
            else
            {
                Debug.LogWarning("CloudSaveManager: [Auto-Save] CurrentData null, kayit atlandi.");
            }
        }
    }

}
