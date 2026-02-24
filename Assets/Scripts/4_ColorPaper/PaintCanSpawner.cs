using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaintCanSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [SerializeField] private GameObject paintCanPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int maxPaintCans = 2;

    [Header("時間設定（隨機間隔）")]
    [SerializeField] private float minSpawnInterval = 8f;
    [SerializeField] private float maxSpawnInterval = 15f;
    [SerializeField] private float initialDelay = 5f;
    [SerializeField] private float respawnDelay = 3f;

    private Dictionary<int, GameObject> spawnedCans = new Dictionary<int, GameObject>();
    private bool isSpawning = false;

    void Start()
    {
        // 遊戲開始後延遲啟動生成
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
            // 檢查是否可以生成
            if (GetCurrentPaintCanCount() < maxPaintCans)
            {
                SpawnRandomPaintCan();
            }

            // 隨機等待時間
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnRandomPaintCan()
    {
        if (paintCanPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        // 找出可用的生成點
        List<int> availablePoints = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!spawnedCans.ContainsKey(i) || spawnedCans[i] == null)
            {
                availablePoints.Add(i);
            }
        }

        if (availablePoints.Count == 0) return;

        // 隨機選擇一個生成點
        int randomIndex = availablePoints[Random.Range(0, availablePoints.Count)];
        SpawnAtPoint(randomIndex);
    }

    void SpawnAtPoint(int pointIndex)
    {
        if (pointIndex < 0 || pointIndex >= spawnPoints.Length) return;

        Transform spawnPoint = spawnPoints[pointIndex];
        GameObject paintCan = Instantiate(paintCanPrefab, spawnPoint.position, Quaternion.identity);

        PaintCanItem item = paintCan.GetComponent<PaintCanItem>();
        if (item != null)
        {
            item.SetSpawnPointIndex(pointIndex);
        }

        spawnedCans[pointIndex] = paintCan;
        Debug.Log($"[PaintCanSpawner] 在生成點 {pointIndex} 生成顏料罐");
    }

    public void OnPaintCanCollected(int spawnPointIndex)
    {
        // 移除追蹤
        if (spawnedCans.ContainsKey(spawnPointIndex))
        {
            spawnedCans.Remove(spawnPointIndex);
        }

        // 延遲後嘗試生成新的
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (isSpawning && GetCurrentPaintCanCount() < maxPaintCans)
        {
            SpawnRandomPaintCan();
        }
    }

    int GetCurrentPaintCanCount()
    {
        // 清理已被銷毀的引用
        List<int> toRemove = new List<int>();
        foreach (var kvp in spawnedCans)
        {
            if (kvp.Value == null)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (int key in toRemove)
        {
            spawnedCans.Remove(key);
        }

        return spawnedCans.Count;
    }

    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    public void ClearAllPaintCans()
    {
        foreach (var kvp in spawnedCans)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        spawnedCans.Clear();
    }
}
