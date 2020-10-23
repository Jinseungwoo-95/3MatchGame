using UnityEngine;

public class BackgroundBlock : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private TileKind tileKind;
    private int hp;
    [SerializeField] Sprite BGblock;
    [SerializeField] Sprite[] breakeBGBlocks;
    private GoalManager goalManager;
    void Awake()
    {
        tileKind = TileKind.NORMAL;
        spriteRenderer = GetComponent<SpriteRenderer>();
        goalManager = FindObjectOfType<GoalManager>();
    }

    public void DecreaseHP()
    {
        --hp;
        if(hp == 1)
        {
            spriteRenderer.sprite = breakeBGBlocks[1];
        }
        // hp 0일 때
        else
        {
            tileKind = TileKind.NORMAL;
            spriteRenderer.sprite = BGblock;
            Color color = spriteRenderer.color;
            color.a = 0.2f;
            spriteRenderer.color = color;

            goalManager.CompareGoal(BlockColor.NONE);
        }
    }

    public TileKind _TileKind
    {
        set
        {
            tileKind = value;
            if(tileKind == TileKind.BREAKABLE)
            {
                hp = 2;
                spriteRenderer.sprite = breakeBGBlocks[0];
                Color color = spriteRenderer.color;
                color.a = 1;
                spriteRenderer.color = color;
            }
            else if(tileKind == TileKind.BLANK)
            {
                spriteRenderer.sprite = null;
            }
        }
        get { return tileKind; }
    }

    public int _HP
    {
        set { hp = value; }
    }
}
