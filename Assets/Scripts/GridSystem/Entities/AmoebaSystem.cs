using UnityEngine;
using UnityEngine.Tilemaps;

public class AmoebaSystem : MonoBehaviour
{
    public GridManager grid;

    public AmoebaSystem(GridManager grid)
    {
        this.grid = grid;
    }

    public void PerformAmoeba(int x, int y)
    {
        if (!grid.IsInside(x, y)) return;

        TryGrowAmoeba(x, y + 1);
        TryGrowAmoeba(x + 1, y);
        TryGrowAmoeba(x, y - 1);
        TryGrowAmoeba(x - 1, y);
    }

    void TryGrowAmoeba(int x, int y)
    {
        if (!grid.IsInside(x, y)) return;

        if (grid.GetCurrentCell(x, y).type != CellType.Empty && grid.GetCurrentCell(x, y).type != CellType.Dirt) return;

        if (Random.value > 0.99)
        {
            grid.intents.Add(new MoveIntent
            {
                from = new Vector3Int(x, y),
                to = new Vector3Int(x, y),
                type = CellType.Amoeba
            });
        }
    }
}