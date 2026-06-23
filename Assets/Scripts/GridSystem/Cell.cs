using UnityEngine;

public class Cell
{
    public CellType type = CellType.Empty;
    public bool isSolid = false;
    public bool isFalling = false;
    public GameObject visual = null;

    public bool isReserved = false;     // utilisé dans des cas spécifiques

    public WallType wallType;

    public void CopyFrom(Cell other)
    {
        type = other.type;
        isSolid = other.isSolid;
        isFalling = other.isFalling;
        visual = other.visual;
    }

    public void Reset()
    {
        type = CellType.Empty;
        isSolid = false;
        isFalling = false;
        visual = null;
    }
}