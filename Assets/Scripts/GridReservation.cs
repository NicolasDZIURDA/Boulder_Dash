using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public enum CellType
{
    Empty,
    Wall,
    Dirt,
    Rock,
    Coin,
    Player,
    Enemy,
    Worm,
    Door
}

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public Tilemap tilemap;
    public TileBase wallTile;
    public TileBase dirtTile;
    private Dictionary<Vector3Int, CellType> grid = new();

    void Awake()
    {
        Instance = this;
        BakeTilemap();
    }

    void BakeTilemap()
    {
        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(pos);

            if (tile == null)
            {
                SetCell(pos, CellType.Empty);
                continue;
            }

            if (tile == wallTile)
                SetCell(pos, CellType.Wall);

            else if (tile == dirtTile)
                SetCell(pos, CellType.Dirt);

            else
                SetCell(pos, CellType.Empty);
        }
    }

    public CellType GetCell(Vector3Int pos)
    {
        return grid.ContainsKey(pos) ? grid[pos] : CellType.Empty;
    }

    public void SetCell(Vector3Int pos, CellType type)
    {
        grid[pos] = type;
    }

    public void ClearCell(Vector3Int pos)
    {
        if (grid.ContainsKey(pos))
            grid[pos] = CellType.Empty;
    }

    public bool IsEmpty(Vector3Int pos)
    {
        return GetCell(pos) == CellType.Empty;
    }

    public bool IsSolid(Vector3Int pos)
    {
        CellType t = GetCell(pos);
        return t == CellType.Wall || t == CellType.Rock || t == CellType.Dirt;
    }

    public void Dig(Vector3Int pos)
    {
        SetCell(pos, CellType.Empty);
        tilemap.SetTile(pos, null);
    }
}