using UnityEngine;
using System.Collections.Generic;

public class ProceduralIslandGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform goldenLine;

    [Header("Prefabs")]
    public GameObject[] islandPrefabs;

    [Header("Blocks Around Player")]
    public int islandsAhead = 5;
    public int islandsBehind = 3;

    [Header("Spacing")]
    public float extraGap = 0.5f;

    [Header("Procedural Settings")]
    public float heightVariation = 2f;
    public float sideVariation = 3f;
    public float smoothness = 0.3f;

    // ---------------- GEMS ----------------
    [Header("Gems")]
    public GameObject gemPrefab;
    [Range(0f, 1f)] public float gemSpawnChance = 0.4f;
    public int maxGemsPerIsland = 2;
    public float gemHeightOffset = 0.5f;

    [Header("Gem Rotation")]
    public Vector3 gemRotationOffset;          // 🔥 FULL XYZ CONTROL
    public bool randomizeYRotation = true;     // 🔥 variation
    public bool alignToPathDirection = false;  // 🔥 face forward

    private Dictionary<int, GameObject> spawned = new Dictionary<int, GameObject>();
    private float blockWidth = 10f;

    void Start()
    {
        CalculateBlockWidth();
    }

    void Update()
    {
        if (player == null || goldenLine == null || islandPrefabs.Length == 0)
            return;

        ManageIslands();
    }

    // ---------------- CORE ----------------

    void ManageIslands()
    {
        float playerX = player.position.x;

        int playerIndex = Mathf.FloorToInt(playerX / blockWidth);

        int minIndex = playerIndex - islandsBehind;
        int maxIndex = playerIndex + islandsAhead;

        // 🔥 SPAWN
        for (int i = minIndex; i <= maxIndex; i++)
        {
            if (!spawned.ContainsKey(i))
            {
                SpawnIsland(i);
            }
        }

        // 🔥 CLEANUP
        List<int> toRemove = new List<int>();

        foreach (var kvp in spawned)
        {
            if (kvp.Key < minIndex || kvp.Key > maxIndex)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);

                toRemove.Add(kvp.Key);
            }
        }

        foreach (int key in toRemove)
        {
            spawned.Remove(key);
        }
    }

    // ---------------- SPAWN ISLAND ----------------

    void SpawnIsland(int index)
    {
        if (spawned.ContainsKey(index)) return;

        GameObject prefab = islandPrefabs[Random.Range(0, islandPrefabs.Length)];

        Renderer rend = prefab.GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogError("Prefab missing Renderer!");
            return;
        }

        Vector3 size = rend.bounds.size;
        float height = size.y;

        float spawnX = index * blockWidth;

        // 🔥 HEIGHT VARIATION
        float noiseY = Mathf.PerlinNoise(index * smoothness, 0f);
        float heightOffset = (noiseY - 0.5f) * heightVariation;
        float spawnY = goldenLine.position.y + heightOffset - (height / 2f);

        // 🔥 SIDE VARIATION
        float noiseZ = Mathf.PerlinNoise(0f, index * smoothness);
        float sideOffset = (noiseZ - 0.5f) * sideVariation;
        float spawnZ = goldenLine.position.z + sideOffset;

        Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);

        GameObject island = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 🔥 GEMS
        SpawnGems(island, size);

        spawned.Add(index, island);
    }

    // ---------------- BLOCK WIDTH ----------------

    void CalculateBlockWidth()
    {
        if (islandPrefabs.Length == 0) return;

        Renderer r = islandPrefabs[0].GetComponentInChildren<Renderer>();
        blockWidth = r.bounds.size.x + extraGap;
    }

    // ---------------- GEM SPAWN ----------------

    void SpawnGems(GameObject island, Vector3 size)
    {
        if (gemPrefab == null) return;

        if (Random.value > gemSpawnChance) return;

        int gemCount = Random.Range(1, maxGemsPerIsland + 1);

        for (int i = 0; i < gemCount; i++)
        {
            float randomX = Random.Range(-size.x * 0.4f, size.x * 0.4f);
            float randomZ = Random.Range(-size.z * 0.4f, size.z * 0.4f);

            Vector3 spawnPos = island.transform.position +
                new Vector3(randomX, size.y / 2f + gemHeightOffset, randomZ);

            // 🔥 BASE ROTATION
            Quaternion rotation = Quaternion.Euler(gemRotationOffset);

            // 🔥 RANDOM Y ROTATION
            if (randomizeYRotation)
            {
                float randomY = Random.Range(0f, 360f);
                rotation *= Quaternion.Euler(0f, randomY, 0f);
            }

            // 🔥 ALIGN TO PATH (X direction)
            if (alignToPathDirection)
            {
                Vector3 forwardDir = Vector3.right;
                rotation = Quaternion.LookRotation(forwardDir) * Quaternion.Euler(gemRotationOffset);
            }

            Instantiate(gemPrefab, spawnPos, rotation, island.transform);
        }
    }
}