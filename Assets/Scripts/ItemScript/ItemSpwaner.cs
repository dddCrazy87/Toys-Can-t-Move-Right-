using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public GameObject[] itemPrefabs;
    public Transform[] spawnPoints;
    public int maxItems = 5;

    private Dictionary<int, GameObject> spawnedItems = new();

    public void StartSpawnItems()
    {
        ClearAllItems();

        int count = Mathf.Min(maxItems, spawnPoints.Length);

        List<int> indices = new();
        for (int i = 0; i < spawnPoints.Length; i++)
            indices.Add(i);

        for (int i = 0; i < count; i++)
        {
            int r = Random.Range(0, indices.Count);
            int point = indices[r];
            indices.RemoveAt(r);

            SpawnItem(point);
        }
    }

    private void SpawnItem(int pointIndex)
    {
        if (spawnedItems.ContainsKey(pointIndex))
        {
            Destroy(spawnedItems[pointIndex]);
            spawnedItems.Remove(pointIndex);
        }

        GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];

        GameObject item = Instantiate(
            prefab,
            spawnPoints[pointIndex].position,
            Quaternion.identity
        );

        ItemData data = item.GetComponent<ItemData>();
        if (!data) data = item.AddComponent<ItemData>();

        data.spawnPointIndex = pointIndex;
        data.owner = null;
        data.index = -1;
        data.isBusy = true; // 保護期（0.2s不被撿）

        // 確保 Follow 不為 null（避免吸到中心）
        ItemFollow f = item.GetComponent<ItemFollow>();
        if (!f) f = item.AddComponent<ItemFollow>();
        f.follow = item.transform;

        // 顏色預設（中立）
        ItemController ic = item.GetComponent<ItemController>();
        if (ic) ic.SetRandomColor();

        spawnedItems[pointIndex] = item;

        StartCoroutine(SpawnProtection(data));
    }

    private IEnumerator SpawnProtection(ItemData data)
    {
        yield return new WaitForSeconds(0.2f);
        data.isBusy = false;
    }

    public void ItemCollected(GameObject item)
    {
        ItemData data = item.GetComponent<ItemData>();
        if (spawnedItems.ContainsKey(data.spawnPointIndex))
            spawnedItems.Remove(data.spawnPointIndex);

        Destroy(item);

        Invoke(nameof(SpawnRandomPoint), 1.5f);
    }

    private void SpawnRandomPoint()
    {
        List<int> unused = new();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!spawnedItems.ContainsKey(i))
                unused.Add(i);
        }

        if (unused.Count > 0)
        {
            int idx = unused[Random.Range(0, unused.Count)];
            SpawnItem(idx);
        }
    }

    public void ClearAllItems()
    {
        foreach (var kv in spawnedItems)
        {
            if (kv.Value) Destroy(kv.Value);
        }
        spawnedItems.Clear();
    }
}
