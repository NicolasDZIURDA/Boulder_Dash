using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    public Direction direction = Direction.Left;

    private float timer;
    private Tilemap tilemap;
    private GridManager grid;

    public Vector3Int cellPosition;
    public bool dropCoins = false;

    void Start()
    {
        tilemap = FindObjectOfType<Tilemap>();
        grid = GridManager.Instance;

        cellPosition = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(cellPosition);
        transform.rotation = Quaternion.Euler(0f, 0f, DirectionTools.UpdateRotation(Direction.Up));
    }

    public Direction GetNextDirection()
    {
        Direction left    = DirectionTools.TurnLeft(direction);
        Direction forward = direction;
        Direction right   = DirectionTools.TurnRight(direction);
        Direction back    = DirectionTools.Opposite(direction);

        // Priorité : Gauche → Devant → Droite → Demi-tour
        if (CanMove(left)) return left;
        if (CanMove(forward)) return forward;
        if (CanMove(right))   return right;
        if (CanMove(back))    return back;

        return forward; // fallback
    }

    public bool CanMove(Direction dir)
    {
        Vector3Int nextPos = cellPosition + DirectionTools.DirToVector(dir);

        if (!grid.IsInside(nextPos.x, nextPos.y))
            return false;

        Cell target = grid.GetCurrentCell(nextPos.x, nextPos.y);
        if (target == null) return false;

        if (target.type != CellType.Empty)
        {
            return false;
        }

        return true;
    }

    public void MoveTo(Vector3Int newCellPosition, Direction newDirection)
    {
        cellPosition = newCellPosition;
        direction = newDirection;
        SnapToGrid();
        transform.rotation = Quaternion.Euler(0f, 0f, DirectionTools.UpdateRotation(direction));
    }

    public void ForceReverse()
    {
        direction = DirectionTools.Opposite(direction);
        transform.rotation = Quaternion.Euler(0f, 0f, DirectionTools.UpdateRotation(direction));
    }

    void SnapToGrid()
    {
        transform.position = tilemap.GetCellCenterWorld(cellPosition);
    }
}