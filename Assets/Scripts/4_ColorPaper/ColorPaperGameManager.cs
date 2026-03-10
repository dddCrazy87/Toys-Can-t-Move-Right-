using UnityEngine;

public class ColorPaperGameManager : MonoBehaviour
{
    [SerializeField] private CountDownUI countdownUI;
    [SerializeField] private SceneFadeInFadeOut sceneFadeInFadeOut;
    [SerializeField] private float gameTimeLimit = 90f;
    [SerializeField] private AudioSource gameOverAudio;

    [Header("填色系統")]
    [SerializeField] private ColorGrid colorGrid;

    [Header("Render Texture 畫布")]
    [SerializeField] private PaintCanvas paintCanvas;
    [SerializeField] private float brushSize = 0.03f;  // 筆刷大小（UV 空間）

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

        // 初始化長條圖計分 UI
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

        // 啟動填色系統
        if (colorGrid) colorGrid.EnableColoring();

        // 初始化並啟動玩家筆刷（Render Texture）
        InitializePlayerBrushes();

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
            gameManager.playersInfo[kvp.Key].point = kvp.Value;
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

        // 最終更新一次分數 UI
        UpdateScoreUI();

        if (bgmPlayer) bgmPlayer.PauseBGM();
        if (gameOverAudio) gameOverAudio.Play();
        Invoke(nameof(LoadNextSceneWithFadeOut), 1f);
    }

    void LoadNextSceneWithFadeOut()
    {
        if (networkManager) networkManager.BroadcastTerminate();
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

            // 取得或添加 PaintBrush 組件
            PaintBrush brush = player.GetComponent<PaintBrush>();
            if (brush == null)
            {
                brush = player.gameObject.AddComponent<PaintBrush>();
            }
            brush.Initialize(paintCanvas, player.playerColor);
            brush.StartPainting();
        }
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
}
