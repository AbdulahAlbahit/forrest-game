using UnityEditor;
using UnityEngine;
using ForrestGame.Data;

public static class TerrainDetailBaselineCapture
{
    [MenuItem("Tools/Ilkbahar Cicek Yogunlugunu Kaydet (Baseline)")]
    public static void Capture()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("[TerrainDetailBaselineCapture] Aktif terrain bulunamadi.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        int layerCount = terrainData.detailPrototypes.Length;
        if (layerCount == 0)
        {
            Debug.LogError("[TerrainDetailBaselineCapture] Terrain'de hic Detail Layer yok. Once Paint Details ile cicek katmanini ekle.");
            return;
        }

        for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
        {
            int[,] density2D = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, layerIndex);

            int width = terrainData.detailWidth;
            int height = terrainData.detailHeight;
            int[] flat = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flat[y * width + x] = density2D[y, x];
                }
            }

            string path = $"Assets/Art/Environments/TerrainDetailBaseline_Layer{layerIndex}.asset";
            TerrainDetailBaseline baseline = AssetDatabase.LoadAssetAtPath<TerrainDetailBaseline>(path);
            if (baseline == null)
            {
                baseline = ScriptableObject.CreateInstance<TerrainDetailBaseline>();
                AssetDatabase.CreateAsset(baseline, path);
            }

            baseline.detailLayerIndex = layerIndex;
            baseline.width = width;
            baseline.height = height;
            baseline.density = flat;
            EditorUtility.SetDirty(baseline);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TerrainDetailBaselineCapture] {layerCount} detail layer icin baseline kaydedildi (Assets/Art/Environments/TerrainDetailBaseline_LayerN.asset). SeasonManager'da hangi layer index cicek ise onu sec.");
    }
}
