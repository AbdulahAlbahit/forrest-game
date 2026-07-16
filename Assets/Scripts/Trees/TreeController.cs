using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TreeController : MonoBehaviour
{
    [Header("--- AĞAÇ SAĞLIK DURUMU ---")]
    [Tooltip("Ağacın maksimum canı (Balta Upgrade ile azalacak)")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("--- AĞAÇ GÖRSELLERİ ---")]
    [Tooltip("Sağlam ağaç objesi (Hierarchy'den sürükle - Normalde bu objenin kendisi)")]
    public GameObject healthyTreeVisual;

    [Tooltip("Yerde kalan güdük modeli (stump_roundDetailed prefabı)")]
    public GameObject stumpPrefab;

    [Tooltip("Yan yatan ağaç tepesi modeli (tree_detailed_fall prefabı)")]
    public GameObject fallenTreePrefab;

    [Header("--- EFEKT AYARLARI ---")]
    [Tooltip("Ağaç yıkılınca devrilme animasyonu süresi (saniye)")]
    public float fallDuration = 0.8f;

    [Tooltip("Sahneden kaybolmadan önce ne kadar beklesin? (saniye)")]
    public float disappearDelay = 4f;

    [Tooltip("Ağaç yeniden ne zaman canlanır? 0 = Canlanmaz")]
    public float respawnTime = 0f;

    [Header("--- EFE & YASİR İÇİN PRİZLER ---")]
    [Tooltip("Ağaç her vurulduğunda tetiklenir (Ses efekti bağlamak için)")]
    public UnityEvent OnTreeHit;

    [Tooltip("Ağaç devrildiğinde tetiklenir (Odun spawn için bağla)")]
    public UnityEvent OnTreeFelled;

    private bool isFelled = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Dışarıdan (Karakter scriptinden veya FakePlayer'dan) vurma komutunu al
    public void TakeHit(int damage = 1)
    {
        if (isFelled) return;

        currentHealth -= damage;
        OnTreeHit?.Invoke();

        // Vurulunca hafif sarsılma efekti
        StartCoroutine(ShakeTree());

        Debug.Log($"<color=yellow>[AĞAÇ VURULDU] {gameObject.name} | Can: {currentHealth}/{maxHealth}</color>");

        if (currentHealth <= 0)
        {
            StartCoroutine(FellTree());
        }
    }

    // Küçük sarsılma animasyonu (CPU harcamaz, saf matematiksel)
    private IEnumerator ShakeTree()
    {
        Vector3 originalRotation = transform.eulerAngles;
        float elapsed = 0f;
        float shakeDuration = 0.25f;
        float shakeMagnitude = 3f;

        while (elapsed < shakeDuration)
        {
            float shakeAngle = Mathf.Sin(elapsed * 40f) * shakeMagnitude * (1f - elapsed / shakeDuration);
            transform.eulerAngles = new Vector3(originalRotation.x, originalRotation.y, originalRotation.z + shakeAngle);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.eulerAngles = originalRotation;
    }

    // Ağaç devrilme sekansı
    private IEnumerator FellTree()
    {
        isFelled = true;
        Debug.Log($"<color=green>[AĞAÇ DEVRİLDİ] {gameObject.name} yıkıldı!</color>");

        // 1. Sağlam ağaç görselini gizle
        if (healthyTreeVisual != null)
        {
            healthyTreeVisual.SetActive(false);
        }

        // 2. Güdüğü (stump) ağacın tam pozisyonuna yerleştir
        if (stumpPrefab != null)
        {
            Instantiate(stumpPrefab, transform.position, transform.rotation);
        }

        // 3. Yan yatan ağaç tepesini spawn et ve devrilme animasyonu oynat
        if (fallenTreePrefab != null)
        {
            // Ağacın yanına (sağına) doğru hafif offset ile doğur
            Vector3 fallPosition = transform.position + transform.right * 1.5f;
            GameObject fallen = Instantiate(fallenTreePrefab, transform.position + Vector3.up * 3f, transform.rotation);

            // Devrilme animasyonu: Yukarıdan yere doğru dönerek düş
            float elapsed = 0f;
            Quaternion startRot = fallen.transform.rotation;
            // Ağacın sağına doğru 90 derece yatık pozisyonu
            Quaternion endRot = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 90f);
            Vector3 startPos = fallen.transform.position;

            while (elapsed < fallDuration)
            {
                float t = elapsed / fallDuration;
                float easeT = 1f - Mathf.Cos(t * Mathf.PI * 0.5f); // Ease-in: Yavaş başlayıp hızlanır

                fallen.transform.rotation = Quaternion.Lerp(startRot, endRot, easeT);
                fallen.transform.position = Vector3.Lerp(startPos, fallPosition, easeT);
                elapsed += Time.deltaTime;
                yield return null;
            }

            fallen.transform.rotation = endRot;
            fallen.transform.position = fallPosition;
        }

        // 4. Ağaç devrildi eventi tetikle (Odun spawn için Yasir/Efe bağlayacak)
        OnTreeFelled?.Invoke();

        // 5. Güdük ve yatan ağaç bir süre dursun, sonra sahneden temizlensin
        yield return new WaitForSeconds(disappearDelay);

        // Bu objeyi (ağaç kontrol objesi) yok et
        Destroy(gameObject);

        // Respawn varsa yeni ağaç doğur
        if (respawnTime > 0f)
        {
            // Yasir veya Efe respawn sistemini buraya bağlayabilir
            Debug.Log($"[AĞAÇ RESPAWN] {respawnTime} saniye sonra yeni ağaç doğacak...");
        }
    }
}
