using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GoalBlock
{
    public int numberNeeded;   // 목표 개수
    public Sprite goalSprite;
    public BlockColor color;
}

public class GoalManager : MonoBehaviour
{
    public GoalBlock[] goalBlocks;
    public int[] numberCollected;
    public GameObject goalPrefab;
    public GameObject goalParent;
    public GameObject panelgoalParent;
    public List<GoalPanel> currentGoals = new List<GoalPanel>();
    [SerializeField] Animator goalAnim;

    void Start()
    {
        MatchContainer matchContainer = FindObjectOfType<MatchContainer>();
        // $ 콜렉트된 블록 개수 설정하는거 다시 생각하기
        goalBlocks = (GoalBlock[])matchContainer.world.levels[matchContainer.level].goalBlocks.Clone();
        numberCollected = new int[goalBlocks.Length];

        SetGoals();
    }

    void SetGoals()
    {
        for (int i = 0; i < goalBlocks.Length; ++i)
        {
            // Goal Panel
            GameObject goal = Instantiate(goalPrefab, panelgoalParent.transform);
            GoalPanel goalPanel = goal.GetComponent<GoalPanel>();
            goal.transform.localScale = new Vector3(2, 2, 1);
            goalPanel.goalSprite = goalBlocks[i].goalSprite;
            goalPanel.textString = "0/" + goalBlocks[i].numberNeeded;

            // Top Panel
            goal = Instantiate(goalPrefab, goalParent.transform.position, Quaternion.identity, goalParent.transform);
            goalPanel = goal.GetComponent<GoalPanel>();
            goalPanel.goalSprite = goalBlocks[i].goalSprite;
            goalPanel.textString = "0/" + goalBlocks[i].numberNeeded;
            currentGoals.Add(goalPanel);
        }
    }

    private void UpdateGoals()
    {
        int goalsCompleted = 0;

        for (int i = 0; i < goalBlocks.Length; ++i)
        {
            if (numberCollected[i] >= goalBlocks[i].numberNeeded)
            {
                goalsCompleted++;
                currentGoals[i].goalText.text = goalBlocks[i].numberNeeded + "/" + goalBlocks[i].numberNeeded;
            }
            else
                currentGoals[i].goalText.text = numberCollected[i] + "/" + goalBlocks[i].numberNeeded;
        }

        if (goalsCompleted >= goalBlocks.Length)
        {
            FindObjectOfType<GameManager>().WinGame();
            Debug.Log("You win");
        }
    }

    public void CompareGoal(BlockColor _color)
    {
        for (int i = 0; i < goalBlocks.Length; ++i)
        {
            if (goalBlocks[i].color == _color)
            {
                numberCollected[i] += 1;
                UpdateGoals();
                break;
            }
        }
    }
}
