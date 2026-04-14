using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class WormSpawner : MonoBehaviour
{
    public GameObject wormHeadPrefab;
    public GameObject wormBodyPrefab;
    public GameObject wormTailPrefab;
    public int wormLength = 5;
    public Tilemap tilemap;

    public float spawnDelay = 0.1f;
    private bool spawned = false;

    public void SpawnWorm()
    {
        if (spawned) return;
        spawned = true;

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        Vector3 spawnPos = transform.position;
        Worm headScript = null;

        for (int i = 0; i < wormLength; i++)
        {
            GameObject segment;

            if (i == 0)
                segment = Instantiate(wormHeadPrefab, spawnPos, Quaternion.identity);
            else if (i == wormLength - 1)
                segment = Instantiate(wormTailPrefab, spawnPos, Quaternion.identity);
            else
                segment = Instantiate(wormBodyPrefab, spawnPos, Quaternion.identity);

            if (i == 0)
            {
                headScript = segment.GetComponent<Worm>();
                headScript.tilemap = tilemap;
                headScript.moveDirection = Vector3Int.right;

                // 🔥 Pré-remplir l'historique pour éviter bugs
                for (int j = 0; j < 50; j++)
                    headScript.positionHistory.Add(spawnPos);
            }
            else
            {
                WormSegment segmentScript = segment.GetComponent<WormSegment>();
                segmentScript.head = headScript;
                segmentScript.index = i;
            }

            // 👇 attendre avant le prochain segment
            yield return new WaitForSeconds(spawnDelay);
        }

        gameObject.SetActive(false);
    }
}