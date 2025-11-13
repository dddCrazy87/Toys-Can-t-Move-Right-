using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("每個道具後方跟隨點和本體的距離")]
    public float itemAnchorDistance = 1.2f;

    private readonly Dictionary<Transform, List<Transform>> chains = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 取得某個玩家的道具串
    public List<Transform> GetChain(Transform player)
    {
        if (!chains.TryGetValue(player, out var list))
        {
            list = new List<Transform>();
            chains[player] = list;
        }
        return list;
    }

    // 玩家要求撿/搶一顆道具
    public void RequestCollect(Transform player, Transform item)
    {
        ItemData data = item.GetComponent<ItemData>();
        if (data == null)
        {
            Debug.LogWarning("嘗試撿取的物件沒有 ItemData", item);
            return;
        }

        if (data.isBusy) return;

        data.isBusy = true;
        StartCoroutine(CollectRoutine(player, item, data));
    }

    private IEnumerator CollectRoutine(Transform player, Transform item, ItemData data)
    {
        yield return null; // 避免同幀多玩家同時撞到

        if (data.owner == null)
        {
            CollectFreeItem(player, item, data);
        }
        else
        {
            TrySteal(player, item, data);
        }

        data.isBusy = false;
    }

    // 取得某顆 item 的後方 anchor（不存在就建立一個）
    private Transform GetItemFollowAnchor(Transform item)
    {
        Transform anchor = item.Find("FollowAnchor");
        if (anchor == null)
        {
            GameObject go = new GameObject("FollowAnchor");
            anchor = go.transform;
            anchor.SetParent(item);
            anchor.localPosition = new Vector3(0f, 0f, -itemAnchorDistance);
            anchor.localRotation = Quaternion.identity;
        }
        return anchor;
    }

    // 自由道具 → 撿起來
    private void CollectFreeItem(Transform player, Transform item, ItemData data)
    {
        List<Transform> chain = GetChain(player);

        data.owner = player;
        data.index = chain.Count;

        ItemFollow follow = item.GetComponent<ItemFollow>();
        if (follow == null)
            follow = item.gameObject.AddComponent<ItemFollow>();

        PlayerController pc = player.GetComponent<PlayerController>();
        Transform followTarget;

        if (chain.Count == 0)
        {
            // 第一顆 item 跟玩家的 followPoint
            followTarget = pc.followPoint;
        }
        else
        {
            // 之後的 item 跟前一顆 item 的 FollowAnchor
            Transform previousItem = chain[^1];
            followTarget = GetItemFollowAnchor(previousItem);
        }

        follow.follow = followTarget;

        chain.Add(item);

        FindFirstObjectByType<GameSoundEffect>()?.PlayGetItemSound();
        item.GetComponent<ItemController>()?.ChangeColor(pc.playerColor);
    }

    // 搶奪
    private void TrySteal(Transform stealer, Transform hitItem, ItemData hitItemData)
    {
        // 已經是自己了就不用處理
        if (hitItemData.owner == stealer) return;

        PlayerController victimPc = hitItemData.owner.GetComponent<PlayerController>();
        if (victimPc == null) return;

        // 被搶方免疫中 → 搶不到
        if (victimPc.isImmuneStolen) return;

        // 啟動免疫
        victimPc.ActivateImmunityStolen();

        List<Transform> victimChain = GetChain(hitItemData.owner);
        List<Transform> stealerChain = GetChain(stealer);
        PlayerController stealerPc = stealer.GetComponent<PlayerController>();

        if (victimChain.Count == 0)
            return;

        // 以實際的 item 在 victimChain 中的位置為準，比 data.index 靠譜
        int startIndex = victimChain.IndexOf(hitItem);
        if (startIndex < 0 || startIndex >= victimChain.Count)
            return;

        int count = victimChain.Count - startIndex;
        if (count <= 0)
            return;

        // 切出被搶走的那一段
        List<Transform> stolen = victimChain.GetRange(startIndex, count);
        victimChain.RemoveRange(startIndex, count);

        // 更新被搶那串的 owner / index / follow
        for (int i = 0; i < stolen.Count; i++)
        {
            Transform t = stolen[i];
            ItemData d = t.GetComponent<ItemData>();

            d.owner = stealer;
            d.index = stealerChain.Count + i;

            ItemFollow f = t.GetComponent<ItemFollow>();
            if (f == null) f = t.gameObject.AddComponent<ItemFollow>();

            // 決定這顆 item 要跟誰
            Transform followTarget;

            if (stealerChain.Count == 0 && i == 0)
            {
                // 搶之前 stealer 沒道具，這是第一顆 → 跟玩家 followPoint
                followTarget = stealerPc.followPoint;
            }
            else
            {
                // 有「前面那顆 item」存在：
                // i == 0 → 前面是 stealer 原本的最後一顆
                // i > 0 → 前面是 stolen[i-1]
                Transform precedingItem;

                if (i == 0)
                {
                    precedingItem = stealerChain[^1];
                }
                else
                {
                    precedingItem = stolen[i - 1];
                }

                followTarget = GetItemFollowAnchor(precedingItem);
            }

            f.follow = followTarget;
            t.GetComponent<ItemController>().ChangeColor(stealerPc.playerColor);
        }

        stealerChain.AddRange(stolen);

        // 這裡可以放搶奪音效
        FindAnyObjectByType<GameSoundEffect>()?.PlayStealItemSound();
    }

    // （選用）把某玩家全部道具拿出來（例如送到終點時）
    public List<Transform> TakeAllItemsFromPlayer(Transform player)
    {
        List<Transform> chain = GetChain(player);
        List<Transform> result = new(chain);

        foreach (Transform item in result)
        {
            ItemData data = item.GetComponent<ItemData>();
            if (data != null)
            {
                data.owner = null;
                data.index = -1;
            }

            ItemFollow f = item.GetComponent<ItemFollow>();
            if (f != null)
            {
                f.follow = null;
            }
        }

        chain.Clear();
        return result;
    }
}
