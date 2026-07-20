using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AxeLevelData
{
    public int Level;
    public int UpgradeCost;
    public int AxePower;
}

[CreateAssetMenu(fileName = "AxeUpgradeConfig", menuName = "ForrestGame/Axe Upgrade Config")]
public class AxeUpgradeConfig : ScriptableObject
{
    public List<AxeLevelData> levels = new List<AxeLevelData>();

    public AxeLevelData GetLevelData(int level)
    {
        if (levels == null || levels.Count == 0)
        {
            return new AxeLevelData();
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
