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
            grid.TryMove(x, y, 0, -1, true);
            return;
        }

        if (CanRollLeft(x, y))
        {
            grid.TryMove(x, y, -1, 0, false);
            return;
        }

        if (CanRollRight(x, y))
        {
            grid.TryMove(x, y, 1, 0, false);
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
        return grid.GetCell(x, y - 1).type == CellType.Rock ||
            grid.GetCell(x, y - 1).type == CellType.Coin ||
            grid.GetCell(x, y - 1).type == CellType.Wall;
    }

    bool IsEmpty(int x, int y)
    {
        return grid.IsInside(x, y) && grid.GetCell(x, y).type == CellType.Empty;
    }

    bool IsPlayerOrEnemy(int x, int y)
    {
        CellType type = grid.GetCell(x, y).type;

        return type == CellType.Player || type == CellType.Enemy;
    }
}
