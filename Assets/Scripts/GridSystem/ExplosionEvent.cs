using UnityEngine;

public struct ExplosionEvent
{
    public int x;
    public int y;
    public bool lootCoins;

    public ExplosionEvent(int x, int y, bool lootCoins)
    {
        this.x = x;
        this.y = y;
        this.lootCoins = lootCoins;
    }
}