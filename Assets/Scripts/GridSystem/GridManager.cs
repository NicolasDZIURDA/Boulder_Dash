using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 32;
    public int height = 32;
    public float tickRate = 1f;

    public static GridManager Instance;
    private FallingObjectSystem fallingObjectSystem;

    [Header("Tiles")]
    public Tilemap tilemap;
    public TileBase dirtTile;
    public TileBase brickWallTile;
    public TileBase steelWallTile;

    [Header("References")]
    public PlayerController inputController;

    private Cell[,] currentGrid;
    private Cell[,] nextGrid;
    public GameObject coinPrefab;
    private int coins;
    private float timer;
    public bool gameOverPending = false;
    public int gameOverTimer = 0;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        inputController = FindObjectOfType<PlayerController>();
        InitGrid();
        InjectSceneObjects();
        fallingObjectSystem = new FallingObjectSystem(this);
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickRate)
        {
            timer = 0f;
            Tick();
        }
    }

    void InitGrid()
    {
        currentGrid = new Cell[width, height];
        nextGrid = new Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                currentGrid[x, y] = new Cell();
                nextGrid[x, y] = new Cell();

                Vector3Int pos = new Vector3Int(x, y, 0);
                TileBase tile = tilemap?.GetTile(pos);

                if (tile == dirtTile)
                    SetCell(currentGrid[x, y], CellType.Dirt, true, true);
                else if (tile == brickWallTile || tile == steelWallTile)
                    SetCell(currentGrid[x, y], CellType.Wall, true, false);
                else
                    SetCell(currentGrid[x, y], CellType.Empty, false, false);
            }
        }
    }

    void InjectSceneObjects()
    {
        InjectObjectsWithTag("Rock", CellType.Rock, true, false);
        InjectObjectsWithTag("Coin", CellType.Coin, true, false);
        InjectObjectsWithTag("Enemy", CellType.Enemy, true, false);
        InjectPlayer();
    }

    void InjectObjectsWithTag(string tag, CellType type, bool isSolid, bool isDestructible)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            Vector3Int cellPos = tilemap.WorldToCell(obj.transform.position);
            int x = cellPos.x;
            int y = cellPos.y;

            if (IsInside(x, y))
            {
                SetCell(currentGrid[x, y], type, isSolid, isDestructible);
                SetCell(nextGrid[x, y], type, isSolid, isDestructible);

                currentGrid[x, y].visual = obj;
                nextGrid[x, y].visual = obj;
            }
        }
    }

    void SetCell(Cell cell, CellType type, bool isSolid, bool isDestructible)
    {
        cell.type = type;
        cell.isSolid = isSolid;
        cell.isDestructible = isDestructible;
    }

    void InjectPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3Int pos = tilemap.WorldToCell(player.transform.position);
        int x = pos.x, y = pos.y;

        if (IsInside(x, y))
        {
            currentGrid[x, y].type = CellType.Player;
            currentGrid[x, y].visual = player;
            nextGrid[x, y].type = CellType.Player;
            nextGrid[x, y].visual = player;
        }
    }

    void Tick()
    {
        if (gameOverPending)
        {
            gameOverTimer--;

            if (gameOverTimer <= 0)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            return;
        }

        ClearNextGrid();
        SimulateWorld();
        SwapGrids();
        RenderGrid();
    }

    void ClearNextGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                nextGrid[x, y].Reset();
                nextGrid[x, y].isReserved = false;
            }
        }
    }

    void SimulateWorld()
    {
        // On parcourt du bas vers le haut pour une bonne simulation de gravité
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                Cell cell = currentGrid[x, y];

                switch (cell.type)
                {
                    case CellType.Rock:
                    case CellType.Coin:
                        fallingObjectSystem.Simulate(x, y);
                        break;
                    case CellType.Player:
                        SimulatePlayer(x, y);
                        break;
                    case CellType.Enemy:
                        SimulateEnemy(x, y);   // À compléter selon ton IA
                        break;
                    case CellType.Dirt:
                    case CellType.Wall:
                        nextGrid[x, y].CopyFrom(cell);
                        break;
                }
            }
        }
        CheckEnemyCollision();
        CheckAllCrushes();
    }

    public bool TryMove(int x, int y, int dx, int dy, bool isFalling)
    {
        int nx = x + dx;
        int ny = y + dy;

        if (!IsInside(nx, ny))
            return false;

        if (currentGrid[nx, ny].isSolid)
            return false;

        if (currentGrid[nx, ny].isReserved)
            return false;

        nextGrid[nx, ny].CopyFrom(currentGrid[x, y]);

        nextGrid[nx, ny].isReserved = true;
        nextGrid[nx, ny].isFalling = isFalling && dy == -1;

        nextGrid[x, y].Reset();

        return true;
    }

    private void CheckAllCrushes()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = currentGrid[x, y];

                if ((currentGrid[x, y].type == CellType.Rock || currentGrid[x, y].type == CellType.Coin) && currentGrid[x, y].isFalling)
                {
                    if (GetCell(x, y - 1).type == CellType.Player)
                    {
                        TriggerExplosion(x, y - 1, false);
                        OnPlayerKilled();
                    }
                    else if (GetCell(x, y - 1).type == CellType.Enemy)
                    {
                        Enemy enemy = currentGrid[x, y - 1].visual.GetComponent<Enemy>();
                        bool dropCoins = enemy.dropCoins;
                        TriggerExplosion(x, y - 1, dropCoins);
                    }
                }
            }
        }
    }

    public void CopyCurrentToNext(int x, int y)
    {
        nextGrid[x, y].CopyFrom(currentGrid[x, y]);
        nextGrid[x, y].isFalling = false;
    }

    // ==================== AUTRES SIMULATIONS ====================
    void SimulatePlayer(int x, int y)
    {
        Vector2Int move = inputController.MoveInput;

        if (move == Vector2Int.zero)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        int nx = x + move.x;
        int ny = y + move.y;

        inputController.ConsumeInput();

        if (!IsInside(nx, ny))
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        Cell target = currentGrid[nx, ny];

        if (target.type == CellType.Wall)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        if (target.type == CellType.Rock)
        {
            if (move == Vector2Int.left || move == Vector2Int.right)
            {
                if (fallingObjectSystem.IsFalling(nx, ny))
                {
                    nextGrid[x, y].CopyFrom(currentGrid[x, y]);
                    return;
                }

                if (!TryMove(nx, ny, move.x, move.y, false))
                {
                    nextGrid[x, y].CopyFrom(currentGrid[x, y]);
                    return;
                }

                // 3. Nettoyage dans les deux grilles
                currentGrid[nx, ny].Reset();
                nextGrid[nx, ny].Reset();
            }
            else
            {
                nextGrid[x, y].CopyFrom(currentGrid[x, y]);
                return;
            }
        }

        if (target.type == CellType.Coin)
            CollectCoin(nx, ny);

        if (target.type == CellType.Dirt)
            Dig(nx, ny);

        // Vérification de réservation
        if (nextGrid[nx, ny].isReserved)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        nextGrid[nx, ny].CopyFrom(currentGrid[x, y]);
        nextGrid[nx, ny].isReserved = true;
    }

    void Dig(int x, int y)
    {
        // 1. Destruction de l'objet visuel (si il y en a un)
        if (currentGrid[x, y].visual != null)
        {
            Destroy(currentGrid[x, y].visual);
        }

        // 2. Suppression de la tile sur la Tilemap (très important !)
        Vector3Int tilePos = new Vector3Int(x, y, 0);
        if (tilemap.HasTile(tilePos))
        {
            tilemap.SetTile(tilePos, null);        // ← C'est ça qui manquait souvent
        }

        // 3. Nettoyage dans les deux grilles
        currentGrid[x, y].Reset();
        nextGrid[x, y].Reset();
    }

    void CollectCoin(int x, int y)
    {
        if (currentGrid[x, y].visual != null)
        {
            Destroy(currentGrid[x, y].visual);
        }

        // IMPORTANT : nettoyer CURRENT aussi immédiatement
        currentGrid[x, y].Reset();
        nextGrid[x, y].Reset();

        coins++;
        Debug.Log("Coins collected : " + coins);
    }

    void CheckEnemyCollision()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = currentGrid[x, y];
                Vector2Int? enemyDir = GetAdjacentTypeDirection(x, y, CellType.Enemy);

                if (currentGrid[x, y].type == CellType.Player && enemyDir is Vector2Int dirEnemy)
                {
                    int ex = x + dirEnemy.x;
                    int ey = y + dirEnemy.y;

                    Enemy enemy = currentGrid[ex, ey].visual.GetComponent<Enemy>();
                    bool dropCoins = enemy != null && enemy.dropCoins;

                    TriggerExplosion(x, y, dropCoins);
                    OnPlayerKilled();
                    return;
                }
            }
        }
    }

    void SimulateEnemy(int x, int y)
    {
        if (currentGrid[x, y].visual == null) return;

        Enemy enemy = currentGrid[x, y].visual.GetComponent<Enemy>();
        if (enemy == null) return;

        Direction chosenDir = enemy.GetNextDirection();
        Vector3Int dirVec = enemy.DirToVector(chosenDir);

        int nx = x + dirVec.x;
        int ny = y + dirVec.y;

        if (!IsInside(nx, ny))
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            enemy.ForceReverse();
            return;
        }

        if (nextGrid[nx, ny].isReserved)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        if (currentGrid[nx, ny].isSolid)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            enemy.ForceReverse();
            return;
        }

        if (GetAdjacentTypeDirection(x, y, CellType.Player) != null)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        // === DÉPLACEMENT ===
        nextGrid[nx, ny].CopyFrom(currentGrid[x, y]);
        nextGrid[nx, ny].isReserved = true;

        // Mise à jour importante : on passe la nouvelle direction
        enemy.MoveTo(new Vector3Int(nx, ny, 0), chosenDir);
    }

    public Vector2Int? GetAdjacentTypeDirection(int x, int y, CellType type)
    {
        Vector2Int[] dirs =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int dir in dirs)
        {
            Cell cell = GetCell(x + dir.x, y + dir.y);

            if (cell != null && cell.type == type)
                return dir;
        }

        return null;
    }

    public void OnPlayerKilled()
    {
        gameOverPending = true;
        gameOverTimer = 2;
    }

    public void TriggerExplosion(int x, int y, bool lootCoins)
    {
        List<Vector3Int> spawnCoins = new List<Vector3Int>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                Vector3Int tilePos = new Vector3Int(nx, ny, 0);

                if (tilemap.HasTile(tilePos) && tilemap.GetTile(tilePos) == steelWallTile)
                {
                    if (tilemap.GetTile(tilePos) == steelWallTile)
                        continue;
                    else
                        tilemap.SetTile(tilePos, null);
                }
                
                currentGrid[nx, ny].Reset();
                
                spawnCoins.Add(new Vector3Int(nx, ny));
            }
        }
        foreach (Vector3Int spawn in spawnCoins)
        {
            Debug.Log(spawn);
        }
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public Cell GetCell(int x, int y)
    {
        if (!IsInside(x, y)) 
            return null;
        return currentGrid[x, y];
    }

    void SwapGrids()
    {
        (currentGrid, nextGrid) = (nextGrid, currentGrid);
    }

    void RenderGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = currentGrid[x, y];

                if (cell.visual != null)
                {
                    Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
                    cell.visual.transform.position = worldPos;
                }
            }
        }
    }
}