using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 白色水球生成器
/// </summary>
public class WhiteBallSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [SerializeField] private GameObject whiteBallPrefab;
    [SerializeField] private int maxWhiteBalls = 1;

    [Header("生成區域（矩形範圍）")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(10f, 10f);
    [SerializeField] private float spawnHeight = 0f;

    [Header("時間設定（隨機間隔）")]
    [SerializeField] private float minSpawnInterval = 20f;
    [SerializeField] private float maxSpawnInterval = 35f;
    [SerializeField] private float initialDelay = 15f;
    [SerializeField] private float respawnDelay = 8f;

    [Header("除錯")]
    [SerializeField] private bool showSpawnArea = true;

    private List<GameObject> spawnedBalls = new List<GameObject>();
    private bool isSpawning = false;

    void Start()
    {
        StartCoroutine(StartSpawningAfterDelay());
    }

    IEnumerator StartSpawningAfterDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        isSpawning = true;
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            if (GetCurrentCount() < maxWhiteBalls)
            {
                SpawnAtRandomPosition();
            }

            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnAtRandomPosition()
    {
        if (whiteBallPrefab == null) return;

        float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float randomZ = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, randomZ);

        GameObject ball = Instantiate(whiteBallPrefab, spawnPosition, Quaternion.identity);

        WhiteBallItem item = ball.GetComponent<WhiteBallItem>();
        if (item != null)
        {
            item.SetSpawnPointIndex(spawnedBalls.Count);
        }

        spawnedBalls.Add(ball);
        Debug.Log($"[WhiteBallSpawner] 在 {spawnPosition} 生成白色水球");
    }

    public void OnWhiteBallCollected(int spawnPointIndex)
    {
        CleanupNullReferences();
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (isSpawning && GetCurrentCount() < maxWhiteBalls)
        {
            SpawnAtRandomPosition();
        }
    }

    int GetCurrentCount()
    {
        CleanupNullReferences();
        return spawnedBalls.Count;
    }

    void CleanupNullReferences()
    {
        spawnedBalls.RemoveAll(item => item == null);
    }

    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    public void ClearAll()
    {
        foreach (var ball in spawnedBalls)
        {
            if (ball != null) Destroy(ball);
        }
        spawnedBalls.Clear();
    }

    void OnDrawGizmos()
    {
        if (!showSpawnArea) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);  // 白色

        Vector3 center = new Vector3(
            (spawnAreaMin.x + spawnAreaMax.x) / 2f,
            spawnHeight,
            (spawnAreaMin.y + spawnAreaMax.y) / 2f
        );

        Vector3 size = new Vector3(
            spawnAreaMax.x - spawnAreaMin.x,
            0.5f,
            spawnAreaMax.y - spawnAreaMin.y
        );

        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, size);
    }
}
