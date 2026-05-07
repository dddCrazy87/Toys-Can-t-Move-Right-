using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance;

    [Header("基本道具設定")]
    public GameObject[] basicItemPrefabs;

    [Header("特殊道具設定")]
    public GameObject[] specialItemPrefabs;
    [Tooltip("每生成 X 個基本道具 (最小值, 最大值)")]
    public Vector2Int xRange = new(5, 10);
    [Tooltip("必定出現 Y 個特殊道具 (最小值, 最大值)")]
    public Vector2Int yRange = new(1, 2);

    [Header("生成點設定")]
    public Transform[] spawnPoints;
    public int maxItems = 5;

    private Dictionary<int, GameObject> spawnedItems = new();

    // ==========================================
    // 計數器狀態
    // ==========================================
    private int targetX;
    private int targetY;
    private int currentBasicCount = 0;
    private int currentSpecialCount = 0;

    // 是否進入特殊道具生成階段
    private bool isSpawningSpecialPhase = false;

    private void Awake()
    {
        Instance = this;

        // 遊戲開始時先抽出第一輪的 X 跟 Y
        GenerateNextXY();
    }

    // 重新抽籤決定下一輪的目標數量，並重置計數
    private void GenerateNextXY()
    {
        targetX = Random.Range(xRange.x, xRange.y + 1);
        targetY = Random.Range(yRange.x, yRange.y + 1);
        currentBasicCount = 0;
        currentSpecialCount = 0;
        isSpawningSpecialPhase = false;
    }

    public void StartSpawnItems()
    {
        ClearAllItems();

        // 重新開始生成時也重置規則
        GenerateNextXY();

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

        bool spawnSpecial = false;

        // ==========================================
        // 判斷這次該生基本還是特殊道具
        // ==========================================
        if (isSpawningSpecialPhase)
        {
            spawnSpecial = true;
            currentSpecialCount++;
            if (currentSpecialCount >= targetY)
            {
                // 特殊道具的額度用完了，重置進入下一輪
                GenerateNextXY();
            }
        }
        else
        {
            spawnSpecial = false;
            currentBasicCount++;
            if (currentBasicCount >= targetX)
            {
                // 基本道具達成 X 個，開啟特殊道具階段
                isSpawningSpecialPhase = true;
                currentSpecialCount = 0;
            }
        }

        // 根據判斷結果，隨機挑選對應陣列中的 Prefab
        GameObject prefab = spawnSpecial
            ? specialItemPrefabs[Random.Range(0, specialItemPrefabs.Length)]
            : basicItemPrefabs[Random.Range(0, basicItemPrefabs.Length)];

        GameObject item = Instantiate(
            prefab,
            spawnPoints[pointIndex].position,
            Quaternion.identity
        );

        // ==========================================
        // 分別處理 基本道具、純效果特殊道具、可收集加分道具
        // ==========================================

        // 檢查這個 Prefab 身上有沒有 ItemData
        // (基本道具我們用 AddComponent 動態加，可收集特殊道具我們事先在 Prefab 裡掛好)
        ItemData data = item.GetComponent<ItemData>();

        // 如果是基本道具 (!spawnSpecial) 或是 帶有 ItemData 的可收集特殊道具 (data != null)
        if (!spawnSpecial || data != null)
        {
            if (!data) data = item.AddComponent<ItemData>();

            data.spawnPointIndex = pointIndex;
            data.owner = null;
            data.index = -1;
            data.isBusy = true; // 保護期

            ItemFollow f = item.GetComponent<ItemFollow>();
            if (!f) f = item.AddComponent<ItemFollow>();
            f.follow = item.transform;

            ItemController ic = item.GetComponent<ItemController>();
            if (!ic) ic = item.AddComponent<ItemController>();

            // 視覺區分：只有基本道具 (extraScore == 0) 才隨機給中立色
            // 加分道具建議你在 Prefab 就設好一個特別的顏色 (例如金色/發光)，讓玩家一眼認出
            if (data.extraScore == 0)
            {
                ic.SetRandomColor();
            }

            StartCoroutine(SpawnProtection(data));
        }
        else
        {
            // 這裡保留給「碰到就有效果、不可收集」的純特殊道具
            // 什麼都不用加
        }

        spawnedItems[pointIndex] = item;
    }

    private IEnumerator SpawnProtection(ItemData data)
    {
        yield return new WaitForSeconds(0.2f);
        if (data != null) data.isBusy = false;
    }

    public void ItemCollected(GameObject item)
    {
        // 為了相容沒有 ItemData 的特殊道具，我們直接遍歷 Dictionary 來反查這個物件佔用的是哪個生成點
        int foundIndex = -1;
        foreach (var kv in spawnedItems)
        {
            if (kv.Value == item)
            {
                foundIndex = kv.Key;
                break;
            }
        }

        if (foundIndex != -1)
        {
            spawnedItems.Remove(foundIndex);
        }

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


