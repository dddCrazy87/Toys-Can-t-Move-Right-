using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;

    [Header("每顆 Item 後方 anchor 的距離")]
    public float anchorDistance = 1.2f;

    [Header("偷取成功或失敗的提示字")]
    public GameObject missCanvas, stealCanvas;
    private Dictionary<Transform, List<Transform>> chains = new();

    private void Awake()
    {
        Instance = this;
    }

    // 取得玩家的道具串
    public List<Transform> GetChain(Transform player)
    {
        if (!chains.TryGetValue(player, out var list))
        {
            list = new List<Transform>();
            chains[player] = list;
        }
        return list;
    }

    // 玩家要求撿
    public void RequestCollect(Transform player, Transform item, float stealSkill)
    {
        ItemData data = item.GetComponent<ItemData>();
        if (data == null || data.isBusy) return;

        data.isBusy = true;
        StartCoroutine(CollectRoutine(player, item, data, stealSkill));
    }

    private IEnumerator CollectRoutine(Transform player, Transform item, ItemData data, float stealSkill)
    {
        yield return null; // 避免同一幀觸發兩次

        if (data.owner == null)
        {
            CollectFree(player, item, data);
        }
        else if (data.owner != player)
        {
            float dw = data.owner.GetComponent<ToyBoxPlayer>().defenceWeakness;
            if (Random.value < GetStealProbability(stealSkill, dw))
            {
                TrySteal(player, item, data);
                Instantiate(stealCanvas, item.position + new Vector3(0f, 2f, 0f), Quaternion.Euler(90, 0, 0));
            }
            else
            {
                Instantiate(missCanvas, item.position + new Vector3(0f, 2f, 0f), Quaternion.Euler(90, 0, 0));
            }
        }

        data.isBusy = false;
    }

    float GetStealProbability(float stealSkill, float defenceWeakness)
    {
        stealSkill = Mathf.Clamp(stealSkill, 1f, 10f);
        defenceWeakness = Mathf.Clamp(defenceWeakness, 1f, 10f);
        float sum = stealSkill + defenceWeakness;
        float t = (sum - 11f) / 9f;
        float k = 3f;
        float pRaw = 1f / (1f + Mathf.Exp(-k * t));
        float p = 0.15f + 0.7f * pRaw;
        p = Mathf.Clamp(p, 0.15f, 0.85f);
        return p;
    }



    // 取得後方 FollowAnchor（不存在就建立）
    private Transform GetAnchor(Transform item)
    {
        Transform anchor = item.Find("FollowAnchor");

        if (anchor == null)
        {
            GameObject go = new GameObject("FollowAnchor");
            anchor = go.transform;
            anchor.SetParent(item);
            anchor.localPosition = new Vector3(0, 0, -anchorDistance);
            anchor.localRotation = Quaternion.identity;
        }

        return anchor;
    }

    // 撿取自由道具
    private void CollectFree(Transform player, Transform item, ItemData data)
    {
        List<Transform> chain = GetChain(player);
        PlayerController pc = player.GetComponent<PlayerController>();

        data.owner = player;
        data.index = chain.Count;

        var follow = item.GetComponent<ItemFollow>();
        if (!follow) follow = item.gameObject.AddComponent<ItemFollow>();

        // 決定要跟誰
        follow.follow = chain.Count == 0 ? player.GetComponent<ToyBoxPlayer>().followPoint : GetAnchor(chain[^1]);

        chain.Add(item);

        // 上色
        ItemController ic = item.GetComponent<ItemController>();
        if (ic) ic.ChangeColor(pc.playerColor);

        FindFirstObjectByType<GameSoundEffect>()?.PlayGetItemSound();
    }

    // 搶奪
    private void TrySteal(Transform stealer, Transform hitItem, ItemData hitData)
    {
        if (hitData.owner == stealer) return;

        ToyBoxPlayer victimTB = hitData.owner.GetComponent<ToyBoxPlayer>();
        if (victimTB.isImmuneStolen) return;

        victimTB.ActivateImmunityStolen();

        List<Transform> victimChain = GetChain(hitData.owner);
        List<Transform> stealerChain = GetChain(stealer);
        PlayerController stealerPC = stealer.GetComponent<PlayerController>();

        int startIndex = victimChain.IndexOf(hitItem);
        if (startIndex < 0) return;

        int count = victimChain.Count - startIndex;

        List<Transform> stolen = victimChain.GetRange(startIndex, count);
        victimChain.RemoveRange(startIndex, count);

        // 更新 stolen 每顆 item
        for (int i = 0; i < stolen.Count; i++)
        {
            Transform t = stolen[i];
            ItemData d = t.GetComponent<ItemData>();

            d.owner = stealer;
            d.index = stealerChain.Count + i;

            ItemFollow f = t.GetComponent<ItemFollow>();
            if (!f) f = t.gameObject.AddComponent<ItemFollow>();

            // follow 接法
            Transform preceding = (i == 0 && stealerChain.Count > 0)
                ? stealerChain[^1]
                : (i > 0 ? stolen[i - 1] : null);

            if (preceding != null)
                f.follow = GetAnchor(preceding);
            else
                f.follow = stealer.GetComponent<ToyBoxPlayer>().followPoint;

            // 上色
            ItemController ic = t.GetComponent<ItemController>();
            if (ic) ic.ChangeColor(stealerPC.playerColor);
        }

        stealerChain.AddRange(stolen);
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
