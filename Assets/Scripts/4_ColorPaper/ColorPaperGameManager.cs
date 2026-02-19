using UnityEngine;

public class ColorPaperGameManager : MonoBehaviour
{
    [SerializeField] private CountDownUI countdownUI;
    [SerializeField] private SceneFadeInFadeOut sceneFadeInFadeOut;
    [SerializeField] private float gameTimeLimit = 90f;
    [SerializeField] private AudioSource gameOverAudio;

    [Header("填色系統")]
    [SerializeField] private ColorGrid colorGrid;

    BgmPlayer bgmPlayer;
    NetworkManager networkManager;
    GameManager gameManager;
    PlayerPointUiManager pointUiManager;

    [Header("分數更新設定")]
    [SerializeField] private float scoreUpdateInterval = 0.5f;  // 每 0.5 秒更新一次 UI
    private float scoreUpdateTimer;
    private bool isGameRunning = false;

    void Start()
    {
        bgmPlayer = FindFirstObjectByType<BgmPlayer>();
        gameManager = FindFirstObjectByType<GameManager>();
        networkManager = FindFirstObjectByType<NetworkManager>();

        if (bgmPlayer) bgmPlayer.ChangeBgm();

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

        pointUiManager = FindFirstObjectByType<PlayerPointUiManager>();
        if (pointUiManager != null)
        {
            pointUiManager.InitialPlayerPointUi();
        }

        // 啟動填色系統
        if (colorGrid) colorGrid.EnableColoring();

        isGameRunning = true;

        if (countdownUI != null)
        {
            countdownUI.StartCountdown(gameTimeLimit, OnCountdownFinished);
        }
    }

    void Update()
    {
        if (!isGameRunning || colorGrid == null || pointUiManager == null) return;

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
        foreach (var kvp in playerScores)
        {
            // 直接設定玩家的 point 為百分比分數
            gameManager.playersInfo[kvp.Key].point = kvp.Value;
            pointUiManager.UpdatePlayerPointUi(kvp.Key);
        }
    }

    void OnCountdownFinished()
    {
        isGameRunning = false;

        // 停止填色
        if (colorGrid) colorGrid.DisableColoring();

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
}
