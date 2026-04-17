using UnityEngine;
using UnityEngine.Tilemaps;

public class WormSegmentGrid : MonoBehaviour
{
    public WormGrid head;
    public int index; // 1 = premier segment, 2 = deuxième...

    public Tilemap tilemap;

    private Vector3Int cellPosition;

    void Start()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();

        SnapToGrid();
    }

    void Update()
    {
        if (head == null) return;

        if (head.positionHistory.Count >= index)
        {
            Vector3Int targetCell = head.positionHistory[index - 1];

            MoveTo(targetCell);
        }
    }

    void MoveTo(Vector3Int newCell)
    {
        // libère ancienne case
        GridManager.Instance.ClearCell(cellPosition);

        cellPosition = newCell;

        // réserve nouvelle case
        GridManager.Instance.SetCell(cellPosition, CellType.Worm);

        transform.position = tilemap.GetCellCenterWorld(cellPosition);
    }

    void SnapToGrid()
    {
        cellPosition = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(cellPosition);

        GridManager.Instance.SetCell(cellPosition, CellType.Worm);
    }
}