using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    // 道具的預製體
    public GameObject[] itemPrefabs;
    // 最大同時存在的道具數量
    public int maxItems = 5;
    // 預先設定好的出生點
    public Transform[] spawnPoints;
    
    // 儲存每個點位上的道具
    private Dictionary<int, GameObject> spawnedItems = new Dictionary<int, GameObject>();
    
    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("沒有設置生成點!");
        }
        
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogError("沒有設置道具預製體!");
        }
    }
    
    public void StartSpawnItems()
    {
        ClearAllItems();
        
        // 計算可生成的最大道具數
        int spawnCount = Mathf.Min(maxItems, spawnPoints.Length);
        
        // 建立一個包含所有可用點位索引的列表
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            availableIndices.Add(i);
        }
        
        // 隨機選擇點位生成道具
        for (int i = 0; i < spawnCount; i++)
        {
            if (availableIndices.Count == 0)
                break;
                
            // 隨機選擇一個索引
            int randomIndex = Random.Range(0, availableIndices.Count);
            int pointIndex = availableIndices[randomIndex];
            
            // 從可用列表中移除該索引
            availableIndices.RemoveAt(randomIndex);
            
            // 在該點位生成道具
            SpawnItemAtPoint(pointIndex);
        }
    }
    
    private void SpawnItemAtPoint(int pointIndex)
    {
        if (pointIndex < 0 || pointIndex >= spawnPoints.Length)
            return;
            
        if (spawnedItems.ContainsKey(pointIndex))
        {
            // 如果該點位已有道具，先移除
            Destroy(spawnedItems[pointIndex]);
            spawnedItems.Remove(pointIndex);
        }
        
        // 隨機選擇一個道具預製體
        GameObject itemPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        
        // 在指定點位生成道具
        Transform spawnPoint = spawnPoints[pointIndex];
        float randomYRotation = UnityEngine.Random.Range(0f, 360f);
        Quaternion randomRotation = Quaternion.Euler(0f, randomYRotation, 0f);
        GameObject item = Instantiate(itemPrefab, spawnPoint.position, randomRotation);
        
        // 記錄該道具對應的點位索引
        spawnedItems[pointIndex] = item;
        
        // 將點位索引保存在道具的 component 中
        ItemData itemData = item.AddComponent<ItemData>();
        itemData.spawnPointIndex = pointIndex;
    }
    
    public void ItemCollected(GameObject item)
    {
        // 從道具獲取點位索引
        ItemData itemData = item.GetComponent<ItemData>();
        if (itemData == null) {
            Debug.LogWarning("收集的道具沒有 ItemData 組件");
            return;
        }
        
        int pointIndex = itemData.spawnPointIndex;
        
        // 移除對應的記錄
        if (spawnedItems.ContainsKey(pointIndex)) {
            spawnedItems.Remove(pointIndex);
        }
        
        // 銷毀道具
        // Destroy(item);
        
        // 在一個新的隨機點位生成道具
        SpawnItemAtRandomPoint();
    }
    
    private void SpawnItemAtRandomPoint()
    {
        // 找出所有未使用的點位
        List<int> unusedIndices = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!spawnedItems.ContainsKey(i))
            {
                unusedIndices.Add(i);
            }
        }
        
        // 如果有未使用的點位，選一個生成道具
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
            if (item != null)
            {
                Destroy(item);
            }
        }
        
        spawnedItems.Clear();
    }
    
    private void OnDestroy()
    {
        ClearAllItems();
    }
}

// 用於保存道具對應的點位索引
public class ItemData : MonoBehaviour
{
    public int spawnPointIndex;
}