using UnityEngine;
using System.Collections.Generic;

public class LinearExactIslandGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform player;           // 🔥 actual player (movement driver)
    public Transform goldenLine;       // 🔥 ONLY used for Y alignment

    [Header("Prefabs")]
    public GameObject[] islandPrefabs;

    [Header("Blocks Around Player")]
    public int islandsAhead = 3;
    public int islandsBehind = 3;

    [Header("Spacing")]
    public float extraGap = 0.5f;

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

    // ---------------- CORE SYSTEM ----------------

    void ManageIslands()
    {
        float playerX = player.position.x;

        // 🔥 Correct index based on PLAYER (not golden line)
        int playerIndex = Mathf.FloorToInt(playerX / blockWidth);

        int minIndex = playerIndex - islandsBehind;
        int maxIndex = playerIndex + islandsAhead;

        // 🔥 SPAWN REQUIRED ISLANDS
        for (int i = minIndex; i <= maxIndex; i++)
        {
            if (!spawned.ContainsKey(i))
            {
                SpawnIsland(i);
            }
        }

        // 🔥 CLEANUP OLD ISLANDS
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

    // ---------------- SPAWN ----------------
void SpawnIsland(int index)
{
    // 🔥 Prevent duplicate spawn by index (extra safety)
    if (spawned.ContainsKey(index))
        return;

    GameObject prefab = islandPrefabs[Random.Range(0, islandPrefabs.Length)];

    Renderer rend = prefab.GetComponentInChildren<Renderer>();
    if (rend == null)
    {
        Debug.LogError("Prefab missing Renderer!");
        return;
    }

    Vector3 size = rend.bounds.size;

    float width = size.x;
    float height = size.y;

    float spawnX = index * blockWidth;

    // 🔥 EXTRA SAFETY: check if something already exists at this position
    float checkRadius = width * 0.45f;

    Collider[] hits = Physics.OverlapSphere(
        new Vector3(spawnX, goldenLine.position.y, goldenLine.position.z),
        checkRadius
    );

    foreach (var hit in hits)
    {
        if (hit.gameObject != null && hit.gameObject != this.gameObject)
        {
            // 🔥 Something already exists here → skip spawn
            return;
        }
    }

    // 🔥 Y alignment (unchanged)
    float spawnY = goldenLine.position.y - (height / 2f);

    Vector3 spawnPos = new Vector3(spawnX, spawnY, goldenLine.position.z);

    GameObject island = Instantiate(prefab, spawnPos, Quaternion.identity);

    spawned.Add(index, island);
}
    // ---------------- INIT ----------------

    void CalculateBlockWidth()
    {
        if (islandPrefabs.Length == 0) return;

        Renderer r = islandPrefabs[0].GetComponentInChildren<Renderer>();
        blockWidth = r.bounds.size.x + extraGap;
    }
}