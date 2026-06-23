using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class Worm : MonoBehaviour
{
    public Tilemap tilemap;
    private GridManager grid;

    public GameObject wormHeadPrefab;
    public GameObject wormBodyPrefab;
    public GameObject wormTailPrefab;
    public Direction direction = Direction.Right;
    public Vector3Int cellPosition;
    public bool isEvil = false;

    private int wormLength = 8;
    public List<GameObject> wormSegments = new List<GameObject>();
    public List<Vector3Int> positionHistory = new();

    public void Init(GridManager grid, Tilemap tilemap)
    {
        this.grid = grid;
        this.tilemap = tilemap;
    }

    public void BuildWorm()
    {
        if (tilemap == null)
            return;

        Vector3Int spawnCell = tilemap.WorldToCell(transform.position);

        wormSegments.Clear();
        positionHistory.Clear();

        wormSegments.Add(gameObject);

        for (int i = 0; i < wormLength - 1; i++)    // la tête est créée depuis le grid managers
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(spawnCell);

            GameObject prefab =
                (i == wormLength - 2) ? wormTailPrefab : wormBodyPrefab;

            GameObject segment = Instantiate(prefab, worldPos, Quaternion.identity);

            segment.SetActive(false);   // avant le spawn

            wormSegments.Add(segment);
        }
    }

    public void StartSpawn()
    {
        StartCoroutine(SpawnWorm());
    }

    IEnumerator SpawnWorm()
    {

        for (int i = 0; i < wormSegments.Count; i++)
        {
            wormSegments[i].SetActive(true);

            yield return new WaitForSeconds(grid.tickRate);
        }
    }

    public Direction GetNextDirection()
    {
        Direction chosen;

        if (CanMove(direction))
        {
            chosen = direction;
        }
        else
        {
            Direction left = DirectionTools.TurnLeft(direction);
            Direction right = DirectionTools.TurnRight(direction);
            Direction back = DirectionTools.Opposite(direction);

            List<Direction> validDirections = new();

            if (CanMove(left))
                validDirections.Add(left);

            if (CanMove(right))
                validDirections.Add(right);

            chosen = validDirections.Count > 0
                ? validDirections[Random.Range(0, validDirections.Count)]
                : back;
        }

        direction = chosen;
        return chosen;
    }

    public bool CanMove(Direction dir)
    {
        cellPosition = tilemap.WorldToCell(transform.position);
        Vector3Int nextPos = cellPosition + DirectionTools.DirToVector(dir);

        if (!grid.IsInside(nextPos.x, nextPos.y))
            return false;

        Cell target = grid.GetCurrentCell(nextPos.x, nextPos.y);
        if (target == null) return false;

        if (target.type == CellType.Rock)
        {
            if (!isEvil)
            {
                //gridManager.TransformObject(nextPos);
            }
        }

        if (target.type != CellType.Empty)
        {
            return false;
        }

        return true;
    }

    public void UpdateHistory(Vector3Int headPos)
    {
        positionHistory.Insert(0, headPos);

        if (positionHistory.Count > wormLength)
            positionHistory.RemoveAt(positionHistory.Count - 1);

        wormSegments[0].transform.position = tilemap.GetCellCenterWorld(positionHistory[0]);

        for (int i = 1; i < wormSegments.Count; i++)
        {
            if (positionHistory.Count > i)
            {
                Vector3 worldPos = tilemap.GetCellCenterWorld(positionHistory[i]);
                wormSegments[i].transform.position = worldPos;
            }
        }
    }
}
