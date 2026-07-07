using UnityEngine;
using UnityEngine.Tilemaps;

public class WallSystem : MonoBehaviour
{
    private GridManager grid;

    public WallSystem(GridManager grid)
    {
        this.grid = grid;
    }

    public void PerformSlimePattern(int x, int y)
    {
        if (!grid.IsInside(x, y)) return;

        if (grid.GetCurrentCell(x, y - 1).type != CellType.Empty) return;
        if (grid.GetCurrentCell(x, y + 1).type != CellType.Rock && grid.GetCurrentCell(x, y + 1).type != CellType.Coin) return;

        if (Random.value > 0.99)
            AddIntent(x, y + 1, x, y - 1, grid.GetCurrentCell(x, y + 1).type, true);

    }

    public void PerformGrowingWall(int x, int y)
    {
        if (!grid.IsInside(x, y)) return;

        TryGrowInto(x - 1, y);
        TryGrowInto(x + 1, y);
    }

    void TryGrowInto(int x, int y)
    {
        if (!grid.IsInside(x, y)) return;

        if (grid.GetCurrentCell(x, y).type != CellType.Empty) return;

        AddIntent(x, y, x, y, CellType.Wall, false);
    }

    public void PerformMagicWall(int x, int y)  // todo : empêcher l'intent de déplacement du nouvel objet créé
    {
        if (!grid.IsInside(x, y)) return;

        if (grid.GetCurrentCell(x, y + 1).type != CellType.Rock && grid.GetCurrentCell(x, y + 1).type != CellType.Coin) return;

        if (!grid.IsMagicWallActivated())
        {
            if (grid.GetMagicWallTime() < 10)
                grid.ActivateMagicWall();
        }

        if (grid.IsMagicWallActivated())
        {
            if (grid.GetCurrentCell(x, y + 1).type == CellType.Rock)
                TransformFallingObject(x, y, CellType.Coin);

            if (grid.GetCurrentCell(x, y + 1).type == CellType.Coin)
                TransformFallingObject(x, y, CellType.Rock);
        }
    }

    void TransformFallingObject(int x, int y, CellType newType)
    {
        Destroy(grid.GetCurrentCell(x, y + 1).visual);
        grid.GetNextCell(x, y + 1).Reset();

        if (grid.GetCurrentCell(x, y - 1).type == CellType.Empty)
        {
            grid.TransformIntoObject(newType, x, y - 1);
        }
    }

    void AddIntent(int x, int y, int nx, int ny, CellType type, bool falling)
    {
        grid.intents.Add(new MoveIntent
        {
            from = new Vector3Int(x, y),
            to = new Vector3Int(nx, ny),
            type = type
        });

        grid.GetCurrentCell(x, y).isFalling = falling;
    }
}