using UnityEngine;

namespace ForrestGame.Data
{
    [CreateAssetMenu(fileName = "TerrainDetailBaseline", menuName = "Forrest Game/Terrain Detail Baseline")]
    public class TerrainDetailBaseline : ScriptableObject
    {
        public int detailLayerIndex;
        public int width;
        public int height;
        public int[] density;
    }
}
