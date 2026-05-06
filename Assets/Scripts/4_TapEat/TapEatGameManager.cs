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
    public CountDownUI countdownUI;                // 遊戲中 60 秒倒數 UI
    public SceneFadeInFadeOut sceneFadeInFadeOut;   // 場景轉場（可選）
    public TextMeshProUGUI[] playerScoreTexts;      // 各玩家分數 UI

    [Header("上傳系統")]
    public ImgBBUploader imgUploader;               // 截圖上傳（可選）

    [Header("Players")]
    public FoodController[] foodControllers;        // 每位玩家的食物控制器 (長度 4)
    public float playerScale = 2f;                  // 角色放大倍率

    private NetworkManager networkManager;
    private GameManager gameManager;
    private Dictionary<int, PlayerEatState> playerStates = new();
    private Dictionary<int, EatAnimator> eatAnimators = new();
    private bool isGameActive = false;

    private class PlayerEatState
    {
        public int totalBites = 0;
        public int currentFoodBites = 0;
        public int platesCompleted = 0;
    }

    void Start()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();
        gameManager = FindFirstObjectByType<GameManager>();

        // 先顯示 GameStartCountDown 3-2-1，然後開始遊戲
        var gameStartCountDown = FindFirstObjectByType<GameStartCountDown>();
        if (gameStartCountDown != null)
        {
            gameStartCountDown.CountDownAndStartGame(OnCountDownFinished);
        }
        else
        {
            OnCountDownFinished();
        }
    }

    void OnCountDownFinished()
    {
        // 呼叫 GameManager.StartGame() 來生成玩家角色
        if (gameManager != null)
        {
            gameManager.StartGame();

            // 放大角色 + 加上吃東西動畫 + 停用移動
            foreach (var kvp in gameManager.playerControllers)
            {
                int idx = kvp.Key;
                PlayerController pc = kvp.Value;

                // 放大角色
                pc.transform.localScale *= playerScale;

                // 讓角色面朝相機（旋轉 180 度，但保持原位）
                Vector3 pos = pc.transform.position;
                pc.transform.Rotate(0f, 180f, 0f);
                pc.transform.position = pos;

                // 停用移動（這關不需要走路）
                pc.moveSpeed = 0f;
                Rigidbody rb = pc.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                // 加上吃東西動畫元件
                EatAnimator eat = pc.gameObject.AddComponent<EatAnimator>();
                eatAnimators[idx] = eat;
            }
        }

        // 初始化玩家吃東西狀態
        if (networkManager != null)
        {
            foreach (var kvp in networkManager.peerIdToPlayer)
            {
                playerStates[kvp.Value.index] = new PlayerEatState();
            }
        }

        // 隱藏未使用的玩家位置（盤子 + 食物）
        for (int i = 0; i < foodControllers.Length; i++)
        {
            if (!playerStates.ContainsKey(i) && foodControllers[i] != null)
            {
                foodControllers[i].transform.parent.gameObject.SetActive(false);
            }
        }

        // 開始 60 秒倒數
        if (countdownUI != null)
        {
            countdownUI.StartCountdown(gameDuration, OnTimerFinished);
        }

        isGameActive = true;
        Debug.Log("[TapEat] 遊戲開始！");
    }

    void OnTimerFinished()
    {
        isGameActive = false;
        OnGameEnd();
    }

    /// <summary>
    /// 收到玩家的點擊動作（由 NetworkManager 呼叫）
    /// </summary>
    public void OnTapAction(int playerIndex)
    {
        if (!isGameActive) return;
        if (!playerStates.ContainsKey(playerIndex)) return;

        var state = playerStates[playerIndex];
        state.totalBites++;
        state.currentFoodBites++;

        // 播放吃東西動畫
        if (eatAnimators.ContainsKey(playerIndex) && eatAnimators[playerIndex] != null)
        {
            eatAnimators[playerIndex].PlayEat();
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

        // 更新分數 UI（顯示盤數）
        UpdateScoreUI(playerIndex, state.platesCompleted);

        // 更新 GameManager 的分數（盤數 = 最終得分）
        if (gameManager != null)
        {
            gameManager.playersInfo[playerIndex].point = state.platesCompleted;
        }
    }

    private void OnGameEnd()
    {
        Debug.Log("[TapEat] 時間到！");
        StartCoroutine(EndGameRoutine());
    }

    private IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        string uploadedUrl = "";

        // 截圖上傳（如果有設定）
        if (imgUploader != null)
        {
            yield return StartCoroutine(imgUploader.UploadToImgBB((url) =>
            {
                uploadedUrl = url;
            }));
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (networkManager != null)
        {
            networkManager.BroadcastTerminate(uploadedUrl);
        }

        // 場景轉場（如果有設定）
        if (sceneFadeInFadeOut != null)
        {
            sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
        }
    }

    private void UpdateScoreUI(int playerIndex, int score)
    {
        if (playerScoreTexts != null && playerIndex < playerScoreTexts.Length && playerScoreTexts[playerIndex] != null)
        {
            playerScoreTexts[playerIndex].text = score.ToString();
        }
    }
}
