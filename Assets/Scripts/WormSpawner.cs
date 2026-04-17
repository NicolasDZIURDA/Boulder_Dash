using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class WormSpawnerGrid : MonoBehaviour
{
    public GameObject wormHeadPrefab;
    public GameObject wormBodyPrefab;
    public GameObject wormTailPrefab;

    public int wormLength = 5;
    public Tilemap tilemap;

    public float spawnDelay = 0.05f;
    private bool spawned = false;

    public Vector3Int initialDirection = Vector3Int.right;

    public void SpawnWorm()
    {
        if (spawned) return;
        spawned = true;

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        Vector3Int startCell = tilemap.WorldToCell(transform.position);

        WormGrid headScript = null;

        // 👉 direction inverse pour construire le corps derrière la tête
        Vector3Int buildDir = -initialDirection;

        List<Vector3Int> spawnCells = new List<Vector3Int>();

        // 🔹 calcul des positions du worm
        for (int i = 0; i < wormLength; i++)
        {
            Vector3Int cell = startCell + (buildDir * i);
            spawnCells.Add(cell);
        }

        for (int i = 0; i < wormLength; i++)
        {
            Vector3Int cell = spawnCells[i];
            Vector3 worldPos = tilemap.GetCellCenterWorld(cell);

            GameObject segment;

            if (i == 0)
                segment = Instantiate(wormHeadPrefab, worldPos, Quaternion.identity);
            else if (i == wormLength - 1)
                segment = Instantiate(wormTailPrefab, worldPos, Quaternion.identity);
            else
                segment = Instantiate(wormBodyPrefab, worldPos, Quaternion.identity);

            if (i == 0)
            {
                headScript = segment.GetComponent<WormGrid>();
                headScript.tilemap = tilemap;
                headScript.moveDirection = initialDirection;

                // 🔥 historique pré-rempli (EN CELLULES)
                for (int j = 0; j < 50; j++)
                {
                    headScript.positionHistory.Add(cell);
                }
            }
            else
            {
                WormSegmentGrid seg = segment.GetComponent<WormSegmentGrid>();
                seg.head = headScript;
                seg.index = i;
                seg.tilemap = tilemap;
            }

            // ⏱ petit délai pour effet visuel
            yield return new WaitForSeconds(spawnDelay);
        }

        gameObject.SetActive(false);
    }
}