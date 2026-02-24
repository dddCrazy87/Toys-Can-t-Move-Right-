using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaintCanSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [SerializeField] private GameObject paintCanPrefab;
    [SerializeField] private int maxPaintCans = 2;

    [Header("生成區域（矩形範圍）")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-10f, -10f);  // X, Z 最小值
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(10f, 10f);    // X, Z 最大值
    [SerializeField] private float spawnHeight = 0f;  // Y 高度

    [Header("時間設定（隨機間隔）")]
    [SerializeField] private float minSpawnInterval = 8f;
    [SerializeField] private float maxSpawnInterval = 15f;
    [SerializeField] private float initialDelay = 5f;
    [SerializeField] private float respawnDelay = 3f;

    [Header("除錯")]
    [SerializeField] private bool showSpawnArea = true;  // 在 Scene 視窗顯示生成區域

    private List<GameObject> spawnedCans = new List<GameObject>();
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
            if (GetCurrentPaintCanCount() < maxPaintCans)
            {
                SpawnAtRandomPosition();
            }

            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnAtRandomPosition()
    {
        if (paintCanPrefab == null) return;

        // 在區域內隨機生成位置
        float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float randomZ = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, randomZ);

        GameObject paintCan = Instantiate(paintCanPrefab, spawnPosition, Quaternion.identity);

        PaintCanItem item = paintCan.GetComponent<PaintCanItem>();
        if (item != null)
        {
            item.SetSpawnPointIndex(spawnedCans.Count);
        }

        spawnedCans.Add(paintCan);
        Debug.Log($"[PaintCanSpawner] 在 {spawnPosition} 生成顏料罐");
    }

    public void OnPaintCanCollected(int spawnPointIndex)
    {
        // 清理列表中的空引用
        CleanupNullReferences();

        // 延遲後嘗試生成新的
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (isSpawning && GetCurrentPaintCanCount() < maxPaintCans)
        {
            SpawnAtRandomPosition();
        }
    }

    int GetCurrentPaintCanCount()
    {
        CleanupNullReferences();
        return spawnedCans.Count;
    }

    void CleanupNullReferences()
    {
        spawnedCans.RemoveAll(item => item == null);
    }

    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    public void ClearAllPaintCans()
    {
        foreach (var paintCan in spawnedCans)
        {
            if (paintCan != null)
            {
                Destroy(paintCan);
            }
        }
        spawnedCans.Clear();
    }

    // 在 Scene 視窗顯示生成區域（方便調整）
    void OnDrawGizmos()
    {
        if (!showSpawnArea) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

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

        // 畫邊框
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}
