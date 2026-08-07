using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipeConfig", menuName = "ForrestGame/Recipe Config")]
public class RecipeConfig : ScriptableObject
{
    [Header("--- Temel Bilgiler ---")]
    public string ItemID;
    public int RequiredWood;
    public float BaseProductionTime;
    public int BaseSalePrice;

    [Header("--- Görsel & Model (Mert için) ---")]
    [Tooltip("Atölye panelinde ve UI'da gösterilecek mobilya ikonu.")]
    public Sprite ItemIcon;

    [Tooltip("Üretim tamamlandığında sahneye yerleştirilecek mobilya 3D modeli (Prefab).")]
    public GameObject FurniturePrefab;

    [Tooltip("Mobilyanın kısa açıklaması.")]
    [TextArea(2, 4)]
    public string Description;
}
