using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class WormGrid : MonoBehaviour
{
    public Tilemap tilemap;
    public Vector3Int moveDirection = Vector3Int.right;

    public float moveDelay = 0.3f;
    private float timer;

    public List<Vector3Int> positionHistory = new List<Vector3Int>();

    private Vector3Int cellPosition;

    void Start()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();

        if (moveDirection == Vector3Int.zero)
            moveDirection = Vector3Int.right;

        SnapToGrid();

        GridManager.Instance.SetCell(cellPosition, CellType.Enemy);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= moveDelay)
        {
            MoveOnGrid();
            timer = 0f;
        }
    }

    void MoveOnGrid()
    {
        Vector3Int nextCell = cellPosition + moveDirection;
        CellType target = GridManager.Instance.GetCell(nextCell);

        // 🧱 bloqué
        if (IsBlocked(target))
        {
            ChooseDirection();
            return;
        }

        // 👾 interaction avec ennemi
        if (target == CellType.Enemy)
        {
            ChooseDirection();
            return;
        }

        // 💀 joueur
        if (target == CellType.Player)
        {
            Debug.Log("Player dead");
            return;
        }

        // ✅ déplacement
        MoveTo(nextCell);
    }

    bool IsBlocked(CellType type)
    {
        return type == CellType.Wall ||
               type == CellType.Rock ||
               type == CellType.Dirt;
    }

    void MoveTo(Vector3Int newCell)
    {
        // sauvegarde historique
        positionHistory.Insert(0, cellPosition);

        if (positionHistory.Count > 10)
            positionHistory.RemoveAt(positionHistory.Count - 1);

        // update grid
        GridManager.Instance.ClearCell(cellPosition);

        cellPosition = newCell;

        GridManager.Instance.SetCell(cellPosition, CellType.Enemy);

        transform.position = tilemap.GetCellCenterWorld(cellPosition);
    }

    void ChooseDirection()
    {
        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        Vector3Int opposite = -moveDirection;
        List<Vector3Int> validDirections = new List<Vector3Int>();

        foreach (var dir in directions)
        {
            if (dir == opposite) continue;

            Vector3Int next = cellPosition + dir;
            CellType type = GridManager.Instance.GetCell(next);

            if (IsBlocked(type)) continue;
            if (type == CellType.Enemy) continue;

            validDirections.Add(dir);
        }

        if (validDirections.Count > 0)
        {
            moveDirection = validDirections[Random.Range(0, validDirections.Count)];
        }
        else
        {
            moveDirection = opposite;
        }
    }

    void SnapToGrid()
    {
        cellPosition = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(cellPosition);
    }
}