using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (fileName = "Level", menuName = "Level")]
public class Level : ScriptableObject
{
    public int width;
    public int height;

    public int goalScore;

    public EndGameRequirement endGameRequirement;

    public GoalBlock[] goalBlocks;
    public TileType[] tileTypes;
}
