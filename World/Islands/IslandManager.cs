using UnityEngine;
using System.Collections.Generic;

public class LinearExactIslandGenerator : MonoBehaviour
{
    [Header("Reference")]
    public Transform referencePoint; // your empty object

    [Header("Prefabs")]
    public GameObject[] islandPrefabs;

    [Header("Generation")]
    public int islandsAhead = 6;
    public int islandsBehind = 2;

    [Header("Spacing")]
    public float extraGap = 0.5f; // small gap between islands

    private List<GameObject> spawned = new List<GameObject>();
    private float lastSpawnX = 0f;

    void Start()
    {
        GenerateInitial();
    }

    void Update()
    {
        if (referencePoint == null) return;

        ManageIslands();
    }

    void GenerateInitial()
    {
        lastSpawnX = referencePoint.position.x;

        for (int i = 0; i < islandsAhead; i++)
        {
            SpawnNextIsland();
        }
    }

    void ManageIslands()
    {
        // Spawn forward
        while (lastSpawnX < referencePoint.position.x + islandsAhead * 10f)
        {
            SpawnNextIsland();
        }

        // Cleanup behind
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] == null) continue;

            if (spawned[i].transform.position.x < referencePoint.position.x - islandsBehind * 10f)
            {
                Destroy(spawned[i]);
                spawned.RemoveAt(i);
            }
        }
    }

    void SpawnNextIsland()
    {
        GameObject prefab = islandPrefabs[Random.Range(0, islandPrefabs.Length)];

        // 🔥 Get correct size
        Renderer rend = prefab.GetComponentInChildren<Renderer>();
        Vector3 size = rend.bounds.size;

        float width = size.x;
        float height = size.y;

        // 🔥 Calculate Y (THIS IS THE KEY FIX)
        float referenceBottom = referencePoint.position.y;
        float spawnY = referenceBottom - (height / 2f);

        // 🔥 Calculate X (edge-to-edge placement)
        float spawnX = lastSpawnX + (width / 2f) + extraGap;

        Vector3 spawnPos = new Vector3(spawnX, spawnY, referencePoint.position.z);

        GameObject island = Instantiate(prefab, spawnPos, Quaternion.identity);

        spawned.Add(island);

        // 🔥 Update last edge position
        lastSpawnX = spawnX + (width / 2f);
    }
}