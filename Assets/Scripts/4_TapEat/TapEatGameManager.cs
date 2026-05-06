using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TapEatGameManager : MonoBehaviour
{
    [Header("Settings")]
    public float gameDuration = 60f;
    public int bitesPerFood = 8;

    [Header("References")]
    public NetworkManager networkManager;
    public TextMeshProUGUI timerText;           // 倒數計時 UI
    public TextMeshProUGUI countdownText;       // 3-2-1 倒數 UI
    public TextMeshProUGUI[] playerScoreTexts;  // 各玩家分數 UI (長度 4)

    [Header("Players")]
    public FoodController[] foodControllers;    // 每位玩家的食物控制器 (長度 4)
    public Animator[] playerAnimators;          // 每位玩家的動畫控制器 (長度 4，可選)

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

        // 隱藏未使用的玩家位置
        for (int i = 0; i < foodControllers.Length; i++)
        {
            if (!playerStates.ContainsKey(i) && foodControllers[i] != null)
            {
                foodControllers[i].gameObject.SetActive(false);
            }
        }

        UpdateTimerUI();
        UpdateAllScoreUI();

        // 開始遊戲倒數
        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            yield return new WaitForSeconds(1f);
            countdownText.text = "2";
            yield return new WaitForSeconds(1f);
            countdownText.text = "1";
            yield return new WaitForSeconds(1f);
            countdownText.text = "開始！";
            yield return new WaitForSeconds(0.5f);

            countdownText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(3f);
        }

        isGameActive = true;
        Debug.Log("[TapEat] 遊戲開始！");
    }

    void Update()
    {
        if (!isGameActive) return;

        remainingTime -= Time.deltaTime;
        UpdateTimerUI();

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

        // 播放角色吃東西動畫
        if (playerAnimators != null && playerIndex < playerAnimators.Length && playerAnimators[playerIndex] != null)
        {
            playerAnimators[playerIndex].SetTrigger("Eat");
        }

        // 食物被咬
        if (foodControllers != null && playerIndex < foodControllers.Length && foodControllers[playerIndex] != null)
        {
            bool finished = foodControllers[playerIndex].Bite();

            if (finished)
            {
                // 吃完一盤，換新的
                state.currentFoodBites = 0;
                state.platesCompleted++;
                foodControllers[playerIndex].ServeNextFood();
                Debug.Log($"[TapEat] Player {playerIndex} 吃完第 {state.platesCompleted} 盤！");
            }
        }

        // 更新分數 UI
        UpdateScoreUI(playerIndex, state.totalBites);
    }

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

            StartCoroutine(EndGameRoutine());
        }
    }

    private IEnumerator EndGameRoutine()
    {
        // 顯示「時間到！」
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "時間到！";
        }

        yield return new WaitForSeconds(2f);

        // TODO: 截圖上傳（如需要）
        networkManager.BroadcastTerminate("");
    }

    // --- UI 更新 ---

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(remainingTime);
            timerText.text = $"0:{seconds:D2}";
        }
    }

    private void UpdateScoreUI(int playerIndex, int score)
    {
        if (playerScoreTexts != null && playerIndex < playerScoreTexts.Length && playerScoreTexts[playerIndex] != null)
        {
            playerScoreTexts[playerIndex].text = score.ToString();
        }
    }

    private void UpdateAllScoreUI()
    {
        foreach (var kvp in playerStates)
        {
            UpdateScoreUI(kvp.Key, kvp.Value.totalBites);
        }
    }
}
