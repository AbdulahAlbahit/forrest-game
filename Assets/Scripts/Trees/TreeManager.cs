using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// FORREST GAME - Ağaç Kesme Yöneticisi
/// -------------------------------------------------------
/// 3 farklı ağaç tipi destekler (Pine, Default, Cone).
/// Her tipin kendi fallen görseli vardır.
/// Kesilen ağaç: Kök bırakır + Fallen fizikle düşer + 10sn sonra geri büyür.
///
/// EFE İÇİN:
///   TreeManager.Instance.HitTreeAt(hit.point, hit.collider.gameObject, hasar);
/// </summary>
public class TreeManager : MonoBehaviour
{
    public static TreeManager Instance { get; private set; }

    // -------------------------------------------------------
    // 3 AĞAÇ TİPİ TANIMI
    // -------------------------------------------------------
    [System.Serializable]
    public class TreeType
    {
        [Tooltip("Bu tipin adı (Inspector'da görünür, Pine / Default / Cone)")]
        public string typeName = "Pine";

        [Tooltip("Terrain'e atılan tam ağaç prefabı (tree_pine_full.prefab gibi)")]
        public GameObject fullTreePrefab;

        [Tooltip("Devrilince çıkan fallen prefabı (tree_pine_fallen.prefab gibi)")]
        public GameObject fallenPrefab;

        [HideInInspector] public Queue<GameObject> fallenPool = new Queue<GameObject>();
    }

    [Header("--- AĞAÇ TİPLERİ (3 Adet) ---")]
    public TreeType[] treeTypes = new TreeType[3];

    [Header("--- KÖK VE ODUN PREFABLARI ---")]
    [Tooltip("Her kesilen ağacın dibinde bırakılacak kök (stump_roundDetailed.prefab)")]
    public GameObject stumpPrefab;

    [Tooltip("Ağaç kesilince yere fırlayacak odun parçası (wood_log.prefab)")]
    public GameObject woodLogPrefab;

    [Header("--- ODUN FIRLATMA AYARLARI ---")]
    [Tooltip("Her ağaçtan kaç adet odun fırlasın?")]
    public int logSpawnCount = 3;

    [Tooltip("Odunların havaya doğru fırlama hızı (Force)")]
    public float logJumpForce = 5f;

    [Header("--- AĞAÇ AYARLARI ---")]
    [Tooltip("Her ağaç kaç vuruşta devrilsin?")]
    public int defaultTreeHealth = 3;

    [Tooltip("Vuruş noktasına kaç metre yakınlıktaki ağaçlar aransın?")]
    public float searchRadius = 2.5f;

    [Tooltip("Devrilme kuvveti (Torque - Düşük tutun ki uzağa fırlamasın)")]
    [Range(5f, 50f)]
    public float fallTorque = 15f;

    [Header("--- ZAMAN AYARLARI ---")]
    [Tooltip("Fallen ağaç yere düştükten kaç saniye sonra kaybolsun?")]
    public float disappearDelay = 2f;

    [Tooltip("Odunlar yerde kaç saniye kaldıktan sonra kaybolsun? (Yasir'in toplamasına kadar sahne temizliği için)")]
    public float logDisappearDelay = 8f;

    [Tooltip("Kaybolma animasyonu süresi (saniye)")]
    public float shrinkDuration = 0.5f;

    [Tooltip("Kesilen ağaç kaç saniye sonra geri büyüsün? (0 = büyümez)")]
    public float respawnTime = 10f;

    [Header("--- OBJECT POOL ---")]
    [Tooltip("Her tip için havuzda kaç fallen obje olsun?")]
    public int poolSize = 5;

    [Tooltip("Havuzda kaç odun parçası beklesin? (logSpawnCount * 5 yeterlidir)")]
    public int logPoolSize = 20;

    [Header("--- EVENT PRİZİ ---")]
    [Tooltip("Ağaç devrilince tetiklenir. Vector3 = devrilme pozisyonu (Odun spawn için)")]
    public UnityEvent<Vector3> OnTreeFelled;

