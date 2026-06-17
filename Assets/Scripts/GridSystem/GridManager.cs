using System.Collections.Generic;
using System.Linq;
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
    public Worm wormPrefab;
    private Worm worm;
    public List<MoveIntent> intents = new List<MoveIntent>();

    [Header("Tiles")]
    public Tilemap tilemap;
    public TileBase dirtTile;
    public TileBase brickWallTile;
    public TileBase steelWallTile;
    public TileBase growingWallTile;
    public TileBase magicWallTile;
    public TileBase quicksandTile;

    [Header("References")]
    public PlayerController inputController;

    private Cell[,] currentGrid;
    private Cell[,] nextGrid;
    public GameObject coinPrefab;
    private int coins;
    private float timer;
    private bool explosionPending;
    private List<ExplosionEvent> pendingExplosions = new();
    private Vector2Int previousPlayer;
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

    // ==================== SET UP ET INJECTION DES ELEMENTS DE LA SCENE ====================
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
                    SetCell(currentGrid[x, y], CellType.Dirt, true);
                else if (tile == brickWallTile || tile == steelWallTile || tile == growingWallTile || tile == magicWallTile || tile == quicksandTile)
                    SetCell(currentGrid[x, y], CellType.Wall, true);
                else
                    SetCell(currentGrid[x, y], CellType.Empty, false);
            }
        }
    }

    void InjectSceneObjects()
    {
        InjectObjectsWithTag("Rock", CellType.Rock, true);
        InjectObjectsWithTag("Coin", CellType.Coin, true);
        InjectObjectsWithTag("Enemy", CellType.Enemy, true);
        InjectObjectsWithTag("WormSpawner", CellType.WormSpawner, true);
        InjectPlayer();
    }

    void InjectObjectsWithTag(string tag, CellType type, bool isSolid)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            Vector3Int cellPos = tilemap.WorldToCell(obj.transform.position);
            int x = cellPos.x;
            int y = cellPos.y;

            if (IsInside(x, y))
            {
                SetCell(currentGrid[x, y], type, isSolid);
                SetCell(nextGrid[x, y], type, isSolid);

                currentGrid[x, y].visual = obj;
                nextGrid[x, y].visual = obj;
            }
        }
    }

    public void SetCell(Cell cell, CellType type, bool isSolid)
    {
        cell.type = type;
        cell.isSolid = isSolid;
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
            currentGrid[x, y].isSolid = true;
            currentGrid[x, y].visual = player;
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
        }
    }

    // ==================== FONCTION CORE ====================
    void Tick()
    {
        intents.Clear();

        CheckGameOver();
        ClearNextGrid();
        CheckAllCrushes();
        CheckEnemyCollision();
        ApplyExplosions();
        
        SimulateWorld();
        ResolveIntents();
        
        SwapGrids();
        RenderGrid();
    }

    // ==================== CHECK EN PRE SIMULATION ====================
    void CheckGameOver()
    {
        if (gameOverPending)
        {
            gameOverTimer--;

            if (gameOverTimer <= 0)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            
            return;
        }
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

    private void CheckAllCrushes()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = currentGrid[x, y];

                if ((currentGrid[x, y].type == CellType.Rock || currentGrid[x, y].type == CellType.Coin) && currentGrid[x, y].isFalling)
                {
                    if (currentGrid[x, y - 1].type == CellType.Player)
                    {
                        ReserveCellsForExplosion(x, y - 1);
                        pendingExplosions.Add(new ExplosionEvent(x, y - 1, false));
                        explosionPending = true;
                        OnPlayerKilled();
                    }
                    else if (currentGrid[x, y - 1].type == CellType.Enemy)
                    {
                        Enemy enemy = currentGrid[x, y - 1].visual.GetComponent<Enemy>();
                        bool dropCoins = enemy.dropCoins;
                        ReserveCellsForExplosion(x, y - 1);
                        pendingExplosions.Add(new ExplosionEvent(x, y - 1, dropCoins));
                        explosionPending = true;
                    }
                }
            }
        }
    }

    void CheckEnemyCollision()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (currentGrid[x, y].type != CellType.Player)
                    continue;

                Cell cell = currentGrid[x, y];
                List<Vector2Int>? enemies = GetAdjacentEnemyPosition(x, y);

                if (enemies != null)
                {
                    foreach (Vector2Int enemyPos in enemies)
                    {
                        Enemy enemy = currentGrid[enemyPos.x, enemyPos.y].visual.GetComponent<Enemy>();
                        bool dropCoins = enemy != null && enemy.dropCoins;

                        if (enemyPos.x - x == 0 || enemyPos.y - y == 0)     // Adjacent direct non diagonal
                        {
                            pendingExplosions.Add(new ExplosionEvent(x, y, dropCoins));
                            explosionPending = true;
                            OnPlayerKilled();
                            return;
                        }
                        else
                        {
                            Vector2Int playerMove = new Vector2Int(x - previousPlayer.x, y - previousPlayer.y);
                            Vector2Int enemyMove = new Vector2Int(0, 0);
                            switch (enemy.direction)
                            {
                                case Direction.Up:
                                    enemyMove = Vector2Int.up;
                                    break;
                                case Direction.Left:
                                    enemyMove = Vector2Int.left;
                                    break;
                                case Direction.Down:
                                    enemyMove = Vector2Int.down;
                                    break;
                                case Direction.Right:
                                    enemyMove = Vector2Int.right;
                                    break;
                            }
                            if (playerMove == -enemyMove)
                            {
                                pendingExplosions.Add(new ExplosionEvent(x, y, dropCoins));
                                explosionPending = true;
                                OnPlayerKilled();
                                return;
                            }
                        }
                    }
                }
            }
        }
    }

    // ==================== SIMULATION DU MONDE ====================    
    void SimulateWorld()
    {
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                switch (currentGrid[x, y].type)
                {
                    case CellType.Rock:
                    case CellType.Coin:
                        fallingObjectSystem.Simulate(x, y);
                        break;
                    case CellType.Player:
                        SimulatePlayer(x, y);
                        break;
                    case CellType.Enemy:
                        SimulateEnemy(x, y);
                        break;
                    case CellType.Dirt:
                    case CellType.Wall:
                    case CellType.WormSpawner:
                        nextGrid[x, y].CopyFrom(currentGrid[x, y]);
                        break;
                    case CellType.Worm:
                        SimulateWorm(x, y);
                        break;
                }
            }
        }
    }

    // ==================== RESOLUTION DES INTENTIONS DE DEPLACEMENT ====================
    void ResolveIntents()
    {
        var groups = intents.GroupBy(i => i.to);

        foreach (var group in groups)
        {
            if (nextGrid[group.Key.x, group.Key.y].isReserved)
            {
                foreach (var intent in group)
                {
                    nextGrid[intent.from.x, intent.from.y].CopyFrom(currentGrid[intent.from.x, intent.from.y]);
                    nextGrid[intent.from.x, intent.from.y].isReserved = true;
                }

                continue;
            }

            MoveIntent winner;

            if (group.Count() == 1)
                winner = group.First();
            else
            {
                int maxPriority = group.Max(i => GetPriority(i.type));
                var candidates = group.Where(i => GetPriority(i.type) == maxPriority).ToList();
                winner = candidates[Random.Range(0, candidates.Count)];
            }
            nextGrid[winner.to.x, winner.to.y].CopyFrom(currentGrid[winner.from.x, winner.from.y]);

            if (winner.type == CellType.Enemy)
            {
                Enemy enemy = currentGrid[winner.from.x, winner.from.y].visual.GetComponent<Enemy>();

                enemy.MoveTo(new Vector3Int(winner.to.x, winner.to.y, 0), DirectionTools.VectorToDir(winner.to - winner.from));
            }

            if (winner.type == CellType.Worm)
            {
                Worm worm = currentGrid[winner.from.x, winner.from.y].visual.GetComponent<Worm>();

                worm.UpdateHistory(new Vector3Int(winner.to.x, winner.to.y, 0));
                
                foreach (var cell in worm.positionHistory)
                {
                    nextGrid[cell.x, cell.y].type = CellType.Worm;
                    nextGrid[cell.x, cell.y].isSolid = true;
                }
            }

            foreach (var loser in group)
            {
                if (loser.from == winner.from) continue;

                nextGrid[loser.from.x, loser.from.y].CopyFrom(currentGrid[loser.from.x, loser.from.y]);
            }
        }
    }

    // ==================== APPLICATION DES EXPLOSION ====================    
    void ApplyExplosions()
    {
        if (explosionPending)
        {
            foreach (var pos in pendingExplosions)
            {
                TriggerExplosion(pos.x, pos.y, pos.lootCoins);
            }

            pendingExplosions.Clear();
            explosionPending = false;
        }
    }

    // ==================== GESTION DES EXPLOSION ====================
    public void ReserveCellsForExplosion(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                nextGrid[nx, ny].isReserved = true; // on garde isReserved pour l'explosion uniquement
            }
        }
    }

    public void TriggerExplosion(int x, int y, bool lootCoins)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (!IsInside(nx, ny))
                    continue;

                if (currentGrid[nx, ny].type == CellType.Player)
                    OnPlayerKilled();

                Vector3Int tilePos = new Vector3Int(nx, ny, 0);

                if (tilemap.HasTile(tilePos))
                {
                    if (tilemap.GetTile(tilePos) == steelWallTile)
                        continue;
                    else
                        tilemap.SetTile(tilePos, null);
                }
                
                if (currentGrid[nx, ny].visual != null)
                {
                    Destroy(currentGrid[nx, ny].visual);
                    currentGrid[nx, ny].Reset();
                }

                nextGrid[nx, ny].Reset();

                if (lootCoins)
                {
                    GameObject coin = Instantiate(coinPrefab, tilemap.GetCellCenterWorld(new Vector3Int(nx, ny)), Quaternion.identity);
                    currentGrid[nx, ny].type = CellType.Coin;
                    currentGrid[nx, ny].isSolid = true;
                    currentGrid[nx, ny].isFalling = false;
                    currentGrid[nx, ny].visual = coin;
                    nextGrid[nx, ny].isReserved = true;
                }
                else
                {
                    nextGrid[nx, ny].Reset();
                }
            }
        }
    }

    // ==================== SIMULATIONS DES ENTITES ====================
    void SimulatePlayer(int x, int y)
    {
        Vector2Int move = inputController.MoveInput;

        previousPlayer = new Vector2Int(x, y);

        if (move == Vector2Int.zero)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        int nx = x + move.x;
        int ny = y + move.y;

        inputController.ConsumeInput();

        if (!IsInside(nx, ny) 
            || currentGrid[nx, ny].type == CellType.Wall
            || currentGrid[nx, ny].type == CellType.Enemy
            || currentGrid[nx, ny].type == CellType.Worm)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        if (currentGrid[nx, ny].type == CellType.WormSpawner)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);

            if (nextGrid[nx, ny].visual != null)
            {
                Destroy(nextGrid[nx, ny].visual);
                nextGrid[nx, ny].visual = null;
            }

            Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(nx, ny, 0));

            worm = Instantiate(wormPrefab, worldPos, Quaternion.identity);
            worm.Init(this, tilemap);
            worm.BuildWorm();
            worm.StartSpawn();
            worm.UpdateHistory(new Vector3Int(nx, ny, 0));

            nextGrid[nx, ny].Reset();
            nextGrid[nx, ny].type = CellType.Worm;
            nextGrid[nx, ny].isSolid = true;
            nextGrid[nx, ny].visual = worm.gameObject;

            return;
        }

        if (currentGrid[nx, ny].type == CellType.Rock)
        {
            if (move == Vector2Int.left || move == Vector2Int.right)
            {
                int rx = nx + move.x;
                int ry = ny + move.y;

                if (!IsInside(rx, ry) || currentGrid[rx, ry].isSolid || fallingObjectSystem.IsFalling(nx, ny))
                {
                    nextGrid[x, y].CopyFrom(currentGrid[x, y]);
                    return;
                }

                intents.Add(new MoveIntent
                {
                    from = new Vector2Int(nx, ny),
                    to = new Vector2Int(rx, ry),
                    type = CellType.Rock
                });
            }
            else
            {
                nextGrid[x, y].CopyFrom(currentGrid[x, y]);
                return;
            }
        }

        if (currentGrid[nx, ny].type == CellType.Coin)
        {
            Destroy(currentGrid[nx, ny].visual);
            currentGrid[nx, ny].Reset();
            coins++;
            Debug.Log("Coins collected : " + coins);
        }

        if (currentGrid[nx, ny].type == CellType.Dirt)
        {
            tilemap.SetTile(new Vector3Int(nx, ny, 0), null);
            nextGrid[nx, ny].Reset();
            currentGrid[nx, ny].Reset();
        }

        intents.Add(new MoveIntent
        {
            from = new Vector2Int(x, y),
            to = new Vector2Int(nx, ny),
            type = CellType.Player
        });
    }

    void SimulateEnemy(int x, int y)
    {
        if (currentGrid[x, y].visual == null) return;

        Enemy enemy = currentGrid[x, y].visual.GetComponent<Enemy>();
        if (enemy == null) return;

        Direction chosenDir = enemy.GetNextDirection();
        Vector3Int dirVec = DirectionTools.DirToVector(chosenDir);

        int nx = x + dirVec.x;
        int ny = y + dirVec.y;

        if (!IsInside(nx, ny))
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        Vector2Int target = new Vector2Int(nx, ny);

        if (currentGrid[nx, ny].isSolid)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            //enemy.ForceReverse();
            return;
        }

        intents.Add(new MoveIntent
        {
            from = new Vector2Int(x, y),
            to = new Vector2Int(nx, ny),
            type = CellType.Enemy
        });
    }

    void SimulateWorm(int x, int y)
    {
        if (currentGrid[x, y].visual == null) return;

        Worm worm = currentGrid[x, y].visual.GetComponent<Worm>();
        if (worm == null) return;

        Direction chosenDir = worm.GetNextDirection();
        Vector3Int dirVec = DirectionTools.DirToVector(chosenDir);

        int nx = x + dirVec.x;
        int ny = y + dirVec.y;

        if (!IsInside(nx, ny)
            || currentGrid[nx, ny].isSolid)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        if (worm.positionHistory.Count > 1)
            if (new Vector3Int(nx, ny, 0) == worm.positionHistory[1])
            {
                foreach (var cell in worm.positionHistory)
                {
                    currentGrid[cell.x, cell.y].type = CellType.Worm;
                    currentGrid[cell.x, cell.y].isSolid = true;
                }

                int length = worm.positionHistory.Count;

                List<Vector3Int> oldHistory = new List<Vector3Int>(worm.positionHistory);

                for (int i = 0; i < length; i++)
                {
                    Vector3Int newPos = oldHistory[length - 1 - i];
                    Vector3 worldPos = tilemap.GetCellCenterWorld(newPos);
                    Vector3Int oldPos = oldHistory[i];
                    worm.wormSegments[i].transform.position = worldPos;
                    worm.positionHistory[i] = newPos;
                    nextGrid[newPos.x, newPos.y].CopyFrom(currentGrid[oldPos.x, oldPos.y]);
                    nextGrid[newPos.x, newPos.y].isReserved = true;
                }

                return;
            }

        intents.Add(new MoveIntent
        {
            from = new Vector2Int(x, y),
            to = new Vector2Int(nx, ny),
            type = CellType.Worm
        });
    }

    // ==================== CHANGEMENT DE GRILLE ET RENDU ====================
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

    // ==================== FONCTIONS PRATIQUES ====================
    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsReserved(int x, int y)
    {
        return nextGrid[x, y].isReserved;
    }

    public Cell GetCurrentCell(int x, int y)
    {
        if (!IsInside(x, y))
            return null;
        return currentGrid[x, y];
    }

    public Cell GetNextCell(int x, int y)
    {
        if (!IsInside(x, y))
            return null;
        return nextGrid[x, y];
    }
    
    public void CopyCurrentToNext(int x, int y)
    {
        nextGrid[x, y].CopyFrom(currentGrid[x, y]);
        nextGrid[x, y].isFalling = false;
    }

    public List<Vector2Int>? GetAdjacentEnemyPosition(int x, int y)
    {
        List<Vector2Int> enemiesAdjacents = new();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (currentGrid[nx, ny].type == CellType.Enemy)
                    enemiesAdjacents.Add(new Vector2Int(nx, ny));
            }
        }

        return enemiesAdjacents;
    }

    int GetPriority(CellType type)
    {
        return type switch
        {
            CellType.Rock => 10,
            CellType.Coin => 10,
            CellType.Worm => 8,
            CellType.Player => 7,
            CellType.Enemy => 6,
            _ => 0
        };
    }

    public void TransformObject(Vector3Int pos)
    {
        Debug.Log("Destruction de " + currentGrid[pos.x, pos.y].type + " en " + pos.x + " " + pos.y);
        Destroy(currentGrid[pos.x, pos.y].visual);
        currentGrid[pos.x, pos.y].Reset();
        GameObject obj = Instantiate(coinPrefab, tilemap.GetCellCenterWorld(new Vector3Int(pos.x, pos.y)), Quaternion.identity);
        currentGrid[pos.x, pos.y].type = CellType.Coin;
        currentGrid[pos.x, pos.y].isSolid = true;
        currentGrid[pos.x, pos.y].isFalling = false;
        currentGrid[pos.x, pos.y].visual = obj;
        nextGrid[pos.x, pos.y].isReserved = true;
    }

    void PerformGrowingWall(int x, int y)
    {
        if (!IsInside(x, y))
            return;

        Cell cell = currentGrid[x, y];

        TryGrowInto(x - 1, y);
        TryGrowInto(x + 1, y);
    }

    void TryGrowInto(int x, int y)
    {
        if (!IsInside(x, y))
            return;

        // 🧠 check futur (très important)
        if (nextGrid[x, y].type != CellType.Empty)
            return;

        if (currentGrid[x, y].type != CellType.Empty)
            return;

        nextGrid[x, y].type = CellType.Wall;
        nextGrid[x, y].isSolid = true;

        Vector3Int tilePos = new Vector3Int(x, y, 0);

        if (!tilemap.HasTile(tilePos))
            tilemap.SetTile(tilePos, growingWallTile);
    }

    public void OnPlayerKilled()
    {
        gameOverPending = true;
        gameOverTimer = 2;
    }
}