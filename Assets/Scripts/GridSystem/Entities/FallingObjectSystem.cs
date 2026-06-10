using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingObjectSystem
{
    private readonly GridManager grid;

    public FallingObjectSystem(GridManager grid)
    {
        this.grid = grid;
    }

    public void Simulate(int x, int y)
    {
        if (CanFallDown(x, y))
        {
            AddIntent(x, y, 0, -1, true);
            return;
        }

        if (CanRollLeft(x, y))
        {
            AddIntent(x, y, -1, 0, false);
            return;
        }

        if (CanRollRight(x, y))
        {
            AddIntent(x, y, 1, 0, false);
            return;
        }

        grid.CopyCurrentToNext(x, y);
    }

    public bool IsFalling(int x, int y)
    {
        return CanFallDown(x, y)
            || CanRollLeft(x, y)
            || CanRollRight(x, y);
    }

    bool CanFallDown(int x, int y)
    {
        return IsEmpty(x, y - 1) && !grid.IsReserved(x, y - 1);
    }

    bool CanRollLeft(int x, int y)
    {
        return IsEmpty(x - 1, y)
            && IsEmpty(x - 1, y - 1)
            && IsAboveSupportToRoll(x, y)
            && !grid.IsReserved(x - 1, y);
    }

    bool CanRollRight(int x, int y)
    {
        return IsEmpty(x + 1, y)
            && IsEmpty(x + 1, y - 1)
            && IsAboveSupportToRoll(x, y)
            && !grid.IsReserved(x + 1, y);
    }

    bool IsAboveSupportToRoll(int x, int y)
    {
        return grid.GetCurrentCell(x, y - 1).type == CellType.Rock ||
            grid.GetCurrentCell(x, y - 1).type == CellType.Coin ||
            grid.GetCurrentCell(x, y - 1).type == CellType.Wall;
    }

    bool IsEmpty(int x, int y)
    {
        return grid.IsInside(x, y) && grid.GetCurrentCell(x, y).type == CellType.Empty;
    }

    bool IsPlayerOrEnemy(int x, int y)
    {
        CellType type = grid.GetCurrentCell(x, y).type;

        return type == CellType.Player || type == CellType.Enemy;
    }

    void AddIntent(int x, int y, int dx, int dy, bool falling)
    {
        grid.intents.Add(new MoveIntent
        {
            from = new Vector2Int(x, y),
            to = new Vector2Int(x + dx, y + dy),
            type = grid.GetCurrentCell(x, y).type
        });

        grid.GetCurrentCell(x, y).isFalling = falling;
    }
}
