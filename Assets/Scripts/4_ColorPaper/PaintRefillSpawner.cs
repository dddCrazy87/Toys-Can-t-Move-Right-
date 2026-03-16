using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 顏料補充道具生成器
/// </summary>
public class PaintRefillSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [SerializeField] private GameObject paintRefillPrefab;
    [SerializeField] private int maxPaintRefills = 2;

    [Header("生成區域（矩形範圍）")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(10f, 10f);
    [SerializeField] private float spawnHeight = 0f;

    [Header("時間設定（隨機間隔）")]
    [SerializeField] private float minSpawnInterval = 15f;
    [SerializeField] private float maxSpawnInterval = 25f;
    [SerializeField] private float initialDelay = 10f;
    [SerializeField] private float respawnDelay = 5f;

    [Header("除錯")]
    [SerializeField] private bool showSpawnArea = true;

    private List<GameObject> spawnedRefills = new List<GameObject>();
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
            if (GetCurrentCount() < maxPaintRefills)
            {
                SpawnAtRandomPosition();
            }

            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnAtRandomPosition()
    {
        if (paintRefillPrefab == null) return;

        float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float randomZ = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, randomZ);

        GameObject refill = Instantiate(paintRefillPrefab, spawnPosition, Quaternion.identity);

        PaintRefillItem item = refill.GetComponent<PaintRefillItem>();
        if (item != null)
        {
            item.SetSpawnPointIndex(spawnedRefills.Count);
        }

        spawnedRefills.Add(refill);
        Debug.Log($"[PaintRefillSpawner] 在 {spawnPosition} 生成顏料補充道具");
    }

    public void OnPaintRefillCollected(int spawnPointIndex)
    {
        CleanupNullReferences();
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (isSpawning && GetCurrentCount() < maxPaintRefills)
        {
            SpawnAtRandomPosition();
        }
    }

    int GetCurrentCount()
    {
        CleanupNullReferences();
        return spawnedRefills.Count;
    }

    void CleanupNullReferences()
    {
        spawnedRefills.RemoveAll(item => item == null);
    }

    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    public void ClearAll()
    {
        foreach (var refill in spawnedRefills)
        {
            if (refill != null) Destroy(refill);
        }
        spawnedRefills.Clear();
    }

    void OnDrawGizmos()
    {
        if (!showSpawnArea) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);  // 綠色

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
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}
