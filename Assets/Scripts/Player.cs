using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase dirtTile;
    public Transform player;
    public float moveCooldown = 0.1f;
    private float lastMoveTime;
    private int coinCount = 0;

    void Update()
    {
        // Déplacement
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(Vector3Int.right);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(Vector3Int.left);
        if (Input.GetKeyDown(KeyCode.UpArrow)) TryMove(Vector3Int.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryMove(Vector3Int.down);

        // Vérifie la mort autour du joueur
        CheckDeathAround();
    }

    void TryMove(Vector3Int dir)
    {
        Vector3Int playerCell = tilemap.WorldToCell(transform.position);
        Vector3Int targetCell = playerCell + dir;

        if (tilemap.HasTile(targetCell) && tilemap.GetTile(targetCell) != dirtTile)
            return;

        Collider2D hit = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(targetCell));

        if (hit != null)
        {
            WormSpawner spawner = hit.GetComponent<WormSpawner>();
            if (spawner != null)
            {
                spawner.SpawnWorm();
                return;
            }
            if (hit.CompareTag("Door"))
            {
                Debug.Log("You Win !");
                transform.position = tilemap.GetCellCenterWorld(targetCell);
            }

            if (hit.CompareTag("Coin"))
            {
                coinCount++;
                Destroy(hit.gameObject);

                Debug.Log("Coins : " + coinCount);

                tilemap.SetTile(targetCell, null);
                transform.position = tilemap.GetCellCenterWorld(targetCell);

                return;
            }

            if (hit.CompareTag("Rock"))
            {
                if (dir == Vector3Int.up || dir == Vector3Int.down)
                    return;

                Vector3Int nextCell = targetCell + dir;

                if (tilemap.HasTile(nextCell))
                    return;

                Collider2D hitNext = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(nextCell));

                if (hitNext == null)
                {
                    // pousser le rocher
                    hit.transform.position = tilemap.GetCellCenterWorld(nextCell);

                    // avancer le joueur
                    transform.position = tilemap.GetCellCenterWorld(targetCell);
                }
                return;
            }

        }
        // case vide → on avance
        tilemap.SetTile(targetCell, null);
        transform.position = tilemap.GetCellCenterWorld(targetCell);
    }

    void CheckDeathAround()
    {
        Vector3Int playerCell = tilemap.WorldToCell(transform.position);

        // Les 5 positions à vérifier : la case du joueur + 4 adjacentes
        Vector3Int[] checkCells = new Vector3Int[]
        {
            playerCell,
            playerCell + Vector3Int.up,
            playerCell + Vector3Int.down,
            playerCell + Vector3Int.left,
            playerCell + Vector3Int.right
        };

        foreach (var cell in checkCells)
        {
            Collider2D hit = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(cell));
            if (hit != null && hit.CompareTag("Enemy"))
            {
                Die();
                return;
            }
        }
    }

    public void Die()
    {
        // désactiver le joueur immédiatement
        gameObject.SetActive(false);

        // recharger la scène
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}