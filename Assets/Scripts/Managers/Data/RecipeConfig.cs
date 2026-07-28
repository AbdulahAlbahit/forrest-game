using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipeConfig", menuName = "ForrestGame/Recipe Config")]
public class RecipeConfig : ScriptableObject
{
    public string ItemID;
    public int RequiredWood;
    public float BaseProductionTime;
    public int BaseSalePrice;
}
