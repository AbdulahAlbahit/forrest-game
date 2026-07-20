using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RegionData
{
    public int RegionID;
    public string RegionName;
    public int UnlockCost;
}

[CreateAssetMenu(fileName = "RegionConfig", menuName = "ForrestGame/Region Config")]
public class RegionConfig : ScriptableObject
{
    public List<RegionData> regions = new List<RegionData>();

    /// <summary>
    /// Verilen RegionID'ye ait bolge verisini dondurur.
    /// Bulunamazsa varsayilan (bos) bir RegionData dondurur.
    /// </summary>
    public RegionData GetRegionData(int regionId)
    {
        if (regions == null || regions.Count == 0)
        {
            return new RegionData();
        }

        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].RegionID == regionId)
            {
                return regions[i];
            }
        }

        return new RegionData();
    }

    /// <summary>
    /// Verilen bolge adina ait bolge verisini dondurur.
    /// Bulunamazsa varsayilan (bos) bir RegionData dondurur.
    /// </summary>
    public RegionData GetRegionDataByName(string regionName)
    {
        if (regions == null || regions.Count == 0)
        {
            return new RegionData();
        }

        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].RegionName == regionName)
            {
                return regions[i];
            }
        }

        return new RegionData();
    }
}
