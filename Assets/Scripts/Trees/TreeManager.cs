using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ForrestGame.Utils;
using ForrestGame.Core;

public class TreeManager : MonoBehaviour
{
    public static TreeManager Instance { get; private set; }

    [System.Serializable]
    public class TreeType
    {
        public string typeName = "Pine";
        public GameObject fullTreePrefab;
        public GameObject fallenPrefab;
        [HideInInspector] public Queue<GameObject> fallenPool = new Queue<GameObject>();
    }

    [Header("Tree Types (3 Max)")]
    public TreeType[] treeTypes = new TreeType[3];

    [Header("Stump & Wood Prefabs")]
    public GameObject stumpPrefab;
    public GameObject woodLogPrefab;

    [System.Serializable]
    public class TreeLogCountEntry
    {
        [Tooltip("Terrain'deki ağaç prototip index'i (Terrain > Paint Trees listesindeki sıra).")]
        public int prototypeIndex;
        public int logCount = 3;
    }

    [Header("Wood Log Settings")]
    [Tooltip("Listede olmayan prototip index'leri bu degeri kullanir.")]
    public int logSpawnCount = 3;
    public List<TreeLogCountEntry> logCountByPrototypeIndex = new List<TreeLogCountEntry>
    {
        new TreeLogCountEntry { prototypeIndex = 0, logCount = 3 },
        new TreeLogCountEntry { prototypeIndex = 1, logCount = 5 },
        new TreeLogCountEntry { prototypeIndex = 3, logCount = 7 }
    };
    public float logJumpForce = 5f;
    public float logSpawnScale = 0.65f;

    [System.Serializable]
    public class TreeHealthEntry
    {
        [Tooltip("Terrain'deki ağaç prototip index'i (Terrain > Paint Trees listesindeki sıra).")]
        public int prototypeIndex;
        public int health = 60;
    }

    [Header("Tree Settings")]
    public int defaultTreeHealth = 3;

    [Tooltip("Listede olmayan prototip index'leri defaultTreeHealth kullanir.")]
    public List<TreeHealthEntry> treeHealthByPrototypeIndex = new List<TreeHealthEntry>
    {
        new TreeHealthEntry { prototypeIndex = 0, health = 60 },
        new TreeHealthEntry { prototypeIndex = 1, health = 200 },
        new TreeHealthEntry { prototypeIndex = 3, health = 400 }
    };

    public float searchRadius = 2.5f;
    [Range(5f, 50f)]
    public float fallTorque = 15f;

    [Header("Non-Tree Terrain Prototypes")]
    [Tooltip("Terrain'e Tree fırçasıyla boyanmış ama gerçek ağaç olmayan prototiplerin (ör. kayalar) index numaraları. Bu index'ler baltayla kesilmez.")]
    public int[] nonTreePrototypeIndices = new int[0];

    [Header("Time Settings")]
    public float disappearDelay = 2f;
    public float logDisappearDelay = 8f;
    public float shrinkDuration = 0.5f;
    public float respawnTime = 10f;

    [Header("Object Pool")]
    public int poolSize = 5;
    public int logPoolSize = 20;

    [Header("Unity Events")]
    public UnityEvent<Vector3> OnTreeFelled;

    // SFX Actions for AudioManager
    public static event Action<Vector3> OnTreeFelledAction;
    public static event Action<Vector3> OnTreeHitAction;

    private Terrain _activeTerrain;
    private Dictionary<int, int> _damagedTrees = new Dictionary<int, int>();
    private Queue<GameObject> _stumpPool = new Queue<GameObject>();
    private Queue<GameObject> _logPool = new Queue<GameObject>();

    private struct RespawnData
    {
        public TreeInstance treeInstance;
        public float respawnAt;
    }
    private List<RespawnData> _respawnQueue = new List<RespawnData>();

    // TerrainData.treeInstances her erisimde TUM agaclarin kopyasini dondurur (pahali).
    // Bu yuzden diziyi bir kere cekip cache'liyoruz, sadece agac silinince/respawn
    // olunca (RemoveTreeFromTerrain/RespawnTree) yeniden olusturuyoruz.
    private TreeInstance[] _cachedTreeInstances = Array.Empty<TreeInstance>();
    private Vector3[] _cachedTreeWorldPositions = Array.Empty<Vector3>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _activeTerrain = Terrain.activeTerrain;
        if (_activeTerrain == null)
        {
            GameLogger.LogError("[TreeManager] Active Terrain not found!", this);
            return;
        }

