using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GridReservation : MonoBehaviour
{
    public static HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    public static Dictionary<Vector3Int, GameObject> objects = new Dictionary<Vector3Int, GameObject>();

    public static void Reserve(Vector3Int cell, GameObject obj)
    {
        occupiedCells.Add(cell);
        objects[cell] = obj;
    }

    public static void Free(Vector3Int cell)
    {
        occupiedCells.Remove(cell);
        objects.Remove(cell);
    }

    public static bool IsOccupied(Vector3Int cell)
    {
        return occupiedCells.Contains(cell);
    }

    public static GameObject GetObject(Vector3Int cell)
    {
        objects.TryGetValue(cell, out GameObject obj);
        return obj;
    }

    public static void ResetAll()
    {
        occupiedCells.Clear();
        objects.Clear();
    }
}