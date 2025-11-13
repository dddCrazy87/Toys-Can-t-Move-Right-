using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToyBoxGameManager : MonoBehaviour
{
    [SerializeField] private CountDownUI countdownUI;
    [SerializeField] private SceneFadeInFadeOut sceneFadeInFadeOut;
    [SerializeField] private float gameTimeLimit = 90f;
    [SerializeField] private AudioSource gameOverAudio;
    void Start()
    {
        FindFirstObjectByType<BgmPlayer>().ChangeBgm();
        FindFirstObjectByType<GameStartCountDown>().CountDownAndStartGame(OnCountDownFinished);
    }

    void OnCountDownFinished()
    {
        FindFirstObjectByType<GameManager>().StartGame();
        FindFirstObjectByType<PlayerPointUiManager>().InitialPlayerPointUi();
        countdownUI.StartCountdown(gameTimeLimit, OnCountdownFinished);
    }

    void OnCountdownFinished()
    {
        FindFirstObjectByType<BgmPlayer>().PauseBGM();
        gameOverAudio.Play();
        Invoke(nameof(LoadNextSceneWithFadeOut), 1f);
    }

    void LoadNextSceneWithFadeOut()
    {
        FindFirstObjectByType<NetworkManager>().BroadcastTerminate();
        sceneFadeInFadeOut.LoadNextSceneWithFadeOut();
    }
}
