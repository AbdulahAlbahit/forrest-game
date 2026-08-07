using System.Collections.Generic;
using UnityEngine;
using ForrestGame.Utils;

namespace ForrestGame.Managers
{











    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public static event System.Action OnQuestsReset;
        public static event System.Action OnQuestProgressUpdated;

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

        private float _nextResetCheckTime = 0f;

        private void Update()
        {
            if (Time.unscaledTime >= _nextResetCheckTime)
            {
                _nextResetCheckTime = Time.unscaledTime + 5f;
                CheckDailyReset();
            }
        }

        private void CheckDailyReset()
        {
            if (CloudSaveManager.Instance?.CurrentData == null) return;
            
            PlayerData data = CloudSaveManager.Instance.CurrentData;
            long currentDay = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalDays;
            
            if (data.LastQuestResetTime < currentDay)
            {
                data.LastQuestResetTime = currentDay;
                
                if (data.ActiveQuests != null) data.ActiveQuests.Clear();
                if (data.CompletedQuests != null) data.CompletedQuests.Clear();
                
                CloudSaveManager.Instance.MarkDirty();
                OnQuestsReset?.Invoke();
                
                GameLogger.Log("[QuestManager] Gunluk gorevler sifirlandi!");
            }
        }

        private void OnEnable()
        {
            ShopManager.OnItemSold += HandleItemSold;
            EfeOynanisEventleri.BaltaAgacaVurdu += HandleAxeHit;
            EfeOynanisEventleri.OdunToplandi += HandleWoodCollected;
            TreeManager.OnTreeFelledAction += HandleTreeFelled;
        }

        private void OnDisable()
        {
            ShopManager.OnItemSold -= HandleItemSold;
            EfeOynanisEventleri.BaltaAgacaVurdu -= HandleAxeHit;
            EfeOynanisEventleri.OdunToplandi -= HandleWoodCollected;
            TreeManager.OnTreeFelledAction -= HandleTreeFelled;
        }

        private void HandleAxeHit(Transform t) => ReportQuestProgress("agaca_vurma", 1);
        private void HandleWoodCollected(Transform t, int current, int cap) => ReportQuestProgress("toplanan_odun", 1);
        private void HandleTreeFelled(Vector3 pos) => ReportQuestProgress("kesilen_agac", 1);

        public void ReportQuestProgress(string actionKey, int amount = 1)
        {
            CheckDailyReset();
            
            if (CloudSaveManager.Instance?.CurrentData == null) return;

            PlayerData data = CloudSaveManager.Instance.CurrentData;

            if (data.ActiveQuests == null)
                data.ActiveQuests = new SerializableDict();
            if (data.CompletedQuests == null)
                data.CompletedQuests = new System.Collections.Generic.List<string>();

            bool progressChanged = false;

            if (tumGorevler != null)
            {
                foreach (QuestConfig quest in tumGorevler)
                {
                    if (quest == null) continue;
                    if (data.CompletedQuests.Contains(quest.QuestID)) continue;

                    if (string.IsNullOrEmpty(quest.TargetKey)) continue;

                    if (quest.TargetKey.Trim().ToLower() == actionKey.Trim().ToLower())
                    {
                        int currentProgress = data.ActiveQuests.Get(quest.QuestID, 0);
                        int newProgress = currentProgress + amount;
                        data.ActiveQuests.Set(quest.QuestID, newProgress);
                        progressChanged = true;
                    }
                }
            }

            if (progressChanged)
            {
                CheckAndRewardQuests(data);
                CloudSaveManager.Instance.MarkDirty();
                OnQuestProgressUpdated?.Invoke();
            }
        }





        private void HandleItemSold(string itemId, int amount)
        {
            GameLogger.Log($"QuestManager: HandleItemSold tetiklendi! Item: {itemId}, Miktar: {amount}");
            ReportQuestProgress($"satilan_{itemId.ToLower()}", amount);
        }





        private bool CheckAndRewardQuests(PlayerData data)
        {
            if (tumGorevler == null || tumGorevler.Count == 0)
            {
                GameLogger.LogWarning("QuestManager: 'Tum Gorevler' listesi bos! Inspector'dan QuestConfig'leri surukle.");
                return false;
            }

            bool anyCompleted = false;

            foreach (QuestConfig quest in tumGorevler)
            {
                if (quest == null) continue;


                if (data.CompletedQuests.Contains(quest.QuestID)) continue;


                int progress = data.ActiveQuests.Get(quest.QuestID, 0);


                if (progress >= quest.TargetAmount)
                {

                    AwardQuest(quest, data);
                    anyCompleted = true;
                }
            }

            return anyCompleted;
        }




        private void AwardQuest(QuestConfig quest, PlayerData data)
        {

            data.CompletedQuests.Add(quest.QuestID);


            if (quest.RewardGold > 0 && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddMoney(quest.RewardGold);
            }


            if (quest.RewardXP > 0 && XPManager.Instance != null)
            {
                XPManager.Instance.AddXP(quest.RewardXP);
            }


            NotificationManager.Instance?.SendNotification(
                $"Gorev Tamamlandi: {quest.QuestName}! +{quest.RewardGold} Altin, +{quest.RewardXP} XP"
            );

            GameLogger.Log($"QuestManager: [TAMAMLANDI] '{quest.QuestName}' (ID: {quest.QuestID}) " +
                      $"| Odul: +{quest.RewardGold} Altin, +{quest.RewardXP} XP");
        }





        public float GetQuestProgress(string questId)
        {
            if (CloudSaveManager.Instance?.CurrentData == null) return 0f;
            if (tumGorevler == null) return 0f;

            QuestConfig quest = tumGorevler.Find(q => q != null && q.QuestID == questId);
            if (quest == null) return 0f;

            int progress = CloudSaveManager.Instance.CurrentData.ActiveQuests.Get(quest.QuestID, 0);
            return Mathf.Clamp01((float)progress / quest.TargetAmount);
        }





        public bool IsQuestCompleted(string questId)
        {
            var data = CloudSaveManager.Instance?.CurrentData;
            return data?.CompletedQuests != null && data.CompletedQuests.Contains(questId);
        }
    }
}
