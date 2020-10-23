using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class StageManager : MonoBehaviour
{
    //[SerializeField] Canvas canvas;
    //[SerializeField] GameObject stageGroup;
    //RectTransform rectTransform;    // 스테이지그룹 rT
    //bool rangeCheck;    // 스테이지 그룹 범위에 클릭되었는지 체크
    //Vector2 firMousePos;
    //GraphicRaycaster gr;
    //PointerEventData ped;
    //float maxHeight;

    GameObject currentPanel;
    bool openPanel = false;

    [SerializeField] GameObject[] stars;    // 스테이지 눌렸을 때 별 오브젝트
    [SerializeField] Toggle music, sound;

    [SerializeField] GameObject settingPanel;
    [SerializeField] GameObject confirmPanel;
    [SerializeField] GameObject quitPanel;

    [SerializeField] int selectLevel;
    [SerializeField] GameObject stagePrefab;
    [SerializeField] Transform stageParent;
    [SerializeField] Sprite lockImage;
    [SerializeField] Sprite[] starImage;

    [SerializeField] Toggle musicToggle;
    [SerializeField] Toggle SoundToggle;

    void Awake()
    {
        //rectTransform = stageGroup.GetComponent<RectTransform>();
        //maxHeight = rectTransform.sizeDelta.y - 540;
        //gr = canvas.GetComponent<GraphicRaycaster>();
        //ped = new PointerEventData(null);

        // 스테이지 셋팅
        for (int i = 0; i < Value.STAGECNT; ++i)
        {
            GameObject obj = Instantiate(stagePrefab, stageParent);
            Button button = obj.GetComponent<Button>();
            int stageNum = i + 1;

            obj.name = "Stage" + stageNum;
            
            if (i < Settings.lastStage)
            {
                button.enabled = true;
                button.onClick.AddListener(() => StageClick(stageNum));
                obj.GetComponentInChildren<Text>().text = stageNum.ToString();
                // 별 개수에 따라 이미지 변경
                if (Settings.stars[i] == 0)
                {
                    obj.transform.GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    obj.transform.GetChild(0).GetComponent<Image>().sprite = starImage[Settings.stars[i]];
                }
            }
            else
            {
                button.enabled = false;
                obj.GetComponent<Image>().sprite = lockImage;
                obj.transform.GetChild(0).gameObject.SetActive(false);
                obj.transform.GetChild(1).gameObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        musicToggle.isOn = Settings.canMusic;
        SoundToggle.isOn = Settings.canSound;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (openPanel)
            {
                currentPanel.SetActive(false);
                openPanel = false;
            }
            else
            {
                currentPanel = quitPanel;
                currentPanel.SetActive(true);
                openPanel = true;
            }
        }

        //if (Input.GetMouseButtonDown(0))
        //{
        //    firMousePos = Input.mousePosition;

        //    ped.position = firMousePos;
        //    List<RaycastResult> results = new List<RaycastResult>();    // 히트 된 개체 저장

        //    gr.Raycast(ped, results);
        //    if(results.Count >0)
        //    {
        //        GameObject obj = results[0].gameObject;
        //        if(obj.CompareTag("MaskPanel"))
        //        {
        //            rangeCheck = true;
        //        }
        //        else
        //        {
        //            rangeCheck = false;
        //        }
        //    }
        //}

        //if(rangeCheck && Input.GetMouseButton(0))
        //{
        //    Vector2 secMousePos = Input.mousePosition;
        //    Vector2 pos = rectTransform.anchoredPosition;

        //    // 마우스 밑으로 이동
        //    if (firMousePos.y - secMousePos.y > 0)
        //    {
        //        if (pos.y > 0) pos.y -= 10;
        //        else pos.y = 0;
        //    }
        //    // 위로 이동
        //    else if(firMousePos.y - secMousePos.y < 0)
        //    {
        //        if (pos.y < maxHeight) pos.y += 10;
        //        else pos.y = maxHeight;
        //    }
        //    rectTransform.anchoredPosition = pos;

        //    firMousePos = secMousePos;
        //}
    }

    // 스테이지 클릭 함수
    public void StageClick(int stageNum)
    {
        openPanel = true;
        currentPanel = confirmPanel;
        selectLevel = stageNum;
        Text stageText = confirmPanel.GetComponentInChildren<Text>();
        stageText.text = "STAGE " + selectLevel.ToString();

        // $ 레벨에 따른 별 이미지 추가해주기
        int starNum = Settings.stars[stageNum - 1];
        for (int i = 0; i < 3; ++i)
        {
            if (i < starNum)
                stars[i].SetActive(true);
            else
                stars[i].SetActive(false);
        }
        currentPanel.SetActive(true);
    }

    public void OnButtonClick(Button button)
    {
        switch (button.name)
        {
            case "BtnClose":
                currentPanel.SetActive(false);
                openPanel = false;
                break;
            case "BtnSetting":
                currentPanel = settingPanel;
                currentPanel.SetActive(true);
                openPanel = true;
                break;
            case "BtnPlay":
                PlayerPrefs.SetInt("SelectLevel", selectLevel - 1);    // 선택한 레벨 저장하기
                SceneManager.LoadScene("Game");
                break;
            case "BtnQuit":
                SaveSettings();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    void SaveSettings()
    {
        SaveData data = new SaveData();
        string path = Application.persistentDataPath + "/Settings.dat";

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.Open(path, FileMode.Create);

        formatter.Serialize(file, data);
        file.Close();
    }

    public void OnToggleClick(Toggle toggle)
    {
        switch (toggle.name)
        {
            case "ToggleMusic":
                Settings.canMusic = toggle.isOn;
                break;
            case "ToggleSound":
                Settings.canSound = toggle.isOn;
                break;
        }
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }
}
