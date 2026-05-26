using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [Header("裝備中的特殊道具")]
    public GameObject iceItem; public GameObject fingerItem; public GameObject blockerItem;
    public Transform eqvPos;
    private GameObject curEqvItemGo;

    [Header("使用特效")]
    public GameObject useVFX;

    // ++ 新增：讓你可以從 Inspector 自由調整特效要播多久才觸發效果 ++
    [Header("特效等待時間 (秒)")]
    public float vfxWaitTime = 1.0f;

    [Header("冰凍持續時間")]
    [SerializeField] private float freezeDuration = 5f;

    // ++ 新增：防止玩家在等待特效播完時重複點擊觸發 ++
    private bool isUsingItem = false;

    public void Initialize()
    {
        playerController = GetComponent<PlayerController>();

        if (followPoint == null) followPoint = transform.Find("follow point");
        if (playerController != null) playerController.OnPressStateChanged += HandlePressState;
    }

    private void OnDestroy()
    {
        if (playerController != null) playerController.OnPressStateChanged -= HandlePressState;
    }

    public void EquipSpecialItem(SpecialItemType newItemType)
    {
        currentSpecialItem = newItemType;
        Debug.Log($"[ToyBoxPlayer] {playerController.playerName} 獲得特殊道具: {newItemType}");

        if (curEqvItemGo) Destroy(curEqvItemGo);
        switch (newItemType)
        {
            case SpecialItemType.None: break;
            case SpecialItemType.Blocker:
                curEqvItemGo = Instantiate(blockerItem, eqvPos); break;
            case SpecialItemType.FingerToyBuffer:
                curEqvItemGo = Instantiate(fingerItem, eqvPos); break;
            case SpecialItemType.Freezer:
                curEqvItemGo = Instantiate(iceItem, eqvPos); break;
            default: break;
        }

        // 更新分數旁邊的道具 icon
        var pointUiManager = FindFirstObjectByType<PlayerPointUiManager>();
        if (pointUiManager != null)
        {
            pointUiManager.UpdatePlayerItemIcon(playerController.playerIndex, newItemType);
        }
    }

    private void HandlePressState(bool isPressed)
    {
        if (isPressed) TriggerToyBoxSkill();
    }

    private void TriggerToyBoxSkill()
    {
        // 如果身上沒有特殊道具，或者「正在使用道具中（等特效）」，就不做任何事
        if (currentSpecialItem == SpecialItemType.None || isUsingItem) return;

        bool canConsume = false;
        Action triggerEffectAction = null;

        // --- 階段 1：事前判定與收集目標 ---
        switch (currentSpecialItem)
        {
            case SpecialItemType.Blocker:
                var blocker = FindFirstObjectByType<Blocker>();
                if (blocker != null && blocker.CanActivate())
                {
                    canConsume = true;
                    triggerEffectAction = () =>
                    {
                        // 加上 null 檢查以防等待期間機關被意外刪除
                        if (blocker != null) blocker.Activate();
                    };
                }
                break;

            case SpecialItemType.FingerToyBuffer:
                var allScalableObjs = FindObjectsByType<FingerToyController>(FindObjectsSortMode.None);
                List<FingerToyController> validToys = new List<FingerToyController>();

                foreach (var obj in allScalableObjs)
                {
                    if (obj.CanActivate()) validToys.Add(obj);
                }

                if (validToys.Count > 0)
                {
                    canConsume = true;
                    triggerEffectAction = () =>
                    {
                        foreach (var obj in validToys)
                        {
                            if (obj != null) obj.Activate();
                        }
                    };
                }
                break;

            case SpecialItemType.Freezer:
                GameManager gm = FindFirstObjectByType<GameManager>();

                // 先取得第一名的名單
                var topPlayers = gm.GetNo1Player();

                // 判斷自己是否在第一名的名單中
                bool amIFirstPlace = false;
                foreach (var pInfo in topPlayers)
                {
                    if (pInfo.index == playerController.playerIndex)
                    {
                        amIFirstPlace = true;
                        break;
                    }
                }

                // 決定要冰凍的目標：如果是第一名，改找第二名；否則就找第一名
                // 注意：這裡假設你在 GameManager 中有實作 GetNo2Player() 的方法
                var targetPlayersInfo = amIFirstPlace ? gm.GetNo2Player() : topPlayers;

                List<PlayerController> targets = new List<PlayerController>();

                foreach (var playerInfo in targetPlayersInfo)
                {
                    // 如果不是單人遊戲，且目標是自己，則跳過 (防呆)
                    if (gm.playersInfo.Count != 1 && playerInfo.index == playerController.playerIndex) continue;

                    if (gm.playerControllers.TryGetValue(playerInfo.index, out PlayerController targetPc)) targets.Add(targetPc);
                }

                if (targets.Count > 0)
                {
                    canConsume = true;
                    triggerEffectAction = () =>
                    {
                        foreach (var target in targets)
                        {
                            // 加上 null 檢查，防止等待特效的期間玩家斷線離開遊戲
                            if (target != null) target.StartFrozen(freezeDuration);
                        }
                    };
                }
                break;
        }

        // --- 階段 2：啟動協程來處理有時間差的步驟 ---
        if (canConsume) StartCoroutine(ExecuteSkillSequence(triggerEffectAction));
    }

    // 處理時間延遲的協程
    private IEnumerator ExecuteSkillSequence(Action effectAction)
    {
        // 標記正在使用道具，鎖定輸入
        isUsingItem = true;

        // 順序 1: 先施放粒子效果一次
        PlayVFX();
        FindFirstObjectByType<GameSoundEffect>()?.PlayUseSpecialSound();

        // 順序 2: 等待特效播放 (可從 Inspector 調整 vfxWaitTime)
        yield return new WaitForSeconds(vfxWaitTime);

        // 順序 3: 將特殊道具刪除
        currentSpecialItem = SpecialItemType.None;
        if (curEqvItemGo) Destroy(curEqvItemGo);

        // 清除分數旁邊的道具 icon
        var pointUiManager = FindFirstObjectByType<PlayerPointUiManager>();
        if (pointUiManager != null)
        {
            pointUiManager.UpdatePlayerItemIcon(playerController.playerIndex, SpecialItemType.None);
        }

        // 順序 4: 觸發道具效果
        effectAction?.Invoke();

        // 解除鎖定
        isUsingItem = false;
    }

    private void PlayVFX()
    {
        if (useVFX == null || eqvPos == null) return;

        GameObject vfx = Instantiate(useVFX, eqvPos);
        for (int i = 0; i < vfx.transform.childCount; i++)
        {
            ParticleSystem ps = vfx.transform.GetChild(i).GetComponent<ParticleSystem>();

            if (ps)
            {
                ps.Stop();
                // var mainModule = ps.main;
                // mainModule.startColor = playerController.playerColor switch
                // {
                //     "blue" => Color.blue,
                //     "yellow" => Color.yellow,
                //     "green" => Color.green,
                //     "red" => Color.red,
                //     _ => Color.yellow
                // };
                ps.Play();
            }
        }
    }

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
            if (data != null) totalScore += data.extraScore;
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




