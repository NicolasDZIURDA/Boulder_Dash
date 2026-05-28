using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class Worm : MonoBehaviour
{
    public Tilemap tilemap;
    public Vector3Int moveDirection = Vector3Int.right;
    public int wormLength = 8;
    public float moveDelay = 0.1f;
    private float timer;
    public List<Vector3> positionHistory = new List<Vector3>();
    private bool justTurnedAround = false;
    public bool isEvil;
    public GameObject rockPrefab;
    public GameObject coinPrefab;

    void Start()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();

        if (moveDirection == Vector3Int.zero)
            moveDirection = Vector3Int.right;

        positionHistory.Clear();
        positionHistory.Add(transform.position);
    }
    
    void Awake()
    {
        positionHistory = new List<Vector3>();
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
        if (justTurnedAround)
        {
            justTurnedAround = false;
            return;
        }

        Vector3Int currentCell = tilemap.WorldToCell(transform.position);
        Vector3Int nextCell = currentCell + moveDirection;

        TileBase tile = tilemap.GetTile(nextCell);
        Collider2D hit = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(nextCell));

        bool blocked = false;

        if (tile != null)
            blocked = true;

        if (hit != null && !hit.CompareTag("Worm"))
        {
            blocked = true;
            if (hit.CompareTag("Enemy"))
            {
                Enemy enemy = hit.GetComponent<Enemy>();

                if (enemy != null && !enemy.dropCoins)
                {
                    Vector3 pos = hit.transform.position;
                    Instantiate(rockPrefab, pos, Quaternion.identity);
                    Destroy(hit.gameObject);
                }
            }
            else if(hit.CompareTag("Rock"))
            {
                Vector3 pos = hit.transform.position;
                Instantiate(coinPrefab, pos, Quaternion.identity);
                Destroy(hit.gameObject);
            }
        }
        
        if (blocked)
        {
            moveDirection = ChooseDirection();
            nextCell = currentCell + moveDirection;
            positionHistory.Insert(0, transform.position);
            transform.position = tilemap.GetCellCenterWorld(nextCell);
        }
        else
        {
            positionHistory.Insert(0, transform.position);
            transform.position = tilemap.GetCellCenterWorld(nextCell);
        }

        CheckHistoryLength();
    }

    Vector3Int ChooseDirection()
    {
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

        foreach (var dir in directions)
        {
            if (dir == opposite) continue; // éviter demi-tour

            Vector3Int nextCell = currentCell + dir;
            TileBase tile = tilemap.GetTile(nextCell);
            if (tile != null) continue;

            Collider2D hit = Physics2D.OverlapPoint(tilemap.GetCellCenterWorld(nextCell));
            if (hit != null) continue;

            validDirections.Add(dir);
        }

        if (validDirections.Count > 0)
        {
            return validDirections[Random.Range(0, validDirections.Count)];
        }
        else
        {
            return opposite;
        }
    }

    void CheckHistoryLength()
    {
        if (positionHistory.Count > wormLength)
        {
            positionHistory.RemoveAt(positionHistory.Count - 1);
        }
    }
}