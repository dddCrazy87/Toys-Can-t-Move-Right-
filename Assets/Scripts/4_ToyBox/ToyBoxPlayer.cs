using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToyBoxPlayer : MonoBehaviour
{
    private PlayerController playerController;

    [Header("ToyBox 專屬數值")]
    [Tooltip("1~10")] public int stealSkill = 5;
    [Tooltip("1~10")] public int defenceWeakness = 5;

    [Header("被搶奪後的免疫時間")]
    public float immunityStolenDuration = 0.3f;
    [HideInInspector] public bool isImmuneStolen = false;
    private Coroutine immunityRoutine;

    [Header("道具跟隨點")]
    public Transform followPoint;

    [Header("目前持有的特殊道具")]
    public SpecialItemType currentSpecialItem = SpecialItemType.None;
    [Header("冰凍持續時間")]
    public float freezeDuration = 2f;

    public void Initialize()
    {
        playerController = GetComponent<PlayerController>();
        // 嘗試自動尋找原本掛在 Player 上的 FollowPoint
        if (followPoint == null)
        {
            followPoint = transform.Find("follow point");
        }

        if (playerController != null)
        {
            playerController.OnPressStateChanged += HandlePressState;
        }
    }

    private void OnDestroy()
    {
        // ++ 新增：腳本銷毀時務必解除訂閱，避免記憶體流失 (Memory Leak) ++
        if (playerController != null)
        {
            playerController.OnPressStateChanged -= HandlePressState;
        }
    }

    public void EquipSpecialItem(SpecialItemType newItemType)
    {
        currentSpecialItem = newItemType;
        Debug.Log($"[ToyBoxPlayer] {playerController.playerName} 獲得了特殊道具: {newItemType}！(舊道具已被覆蓋)");

        // 這裡未來可以加上 UI 更新，例如在玩家頭上顯示一個道具小圖示
    }

    // ++ 新增：接收輸入，轉換為單擊邏輯 ++
    private void HandlePressState(bool isPressed)
    {
        // 因為 ToyBox 需要的是「單擊」(按下那一刻)，所以只在 isPressed 為 true 時觸發
        if (isPressed)
        {
            TriggerToyBoxSkill();
        }
    }

    private void TriggerToyBoxSkill()
    {
        // 如果身上沒有特殊道具，就不做任何事
        if (currentSpecialItem == SpecialItemType.None) return;

        bool isItemConsumed = false; // 用來判定這次發動有沒有成功

        switch (currentSpecialItem)
        {
            case SpecialItemType.Blocker:
                // 尋找場景中的生成機關
                var spawnableObj = FindFirstObjectByType<Blocker>();
                if (spawnableObj != null)
                {
                    // 嘗試觸發。如果機關沒在忙，就會回傳 true 讓我們消耗道具
                    isItemConsumed = spawnableObj.TryActivate();
                }
                break;

            case SpecialItemType.FingerToyBuffer:
                // 找出場景中所有的變大機關
                var allScalableObjs = FindObjectsByType<FingerToyController>(FindObjectsSortMode.None);

                foreach (var obj in allScalableObjs)
                {
                    // 嘗試觸發每一個機關。只要有任何一個成功，就標記為消耗道具
                    if (obj.TryActivate())
                    {
                        isItemConsumed = true;
                    }
                }
                break;
            case SpecialItemType.Freezer:

                GameManager gm = FindFirstObjectByType<GameManager>();

                // 1. 取得目前分數第一名的玩家清單 (可能有多人並列第一)
                var topPlayers = gm.GetNo1Player();

                foreach (var playerInfo in topPlayers)
                {
                    // 2. 檢查：如果第一名是自己，就跳過
                    if (playerInfo.index == playerController.playerIndex) continue;

                    // 3. 從 GameManager 找到對應的 PlayerController 實體
                    if (gm.playerControllers.TryGetValue(playerInfo.index, out PlayerController targetPc))
                    {
                        targetPc.StartFrozen(freezeDuration);
                        isItemConsumed = true; // 只要有冰到至少一個「別人」，就標記為成功消耗
                        Debug.Log($"[ToyBoxPlayer] 成功冰凍了第一名玩家: {playerInfo.name}");
                    }
                }
                break;
        }

        if (isItemConsumed)
        {
            currentSpecialItem = SpecialItemType.None;
        }
        else if (currentSpecialItem == SpecialItemType.Freezer)
        {
            Debug.Log("[ToyBoxPlayer] 第一名是自己或沒找到目標，不消耗冰凍道具");
        }
        else
        {
            // 如果 isItemConsumed 是 false，代表機關正在啟動中，道具會保留在玩家身上
            Debug.Log($"[ToyBoxPlayer] 機關冷卻中，不消耗道具！");
        }
    }

    // 玩家碰到道具
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Collectable"))
        {
            ItemManager.Instance.RequestCollect(transform, other.transform, stealSkill);
        }
    }

    // 被搶成功時，ItemManager 會叫這個
    public void ActivateImmunityStolen()
    {
        if (isImmuneStolen) return;
        isImmuneStolen = true;
        if (immunityRoutine != null) StopCoroutine(immunityRoutine);
        immunityRoutine = StartCoroutine(ImmunityCountdown());
    }

    private IEnumerator ImmunityCountdown()
    {
        yield return new WaitForSeconds(immunityStolenDuration);
        isImmuneStolen = false;
        immunityRoutine = null;
    }

    // 結算或放入終點時呼叫
    public void CompleteItemCollection()
    {
        if (playerController == null) return;

        var items = ItemManager.Instance.TakeAllItemsFromPlayer(transform);
        if (items.Count <= 0) return;

        int totalScore = 0;
        foreach (var t in items)
        {
            totalScore += 1;

            ItemData data = t.GetComponent<ItemData>();
            if (data != null)
            {
                totalScore += data.extraScore;
            }
        }

        FindFirstObjectByType<GameManager>().IncreasePlayerPoint(playerController.playerIndex, totalScore);
        FindFirstObjectByType<PlayerPointUiManager>().UpdatePlayerPointUi(playerController.playerIndex);
        FindFirstObjectByType<GameSoundEffect>()?.PlayGetPointSound();

        ItemSpawner spawner = FindFirstObjectByType<ItemSpawner>();
        foreach (var t in items)
        {
            if (spawner != null) spawner.ItemCollected(t.gameObject);
            else Destroy(t.gameObject);
        }
    }
}




