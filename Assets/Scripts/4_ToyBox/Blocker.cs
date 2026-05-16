using UnityEngine;
using System.Collections;

public class Blocker : MonoBehaviour
{
    [Header("目標位置 (世界座標)")]
    public Vector3 targetPosition;

    [Header("移動花費時間")]
    public float moveDuration = 0.5f;

    [Header("停留持續時間")]
    public float activeDuration = 5f;

    private bool isActive = false;
    private Vector3 initialPosition; // 用來記錄遊戲開始時的初始位置

    private void Start()
    {
        // 遊戲一開始先記錄該物件的初始位置
        initialPosition = transform.position;
    }

    // 讓玩家呼叫的方法。回傳 true 代表成功觸發，回傳 false 代表正在忙
    public bool TryActivate()
    {
        // 如果已經在發動中，或是根本沒設定目標物件，就拒絕觸發
        if (isActive) return false;

        StartCoroutine(ActivateRoutine());
        return true;
    }

    private IEnumerator ActivateRoutine()
    {
        isActive = true;

        // 1. 從初始位置移動到指定位置
        yield return StartCoroutine(MoveToPosition(targetPosition));

        // 2. 停留在該位置等待設定的秒數
        yield return new WaitForSeconds(activeDuration);

        // 3. 從指定位置移回初始位置
        yield return StartCoroutine(MoveToPosition(initialPosition));

        isActive = false; // 恢復可觸發狀態
    }

    // 負責處理平滑移動的協程
    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            // 根據經過的時間比例，平滑計算當前位置
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // 等待下一幀
        }

        // 迴圈結束後，強制對齊到精確的目標位置，避免浮點數誤差
        transform.position = targetPos;
    }
}


