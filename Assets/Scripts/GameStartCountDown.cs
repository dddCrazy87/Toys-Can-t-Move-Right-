using System;
using UnityEngine;

public class GameStartCountDown : MonoBehaviour
{
    public UIFadeScript countDown3, countDown2, countDown1;
    private Action onCountDownFinished;

    [Header("倒數音效（分開的四個音檔）")]
    [SerializeField] private AudioClip beep3Sound;  // 顯示 3 時播放
    [SerializeField] private AudioClip beep2Sound;  // 顯示 2 時播放
    [SerializeField] private AudioClip beep1Sound;  // 顯示 1 時播放
    [SerializeField] private AudioClip startSound;  // 遊戲開始時播放（嗶-）
    [SerializeField] [Range(0f, 1f)] private float countdownVolume = 1f;

    private AudioSource audioSource;

    // --------Count Down And Start Game--------
    public void CountDownAndStartGame(Action onFinished = null)
    {
        onCountDownFinished = onFinished;

        countDown3.gameObject.SetActive(true);
        countDown2.gameObject.SetActive(true);
        countDown1.gameObject.SetActive(true);

        // 播放 "3" 的音效
        PlaySound(beep3Sound);

        countDown3.ShowUI(2f);
        Invoke(nameof(CountDown3), 1.5f);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;  // 2D 音效
        }

        audioSource.PlayOneShot(clip, countdownVolume);
    }

    void CountDown3()
    {
        countDown3.HideUI(2f);
        Invoke(nameof(ShowCountDown2), 0.7f);
    }

    void ShowCountDown2()
    {
        PlaySound(beep2Sound);
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
        PlaySound(beep1Sound);
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
        PlaySound(startSound);
        onCountDownFinished?.Invoke();
        onCountDownFinished = null;
    }
}
