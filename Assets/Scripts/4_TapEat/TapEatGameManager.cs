using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TapEatGameManager : MonoBehaviour
{
    [Header("Settings")]
    public float gameDuration = 60f;
    public int bitesPerFood = 8;

    [Header("References")]
    public NetworkManager networkManager;
    public TextMeshProUGUI timerText;              // CountDownUI → Text (TMP)，顯示 60 秒倒數
    public GameObject countdownRoot;               // GameStartCountDown 物件（底下有 3, 2, 1 子物件）
    public TextMeshProUGUI[] playerScoreTexts;     // 各玩家分數 UI (長度 4)

    [Header("Players")]
    public FoodController[] foodControllers;       // 每位玩家的食物控制器 (長度 4)
    public Animator[] playerAnimators;             // 每位玩家的動畫控制器 (可選)

    private Dictionary<int, PlayerEatState> playerStates = new();
    private float remainingTime;
    private bool isGameActive = false;

    private class PlayerEatState
    {
        public int totalBites = 0;
        public int currentFoodBites = 0;
        public int platesCompleted = 0;
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
                // 隱藏整個 SpawnPoint（食物 + 盤子 + 角色）
                foodControllers[i].transform.parent.gameObject.SetActive(false);
            }
        }

        UpdateTimerUI();
        UpdateAllScoreUI();

        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        // 使用 GameStartCountDown 底下的子物件 (3, 2, 1)
        if (countdownRoot != null)
        {
            countdownRoot.SetActive(true);

            // 先全部隱藏
            foreach (Transform child in countdownRoot.transform)
            {
                child.gameObject.SetActive(false);
            }

            // 依序顯示 3 → 2 → 1
            int childCount = countdownRoot.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = countdownRoot.transform.GetChild(i);
                child.gameObject.SetActive(true);
                yield return new WaitForSeconds(1f);
                child.gameObject.SetActive(false);
            }

            countdownRoot.SetActive(false);
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
                state.currentFoodBites = 0;
                state.platesCompleted++;
                foodControllers[playerIndex].ServeNextFood();
                Debug.Log($"[TapEat] Player {playerIndex} 吃完第 {state.platesCompleted} 盤！");
            }
        }

        UpdateScoreUI(playerIndex, state.totalBites);
    }

    private void OnGameEnd()
    {
        Debug.Log("[TapEat] 時間到！");

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
        // 顯示「時間到！」用倒數 UI
        if (timerText != null)
        {
            timerText.text = "時間到！";
        }

        yield return new WaitForSeconds(2f);

        networkManager.BroadcastTerminate("");
    }

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
