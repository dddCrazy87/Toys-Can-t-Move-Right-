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

    [Header("上傳系統")]
    public ImgBBUploader imgUploader;               // 截圖上傳（可選）

    [Header("Players")]
    public FoodController[] foodControllers;        // 每位玩家的食物控制器 (長度 4)
    public float playerScale = 2f;                  // 角色放大倍率

    [Header("音效")]
    public AudioClip biteSound;                     // 咬一口音效
    public AudioClip plateCompleteSound;            // 吃完一盤音效
    public AudioClip bgmClip;                       // 背景音樂
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    private AudioSource sfxSource;                  // 音效播放器
    private AudioSource bgmSource;                  // 背景音樂播放器
    private NetworkManager networkManager;
    private GameManager gameManager;
    private Dictionary<int, PlayerEatState> playerStates = new();
    private Dictionary<int, EatAnimator> eatAnimators = new();
    private PlayerPointUiManager pointUiManager;
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

        // 初始化音效播放器
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.loop = true;

        // 暫停主 BGM（跟 ToyBox 一樣）
        BgmPlayer mainBgm = FindFirstObjectByType<BgmPlayer>();
        if (mainBgm != null) mainBgm.PauseBGM();

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

            // 放大角色 + 面朝相機 + 吃東西動畫 + 停用移動
            Camera cam = Camera.main;
            foreach (var kvp in gameManager.playerControllers)
            {
                int idx = kvp.Key;
                PlayerController pc = kvp.Value;

                // 停用移動和物理（這關不需要走路）
                pc.moveSpeed = 0f;
                Rigidbody rb = pc.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                // 記住 SpawnPoint 位置
                Vector3 spawnPos = pc.transform.position;

                // 放大角色
                pc.transform.localScale *= playerScale;

                // 讓角色面朝相機（只轉 Y 軸，保持站立）
                if (cam != null)
                {
                    Vector3 lookDir = cam.transform.position - spawnPos;
                    lookDir.y = 0; // 只在水平面旋轉
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        pc.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                    }
                }

                // 強制把角色放回 SpawnPoint 位置（防止 pivot 偏移）
                pc.transform.position = spawnPos;

                // 加上吃東西動畫
                EatAnimator eat = pc.gameObject.AddComponent<EatAnimator>();
                eatAnimators[idx] = eat;
            }
        }

        // 初始化玩家吃東西狀態 + 設定 FoodController 的 playerIndex
        if (networkManager != null)
        {
            foreach (var kvp in networkManager.peerIdToPlayer)
            {
                int idx = kvp.Value.index;
                playerStates[idx] = new PlayerEatState();

                // 設定 FoodController 的 playerIndex（用於隔離 SpriteMask）
                if (foodControllers != null && idx < foodControllers.Length && foodControllers[idx] != null)
                {
                    foodControllers[idx].playerIndex = idx;
                }
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

        // 初始化分數 UI（跟 Toybox 一樣）
        pointUiManager = FindFirstObjectByType<PlayerPointUiManager>();
        if (pointUiManager != null)
        {
            pointUiManager.InitialPlayerPointUi();
        }

        // 開始 60 秒倒數
        if (countdownUI != null)
        {
            countdownUI.StartCountdown(gameDuration, OnTimerFinished);
        }

        // 開始播放背景音樂
        if (bgmClip != null && bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
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

        // 播放咬一口音效
        if (biteSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(biteSound, sfxVolume);
        }

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
                // 播放吃完一盤音效
                if (plateCompleteSound != null && sfxSource != null)
                {
                    sfxSource.PlayOneShot(plateCompleteSound, sfxVolume);
                }
                state.currentFoodBites = 0;
                state.platesCompleted++;
                foodControllers[playerIndex].ServeNextFood();
                Debug.Log($"[TapEat] Player {playerIndex} 吃完第 {state.platesCompleted} 盤！");
            }
        }

        // 更新 GameManager 的分數（盤數 = 最終得分）
        if (gameManager != null)
        {
            gameManager.playersInfo[playerIndex].point = state.platesCompleted;

            // 更新分數 UI
            if (pointUiManager != null)
            {
                pointUiManager.UpdatePlayerPointUi(playerIndex);
            }
        }
    }

    private void OnGameEnd()
    {
        Debug.Log("[TapEat] 時間到！");

        // 停止背景音樂
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }

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

}
