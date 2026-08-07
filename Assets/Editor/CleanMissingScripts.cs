using UnityEditor;
using UnityEngine;

public class CleanMissingScripts : Editor
{
    [MenuItem("Tools/Mert'in Kurtaricisi (Bozuk Scriptleri Temizle)")]
    public static void CleanUp()
    {
        var gameObjects = GameObject.FindObjectsOfType<GameObject>(true);
        int count = 0;
        foreach (var go in gameObjects)
        {
            count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }
        Debug.Log($"[Kurtarici] Sahnede toplam {count} adet eksik/silinmis script bulundu ve basariyla temizlendi!");
    }
}
