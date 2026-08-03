using System;
using System.Threading.Tasks;
using UnityEngine;
using ForrestGame.Utils;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }

    // Google Sign-In Web Client ID (Firebase Authentication -> Google -> Web SDK configuration)
    private const string WebClientId = "260516912993-7e4abgnb4ovvffg6d99a7fkfdfea8t3v.apps.googleusercontent.com";

    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private GoogleSignInConfiguration googleSignInConfig;

    // Misafir / Firebase oturumu eventleri
    public event Action OnLoginSuccess;
    public event Action<string> OnLoginFailed;

    // Google Sign-In eventleri
    public event Action OnGoogleSignInSuccess;
    public event Action<string> OnGoogleSignInFailed;

    public FirebaseUser CurrentUser => currentUser;
    public bool IsFirebaseReady { get; private set; }
    public bool IsLoggedIn => currentUser != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // DİKKAT: gameObject'i silersek ayni objede bulunan EconomyManager vb. de silinir!
            // Bu yuzden sadece bu script'in kopyasini siliyoruz.
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameLogger.Log("FirebaseAuthManager: Firebase bagimliliklari kontrol ediliyor...");

        // Google Sign-In yapilandirmasi
        googleSignInConfig = new GoogleSignInConfiguration
        {
            WebClientId = WebClientId,
            RequestIdToken = true,
            RequestEmail = true
        };

        InitializeFirebase();
    }

    // ═══════════════════════════════════════════
    //  Firebase Baslangic
    // ═══════════════════════════════════════════

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
                    // Daha once giris yapilmis, tekrar sormuyoruz
                    currentUser = auth.CurrentUser;
                    GameLogger.Log("FirebaseAuthManager: Mevcut oyuncu bulundu, yeniden giris yapilmadi.");
                    GameLogger.Log($"  -> Kullanici ID: {currentUser.UserId}");
                    GameLogger.Log($"  -> Anonim mi: {currentUser.IsAnonymous}");
                    string loginMethod = currentUser.IsAnonymous ? "guest" : "google";
                    ForrestGame.Managers.AnalyticsManager.Instance?.LogUserLogin(loginMethod, currentUser.UserId);
                    OnLoginSuccess?.Invoke();
                }
                else
                {
                    // Hic giris yapilmamis, oyuncu secmeli (Misafir veya Google)
                    GameLogger.Log("FirebaseAuthManager: Mevcut oturum bulunamadi, giris bekleniyor...");
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

    // ═══════════════════════════════════════════
    //  Misafir Girisi
    // ═══════════════════════════════════════════

    public async void SignInAsGuest()
    {
        GameLogger.Log("FirebaseAuthManager: Misafir girisi yapiliyor...");

        try
        {
            AuthResult result = await auth.SignInAnonymouslyAsync();
            currentUser = result.User;

            GameLogger.Log($"FirebaseAuthManager: Misafir girisi basarili!");
            GameLogger.Log($"  -> Kullanici ID: {currentUser.UserId}");
            GameLogger.Log($"  -> Anonim mi: {currentUser.IsAnonymous}");

            ForrestGame.Managers.AnalyticsManager.Instance?.LogUserLogin("guest", currentUser.UserId);
            OnLoginSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Misafir girisi sirasinda hata olustu: {ex.GetBaseException().Message}";
            GameLogger.LogError($"FirebaseAuthManager: {errorMsg}");
            OnLoginFailed?.Invoke(errorMsg);
        }
    }

    // ═══════════════════════════════════════════
    //  Google Sign-In
    // ═══════════════════════════════════════════

    public async void SignInWithGoogle()
    {
        GameLogger.Log("FirebaseAuthManager: Google ile giris baslatiliyor...");

        GoogleSignIn.Configuration = googleSignInConfig;
        
        try
        {
            GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignIn();
            string idToken = googleUser.IdToken;
            
            GameLogger.Log("FirebaseAuthManager: Google ID Token alindi, Firebase'e gonderiliyor...");

            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);
            FirebaseSignInWithCredential(credential);
        }
        catch (Exception ex)
        {
            string errorMsg = $"Google girisi sirasinda hata olustu: {ex.GetBaseException().Message}";
            GameLogger.LogError($"FirebaseAuthManager: {errorMsg}");
            OnGoogleSignInFailed?.Invoke(errorMsg);
        }
    }

    private async void FirebaseSignInWithCredential(Credential credential)
    {
        try
        {
            // Firebase'in bu ara sürümünde SignInAnonymouslyAsync AuthResult döndürürken, 
            // SignInWithCredentialAsync direkt FirebaseUser döndürüyor!
            FirebaseUser user = await auth.SignInWithCredentialAsync(credential);
            currentUser = user;

            GameLogger.Log("FirebaseAuthManager: Google ile giris basarili!");
            GameLogger.Log($"  -> Kullanici ID: {currentUser.UserId}");
            GameLogger.Log($"  -> Email: {currentUser.Email}");

            ForrestGame.Managers.AnalyticsManager.Instance?.LogUserLogin("google", currentUser.UserId);
            OnGoogleSignInSuccess?.Invoke();
            OnLoginSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Firebase kimlik dogrulama hatasi: {ex.GetBaseException().Message}";
            GameLogger.LogError($"FirebaseAuthManager: {errorMsg}");
            OnGoogleSignInFailed?.Invoke(errorMsg);
        }
    }

    // ═══════════════════════════════════════════
    //  Cikis
    // ═══════════════════════════════════════════

    public void SignOut()
    {
        if (auth != null)
        {
            // Google oturumunu da kapat
            if (GoogleSignIn.DefaultInstance != null)
            {
                GoogleSignIn.DefaultInstance.SignOut();
            }
            
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
