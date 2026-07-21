using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BasketLevelData
{
    public int Level;
    public int UpgradeCost;
    public int BasketCapacity;
}

[CreateAssetMenu(fileName = "BasketUpgradeConfig", menuName = "ForrestGame/Basket Upgrade Config")]
public class BasketUpgradeConfig : ScriptableObject
{
    public List<BasketLevelData> levels = new List<BasketLevelData>();

    public BasketLevelData GetLevelData(int level)
    {
        if (levels == null || levels.Count == 0)
        {
            return new BasketLevelData();
        }

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i].Level == level)
            {
                return levels[i];
            }
        }

        // Return the last element if not found
        return levels[levels.Count - 1];
    }
}
