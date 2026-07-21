using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    // --- Temel Oyuncu Verileri ---
    public int Para;
    public int XP;
    public int OyuncuSeviyesi;

    // --- Upgrade Seviyeleri ---
    public int BaltaSeviyesi;
    public int SepetSeviyesi;
    public int KamyonSeviyesi;
    public int AtolyeSeviyesi;

    // --- Zaman Verileri ---
    public float MevsimZamani;
    public float OyunSuresi;

    // --- Acilan Bolgeler ---
    public List<string> AcilanBolgeler;

    public PlayerData()
    {
        Para = 0;
        XP = 0;
        OyuncuSeviyesi = 1;

        BaltaSeviyesi = 1;
        SepetSeviyesi = 1;
        KamyonSeviyesi = 1;
        AtolyeSeviyesi = 1;

        MevsimZamani = 0f;
        OyunSuresi = 0f;

        AcilanBolgeler = new List<string>();
    }
}
