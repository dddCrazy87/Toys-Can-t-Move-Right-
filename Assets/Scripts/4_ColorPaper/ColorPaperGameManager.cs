using UnityEngine;
using System.Collections;

public class ColorPaperGameManager : MonoBehaviour
{
    [SerializeField] private CountDownUI countdownUI;
    [SerializeField] private SceneFadeInFadeOut sceneFadeInFadeOut;
    [SerializeField] private float gameTimeLimit = 90f;
    [SerializeField] private AudioSource gameOverAudio;

    [Header("遊戲說明")]
    [SerializeField] private GameInstructionUI instructionUI;

    [Header("填色系統")]
    [SerializeField] private ColorGrid colorGrid;

    [Header("Render Texture 畫布")]
    [SerializeField] private PaintCanvas paintCanvas;
    [SerializeField] private float brushSize = 0.06f;  // 筆刷大小（UV 空間）

    // 顏料 UI 已整合到 ColorPaperScoreUI

    BgmPlayer bgmPlayer;
    NetworkManager networkManager;
    GameManager gameManager;
    ColorPaperScoreUI scoreUI;

    [Header("分數更新設定")]
    [SerializeField] private float scoreUpdateInterval = 0.5f;  // 每 0.5 秒更新一次 UI
    private float scoreUpdateTimer;
    private bool isGameRunning = false;

    void Start()
    {
        bgmPlayer = FindFirstObjectByType<BgmPlayer>();
        gameManager = FindFirstObjectByType<GameManager>();
        networkManager = FindFirstObjectByType<NetworkManager>();

        // 倒數期間先暫停 BGM
        if (bgmPlayer) bgmPlayer.PauseBGM();

        // 先顯示說明圖，完成後再開始倒數
        if (instructionUI != null)
        {
            Debug.Log("[ColorPaperGameManager] 顯示說明圖...");
            instructionUI.ShowInstruction(OnInstructionComplete);
        }
        else
        {
            // 沒有說明圖，直接開始倒數
            Debug.LogWarning("[ColorPaperGameManager] instructionUI 未設定，跳過說明圖");
            OnInstructionComplete();
        }
    }

    void OnInstructionComplete()
    {
        var gameStartCountDown = FindFirstObjectByType<GameStartCountDown>();
        if (gameStartCountDown != null)
        {
            gameStartCountDown.CountDownAndStartGame(OnCountDownFinished);
        }
        else
        {
            // 如果沒有開始倒數，直接開始遊戲
            OnCountDownFinished();
        }
    }

    void OnCountDownFinished()
    {
        if (gameManager != null)
        {
            gameManager.StartGame();
        }

        // 倒數結束後才播放 BGM
        if (bgmPlayer) bgmPlayer.ChangeBgm();

        // 啟動填色系統
        if (colorGrid) colorGrid.EnableColoring();

        // 先初始化玩家筆刷和能量系統（必須在 UI 之前）
        InitializePlayerBrushes();

        // 再初始化長條圖計分 UI（這時候 PaintEnergy 已經存在）
        scoreUI = FindFirstObjectByType<ColorPaperScoreUI>();
        Debug.Log($"[ColorPaperGameManager] 找到 scoreUI: {scoreUI != null}");
        if (scoreUI != null)
        {
            scoreUI.Initialize();
        }
        else
        {
            Debug.LogWarning("[ColorPaperGameManager] 找不到 ColorPaperScoreUI！");
        }

        // 啟用玩家互撞
        EnablePlayerCollision();

        isGameRunning = true;

        if (countdownUI != null)
        {
            countdownUI.StartCountdown(gameTimeLimit, OnCountdownFinished);
        }
    }

    void Update()
    {
        if (!isGameRunning || colorGrid == null || scoreUI == null) return;

        scoreUpdateTimer += Time.deltaTime;
        if (scoreUpdateTimer >= scoreUpdateInterval)
        {
            scoreUpdateTimer = 0f;
            UpdateScoreUI();
        }
    }

    void UpdateScoreUI()
    {
        var playerScores = colorGrid.GetPlayerScores();

        // 更新 GameManager 的分數（給結算畫面用）
        foreach (var kvp in playerScores)
        {
            gameManager.SetPlayerPoint(kvp.Key, kvp.Value);
        }

        // 更新長條圖 UI
        scoreUI.UpdateScores(playerScores);
    }

    void OnCountdownFinished()
    {
        isGameRunning = false;

        // 停止填色
        if (colorGrid) colorGrid.DisableColoring();

        // 停止畫圖
        StopPlayerBrushes();

        // 鎖定所有玩家的移動與控制
        if (gameManager != null)
        {
            foreach (var kvp in gameManager.playerControllers)
            {
                PlayerController player = kvp.Value;
                if (player != null)
                {
                    player.ForceStopMotion();
                    player.enabled = false;
                }
            }
        }

        // 最終更新一次分數 UI
        UpdateScoreUI();

        if (bgmPlayer) bgmPlayer.PauseBGM();
        if (gameOverAudio) gameOverAudio.Play();
        StartCoroutine(EndGameRoutine());
    }

    [Header("上傳系統")]
    [SerializeField] private ImgBBUploader imgUploader;

    private IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        string uploadedUrl = "";

        if (imgUploader != null)
        {
            yield return StartCoroutine(imgUploader.UploadToImgBB((url) =>
            {
                uploadedUrl = url;
            }));

            if (!string.IsNullOrEmpty(uploadedUrl))
            {
                Debug.Log($"圖片網址: {uploadedUrl}");
                if (gameManager) gameManager.hasPostcard = true;
            }
            else
            {
                Debug.LogWarning("[ColorPaperGameManager] 上傳失敗");
            }
        }
        else
        {
            Debug.LogWarning("[ColorPaperGameManager] 上傳被跳過");
            yield return new WaitForSeconds(0.5f);
        }

        if (networkManager) networkManager.BroadcastTerminate(uploadedUrl);
        sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
    }


    void InitializePlayerBrushes()
    {
        if (gameManager == null || paintCanvas == null) return;

        // 設定筆刷大小
        paintCanvas.SetBrushSize(brushSize);

        foreach (var kvp in gameManager.playerControllers)
        {
            PlayerController player = kvp.Value;
            if (player == null) continue;

            // 取得或添加 PaintEnergy 組件
            PaintEnergy energy = player.GetComponent<PaintEnergy>();
            if (energy == null)
            {
                energy = player.gameObject.AddComponent<PaintEnergy>();
            }
            energy.Initialize();  // 初始化能量（預設 3 格）

            // 取得或添加 PaintBrush 組件
            PaintBrush brush = player.GetComponent<PaintBrush>();
            if (brush == null)
            {
                brush = player.gameObject.AddComponent<PaintBrush>();
            }
            brush.Initialize(paintCanvas, player.playerColor);
            // 不再自動開始繪製，改由按壓事件控制
        }
        // 顏料 UI 已整合到 ColorPaperScoreUI，會在 scoreUI.Initialize() 時自動設定
    }

    void StopPlayerBrushes()
    {
        if (gameManager == null) return;

        foreach (var kvp in gameManager.playerControllers)
        {
            PlayerController player = kvp.Value;
            if (player == null) continue;

            PaintBrush brush = player.GetComponent<PaintBrush>();
            if (brush != null)
            {
                brush.StopPainting();
            }
        }
    }

    void EnablePlayerCollision()
    {
        if (gameManager == null) return;

        foreach (var kvp in gameManager.playerControllers)
        {
            PlayerController player = kvp.Value;
            if (player != null)
            {
                player.enablePlayerCollision = true;
            }
        }
    }
}
