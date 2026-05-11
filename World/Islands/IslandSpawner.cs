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
    public GameObject[] gemPrefabs;
    [Range(0f, 1f)] public float gemSpawnChance = 0.4f;
    public int maxGemsPerIsland = 2;
    public float gemHeightOffset = 0.5f;

    [Header("Obstacle Scale")]
    public bool useCustomObstacleScale = false;

    public Vector3 obstacleScale = Vector3.one;

    [Header("Gem Rotation")]
    public Vector3 gemRotationOffset;
    public bool randomizeYRotation = true;
    public bool alignToPathDirection = false;

    // ---------------- OBSTACLES ----------------
    [Header("Obstacles")]
    public GameObject[] obstaclePrefabs;

    [Range(0f, 1f)]
    public float obstacleSpawnChance = 0.7f;

    public int maxObstaclesPerIsland = 3;

    public float obstacleHeightOffset = 0.5f;

    [Header("Obstacle Placement")]
    public float minimumObstacleSpacing = 2f;
    public float minimumGemSpacing = 1.5f;

    [Header("Obstacle Rotation")]
    public Vector3 obstacleRotationOffset;
    public bool randomizeObstacleYRotation = true;

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

        // 🔥 SHARED POSITION CACHE
        List<Vector3> occupiedPositions = new List<Vector3>();

        // 🔥 SPAWN GEMS
        SpawnGems(island, size, occupiedPositions);

        // 🔥 SPAWN OBSTACLES
        SpawnObstacles(island, size, occupiedPositions);

        spawned.Add(index, island);
    }

    // ---------------- BLOCK WIDTH ----------------

    void CalculateBlockWidth()
    {
        if (islandPrefabs.Length == 0) return;

        Renderer r = islandPrefabs[0].GetComponentInChildren<Renderer>();

        blockWidth = r.bounds.size.x + extraGap;
    }

    // ---------------- GEMS ----------------

void SpawnGems(
    GameObject island,
    Vector3 size,
    List<Vector3> occupied
)
{
    if (gemPrefabs == null ||
        gemPrefabs.Length == 0)
        return;

    if (Random.value > gemSpawnChance)
        return;

    int gemCount =
        Random.Range(
            1,
            maxGemsPerIsland + 1
        );

    for (int i = 0; i < gemCount; i++)
    {
        Vector3 spawnPos;

        if (!TryGetValidPosition(
            island,
            size,
            occupied,
            minimumGemSpacing,
            out spawnPos))
            continue;

        // 🔥 RANDOM GEM PREFAB
        GameObject selectedGem =
            gemPrefabs[
                Random.Range(
                    0,
                    gemPrefabs.Length
                )
            ];

        Quaternion rotation =
            Quaternion.Euler(
                gemRotationOffset
            );

        // 🔥 RANDOM Y ROTATION
        if (randomizeYRotation)
        {
            float randomY =
                Random.Range(0f, 360f);

            rotation *=
                Quaternion.Euler(
                    0f,
                    randomY,
                    0f
                );
        }

        // 🔥 ALIGN TO PATH
        if (alignToPathDirection)
        {
            Vector3 forwardDir =
                Vector3.right;

            rotation =
                Quaternion.LookRotation(
                    forwardDir
                ) *
                Quaternion.Euler(
                    gemRotationOffset
                );
        }

        // 🔥 SPAWN GEM
        GameObject spawnedGem =
            Instantiate(
                selectedGem,
                spawnPos,
                rotation,
                island.transform
            );

        // 🔥 PRESERVE PREFAB SCALE
        spawnedGem.transform.localScale =
            selectedGem.transform.localScale;

        occupied.Add(spawnPos);
    }
}
    // ---------------- OBSTACLES ----------------

void SpawnObstacles(
    GameObject island,
    Vector3 size,
    List<Vector3> occupied
)
{
    if (obstaclePrefabs == null ||
        obstaclePrefabs.Length == 0)
        return;

    if (Random.value > obstacleSpawnChance)
        return;

    int obstacleCount =
        Random.Range(
            1,
            maxObstaclesPerIsland + 1
        );

    for (int i = 0; i < obstacleCount; i++)
    {
        Vector3 spawnPos;

        bool foundValid = false;

        int attempts = 20;

        for (int a = 0; a < attempts; a++)
        {
            // 🔥 RANDOM LOCAL POSITION
            float randomX =
                Random.Range(
                    -size.x * 0.4f,
                    size.x * 0.4f
                );

            float randomZ =
                Random.Range(
                    -size.z * 0.4f,
                    size.z * 0.4f
                );

            Vector3 candidatePos =
                island.transform.position +
                new Vector3(
                    randomX,
                    size.y / 2f + obstacleHeightOffset,
                    randomZ
                );

            bool overlaps = false;

            // 🔥 CHECK OTHER OBJECTS
            foreach (Vector3 used in occupied)
            {
                if (
                    Vector3.Distance(
                        candidatePos,
                        used
                    ) < minimumObstacleSpacing
                )
                {
                    overlaps = true;
                    break;
                }
            }

            // 🔥 PREVENT SPAWN UNDER PLAYER
            if (player != null)
            {
                float distanceToPlayer =
                    Vector3.Distance(
                        new Vector3(
                            candidatePos.x,
                            0f,
                            candidatePos.z
                        ),
                        new Vector3(
                            player.position.x,
                            0f,
                            player.position.z
                        )
                    );

                // 🔥 SAFE START AREA
                if (distanceToPlayer < 4f)
                {
                    overlaps = true;
                }
            }

            if (!overlaps)
            {
                spawnPos = candidatePos;
                foundValid = true;
                goto SpawnObstacle;
            }
        }

        continue;

    SpawnObstacle:

        if (!foundValid)
            continue;

        GameObject obstaclePrefab =
            obstaclePrefabs[
                Random.Range(
                    0,
                    obstaclePrefabs.Length
                )
            ];

        Quaternion rotation =
            Quaternion.Euler(
                obstacleRotationOffset
            );

        if (randomizeObstacleYRotation)
        {
            float randomY =
                Random.Range(0f, 360f);

            rotation *=
                Quaternion.Euler(
                    0f,
                    randomY,
                    0f
                );
        }

        // 🔥 SPAWN
        GameObject spawnedObstacle =
            Instantiate(
                obstaclePrefab,
                spawnPos,
                rotation
            );

        // 🔥 PARENT
        spawnedObstacle.transform.SetParent(
            island.transform
        );

        // 🔥 SCALE
        if (useCustomObstacleScale)
        {
            spawnedObstacle.transform.localScale =
                obstacleScale;
        }
        else
        {
            spawnedObstacle.transform.localScale =
                obstaclePrefab.transform.localScale;
        }

        occupied.Add(spawnPos);
    }
}
    // ---------------- VALID POSITION ----------------

    bool TryGetValidPosition(
        GameObject island,
        Vector3 size,
        List<Vector3> occupied,
        float minSpacing,
        out Vector3 validPos)
    {
        int attempts = 20;

        for (int i = 0; i < attempts; i++)
        {
            float randomX =
                Random.Range(-size.x * 0.4f, size.x * 0.4f);

            float randomZ =
                Random.Range(-size.z * 0.4f, size.z * 0.4f);

            Vector3 pos =
                island.transform.position +
                new Vector3(
                    randomX,
                    size.y / 2f + obstacleHeightOffset,
                    randomZ
                );

            bool overlaps = false;

            foreach (Vector3 used in occupied)
            {
                if (Vector3.Distance(pos, used) < minSpacing)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                validPos = pos;
                return true;
            }
        }

        validPos = Vector3.zero;
        return false;
    }
}