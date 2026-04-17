using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class Worm : MonoBehaviour
{
    public Tilemap tilemap;
    public Vector3Int moveDirection = Vector3Int.right;
    public float moveDelay = 0.3f;
    private float timer;
    public List<Vector3> positionHistory = new List<Vector3>();

    void Start()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();
            
        // initialiser la direction si le prefab n'a rien
        if (moveDirection == Vector3Int.zero)
            moveDirection = Vector3Int.right;
    }
    
    void Update()
    {
        if (tilemap == null) return;

        timer += Time.deltaTime;

        if (timer >= moveDelay)
        {
            MoveOnGrid();
            timer = 0f;
        }
    }

    void MoveOnGrid()
    {
        Vector3Int currentCell = tilemap.WorldToCell(transform.position);
        Vector3Int nextCell = currentCell + new Vector3Int((int)moveDirection.x, (int)moveDirection.y, 0);

        TileBase tile = tilemap.GetTile(nextCell);
        Collider2D hit = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(nextCell));
        
        if (tile != null)
        {
            ChooseDirection();
            return;
        }

        if (hit != null)
        {
            if (hit.CompareTag("Enemy"))
            {
                ChooseDirection();

                Enemy other = hit.GetComponent<Enemy>();
                if (other != null)
                {
                    other.ReverseDirection();
                }
                return;
            }
            else
            {
                ChooseDirection();
                return;
            }
        }
        
        positionHistory.Insert(0, transform.position); // sauvegarder position actuelle
        transform.position = tilemap.GetCellCenterWorld(nextCell);

        // (optionnel) limiter la taille de l'historique
        if (positionHistory.Count > 10)
        {
            positionHistory.RemoveAt(positionHistory.Count - 1);
        }
    }

    void ChooseDirection()
    {
        // directions possibles
        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        Vector3Int currentCell = tilemap.WorldToCell(transform.position);

        Vector3Int opposite = -moveDirection;

        List<Vector3Int> validDirections = new List<Vector3Int>();

        // 🔍 chercher directions valides (sans demi-tour)
        foreach (var dir in directions)
        {
            if (dir == opposite) continue; // ❌ éviter demi-tour

            Vector3Int nextCell = currentCell + dir;
            TileBase tile = tilemap.GetTile(nextCell);
            if (tile != null) continue;

            Collider2D hit = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(nextCell));
            if (hit != null) continue;

            validDirections.Add(dir);
        }
        // ✅ si on peut tourner → choisir au hasard
        if (validDirections.Count > 0)
        {
            moveDirection = validDirections[Random.Range(0, validDirections.Count)];
        }
        else
        {
            // 🔁 sinon → demi-tour forcé
            moveDirection = opposite;
        }
    }
}