using System.Collections;
using UnityEngine;

/// <summary>
/// Toast 提示：延遲 → 淡入 → 停留 → 淡出 → 自動隱藏
/// 需要 CanvasGroup 元件（會自動加上）
/// </summary>
public class ToastUI : MonoBehaviour
{
    [Header("時間設定")]
    [SerializeField] private float delayBeforeShow = 2f;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.6f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 用 alpha 隱藏，保持 active 才能啟動 coroutine
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 顯示 Toast
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);

        // Awake 可能還沒跑過（物件一開始是 inactive）
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;

        StopAllCoroutines();
        StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        // 延遲
        canvasGroup.alpha = 0;
        yield return new WaitForSeconds(delayBeforeShow);

        // 淡入
        canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 停留
        yield return new WaitForSeconds(displayDuration);

        // 淡出
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}
