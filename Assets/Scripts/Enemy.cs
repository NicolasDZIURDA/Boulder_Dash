using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyGrid : MonoBehaviour
{
    public Vector3Int cellPosition;
    public Direction direction;
    public bool dropCoins = true;

    public float moveInterval = 0.2f;
    private float timer;

    public float turnCooldown = 0.1f;
    private float lastTurnTime;

    private Tilemap tilemap;

    void Start()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();

        SnapToGrid();

        GridManager.Instance.SetCell(cellPosition, CellType.Enemy);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= moveInterval)
        {
            MoveLogic();
            timer = 0f;
        }
    }

    void MoveLogic()
    {
        Direction left = TurnLeft(direction);
        Direction right = TurnRight(direction);
        Direction back = Opposite(direction);

        // priorité : gauche → devant → droite → arrière
        if (TryMove(left))
        {
            direction = left;
        }
        else if (TryMove(direction))
        {
            // continue
        }
        else if (TryMove(right))
        {
            direction = right;
        }
        else if (TryMove(back))
        {
            direction = back;
        }
    }

    bool TryMove(Direction dir)
    {
        Vector3Int next = cellPosition + DirToVector(dir);
        CellType target = GridManager.Instance.GetCell(next);

        // 🧱 bloqué par solide
        if (IsBlocked(target))
            return false;

        // 👾 collision avec autre ennemi
        if (target == CellType.Enemy)
        {
            ReverseDirection();
            return false;
        }

        // 💀 touche joueur
        if (target == CellType.Player)
        {
            Debug.Log("Player dead");
            // appelle ton Player.Die() ici si tu as une ref
            return false;
        }

        // ✅ déplacement autorisé
        MoveTo(next);
        return true;
    }

    void MoveTo(Vector3Int newCell)
    {
        // libère ancienne case
        GridManager.Instance.ClearCell(cellPosition);

        cellPosition = newCell;

        // place dans la grille
        GridManager.Instance.SetCell(cellPosition, CellType.Enemy);

        // update position monde
        transform.position = tilemap.GetCellCenterWorld(cellPosition);

        UpdateRotation();
    }

    public void ReverseDirection()
    {
        if (Time.time - lastTurnTime < turnCooldown) return;

        direction = Opposite(direction);
        lastTurnTime = Time.time;
    }

    void SnapToGrid()
    {
        cellPosition = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(cellPosition);
    }

    void UpdateRotation()
    {
        float angle = 0f;

        switch (direction)
        {
            case Direction.Up: angle = 0f; break;
            case Direction.Right: angle = -90f; break;
            case Direction.Down: angle = 180f; break;
            case Direction.Left: angle = 90f; break;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    Vector3Int DirToVector(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Vector3Int.up;
            case Direction.Down: return Vector3Int.down;
            case Direction.Left: return Vector3Int.left;
            case Direction.Right: return Vector3Int.right;
        }
        return Vector3Int.zero;
    }

    Direction TurnLeft(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Direction.Left;
            case Direction.Left: return Direction.Down;
            case Direction.Down: return Direction.Right;
            case Direction.Right: return Direction.Up;
        }
        return dir;
    }

    Direction TurnRight(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Direction.Right;
            case Direction.Right: return Direction.Down;
            case Direction.Down: return Direction.Left;
            case Direction.Left: return Direction.Up;
        }
        return dir;
    }

    Direction Opposite(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
            case Direction.Left: return Direction.Right;
            case Direction.Right: return Direction.Left;
        }
        return dir;
    }

    bool IsBlocked(CellType t)
    {
        return t == CellType.Wall ||
            t == CellType.Rock ||
            t == CellType.Dirt ||
            t == CellType.Enemy;
    }

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }
}