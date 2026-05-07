using UnityEngine;
using System.Collections;

public class Blocker : MonoBehaviour
{
    [Header("柵欄物件")]
    public GameObject blocker;

    [Header("持續時間")]
    public float activeDuration = 5f;

    private bool isActive = false;

    private void Start()
    {
        // 遊戲一開始先確保它是隱藏的
        if (blocker != null) blocker.SetActive(false);
    }

    // 讓玩家呼叫的方法。回傳 true 代表成功觸發，回傳 false 代表正在忙
    public bool TryActivate()
    {
        // 如果已經在發動中，或是根本沒設定目標物件，就拒絕觸發
        if (isActive || blocker == null) return false;

        StartCoroutine(ActivateRoutine());
        return true;
    }

    private IEnumerator ActivateRoutine()
    {
        isActive = true;
        blocker.SetActive(true); // 顯示物件

        yield return new WaitForSeconds(activeDuration); // 等待秒數

        blocker.SetActive(false); // 隱藏物件
        isActive = false; // 恢復可觸發狀態
    }
}
