using UnityEngine;
using UnityEngine.Tilemaps;

public class FallGrid : MonoBehaviour
{
    public Tilemap tilemap;
    public float fallDelay = 0.2f;
    private float timer;
    private Vector3Int cell;
    private bool isFalling = false;

    void Start()
    {
        SnapToGrid();
        GridManager.Instance.SetCell(cell, GetCellType());
    }

    void SnapToGrid()
    {
        cell = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(cell);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < fallDelay) return;
        timer = 0f;

        TryFall();
    }

    void TryFall()
    {
        Vector3Int below = cell + Vector3Int.down;
        CellType belowType = GridManager.Instance.GetCell(below);

        // 🔽 chute
        if (belowType == CellType.Empty)
        {
            MoveTo(below);
            isFalling = true;
            return;
        }

        if (TrySlide())
        {
            isFalling = true;
            return;
        }

        isFalling = false;
    }

    bool TrySlide()
    {
        Vector3Int down = cell + Vector3Int.down;
        Collider2D hitDown = Physics2D.OverlapCircle(tilemap.GetCellCenterWorld(down), 0.1f);

        Vector3Int left = cell + Vector3Int.left;
        Vector3Int downLeft = left + Vector3Int.down;

        Vector3Int right = cell + Vector3Int.right;
        Vector3Int downRight = right + Vector3Int.down;

        if (hitDown != null && hitDown.CompareTag("Player"))
            return false;

        // gauche
        if (GridManager.Instance.IsEmpty(left) && GridManager.Instance.IsEmpty(downLeft))
        {
            MoveTo(left);
            return true;
        }

        // droite
        if (GridManager.Instance.IsEmpty(right) && GridManager.Instance.IsEmpty(downRight))
        {
            MoveTo(right);
            return true;
        }

        return false;
    }

    void MoveTo(Vector3Int target)
    {
        CellType targetType = GridManager.Instance.GetCell(target);

        if (targetType == CellType.Player || targetType == CellType.Enemy)
        {
            Explode(target);
            return;
        }
        
        GridManager.Instance.ClearCell(cell);

        cell = target;
        transform.position = tilemap.GetCellCenterWorld(cell);
        GridManager.Instance.SetCell(cell, GetCellType());
    }

    void Explode(Vector3Int center)
    {
        Debug.Log("Explode function");
        int radius = 1;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int pos = center + new Vector3Int(x, y, 0);
                CellType type = GridManager.Instance.GetCell(pos);

                if (type == CellType.Player)
                {
                    PlayerGrid player = FindObjectOfType<PlayerGrid>();

                    if (player != null)
                        player.Die();
                }

                if (type == CellType.Enemy)
                {
                    Debug.Log("Enemy dead → spawn coins");
                    // ici tu pourras spawn des coins
                }

                GridManager.Instance.ClearCell(pos);
            }
        }

        Destroy(gameObject);
    }

    CellType GetCellType()
    {
        if (CompareTag("Rock")) return CellType.Rock;
        if (CompareTag("Coin")) return CellType.Coin;
        return CellType.Empty;
    }
}