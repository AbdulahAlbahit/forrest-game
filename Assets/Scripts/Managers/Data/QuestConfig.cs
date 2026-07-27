using UnityEngine;

/// <summary>
/// Tek bir gorev tanimini tutan ScriptableObject.
/// Unity Inspector uzerinden yeni gorev olusturmak icin:
/// Assets > Create > ForrestGame > Quest Config
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "ForrestGame/Quest Config")]
public class QuestConfig : ScriptableObject
{
    [Header("Kimlik Bilgileri")]
    [Tooltip("Firebase CompletedQuests listesinde kullanilacak benzersiz ID. Degistirme!")]
    public string QuestID;

    [Tooltip("Oyuncuya gosterilecek gorev adi.")]
    public string QuestName;

    [Tooltip("Gorevin ne yapilmasi gerektigini aciklayan metin.")]
    [TextArea(2, 4)]
    public string QuestDescription;

    [Header("Hedef")]
    [Tooltip("ActiveQuests sozlugundeki hangi sayac izlenecek? (orn: satilan_sandalye, satilan_masa)")]
    public string TargetKey;

    [Tooltip("Gorevin tamamlanmasi icin gereken toplam miktar.")]
    public int TargetAmount;

    [Header("Oduller")]
    [Tooltip("Tamamlaninca verilecek altin miktari.")]
    public int RewardGold;

    [Tooltip("Tamamlaninca verilecek XP miktari.")]
    public int RewardXP;
}