    // --- İÇ VERİLER ---
    private Terrain activeTerrain;

    // Sadece VURULMUŞ ağaçların canı tutulur → RAM sıfır!
    private Dictionary<int, int> damagedTrees = new Dictionary<int, int>();

    // Kök (stump) ve Odun (log) poolları
    private Queue<GameObject> stumpPool = new Queue<GameObject>();
    private Queue<GameObject> logPool = new Queue<GameObject>();

    // Respawn için terrain tree verisi saklanır
    private struct RespawnData
    {
        public TreeInstance treeInstance;
        public float respawnAt;
    }
    private List<RespawnData> respawnQueue = new List<RespawnData>();

    // -------------------------------------------------------
    // BAŞLANGIÇ
    // -------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        activeTerrain = Terrain.activeTerrain;
        if (activeTerrain == null)
        {
            Debug.LogError("[TreeManager] Sahnede aktif Terrain bulunamadı!");
            return;
        }

        BuildPools();
    }

    void Update()
    {
        // Respawn zamanı gelen ağaçları geri büyüt
        if (respawnQueue.Count > 0 && respawnTime > 0f)
        {
            for (int i = respawnQueue.Count - 1; i >= 0; i--)
            {
                if (Time.time >= respawnQueue[i].respawnAt)
                {
                    RespawnTree(respawnQueue[i].treeInstance);
                    respawnQueue.RemoveAt(i);
                }
            }
        }
    }

    private void BuildPools()
    {
        // Kök poolu
        if (stumpPrefab != null)
        {
            for (int i = 0; i < poolSize * treeTypes.Length; i++)
            {
                GameObject s = Instantiate(stumpPrefab);
                s.name = $"Pool_Stump_{i}";
                s.SetActive(false);
                stumpPool.Enqueue(s);
            }
        }

        // Odun parçası (Wood Log) poolu
        if (woodLogPrefab != null)
        {
            for (int i = 0; i < logPoolSize; i++)
            {
                GameObject log = Instantiate(woodLogPrefab);
                log.name = $"Pool_WoodLog_{i}";

                Rigidbody rb = log.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                log.SetActive(false);
                logPool.Enqueue(log);
            }
        }

        // Her ağaç tipi için fallen poolu
        foreach (var treeType in treeTypes)
        {
            if (treeType.fallenPrefab == null) continue;

            for (int i = 0; i < poolSize; i++)
            {
                GameObject f = Instantiate(treeType.fallenPrefab);
                f.name = $"Pool_{treeType.typeName}_Fallen_{i}";

                Rigidbody rb = f.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                f.SetActive(false);
                treeType.fallenPool.Enqueue(f);
            }
        }

        Debug.Log($"<color=cyan>[TreeManager] Pool hazır: {treeTypes.Length} tip × {poolSize} fallen + {stumpPool.Count} kök</color>");
    }

    // -------------------------------------------------------
    // EFE'NİN ÇAĞIRACAĞI TEK METOD
    // -------------------------------------------------------
    public void HitTreeAt(Vector3 worldHitPos, int damage = 1)
    {
        if (activeTerrain == null) return;

        int treeIndex = FindNearestTreeIndex(worldHitPos);
        if (treeIndex < 0) return;

        TreeInstance hitTree = activeTerrain.terrainData.treeInstances[treeIndex];
        Vector3 treeWorldPos = GetTreeWorldPos(hitTree);

        // Can takibi
        if (!damagedTrees.ContainsKey(treeIndex))
            damagedTrees[treeIndex] = defaultTreeHealth;

        damagedTrees[treeIndex] -= damage;
        Debug.Log($"<color=yellow>[AĞAÇ VURULDU] Can: {damagedTrees[treeIndex]}/{defaultTreeHealth}</color>");

        if (damagedTrees[treeIndex] <= 0)
        {
            damagedTrees.Remove(treeIndex);

            // Ağaç tipini tespit et (prefab prototype index'e göre)
            int typeIndex = GetTreeTypeIndex(hitTree.prototypeIndex);

            // Terrain'den kaldır
            RemoveTreeFromTerrain(treeIndex);

            // Respawn kuyruğuna ekle
            if (respawnTime > 0f)
            {
                respawnQueue.Add(new RespawnData
                {
                    treeInstance = hitTree,
                    respawnAt = Time.time + respawnTime
                });
            }

            // Görsel efektleri başlat
            StartCoroutine(SpawnFelledVisuals(treeWorldPos, typeIndex));
            OnTreeFelled?.Invoke(treeWorldPos);

            Debug.Log($"<color=green>[AĞAÇ DEVRİLDİ] Tip:{typeIndex} | {respawnTime}sn sonra geri büyüyecek</color>");
        }
    }

    // -------------------------------------------------------
    // TİP TESPİTİ (Prototype Index → TreeType Index)
    // -------------------------------------------------------
    private int GetTreeTypeIndex(int prototypeIndex)
    {
        // Terrain'deki tree prototype sırası ile treeTypes dizisi sırası eşleşmeli.
        // 0 = Pine, 1 = Default, 2 = Cone (Inspector'da aynı sırayla ayarla!)
        if (prototypeIndex >= 0 && prototypeIndex < treeTypes.Length)
            return prototypeIndex;
        return 0; // Tanınmayan tip → Pine kullan
    }

    // -------------------------------------------------------
    // TERRAIN İŞLEMLERİ
    // -------------------------------------------------------
    private int FindNearestTreeIndex(Vector3 worldPos)
    {
        TreeInstance[] trees = activeTerrain.terrainData.treeInstances;
        int closestIndex = -1;
        float closestDist = searchRadius;

        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 treePos = GetTreeWorldPos(trees[i]);
            float dist = Vector2.Distance(
                new Vector2(worldPos.x, worldPos.z),
                new Vector2(treePos.x, treePos.z)
            );
            if (dist < closestDist) { closestDist = dist; closestIndex = i; }
        }
        return closestIndex;
    }

    private Vector3 GetTreeWorldPos(TreeInstance tree)
    {
        Vector3 size = activeTerrain.terrainData.size;
        Vector3 origin = activeTerrain.transform.position;
        return new Vector3(
            tree.position.x * size.x + origin.x,
            tree.position.y * size.y + origin.y,
            tree.position.z * size.z + origin.z
        );
    }

    private void RemoveTreeFromTerrain(int indexToRemove)
    {
        TreeInstance[] old = activeTerrain.terrainData.treeInstances;
        TreeInstance[] updated = new TreeInstance[old.Length - 1];
        for (int i = 0, j = 0; i < old.Length; i++)
            if (i != indexToRemove) updated[j++] = old[i];

        activeTerrain.terrainData.treeInstances = updated;
        activeTerrain.Flush();
    }

    private void RespawnTree(TreeInstance treeInstance)
    {
        // Ağacı terrain'e geri ekle
        TreeInstance[] old = activeTerrain.terrainData.treeInstances;
        TreeInstance[] updated = new TreeInstance[old.Length + 1];
        old.CopyTo(updated, 0);
        updated[updated.Length - 1] = treeInstance;

        activeTerrain.terrainData.treeInstances = updated;
        activeTerrain.Flush();

        Debug.Log($"<color=lime>[AĞAÇ GERİ BÜYÜDÜ] Terrain'e eklendi!</color>");
    }

    // -------------------------------------------------------
    // GÖRSEL: KÖK + FİZİKLE DÜŞEN AĞAÇ
    // -------------------------------------------------------
    private IEnumerator SpawnFelledVisuals(Vector3 pos, int typeIndex)
    {
        // --- KÖK ---
        if (stumpPool.Count > 0)
        {
            GameObject stump = stumpPool.Dequeue();
            stump.transform.position = pos;
            stump.transform.localScale = Vector3.one;
            stump.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            stump.SetActive(true);

            // Ağaç geri büyüyünce kökte kaybolsun
            StartCoroutine(ShrinkAndReturn(stump, stumpPool, respawnTime > 0 ? respawnTime - 0.5f : disappearDelay));
        }

        // --- FALLEN AĞAÇ (Tipe göre doğru görseli seç) ---
        if (typeIndex < treeTypes.Length && treeTypes[typeIndex].fallenPool.Count > 0)
        {
            GameObject fallen = treeTypes[typeIndex].fallenPool.Dequeue();
            float randomY = Random.Range(0f, 360f);
            
            // Kök ve Terrain ile collider çakışması (fırlama) olmasın diye biraz daha yukarıdan bırakıyoruz
            fallen.transform.position = pos + Vector3.up * 1.2f;
            fallen.transform.rotation = Quaternion.Euler(0f, randomY, 0f);
            fallen.transform.localScale = Vector3.one;
            fallen.SetActive(true);

            Rigidbody rb = fallen.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Unity'nin collider çakışmasında objeyi roket gibi fırlatma hızını kısıtla!
                rb.maxDepenetrationVelocity = 2f;

                // Tork yerine ağacın gövdesine (üst kısmına) hafif bir ittirme kuvveti uygula ki yana devrilsin
                Vector3 pushDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                Vector3 pushPoint = fallen.transform.position + Vector3.up * 3f; // Ağacın tepe noktası
                
                // Kuvveti uygula
                rb.AddForceAtPosition(pushDirection * (fallTorque * 2f), pushPoint, ForceMode.Impulse);
            }

            StartCoroutine(ShrinkAndReturn(fallen, treeTypes[typeIndex].fallenPool, disappearDelay, rb));
        }

        // --- ODUN PARÇALARINI FIRLAT (WOOD LOGS) ---
        if (woodLogPrefab != null && logPool.Count > 0)
        {
            for (int i = 0; i < logSpawnCount; i++)
            {
                if (logPool.Count == 0) break;

                GameObject log = logPool.Dequeue();
                
                // Odunlar ağacın dibinden biraz yukarısında, hafif rastgele dağılarak çıksın
                Vector3 spawnOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(1f, 2.5f), Random.Range(-0.5f, 0.5f));
                log.transform.position = pos + spawnOffset;
                log.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                log.transform.localScale = Vector3.one;
                log.SetActive(true);

                Rigidbody rbLog = log.GetComponent<Rigidbody>();
                if (rbLog != null)
                {
                    rbLog.isKinematic = false;
                    rbLog.linearVelocity = Vector3.zero;
                    rbLog.angularVelocity = Vector3.zero;

                    // Havaya (ve rastgele yana) doğru fırlatma kuvveti (Parabolik yay çizer)
                    Vector3 jumpDir = new Vector3(Random.Range(-1f, 1f), Random.Range(1.5f, 2.5f), Random.Range(-1f, 1f)).normalized;
                    rbLog.AddForce(jumpDir * logJumpForce, ForceMode.Impulse);

                    // Havada döne döne düşsün
                    rbLog.AddTorque(Random.insideUnitSphere * (logJumpForce * 2f), ForceMode.Impulse);
                }

                // Odunlar yerde kalsın, Yasir'in toplamasına kadar veya süre bitene kadar
                StartCoroutine(ShrinkAndReturn(log, logPool, logDisappearDelay, rbLog));
            }
        }

        yield return null;
    }

    private IEnumerator ShrinkAndReturn(GameObject obj, Queue<GameObject> pool, float delay, Rigidbody rb = null)
    {
        yield return new WaitForSeconds(delay);

        if (rb != null) rb.isKinematic = true;

        // Küçülerek kaybol
        Vector3 originalScale = obj.transform.localScale;
        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            obj.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / shrinkDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.transform.localScale = Vector3.one;
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
