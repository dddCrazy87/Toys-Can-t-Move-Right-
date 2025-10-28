using System;
using UnityEngine;

public class GameStartCountDown : MonoBehaviour
{
    public UIFadeScript countDown3, countDown2, countDown1;
    private Action onCountDownFinished;

    // --------Count Down And Start Game--------
    public void CountDownAndStartGame(Action onFinished = null)
    {
        onCountDownFinished = onFinished;

        countDown3.gameObject.SetActive(true);
        countDown2.gameObject.SetActive(true);
        countDown1.gameObject.SetActive(true);

        countDown3.ShowUI(2f);
        Invoke(nameof(CountDown3), 1.5f);
    }

    void CountDown3()
    {
        countDown3.HideUI(2f);
        Invoke(nameof(ShowCountDown2), 0.7f);
    }

    void ShowCountDown2()
    {
        countDown2.ShowUI(2f);
        Invoke(nameof(CountDown2), 1.5f);
    }

    void CountDown2()
    {
        countDown2.HideUI(2f);
        Invoke(nameof(ShowCountDown1), 0.7f);
    }

    void ShowCountDown1()
    {
        countDown1.ShowUI(2f);
        Invoke(nameof(CountDown1), 1.5f);
    }

    void CountDown1()
    {
        countDown1.HideUI(2f);
        Invoke(nameof(StartGame), 1f);
    }

    void StartGame()
    {
        onCountDownFinished?.Invoke();
        onCountDownFinished = null;
    }
}
