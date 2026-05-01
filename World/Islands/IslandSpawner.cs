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
    public float heightVariation = 2f;     // 🔥 up/down variation
    public float sideVariation = 3f;       // 🔥 zig-zag
    public float smoothness = 0.3f;        // 🔥 smooth curve

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

    void ManageIslands()
    {
        float playerX = player.position.x;

        int playerIndex = Mathf.FloorToInt(playerX / blockWidth);

        int minIndex = playerIndex - islandsBehind;
        int maxIndex = playerIndex + islandsAhead;

        // SPAWN
        for (int i = minIndex; i <= maxIndex; i++)
        {
            if (!spawned.ContainsKey(i))
            {
                SpawnIsland(i);
            }
        }

        // CLEANUP
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

        // 🔥 BASE FORWARD POSITION
        float spawnX = index * blockWidth;

        // 🔥 SMOOTH HEIGHT USING PERLIN NOISE
        float noiseY = Mathf.PerlinNoise(index * smoothness, 0f);
        float heightOffset = (noiseY - 0.5f) * heightVariation;

        float spawnY = goldenLine.position.y + heightOffset - (height / 2f);

        // 🔥 SIDE ZIG-ZAG (smooth curve)
        float noiseZ = Mathf.PerlinNoise(0f, index * smoothness);
        float sideOffset = (noiseZ - 0.5f) * sideVariation;

        float spawnZ = goldenLine.position.z + sideOffset;

        Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);

        GameObject island = Instantiate(prefab, spawnPos, Quaternion.identity);

        spawned.Add(index, island);
    }

    void CalculateBlockWidth()
    {
        if (islandPrefabs.Length == 0) return;

        Renderer r = islandPrefabs[0].GetComponentInChildren<Renderer>();
        blockWidth = r.bounds.size.x + extraGap;
    }
}