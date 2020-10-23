using System.Collections;
using UnityEngine;

[System.Serializable]
public class BlockSprite
{
    public Sprite[] Color;
}

public class Block : MonoBehaviour
{
    public BlockSprite[] blockSprites;
    [SerializeField] Sprite fiveBlockSprite;

    [SerializeField] State state;
    [SerializeField] BlockColor blockColor;
    [SerializeField] BlockType blockType;

    [SerializeField] int xPos;
    [SerializeField] int yPos;

    SpriteRenderer spriteRenderer;

    // 모든 블록이 똑같이 공유해도되므로 static~~?
    static Vector2 blockScale = new Vector2(Value.BlockScale, Value.BlockScale);
    static Vector2 selectedScale = blockScale * .8f;
    static MatchContainer matchContainer;
    static GameManager gameManager;
    static  GoalManager goalManager;

    public bool endBomb;

    #region ------- Default Method -------
    private void Awake()
    {
        state = State.IDLE;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (matchContainer == null) matchContainer = FindObjectOfType<MatchContainer>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if( goalManager == null) goalManager = FindObjectOfType<GoalManager>();
    }
    #endregion


    #region ------- Public Method -------
    public void Move(int x, int y)
    {
        if (state == State.MOVE) return;
        StopCoroutine("ScaleCoroutine");
        StartCoroutine(MoveCoroutine(x, y, state));
    }

    public void ScaleFade()
    {
        StopCoroutine("ScaleCoroutine");
        StartCoroutine("ScaleCoroutine", Vector2.zero);
    }

    public void OnHint()
    {
        if (state == State.HINT) return;
        state = State.HINT;
        StopCoroutine("HintCoroutine");
        StartCoroutine("HintCoroutine");
    }

    public void OffHint()
    {
        if (state != State.HINT) return;
        state = State.IDLE;
        StopCoroutine("HintCoroutine");
        ColorReset();
    }

    public void Match()
    {
        if (state == State.MATCH_WAIT || state == State.MATCH_END) return;
        state = State.MATCH_WAIT;
        gameManager._Score = 10;

        // 파티클
        matchContainer.PlayParticleSystem(xPos, yPos);

        // 브레이크블록일 시 hp 감소
        matchContainer.CheckBreakable(xPos, yPos);

        if (gameManager.gameState == GameState.PLAY)
        {
            // 목표 수정
            goalManager.CompareGoal(blockColor);
        }

        StartCoroutine(MatchCoroutine());
    }

    public void Select(out Block block)
    {
        if (state == State.SELECT) // 이미 선택된 블록일 경우 블록 null로 해주기
        {
            Debug.Log("Select");
            // state = State.IDLE; // IDLE로 바꿔주는거 한번 생각해보자
            block = null;
            return;
        }
        block = this;
        state = State.SELECT;
        StartCoroutine(ScaleCoroutine(selectedScale));  // 스케일 0.8로 바꿔주기
    }

    public void DeSelect()
    {
        state = State.IDLE;
        StartCoroutine(ScaleCoroutine(blockScale));
    }

    public void SetRandomBlockColor()
    {
        int i = Random.Range(0, 6);
        spriteRenderer.sprite = blockSprites[0].Color[i];
        blockColor = (BlockColor)i;
    }

    public void BlockReset()
    {
        StopAllCoroutines();
        state = State.IDLE;
        blockType = BlockType.NOMAL;
        transform.localScale = blockScale;
        ColorReset();
        SetRandomBlockColor();
    }


    // Pos설정하는거 한번 수정 생각해보기
    public void SetPos()
    {
        xPos = (int)transform.localPosition.x;
        yPos = (int)transform.localPosition.y;
    }
    #endregion


    #region ------- Private Method -------

    void ColorReset()
    {
        if(spriteRenderer.color.a != 1)
        {
            Color color = spriteRenderer.color;
            color.a = 1;
            spriteRenderer.color = color;
        }
    }

    IEnumerator MoveCoroutine(int x, int y, State preState)
    {
        state = State.MOVE;
        Vector2 startPos = transform.localPosition;
        Vector2 endPos = new Vector2(x, y);
        float startTime = Time.time;

        while (Time.time - startTime <= Value.ChangeDuration)
        {
            transform.localPosition = Vector2.Lerp(startPos, endPos, (Time.time - startTime) / Value.ChangeDuration);
            yield return null;
        }

        state = preState;
        transform.localPosition = endPos;
        SetPos();
        matchContainer.FinishedMoveEvent(this);
    }

    IEnumerator MatchCoroutine()
    {
        yield return StartCoroutine(ScaleCoroutine(Vector2.zero));
        state = State.MATCH_END;
    }

    // 크기 바꾸기
    IEnumerator ScaleCoroutine(Vector2 endScale)
    {
        float startTime = Time.time;
        Vector2 orgScale = transform.localScale;

        while (Time.time - startTime <= Value.ChangeDuration)
        {
            transform.localScale = Vector2.Lerp(orgScale, endScale, (Time.time - startTime) / Value.ChangeDuration);
            yield return null;
        }

        transform.localScale = endScale;
    }
    
    IEnumerator HintCoroutine()
    {
        while (state == State.HINT)
        {
            yield return StartCoroutine(PingPongFade());
            yield return null;
        }
    }

    IEnumerator PingPongFade()
    {
        Color orgColor = spriteRenderer.color;

        float time = Time.time;

        while (Time.time - time <= .5f)
        {
            orgColor.a = Mathf.Lerp(1, 0, (Time.time - time) / .5f);
            spriteRenderer.color = orgColor;
            yield return null;
        }

        time = Time.time;
        while (Time.time - time <= .5f)
        {
            orgColor.a = Mathf.Lerp(0, 1, (Time.time - time) / .5f);
            spriteRenderer.color = orgColor;
            yield return null;
        }

        orgColor.a = 1;
        spriteRenderer.color = orgColor;
    }

    #endregion

    #region ------- Property -------
    public State _State
    {
        get { return state; }
    }

    // 블록 색깔 및 스프라이트 변경
    public BlockColor _BlockColor
    {
        set
        {
            blockColor = value;

            // 렌더러 스프라이트 바꿔주기
            if (value != BlockColor.NONE)
            {
                spriteRenderer.sprite = blockSprites[0].Color[(int)value];
            }
        }
        get { return blockColor; }
    }

    // 블록 타입 및 스프라이트 변경
    public BlockType _BlockType
    {
        set
        {
            blockType = value;
            
            if(blockType == BlockType.FIVE)
            {
                spriteRenderer.sprite = fiveBlockSprite;
            }
            else
            {
                spriteRenderer.sprite = blockSprites[(int)blockType].Color[(int)blockColor];
            }
        }
        get { return blockType; }
    }

    public int _X
    {
        get { return xPos; }
    }

    public int _Y
    {
        get { return yPos; }
    }

    public Vector2 _Pos
    {
        get { return new Vector2(xPos, yPos); }
    }
    #endregion
}
