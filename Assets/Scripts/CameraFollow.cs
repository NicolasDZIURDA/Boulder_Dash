using UnityEngine;

public class CameraStep : MonoBehaviour
{
    public Transform player;

    public float stepX = 3f;
    public float stepY = 2f;
    public float thresholdX = 2f;
    public float thresholdY = 1.5f;

    void LateUpdate()
    {
        if (!player) return;

        Vector3 camPos = transform.position;

        float dx = player.position.x - camPos.x;
        float dy = player.position.y - camPos.y;

        // Horizontal
        if (Mathf.Abs(dx) > thresholdX)
        {
            camPos.x += Mathf.Sign(dx) * stepX;
        }

        // Vertical
        if (Mathf.Abs(dy) > thresholdY)
        {
            camPos.y += Mathf.Sign(dy) * stepY;
        }

        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
    }
}