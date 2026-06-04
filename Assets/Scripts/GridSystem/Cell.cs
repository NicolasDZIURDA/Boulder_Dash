using UnityEngine;

public class Cell
{
    public CellType type = CellType.Empty;
    public bool isSolid = false;
    public bool isDestructible = false;
    public bool isFalling = false;
    public GameObject visual = null;
    public bool isReserved = false;

    public void CopyFrom(Cell other)
    {
        type = other.type;
        isSolid = other.isSolid;
        isDestructible = other.isDestructible;
        isFalling = other.isFalling;
        visual = other.visual;
    }

    public void Reset()
    {
        type = CellType.Empty;
        isSolid = false;
        isDestructible = false;
        isFalling = false;
        visual = null;
    }
}