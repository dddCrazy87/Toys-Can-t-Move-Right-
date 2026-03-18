using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToyBoxGameManager : MonoBehaviour
{
    [SerializeField] private CountDownUI countdownUI;
    [SerializeField] private SceneFadeInFadeOut sceneFadeInFadeOut;
    [SerializeField] private float gameTimeLimit = 90f;
    [SerializeField] private AudioSource gameOverAudio;

    [Header("遊戲說明")]
    [SerializeField] private GameInstructionUI instructionUI;

    BgmPlayer bgmPlayer;
    NetworkManager networkManager;
    GameManager gameManager;

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
            Debug.Log("[ToyBoxGameManager] 顯示說明圖...");
            instructionUI.ShowInstruction(OnInstructionComplete);
        }
        else
        {
            // 沒有說明圖，直接開始倒數
            Debug.LogWarning("[ToyBoxGameManager] instructionUI 未設定，跳過說明圖");
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
            OnCountDownFinished();
        }
    }

    void OnCountDownFinished()
    {
        gameManager.StartGame();

        // 倒數結束後才播放 BGM
        if (bgmPlayer) bgmPlayer.ChangeBgm();

        FindFirstObjectByType<PlayerPointUiManager>().InitialPlayerPointUi();
        countdownUI.StartCountdown(gameTimeLimit, OnCountdownFinished);
    }

    void OnCountdownFinished()
    {
        if (bgmPlayer) bgmPlayer.PauseBGM();
        gameOverAudio.Play();
        Invoke(nameof(LoadNextSceneWithFadeOut), 1f);
    }

    void LoadNextSceneWithFadeOut()
    {
        if (networkManager) networkManager.BroadcastTerminate();
        sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
    }
}
