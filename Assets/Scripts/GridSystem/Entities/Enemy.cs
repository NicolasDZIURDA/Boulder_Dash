using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    public Direction direction = Direction.Left;
    public float moveInterval = 0.1f;

    private float timer;
    private Tilemap tilemap;
    private GridManager gridManager;

    public Vector3Int cellPosition;
    public bool dropCoins = false;

    void Start()
    {
        tilemap = FindObjectOfType<Tilemap>();
        gridManager = GridManager.Instance;

        cellPosition = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(cellPosition);

        UpdateRotation();
    }

    public Direction GetNextDirection()
    {
        Direction left    = TurnLeft(direction);
        Direction forward = direction;
        Direction right   = TurnRight(direction);
        Direction back    = Opposite(direction);

        // Priorité : Gauche → Devant → Droite → Demi-tour
        if (CanMove(left))    return left;
        if (CanMove(forward)) return forward;
        if (CanMove(right))   return right;
        if (CanMove(back))    return back;

        return forward; // fallback
    }

    public bool CanMove(Direction dir)
    {
        Vector3Int nextPos = cellPosition + DirToVector(dir);

        if (!gridManager.IsInside(nextPos.x, nextPos.y))
            return false;

        Cell target = gridManager.GetCell(nextPos.x, nextPos.y);   // ← À ajouter dans GridManager
        if (target == null) return false;

        // L'ennemi est bloqué par :
        if (target.type == CellType.Rock || 
            target.type == CellType.Wall || 
            target.type == CellType.Coin ||      // ← Bloqué par les pièces
            target.type == CellType.Enemy)       // ← Bloqué par les autres ennemis
        {
            return false;
        }

        // Il ne creuse pas la terre
        if (target.type == CellType.Dirt)
            return false;

        return true;
    }

    public void MoveTo(Vector3Int newCellPosition, Direction newDirection)
    {
        cellPosition = newCellPosition;
        direction = newDirection;
        SnapToGrid();
        UpdateRotation();
    }

    public void ForceReverse()
    {
        direction = Opposite(direction);
        UpdateRotation();
    }

    void SnapToGrid()
    {
        transform.position = tilemap.GetCellCenterWorld(cellPosition);
    }

    void UpdateRotation()
    {
        float angle = direction switch
        {
            Direction.Up    => 0f,
            Direction.Right => -90f,
            Direction.Down  => 180f,
            Direction.Left  => 90f,
            _ => 0f
        };

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public Vector3Int DirToVector(Direction dir)
    {
        return dir switch
        {
            Direction.Up    => Vector3Int.up,
            Direction.Down  => Vector3Int.down,
            Direction.Left  => Vector3Int.left,
            Direction.Right => Vector3Int.right,
            _ => Vector3Int.zero
        };
    }

    Direction TurnLeft(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Left,
            Direction.Left => Direction.Down,
            Direction.Down => Direction.Right,
            Direction.Right => Direction.Up,
            _ => dir
        };
    }

    Direction TurnRight(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Right,
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            Direction.Left => Direction.Up,
            _ => dir
        };
    }

    Direction Opposite(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => dir
        };
    }
}

// ====================== ENUMERATION ======================
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}