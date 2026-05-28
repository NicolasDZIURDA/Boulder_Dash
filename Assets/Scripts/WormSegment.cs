using UnityEngine;

public class WormSegment : MonoBehaviour
{
    public Worm head;
    public int index;

    void Update()
    {
        if (head == null) return;

        if (head.positionHistory.Count > index)
        {
            transform.position = head.positionHistory[index - 1];
        }
    }
}