        RefreshTreeInstanceCache();
        BuildPools();
        StartCoroutine(RespawnRoutine());
    }

    private void RefreshTreeInstanceCache()
    {
        _cachedTreeInstances = _activeTerrain.terrainData.treeInstances;
        _cachedTreeWorldPositions = new Vector3[_cachedTreeInstances.Length];
        for (int i = 0; i < _cachedTreeInstances.Length; i++)
        {
            _cachedTreeWorldPositions[i] = GetTreeWorldPos(_cachedTreeInstances[i]);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        while (true)
        {
            if (_respawnQueue.Count > 0 && respawnTime > 0f)
            {
                for (int i = _respawnQueue.Count - 1; i >= 0; i--)
                {
                    if (Time.time >= _respawnQueue[i].respawnAt)
                    {
                        RespawnTree(_respawnQueue[i].treeInstance);
                        _respawnQueue.RemoveAt(i);
                    }
                }
            }
            // Saniyede 60 kez (Update) kontrol etmek yerine saniyede sadece 1 kez kontrol et!
            yield return new WaitForSeconds(1f);
        }
    }

    private void BuildPools()
    {
        if (stumpPrefab != null)
        {
            for (int i = 0; i < poolSize * treeTypes.Length; i++)
            {
                GameObject s = Instantiate(stumpPrefab);
                s.name = $"Pool_Stump_{i}";
                s.SetActive(false);
                _stumpPool.Enqueue(s);
            }
        }

        if (woodLogPrefab != null)
        {
            for (int i = 0; i < logPoolSize; i++)
            {
                GameObject log = Instantiate(woodLogPrefab);
                log.name = $"Pool_WoodLog_{i}";

                Rigidbody rb = log.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                log.SetActive(false);
                _logPool.Enqueue(log);
            }
        }

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

        GameLogger.Log($"[TreeManager] Pool ready: {treeTypes.Length} types × {poolSize} fallen + {_stumpPool.Count} stumps", this);
    }

    public void HitTreeAt(Vector3 worldHitPos, int damage = 1)
    {
        if (_activeTerrain == null) return;

        int treeIndex = FindNearestTreeIndex(worldHitPos);
        if (treeIndex < 0) return;

        TreeInstance hitTree = _cachedTreeInstances[treeIndex];
        Vector3 treeWorldPos = _cachedTreeWorldPositions[treeIndex];

        int treeMaxHealth = GetTreeHealthForPrototype(hitTree.prototypeIndex);
        if (!_damagedTrees.ContainsKey(treeIndex))
            _damagedTrees[treeIndex] = treeMaxHealth;

        _damagedTrees[treeIndex] -= damage;
        GameLogger.Log($"[TreeManager] Tree Hit! Health: {_damagedTrees[treeIndex]}/{treeMaxHealth}", this);
        
        OnTreeHitAction?.Invoke(treeWorldPos);

        if (_damagedTrees[treeIndex] <= 0)
        {
            _damagedTrees.Remove(treeIndex);
            int typeIndex = GetTreeTypeIndex(hitTree.prototypeIndex);
            int logCount = GetLogCountForPrototype(hitTree.prototypeIndex);
            RemoveTreeFromTerrain(treeIndex);

            float effectiveRespawnTime = GetEffectiveRespawnTime();

            if (effectiveRespawnTime > 0f)
            {
                _respawnQueue.Add(new RespawnData
                {
                    treeInstance = hitTree,
                    respawnAt = Time.time + effectiveRespawnTime
                });
            }

            StartCoroutine(SpawnFelledVisuals(treeWorldPos, typeIndex, effectiveRespawnTime, logCount));

            OnTreeFelled?.Invoke(treeWorldPos);
            OnTreeFelledAction?.Invoke(treeWorldPos);

            if (XPManager.Instance != null)
            {
                XPManager.Instance.AddXP(20);
                GameLogger.Log("[TreeManager] +20 XP kazanildi!", this);
            }
            else
            {
                GameLogger.LogWarning("[TreeManager] UYARI: XPManager bulunamadi! XP verilemedi.", this);
            }

            if (ForrestGame.Managers.AnalyticsManager.Instance != null)
            {
                ForrestGame.Managers.AnalyticsManager.Instance.LogAdImpression("tree_cut_ad_test");
                GameLogger.Log("[TreeManager] Ad impression (tree_cut_ad_test) analytics eventi gonderildi.", this);
            }

            GameLogger.Log($"[TreeManager] Tree Felled! Type: {typeIndex}. Respawn in {effectiveRespawnTime}s.", this);
        }
    }

    private float GetEffectiveRespawnTime()
    {
        if (SeasonManager.Instance == null || SeasonManager.Instance.ayarlar == null) return respawnTime;

        float multiplier = 1f;
        switch (SeasonManager.Instance.aktifMevsim)
        {
            case MevsimTuru.Ilkbahar:
                multiplier = SeasonManager.Instance.ayarlar.ilkbaharAgacBuyumeHizi;
                break;
            case MevsimTuru.Kis:
                multiplier = SeasonManager.Instance.ayarlar.kisAgacBuyumeHizi;
                break;
        }

        return multiplier > 0f ? respawnTime / multiplier : respawnTime;
    }

    private int GetTreeTypeIndex(int prototypeIndex)
    {
        if (prototypeIndex >= 0 && prototypeIndex < treeTypes.Length)
            return prototypeIndex;
        return 0;
    }

    private int GetTreeHealthForPrototype(int prototypeIndex)
    {
        foreach (TreeHealthEntry entry in treeHealthByPrototypeIndex)
        {
            if (entry != null && entry.prototypeIndex == prototypeIndex)
                return entry.health;
        }
        return defaultTreeHealth;
    }

    private int GetLogCountForPrototype(int prototypeIndex)
    {
        foreach (TreeLogCountEntry entry in logCountByPrototypeIndex)
        {
            if (entry != null && entry.prototypeIndex == prototypeIndex)
                return entry.logCount;
        }
        return logSpawnCount;
    }

    public bool IsChoppablePrototype(int prototypeIndex)
    {
        for (int i = 0; i < nonTreePrototypeIndices.Length; i++)
        {
            if (nonTreePrototypeIndices[i] == prototypeIndex) return false;
        }
        return true;
    }

    private int FindNearestTreeIndex(Vector3 worldPos)
    {
        int closestIndex = -1;
        float closestDist = searchRadius;

        for (int i = 0; i < _cachedTreeInstances.Length; i++)
        {
            if (!IsChoppablePrototype(_cachedTreeInstances[i].prototypeIndex)) continue;

            Vector3 treePos = _cachedTreeWorldPositions[i];
            float dist = Vector2.Distance(
                new Vector2(worldPos.x, worldPos.z),
                new Vector2(treePos.x, treePos.z)
            );
            if (dist < closestDist) { closestDist = dist; closestIndex = i; }
        }
        return closestIndex;
    }

    /// <summary>
    /// En yakin kesilebilir agacin dunya pozisyonunu dondurur. EfeBaltaKontrol gibi
    /// disaridaki sistemlerin kendi terrain taramasi yapmasi yerine bu cache'e
    /// sormasi icin acildi (TerrainData.treeInstances her erisimde pahali kopya doner).
    /// </summary>
    public bool TryGetNearestChoppableTree(Vector3 worldPos, float maxDistance, out Vector3 nearestTreeWorldPos)
    {
        nearestTreeWorldPos = Vector3.zero;
        float closestDistSqr = maxDistance * maxDistance;
        bool found = false;

        for (int i = 0; i < _cachedTreeInstances.Length; i++)
        {
            if (!IsChoppablePrototype(_cachedTreeInstances[i].prototypeIndex)) continue;

            Vector3 treePos = _cachedTreeWorldPositions[i];
            Vector3 yatayFark = treePos - worldPos;
            yatayFark.y = 0f;
            float distSqr = yatayFark.sqrMagnitude;

            if (distSqr <= closestDistSqr)
            {
                closestDistSqr = distSqr;
                nearestTreeWorldPos = treePos;
                found = true;
            }
        }

        return found;
    }

    private Vector3 GetTreeWorldPos(TreeInstance tree)
    {
        Vector3 size = _activeTerrain.terrainData.size;
        Vector3 origin = _activeTerrain.transform.position;
        return new Vector3(
            tree.position.x * size.x + origin.x,
            tree.position.y * size.y + origin.y,
            tree.position.z * size.z + origin.z
        );
    }

    private void RemoveTreeFromTerrain(int indexToRemove)
    {
        TreeInstance[] old = _cachedTreeInstances;
        TreeInstance[] updated = new TreeInstance[old.Length - 1];
        for (int i = 0, j = 0; i < old.Length; i++)
            if (i != indexToRemove) updated[j++] = old[i];

        _activeTerrain.terrainData.treeInstances = updated;
        _activeTerrain.Flush();
        
        TerrainCollider tc = _activeTerrain.GetComponent<TerrainCollider>();
        if (tc != null)
        {
            tc.enabled = false;
            tc.enabled = true;
        }
        
        RefreshTreeInstanceCache();
    }

    private void RespawnTree(TreeInstance treeInstance)
    {
        TreeInstance[] old = _cachedTreeInstances;
        TreeInstance[] updated = new TreeInstance[old.Length + 1];
        old.CopyTo(updated, 0);
        updated[updated.Length - 1] = treeInstance;

        _activeTerrain.terrainData.treeInstances = updated;
        _activeTerrain.Flush();
        
        TerrainCollider tc = _activeTerrain.GetComponent<TerrainCollider>();
        if (tc != null)
        {
            tc.enabled = false;
            tc.enabled = true;
        }
        
        RefreshTreeInstanceCache();

        GameLogger.Log("[TreeManager] Tree respawned on terrain.", this);
    }

    private IEnumerator SpawnFelledVisuals(Vector3 pos, int typeIndex, float effectiveRespawnTime, int logCount)
    {
        GameObject stump = null;
        if (_stumpPool.Count > 0)
        {
            stump = _stumpPool.Dequeue();
        }
        else if (stumpPrefab != null)
        {
            stump = Instantiate(stumpPrefab);
            stump.name = "Pool_Stump_Dynamic";
        }

        if (stump != null)
        {
            stump.transform.position = pos;
            stump.transform.localScale = Vector3.one;
            stump.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            stump.SetActive(true);

            StartCoroutine(ShrinkAndReturn(stump, _stumpPool, effectiveRespawnTime > 0 ? effectiveRespawnTime - 0.5f : disappearDelay));
        }

        if (typeIndex < treeTypes.Length && treeTypes[typeIndex].fallenPrefab != null)
        {
            GameObject fallen = null;
            if (treeTypes[typeIndex].fallenPool.Count > 0)
            {
                fallen = treeTypes[typeIndex].fallenPool.Dequeue();
            }
            else
            {
                fallen = Instantiate(treeTypes[typeIndex].fallenPrefab);
                fallen.name = $"Pool_{treeTypes[typeIndex].typeName}_Fallen_Dynamic";
            }

            if (fallen != null)
            {
                float randomY = UnityEngine.Random.Range(0f, 360f);
                
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
                    rb.maxDepenetrationVelocity = 2f;

                    Vector3 pushDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized;
                    Vector3 pushPoint = fallen.transform.position + Vector3.up * 3f; 
                    
                    rb.AddForceAtPosition(pushDirection * (fallTorque * 2f), pushPoint, ForceMode.Impulse);
                }

                StartCoroutine(ShrinkAndReturn(fallen, treeTypes[typeIndex].fallenPool, disappearDelay, rb));
            }
        }

        if (woodLogPrefab != null)
        {
            // Fiziksel çarpışma (patlama) olmaması için ağacın düşmesine çok ufak bir süre tanı
            yield return new WaitForSeconds(0.2f); 

            for (int i = 0; i < logCount; i++)
            {
                GameObject log = null;
                
                if (_logPool.Count > 0)
                {
                    log = _logPool.Dequeue();
                }
                else
                {
                    // Havuz boşaldıysa yeni kütük üretip havuza dahil et (Dinamik Genişleme)
                    log = Instantiate(woodLogPrefab);
                    log.name = $"Pool_WoodLog_Dynamic";
                    Rigidbody rbDynamic = log.GetComponent<Rigidbody>();
                    if (rbDynamic != null) rbDynamic.isKinematic = true;
                    log.SetActive(false);
                    GameLogger.Log("[TreeManager] Log pool exhausted! Dynamically expanded the pool.", this);
                }
                
                // Odunların devrilen gövdeye sıkışıp uzaya fırlamaması için yukarıdan spawn ediyoruz.
                // Yana saçılma (X, Z) tekrar daraltıldı ki etrafa çok dağılmasın.
                Vector3 spawnOffset = new Vector3(
                    UnityEngine.Random.Range(-0.7f, 0.7f), 
                    UnityEngine.Random.Range(2.0f, 3.5f), 
                    UnityEngine.Random.Range(-0.7f, 0.7f)
                );
                
                log.transform.position = pos + spawnOffset;
                log.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f));
                log.transform.localScale = Vector3.one * logSpawnScale;
                log.SetActive(true);

                Rigidbody rbLog = log.GetComponent<Rigidbody>();
                if (rbLog != null)
                {
                    rbLog.isKinematic = false;
                    rbLog.linearVelocity = Vector3.zero;
                    rbLog.angularVelocity = Vector3.zero;

                    Vector3 jumpDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(1.5f, 2.5f), UnityEngine.Random.Range(-1f, 1f)).normalized;
                    rbLog.AddForce(jumpDir * logJumpForce, ForceMode.Impulse);
                    rbLog.AddTorque(UnityEngine.Random.insideUnitSphere * (logJumpForce * 2f), ForceMode.Impulse);
                }

                StartCoroutine(ShrinkAndReturn(log, _logPool, logDisappearDelay, rbLog));
            }
        }

        yield return null;
    }

    private IEnumerator ShrinkAndReturn(GameObject obj, Queue<GameObject> pool, float delay, Rigidbody rb = null)
    {
        yield return new WaitForSeconds(delay);

        if (rb != null) rb.isKinematic = true;

        Vector3 originalScale = obj.transform.localScale;
        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            obj.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / shrinkDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.transform.localScale = originalScale;
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
