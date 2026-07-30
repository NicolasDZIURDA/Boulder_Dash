using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewTheme", menuName = "BoulderDash/Theme")]
public class ThemeData : ScriptableObject
{
    [Header("Background")]
    public Color backgroundColor;

    [Header("Sprites")]
    public Sprite player;
    public Sprite rock;
    public Sprite coin;
    public Sprite shinyCoin;
    public Tile dirt;
    public Sprite butterfly;
    public Sprite butterfly2;
    public Sprite firefly;
    public Tile brickWall;
    public Tile steelWall;
    public Tile slime;
    public Tile growingWall;
    public Tile magicWall;
    public Tile amoeba;
    public Sprite goodWormSpawn;
    public Sprite goodWormHead;
    public Sprite goodWormBody;
    public Sprite goodWormTail;
    public Sprite evilWormSpawn;
    public Sprite evilWormHead;
    public Sprite evilWormBody;
    public Sprite evilWormTail;
    public Sprite door;
}