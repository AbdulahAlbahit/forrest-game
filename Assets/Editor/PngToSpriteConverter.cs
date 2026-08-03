using UnityEditor;
using UnityEngine;

public static class PngToSpriteConverter
{
    [MenuItem("Tools/Secili PNG'leri Sprite'a Cevir")]
    public static void ConvertSelection()
    {
        Object[] selection = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        if (selection.Length == 0)
        {
            Debug.LogWarning("[PngToSpriteConverter] Project penceresinde en az bir PNG/texture secmelisin.");
            return;
        }

        int converted = 0;
        foreach (Object obj in selection)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[PngToSpriteConverter] Atlandi (texture importer bulunamadi): {path}");
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            converted++;
        }

        Debug.Log($"[PngToSpriteConverter] {converted} texture Sprite'a cevrildi.");
    }
}
