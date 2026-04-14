using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GridReservation : MonoBehaviour
{
    public static HashSet<Vector3Int> reservedCells = new HashSet<Vector3Int>();
    public static Dictionary<Vector3Int, Enemy> occupiedCells = new Dictionary<Vector3Int, Enemy>();

    void LateUpdate()
    {
        // reset à chaque frame
        reservedCells.Clear();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        occupiedCells.Clear();
    }

    public static bool IsOccupied(Vector3Int pos)
    {
        return occupiedCells.ContainsKey(pos);
    }

    public static Enemy GetEnemyAt(Vector3Int pos)
    {
        if (occupiedCells.TryGetValue(pos, out Enemy e))
            return e;

        return null;
    }

    public static void Reserve(Vector3Int pos, Enemy enemy)
    {
        occupiedCells[pos] = enemy;
    }

    public static void Release(Vector3Int pos)
    {
        if (occupiedCells.ContainsKey(pos))
            occupiedCells.Remove(pos);
    }
}