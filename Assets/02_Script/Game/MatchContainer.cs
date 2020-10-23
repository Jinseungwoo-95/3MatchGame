using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TileType
{
    public int x;
    public int y;
    public TileKind tileKind;
}

public class MatchContainer : MonoBehaviour
{
    public World world;
    public int level;   // 인덱스 번호로 오기때문에 1이 더 작음

    public Block[,] blockArray;
    public BackgroundBlock[,] bgBlocks;

    [SerializeField] TileType[] tileTypes;

    // $ 나중에 오브젝트 풀링 큐로 바꾸는거 생각해보기
    List<ParticleSystem> particleList;
    List<MatchData> matchList = new List<MatchData>();
    List<Block> endBlocks = new List<Block>();  // State.MATCH_END인 블록 저장 List

    [SerializeField] GameObject blockPrefab;
    [SerializeField] ParticleSystem particlePrefab;
    [SerializeField] GameObject bgBlockPrefab;

    [SerializeField] int width;   // 가로 블록 개수
    [SerializeField] int height;  // 세로 블록 개수

    [SerializeField] GameManager gameManager;
    [SerializeField] EndGameManager endGameManager;

    Camera camera;
    Block firBlock, secBlock;
    Block hintBlock;
    BlockColor fiveBlockColor;  // 파이브 블록할 때 처리할 블록색깔
    Vector2 firMousePos;
    float moveDistance = .9f;
    float preMatchTime;
    float hintDelay = 3f;

    bool reMatchDownCo;
    Coroutine matchDownCo;

    #region ------- Default Method -------
    private void Awake()
    {
        // 레벨
        if(PlayerPrefs.HasKey("SelectLevel"))
        {
            level = PlayerPrefs.GetInt("SelectLevel");
        }

        if(world != null)
        {
            if(world.levels[level] != null)
            {
                width = world.levels[level].width;
                height = world.levels[level].height;
                tileTypes = (TileType[])world.levels[level].tileTypes.Clone();
            }
        }
        
        blockArray = new Block[width, height];
        bgBlocks = new BackgroundBlock[width, height];

        camera = Camera.main;
        preMatchTime = Time.time;
        float offset = Value.BlockScale / 2;

        transform.position = new Vector2(-(width / 2f) + offset, -(height / 2f) + offset);

        gameManager = FindObjectOfType<GameManager>();
        endGameManager = FindObjectOfType<EndGameManager>();

        //CameraSize();
        InitializeBGBlock();
        InitializeBlock();
        InitializeParticle();
        SetHintBlock();
    }

