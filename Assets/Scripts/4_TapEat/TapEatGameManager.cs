using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TapEatGameManager : MonoBehaviour
{
    [Header("Settings")]
    public float gameDuration = 60f;
    public int bitesPerFood = 8;

    [Header("References")]
    public NetworkManager networkManager;

    // 每位玩家的吃東西狀態
    private Dictionary<int, PlayerEatState> playerStates = new();

    private float remainingTime;
    private bool isGameActive = false;

    private class PlayerEatState
    {
        public int totalBites = 0;        // 總咬數 = 分數
        public int currentFoodBites = 0;  // 目前這盤的咬數
        public int platesCompleted = 0;   // 吃完幾盤
    }

    void Start()
    {
        remainingTime = gameDuration;
        networkManager = FindFirstObjectByType<NetworkManager>();

        // 初始化玩家狀態
        if (networkManager != null)
        {
            foreach (var kvp in networkManager.peerIdToPlayer)
            {
                playerStates[kvp.Value.index] = new PlayerEatState();
            }
        }

        // 開始遊戲倒數
        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        // 3-2-1 倒數
        Debug.Log("[TapEat] 3...");
        yield return new WaitForSeconds(1f);
        Debug.Log("[TapEat] 2...");
        yield return new WaitForSeconds(1f);
        Debug.Log("[TapEat] 1...");
        yield return new WaitForSeconds(1f);
        Debug.Log("[TapEat] 開始！");

        isGameActive = true;
    }

    void Update()
    {
        if (!isGameActive) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isGameActive = false;
            OnGameEnd();
        }
    }

    /// <summary>
    /// 收到玩家的點擊動作
    /// </summary>
    public void OnTapAction(int playerIndex)
    {
        if (!isGameActive) return;
        if (!playerStates.ContainsKey(playerIndex)) return;

        var state = playerStates[playerIndex];
        state.totalBites++;
        state.currentFoodBites++;

        Debug.Log($"[TapEat] Player {playerIndex} bite! Total: {state.totalBites}, Current food: {state.currentFoodBites}/{bitesPerFood}");

        // TODO: 播放角色吃東西動畫
        // TODO: 在食物上加咬痕遮罩

        // 吃完一盤
        if (state.currentFoodBites >= bitesPerFood)
        {
            state.currentFoodBites = 0;
            state.platesCompleted++;
            Debug.Log($"[TapEat] Player {playerIndex} finished plate {state.platesCompleted}! Serving new food...");

            // TODO: 換盤動畫 + 新食物
        }
    }

    /// <summary>
    /// 遊戲結束，計算排名並發送 terminate
    /// </summary>
    private void OnGameEnd()
    {
        Debug.Log("[TapEat] 時間到！計算排名...");

        // 更新玩家分數到 NetworkManager 的 playersInfo
        if (networkManager != null)
        {
            foreach (var kvp in networkManager.peerIdToPlayer)
            {
                if (playerStates.ContainsKey(kvp.Value.index))
                {
                    kvp.Value.point = playerStates[kvp.Value.index].totalBites;
                }
            }

            // 發送 terminate（NetworkManager 會自動排序）
            StartCoroutine(EndGameRoutine());
        }
    }

    private IEnumerator EndGameRoutine()
    {
        // 顯示「時間到！」2 秒
        yield return new WaitForSeconds(2f);

        // TODO: 截圖上傳（如果需要的話）
        networkManager.BroadcastTerminate("");
    }
}
