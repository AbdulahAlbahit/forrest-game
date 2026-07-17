using System;
using UnityEngine;

public class WorkshopManager : MonoBehaviour
{
    public static WorkshopManager Instance { get; private set; }

    [Header("Atolye Ayarlari")]
    [Tooltip("Bir odunu islemek icin gereken sure (saniye cinsinden).")]
    [SerializeField] private float uretimHizi = 2f;

    [Tooltip("Atolyenin ayni anda kuyrukta bekletebilecegi maksimum odun sayisi.")]
    [SerializeField] private int kuyrukKapasitesi = 10;

    private int mevcutKuyruk;
    private float uretimZamanlayicisi;

    /// <summary>
    /// Atolye kuyrugu degistiginde tetiklenir. (mevcutKuyruk, kuyrukKapasitesi)
    /// </summary>
    public event Action<int, int> OnQueueChanged;

    /// <summary>
    /// Bir odun islenip urune donustugunde tetiklenir.
    /// </summary>
    public event Action OnWoodProcessed;

    public int MevcutKuyruk => mevcutKuyruk;
    public int KuyrukKapasitesi => kuyrukKapasitesi;
    public float UretimHizi => uretimHizi;
    
    public bool IsQueueFull => mevcutKuyruk >= kuyrukKapasitesi;
    public bool IsQueueEmpty => mevcutKuyruk <= 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        mevcutKuyruk = 0;
        uretimZamanlayicisi = 0f;
    }

    private void Update()
    {
        ProcessQueue();
    }

    /// <summary>
    /// Atolyeye islenmesi icin odun ekler.
    /// Eger kapasite doluysa false doner ve eklemez.
    /// </summary>
    public bool AddWoodToQueue()
    {
        if (IsQueueFull)
        {
            Debug.LogWarning("WorkshopManager: Kuyruk dolu! Odun eklenemiyor.");
            return false;
        }

        mevcutKuyruk++;
        Debug.Log($"WorkshopManager: Kuyruga 1 odun eklendi. Durum: {mevcutKuyruk}/{kuyrukKapasitesi}");
        OnQueueChanged?.Invoke(mevcutKuyruk, kuyrukKapasitesi);
        
        return true;
    }

    /// <summary>
    /// Kuyrukta odun varsa, uretim hizina bagli olarak onlari isler.
    /// </summary>
    private void ProcessQueue()
    {
        if (IsQueueEmpty)
        {
            // Eger kuyruk bossa zamanlayiciyi sifirla
            if (uretimZamanlayicisi > 0)
            {
                uretimZamanlayicisi = 0f;
            }
            return;
        }

        uretimZamanlayicisi += Time.deltaTime;

        if (uretimZamanlayicisi >= uretimHizi)
        {
            // Bir odun islendi
            mevcutKuyruk--;
            uretimZamanlayicisi -= uretimHizi;

            Debug.Log($"WorkshopManager: 1 odun islendi! Kalan: {mevcutKuyruk}/{kuyrukKapasitesi}");
            
            OnWoodProcessed?.Invoke();
            OnQueueChanged?.Invoke(mevcutKuyruk, kuyrukKapasitesi);
        }
    }

    /// <summary>
    /// Isleme suresinin (0.0 ~ 1.0) yuzdesini dondurur.
    /// HUD'daki uretim bari icin kullanilabilir.
    /// </summary>
    public float GetProcessingProgress()
    {
        if (IsQueueEmpty) return 0f;
        return Mathf.Clamp01(uretimZamanlayicisi / uretimHizi);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
