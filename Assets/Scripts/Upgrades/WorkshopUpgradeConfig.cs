using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct WorkshopLevelData
{
    public int Level;
    public int UpgradeCost;
    public float CraftSpeed;
    public int QueueCapacity;
}

[CreateAssetMenu(fileName = "WorkshopUpgradeConfig", menuName = "ForrestGame/Workshop Upgrade Config")]
public class WorkshopUpgradeConfig : ScriptableObject
{
    public List<WorkshopLevelData> levels = new List<WorkshopLevelData>();

    public WorkshopLevelData GetLevelData(int level)
    {
        if (levels == null || levels.Count == 0)
        {
            return new WorkshopLevelData();
        }

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i].Level == level)
            {
                return levels[i];
            }
        }

        return levels[levels.Count - 1];
    }
}
