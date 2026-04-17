using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class PlayerGrid : MonoBehaviour
{
    public Tilemap tilemap;
    public float moveCooldown = 0.1f;
    private float lastMoveTime;
    private int coinCount = 0;
    private Vector3Int cellPosition;

    void Start()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();

        SnapToGrid();

        GridManager.Instance.SetCell(cellPosition, CellType.Player);
    }

    void Update()
    {
        if (Time.time - lastMoveTime < moveCooldown) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(Vector3Int.right);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(Vector3Int.left);
        if (Input.GetKeyDown(KeyCode.UpArrow)) TryMove(Vector3Int.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(Vector3Int.down);
    }

    void TryMove(Vector3Int dir)
    {
        Vector3Int target = cellPosition + dir;
        CellType targetType = GridManager.Instance.GetCell(target);

        // 🧱 mur
        if (targetType == CellType.Wall)
            return;

        // 🟫 dirt → on creuse
        if (targetType == CellType.Dirt)
        {
            GridManager.Instance.Dig(target);
            MoveTo(target);
            return;
        }

        Collider2D hit = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(target));

        if (targetType == CellType.Coin)
        {
            coinCount++;
            Debug.Log("Coins : " + coinCount);

            GridManager.Instance.SetCell(target, CellType.Empty);

            if (hit != null)
                Destroy(hit.gameObject);
                
            MoveTo(target);
            return;
        }

        // 🚪 porte
        if (targetType == CellType.Door)
        {
            Debug.Log("You Win !");
            MoveTo(target);
            return;
        }

        // 🪨 pousser rocher
        if (targetType == CellType.Rock)
        {
            // uniquement gauche/droite
            if (dir == Vector3Int.up || dir == Vector3Int.down)
                return;

            Vector3Int pushCell = target + dir;

            if (GridManager.Instance.IsEmpty(pushCell))
            {
                // déplacer rocher
                MoveRock(target, pushCell);

                // déplacer joueur
                MoveTo(target);
            }
            return;
        }

        // 💀 ennemi ou worm
        if (targetType == CellType.Enemy || targetType == CellType.Worm)
        {
            Die();
            return;
        }

        // ✅ case vide
        if (targetType == CellType.Empty)
        {
            MoveTo(target);
        }
    }

    void MoveTo(Vector3Int newCell)
    {
        GridManager.Instance.ClearCell(cellPosition);

        cellPosition = newCell;
        transform.position = tilemap.GetCellCenterWorld(cellPosition);

        GridManager.Instance.SetCell(cellPosition, CellType.Player);

        lastMoveTime = Time.time;
    }

    void MoveRock(Vector3Int from, Vector3Int to)
    {
        // trouver le GameObject rock
        foreach (var obj in FindObjectsOfType<FallGrid>())
        {
            if (obj.transform.position == tilemap.GetCellCenterWorld(from))
            {
                obj.transform.position = tilemap.GetCellCenterWorld(to);
                break;
            }
        }

        GridManager.Instance.ClearCell(from);
        GridManager.Instance.SetCell(to, CellType.Rock);
    }

    void SnapToGrid()
    {
        cellPosition = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(cellPosition);
    }

    public void Die()
    {
        gameObject.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}