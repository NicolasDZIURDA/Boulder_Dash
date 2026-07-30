using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 32;
    public int height = 32;
    public float tickRate = 0.1f;

    public static GridManager Instance;
    private FallingObjectSystem fallingObjectSystem;
    private WallSystem wallSystem;
    private AmoebaSystem amoebaSystem;
    public Worm goodWormPrefab;
    public Worm evilWormPrefab;
    private Worm worm;

    public List<MoveIntent> intents = new List<MoveIntent>();

    [Header("Theme & Tiles")]
    private ThemeData theme;
    public Tilemap tilemap;
    public TileBase dirtTile;
    public TileBase brickWallTile;
    public TileBase steelWallTile;
    public TileBase slimeTile;
    public TileBase growingWallTile;
    public TileBase magicWallTile;
    public TileBase amoeba;

    private Cell[,] currentGrid;
    private Cell[,] nextGrid;

    [Header("References")]
    public Player inputController;
    public GameObject coinPrefab;
    public GameObject rockPrefab;
    public GameObject enemyPrefab;

    private int coins;
    private float timer;
    private bool explosionPending;
    private List<ExplosionEvent> pendingExplosions = new();
    private Vector2Int previousPlayer;
    public bool magicWallActivated = false;
    public int magicWallTime = 0;
    private int countAmoeba = 0;
    public bool gameOverPending = false;
    public int gameOverTimer = 0;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        theme = LevelManager.Instance.currentTheme;
        Camera.main.backgroundColor = theme.backgroundColor;
        InitGrid();
        InjectSceneObjects();
        inputController = FindObjectOfType<Player>();
        fallingObjectSystem = new FallingObjectSystem(this);
        wallSystem = new WallSystem(this);
        amoebaSystem = new AmoebaSystem(this);

        int rockCount = FindObjectsOfType<GameObject>().Count(go => go.name == "Rock");
        //Debug.Log($"Rocks : {rockCount}");
        int coinCount = FindObjectsOfType<GameObject>().Count(go => go.name == "Coin");
        //Debug.Log($"Coins : {coinCount}");
        int bfCount = FindObjectsOfType<GameObject>().Count(go => go.name == "Butterfly");
        //Debug.Log($"Butterflies : {bfCount}");
        int ffCount = FindObjectsOfType<GameObject>().Count(go => go.name == "Firefly");
        //Debug.Log($"Fireflies : {ffCount}");
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
                {
                    SetCell(currentGrid[x, y], CellType.Dirt, true);
                    tilemap.SetTile(pos, theme.dirt);
                }
                else if (tile == brickWallTile)
                {
                    SetCell(currentGrid[x, y], CellType.Wall, true, WallType.Brick);
                    tilemap.SetTile(pos, theme.brickWall);
                }
                else if (tile == steelWallTile)
                {
                    SetCell(currentGrid[x, y], CellType.Wall, true, WallType.Steel);
                    tilemap.SetTile(pos, theme.steelWall);
                }
                else if (tile == slimeTile)
                {
                    SetCell(currentGrid[x, y], CellType.Wall, true, WallType.Slime);
                    tilemap.SetTile(pos, theme.slime);
                }
                else if (tile == growingWallTile)
                {
                    SetCell(currentGrid[x, y], CellType.Wall, true, WallType.Growing);
                    tilemap.SetTile(pos, theme.growingWall);
                }
                else if (tile == magicWallTile)
                {
                    SetCell(currentGrid[x, y], CellType.Wall, true, WallType.Magic);
                    tilemap.SetTile(pos, theme.magicWall);
                }
                else if (tile == amoeba)
                {
                    SetCell(currentGrid[x, y], CellType.Amoeba, true);
                    tilemap.SetTile(pos, theme.amoeba);
                }
                else
                    SetCell(currentGrid[x, y], CellType.Empty, false);
            }
        }
    }

    void InjectSceneObjects()
    {
        InjectObjectsWithTag("Player", CellType.Player, true, theme.player);
        InjectObjectsWithTag("Rock", CellType.Rock, true, theme.rock);
        InjectObjectsWithTag("Coin", CellType.Coin, true, theme.coin);
        InjectObjectsWithTag("Enemy", CellType.Enemy, true, null);
        InjectObjectsWithTag("WormSpawner", CellType.WormSpawner, true, null);
        InjectObjectsWithTag("Door", CellType.Door, true, theme.door);
    }

    void InjectObjectsWithTag(string tag, CellType type, bool isSolid, Sprite sprite)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);

        if ((tag == "Player" || tag == "Door") && objects.Length != 1)
        {
            Debug.LogError($"Error : To many objects of type '{tag}'. Found : {objects.Length}");
            return;
        }

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

                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                if (tag == "Coin")
                {
                    Coin coin = obj.GetComponent<Coin>();
                    if (coin == null) return;

                    sr.sprite = coin.coinType switch
                    {
                        CoinType.Normal => theme.coin,
                        CoinType.Shiny => theme.shinyCoin,
                    };
                }
                else if (tag == "Enemy")
                {
                    Enemy enemy = obj.GetComponent<Enemy>();
                    if (enemy == null) return;

                    sr.sprite = enemy.enemyType switch
                    {
                        EnemyType.Butterfly => theme.butterfly,
                        EnemyType.Firefly => theme.firefly,
                    };
                }
                else if (tag == "WormSpawner")
                {
                    WormSpawner wormSpawner = obj.GetComponent<WormSpawner>();
                    if (wormSpawner == null) return;

                    sr.sprite = wormSpawner.wormType switch
                    {
                        WormType.Good => theme.goodWormSpawn,
                        WormType.Evil => theme.evilWormSpawn
                    };
                }
                else
                {
                    sr.sprite = sprite;
                }
            }
        }
    }

    public void SetCell(Cell cell, CellType type, bool isSolid, WallType walltype = WallType.None)
    {
        cell.type = type;
        cell.isSolid = isSolid;
        cell.wallType = walltype;
    }

    // ==================== FONCTION CORE ====================
    void Tick()
    {
        intents.Clear();

        CheckGameOver();
        ClearNextGrid();
        CheckAllCrushes();
        CheckEnemyCollisions();
        ApplyExplosions();
        
        SimulateWorld();
        ResolveIntents();

        SwapGrids();
        RenderGrid();

        CheckMagicWallTime();
        CheckAmoebaSize();
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
                        pendingExplosions.Add(new ExplosionEvent(x, y - 1, currentGrid[x, y].type == CellType.Coin));
                        explosionPending = true;
                        OnPlayerKilled();
                    }
                    else if (currentGrid[x, y - 1].type == CellType.Enemy)
                    {
                        Enemy enemy = currentGrid[x, y - 1].visual.GetComponent<Enemy>();
                        ReserveCellsForExplosion(x, y - 1);
                        pendingExplosions.Add(new ExplosionEvent(x, y - 1, enemy.enemyType == EnemyType.Butterfly));
                        explosionPending = true;
                    }
                }
            }
        }
    }

    void CheckEnemyCollisions()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (currentGrid[x, y].type != CellType.Player && currentGrid[x, y].type != CellType.Amoeba) continue;

                Cell cell = currentGrid[x, y];
                List<Vector2Int>? enemies = GetAdjacentEnemyPosition(x, y);

                if (enemies != null)
                {
                    foreach (Vector2Int enemyPos in enemies)
                    {
                        Enemy enemy = currentGrid[enemyPos.x, enemyPos.y].visual.GetComponent<Enemy>();
                        if (enemy == null) continue;

                        if (enemyPos.x - x == 0 || enemyPos.y - y == 0)     // Adjacent direct non diagonal
                        {
                            Vector2Int explosionPos = currentGrid[x, y].type == CellType.Player ? new Vector2Int(x, y) : enemyPos;    // définir le centre de l'explosion
                            pendingExplosions.Add(new ExplosionEvent(explosionPos.x, explosionPos.y, enemy.enemyType == EnemyType.Butterfly));
                            explosionPending = true;
                            if (currentGrid[x, y].type == CellType.Player)
                                OnPlayerKilled();
                            return;
                        }
                        else    // On ajoute une collision si l'ennemi et le joueur se croise en diagonale
                        {
                            if (currentGrid[x, y].type == CellType.Player)
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
                                    pendingExplosions.Add(new ExplosionEvent(x, y, enemy.enemyType == EnemyType.Butterfly));
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
    }

    // ==================== SIMULATION DU MONDE ====================    
    void SimulateWorld()
    {
        for (int y = height - 1; y >= 0; y--)   // en haut à gauche jusqu'en bas à droite
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
                    case CellType.Door:
                        nextGrid[x, y].CopyFrom(currentGrid[x, y]);
                        switch (currentGrid[x, y].wallType)
                        {
                            case WallType.Slime:
                                wallSystem.PerformSlime(x, y);
                                break;
                            case WallType.Growing:
                                wallSystem.PerformGrowingWall(x, y);
                                break;
                            case WallType.Magic:
                                wallSystem.PerformMagicWall(x, y);
                                break;
                        }
                        break;
                    case CellType.Worm:
                        SimulateWorm(x, y);
                        break;
                    case CellType.Amoeba:
                        nextGrid[x, y].CopyFrom(currentGrid[x, y]);
                        amoebaSystem.PerformAmoeba(x, y);
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
                    nextGrid[intent.from.x, intent.from.y].isReserved = true;   // fix en cas de push de rocher sur worm
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
            nextGrid[winner.from.x, winner.from.y].Reset();

            if (winner.type == CellType.Enemy)
            {
                Enemy enemy = currentGrid[winner.from.x, winner.from.y].visual.GetComponent<Enemy>();
                if (enemy != null)
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

            if (winner.type == CellType.Wall)   // pour le growing wall
            {
                nextGrid[winner.to.x, winner.to.y].type = CellType.Wall;
                nextGrid[winner.to.x, winner.to.y].isSolid = true;
                nextGrid[winner.to.x, winner.to.y].wallType = WallType.Growing;

                Vector3Int tilePos = new Vector3Int(winner.to.x, winner.to.y, 0);

                if (!tilemap.HasTile(tilePos))
                    tilemap.SetTile(tilePos, theme.growingWall);
            }

            if (winner.type == CellType.Amoeba)
            {
                nextGrid[winner.to.x, winner.to.y].type = CellType.Amoeba;
                nextGrid[winner.to.x, winner.to.y].isSolid = true;

                Vector3Int tilePos = new Vector3Int(winner.to.x, winner.to.y, 0);

                if (!tilemap.HasTile(tilePos) || tilemap.GetTile(tilePos) == theme.dirt)
                    tilemap.SetTile(tilePos, theme.amoeba);
            }

            foreach (var loser in group)
            {
                if (loser.from == winner.from) continue;

                if (loser.type != CellType.Wall && loser.type != CellType.Amoeba)
                    nextGrid[loser.from.x, loser.from.y].CopyFrom(currentGrid[loser.from.x, loser.from.y]);
            }
        }
    }

    // ==================== APPLICATION DES EXPLOSION ====================    
    void ApplyExplosions()
    {
        if (explosionPending)
        {
            foreach (var explosion in pendingExplosions)
            {
                TriggerExplosion(explosion.x, explosion.y, explosion.lootCoins);
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
                nextGrid[nx, ny].isReserved = true;
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

                if (!IsInside(nx, ny)) continue;

                if (currentGrid[nx, ny].type == CellType.Player)
                    OnPlayerKilled();

                Vector3Int tilePos = new Vector3Int(nx, ny, 0);

                if (tilemap.HasTile(tilePos))
                {
                    if (tilemap.GetTile(tilePos) == theme.steelWall)
                        continue;
                    else
                        tilemap.SetTile(tilePos, null);
                }

                if (currentGrid[nx, ny].visual != null)
                {
                    Destroy(currentGrid[nx, ny].visual);
                }

                currentGrid[nx, ny].Reset();
                nextGrid[nx, ny].Reset();

                if (lootCoins)
                {
                    GameObject coin = Instantiate(coinPrefab, tilemap.GetCellCenterWorld(new Vector3Int(nx, ny)), Quaternion.identity);
                    currentGrid[nx, ny].type = CellType.Coin;
                    currentGrid[nx, ny].isSolid = true;
                    currentGrid[nx, ny].visual = coin;
                    currentGrid[nx, ny].justSpawned = true;
                    nextGrid[nx, ny].isReserved = true;
                    SpriteRenderer sr = coin.GetComponent<SpriteRenderer>();
                    sr.sprite = theme.coin;
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
            || currentGrid[nx, ny].type == CellType.Worm
            || currentGrid[nx, ny].type == CellType.Amoeba)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        if (currentGrid[nx, ny].type == CellType.WormSpawner)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);

            WormSpawner spawner = currentGrid[nx, ny].visual.GetComponent<WormSpawner>();

            Destroy(currentGrid[nx, ny].visual);
            currentGrid[nx, ny].visual = null;

            if (nextGrid[nx, ny].visual != null)
            {
                Destroy(nextGrid[nx, ny].visual);
                nextGrid[nx, ny].visual = null;
            }

            Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(nx, ny, 0));

            Worm wormPrefab = (spawner.wormType == WormType.Good) ? goodWormPrefab : evilWormPrefab;

            worm = Instantiate(wormPrefab, worldPos, Quaternion.identity);
            worm.Init(this, tilemap);
            worm.BuildWorm();
            worm.StartSpawn();
            worm.UpdateHistory(new Vector3Int(nx, ny, 0));

            currentGrid[nx, ny].Reset();    // au cas où le wormspawner est simulé après le player
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
                    from = new Vector3Int(nx, ny),
                    to = new Vector3Int(rx, ry),
                    type = CellType.Rock
                });
            }
            else
            {
                nextGrid[x, y].CopyFrom(currentGrid[x, y]);
                return;
            }
        }

        if (currentGrid[nx, ny].type == CellType.Dirt)
        {
            tilemap.SetTile(new Vector3Int(nx, ny, 0), null);
            nextGrid[nx, ny].Reset();
            currentGrid[nx, ny].Reset();
        }

        if (currentGrid[nx, ny].type == CellType.Coin)
        {
            Destroy(currentGrid[nx, ny].visual);
            currentGrid[nx, ny].Reset();
            coins++;
            Debug.Log("Coins collected : " + coins);
        }
        
        if (currentGrid[nx, ny].type == CellType.Door)
        {
            Debug.Log("You win !");
            LevelManager.Instance.LoadNextLevel();
        }

        intents.Add(new MoveIntent
        {
            from = new Vector3Int(x, y),
            to = new Vector3Int(nx, ny),
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
            enemy.ForceReverse();
            return;
        }

        intents.Add(new MoveIntent
        {
            from = new Vector3Int(x, y),
            to = new Vector3Int(nx, ny),
            type = CellType.Enemy
        });

        nextGrid[x, y].Reset();
    }

    void SimulateWorm(int x, int y)
    {
        if (currentGrid[x, y].visual == null) return;

        Worm worm = currentGrid[x, y].visual.GetComponent<Worm>();
        if (worm == null) return;

        int dx = x + DirectionTools.DirToVector(worm.direction).x;
        int dy = y + DirectionTools.DirToVector(worm.direction).y;

        if (currentGrid[dx, dy].type == CellType.Rock)
        {
            if (worm.wormType == WormType.Good)
                TransformIntoObject(CellType.Coin, dx, dy);
            else
                TransformIntoObject(CellType.Enemy, dx, dy);
        }

        if (currentGrid[dx, dy].type == CellType.Coin && worm.wormType == WormType.Evil)
        {
            TransformIntoObject(CellType.Rock, dx, dy);
        }

        if (currentGrid[dx, dy].type == CellType.Enemy && worm.wormType == WormType.Good)
        {
            Enemy enemy = currentGrid[dx, dy].visual.GetComponent<Enemy>();
            if (enemy.enemyType == EnemyType.Firefly)                                   // Transforme uniquement les fireflies
            {
                TransformIntoObject(CellType.Rock, dx, dy);
                intents.RemoveAll(i =>
                    i.from.x == dx &&
                    i.from.y == dy);
            }
        }

        Direction chosenDir = worm.GetNextDirection();
        Vector3Int dirVec = DirectionTools.DirToVector(chosenDir);

        int nx = x + dirVec.x;
        int ny = y + dirVec.y;

        if (!IsInside(nx, ny) || currentGrid[nx, ny].isSolid)
        {
            nextGrid[x, y].CopyFrom(currentGrid[x, y]);
            return;
        }

        if (worm.positionHistory.Count > 1)
            if (new Vector3Int(nx, ny, 0) == worm.positionHistory[1])   // demi-tour
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

                worm.direction = DirectionTools.VectorToDir(worm.positionHistory[0] - worm.positionHistory[1]);

                return;
            }

        intents.Add(new MoveIntent
        {
            from = new Vector3Int(x, y),
            to = new Vector3Int(nx, ny),
            type = CellType.Worm
        });
    }

    public void TransformIntoObject(CellType type, int x, int y)
    {
        GameObject prefab = null;
        Sprite sprite = null;

        switch (type)
        {
            case CellType.Rock:
                prefab = rockPrefab;
                sprite = theme.rock;
                break;

            case CellType.Coin:
                prefab = coinPrefab;
                sprite = theme.coin;
                break;

            case CellType.Enemy:
                prefab = enemyPrefab;
                sprite = theme.firefly;
                break;
        }

        if (currentGrid[x, y].visual != null)
        {
            Destroy(currentGrid[x, y].visual);
            currentGrid[x, y].Reset();
        }

        GameObject obj = Instantiate(prefab, tilemap.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);

        if (prefab == enemyPrefab)
            obj.GetComponent<Enemy>().Init();

        currentGrid[x, y].type = type;
        currentGrid[x, y].isSolid = true;
        currentGrid[x, y].visual = obj;
        currentGrid[x, y].justSpawned = true;
        nextGrid[x, y].isReserved = true;
        nextGrid[x, y].CopyFrom(currentGrid[x, y]);

        obj.GetComponent<SpriteRenderer>().sprite = sprite;
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

    // ==================== MAGIC WALL CHECK ====================
    void CheckMagicWallTime()
    {
        if (magicWallActivated)
        {
            magicWallTime += 1;

            if (magicWallTime >= 600)
            {
                magicWallActivated = false;
            }
        }
    }

    // ==================== AMOEBA CHECK ====================
    void CheckAmoebaSize()
    {
        if (IsAmoebaEnclosed())
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (currentGrid[x, y].type == CellType.Amoeba)
                        TransformAmoeba(coinPrefab, x, y, CellType.Coin, theme.coin);
                }
            }
        }

        if (CountAmoeba() > 50)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (currentGrid[x, y].type == CellType.Amoeba)
                        TransformAmoeba(rockPrefab, x, y, CellType.Rock, theme.rock);
                }
            }
        }
    }

    int CountAmoeba()
    {
        int count = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (currentGrid[x, y].type == CellType.Amoeba)
                {
                    count++;
                }
            }
        }

        return count;
    }

    void TransformAmoeba(GameObject newObj, int x, int y, CellType type, Sprite sprite)
    {
        currentGrid[x, y].Reset();

        Vector3Int tilePos = new Vector3Int(x, y, 0);
        tilemap.SetTile(tilePos, null);
        
        GameObject obj = Instantiate(newObj, tilemap.GetCellCenterWorld(new Vector3Int(x, y, 0)), Quaternion.identity);
        currentGrid[x, y].type = type;
        currentGrid[x, y].isSolid = true;
        currentGrid[x, y].visual = obj;
        nextGrid[x, y].isReserved = true;
        obj.GetComponent<SpriteRenderer>().sprite = sprite;
    }

    bool IsAmoebaEnclosed()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (currentGrid[x, y].type != CellType.Amoeba)
                    continue;

                if (IsFree(x, y + 1) || IsFree(x + 1, y) || IsFree(x, y - 1) || IsFree(x - 1, y))
                    return false;
            }
        }

        return true;
    }

    bool IsFree(int x, int y)
    {
        if (!IsInside(x, y)) return false;

        return currentGrid[x, y].type == CellType.Empty || currentGrid[x, y].type == CellType.Dirt;
    }

    // ==================== FONCTIONS PRATIQUES ====================
    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
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
            CellType.Amoeba => 9,
            CellType.Worm => 8,
            CellType.Player => 7,
            CellType.Enemy => 6,
            CellType.Wall => 5,
            _ => 0
        };
    }

    public void OnPlayerKilled()
    {
        gameOverPending = true;
        gameOverTimer = 2;
    }
}