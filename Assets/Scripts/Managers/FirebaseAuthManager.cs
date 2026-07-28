using System;
using UnityEngine;
using ForrestGame.Utils;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseUser currentUser;




    public event Action OnLoginSuccess;




    public event Action<string> OnLoginFailed;




    public FirebaseUser CurrentUser => currentUser;




    public bool IsFirebaseReady { get; private set; }




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
        GameLogger.Log("FirebaseAuthManager: Firebase bagimliliklari kontrol ediliyor...");
        InitializeFirebase();
    }




    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                GameLogger.Log("FirebaseAuthManager: Firebase basariyla hazirlandi!");

                auth = FirebaseAuth.DefaultInstance;
                IsFirebaseReady = true;


                if (auth.CurrentUser != null)
                {

                    currentUser = auth.CurrentUser;
                    GameLogger.Log("FirebaseAuthManager: Mevcut oyuncu bulundu, yeniden giris yapilmadi.");
                    GameLogger.Log($"  -> Kullanici ID: {currentUser.UserId}");
                    GameLogger.Log($"  -> Anonim mi: {currentUser.IsAnonymous}");
                    OnLoginSuccess?.Invoke();
                }
                else
                {

                    GameLogger.Log("FirebaseAuthManager: Mevcut oturum bulunamadi, yeni misafir girisi baslatiliyor...");
                    SignInAsGuest();
                }
            }
            else
            {
                string errorMsg = $"Firebase bagimliliklari cozulemedi: {dependencyStatus}";
                GameLogger.LogError($"FirebaseAuthManager: {errorMsg}");
                OnLoginFailed?.Invoke(errorMsg);
            }
        });
    }




    private void SignInAsGuest()
    {
        GameLogger.Log("FirebaseAuthManager: Misafir girisi yapiliyor...");

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                string errorMsg = "Misafir girisi iptal edildi.";
                GameLogger.LogWarning($"FirebaseAuthManager: {errorMsg}");
                OnLoginFailed?.Invoke(errorMsg);
                return;
            }

            if (task.IsFaulted)
            {
                string errorMsg = $"Misafir girisi sirasinda hata olustu: {task.Exception?.Message}";
                GameLogger.LogError($"FirebaseAuthManager: {errorMsg}");
                OnLoginFailed?.Invoke(errorMsg);
                return;
            }


            AuthResult result = task.Result;
            currentUser = result.User;

            GameLogger.Log($"FirebaseAuthManager: Misafir girisi basarili!");
            GameLogger.Log($"  -> Kullanici ID: {currentUser.UserId}");
            GameLogger.Log($"  -> Anonim mi: {currentUser.IsAnonymous}");

            OnLoginSuccess?.Invoke();
        });
    }




    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            currentUser = null;
            GameLogger.Log("FirebaseAuthManager: Oturum kapatildi.");
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
