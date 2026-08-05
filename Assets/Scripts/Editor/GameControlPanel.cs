using UnityEngine;
using UnityEditor;

public class GameControlPanel : EditorWindow
{
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;
    private Editor currentEditor;
    private Object currentTarget;

    [MenuItem("Forrest Game/Kontrol Paneli")]
    public static void ShowWindow()
    {
        GetWindow<GameControlPanel>("Forrest Game Kontrol Paneli");
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();

        // --- SOL MENÜ (250px genişlik) ---
        GUILayout.BeginVertical(GUILayout.Width(250));
        leftScrollPos = GUILayout.BeginScrollView(leftScrollPos, GUILayout.ExpandHeight(true));

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) {
            alignment = TextAnchor.MiddleCenter,
            margin = new RectOffset(0, 0, 10, 5)
        };

        // 1. Upgrade Configs
        GUILayout.Label("--- UPRGADE AYARLARI ---", headerStyle);
        DrawAssetButton("Axe (Balta 1)", "Assets/Scripts/Upgrades/Axe.asset");
        DrawAssetButton("Axe 2", "Assets/Scripts/Upgrades/Axe2.asset");
        DrawAssetButton("Axe 3", "Assets/Scripts/Upgrades/Axe3.asset");
        DrawAssetButton("Axe 4", "Assets/Scripts/Upgrades/Axe4.asset");
        DrawAssetButton("Axe 5", "Assets/Scripts/Upgrades/Axe5.asset");
        DrawAssetButton("Basket (Sepet)", "Assets/Scripts/Upgrades/Basket.asset");
        DrawAssetButton("Truck (Kamyon)", "Assets/Scripts/Upgrades/Truck.asset");
        DrawAssetButton("Workshop (Atölye)", "Assets/Scripts/Upgrades/Workshop.asset");
        DrawAssetButton("Workshop Queue", "Assets/Scripts/Upgrades/WorkshopQueue.asset");
        DrawAssetButton("Region (Bölge)", "Assets/Scripts/Upgrades/RegionConfigdata.asset");

        GUILayout.Space(10);

        // 2. Scene Components
        GUILayout.Label("--- SAHNE BİLEŞENLERİ ---", headerStyle);
        DrawSceneComponentButton<ForrestGame.Core.SeasonManager>("Season Manager");
        DrawSceneComponentButton<TreeManager>("Tree Manager");

        DrawSceneComponentButton<EfeMevsimKiyafetleri>("Efe Mevsim Kıyafetleri");
        DrawSceneComponentButton<EfeKiyafetAnimatorEsleyici>("Efe Kıyafet Animatör Eşleyici");
        DrawSceneComponentButton<EfeBaltaGorunurElBaglayici>("Efe Balta El Bağlayıcı");
        DrawSceneComponentButton<CustomerOrderManager>("Sipariş Süreleri (Order Manager)");
        DrawSceneComponentButton<ForrestGame.Managers.AdManager>("Reklam Ayarları (Ad Manager)");

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        // Araya dikey bir çizgi
        GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

        // --- SAĞ PANEL (İçerik) ---
        GUILayout.BeginVertical();
        rightScrollPos = GUILayout.BeginScrollView(rightScrollPos, GUILayout.ExpandHeight(true));

        if (currentEditor != null && currentTarget != null)
        {
            GUILayout.Label($"Düzenlenen: {currentTarget.name}", EditorStyles.largeLabel);
            
            if (currentTarget is Component comp) {
                GUILayout.Label($"Obje: {comp.gameObject.name}", EditorStyles.miniLabel);
            }
            
            GUILayout.Space(10);

            // Değişiklikleri takip et
            EditorGUI.BeginChangeCheck();
            
            currentEditor.OnInspectorGUI(); // Mevcut objenin kendi Inspector'ını çiz
            
            if (EditorGUI.EndChangeCheck())
            {
                // Değişiklik yapıldıysa kaydetmesi için işaretle
                EditorUtility.SetDirty(currentTarget);
            }
        }
        else
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Sol menüden düzenlemek istediğiniz bir ayarı seçin.", EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawAssetButton(string label, string path)
    {
        if (GUILayout.Button(label, GUILayout.Height(25)))
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj != null)
            {
                SetCurrentTarget(obj);
            }
            else
            {
                Debug.LogWarning($"[Kontrol Paneli] Dosya bulunamadı: {path}");
            }
        }
    }

    private void DrawSceneComponentButton<T>(string label) where T : Component
    {
        if (GUILayout.Button(label, GUILayout.Height(25)))
        {
            T comp = FindObjectOfType<T>();
            if (comp != null)
            {
                SetCurrentTarget(comp);
            }
            else
            {
                Debug.LogWarning($"[Kontrol Paneli] Sahnede '{typeof(T).Name}' bulunamadı! Lütfen sahnenin açık olduğundan emin olun.");
            }
        }
    }

    private void SetCurrentTarget(Object target)
    {
        if (currentTarget == target) return;

        currentTarget = target;
        if (currentEditor != null)
        {
            DestroyImmediate(currentEditor);
        }

        currentEditor = Editor.CreateEditor(target);
    }
    
    private void OnDestroy()
    {
        if (currentEditor != null)
        {
            DestroyImmediate(currentEditor);
        }
    }
}
