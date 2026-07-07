using UnityEngine;

public class Cell
{
    public CellType type = CellType.Empty;
    public bool isSolid = false;
    public bool isFalling = false;
    public GameObject visual = null;
    public WallType wallType;

    public bool isReserved = false;     // utilisé dans des cas spécifiques
    public bool justSpawned = false;    // pour éviter instantiation et simulation au même tick

    public void CopyFrom(Cell other)
    {
        type = other.type;
        isSolid = other.isSolid;
        isFalling = other.isFalling;
        visual = other.visual;
        wallType = other.wallType;
    }

    public void Reset()
    {
        type = CellType.Empty;
        isSolid = false;
        isFalling = false;
        visual = null;
        wallType = WallType.None;
    }
}