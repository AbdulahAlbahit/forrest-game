using System;
using UnityEngine;
using Firebase;
using Firebase.Auth;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseUser currentUser;

    /// <summary>
    /// Giris basarili oldugunda tetiklenir.
    /// </summary>
    public event Action OnLoginSuccess;

    /// <summary>
    /// Giris basarisiz oldugunda hata mesajiyla tetiklenir.
    /// </summary>
    public event Action<string> OnLoginFailed;

    /// <summary>
    /// Mevcut oturum acmis kullaniciyi dondurur. Giris yapilmamissa null doner.
    /// </summary>
    public FirebaseUser CurrentUser => currentUser;

    /// <summary>
    /// Firebase'in hazir olup olmadigini belirtir.
    /// </summary>
    public bool IsFirebaseReady { get; private set; }

    /// <summary>
    /// Kullanicinin giris yapip yapmadigini belirtir.
    /// </summary>
    public bool IsLoggedIn => currentUser != null;

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

    private void Start()
    {
        Debug.Log("FirebaseAuthManager: Firebase bagimliliklari kontrol ediliyor...");
        InitializeFirebase();
    }

    /// <summary>
    /// Firebase bagimliliklarini kontrol eder ve hazirsa anonim giris baslatir.
    /// </summary>
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            DependencyStatus dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("FirebaseAuthManager: Firebase basariyla hazirlandi!");

                auth = FirebaseAuth.DefaultInstance;
                IsFirebaseReady = true;

                // Firebase hazir, anonim giris baslatiliyor
                SignInAsGuest();
            }
            else
            {
                string errorMsg = $"Firebase bagimliliklari cozulemedi: {dependencyStatus}";
                Debug.LogError($"FirebaseAuthManager: {errorMsg}");
                OnLoginFailed?.Invoke(errorMsg);
            }
        });
    }

    /// <summary>
    /// Anonim (misafir) giris islemi baslatir.
    /// </summary>
    private void SignInAsGuest()
    {
        Debug.Log("FirebaseAuthManager: Misafir girisi yapiliyor...");

        auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                string errorMsg = "Misafir girisi iptal edildi.";
                Debug.LogWarning($"FirebaseAuthManager: {errorMsg}");
                OnLoginFailed?.Invoke(errorMsg);
                return;
            }

            if (task.IsFaulted)
            {
                string errorMsg = $"Misafir girisi sirasinda hata olustu: {task.Exception?.Message}";
                Debug.LogError($"FirebaseAuthManager: {errorMsg}");
                OnLoginFailed?.Invoke(errorMsg);
                return;
            }

            // Giris basarili
            AuthResult result = task.Result;
            currentUser = result.User;

            Debug.Log($"FirebaseAuthManager: Misafir girisi basarili!");
            Debug.Log($"  -> Kullanici ID: {currentUser.UserId}");
            Debug.Log($"  -> Anonim mi: {currentUser.IsAnonymous}");

            OnLoginSuccess?.Invoke();
        });
    }

    /// <summary>
    /// Mevcut kullanicinin oturumunu kapatir.
    /// </summary>
    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            currentUser = null;
            Debug.Log("FirebaseAuthManager: Oturum kapatildi.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            auth = null;
            Instance = null;
        }
    }
}
