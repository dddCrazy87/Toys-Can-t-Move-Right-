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

        var pointUiManager = FindFirstObjectByType<PlayerPointUiManager>();
        if (pointUiManager != null)
        {
            pointUiManager.InitialPlayerPointUi();
        }

        // 啟動填色系統
        if (colorGrid) colorGrid.EnableColoring();

        if (countdownUI != null)
        {
            countdownUI.StartCountdown(gameTimeLimit, OnCountdownFinished);
        }
    }

    void OnCountdownFinished()
    {
        // 停止填色
        if (colorGrid) colorGrid.DisableColoring();

        // 計算最終分數
        CalculateFinalScores();

        if (bgmPlayer) bgmPlayer.PauseBGM();
        if (gameOverAudio) gameOverAudio.Play();
        Invoke(nameof(LoadNextSceneWithFadeOut), 1f);
    }

    void CalculateFinalScores()
    {
        if (colorGrid == null) return;

        // 取得每個玩家的填色數量，更新分數
        var playerScores = colorGrid.GetPlayerScores();
        foreach (var kvp in playerScores)
        {
            int playerIndex = kvp.Key;
            int score = kvp.Value;
            gameManager.IncreasePlayerPoint(playerIndex, score);
        }

        // 更新 UI
        var pointUiManager = FindFirstObjectByType<PlayerPointUiManager>();
        if (pointUiManager)
        {
            foreach (var kvp in playerScores)
            {
                pointUiManager.UpdatePlayerPointUi(kvp.Key);
            }
        }
    }

    void LoadNextSceneWithFadeOut()
    {
        if (networkManager) networkManager.BroadcastTerminate();
        sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
    }
}
