using System.Collections.Generic;
using UnityEngine;

namespace ForrestGame.Managers
{
    /// <summary>
    /// Tum gorevlerin ilerleme takibini ve otomatik odullendirme islemini yonetir.
    /// 
    /// KULLANIM (Unity Inspector):
    /// 1. Bu component'in 'Tum Gorevler' listesine istedigin QuestConfig asset'lerini surukle.
    /// 2. Player oyunda bir seyler yaptiginda (orn: masa sattigi an) QuestManager otomatik kontrol eder.
    /// 3. Bir gorevin hedefine ulasilirsa Para ve XP aninda hesaba yatar, gorev CompletedQuests listesine kaydedilir.
    /// 
    /// YENI GOREV EKLEMEK ICIN:
    /// Assets > Create > ForrestGame > Quest Config ile yeni bir asset olustur, doldurup bu listeye ekle.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Tum Gorevler")]
        [Tooltip("Oyundaki tum QuestConfig asset'lerini buraya surukle.")]
        public List<QuestConfig> tumGorevler;

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

        /// <summary>
        /// ShopManager'dan bir satis eventi geldiginde calisir.
        /// Ilerlemeyi gunceller, ardindan tum gorevleri tamamlanma acisindan kontrol eder.
        /// </summary>
        private void HandleItemSold(string itemId, int amount)
        {
            if (CloudSaveManager.Instance?.CurrentData == null) return;

            PlayerData data = CloudSaveManager.Instance.CurrentData;

            if (data.ActiveQuests == null)
                data.ActiveQuests = new SerializableDict();
            if (data.CompletedQuests == null)
                data.CompletedQuests = new System.Collections.Generic.List<string>();

            string actionKey = $"satilan_{itemId.ToLower()}";
            bool progressChanged = false;

            // Sadece tamamlanmamis ve bu aksiyonu izleyen gorevlerin ilerlemesini QuestID uzerinden arttir
            if (tumGorevler != null)
            {
                foreach (QuestConfig quest in tumGorevler)
                {
                    if (quest == null) continue;
                    if (data.CompletedQuests.Contains(quest.QuestID)) continue;

                    if (quest.TargetKey.ToLower() == actionKey)
                    {
                        int currentProgress = data.ActiveQuests.Get(quest.QuestID, 0);
                        int newProgress = currentProgress + amount;
                        data.ActiveQuests.Set(quest.QuestID, newProgress);
                        progressChanged = true;
                        Debug.Log($"QuestManager: Gorev [{quest.QuestName}] ilerledi: {currentProgress} -> {newProgress}");
                    }
                }
            }

            if (progressChanged)
            {
                CheckAndRewardQuests(data);
                CloudSaveManager.Instance.SaveData(data);
            }
        }

        /// <summary>
        /// Tum gorevleri tarar. Henuz tamamlanmamis ve hedefine ulasmi? gorevleri
        /// otomatik olarak oduller ve CompletedQuests listesine ekler.
        /// </summary>
        private bool CheckAndRewardQuests(PlayerData data)
        {
            if (tumGorevler == null || tumGorevler.Count == 0)
            {
                Debug.LogWarning("QuestManager: 'Tum Gorevler' listesi bos! Inspector'dan QuestConfig'leri surukle.");
                return false;
            }

            bool anyCompleted = false;

            foreach (QuestConfig quest in tumGorevler)
            {
                if (quest == null) continue;

                // Zaten tamamlanmis mi?
                if (data.CompletedQuests.Contains(quest.QuestID)) continue;

                // Bu gorevin ilerlemesini kendi QuestID'si uzerinden al
                int progress = data.ActiveQuests.Get(quest.QuestID, 0);

                // Hedef doldu mu?
                if (progress >= quest.TargetAmount)
                {
                    // ODULLENDIRME
                    AwardQuest(quest, data);
                    anyCompleted = true;
                }
            }

            return anyCompleted;
        }

        /// <summary>
        /// Belirtilen gorevi tamamlanmis olarak isaretler ve oduller verir.
        /// </summary>
        private void AwardQuest(QuestConfig quest, PlayerData data)
        {
            // CompletedQuests'e ekle (bir daha verilmesin)
            data.CompletedQuests.Add(quest.QuestID);

            // Para ver
            if (quest.RewardGold > 0 && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddMoney(quest.RewardGold);
            }

            // XP ver
            if (quest.RewardXP > 0 && XPManager.Instance != null)
            {
                XPManager.Instance.AddXP(quest.RewardXP);
            }

            // Bildirim gonder
            NotificationManager.Instance?.SendNotification(
                $"Gorev Tamamlandi: {quest.QuestName}! +{quest.RewardGold} Altin, +{quest.RewardXP} XP"
            );

            Debug.Log($"QuestManager: [TAMAMLANDI] '{quest.QuestName}' (ID: {quest.QuestID}) " +
                      $"| Odul: +{quest.RewardGold} Altin, +{quest.RewardXP} XP");
        }

        /// <summary>
        /// Belirtilen gorevin mevcut ilerlemesini yuzde olarak dondurur (0.0 - 1.0).
        /// UI paneli icin kullanilabilir.
        /// </summary>
        public float GetQuestProgress(string questId)
        {
            if (CloudSaveManager.Instance?.CurrentData == null) return 0f;
            if (tumGorevler == null) return 0f;

            QuestConfig quest = tumGorevler.Find(q => q != null && q.QuestID == questId);
            if (quest == null) return 0f;

            int progress = CloudSaveManager.Instance.CurrentData.ActiveQuests.Get(quest.QuestID, 0);
            return Mathf.Clamp01((float)progress / quest.TargetAmount);
        }

        /// <summary>
        /// Belirtilen gorevin tamamlanip tamamlanmadigini dondurur.
        /// UI paneli icin kullanilabilir.
        /// </summary>
        public bool IsQuestCompleted(string questId)
        {
            var data = CloudSaveManager.Instance?.CurrentData;
            return data?.CompletedQuests != null && data.CompletedQuests.Contains(questId);
        }
    }
}
