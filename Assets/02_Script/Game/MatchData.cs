using System.Collections.Generic;
using UnityEngine;

public class MatchData : MonoBehaviour
{
    public Block moveBlock;
    public Queue<Block> horBlocks;
    public Queue<Block> verBlocks;

    public MatchData(Block block)
    {
        moveBlock = block;
        horBlocks = new Queue<Block>();
        verBlocks = new Queue<Block>();
    }
}
