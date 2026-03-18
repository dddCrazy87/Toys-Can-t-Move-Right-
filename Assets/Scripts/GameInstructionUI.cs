using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 遊戲說明圖 UI
/// 在遊戲開始前顯示說明圖，按任意鍵或等待指定時間後關閉
/// </summary>
public class GameInstructionUI : MonoBehaviour
{
    [Header("UI 設定")]
    [SerializeField] private GameObject instructionPanel;  // 說明圖面板
    [SerializeField] private Image instructionImage;       // 說明圖 Image
    [SerializeField] private CanvasGroup canvasGroup;      // 用於淡入淡出（可選）

    [Header("時間設定")]
    [SerializeField] private float displayDuration = 5f;   // 顯示時間（秒）
    [SerializeField] private float fadeInDuration = 0.3f;  // 淡入時間
    [SerializeField] private float fadeOutDuration = 0.3f; // 淡出時間

    [Header("提示文字")]
    [SerializeField] private GameObject skipHintText;      // 「按任意鍵跳過」提示（可選）

    private bool isShowing = false;
    private Action onCompleteCallback;

    void Awake()
    {
        // 初始隱藏
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 顯示說明圖，完成後呼叫 callback
    /// </summary>
    public void ShowInstruction(Action onComplete = null)
    {
        onCompleteCallback = onComplete;
        StartCoroutine(ShowInstructionCoroutine());
    }

    /// <summary>
    /// 顯示說明圖（指定圖片），完成後呼叫 callback
    /// </summary>
    public void ShowInstruction(Sprite instructionSprite, Action onComplete = null)
    {
        if (instructionImage != null && instructionSprite != null)
        {
            instructionImage.sprite = instructionSprite;
        }
        onCompleteCallback = onComplete;
        StartCoroutine(ShowInstructionCoroutine());
    }

    private IEnumerator ShowInstructionCoroutine()
    {
        if (instructionPanel == null)
        {
            Debug.LogWarning("[GameInstructionUI] instructionPanel 未設定！");
            onCompleteCallback?.Invoke();
            yield break;
        }

        isShowing = true;

        // 顯示面板
        instructionPanel.SetActive(true);

        // 淡入效果
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeInDuration));
        }

        // 顯示跳過提示
        if (skipHintText != null)
        {
            skipHintText.SetActive(true);
        }

        // 等待指定時間或按任意鍵
        float elapsedTime = 0f;

        while (elapsedTime < displayDuration)
        {
            // 檢查按任意鍵跳過
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                Debug.Log("[GameInstructionUI] 按鍵跳過");
                break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 淡出效果
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, fadeOutDuration));
        }

        // 隱藏面板
        instructionPanel.SetActive(false);
        isShowing = false;

        // 呼叫完成回調
        Debug.Log("[GameInstructionUI] 說明圖結束，開始遊戲流程");
        onCompleteCallback?.Invoke();
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    /// <summary>
    /// 強制關閉說明圖
    /// </summary>
    public void ForceClose()
    {
        StopAllCoroutines();
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
        isShowing = false;
    }

    /// <summary>
    /// 是否正在顯示
    /// </summary>
    public bool IsShowing()
    {
        return isShowing;
    }
}
