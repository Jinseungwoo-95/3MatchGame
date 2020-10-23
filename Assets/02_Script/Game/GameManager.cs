using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public enum GameState
{
    WAIT,
    PLAY,
    PAUSE,
    WIN,
    LOSE
}

public class GameManager : MonoBehaviour
{
    public GameState gameState;

    [SerializeField] float score = 0;
    [SerializeField] GameObject[] stars;

    float goalScore;
    GameObject pnPause;
    GameObject pnClear;
    GameObject pnFail;
    [SerializeField] GameObject clearText;
    MatchContainer matchContainer;
    Image scoreBar;
    Text scoreText;

    #region ------- Default Method -------
    private void Awake()
    {
        gameState = GameState.WAIT;
        clearText = GameObject.Find("ClearText");
        clearText.SetActive(false);
        matchContainer = FindObjectOfType<MatchContainer>();
        goalScore = matchContainer.world.levels[matchContainer.level].goalScore;
        stars = GameObject.FindGameObjectsWithTag("Star");
        
        InitGame();
    }
    #endregion

    #region ------- Public Method -------
    public void OnButtonClick(GameObject button)
    {
        switch (button.name)
        {
            case "GoalBtnOK":
                gameState = GameState.PLAY;
                GameObject.Find("GoalPanel").SetActive(false);
                break;
            case "BtnPause":
                gameState = GameState.PAUSE;
                pnPause.SetActive(true);
                break;
            case "BtnPlay":
                gameState = GameState.PLAY;
                pnPause.SetActive(false);
                break;
            case "BtnReload":
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
            case "BtnReturn":
            case "BtnOK":
                SceneManager.LoadScene("Stage");
                break;
                
        }
    }
    
    public int SetStar()
    {
        if (score > goalScore) return 3;
        else if (score > goalScore * 0.7f) return 2;
        else if (score > goalScore * 0.5f) return 1;
        else return 0;
    }

    public void WinGame()
    {
        gameState = GameState.WIN;

        clearText.SetActive(true);
        // $ 남은 시간 or 이동 만큼 스코어 변경해주기
        matchContainer.EndMoveEvent(FindObjectOfType<EndGameManager>()._CurrentCount);
    }

    public void WinGameEnd()
    {
        int starNum = SetStar();

        int level = matchContainer.level;
        if (level + 1 == Settings.lastStage)
        {
            ++Settings.lastStage;
            Settings.stars[matchContainer.level] = starNum;
        }
        else
        {
            // 이미 클리어했던 스테이지의 별 개수보다 많으면 값 바꿔주기
            if (Settings.stars[level] < starNum)
            {
                Settings.stars[level] = starNum;
            }
        }

        for (int i = 0; i < 3; ++i)
        {
            if (i < starNum)
                stars[i].SetActive(true);
            else
                stars[i].SetActive(false);
        }

        pnClear.SetActive(true);
    }

    public void LoseGame()
    {
        gameState = GameState.LOSE;
        pnFail.SetActive(true);
    }
    #endregion

    #region ------- Private Method -------

    private void OnApplicationQuit()
    {
        SaveSettings();
    }

    private void OnApplicationPause(bool pause)
    {
        if(pause)
        {
            // 어플레케이션을 내리는 순간에 처리할 행동
            gameState = GameState.PAUSE;
        }
        else
        {
            if(gameState == GameState.PAUSE)
            {
                gameState = GameState.PLAY;
            }
        }
    }

    void SaveSettings()
    {
        Debug.Log("Save");
        SaveData data = new SaveData();
        string path = Application.persistentDataPath + "/Settings.dat";

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.Open(path, FileMode.Create);

        formatter.Serialize(file, data);
        file.Close();
    }

    void InitGame()
    {
        gameState = GameState.PAUSE;

        pnPause = GameObject.Find("PausePanel");
        pnPause.SetActive(false);

        pnClear = GameObject.Find("ClearPanel");
        pnClear.SetActive(false);
        pnFail = GameObject.Find("FailPanel");
        pnFail.SetActive(false);

        scoreBar = GameObject.Find("ScoreBar").GetComponent<Image>();
        scoreBar.fillAmount = 0;

        scoreText = GameObject.Find("ScoreText").GetComponent<Text>();
        scoreText.text = score.ToString();
    }
    
    #endregion

    public float _Score
    {
        set
        {
            score += value;
            scoreText.text = score.ToString();
            scoreBar.fillAmount = score / goalScore;
        }
    }
}