    private void Update()
    {
        if (gameManager.gameState == GameState.PLAY)
        {
            // 힌트
            if (Time.time - preMatchTime >= hintDelay)
            {
                // $ 블록이 변경되었을 경우 체크해주는게 더 좋지 않을까?
                preMatchTime = Time.time;

                hintBlock.OnHint();
            }

            // 마우스 클릭 이벤트
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 pos = camera.ScreenToWorldPoint(Input.mousePosition);
                // 블록리스트 범위 안에 있을 경우
                if (pos.x > -(width / 2f) && pos.x < width / 2f && pos.y > -(height / 2f) && pos.y < height / 2f)
                {
                    if (!IsMoving())
                    {
                        int x = (int)(pos.x + width / 2f);  // width / 2f 해주는 이유는 왼쪽 맨 밑이 (0 , 0) 이므로
                        int y = (int)(pos.y + height / 2f);

                        if (blockArray[x, y] != null)
                        {
                            BlockClickEvent(x, y);
                        }
                    }
                }
            }

            // 마우스 드래그
            if (firBlock && !secBlock && Input.GetMouseButton(0))
            {
                Vector2 secMousePos = camera.ScreenToWorldPoint(Input.mousePosition);
                if (secMousePos.x > -(width / 2f) && secMousePos.x < width / 2f && secMousePos.y > -(height / 2f) && secMousePos.y < height / 2f)
                {
                    if (!IsMoving())
                    {
                        // right
                        if (firMousePos.x - secMousePos.x < -moveDistance && firBlock._X < width - 1 && blockArray[firBlock._X + 1, firBlock._Y] != null)
                        {
                            blockArray[firBlock._X + 1, firBlock._Y].Select(out secBlock);
                            CompleteSelect();
                        }
                        //left
                        else if (firMousePos.x - secMousePos.x > moveDistance && firBlock._X > 0 && blockArray[firBlock._X - 1, firBlock._Y] != null)
                        {
                            blockArray[firBlock._X - 1, firBlock._Y].Select(out secBlock);
                            CompleteSelect();
                        }
                        //top
                        else if (firMousePos.y - secMousePos.y < -moveDistance && firBlock._Y < height - 1 && blockArray[firBlock._X, firBlock._Y + 1] != null)
                        {
                            blockArray[firBlock._X, firBlock._Y + 1].Select(out secBlock);
                            CompleteSelect();
                        }
                        // bottom
                        else if (firMousePos.y - secMousePos.y > moveDistance && firBlock._Y > 0 && blockArray[firBlock._X, firBlock._Y - 1] != null)
                        {
                            blockArray[firBlock._X, firBlock._Y - 1].Select(out secBlock);
                            CompleteSelect();
                        }
                    }
                }
                else
                {
                    firBlock.DeSelect();
                    firBlock = null;
                }
            }

            // 첫번째 클릭하고 두번째에서 제대로 된 클릭 했을 경우
            if (Input.GetMouseButtonUp(0) && firBlock && secBlock)
            {
                CompleteSelect();
            }
        }
    }
    #endregion

    #region ------- Public Method -------

    public void FinishedMoveEvent(Block block)
    {
        if (FindMatchAtBlock(block))
        {
            MatchProcess();
        }
    }

    // 게임 끝 이벤트
    public void EndMoveEvent(int count)
    {
        StartCoroutine(EndMoveCoroutine(count));
    }

    public void PlayParticleSystem(int x, int y)
    {
        ParticleSystem particle = GetParticleSystem();
        particle.transform.localPosition = new Vector3(x, y, 0);
        particle.gameObject.SetActive(true);
        particle.Play();
    }

    public void CheckBreakable(int x, int y)
    {
        if(bgBlocks[x,y]._TileKind == TileKind.BREAKABLE)
        {
            bgBlocks[x, y].DecreaseHP();
        }
    }
    #endregion

    #region ------- Private Method -------

    // Awake()

    void CameraSize()
    {
        //Debug.Log(Screen.width);
        //Debug.Log("he" + Screen.height);

        //if (Screen.width > Screen.height)
        //{
        //    float hRatio = 1f;
        //    float wRatio = (float)Screen.width / Screen.height;

        //    float hViewSize = height / (hRatio * 2);
        //    float wViewSize = width / (wRatio * 2);

        //    camera.orthographicSize = hViewSize >= wViewSize ? hViewSize : wViewSize;
        //}
        //else if (Screen.width < Screen.height)
        //{
        //    float hRatio = (float)Screen.height / Screen.width;
        //    float wRatio = 1;

        //    float hViewSize = height / (wRatio * 2);
        //    float wViewSize = width / (hRatio * 2);

        //    camera.orthographicSize = hViewSize >= wViewSize ? hViewSize : wViewSize;
        //}
        //else
        //    camera.orthographicSize = width / 2f;
    }

    void InitializeBlock()
    {
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                if (bgBlocks[x, y]._TileKind == TileKind.BLANK) continue;

                GameObject obj = Instantiate(blockPrefab, transform) as GameObject;
                obj.transform.localPosition = new Vector2(x, y);

                Block block = obj.GetComponent<Block>();
                blockArray[x, y] = block;

                block.SetPos();
            }
        }

        // BlockType 설정해주기(매치되는 블록은 다시하도록)
        AllBlockColorSet();
    }

    void AllBlockColorSet()
    {
        if (blockArray == null) return;

        foreach (Block block in blockArray)
        {
            if (block == null) continue;

            do
            {
                block.SetRandomBlockColor();
            }
            while (CheckMatch(block));
        }
    }

    // 매치가 되는 검사
    bool CheckMatch(Block block)
    {
        BlockColor blockType = block._BlockColor;
        int x = block._X;
        int y = block._Y;

        // 왼쪽 아래부터 채우므로 오른쪽 위는 검사할 필요가 없음.
        // 왼쪽
        if(x > 1 && blockArray[x - 1, y] != null && blockArray[x - 2, y] != null)
        {
            if (blockType == blockArray[x - 1, y]._BlockColor && blockType == blockArray[x - 2, y]._BlockColor) return true;
        }

        // 아래
        if (y > 1 && blockArray[x, y - 1] != null && blockArray[x, y - 2] != null)
        {
            if (blockType == blockArray[x, y - 1]._BlockColor && blockType == blockArray[x, y - 2]._BlockColor) return true;
        }
        
        // 매치 안된 경우
        return false;
    }

    void InitializeParticle()
    {
        particleList = new List<ParticleSystem>();
        for (int i = 0; i < 30; ++i)
        {
            ParticleSystem obj = Instantiate(particlePrefab, transform);
            obj.gameObject.SetActive(false);
            particleList.Add(obj);
        }
    }

    void InitializeBGBlock()
    {
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                GameObject obj = Instantiate(bgBlockPrefab, transform) as GameObject;
                obj.transform.localPosition = new Vector2(x, y);
        
                BackgroundBlock bgBlock = obj.GetComponent<BackgroundBlock>();
                bgBlocks[x, y] = bgBlock;
            }
        }

        // 타일 종류 설정
        for (int i = 0; i < tileTypes.Length; ++i)
        {
            bgBlocks[tileTypes[i].x, tileTypes[i].y]._TileKind = tileTypes[i].tileKind;
        }
    }

    void SetHintBlock()
    {
        foreach (Block block in blockArray)
        {
            if (block == null) continue;

            if (CheckMatchAtBlock(block))
            {
                hintBlock = block;
                break;
            }
        }
    }
    // ---------


    // Update

    void CompleteSelect()
    {
        StartCoroutine(SwapCoroutine());
        firBlock = null;
        secBlock = null;
    }

    // 블록 근처 매치 가능한지 체크
    bool CheckMatchAtBlock(Block block)
    {
        //left
        if (CompareBlockColor(block, -1, 1, -1, 2)) return true;
        if (CompareBlockColor(block, -1, -1, -1, -2)) return true;
        if (CompareBlockColor(block, -1, 1, -1, -1)) return true;
        if (CompareBlockColor(block, -2, 0, -3, 0)) return true;
        //right
        if (CompareBlockColor(block, 1, 1, 1, 2)) return true;
        if (CompareBlockColor(block, 1, -1, 1, -2)) return true;
        if (CompareBlockColor(block, 1, 1, 1, -1)) return true;
        if (CompareBlockColor(block, 2, 0, 3, 0)) return true;
        //top
        if (CompareBlockColor(block, 1, 1, 2, 1)) return true;
        if (CompareBlockColor(block, -1, 1, -2, 1)) return true;
        if (CompareBlockColor(block, -1, 1, 1, 1)) return true;
        if (CompareBlockColor(block, 0, 2, 0, 3)) return true;
        //bottom
        if (CompareBlockColor(block, 1, -1, 2, -1)) return true;
        if (CompareBlockColor(block, -1, -1, -2, -1)) return true;
        if (CompareBlockColor(block, -1, -1, 1, -1)) return true;
        if (CompareBlockColor(block, 0, -2, 0, -3)) return true;

        return false;
    }

    bool CompareBlockColor(Block block, int fx, int fy, int sx, int sy)
    {
        BlockColor blockColor = block._BlockColor;
        int x = block._X;
        int y = block._Y;

        int x1 = x + fx;
        int y1 = y + fy;
        int x2 = x + sx;
        int y2 = y + sy;

        if (RangeCheck(x1, y1) && RangeCheck(x2, y2))
        {
            if (blockArray[x1, y1] != null && blockArray[x2, y2] != null)
            {
                return ((blockColor == blockArray[x1, y1]._BlockColor) && (blockColor == blockArray[x2, y2]._BlockColor));
            }
            return false;
        }
        else
            return false;
    }

    // 마우스로 인한 블록 클릭 함수
    void BlockClickEvent(int x, int y)
    {
        //if (blockArray[x, y]._State == State.MOVE || blockArray[x, y]._State == State.MATCH_END) return;

        // 첫번째 블록이 널이라면
        if (firBlock == null)
        {
            firMousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            blockArray[x, y].Select(out firBlock);
        }
        // 두번째 블록 널 && 첫번째 블록이 선택한 블록이랑 다를 경우
        else if (firBlock != blockArray[x, y])// !secBlock && 
        {
            int disX = firBlock._X - x;
            int disY = firBlock._Y - y;

            // 1칸 이상 거리되는 블록 선택했을 때 첫번째 블록 없애기
            if (Mathf.Abs(disX) > 1 || Mathf.Abs(disY) > 1)
            {
                firBlock.DeSelect();
                firBlock = null;
                return;
            }
            blockArray[x, y].Select(out secBlock);   // 아닐 경우 두번째 블록 선택
        }
        // 첫번째 블록을 또 눌렸을 경우 실행
        else
        {
            if (firBlock)
            {
                firBlock.DeSelect();
                firBlock = null;
            }
        }
    }

    IEnumerator SwapCoroutine()
    {
        // f, s 변수에 따로 저장하고 다음 클릭 할 수 있도록 하기 위해서!!!!
        Block f = firBlock;
        Block s = secBlock;

        Vector2 firPos = f.transform.localPosition;
        Vector2 secPos = s.transform.localPosition;

        float startTime = Time.time;

        while (Time.time - startTime <= Value.ChangeDuration)
        {
            f.transform.localPosition = Vector2.Lerp(firPos, secPos, (Time.time - startTime) / Value.ChangeDuration);
            s.transform.localPosition = Vector2.Lerp(secPos, firPos, (Time.time - startTime) / Value.ChangeDuration);
            yield return null;
        }

        f.transform.localPosition = secPos;
        s.transform.localPosition = firPos;

        Swap(f, s);

        // Five 블록 처리
        if ((f._BlockType == BlockType.FIVE && s._BlockType == BlockType.NOMAL) || (f._BlockType == BlockType.NOMAL && s._BlockType == BlockType.FIVE))
        {
            fiveBlockColor = (f._BlockType == BlockType.NOMAL) ? f._BlockColor : s._BlockColor;
            BlockProcess(f._BlockType == BlockType.NOMAL ? s : f);
        }
        // 나머지 블록 처리
        else
        {
            bool matchFirst = FindMatchAtBlock(f);
            bool matchSecond = FindMatchAtBlock(s);

            if (!matchFirst) f.DeSelect();
            if (!matchSecond) s.DeSelect();

            // 매치되는 블록이 없을 경우 다시 위치 바꿔주기
            if (!matchFirst && !matchSecond)
            {
                startTime = Time.time;

                while (Time.time - startTime <= Value.ChangeDuration)
                {
                    f.transform.localPosition = Vector2.Lerp(secPos, firPos, (Time.time - startTime) / Value.ChangeDuration);
                    s.transform.localPosition = Vector2.Lerp(firPos, secPos, (Time.time - startTime) / Value.ChangeDuration);
                    yield return null;
                }

                f.transform.localPosition = firPos;
                s.transform.localPosition = secPos;

                Swap(f, s);
            }
            else
            {
                if (matchFirst || matchSecond)
                {
                    if (endGameManager.requirement.endType == EndType.MOVES) // && gameManager.gameState == GameState.PLAY
                    {
                        endGameManager.DecreaseCount();
                    }
                }

                if (matchList.Count != 0)
                {
                    MatchProcess();
                }
            }
        }
    }

    void Swap(Block firBlock, Block secBlock)
    {
        // 배열 값 변경
        blockArray[firBlock._X, firBlock._Y] = secBlock;
        blockArray[secBlock._X, secBlock._Y] = firBlock;

        // xPos, yPos 값 변경
        firBlock.SetPos();
        secBlock.SetPos();
    }

    // 블록의 위치에 매치되는 블록 찾아서 matchList 추가해주기
    bool FindMatchAtBlock(Block block)
    {
        if (block._State == State.MATCH_END ) return false;

        bool match = false;

        bool hor, ver, left, right, top, bottom;
        int leftLength, rightLength, topLength, bottomLegth;

        leftLength = rightLength = topLength = bottomLegth = 0;

        hor = CompareBlockColor(block, -1, 0, 1, 0);
        ver = CompareBlockColor(block, 0, -1, 0, 1);
        left = CompareBlockColor(block, -1, 0, -2, 0);
        right = CompareBlockColor(block, 1, 0, 2, 0);
        top = CompareBlockColor(block, 0, 1, 0, 2);
        bottom = CompareBlockColor(block, 0, -1, 0, -2);

        match = hor || ver || left || right || top || bottom;


        if (match)
        {
            leftLength = rightLength = hor ? 1 : 0;
            if (left) leftLength = 2;
            if (right) rightLength = 2;

            topLength = bottomLegth = ver ? 1 : 0;
            if (top) topLength = 2;
            if (bottom) bottomLegth = 2;

            MatchData matchData = new MatchData(block);

            int x = block._X;
            int y = block._Y;

            for (int i = 1; i <= leftLength; ++i) matchData.horBlocks.Enqueue(blockArray[x - i, y]);
            for (int i = 1; i <= rightLength; ++i) matchData.horBlocks.Enqueue(blockArray[x + i, y]);
            for (int i = 1; i <= topLength; ++i) matchData.verBlocks.Enqueue(blockArray[x, y + i]);
            for (int i = 1; i <= bottomLegth; ++i) matchData.verBlocks.Enqueue(blockArray[x, y - i]);

            matchList.Add(matchData);
        }

        return match;
    }

    // 매치된 블록들 처리
    void MatchProcess()
    {
        hintBlock.OffHint();

        preMatchTime = Time.time;

        Debug.Log(matchList.Count);

        foreach (MatchData data in matchList)
        {
            if (data.moveBlock._State == State.MATCH_END) continue;
            Block block = data.moveBlock;

            int horLength = data.horBlocks.Count;
            int verLength = data.verBlocks.Count;

            // 블록 처리
            if (block._BlockType == BlockType.NOMAL)
            {
                if ((horLength == 4) || (verLength == 4))   // five
                {
                    block._BlockType = BlockType.FIVE;
                    block._BlockColor = BlockColor.NONE;
                    block.DeSelect();
                }
                else if ((horLength == 2) && (verLength == 2)) // bomb
                {
                    block._BlockType = BlockType.BOMB;
                    block.DeSelect();
                }
                else if (horLength == 3) // ver 생성
                {
                    block._BlockType = BlockType.VERTICAL;
                    block.DeSelect();
                }
                else if (verLength == 3)   // hor 생성
                {
                    block._BlockType = BlockType.HORIZONTAL;
                    block.DeSelect();
                }
                else
                {
                    block.Match();
                }
            }
            else
            {
                BlockProcess(block);
            }

            SideBlockProcess(data);
        }

        matchList.Clear();
    }

    // 사이드 블록 처리
    void SideBlockProcess(MatchData data)
    {
        foreach (Block block in data.horBlocks)
        {
            BlockProcess(block);
        }

        foreach (Block block in data.verBlocks)
        {
            BlockProcess(block);
        }
    }

    // 블록 Type에 따른 처리
    void BlockProcess(Block block)
    {
        Block matchBlock;
        
        switch(block._BlockType)
        {
            case BlockType.NOMAL:
                block.Match();
                break;
            case BlockType.HORIZONTAL:
                int yPos = block._Y;
                for (int x = 0; x < width; ++x)
                {
                    if (blockArray[x, yPos] != null)
                    {
                        matchBlock = blockArray[x, yPos];
                        if (matchBlock.endBomb == false)
                        {
                            if (matchBlock._BlockType == BlockType.VERTICAL || matchBlock._BlockType == BlockType.BOMB)
                            {
                                BlockProcess(matchBlock);
                            }
                            matchBlock.Match();
                        }
                    }
                }
                break;
        
            case BlockType.VERTICAL:
                int xPos = block._X;
                for(int y = 0; y < height; ++y)
                {
                    if (blockArray[xPos, y] != null)
                    {
                        matchBlock = blockArray[xPos, y];
                        if (matchBlock.endBomb == false)
                        {
                            if (matchBlock._BlockType == BlockType.HORIZONTAL || matchBlock._BlockType == BlockType.BOMB)
                            {
                                BlockProcess(matchBlock);
                            }
                            matchBlock.Match();
                        }
                    }
                }
                break;
        
            case BlockType.BOMB:
                xPos = block._X;
                yPos = block._Y;
                for (int x = -1; x < 2; ++x)
                {
                    int x1 = xPos + x;
                    for (int y = -1; y < 2; ++y)
                    {
                        int y1 = yPos + y;

                        if (RangeCheck(x1, y1))
                        {
                            if (blockArray[x1, y1] != null)
                            {
                                matchBlock = blockArray[x1, y1];
                                if (matchBlock.endBomb == false)
                                {
                                    if (matchBlock._BlockType == BlockType.HORIZONTAL || matchBlock._BlockType == BlockType.VERTICAL)
                                    {
                                        BlockProcess(matchBlock);
                                    }
                                    blockArray[x1, y1].Match();
                                }
                            }
                        }
                    }
                }
                break;

            case BlockType.FIVE:
                block.Match();
                for (int x = 0; x < width; ++x)
                {
                    for (int y = 0; y < height; ++y)
                    {
                        if (blockArray[x, y] == null) continue;

                        if(blockArray[x,y]._BlockColor == fiveBlockColor)
                        {
                            blockArray[x, y].Match();
                        }
                    }
                }
                break;
        }

        // MatchDownCo가 이미 실행중인지 체크
        if (matchDownCo == null)
        {
            matchDownCo = StartCoroutine(MatchDownCo());
        }
        else
        {
            reMatchDownCo = true;
        }
    }

    // 매치된 블록 다시 내려주는 코루틴
    IEnumerator MatchDownCo()
    {

        yield return new WaitForSeconds(.3f);
        int yPos;
        int count;
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                if (blockArray[x, y] == null)
                {
                    continue;
                }

                Block block = blockArray[x, y];

                if (block._State == State.MATCH_END)
                {
                    // 매치된 블록을 리스트에 추가하고 위치를 맨위로 보내기.
                    if (!endBlocks.Contains(block))
                    {
                        endBlocks.Add(block);
                        block.transform.localPosition = new Vector2(x, height);
                    }
                }
                // 매치된 자리 채우기위해 매치안된 블록들 밑으로 이동
                else if (endBlocks.Count != 0)
                {
                    count = endBlocks.Count;
                    yPos = y;
                    while (count != 0)
                    {
                        if (blockArray[x, yPos - 1] != null)
                        {
                            --count;
                        }

                        --yPos;
                    }

                    block.Move(x, yPos);
                    Swap(x, y, x, yPos);
                }
            }

            count = 0;

            // 매치된 블록 밑으로 이동
            for (int i = 0; i < endBlocks.Count; ++i)
            {
                yPos = height - i - 1;
                while (blockArray[x, yPos - count] == null)
                {
                    ++count;
                }

                Block block = blockArray[x, yPos - count];
                block.BlockReset();

                block.Move(x, yPos - count);
            }

            endBlocks.Clear();
        }

        yield return StartCoroutine(CheckCanMatchCoroutine());  // 매치되는 블록이 있는지 체크

        // 코루틴 다시 실행해야되는지 체크
        if (reMatchDownCo)
        {
            matchDownCo = StartCoroutine(MatchDownCo());
            reMatchDownCo = false;
        }
        else
            matchDownCo = null;
    }

    // blockArray의 값(Block) 스왑
    void Swap(int x1, int y1, int x2, int y2)
    {
        Block temp = blockArray[x1, y1];
        blockArray[x1, y1] = blockArray[x2, y2];
        blockArray[x2, y2] = temp;
    }

    // 매치 가능한 블록이 있는지 체크하기
    IEnumerator CheckCanMatchCoroutine()
    {
        yield return new WaitUntil(() => !IsMoving()); // 블록이 움직이고 있지 않을때 까지 대기

        foreach (Block block in blockArray)
        {
            if (block == null) continue;

            if (CheckMatchAtBlock(block))   // 지울 수 있는 블록이 있을 경우 그만 검사
            {
                hintBlock = block;
                yield break;
            }
        }

        AllBlockColorSet();
    }

    // 움직이고 있는 블록이 있는지 체크
    bool IsMoving()
    {
        foreach (Block block in blockArray)
        {
            if (block == null) continue;
            if (block._State == State.MOVE) return true;
        }

        return false;
    }

    ParticleSystem GetParticleSystem()
    {
        foreach (ParticleSystem particle in particleList)
        {
            if (!particle.gameObject.activeSelf)
            {
                return particle;
            }
        }

        ParticleSystem obj = Instantiate(particlePrefab, transform);
        obj.gameObject.SetActive(false);
        particleList.Add(obj);

        return obj;
    }
    
    // 범위 안 인지 체크
    bool RangeCheck(int x, int y)
    {
        if (x < 0 || x > width - 1 || y < 0 || y > height - 1) return false;
        else return true;
    }

    IEnumerator EndMoveCoroutine(int count)
    {
        yield return new WaitUntil(() => !reMatchDownCo && matchDownCo == null);

        List<Block> blocks = new List<Block>();

        // 남은 스페셜 블록 삭제
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                if (blockArray[x, y] == null) continue;

                if (blockArray[x, y]._BlockType != BlockType.NOMAL)
                {
                    BlockProcess(blockArray[x, y]);
                    yield return new WaitUntil(() => !reMatchDownCo && matchDownCo == null);
                }
            }
        }

        // 카운트만큼 BOMB 블록으로 해주기
        for (int i = 0; i < count; ++i)
        {
            endGameManager.DecreaseCount();
            while(true)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                if (blockArray[x, y] != null)
                {
                    if (blockArray[x, y].endBomb == false)
                    {
                        blocks.Add(blockArray[x, y]);
                        blockArray[x, y].endBomb = true;
                        blockArray[x, y]._BlockType = BlockType.BOMB;
                        // $ 애니메이션 넣어주면 좋을듯.
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
        }

        // BOMB 블록 제거
        while(blocks.Count != 0)
        {
            Block block = blocks[blocks.Count - 1];
            BlockProcess(block);
            block.endBomb = false;
            block.Match();
            blocks.RemoveAt(blocks.Count - 1);
            yield return new WaitUntil(() => !reMatchDownCo && matchDownCo == null);
        }

        // 남은 스페셜 블록 삭제
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                if (blockArray[x, y] == null) continue;

                if (blockArray[x, y]._BlockType != BlockType.NOMAL)
                {
                    BlockProcess(blockArray[x, y]);
                    yield return new WaitUntil(() => !reMatchDownCo && matchDownCo == null);
                }
            }
        }

        yield return new WaitForSeconds(1f);

        gameManager.WinGameEnd();
    }
    // ----------
    #endregion
}
