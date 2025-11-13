using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("道具設定")]
    public GameObject[] itemPrefabs;   // 可用的道具預製體
    public int maxItems = 5;           // 場上最多幾顆
    public Transform[] spawnPoints;    // 固定生成點

    // 每個點位目前生成的道具
    private readonly Dictionary<int, GameObject> spawnedItems = new();

    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogError("沒有設置生成點");

        if (itemPrefabs == null || itemPrefabs.Length == 0)
            Debug.LogError("沒有設置道具預製體");
    }

    public void StartSpawnItems()
    {
        ClearAllItems();

        int spawnCount = Mathf.Min(maxItems, spawnPoints.Length);

        List<int> availableIndices = new();
        for (int i = 0; i < spawnPoints.Length; i++)
            availableIndices.Add(i);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availableIndices.Count == 0) break;

            int randomIndex = Random.Range(0, availableIndices.Count);
            int pointIndex = availableIndices[randomIndex];
            availableIndices.RemoveAt(randomIndex);

            SpawnItemAtPoint(pointIndex);
        }
    }

    private void SpawnItemAtPoint(int pointIndex)
    {
        if (pointIndex < 0 || pointIndex >= spawnPoints.Length) return;

        // 如果該點已經有舊道具 → 先移除
        if (spawnedItems.ContainsKey(pointIndex))
        {
            if (spawnedItems[pointIndex] != null)
                Destroy(spawnedItems[pointIndex]);

            spawnedItems.Remove(pointIndex);
        }

        GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        GameObject item = Instantiate(prefab);

        // ⭕ 正確做法：指定位置，而不是 +=
        item.transform.position = spawnPoints[pointIndex].position;
        item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // 確保有 ItemFollow & ItemData
        if (!item.TryGetComponent<ItemFollow>(out _))
            item.AddComponent<ItemFollow>();

        ItemData data = item.GetComponent<ItemData>();
        if (data == null) data = item.AddComponent<ItemData>();

        data.owner = null;
        data.index = -1;
        data.isBusy = false;
        data.spawnPointIndex = pointIndex;

        spawnedItems[pointIndex] = item;

        item.GetComponent<ItemController>().SetRandomColor();
    }

    public void ItemCollected(GameObject item)
    {
        ItemData data = item.GetComponent<ItemData>();
        if (data == null)
        {
            Debug.LogWarning("ItemCollected 收到沒有 ItemData 的物件", item);
            return;
        }

        int pointIndex = data.spawnPointIndex;

        if (spawnedItems.ContainsKey(pointIndex))
        {
            spawnedItems.Remove(pointIndex);
        }

        // 過一段時間在空點再生一顆
        Invoke(nameof(SpawnItemAtRandomPoint), 1.5f);
    }

    private void SpawnItemAtRandomPoint()
    {
        List<int> unusedIndices = new();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!spawnedItems.ContainsKey(i))
                unusedIndices.Add(i);
        }

        if (unusedIndices.Count > 0)
        {
            int randomIndex = Random.Range(0, unusedIndices.Count);
            SpawnItemAtPoint(unusedIndices[randomIndex]);
        }
        else
        {
            Debug.Log("所有點位都已使用");
        }
    }

    public void ClearAllItems()
    {
        foreach (GameObject item in spawnedItems.Values)
        {
            if (item != null) Destroy(item);
        }
        spawnedItems.Clear();
    }

    private void OnDestroy()
    {
        ClearAllItems();
    }
}
