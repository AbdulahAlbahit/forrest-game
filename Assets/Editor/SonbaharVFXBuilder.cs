using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SonbaharVFXBuilder
{
    [MenuItem("Tools/Sonbahar VFX Olustur (Yagmur + Ucusan Yapraklar)")]
    public static void Build()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[SonbaharVFXBuilder] Sahnede MainCamera tag'li bir kamera bulunamadi.");
            return;
        }

        Transform existing = mainCamera.transform.Find("VFX_Sonbahar");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject root = new GameObject("VFX_Sonbahar");
        Undo.RegisterCreatedObjectUndo(root, "Create VFX_Sonbahar");
        root.transform.SetParent(mainCamera.transform, false);
        root.transform.localPosition = new Vector3(-2.88f, 3.625f, 7.08f);
        root.transform.localRotation = Quaternion.Euler(45.6f, 26.271f, 3.459f);

        BuildRain(root.transform);
        BuildFallingLeaves(root.transform);

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(root);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("[SonbaharVFXBuilder] VFX_Sonbahar olusturuldu. SeasonManager'daki sonbaharVFX alanina bu objeyi surukle.");
    }

    private static void BuildRain(Transform parent)
    {
        GameObject go = new GameObject("Rain");
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 1.1f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
        main.startColor = new Color(0.75f, 0.8f, 0.85f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 250;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 180f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0f, 5f, 4f);
        shape.scale = new Vector3(10f, 0.1f, 10f);
        shape.randomDirectionAmount = 0f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(-9f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.12f;
        renderer.lengthScale = 3f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enableGPUInstancing = true;
        renderer.material = CreateOrLoadParticleMaterial("M_SonbaharYagmur", new Color(0.75f, 0.8f, 0.85f, 1f));
    }

    private static void BuildFallingLeaves(Transform parent)
    {
        GameObject go = new GameObject("FallingLeaves");
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 4f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
        main.startColor = new Color(1f, 0.6f, 0.2f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 60;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 6f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0f, 5f, 4f);
        shape.scale = new Vector3(10f, 0.1f, 10f);
        shape.randomDirectionAmount = 0f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(-1.2f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.1f;

        ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-90f, 90f);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enableGPUInstancing = true;
        renderer.material = CreateOrLoadParticleMaterial("M_SonbaharYaprak", new Color(1f, 0.6f, 0.2f, 1f));

        go.AddComponent<SnowStormController>();
    }

    private static Material CreateOrLoadParticleMaterial(string assetName, Color baseColor)
    {
        string path = $"Assets/Art/Environments/Materials/{assetName}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material mat = new Material(shader) { name = assetName };
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
