using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToyBoxGameManager : MonoBehaviour
{
    [SerializeField] private CountDownUI countdownUI;
    [SerializeField] private SceneFadeInFadeOut sceneFadeInFadeOut;
    [SerializeField] private float gameTimeLimit = 90f;
    [SerializeField] private AudioSource gameOverAudio;
    BgmPlayer bgmPlayer;
    NetworkManager networkManager;
    GameManager gameManager;

    void Start()
    {
        bgmPlayer = FindFirstObjectByType<BgmPlayer>();
        gameManager = FindFirstObjectByType<GameManager>();
        networkManager = FindFirstObjectByType<NetworkManager>();
        if (bgmPlayer) bgmPlayer.ChangeBgm();
        FindFirstObjectByType<GameStartCountDown>().CountDownAndStartGame(OnCountDownFinished);
    }

    void OnCountDownFinished()
    {
        gameManager.StartGame();
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
