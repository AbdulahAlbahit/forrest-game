using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ForrestGame.Utils;
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




    public event Action<PlayerData> OnDataLoaded;




    public event Action OnDataSaved;




    public event Action<string> OnCloudError;




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
            StartCoroutine(WaitForAuthManager());
        }
    }

    private IEnumerator WaitForAuthManager()
    {
        GameLogger.Log("CloudSaveManager: FirebaseAuthManager bekleniyor...");

        while (FirebaseAuthManager.Instance == null)
        {
            yield return null;
        }

        SubscribeToAuth();
        GameLogger.Log("CloudSaveManager: FirebaseAuthManager bulundu, event aboneligi yapildi.");
    }

    private void SubscribeToAuth()
    {
        if (!isSubscribed)
        {
            FirebaseAuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
            isSubscribed = true;
        }



        if (FirebaseAuthManager.Instance.IsLoggedIn)
        {
            GameLogger.Log("CloudSaveManager: Giris zaten tamamlanmis, dogrudan Firestore baslatiliyor.");
            HandleLoginSuccess();
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




    private void HandleLoginSuccess()
    {
        GameLogger.Log("CloudSaveManager: Giris basarili, Firestore baglantisi kuruluyor...");

        db = FirebaseFirestore.DefaultInstance;
        userId = FirebaseAuthManager.Instance.CurrentUser.UserId;

        GameLogger.Log($"CloudSaveManager: Firestore hazir. Kullanici ID: {userId}");


        LoadData();


        if (autoSaveCoroutine != null)
            StopCoroutine(autoSaveCoroutine);
        autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
    }





    private DocumentReference GetPlayerDocument()
    {
        return db.Collection("players").Document(userId);
    }





    public void SaveData(PlayerData data)
    {
        if (data == null)
        {
            GameLogger.LogError("CloudSaveManager: SaveData'ya null PlayerData gonderildi!");
            return;
        }


        data.LastSaveTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();



        SaveToLocal(data);


        if (db == null || string.IsNullOrEmpty(userId))
        {
            string errorMsg = "Firestore baglantisi hazir degil veya kullanici girisi yapilmamis. Sadece lokal kayit yapildi.";
            GameLogger.LogWarning($"CloudSaveManager: {errorMsg}");
            OnCloudError?.Invoke(errorMsg);
            return;
        }

        GameLogger.Log("CloudSaveManager: Veriler buluta kaydediliyor...");

        Dictionary<string, object> playerDict = new Dictionary<string, object>
        {
            { "Para", data.Para },
            { "XP", data.XP },
            { "OyuncuSeviyesi", data.OyuncuSeviyesi },
            { "MevsimZamani", data.MevsimZamani },
            { "OyunSuresi", data.OyunSuresi },
            { "WorkshopRawWood", data.WorkshopRawWood },
            { "AcilanBolgeler", data.AcilanBolgeler },
            { "SonGuncelleme", FieldValue.ServerTimestamp },
            { "LastSaveTime", data.LastSaveTime }
        };


        if (data.ItemLevels != null)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach(string key in data.ItemLevels.keys) dict[key] = data.ItemLevels.Get(key);
            playerDict["ItemLevels"] = dict;
        }
        

        if (data.ShopInventory != null)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach(string key in data.ShopInventory.keys) dict[key] = data.ShopInventory.Get(key);
            playerDict["ShopInventory"] = dict;
        }
        
        if (data.WorkshopQueue != null)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach(string key in data.WorkshopQueue.keys) dict[key] = data.WorkshopQueue.Get(key);
            playerDict["WorkshopQueue"] = dict;
        }

        if (data.ReadyForTruck != null)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach(string key in data.ReadyForTruck.keys) dict[key] = data.ReadyForTruck.Get(key);
            playerDict["ReadyForTruck"] = dict;
        }

        if (data.PendingCustomerOrders != null)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach(string key in data.PendingCustomerOrders.keys) dict[key] = data.PendingCustomerOrders.Get(key);
            playerDict["PendingCustomerOrders"] = dict;
        }

        if (data.ActiveQuests != null)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach(string key in data.ActiveQuests.keys) dict[key] = data.ActiveQuests.Get(key);
            playerDict["ActiveQuests"] = dict;
        }


        if (data.CompletedQuests != null)
        {
            playerDict["CompletedQuests"] = data.CompletedQuests;
        }

        GetPlayerDocument().SetAsync(playerDict).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMsg = $"Buluta kayit sirasinda hata olustu: {task.Exception?.Message}";
                GameLogger.LogError($"CloudSaveManager: {errorMsg}");
                OnCloudError?.Invoke(errorMsg);
                return;
            }

            CurrentData = data;
            GameLogger.Log("CloudSaveManager: Veriler buluta basariyla kaydedildi!");
            GameLogger.Log($"  -> Para: {data.Para}, XP: {data.XP}, Seviye: {data.OyuncuSeviyesi}");
            OnDataSaved?.Invoke();
        });
    }






    public void LoadData()
    {
        if (db == null || string.IsNullOrEmpty(userId))
        {
            GameLogger.LogWarning("CloudSaveManager: Firestore baglantisi hazir degil. Lokal veri deneniyor...");
            LoadFromLocalFallback();
            return;
        }

        GameLogger.Log("CloudSaveManager: Buluttan veriler yukleniyor...");

        GetPlayerDocument().GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                string errorMsg = $"Buluttan veri yuklenirken hata olustu: {task.Exception?.Message}";
                GameLogger.LogError($"CloudSaveManager: {errorMsg}");
                OnCloudError?.Invoke(errorMsg);


                LoadFromLocalFallback();
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (snapshot.Exists)
            {
                GameLogger.Log("CloudSaveManager: Bulutta mevcut veri bulundu, yukleniyor...");

                PlayerData data = new PlayerData();
                Dictionary<string, object> dict = snapshot.ToDictionary();

                if (dict.ContainsKey("Para")) data.Para = Convert.ToInt32(dict["Para"]);
                if (dict.ContainsKey("XP")) data.XP = Convert.ToInt32(dict["XP"]);
                if (dict.ContainsKey("OyuncuSeviyesi")) data.OyuncuSeviyesi = Convert.ToInt32(dict["OyuncuSeviyesi"]);


                if (dict.ContainsKey("MevsimZamani")) data.MevsimZamani = Convert.ToSingle(dict["MevsimZamani"]);
                if (dict.ContainsKey("OyunSuresi")) data.OyunSuresi = Convert.ToSingle(dict["OyunSuresi"]);
                if (dict.ContainsKey("WorkshopRawWood")) data.WorkshopRawWood = Convert.ToInt32(dict["WorkshopRawWood"]);


                if (dict.ContainsKey("AcilanBolgeler") && dict["AcilanBolgeler"] is List<object> bolgelerRaw)
                {
                    data.AcilanBolgeler = new List<string>();
                    foreach (object bolge in bolgelerRaw)
                    {
                        data.AcilanBolgeler.Add(bolge.ToString());
                    }
                }

                if (dict.ContainsKey("LastSaveTime")) 
                {
                    data.LastSaveTime = Convert.ToInt64(dict["LastSaveTime"]);
                }





                data.ItemLevels = new SerializableDict();
                bool needsMigration = false;


                if (dict.ContainsKey("ItemLevels") && dict["ItemLevels"] is Dictionary<string, object> itemLevelsRaw)
                {
                    data.ItemLevels.FromDictionary(itemLevelsRaw);
                    GameLogger.Log($"CloudSaveManager: ItemLevels yuklendi ({itemLevelsRaw.Count} esya).");
                }


                data.ShopInventory = new SerializableDict();
                if (dict.ContainsKey("ShopInventory") && dict["ShopInventory"] is Dictionary<string, object> shopInventoryRaw)
                {
                    data.ShopInventory.FromDictionary(shopInventoryRaw);
                }

                data.WorkshopQueue = new SerializableDict();
                if (dict.ContainsKey("WorkshopQueue") && dict["WorkshopQueue"] is Dictionary<string, object> workshopQueueRaw)
                {
                    data.WorkshopQueue.FromDictionary(workshopQueueRaw);
                }

                data.ReadyForTruck = new SerializableDict();
                if (dict.ContainsKey("ReadyForTruck") && dict["ReadyForTruck"] is Dictionary<string, object> readyForTruckRaw)
                {
                    data.ReadyForTruck.FromDictionary(readyForTruckRaw);
                }

                data.PendingCustomerOrders = new SerializableDict();
                if (dict.ContainsKey("PendingCustomerOrders") && dict["PendingCustomerOrders"] is Dictionary<string, object> pendingRaw)
                {
                    data.PendingCustomerOrders.FromDictionary(pendingRaw);
                }

                data.ActiveQuests = new SerializableDict();
                if (dict.ContainsKey("ActiveQuests") && dict["ActiveQuests"] is Dictionary<string, object> activeQuestsRaw)
                {
                    data.ActiveQuests.FromDictionary(activeQuestsRaw);
                }


                data.CompletedQuests = new System.Collections.Generic.List<string>();
                if (dict.ContainsKey("CompletedQuests") && dict["CompletedQuests"] is System.Collections.Generic.List<object> completedRaw)
                {
                    foreach (object id in completedRaw)
                        data.CompletedQuests.Add(id.ToString());
                }



                if (dict.ContainsKey("BaltaSeviyesi") && !data.ItemLevels.ContainsKey("Axe"))
                {
                    data.ItemLevels.Set("Axe", Convert.ToInt32(dict["BaltaSeviyesi"]));
                    needsMigration = true;
                }
                if (dict.ContainsKey("SepetSeviyesi") && !data.ItemLevels.ContainsKey("Basket"))
                {
                    data.ItemLevels.Set("Basket", Convert.ToInt32(dict["SepetSeviyesi"]));
                    needsMigration = true;
                }
                if (dict.ContainsKey("KamyonSeviyesi") && !data.ItemLevels.ContainsKey("Truck"))
                {
                    data.ItemLevels.Set("Truck", Convert.ToInt32(dict["KamyonSeviyesi"]));
                    needsMigration = true;
                }
                if (dict.ContainsKey("AtolyeSeviyesi") && !data.ItemLevels.ContainsKey("Workshop"))
                {
                    data.ItemLevels.Set("Workshop", Convert.ToInt32(dict["AtolyeSeviyesi"]));
                    needsMigration = true;
                }

                if (needsMigration)
                {
                    GameLogger.LogWarning("CloudSaveManager: [MIGRATION] Eski format tespit edildi! Veriler yeni ItemLevels yapisina aktarildi.");
                }




                PlayerData localData = GetLocalData();
                if (localData != null && localData.LastSaveTime > data.LastSaveTime)
                {
                    GameLogger.LogWarning($"CloudSaveManager: [Cakisma] Lokal veri (Time: {localData.LastSaveTime}) buluttaki veriden (Time: {data.LastSaveTime}) daha guncel! Lokal veri korunuyor ve buluta yaziliyor...");
                    CurrentData = localData;
                    

                    SaveData(localData);
                    
                    OnDataLoaded?.Invoke(localData);
                    return;
                }

                CurrentData = data;


                SaveToLocal(data);


                if (needsMigration)
                {
                    SaveData(data);
                    GameLogger.Log("CloudSaveManager: [MIGRATION] Yeni format buluta kaydedildi.");
                }

                GameLogger.Log("CloudSaveManager: Veriler basariyla yuklendi!");
                OnDataLoaded?.Invoke(data);
            }
            else
            {
                GameLogger.Log("CloudSaveManager: Bulutta veri bulunamadi. Yeni oyuncu verisi olusturuluyor...");

                PlayerData newData = new PlayerData();
                CurrentData = newData;


                SaveData(newData);

                GameLogger.Log("CloudSaveManager: Yeni oyuncu verisi olusturuldu ve buluta kaydedildi.");
                OnDataLoaded?.Invoke(newData);
            }
        });
    }









    private void SaveToLocal(PlayerData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(LOCAL_SAVE_KEY, json);
            PlayerPrefs.Save();
            GameLogger.Log("CloudSaveManager: Lokal yedek guncellendi (PlayerPrefs).");
        }
        catch (Exception e)
        {
            GameLogger.LogError($"CloudSaveManager: Lokal kayit sirasinda hata: {e.Message}");
        }
    }

    private void LoadFromLocalFallback()
    {
        PlayerData localData = GetLocalData();
        if (localData != null)
        {
            CurrentData = localData;
            GameLogger.LogWarning("CloudSaveManager: Lokal yedekten veri yuklendi (Offline mod).");
            OnDataLoaded?.Invoke(localData);
            return;
        }


        GameLogger.LogWarning("CloudSaveManager: Lokal yedek bulunamadi. Yeni oyuncu verisi olusturuluyor...");
        PlayerData newData = new PlayerData();
        CurrentData = newData;
        OnDataLoaded?.Invoke(newData);
    }




    public PlayerData GetLocalData()
    {
        if (PlayerPrefs.HasKey(LOCAL_SAVE_KEY))
        {
            try
            {
                string json = PlayerPrefs.GetString(LOCAL_SAVE_KEY);
                PlayerData localData = JsonUtility.FromJson<PlayerData>(json);

                if (localData != null)
                {
                    if (localData.ItemLevels == null) localData.ItemLevels = new SerializableDict();
                    if (localData.ShopInventory == null) localData.ShopInventory = new SerializableDict();
                    if (localData.WorkshopQueue == null) localData.WorkshopQueue = new SerializableDict();
                    if (localData.ReadyForTruck == null) localData.ReadyForTruck = new SerializableDict();
                    if (localData.PendingCustomerOrders == null) localData.PendingCustomerOrders = new SerializableDict();
                    if (localData.ActiveQuests == null) localData.ActiveQuests = new SerializableDict();
                    if (localData.CompletedQuests == null) localData.CompletedQuests = new System.Collections.Generic.List<string>();

                    return localData;
                }
            }
            catch (Exception e)
            {
                GameLogger.LogError($"CloudSaveManager: Lokal veri okuma hatasi: {e.Message}");
            }
        }
        return null;
    }









    private IEnumerator AutoSaveRoutine()
    {
        GameLogger.Log($"CloudSaveManager: Otomatik kayit basladi. Aralik: {autoSaveInterval} saniye.");

        while (true)
        {
            yield return new WaitForSecondsRealtime(autoSaveInterval);

            if (CurrentData != null)
            {
                GameLogger.Log("CloudSaveManager: [Auto-Save] Otomatik kayit tetiklendi...");
                SaveData(CurrentData);
            }
            else
            {
                GameLogger.LogWarning("CloudSaveManager: [Auto-Save] CurrentData null, kayit atlandi.");
            }
        }
    }

}
