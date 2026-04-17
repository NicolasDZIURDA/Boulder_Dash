using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class Fall : MonoBehaviour
{
    [Header("Réglages")]
    public Tilemap tilemap;
    public float fallDelay = 1f;
    public int explosionRadius = 1;
    public GameObject coinPrefab;
    
    [Header("Autre")]
    private float timer = 0f;
    private bool isFalling = false;
    private Vector3Int cellPosition;

    void Start()
    {
        if (tilemap == null)
            tilemap = FindObjectOfType<Tilemap>();

        SnapToGrid();
    }

    void SnapToGrid()
    {
        cellPosition = tilemap.WorldToCell(transform.position);
        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPosition);
        transform.position = worldPos;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer < fallDelay) return;

        timer = 0f;

        Vector3Int currentCell = tilemap.WorldToCell(transform.position);
        Vector3Int belowCell = currentCell + Vector3Int.down;
        Vector3 worldPosBelow = tilemap.GetCellCenterWorld(belowCell);

        // 🔴 Si un ennemi est déjà sur la même case → explosion directe
        if (IsTagAtPosition(transform.position, "Enemy"))
        {
            Explode(currentCell);
            return;
        }

        // 🧱 Si tile en dessous → stop
        TileBase tileBelow = tilemap.GetTile(belowCell);
        
        if (tileBelow != null)
        {
            // 🚫 si c'est de la terre → pas de glissement
            if (tileBelow.name == "dirt")
            {
                isFalling = false;
                return;
            }

            // sinon comportement normal
            if (TrySlide(currentCell))
                return;

            isFalling = false;
            return;
        }
                
        // 🔍 Détection objet en dessous
        Collider2D hitBelow = Physics2D.OverlapCircle(worldPosBelow, 0.2f);

        if (hitBelow != null)
        {
            if (hitBelow.CompareTag("Door"))
            {
                isFalling = false;
                return;
            }
            if (isFalling)
            {
                if (hitBelow.CompareTag("Enemy"))
                {
                    transform.position = worldPosBelow;
                    Explode(belowCell);
                    return;
                }
                else if (hitBelow.CompareTag("Player"))
                {
                    Explode(belowCell);
                    return;
                }
            }

            // 👉 tentative de glissement
            if (TrySlide(currentCell))
                return;

            isFalling = false;
            return;
        }

        if (!GridReservation.reservedCells.Contains(belowCell))
        {
            GridReservation.reservedCells.Add(belowCell);
            transform.position = worldPosBelow;
            isFalling = true;
        }
        // ⬇ chute normale
        isFalling = true;
    }

    // 💥 Explosion → passe en coroutine
    void Explode(Vector3Int cell)
    {
        StartCoroutine(ExplodeThenSpawn(cell));
    }

    IEnumerator ExplodeThenSpawn(Vector3Int centerCell)
    {
        List<Vector3Int> affectedCells = new List<Vector3Int>();
        List<Vector3Int> coinSpawnCells = new List<Vector3Int>();

        // 💥 1. CALCUL DES CASES (AVANT toute modif)
        for (int x = -explosionRadius; x <= explosionRadius; x++)
        {
            for (int y = -explosionRadius; y <= explosionRadius; y++)
            {
                affectedCells.Add(centerCell + new Vector3Int(x, y, 0));
            }
        }

        // 💥 2. EXPLOSION
        foreach (Vector3Int targetCell in affectedCells)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(targetCell);
            Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.2f);

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    hit.GetComponent<Player>().Die();
                }

                if (hit.CompareTag("Enemy"))
                {
                    Enemy enemy = hit.GetComponent<Enemy>();

                    if (enemy != null && enemy.dropCoins)
                    {
                        for (int x = -explosionRadius; x <= explosionRadius; x++)
                        {
                            for (int y = -explosionRadius; y <= explosionRadius; y++)
                            {
                                coinSpawnCells.Add(centerCell + new Vector3Int(x, y, 0));
                            }
                        }
                    }
                    Destroy(hit.gameObject);
                    Debug.Log("Ennemi détruit : " + targetCell);
                }
            }
            TileBase tile = tilemap.GetTile(targetCell);

            if (tile != null)
            {
                if (tile.name != "wall1")
                {
                    tilemap.SetTile(targetCell, null);
                }
            }

            GridReservation.Release(targetCell);
        }
        // ⏱ attendre que Unity nettoie tout
        yield return null;

        // 💰 3. SPAWN (SANS conditions bloquantes)
        foreach (Vector3Int cellPos in coinSpawnCells)
        {
            TileBase tile = tilemap.GetTile(cellPos);
            Vector3 pos = tilemap.GetCellCenterWorld(cellPos);

            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.2f);

            bool alreadyCoin = false;

            foreach (var h in hits)
            {
                if (h.CompareTag("Coin"))
                {
                    alreadyCoin = true;
                    break;
                }
            }

            if (tile == null && !alreadyCoin)
            {
                Instantiate(coinPrefab, pos, Quaternion.identity);
            }
        }

        if (gameObject.tag == "Rock" || gameObject.tag == "Coin")
        {
            Destroy(gameObject);
        }

        foreach (KeyValuePair<Vector3Int, Enemy> cell in GridReservation.occupiedCells)
        {
            print(cell);
        }
    }

    // 🔍 Détection propre multi-colliders
    private bool IsTagAtPosition(Vector3 position, string tag)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(position);

        foreach (Collider2D hit in hits)
        {
            if ((1 << hit.gameObject.layer) != 0)
            {
                if (hit.CompareTag(tag))
                    return true;
            }
        }

        return false;
    }

    bool TrySlide(Vector3Int currentCell)
    {
        // dessous (vérifier si un game object empêche de glisser)
        Vector3Int down = currentCell + Vector3Int.down;

        // gauche
        Vector3Int left = currentCell + Vector3Int.left;
        Vector3Int downLeft = left + Vector3Int.down;

        // droite
        Vector3Int right = currentCell + Vector3Int.right;
        Vector3Int downRight = right + Vector3Int.down;

        Collider2D hitDown = Physics2D.OverlapCircle(tilemap.GetCellCenterWorld(down), 0.2f);

        if (hitDown != null && hitDown.CompareTag("Player"))
        {
            return false;
        }

        // Vérif gauche
        if (!tilemap.HasTile(left) && !tilemap.HasTile(downLeft))
        {
            Collider2D hitLeft = Physics2D.OverlapCircle(tilemap.GetCellCenterWorld(left), 0.2f);
            Collider2D hitDownLeft = Physics2D.OverlapCircle(tilemap.GetCellCenterWorld(downLeft), 0.2f);

            if (hitLeft == null && hitDownLeft == null)
            {
                GridReservation.reservedCells.Add(left);
                transform.position = tilemap.GetCellCenterWorld(left);
                isFalling = true;
                return true;
            }
        }

        // Vérif droite
        if (!tilemap.HasTile(right) && !tilemap.HasTile(downRight))
        {
            Collider2D hitRight = Physics2D.OverlapCircle(tilemap.GetCellCenterWorld(right), 0.2f);
            Collider2D hitDownRight = Physics2D.OverlapCircle(tilemap.GetCellCenterWorld(downRight), 0.2f);

            if (hitRight == null && hitDownRight == null)
            {
                GridReservation.reservedCells.Add(right);
                transform.position = tilemap.GetCellCenterWorld(right);
                isFalling = true;
                return true;
            }
        }

        return false;
    }

    // à retirer
    void DisableAndDestroy(GameObject obj)
    {
        // désactive tous les scripts (empêche Update)
        MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
            s.enabled = false;

        // désactive collider (plus d'interactions)
        Collider2D col = obj.GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // optionnel : désactiver rendu immédiat
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = false;

        Destroy(obj);
    }
}