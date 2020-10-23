#region ------- Default Method -------
#endregion

#region ------- Public Method -------

#endregion
#region ------- Private Method -------
#endregion

public enum State : int
{
    IDLE,
    MOVE,
    SELECT,
    MATCH_WAIT,
    MATCH_END,
    HINT
}

public enum BlockType : int
{
    NOMAL,
    HORIZONTAL,
    VERTICAL,
    BOMB,
    FIVE
}

public enum BlockColor : int
{
    BLUE,
    GREEN,
    ORANGE,
    PURPLE,
    RED,
    YELLOW,
    NONE
}

public enum TileKind
{
    NORMAL,
    BLANK,
    BREAKABLE
}
public class Value
{
    public const float BlockScale = 1f;
    public const float ChangeDuration = .25f;
    public const int STAGECNT = 16;
}
