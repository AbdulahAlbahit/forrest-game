using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public int Para;
    public int XP;
    public int OyuncuSeviyesi;
    public int BaltaSeviyesi;
    public int SepetSeviyesi;
    public int KamyonSeviyesi;
    public int AtolyeSeviyesi;
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
        AcilanBolgeler = new List<string>();
    }
}
