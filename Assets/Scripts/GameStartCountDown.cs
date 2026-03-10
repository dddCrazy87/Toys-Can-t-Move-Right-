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

        // 播放 "3" 的音效，同時顯示 3
        PlaySound(beep3Sound);
        countDown3.ShowUI(0.3f);

        // 1 秒後進入下一階段
        Invoke(nameof(ShowCountDown2), 1.0f);
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

    void ShowCountDown2()
    {
        // 隱藏 3，播放音效，顯示 2
        countDown3.HideUI(0.3f);
        PlaySound(beep2Sound);
        countDown2.ShowUI(0.3f);

        // 1 秒後進入下一階段
        Invoke(nameof(ShowCountDown1), 1.0f);
    }

    void ShowCountDown1()
    {
        // 隱藏 2，播放音效，顯示 1
        countDown2.HideUI(0.3f);
        PlaySound(beep1Sound);
        countDown1.ShowUI(0.3f);

        // 1 秒後進入開始階段
        Invoke(nameof(StartGame), 1.0f);
    }

    void StartGame()
    {
        // 隱藏 1，播放開始音效
        countDown1.HideUI(0.3f);
        PlaySound(startSound);
        onCountDownFinished?.Invoke();
        onCountDownFinished = null;
    }
}
