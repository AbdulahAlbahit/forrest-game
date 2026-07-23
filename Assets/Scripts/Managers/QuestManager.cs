using UnityEngine;

namespace ForrestGame.Managers
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

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

        private void OnEnable()
        {
            ShopManager.OnItemSold += HandleItemSold;
        }

        private void OnDisable()
        {
            ShopManager.OnItemSold -= HandleItemSold;
        }

        private void HandleItemSold(string itemId, int amount)
        {
            if (CloudSaveManager.Instance == null || CloudSaveManager.Instance.CurrentData == null) return;

            PlayerData data = CloudSaveManager.Instance.CurrentData;
            
            if (data.ActiveQuests == null)
            {
                data.ActiveQuests = new SerializableDict();
            }

            // Guncellenecek gorev sayacinin anahtari
            string questKey = $"satilan_{itemId.ToLower()}";
            
            // Mevcut ilerlemeyi al ve arttir
            int currentProgress = data.ActiveQuests.Get(questKey, 0);
            data.ActiveQuests.Set(questKey, currentProgress + amount);

            Debug.Log($"QuestManager: Gorev ilerlemesi guncellendi -> {questKey}: {currentProgress + amount}");

            // Aninda buluta kaydet
            CloudSaveManager.Instance.SaveData(data);
        }
    }
}
