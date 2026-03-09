using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EraserSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [SerializeField] private GameObject eraserPrefab;
    [SerializeField] private int maxErasers = 1;

    [Header("生成區域（矩形範圍）")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-10f, -10f);  // X, Z 最小值
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(10f, 10f);    // X, Z 最大值
    [SerializeField] private float spawnHeight = 0f;  // Y 高度

    [Header("時間設定（隨機間隔）")]
    [SerializeField] private float minSpawnInterval = 15f;
    [SerializeField] private float maxSpawnInterval = 25f;
    [SerializeField] private float initialDelay = 10f;
    [SerializeField] private float respawnDelay = 5f;

    [Header("除錯")]
    [SerializeField] private bool showSpawnArea = true;  // 在 Scene 視窗顯示生成區域

    private List<GameObject> spawnedErasers = new List<GameObject>();
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
            if (GetCurrentEraserCount() < maxErasers)
            {
                SpawnAtRandomPosition();
            }

            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnAtRandomPosition()
    {
        if (eraserPrefab == null) return;

        // 在區域內隨機生成位置
        float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float randomZ = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, randomZ);

        GameObject eraser = Instantiate(eraserPrefab, spawnPosition, Quaternion.identity);

        EraserItem item = eraser.GetComponent<EraserItem>();
        if (item != null)
        {
            item.SetSpawnPointIndex(spawnedErasers.Count);
        }

        spawnedErasers.Add(eraser);
        Debug.Log($"[EraserSpawner] 在 {spawnPosition} 生成橡皮擦");
    }

    public void OnEraserCollected(int spawnPointIndex)
    {
        // 清理列表中的空引用
        CleanupNullReferences();

        // 延遲後嘗試生成新的
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (isSpawning && GetCurrentEraserCount() < maxErasers)
        {
            SpawnAtRandomPosition();
        }
    }

    int GetCurrentEraserCount()
    {
        CleanupNullReferences();
        return spawnedErasers.Count;
    }

    void CleanupNullReferences()
    {
        spawnedErasers.RemoveAll(item => item == null);
    }

    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    public void ClearAllErasers()
    {
        foreach (var eraser in spawnedErasers)
        {
            if (eraser != null)
            {
                Destroy(eraser);
            }
        }
        spawnedErasers.Clear();
    }

    // 在 Scene 視窗顯示生成區域（方便調整）
    void OnDrawGizmos()
    {
        if (!showSpawnArea) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);  // 橘色

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
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireCube(center, size);
    }
}
