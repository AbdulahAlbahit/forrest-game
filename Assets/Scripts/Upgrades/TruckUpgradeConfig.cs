using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TruckLevelData
{
    public int Level;
    public int UpgradeCost;
    public int TruckCapacity;
}

[CreateAssetMenu(fileName = "TruckUpgradeConfig", menuName = "ForrestGame/Truck Upgrade Config")]
public class TruckUpgradeConfig : ScriptableObject
{
    public List<TruckLevelData> levels = new List<TruckLevelData>();

    public TruckLevelData GetLevelData(int level)
    {
        if (levels == null || levels.Count == 0)
        {
            return new TruckLevelData();
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
