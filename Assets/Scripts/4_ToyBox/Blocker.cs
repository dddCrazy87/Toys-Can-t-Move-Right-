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

    [Header("得分感應物件")]
    public GameObject pointTrigger;

    private bool isActive = false;
    private Vector3 initialPosition; // 用來記錄遊戲開始時的初始位置

    private void Start()
    {
        initialPosition = transform.position;
    }

    // ++ 新增：讓外部事前檢查是否可觸發 ++
    public bool CanActivate()
    {
        return !isActive;
    }

    // ++ 新增：純粹負責觸發效果 ++
    public void Activate()
    {
        if (isActive) return;
        StartCoroutine(ActivateRoutine());
    }

    // (為了相容性保留原本的方法，如果你其他腳本沒有呼叫到這個，也可以刪除)
    public bool TryActivate()
    {
        if (isActive) return false;
        StartCoroutine(ActivateRoutine());
        return true;
    }

    private IEnumerator ActivateRoutine()
    {
        isActive = true;

        pointTrigger.SetActive(false);
        FindFirstObjectByType<GameSoundEffect>()?.PlayBlockerSound();

        // 1. 從初始位置移動到指定位置
        yield return StartCoroutine(MoveToPosition(targetPosition));

        // 2. 停留在該位置等待設定的秒數
        yield return new WaitForSeconds(activeDuration);

        // 3. 從指定位置移回初始位置
        yield return StartCoroutine(MoveToPosition(initialPosition));

        isActive = false; // 恢復可觸發狀態

        pointTrigger.SetActive(true);
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


