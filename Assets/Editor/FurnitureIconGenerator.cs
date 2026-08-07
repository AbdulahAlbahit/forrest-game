using System.IO;
using UnityEditor;
using UnityEngine;

public static class FurnitureIconGenerator
{
    private const int IconSize = 512;
    private const string OutputFolder = "Assets/Sprites/FurnitureIcons";
    private static readonly Vector3 RenderOrigin = new Vector3(0f, 5000f, 0f);
    private static readonly Quaternion CameraAngle = Quaternion.Euler(30f, -135f, 0f);
    private static readonly Quaternion CameraAngleReversed = Quaternion.Euler(30f, 45f, 0f);

    [MenuItem("Tools/Furniture Icon Generator")]
    public static void GenerateFromSelection()
    {
        GenerateFromSelection(CameraAngle);
    }

    [MenuItem("Tools/Furniture Icon Generator (Reversed Angle)")]
    public static void GenerateFromSelectionReversed()
    {
        GenerateFromSelection(CameraAngleReversed);
    }

    private static void GenerateFromSelection(Quaternion cameraAngle)
    {
        var prefabs = Selection.gameObjects;
        if (prefabs.Length == 0)
        {
            Debug.LogWarning("[FurnitureIconGenerator] Select one or more furniture prefabs in the Project window first.");
            return;
        }

        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        int generated = 0;
        foreach (var prefab in prefabs)
        {
            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
            {
                Debug.LogWarning($"[FurnitureIconGenerator] Skipped '{prefab.name}', not a prefab asset.");
                continue;
            }

            if (RenderPrefabToPng(prefab, cameraAngle))
                generated++;
        }

        Debug.Log($"[FurnitureIconGenerator] Generated {generated} icon(s) in {OutputFolder}.");
    }

    private static bool RenderPrefabToPng(GameObject prefabAsset, Quaternion cameraAngle)
    {
        // Instantiated far from the origin so it never overlaps real scene content or the
        // CameraCullingManager's layer-based culling volumes.
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
        instance.transform.position = RenderOrigin;

        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[FurnitureIconGenerator] Skipped '{prefabAsset.name}', no renderers found.");
            Object.DestroyImmediate(instance);
            return false;
        }

        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);

        var cameraObj = new GameObject("IconCaptureCamera");
        var camera = cameraObj.AddComponent<Camera>();
        var lightObj = new GameObject("IconCaptureLight");
        var light = lightObj.AddComponent<Light>();

        try
        {
            SetupCamera(camera, bounds, cameraAngle);
            SetupLight(light, cameraAngle);

            var renderTexture = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            camera.targetTexture = renderTexture;
            camera.Render();

            var previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;

            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            texture.Apply();

            RenderTexture.active = previousActive;
            camera.targetTexture = null;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);

            var pngPath = $"{OutputFolder}/{prefabAsset.name}.png";
            File.WriteAllBytes(pngPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
            ConfigureAsSprite(pngPath);
            return true;
        }
        finally
        {
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(cameraObj);
            Object.DestroyImmediate(lightObj);
        }
    }

    private static void SetupCamera(Camera camera, Bounds bounds, Quaternion cameraAngle)
    {
        camera.transform.position = bounds.center - cameraAngle * Vector3.forward * (bounds.extents.magnitude * 4f);
        camera.transform.rotation = cameraAngle;

        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.3f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = bounds.extents.magnitude * 8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.cullingMask = ~0;
    }

    private static void SetupLight(Light light, Quaternion cameraAngle)
    {
        light.type = LightType.Directional;
        light.transform.rotation = cameraAngle * Quaternion.Euler(20f, 0f, 0f);
        light.intensity = 1.2f;
        light.shadows = LightShadows.None;
    }

    private static void ConfigureAsSprite(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }
}
