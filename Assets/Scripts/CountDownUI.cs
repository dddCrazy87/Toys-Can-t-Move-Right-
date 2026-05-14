using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class CountDownUI : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds1 = new WaitForSeconds(1f);
    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
    [Header("UI 元件設定")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lastSecondsColor = Color.red;

    [Header("動畫設定")]
    [SerializeField] private float scaleUpFactor = 1.3f;    // 每秒放大倍率
    [SerializeField] private float scaleAnimDuration = 0.25f; // 放大動畫時間
    [SerializeField] private bool hideWhenFinished = true;  // 結束時是否隱藏

    [Header("最後 10 秒 BGM 加速")]
    [SerializeField] private float lastSecondsBgmPitch = 1.2f; // 加速後的 pitch

    private Coroutine countdownCoroutine;
    private Action onCountdownFinish;
    private BgmPlayer bgmPlayer;

    public void StartCountdown(float seconds, Action onFinish = null)
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        gameObject.SetActive(true);
        countdownCoroutine = StartCoroutine(CountdownRoutine(seconds));
        onCountdownFinish = onFinish;
    }


    public void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        gameObject.SetActive(false);
    }

    private IEnumerator CountdownRoutine(float totalSeconds)
    {
        float timeLeft = totalSeconds;
        Vector3 baseScale = countdownText.transform.localScale;
        bgmPlayer = FindFirstObjectByType<BgmPlayer>();

        while (timeLeft > 0f)
        {
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            countdownText.text = $"{minutes:00}:{seconds:00}";

            countdownText.color = timeLeft <= 10f ? lastSecondsColor : normalColor;

            // 最後 15 秒漸漸加速 BGM
            if (timeLeft <= 15f && bgmPlayer != null)
            {
                float t = 1f - (timeLeft / 15f); // 0 → 1
                bgmPlayer.SetPitch(Mathf.Lerp(1f, lastSecondsBgmPitch, t));
            }

            // 動畫：縮放效果
            StartCoroutine(ScaleText(countdownText.transform, baseScale * scaleUpFactor, scaleAnimDuration));

            yield return _waitForSeconds1;
            timeLeft -= 1f;
        }

        countdownText.text = "00:00";
        countdownText.color = normalColor;

        // 恢復 BGM 速度
        if (bgmPlayer != null)
        {
            bgmPlayer.SetPitch(1f);
        }

        // 最後一個縮放動畫
        StartCoroutine(ScaleText(countdownText.transform, baseScale * scaleUpFactor, scaleAnimDuration));

        yield return _waitForSeconds0_5;

        if (hideWhenFinished)
            gameObject.SetActive(false);

        onCountdownFinish?.Invoke();
        countdownCoroutine = null;
    }

    private IEnumerator ScaleText(Transform target, Vector3 targetScale, float duration)
    {
        Vector3 startScale = target.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            target.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        // 回到原始大小
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            target.localScale = Vector3.Lerp(targetScale, Vector3.one, t);
            yield return null;
        }
    }
}
