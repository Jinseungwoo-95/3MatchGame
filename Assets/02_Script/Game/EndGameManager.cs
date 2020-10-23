using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EndType
{
    MOVES,
    TIME
}

[System.Serializable]
public class EndGameRequirement
{
    public EndType endType;
    public int count;
}

public class EndGameManager : MonoBehaviour
{
    public EndGameRequirement requirement;
    public Text endTypeText;
    public Text countText;
    int currentCount;
    float timerSeconds;
    GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        MatchContainer matchContainer = FindObjectOfType<MatchContainer>();
        requirement = matchContainer.world.levels[matchContainer.level].endGameRequirement;

        gameManager = FindObjectOfType<GameManager>();
        SetInfo();
    }

    void SetInfo()
    {
        currentCount = requirement.count;
        if (requirement.endType == EndType.MOVES)
        {
            endTypeText.text = "남은 이동";
        }
        else
        {
            timerSeconds = 1;
            endTypeText.text = "남은 시간";
        }
        countText.text = "" + currentCount;
    }

    public void DecreaseCount()
    {
        currentCount--;
        countText.text = "" + currentCount;
        if (currentCount <= 0 && gameManager.gameState == GameState.PLAY)
        {
            gameManager.LoseGame();
        }
    }

    void Update()
    {
        if (gameManager.gameState == GameState.PLAY)
        {
            if (requirement.endType == EndType.TIME && currentCount > 0)
            {
                timerSeconds -= Time.deltaTime;
                if (timerSeconds <= 0)
                {
                    DecreaseCount();
                    timerSeconds = 1;
                }
            }
        }
    }

    public int _CurrentCount
    {
        get { return currentCount; }
    }
}
