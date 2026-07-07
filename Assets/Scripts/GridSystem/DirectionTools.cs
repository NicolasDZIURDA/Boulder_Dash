using UnityEngine;

public static class DirectionTools
{
    public static Direction VectorToDir(Vector3Int v)
    {
        if (v == Vector3Int.up) return Direction.Up;
        if (v == Vector3Int.down) return Direction.Down;
        if (v == Vector3Int.left) return Direction.Left;
        if (v == Vector3Int.right) return Direction.Right;

        return Direction.Down; // fallback
    }

    public static Vector3Int DirToVector(Direction d)
    {
        return d switch
        {
            Direction.Up => Vector3Int.up,
            Direction.Down => Vector3Int.down,
            Direction.Left => Vector3Int.left,
            Direction.Right => Vector3Int.right,
            _ => Vector3Int.zero
        };
    }

    public static Direction TurnLeft(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Left,
            Direction.Left => Direction.Down,
            Direction.Down => Direction.Right,
            Direction.Right => Direction.Up,
            _ => dir
        };
    }

    public static Direction TurnRight(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Right,
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            Direction.Left => Direction.Up,
            _ => dir
        };
    }

    public static Direction Opposite(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => dir
        };
    }

    public static float UpdateRotation(Direction dir)
    {
        return dir switch
        {
            Direction.Up => 0f,
            Direction.Right => -90f,
            Direction.Down => 180f,
            Direction.Left => 90f,
            _ => 0f
        };
    }
}
