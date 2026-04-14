using UnityEngine;

public class WormSegment : MonoBehaviour
{
    public Worm head;
    public int index; // 1 = premier segment, 2 = deuxième...

    void Update()
    {
        if (head == null) return;

        if (head.positionHistory.Count > index)
        {
            transform.position = head.positionHistory[index - 1];
        }
    }
}