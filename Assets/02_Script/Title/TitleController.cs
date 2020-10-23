using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class TitleController : MonoBehaviour
{
    [SerializeField] Button titleButton;

    private void Awake()
    {
        LoadSettings();
        titleButton.onClick.AddListener(SceneChange);
    }

    void LoadSettings()
    {
        string path = Application.persistentDataPath + "/Settings.dat";
        if (!File.Exists(path)) return; // 파일이 없으면 리턴~

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.Open(path, FileMode.Open);
        SaveData data = (SaveData)formatter.Deserialize(file);
        file.Close();

        // 셋팅 설정
        Settings.lastStage = data.lastStage;
        Settings.stars = data.stars;
        Settings.canMusic = data.canMusic;
        Settings.canSound = data.canSound;
    }

    void SceneChange()
    {
        SceneManager.LoadScene("Stage");
    }
}
