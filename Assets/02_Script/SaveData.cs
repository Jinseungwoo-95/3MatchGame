
[System.Serializable]
public class SaveData
{
    public int lastStage;
    public int[] stars;
    public bool canMusic;
    public bool canSound;

    public SaveData()
    {
        lastStage = Settings.lastStage;
        stars = Settings.stars;
        canMusic = Settings.canMusic;
        canSound = Settings.canSound;
    }
}

