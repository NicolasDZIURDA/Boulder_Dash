using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    public Vector3Int cellPosition;
    public Direction direction;
    public bool dropCoins;
    public float moveInterval = 0.1f;
    private float timer;
    public float turnCooldown = 0.1f;
    private float lastTurnTime;
    private Tilemap tilemap;

    void Start()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();

        cellPosition = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(cellPosition);
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

        // priorité Boulder Dash : gauche → devant → droite → arrière

        if (CanMove(left))
        {
            direction = left;
            Move(direction);
        }
        else if (CanMove(direction))
        {
            Move(direction);
        }
        else if (CanMove(right))
        {
            direction = right;
            Move(direction);
        }
        else if (CanMove(back))
        {
            direction = back;
            Move(direction);
        }
    }

    bool CanMove(Direction dir)
    {
        Vector3Int next = cellPosition + DirToVector(dir);
        Enemy other = GridReservation.GetEnemyAt(next);

        if (other != null && other != this)
        {
            ReverseDirection();
            other.ReverseDirection();
            return false;
        }

        if (tilemap.HasTile(next))
            return false;

        Collider2D hit = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(next));

        if (hit != null)
        {
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<Player>().Die();
            }

            return false;
        }

        return true;
    }

    void Move(Direction dir)
    {
        Vector3Int next = cellPosition + DirToVector(dir);

        if (GridReservation.IsOccupied(next))
            return;
        
        GridReservation.Release(cellPosition);
        GridReservation.Reserve(next, this);

        cellPosition = next;
        SnapToGrid();

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
        // centre de la cellule
        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPosition);
        transform.position = worldPos;
    }

    void UpdateRotation()
    {
        float angle = 0f;

        switch (direction)
        {
            case Direction.Up:
                angle = 0f;
                break;
            case Direction.Right:
                angle = -90f;
                break;
            case Direction.Down:
                angle = 180f;
                break;
            case Direction.Left:
                angle = 90f;
                break;
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
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}